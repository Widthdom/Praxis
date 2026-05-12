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
    private readonly ActionHistory<ButtonHistoryAction> history = new();
    private readonly Dictionary<Guid, LauncherButtonRecord> dragStart = [];
    private readonly List<LauncherButtonModel> dragTargets = [];

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

    private bool editorCreatesNewButton;

    public MainModel(
        ILauncherExecutionService executionService,
        ILauncherButtonRepository repository)
    {
        this.executionService = executionService;
        this.repository = repository;
    }

    public ObservableCollection<LauncherButtonModel> Buttons { get; } = [];

    public ObservableCollection<LauncherButtonModel> VisibleButtons { get; } = [];

    public ObservableCollection<LauncherButtonModel> RecentButtons { get; } = [];

    public ObservableCollection<CommandSuggestionModel> CommandSuggestions { get; } = [];

    public StatusModel Status { get; } = new();

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
            Text = $"New {ordinal}",
            X = 48 + ((ordinal - 1) % 4) * 148,
            Y = 48 + ((ordinal - 1) / 4) * 76,
            ColorKey = LauncherButtonColorKey.Default,
            Command = $"new-{ordinal}",
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
            var before = LauncherButtonModelMapper.ToRecord(button);
            await repository.DeleteButtonAsync(button.Id, cancellationToken);
            Buttons.Remove(button);
            VisibleButtons.Remove(button);
            RecentButtons.Remove(button);
            history.Push(new ButtonHistoryAction
            {
                Kind = ButtonHistoryActionKind.Delete,
                Before = before,
            });
            if (ReferenceEquals(ContextMenuTarget, button))
            {
                CloseContextMenu();
            }

            RefreshCommandSuggestions();
            Status.Set(LauncherStatusKind.Success, "Button deleted.");
        }
        catch (Exception ex)
        {
            Status.Set(LauncherStatusKind.Error, $"Delete failed: {ex.Message}");
        }
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
            Text = $"New {ordinal}",
            Command = $"new-{ordinal}",
            ToolTip = $"new-{ordinal}",
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
    }

    public async Task SaveEditorAsync(CancellationToken cancellationToken = default)
    {
        if (EditorButton is null)
        {
            return;
        }

        var draft = EditorButton;
        var existing = FindButtonById(draft.Id);
        var before = existing is null ? null : LauncherButtonModelMapper.ToRecord(existing);
        var target = existing;
        if (target is null)
        {
            target = LauncherButtonModelMapper.FromRecord(LauncherButtonModelMapper.ToRecord(draft));
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
        VisibleButtons.Clear();
        foreach (var button in Buttons.Where(ButtonMatchesSearch))
        {
            VisibleButtons.Add(button);
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
            VisibleButtons.Remove(model);
            RecentButtons.Remove(model);
        }

        await repository.DeleteButtonAsync(id, cancellationToken);
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
