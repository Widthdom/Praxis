using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Praxis.Core.Logic;
using Praxis.Core.Services;

namespace Praxis.Core.Models;

public partial class MainModel : ObservableObject
{
    private const int MaxDockButtons = 20;
    private const int MaxCommandSuggestions = 30;

    private readonly ILauncherExecutionService executionService;
    private readonly ILauncherButtonRepository repository;
    private readonly IStateSyncNotifier? stateSyncNotifier;
    private readonly ActionHistory<ButtonHistoryAction> history = new();
    private readonly Dictionary<Guid, LauncherButtonRecord> dragStart = [];
    private readonly List<LauncherButtonModel> dragTargets = [];
    private readonly List<LauncherButtonModel> filteredButtons = [];
    private const double ViewportMargin = 240;
    private double viewportX;
    private double viewportY;
    private double viewportWidth;
    private double viewportHeight;

    [ObservableProperty]
    private string commandText = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isCommandSuggestionOpen;

    [ObservableProperty]
    private CommandSuggestionModel? selectedCommandSuggestion;

    [ObservableProperty]
    private int selectedCommandSuggestionIndex = -1;

    [ObservableProperty]
    private bool isContextMenuOpen;

    [ObservableProperty]
    private LauncherButtonModel? contextMenuTarget;

    [ObservableProperty]
    private bool isEditorOpen;

    [ObservableProperty]
    private LauncherButtonModel? editorButton;

    [ObservableProperty]
    private ThemeMode selectedTheme = ThemeMode.System;

    [ObservableProperty]
    private bool isEditorCreatingNewButton;

    [ObservableProperty]
    private bool isConflictDialogOpen;

    [ObservableProperty]
    private string conflictTitle = "Database conflict";

    [ObservableProperty]
    private string conflictMessage = "Another Praxis window changed this button. Reload the latest data or overwrite it with the current edit.";

    [ObservableProperty]
    private double placementSurfaceWidth;

    [ObservableProperty]
    private double placementSurfaceHeight;

    private bool editorCreatesNewButton;
    private bool pendingExternalReload;
    private int externalReloadInProgress;
    private EditorConflictContext? pendingConflict;

    public MainModel(
        ILauncherExecutionService executionService,
        ILauncherButtonRepository repository,
        IStateSyncNotifier? stateSyncNotifier = null)
    {
        this.executionService = executionService;
        this.repository = repository;
        this.stateSyncNotifier = stateSyncNotifier;
    }

    public ObservableCollection<LauncherButtonModel> Buttons { get; } = [];

    public ObservableCollection<LauncherButtonModel> VisibleButtons { get; } = [];

    public ObservableCollection<LauncherButtonModel> RecentButtons { get; } = [];

    public ObservableCollection<CommandSuggestionModel> CommandSuggestions { get; } = [];

    public StatusModel Status { get; } = new();

    public void UpdateViewport(double scrollX, double scrollY, double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        viewportX = Math.Max(0, scrollX);
        viewportY = Math.Max(0, scrollY);
        viewportWidth = width;
        viewportHeight = height;
        RefreshPlacementSurfaceExtent();
        RefreshVisibleButtons();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Status.Set(LauncherStatusKind.Busy, "Loading buttons...");
        try
        {
            await repository.InitializeAsync(cancellationToken);
            var records = await repository.GetButtonsAsync(cancellationToken);
            Buttons.Clear();
            RecentButtons.Clear();
            foreach (var record in records)
            {
                Buttons.Add(LauncherButtonModelMapper.FromRecord(record));
            }

            ApplyFilter();
            RefreshCommandSuggestions();
            await RestoreDockAsync(cancellationToken);

            Status.Set(
                LauncherStatusKind.Idle,
                Buttons.Count == 0 ? "No buttons yet." : $"Loaded {Buttons.Count} buttons.");
        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Load failed: {ex.Message}");
        }
    }

    public void AddButton()
    {
        var ordinal = Buttons.Count + 1;
        var button = new LauncherButtonModel
        {
            Text = "New",
            X = 48 + ((ordinal - 1) % 4) * 148,
            Y = 48 + ((ordinal - 1) / 4) * 76,
            ColorKey = LauncherButtonColorKey.Default,
            ToolTip = "New launcher button",
        };

        Buttons.Add(button);
        ApplyFilter();
        RefreshCommandSuggestions();
        Status.Set(LauncherStatusKind.Success, "Button added.");
    }

    public async Task AddButtonAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        OpenNewButtonEditor(null);
    }

    public async Task ExecuteButtonAsync(
        LauncherButtonModel? button,
        CancellationToken cancellationToken = default)
    {
        if (button is null)
        {
            Status.Set(LauncherStatusKind.Error, "No button selected.");
            return;
        }

        await ExecuteButtonCoreAsync(button, source: "button", updateStatus: true, cancellationToken);
    }

    public async Task ExecuteCommandInputAsync(CancellationToken cancellationToken = default)
    {
        var command = CommandText.Trim();
        if (SelectedCommandSuggestion is not null && IsCommandSuggestionOpen)
        {
            command = SelectedCommandSuggestion.Command;
            CommandText = command;
        }

        CloseCommandSuggestions();
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        try
        {
            var targets = CommandRecordMatcher
                .FindMatches(Buttons.Select(LauncherButtonModelMapper.ToRecord), command)
                .Select(record => FindButtonById(record.Id))
                .Where(static button => button is not null)
                .Cast<LauncherButtonModel>()
                .ToList();

            if (targets.Count == 0)
            {
                var fallback = await repository.GetByCommandAsync(command, cancellationToken);
                if (fallback is not null)
                {
                    targets.Add(FindButtonById(fallback.Id) ?? LauncherButtonModelMapper.FromRecord(fallback));
                }
            }

            if (targets.Count == 0)
            {
                Status.Set(LauncherStatusKind.Error, $"Command not found: {command}");
                return;
            }

            if (targets.Count == 1)
            {
                await ExecuteButtonCoreAsync(targets[0], source: "command", updateStatus: true, cancellationToken);
                return;
            }

            var successCount = 0;
            string? lastFailureMessage = null;
            foreach (var target in targets)
            {
                var result = await ExecuteButtonCoreAsync(target, source: "command", updateStatus: false, cancellationToken);
                if (result.Succeeded)
                {
                    successCount++;
                }
                else
                {
                    lastFailureMessage = result.Message;
                }
            }

            Status.Set(
                successCount == targets.Count ? LauncherStatusKind.Success : LauncherStatusKind.Error,
                successCount == targets.Count
                    ? $"Executed {targets.Count} commands."
                    : $"Executed {successCount}/{targets.Count}. Last error: {lastFailureMessage}");
        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Command failed: {ex.Message}");
        }
    }

    public async Task PickSuggestionAsync(
        CommandSuggestionModel? suggestion,
        CancellationToken cancellationToken = default)
    {
        if (suggestion is null)
        {
            return;
        }

        CommandText = suggestion.Command;
        CloseCommandSuggestions();
        await ExecuteButtonCoreAsync(suggestion.Source, source: "command", updateStatus: true, cancellationToken);
    }

    public void MoveSuggestionUp()
    {
        if (CommandSuggestions.Count == 0)
        {
            return;
        }

        if (!IsCommandSuggestionOpen)
        {
            IsCommandSuggestionOpen = true;
        }

        var next = SelectedCommandSuggestionIndex <= 0
            ? CommandSuggestions.Count - 1
            : SelectedCommandSuggestionIndex - 1;
        SetSelectedSuggestionIndex(next);
    }

    public void MoveSuggestionDown()
    {
        if (CommandSuggestions.Count == 0)
        {
            return;
        }

        if (!IsCommandSuggestionOpen)
        {
            IsCommandSuggestionOpen = true;
        }

        var next = SelectedCommandSuggestionIndex < 0 || SelectedCommandSuggestionIndex >= CommandSuggestions.Count - 1
            ? 0
            : SelectedCommandSuggestionIndex + 1;
        SetSelectedSuggestionIndex(next);
    }

    public void MoveSuggestionDownFromInput()
    {
        if (CommandSuggestions.Count == 0)
        {
            return;
        }

        if (!IsCommandSuggestionOpen)
        {
            IsCommandSuggestionOpen = true;
        }

        var next = SelectedCommandSuggestionIndex < 0 || SelectedCommandSuggestionIndex >= CommandSuggestions.Count - 1
            ? 0
            : SelectedCommandSuggestionIndex + 1;
        SetSelectedSuggestionIndex(next);
    }

    public void SelectSuggestion(CommandSuggestionModel? suggestion)
    {
        if (suggestion is null)
        {
            return;
        }

        var index = CommandSuggestions.IndexOf(suggestion);
        if (index >= 0)
        {
            SetSelectedSuggestionIndex(index);
        }
    }

    public void CloseCommandSuggestions()
    {
        foreach (var suggestion in CommandSuggestions)
        {
            suggestion.IsSelected = false;
        }

        IsCommandSuggestionOpen = false;
        SelectedCommandSuggestion = null;
        SelectedCommandSuggestionIndex = -1;
    }

    public void ClearCommandText()
    {
        CommandText = string.Empty;
        CloseCommandSuggestions();
    }

    public void ClearSearchText()
    {
        SearchText = string.Empty;
    }

    public async Task DeleteButtonAsync(
        LauncherButtonModel? button,
        CancellationToken cancellationToken = default)
    {
        if (button is null)
        {
            return;
        }

        try
        {
            var targets = ResolveDeleteTargets(button);
            var beforeRecords = targets.Select(LauncherButtonModelMapper.ToRecord).ToList();
            foreach (var target in targets)
            {
                await repository.DeleteButtonAsync(target.Id, cancellationToken);
                Buttons.Remove(target);
                filteredButtons.Remove(target);
                VisibleButtons.Remove(target);
                RecentButtons.Remove(target);
                history.Push(new ButtonHistoryAction
                {
                    Kind = ButtonHistoryActionKind.Delete,
                    Before = beforeRecords.First(record => record.Id == target.Id),
                });
            }

            if (ContextMenuTarget is not null && targets.Any(target => ReferenceEquals(ContextMenuTarget, target)))
            {
                CloseContextMenu();
            }

            ApplyFilter();
            RefreshCommandSuggestions();
            await TryNotifyButtonsChangedAsync(cancellationToken);
            Status.Set(
                LauncherStatusKind.Success,
                targets.Count == 1 ? "Button deleted." : $"{targets.Count} buttons deleted.");
        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Delete failed: {ex.Message}");
        }
    }

    private IReadOnlyList<LauncherButtonModel> ResolveDeleteTargets(LauncherButtonModel button)
    {
        if (!button.IsSelected)
        {
            return [button];
        }

        var selected = Buttons.Where(static candidate => candidate.IsSelected).ToList();
        return selected.Count == 0 ? [button] : selected;
    }

    public void OpenContextMenu(LauncherButtonModel? button)
    {
        if (button is null)
        {
            return;
        }

        ContextMenuTarget = button;
        IsContextMenuOpen = true;
        CloseCommandSuggestions();
    }

    public void CloseContextMenu()
    {
        ContextMenuTarget = null;
        IsContextMenuOpen = false;
    }

    public void ToggleSelection(LauncherButtonModel? button)
    {
        if (button is null)
        {
            return;
        }

        button.IsSelected = !button.IsSelected;
    }

    public void ApplySelection(SelectionPayload? payload)
    {
        if (payload is null)
        {
            return;
        }

        if (payload.Status == InteractionStatus.Started)
        {
            foreach (var button in Buttons)
            {
                button.IsSelected = false;
            }
        }

        var left = Math.Min(payload.StartX, payload.CurrentX);
        var top = Math.Min(payload.StartY, payload.CurrentY);
        var right = Math.Max(payload.StartX, payload.CurrentX);
        var bottom = Math.Max(payload.StartY, payload.CurrentY);

        foreach (var button in Buttons)
        {
            button.IsSelected =
                button.X >= left &&
                button.Y >= top &&
                button.X + button.Width <= right &&
                button.Y + button.Height <= bottom;
        }
    }

    public async Task HandleButtonDragAsync(
        ButtonDragPayload? payload,
        CancellationToken cancellationToken = default)
    {
        if (payload?.Button is null)
        {
            return;
        }

        if (payload.Status == InteractionStatus.Started)
        {
            dragStart.Clear();
            dragTargets.Clear();
            var selected = Buttons.Where(static button => button.IsSelected).ToList();
            if (selected.Count > 0 && payload.Button.IsSelected)
            {
                dragTargets.AddRange(selected);
            }
            else
            {
                dragTargets.Add(payload.Button);
            }

            foreach (var target in dragTargets)
            {
                dragStart[target.Id] = LauncherButtonModelMapper.ToRecord(target);
            }

            return;
        }

        if (dragTargets.Count == 0)
        {
            return;
        }

        foreach (var target in dragTargets)
        {
            if (!dragStart.TryGetValue(target.Id, out var origin))
            {
                continue;
            }

            var nextX = GridSnapper.Snap(origin.X + payload.TotalX);
            var nextY = GridSnapper.Snap(origin.Y + payload.TotalY);
            if (nextX < 0 || nextY < 0)
            {
                return;
            }
        }

        foreach (var target in dragTargets)
        {
            if (!dragStart.TryGetValue(target.Id, out var origin))
            {
                continue;
            }

            target.X = Math.Max(0, GridSnapper.Snap(origin.X + payload.TotalX));
            target.Y = Math.Max(0, GridSnapper.Snap(origin.Y + payload.TotalY));
        }

        ApplyFilter();

        if (payload.Status != InteractionStatus.Completed)
        {
            return;
        }

        try
        {
            var beforeRecords = dragTargets
                .Select(target => dragStart.TryGetValue(target.Id, out var origin) ? origin : null)
                .Where(static record => record is not null)
                .Cast<LauncherButtonRecord>()
                .ToList();
            foreach (var target in dragTargets)
            {
                await SaveButtonAsync(target, cancellationToken);
            }

            foreach (var target in dragTargets)
            {
                var before = beforeRecords.FirstOrDefault(record => record.Id == target.Id);
                if (before is not null)
                {
                    history.Push(new ButtonHistoryAction
                    {
                        Kind = ButtonHistoryActionKind.Update,
                        Before = before,
                        After = LauncherButtonModelMapper.ToRecord(target),
                    });
                }
            }

            await TryNotifyButtonsChangedAsync(cancellationToken);

        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Move failed: {ex.Message}");
        }
        finally
        {
            dragStart.Clear();
            dragTargets.Clear();
        }
    }

    public void OpenEditor(LauncherButtonModel? button)
    {
        if (button is null)
        {
            return;
        }

        EditorButton = LauncherButtonModelMapper.FromRecord(LauncherButtonModelMapper.ToRecord(button));
        editorCreatesNewButton = false;
        IsEditorCreatingNewButton = false;
        IsEditorOpen = true;
        CloseContextMenu();
    }

    public void OpenNewButtonEditor(NewButtonPayload? payload)
    {
        var ordinal = Buttons.Count + 1;
        var x = payload?.HasPosition == true ? payload.X : 48 + ((ordinal - 1) % 4) * 148;
        var y = payload?.HasPosition == true ? payload.Y : 48 + ((ordinal - 1) / 4) * 76;
        EditorButton = new LauncherButtonModel
        {
            Text = "New",
            X = Math.Max(0, GridSnapper.Snap(x)),
            Y = Math.Max(0, GridSnapper.Snap(y)),
            ColorKey = LauncherButtonColorKey.Default,
        };
        editorCreatesNewButton = true;
        IsEditorCreatingNewButton = true;
        IsEditorOpen = true;
        CloseContextMenu();
    }

    public void CancelEditor()
    {
        EditorButton = null;
        editorCreatesNewButton = false;
        IsEditorCreatingNewButton = false;
        IsEditorOpen = false;
        if (pendingExternalReload)
        {
            pendingExternalReload = false;
            _ = ReloadFromExternalChangeAsync();
        }
    }

    public async Task SaveEditorAsync(CancellationToken cancellationToken = default)
        => await SaveEditorCoreAsync(forceOverwrite: false, cancellationToken);

    private async Task SaveEditorCoreAsync(bool forceOverwrite, CancellationToken cancellationToken = default)
    {
        if (EditorButton is null)
        {
            return;
        }

        var draft = EditorButton;
        var existing = FindButtonById(draft.Id);
        var before = existing is null ? null : LauncherButtonModelMapper.ToRecord(existing);
        var record = LauncherButtonModelMapper.ToRecord(draft);

        if (!forceOverwrite && !editorCreatesNewButton)
        {
            var latest = await repository.GetByIdAsync(record.Id, forceReload: true, cancellationToken);
            if (latest is null || (before is not null && RecordVersionComparer.HasConflict(before, latest)))
            {
                pendingConflict = new EditorConflictContext
                {
                    EditingRecord = record,
                    LatestRecord = latest,
                    ConflictType = latest is null ? EditorConflictType.DeletedByOtherWindow : EditorConflictType.UpdatedByOtherWindow,
                };
                ConflictTitle = latest is null ? "Button deleted in another window" : "Button changed in another window";
                ConflictMessage = latest is null
                    ? "This button was deleted in another Praxis window. Reload to close this editor, or overwrite to save it again."
                    : "This button was changed in another Praxis window. Reload the latest data, or overwrite it with the current edit.";
                IsConflictDialogOpen = true;
                return;
            }
        }

        var target = existing;
        if (target is null)
        {
            target = LauncherButtonModelMapper.FromRecord(record);
            Buttons.Add(target);
        }
        else
        {
            CopyButtonState(draft, target);
        }

        try
        {
            await SaveButtonAsync(target, cancellationToken);
            ApplyFilter();
            RefreshCommandSuggestions();
            history.Push(new ButtonHistoryAction
            {
                Kind = editorCreatesNewButton ? ButtonHistoryActionKind.Add : ButtonHistoryActionKind.Update,
                Before = before,
                After = LauncherButtonModelMapper.ToRecord(target),
            });
            var message = editorCreatesNewButton ? "Button added." : "Button saved.";
            CancelEditor();
            await TryNotifyButtonsChangedAsync(cancellationToken);
            Status.Set(LauncherStatusKind.Success, message);
        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Save failed: {ex.Message}");
        }
    }

    public void ApplyTheme(ThemeMode mode)
    {
        SelectedTheme = mode;
        Status.Set(LauncherStatusKind.Success, $"Theme: {mode}");
        _ = TryNotifyButtonsChangedAsync();
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (!history.TryBeginUndo(out var action))
        {
            Status.Set(LauncherStatusKind.Idle, "Nothing to undo.");
            return;
        }

        var applied = await ApplyHistoryUndoAsync(action, cancellationToken);
        history.CompleteUndo(action, applied);
    }

    public void ReloadConflict()
    {
        if (pendingConflict?.LatestRecord is { } latest)
        {
            EditorButton = LauncherButtonModelMapper.FromRecord(latest);
            Status.Set(LauncherStatusKind.Success, "Reloaded latest changes.");
        }
        else
        {
            CancelEditor();
            Status.Set(LauncherStatusKind.Success, "Record was deleted in another window.");
        }

        pendingConflict = null;
        IsConflictDialogOpen = false;
    }

    public async void OverwriteConflict()
    {
        IsConflictDialogOpen = false;
        pendingConflict = null;
        await SaveEditorCoreAsync(forceOverwrite: true);
    }

    public void CancelConflict()
    {
        pendingConflict = null;
        IsConflictDialogOpen = false;
    }

    public async Task ReloadFromExternalChangeAsync(CancellationToken cancellationToken = default)
    {
        if (IsEditorOpen)
        {
            pendingExternalReload = true;
            return;
        }

        if (Interlocked.Exchange(ref externalReloadInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            var records = await repository.ReloadButtonsAsync(cancellationToken);
            Buttons.Clear();
            foreach (var record in records)
            {
                Buttons.Add(LauncherButtonModelMapper.FromRecord(record));
            }

            ApplyFilter();
            RefreshCommandSuggestions();
            await RestoreDockAsync(cancellationToken);
            Status.Set(LauncherStatusKind.Success, "Synced from another window.");
        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Sync failed: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref externalReloadInProgress, 0);
        }
    }

    public async Task RedoAsync(CancellationToken cancellationToken = default)
    {
        if (!history.TryBeginRedo(out var action))
        {
            Status.Set(LauncherStatusKind.Idle, "Nothing to redo.");
            return;
        }

        var applied = await ApplyHistoryRedoAsync(action, cancellationToken);
        history.CompleteRedo(action, applied);
    }

    public async Task MoveButtonAsync(
        LauncherButtonModel? button,
        double x,
        double y,
        CancellationToken cancellationToken = default)
    {
        if (button is null)
        {
            return;
        }

        try
        {
            button.X = Math.Max(0, GridSnapper.Snap(x));
            button.Y = Math.Max(0, GridSnapper.Snap(y));
            await SaveButtonAsync(button, cancellationToken);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Move failed: {ex.Message}");
        }
    }

    public async Task UpsertButtonAsync(
        LauncherButtonModel button,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(button);

        var existing = FindButtonById(button.Id);
        if (existing is null)
        {
            Buttons.Add(button);
        }
        else if (!ReferenceEquals(existing, button))
        {
            CopyButtonState(button, existing);
        }

        await SaveButtonAsync(existing ?? button, cancellationToken);
        ApplyFilter();
        RefreshCommandSuggestions();
        Status.Set(LauncherStatusKind.Success, "Saved.");
    }

    private async Task<LauncherExecutionResult> ExecuteButtonCoreAsync(
        LauncherButtonModel button,
        string source,
        bool updateStatus,
        CancellationToken cancellationToken)
    {
        button.IsExecuting = true;
        Status.Set(LauncherStatusKind.Busy, $"Launching {button.Text}...");
        try
        {
            var result = await executionService.ExecuteAsync(button, cancellationToken);
            if (updateStatus)
            {
                Status.Set(
                    result.Succeeded ? LauncherStatusKind.Success : LauncherStatusKind.Error,
                    result.Message);
            }

            if (result.Succeeded)
            {
                button.LastExecutedAtUtc = DateTime.UtcNow;
                await SaveButtonAsync(button, cancellationToken);
                await MoveDockButtonToFrontAsync(button, cancellationToken);
            }

            await TryAddLaunchLogAsync(button, result, source, cancellationToken);
            await TryPurgeLaunchLogsAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Launch failed: {ex.Message}");
            return LauncherExecutionResult.Failure(ex.Message);
        }
        finally
        {
            button.IsExecuting = false;
        }
    }

    private async Task MoveDockButtonToFrontAsync(
        LauncherButtonModel button,
        CancellationToken cancellationToken = default)
    {
        var existingIndex = RecentButtons.IndexOf(button);
        if (existingIndex >= 0)
        {
            RecentButtons.Move(existingIndex, 0);
        }
        else
        {
            RecentButtons.Insert(0, button);
        }

        while (RecentButtons.Count > MaxDockButtons)
        {
            RecentButtons.RemoveAt(RecentButtons.Count - 1);
        }

        await repository.SetDockButtonIdsAsync(RecentButtons.Select(static item => item.Id).ToList(), cancellationToken);
        await TryNotifyButtonsChangedAsync(cancellationToken);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnCommandTextChanged(string value)
    {
        RefreshCommandSuggestions();
    }

    private void ApplyFilter()
    {
        filteredButtons.Clear();
        foreach (var button in Buttons.Where(ButtonMatchesSearch))
        {
            filteredButtons.Add(button);
        }

        RefreshPlacementSurfaceExtent();
        RefreshVisibleButtons();
    }

    private void RefreshPlacementSurfaceExtent()
    {
        var extent = PlacementSurfaceExtentPolicy.Resolve(
            filteredButtons,
            viewportWidth,
            viewportHeight);

        PlacementSurfaceWidth = extent.Width;
        PlacementSurfaceHeight = extent.Height;
        ClampViewportToPlacementSurface();
    }

    private void ClampViewportToPlacementSurface()
    {
        viewportX = Math.Min(
            viewportX,
            Math.Max(0, PlacementSurfaceWidth - viewportWidth));
        viewportY = Math.Min(
            viewportY,
            Math.Max(0, PlacementSurfaceHeight - viewportHeight));
    }

    private void RefreshVisibleButtons()
    {
        IEnumerable<LauncherButtonModel> source = filteredButtons;
        if (viewportWidth > 0 && viewportHeight > 0)
        {
            var left = Math.Max(0, viewportX - ViewportMargin);
            var top = Math.Max(0, viewportY - ViewportMargin);
            var right = viewportX + viewportWidth + ViewportMargin;
            var bottom = viewportY + viewportHeight + ViewportMargin;

            source = source.Where(button =>
                button.X < right &&
                button.X + button.Width > left &&
                button.Y < bottom &&
                button.Y + button.Height > top);
        }

        ApplyVisibleButtonsDiff(source.ToList());
    }

    private void ApplyVisibleButtonsDiff(IReadOnlyList<LauncherButtonModel> target)
    {
        var targetSet = new HashSet<LauncherButtonModel>(target);
        for (var i = VisibleButtons.Count - 1; i >= 0; i--)
        {
            if (!targetSet.Contains(VisibleButtons[i]))
            {
                VisibleButtons.RemoveAt(i);
            }
        }

        for (var i = 0; i < target.Count; i++)
        {
            var desired = target[i];
            if (i < VisibleButtons.Count && ReferenceEquals(VisibleButtons[i], desired))
            {
                continue;
            }

            var existingIndex = VisibleButtons.IndexOf(desired);
            if (existingIndex >= 0)
            {
                VisibleButtons.Move(existingIndex, i);
            }
            else
            {
                VisibleButtons.Insert(i, desired);
            }
        }

        while (VisibleButtons.Count > target.Count)
        {
            VisibleButtons.RemoveAt(VisibleButtons.Count - 1);
        }
    }

    private bool ButtonMatchesSearch(LauncherButtonModel button)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return ButtonSearchMatcher.IsMatch(
            LauncherButtonModelMapper.ToRecord(button),
            SearchText);
    }

    private void RefreshCommandSuggestions()
    {
        CommandSuggestions.Clear();
        SelectedCommandSuggestion = null;
        SelectedCommandSuggestionIndex = -1;

        var query = CommandText.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            IsCommandSuggestionOpen = false;
            return;
        }

        var matches = Buttons
            .Where(static button => !string.IsNullOrWhiteSpace(button.Command))
            .Where(button => button.Command.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(button => button.Command.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(static button => button.Command)
            .ThenBy(static button => button.Text)
            .Take(MaxCommandSuggestions)
            .Select(static button => new CommandSuggestionModel(button));

        foreach (var suggestion in matches)
        {
            CommandSuggestions.Add(suggestion);
        }

        IsCommandSuggestionOpen = CommandSuggestions.Count > 0;
    }

    private void SetSelectedSuggestionIndex(int index)
    {
        if (index < 0 || index >= CommandSuggestions.Count)
        {
            foreach (var suggestion in CommandSuggestions)
            {
                suggestion.IsSelected = false;
            }

            SelectedCommandSuggestionIndex = -1;
            SelectedCommandSuggestion = null;
            return;
        }

        SelectedCommandSuggestionIndex = index;
        for (var i = 0; i < CommandSuggestions.Count; i++)
        {
            CommandSuggestions[i].IsSelected = i == index;
        }

        SelectedCommandSuggestion = CommandSuggestions[index];
    }

    private async Task RestoreDockAsync(CancellationToken cancellationToken)
    {
        RecentButtons.Clear();
        var knownById = Buttons.ToDictionary(static button => button.Id);
        var dockIds = await repository.GetDockButtonIdsAsync(cancellationToken);
        foreach (var id in dockIds)
        {
            if (knownById.TryGetValue(id, out var button) && !RecentButtons.Contains(button))
            {
                RecentButtons.Add(button);
            }
        }

        if (RecentButtons.Count > 0)
        {
            return;
        }

        foreach (var recent in Buttons
                     .Where(static button => button.LastExecutedAtUtc is not null)
                     .OrderByDescending(static button => button.LastExecutedAtUtc)
                     .Take(MaxDockButtons))
        {
            RecentButtons.Add(recent);
        }
    }

    private LauncherButtonModel? FindButtonById(Guid id)
        => Buttons.FirstOrDefault(button => button.Id == id);

    private async Task<bool> ApplyHistoryUndoAsync(ButtonHistoryAction action, CancellationToken cancellationToken)
    {
        try
        {
            switch (action.Kind)
            {
                case ButtonHistoryActionKind.Add when action.After is not null:
                    await RemoveRecordAsync(action.After.Id, cancellationToken);
                    Status.Set(LauncherStatusKind.Success, "Undid add.");
                    return true;
                case ButtonHistoryActionKind.Delete when action.Before is not null:
                    await RestoreRecordAsync(action.Before, cancellationToken);
                    Status.Set(LauncherStatusKind.Success, "Undid delete.");
                    return true;
                case ButtonHistoryActionKind.Update when action.Before is not null:
                    await RestoreRecordAsync(action.Before, cancellationToken);
                    Status.Set(LauncherStatusKind.Success, "Undid edit.");
                    return true;
            }
        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Undo failed: {ex.Message}");
        }

        return false;
    }

    private async Task<bool> ApplyHistoryRedoAsync(ButtonHistoryAction action, CancellationToken cancellationToken)
    {
        try
        {
            switch (action.Kind)
            {
                case ButtonHistoryActionKind.Add when action.After is not null:
                    await RestoreRecordAsync(action.After, cancellationToken);
                    Status.Set(LauncherStatusKind.Success, "Redid add.");
                    return true;
                case ButtonHistoryActionKind.Delete when action.Before is not null:
                    await RemoveRecordAsync(action.Before.Id, cancellationToken);
                    Status.Set(LauncherStatusKind.Success, "Redid delete.");
                    return true;
                case ButtonHistoryActionKind.Update when action.After is not null:
                    await RestoreRecordAsync(action.After, cancellationToken);
                    Status.Set(LauncherStatusKind.Success, "Redid edit.");
                    return true;
            }
        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Redo failed: {ex.Message}");
        }

        return false;
    }

    private async Task RestoreRecordAsync(LauncherButtonRecord record, CancellationToken cancellationToken)
    {
        var model = FindButtonById(record.Id);
        if (model is null)
        {
            Buttons.Add(LauncherButtonModelMapper.FromRecord(record));
        }
        else
        {
            CopyButtonState(LauncherButtonModelMapper.FromRecord(record), model);
        }

        await repository.UpsertButtonAsync(record, cancellationToken);
        ApplyFilter();
        RefreshCommandSuggestions();
    }

    private async Task RemoveRecordAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = FindButtonById(id);
        if (model is not null)
        {
            Buttons.Remove(model);
            filteredButtons.Remove(model);
            VisibleButtons.Remove(model);
            RecentButtons.Remove(model);
        }

        await repository.DeleteButtonAsync(id, cancellationToken);
        ApplyFilter();
        RefreshCommandSuggestions();
    }

    private async Task TryAddLaunchLogAsync(
        LauncherButtonModel button,
        LauncherExecutionResult result,
        string source,
        CancellationToken cancellationToken)
    {
        try
        {
            await repository.AddLogAsync(new LaunchLogEntry
            {
                ButtonId = button.Id,
                Source = source,
                Tool = button.CommandPath,
                Arguments = button.Arguments,
                Succeeded = result.Succeeded,
                Message = result.Message,
                TimestampUtc = DateTime.UtcNow,
            }, cancellationToken);
        }
        catch
        {
        }
    }

    private async Task TryPurgeLaunchLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await repository.PurgeOldLogsAsync(30, cancellationToken);
        }
        catch
        {
        }
    }

    private async Task TryNotifyButtonsChangedAsync(CancellationToken cancellationToken = default)
    {
        if (stateSyncNotifier is null)
        {
            return;
        }

        try
        {
            await stateSyncNotifier.NotifyButtonsChangedAsync(cancellationToken);
        }
        catch
        {
        }
    }

    private static void CopyButtonState(LauncherButtonModel source, LauncherButtonModel target)
    {
        target.Command = source.Command;
        target.Text = source.Text;
        target.CommandPath = source.CommandPath;
        target.Arguments = source.Arguments;
        target.ClipText = source.ClipText;
        target.Note = source.Note;
        target.X = source.X;
        target.Y = source.Y;
        target.Width = source.Width;
        target.Height = source.Height;
        target.UseInvertedThemeColors = source.UseInvertedThemeColors;
        target.ColorKey = source.ColorKey;
        target.ToolTip = source.ToolTip;
        target.LastExecutedAtUtc = source.LastExecutedAtUtc;
        target.SortOrder = source.SortOrder;
        target.CreatedAtUtc = source.CreatedAtUtc;
        target.UpdatedAtUtc = source.UpdatedAtUtc;
    }

    private async Task SaveButtonAsync(
        LauncherButtonModel button,
        CancellationToken cancellationToken = default)
    {
        await repository.UpsertButtonAsync(LauncherButtonModelMapper.ToRecord(button), cancellationToken);
    }
}

public enum EditorConflictType
{
    UpdatedByOtherWindow,
    DeletedByOtherWindow,
}

public sealed class EditorConflictContext
{
    public required LauncherButtonRecord EditingRecord { get; init; }

    public required LauncherButtonRecord? LatestRecord { get; init; }

    public required EditorConflictType ConflictType { get; init; }
}
