using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Praxis.Behaviors;
using Praxis.Core.Logic;
using Praxis.Core.Models;
using Praxis.Services;

namespace Praxis.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAppRepository repository;
    private readonly ICommandExecutor commandExecutor;
    private readonly IClipboardService clipboardService;
    private readonly IThemeService themeService;
    private readonly IStateSyncNotifier stateSyncNotifier;

    private readonly ObservableCollection<LauncherButtonItemViewModel> allButtons = [];
    private readonly List<LauncherButtonItemViewModel> filteredButtons = [];
    private readonly Dictionary<Guid, (double X, double Y)> dragStart = [];
    private readonly List<LauncherButtonItemViewModel> dragTargets = [];
    private bool suppressCommandSuggestionRefresh;
    private bool pendingExternalReload;
    private int externalReloadInProgress;
    private CancellationTokenSource? commandSuggestionDebounceCts;
    private int dragCanvasUpdateCounter;
    private const int CommandSuggestionDebounceMs = 120;
    private const int DragCanvasUpdateIntervalEvents = 4;
    private const double ViewportMargin = 120;

    [ObservableProperty] private string commandInput = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string statusText = "Ready";
    [ObservableProperty] private int statusRevision;
    [ObservableProperty] private bool isCommandSuggestionOpen;
    [ObservableProperty] private double commandSuggestionPopupHeight = 44;
    [ObservableProperty] private CommandSuggestionItemViewModel? selectedCommandSuggestion;
    [ObservableProperty] private int selectedCommandSuggestionIndex = -1;
    [ObservableProperty] private double areaWidth = 820;
    [ObservableProperty] private double areaHeight = 420;
    [ObservableProperty] private double canvasWidth = 820;
    [ObservableProperty] private double canvasHeight = 420;
    [ObservableProperty] private double viewportX;
    [ObservableProperty] private double viewportY;
    [ObservableProperty] private double viewportWidth = 820;
    [ObservableProperty] private double viewportHeight = 420;
    [ObservableProperty] private bool hasHorizontalOverflow;
    [ObservableProperty] private bool hasVerticalOverflow;
    [ObservableProperty] private ScrollOrientation placementScrollOrientation = ScrollOrientation.Neither;
    [ObservableProperty] private bool isEditorOpen;
    [ObservableProperty] private ButtonEditorViewModel editor = new();
    [ObservableProperty] private ThemeMode selectedTheme = ThemeMode.System;
    [ObservableProperty] private bool isContextMenuOpen;
    [ObservableProperty] private LauncherButtonItemViewModel? contextMenuTarget;
    public Func<EditorConflictContext, Task<EditorConflictResolution>>? ResolveEditorConflictAsync { get; set; }

    public ObservableCollection<LauncherButtonItemViewModel> VisibleButtons { get; } = [];
    public ObservableCollection<LauncherButtonItemViewModel> DockButtons { get; } = [];
    public ObservableCollection<CommandSuggestionItemViewModel> CommandSuggestions { get; } = [];

    public MainViewModel(
        IAppRepository repository,
        ICommandExecutor commandExecutor,
        IClipboardService clipboardService,
        IThemeService themeService,
        IStateSyncNotifier stateSyncNotifier)
    {
        this.repository = repository;
        this.commandExecutor = commandExecutor;
        this.clipboardService = clipboardService;
        this.themeService = themeService;
        this.stateSyncNotifier = stateSyncNotifier;
        this.stateSyncNotifier.ButtonsChanged += StateSyncNotifierOnButtonsChanged;
    }

    public async Task InitializeAsync()
    {
        await repository.InitializeAsync();
        await LoadButtonsFromRepositoryAsync(forceReload: true);

        SelectedTheme = await repository.GetThemeAsync();
        themeService.Apply(SelectedTheme);
        ApplyFilter();
        UpdateCanvasSize();
        await RestoreDockAsync();
    }

    partial void OnIsEditorOpenChanged(bool value)
    {
        if (value || !pendingExternalReload)
        {
            return;
        }

        pendingExternalReload = false;
        _ = ReloadFromExternalChangeAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ClearSelectionState();
        ApplyFilter();
    }

    partial void OnCommandInputChanged(string value)
    {
        if (suppressCommandSuggestionRefresh)
        {
            return;
        }

        DebounceRefreshCommandSuggestions(value);
    }

    [RelayCommand]
    private async Task ExecuteCommandInputAsync()
    {
        var cmd = CommandInput.Trim();
        if (IsCommandSuggestionOpen && SelectedCommandSuggestion is not null)
        {
            cmd = SelectedCommandSuggestion.Command;
            suppressCommandSuggestionRefresh = true;
            CommandInput = cmd;
            suppressCommandSuggestionRefresh = false;
        }

        CloseCommandSuggestions();

        if (string.IsNullOrWhiteSpace(cmd))
        {
            return;
        }

        var targets = CommandRecordMatcher.FindMatches(allButtons.Select(x => x.ToRecord()), cmd).ToList();
        if (targets.Count == 0)
        {
            var singleTarget = await repository.GetByCommandAsync(cmd);
            if (singleTarget is not null)
            {
                targets.Add(singleTarget);
            }
        }

        if (targets.Count == 0)
        {
            SetStatus($"Command not found: {cmd}");
            return;
        }

        if (targets.Count == 1)
        {
            await ExecuteRecordAsync(targets[0], false);
            return;
        }

        var successCount = 0;
        string? lastFailureMessage = null;

        foreach (var target in targets)
        {
            var result = await ExecuteRecordAsync(target, false, updateStatus: false);
            if (result.Success)
            {
                successCount++;
            }
            else
            {
                lastFailureMessage = result.Message;
            }
        }

        if (successCount == targets.Count)
        {
            SetStatus($"Executed {targets.Count} commands.");
            return;
        }

        SetStatus($"Executed {successCount}/{targets.Count}. Last error: {lastFailureMessage}");
    }

    [RelayCommand]
    private async Task ExecuteButtonAsync(LauncherButtonItemViewModel? item)
    {
        if (item is null) return;
        await ExecuteRecordAsync(item.ToRecord(), true);
        await AddToDockAsync(item);
    }

    [RelayCommand]
    private void OpenEditor(LauncherButtonItemViewModel? item)
    {
        if (item is null) return;
        Editor = ButtonEditorViewModel.FromRecord(item.ToRecord(), isExistingRecord: true);
        IsEditorOpen = true;
        IsContextMenuOpen = false;
    }

    [RelayCommand]
    private async Task DeleteButtonAsync(LauncherButtonItemViewModel? item)
    {
        if (item is null) return;
        await repository.DeleteButtonAsync(item.Id);
        allButtons.Remove(item);
        filteredButtons.Remove(item);
        VisibleButtons.Remove(item);
        DockButtons.Remove(item);
        UpdateCanvasSize();
        await PersistDockAsync();
        await stateSyncNotifier.NotifyButtonsChangedAsync();
        SetStatus("Button deleted.");
        IsContextMenuOpen = false;
        ContextMenuTarget = null;
    }

    [RelayCommand]
    private void OpenContextMenu(LauncherButtonItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        ContextMenuTarget = item;
        IsContextMenuOpen = true;
    }

    [RelayCommand]
    private void CloseContextMenu()
    {
        IsContextMenuOpen = false;
        ContextMenuTarget = null;
    }

    [RelayCommand]
    private void ContextEdit()
    {
        if (ContextMenuTarget is null)
        {
            return;
        }

        OpenEditor(ContextMenuTarget);
    }

    [RelayCommand]
    private async Task ContextDeleteAsync()
    {
        var selected = allButtons.Where(x => x.IsSelected).ToList();
        if (selected.Count > 0)
        {
            foreach (var item in selected)
            {
                await repository.DeleteButtonAsync(item.Id);
                allButtons.Remove(item);
                filteredButtons.Remove(item);
                VisibleButtons.Remove(item);
                DockButtons.Remove(item);
            }

            UpdateCanvasSize();
            await PersistDockAsync();
            await stateSyncNotifier.NotifyButtonsChangedAsync();
            SetStatus(selected.Count == 1 ? "Button deleted." : $"{selected.Count} buttons deleted.");
            IsContextMenuOpen = false;
            ContextMenuTarget = null;
            return;
        }

        if (ContextMenuTarget is null)
        {
            return;
        }

        await DeleteButtonAsync(ContextMenuTarget);
    }

    [RelayCommand]
    private void CreateNew()
    {
        OpenCreateEditor(20, 20, string.Empty);
    }

    public async Task OpenCreateEditorAtAsync(double x, double y, bool useClipboardForArguments)
    {
        var args = useClipboardForArguments
            ? await clipboardService.GetTextAsync() ?? string.Empty
            : string.Empty;

        OpenCreateEditor(x, y, args);
    }

    private void OpenCreateEditor(double x, double y, string arguments)
    {
        var record = new LauncherButtonRecord
        {
            Id = Guid.NewGuid(),
            ButtonText = "New",
            Arguments = arguments,
            ClipText = string.Empty,
            X = Math.Max(0, GridSnapper.Snap(x)),
            Y = Math.Max(0, GridSnapper.Snap(y)),
            Width = ButtonLayoutDefaults.Width,
            Height = ButtonLayoutDefaults.Height,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        SearchText = string.Empty;
        Editor = ButtonEditorViewModel.FromRecord(record, isExistingRecord: false);
        IsEditorOpen = true;
        IsContextMenuOpen = false;
    }

    [RelayCommand]
    private void CancelEditor()
    {
        if (!IsEditorOpen)
        {
            return;
        }

        IsEditorOpen = false;
    }

    [RelayCommand]
    private async Task SaveEditorAsync()
    {
        if (!IsEditorOpen)
        {
            return;
        }

        var record = Editor.ToRecord();
        record.X = Math.Max(0, record.X);
        record.Y = Math.Max(0, record.Y);

        if (Editor.IsExistingRecord)
        {
            var latest = await repository.GetByIdAsync(record.Id, forceReload: true);
            if (latest is null)
            {
                var resolution = await ResolveConflictAsync(new EditorConflictContext
                {
                    EditingRecord = record,
                    LatestRecord = null,
                    ConflictType = EditorConflictType.DeletedByOtherWindow,
                });

                if (resolution == EditorConflictResolution.Reload)
                {
                    IsEditorOpen = false;
                    SetStatus("Record was deleted in another window.");
                    return;
                }

                if (resolution == EditorConflictResolution.Cancel)
                {
                    SetStatus("Save canceled due to conflict.");
                    return;
                }
            }
            else if (RecordVersionComparer.HasConflict(Editor.OriginalUpdatedAtUtc, latest.UpdatedAtUtc))
            {
                var resolution = await ResolveConflictAsync(new EditorConflictContext
                {
                    EditingRecord = record,
                    LatestRecord = latest,
                    ConflictType = EditorConflictType.UpdatedByOtherWindow,
                });

                if (resolution == EditorConflictResolution.Reload)
                {
                    Editor = ButtonEditorViewModel.FromRecord(latest, isExistingRecord: true);
                    SetStatus("Reloaded latest changes from another window.");
                    return;
                }

                if (resolution == EditorConflictResolution.Cancel)
                {
                    SetStatus("Save canceled due to conflict.");
                    return;
                }
            }
        }

        await repository.UpsertButtonAsync(record);

        var existing = allButtons.FirstOrDefault(x => x.Id == record.Id);
        if (existing is null)
        {
            allButtons.Add(new LauncherButtonItemViewModel(record));
        }
        else
        {
            existing.Overwrite(record);
        }

        IsEditorOpen = false;
        ApplyFilter();
        UpdateCanvasSize();
        await stateSyncNotifier.NotifyButtonsChangedAsync();
        SetStatus("Saved.");
    }

    private async Task<EditorConflictResolution> ResolveConflictAsync(EditorConflictContext context)
    {
        if (ResolveEditorConflictAsync is null)
        {
            return EditorConflictResolution.Cancel;
        }

        try
        {
            return await ResolveEditorConflictAsync.Invoke(context);
        }
        catch
        {
            return EditorConflictResolution.Cancel;
        }
    }

    [RelayCommand]
    private async Task CopyFieldAsync(string? value)
    {
        await clipboardService.SetTextAsync(value ?? string.Empty);
        SetStatus("Copied to clipboard.");
    }

    [RelayCommand]
    private async Task SetThemeAsync(string? mode)
    {
        if (!Enum.TryParse<ThemeMode>(mode, true, out var parsed))
        {
            parsed = ThemeMode.System;
        }

        SelectedTheme = parsed;
        themeService.Apply(parsed);
        await repository.SetThemeAsync(parsed);
        await stateSyncNotifier.NotifyButtonsChangedAsync();
    }

    [RelayCommand]
    private Task OnDragAsync(DragPayload? payload)
        => HandleDragAsync(payload);

    private async Task HandleDragAsync(DragPayload? payload)
    {
        if (payload?.Item is not LauncherButtonItemViewModel item)
        {
            return;
        }

        if (payload.Status == GestureStatus.Started)
        {
            dragStart.Clear();
            dragTargets.Clear();
            dragCanvasUpdateCounter = 0;

            var selected = allButtons.Where(x => x.IsSelected).ToList();
            if (selected.Count > 0 && item.IsSelected)
            {
                dragTargets.AddRange(selected);
            }
            else
            {
                dragTargets.Add(item);
            }

            foreach (var target in dragTargets)
            {
                dragStart[target.Id] = (target.X, target.Y);
            }
            return;
        }

        if (dragTargets.Count == 0 || !dragStart.ContainsKey(dragTargets[0].Id))
        {
            return;
        }

        // If any selected item would cross top/left border, stop drag update.
        foreach (var target in dragTargets)
        {
            if (!dragStart.TryGetValue(target.Id, out var origin))
            {
                continue;
            }

            var nx = GridSnapper.Snap(origin.X + payload.TotalX);
            var ny = GridSnapper.Snap(origin.Y + payload.TotalY);
            if (nx < 0 || ny < 0)
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

        dragCanvasUpdateCounter++;
        if (payload.Status != GestureStatus.Running || dragCanvasUpdateCounter % DragCanvasUpdateIntervalEvents == 0)
        {
            UpdateCanvasSize();
        }

        if (payload.Status == GestureStatus.Completed)
        {
            foreach (var target in dragTargets)
            {
                await repository.UpsertButtonAsync(target.ToRecord());
            }

            await stateSyncNotifier.NotifyButtonsChangedAsync();

            dragStart.Clear();
            dragTargets.Clear();
        }
    }

    public void ToggleSelection(LauncherButtonItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        item.IsSelected = !item.IsSelected;
    }

    public void ApplySelection(SelectionPayload? payload)
    {
        if (payload is null) return;

        if (payload.Status == GestureStatus.Started)
        {
            foreach (var b in allButtons)
            {
                b.IsSelected = false;
            }
            return;
        }

        var sx = Math.Min(payload.StartX, payload.CurrentX);
        var sy = Math.Min(payload.StartY, payload.CurrentY);
        var ex = Math.Max(payload.StartX, payload.CurrentX);
        var ey = Math.Max(payload.StartY, payload.CurrentY);

        foreach (var b in allButtons)
        {
            var inside = b.X >= sx && b.Y >= sy && b.X + b.Width <= ex && b.Y + b.Height <= ey;
            b.IsSelected = inside;
        }
    }

    [RelayCommand]
    private async Task SaveAreaSizeAsync(Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        AreaWidth = size.Width;
        AreaHeight = size.Height;
        ViewportWidth = size.Width;
        ViewportHeight = size.Height;
        RefreshVisibleButtons();
        UpdateCanvasSize();
        await Task.CompletedTask;
    }

    private async Task<(bool Success, string Message)> ExecuteRecordAsync(LauncherButtonRecord record, bool fromButton, bool updateStatus = true)
    {
        var result = await commandExecutor.ExecuteAsync(record.Tool, record.Arguments);

        if (!string.IsNullOrWhiteSpace(record.ClipText))
        {
            await clipboardService.SetTextAsync(record.ClipText);
        }

        await repository.AddLogAsync(new LaunchLogEntry
        {
            ButtonId = record.Id,
            Source = fromButton ? "button" : "command",
            Tool = record.Tool,
            Arguments = record.Arguments,
            Succeeded = result.Success,
            Message = result.Message,
            TimestampUtc = DateTime.UtcNow,
        });

        await repository.PurgeOldLogsAsync(30);
        if (updateStatus)
        {
            SetStatus(result.Success ? "Executed." : $"Failed: {result.Message}");
        }

        return result;
    }

    private async Task AddToDockAsync(LauncherButtonItemViewModel item)
    {
        var old = DockButtons.FirstOrDefault(x => x.Id == item.Id);
        if (old is not null)
        {
            DockButtons.Remove(old);
        }

        DockButtons.Insert(0, item);
        while (DockButtons.Count > 20)
        {
            DockButtons.RemoveAt(DockButtons.Count - 1);
        }

        await PersistDockAsync();
        await stateSyncNotifier.NotifyButtonsChangedAsync();
    }

    private async Task PersistDockAsync()
        => await repository.SetDockButtonIdsAsync(DockButtons.Select(x => x.Id).ToList());

    private async Task RestoreDockAsync()
    {
        var order = await repository.GetDockButtonIdsAsync();
        if (order.Count == 0)
        {
            return;
        }

        DockButtons.Clear();
        foreach (var id in order)
        {
            var item = allButtons.FirstOrDefault(x => x.Id == id);
            if (item is not null)
            {
                DockButtons.Add(item);
            }
        }
    }

    private void ApplyFilter()
    {
        var list = allButtons
            .Where(x => ButtonSearchMatcher.IsMatch(x.ToRecord(), SearchText))
            .OrderBy(x => x.Y)
            .ThenBy(x => x.X)
            .ToList();

        filteredButtons.Clear();
        filteredButtons.AddRange(list);

        RefreshVisibleButtons();
        UpdateCanvasSize();
    }

    private void ClearSelectionState()
    {
        foreach (var item in allButtons)
        {
            item.IsSelected = false;
        }

        dragStart.Clear();
        dragTargets.Clear();
    }

    private void UpdateCanvasSize()
    {
        var right = allButtons.Count == 0 ? 0 : allButtons.Max(x => x.X + x.Width);
        var bottom = allButtons.Count == 0 ? 0 : allButtons.Max(x => x.Y + x.Height);
        CanvasWidth = Math.Max(AreaWidth, right + 32);
        CanvasHeight = Math.Max(AreaHeight, bottom + 32);
        HasHorizontalOverflow = CanvasWidth > AreaWidth + 0.5;
        HasVerticalOverflow = CanvasHeight > AreaHeight + 0.5;
        PlacementScrollOrientation = (HasHorizontalOverflow, HasVerticalOverflow) switch
        {
            (true, true) => ScrollOrientation.Both,
            (true, false) => ScrollOrientation.Horizontal,
            (false, true) => ScrollOrientation.Vertical,
            _ => ScrollOrientation.Neither,
        };
    }

    [RelayCommand]
    private void MoveSuggestionUp()
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

    [RelayCommand]
    private void MoveSuggestionDown()
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

    [RelayCommand]
    private void CloseCommandSuggestions()
    {
        commandSuggestionDebounceCts?.Cancel();
        commandSuggestionDebounceCts?.Dispose();
        commandSuggestionDebounceCts = null;
        foreach (var item in CommandSuggestions)
        {
            item.IsSelected = false;
        }
        CommandSuggestionPopupHeight = 44;
        IsCommandSuggestionOpen = false;
        SelectedCommandSuggestion = null;
        SelectedCommandSuggestionIndex = -1;
    }

    [RelayCommand]
    private void PickSuggestion(CommandSuggestionItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        suppressCommandSuggestionRefresh = true;
        CommandInput = item.Command;
        suppressCommandSuggestionRefresh = false;
        CloseCommandSuggestions();
    }

    [RelayCommand]
    private void ReopenCommandSuggestions()
    {
        if (string.IsNullOrWhiteSpace(CommandInput))
        {
            CloseCommandSuggestions();
            return;
        }

        RefreshCommandSuggestionsOnMainThread(CommandInput);
    }

    private void SetStatus(string message)
    {
        StatusText = message;
        StatusRevision++;
    }

    public void UpdateViewport(double scrollX, double scrollY, double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        ViewportX = Math.Max(0, scrollX);
        ViewportY = Math.Max(0, scrollY);
        ViewportWidth = width;
        ViewportHeight = height;
        RefreshVisibleButtons();
    }

    private void RefreshVisibleButtons()
    {
        IEnumerable<LauncherButtonItemViewModel> source = filteredButtons;
        if (ViewportWidth > 0 && ViewportHeight > 0)
        {
            var left = Math.Max(0, ViewportX - ViewportMargin);
            var top = Math.Max(0, ViewportY - ViewportMargin);
            var right = ViewportX + ViewportWidth + ViewportMargin;
            var bottom = ViewportY + ViewportHeight + ViewportMargin;

            source = source.Where(x =>
                x.X < right &&
                x.X + x.Width > left &&
                x.Y < bottom &&
                x.Y + x.Height > top);
        }

        var target = source.ToList();
        ApplyVisibleButtonsDiff(target);
    }

    private void ApplyVisibleButtonsDiff(IReadOnlyList<LauncherButtonItemViewModel> target)
    {
        var targetSet = new HashSet<LauncherButtonItemViewModel>(target);
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

    private void DebounceRefreshCommandSuggestions(string value)
    {
        commandSuggestionDebounceCts?.Cancel();
        commandSuggestionDebounceCts?.Dispose();
        commandSuggestionDebounceCts = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            RefreshCommandSuggestionsOnMainThread(value);
            return;
        }

        var cts = new CancellationTokenSource();
        commandSuggestionDebounceCts = cts;
        _ = DebouncedRefreshCommandSuggestionsAsync(cts.Token);
    }

    private async Task DebouncedRefreshCommandSuggestionsAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(CommandSuggestionDebounceMs, token);
            if (token.IsCancellationRequested || commandSuggestionDebounceCts?.Token != token)
            {
                return;
            }

            RefreshCommandSuggestionsOnMainThread(CommandInput);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(CloseCommandSuggestions);
        }
    }

    private void RefreshCommandSuggestionsOnMainThread(string value)
    {
        if (MainThread.IsMainThread)
        {
            RefreshCommandSuggestions(value);
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => RefreshCommandSuggestions(value));
    }

    private void RefreshCommandSuggestions(string value)
    {
        try
        {
            var query = value?.Trim() ?? string.Empty;
            CommandSuggestions.Clear();
            CommandSuggestionPopupHeight = 44;
            IsCommandSuggestionOpen = false;
            SelectedCommandSuggestion = null;
            SelectedCommandSuggestionIndex = -1;

            if (string.IsNullOrWhiteSpace(query))
            {
                CloseCommandSuggestions();
                return;
            }

            var matches = allButtons
                .Where(x => !string.IsNullOrWhiteSpace(x.Command) && x.Command.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Command.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => x.Command)
                .ThenBy(x => x.ButtonText)
                .Take(30)
                .Select(x => new CommandSuggestionItemViewModel(x));

            foreach (var item in matches)
            {
                CommandSuggestions.Add(item);
            }

            if (CommandSuggestions.Count == 0)
            {
                CloseCommandSuggestions();
                return;
            }

            CommandSuggestionPopupHeight = Math.Min(280, Math.Max(44, 12 + CommandSuggestions.Count * 38));
            IsCommandSuggestionOpen = true;
            SetSelectedSuggestionIndex(0);
        }
        catch
        {
            CloseCommandSuggestions();
        }
    }

    private void SetSelectedSuggestionIndex(int index)
    {
        if (index < 0 || index >= CommandSuggestions.Count)
        {
            foreach (var item in CommandSuggestions)
            {
                item.IsSelected = false;
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

    private async Task LoadButtonsFromRepositoryAsync(bool forceReload = false)
    {
        var loaded = forceReload
            ? await repository.ReloadButtonsAsync()
            : await repository.GetButtonsAsync();
        var loadedById = loaded.ToDictionary(x => x.Id, x => x);

        for (var i = allButtons.Count - 1; i >= 0; i--)
        {
            var existing = allButtons[i];
            if (!loadedById.ContainsKey(existing.Id))
            {
                allButtons.RemoveAt(i);
            }
        }

        foreach (var record in loaded)
        {
            var existing = allButtons.FirstOrDefault(x => x.Id == record.Id);
            if (existing is null)
            {
                allButtons.Add(new LauncherButtonItemViewModel(record));
            }
            else
            {
                existing.Overwrite(record);
            }
        }

        ApplyFilter();
        UpdateCanvasSize();
    }

    private void StateSyncNotifierOnButtonsChanged(object? sender, StateSyncChangedEventArgs e)
    {
        if (IsEditorOpen)
        {
            pendingExternalReload = true;
            _ = SyncThemeFromExternalChangeAsync();
            return;
        }

        _ = ReloadFromExternalChangeAsync();
    }

    private async Task SyncThemeFromExternalChangeAsync()
    {
        try
        {
            var latestTheme = await repository.GetThemeAsync();
            if (latestTheme == SelectedTheme)
            {
                return;
            }

            void apply()
            {
                SelectedTheme = latestTheme;
                themeService.Apply(latestTheme);
            }

            if (MainThread.IsMainThread)
            {
                apply();
                return;
            }

            MainThread.BeginInvokeOnMainThread(apply);
        }
        catch
        {
        }
    }

    private async Task ReloadFromExternalChangeAsync()
    {
        if (Interlocked.Exchange(ref externalReloadInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            if (MainThread.IsMainThread)
            {
                await ReloadOnMainThreadAsync();
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await ReloadOnMainThreadAsync();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            await tcs.Task;
        }
        finally
        {
            Interlocked.Exchange(ref externalReloadInProgress, 0);
        }
    }

    private async Task ReloadOnMainThreadAsync()
    {
        await LoadButtonsFromRepositoryAsync(forceReload: true);
        await RestoreDockAsync();
        var latestTheme = await repository.GetThemeAsync();
        if (latestTheme != SelectedTheme)
        {
            SelectedTheme = latestTheme;
            themeService.Apply(latestTheme);
        }

        if (!string.IsNullOrWhiteSpace(CommandInput))
        {
            RefreshCommandSuggestions(CommandInput, IsCommandSuggestionOpen);
        }

        SetStatus("Synced from another window.");
    }

    private void RefreshCommandSuggestions(string value, bool keepOpenState)
    {
        var wasOpen = IsCommandSuggestionOpen;
        RefreshCommandSuggestions(value);
        if (keepOpenState || wasOpen)
        {
            return;
        }

        IsCommandSuggestionOpen = false;
    }
}

public enum EditorConflictType
{
    UpdatedByOtherWindow = 0,
    DeletedByOtherWindow = 1,
}

public enum EditorConflictResolution
{
    Reload = 0,
    Overwrite = 1,
    Cancel = 2,
}

public sealed class EditorConflictContext
{
    public required LauncherButtonRecord EditingRecord { get; init; }
    public required LauncherButtonRecord? LatestRecord { get; init; }
    public required EditorConflictType ConflictType { get; init; }
}
