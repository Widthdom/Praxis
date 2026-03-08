using CommunityToolkit.Mvvm.Input;
using Praxis.Behaviors;
using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.ViewModels;

public partial class MainViewModel
{
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

        await ExecuteCommandMatchesAsync(cmd);
    }

    [RelayCommand]
    private void ClearCommandInput()
    {
        if (string.IsNullOrEmpty(CommandInput))
        {
            return;
        }

        suppressCommandSuggestionRefresh = true;
        CommandInput = string.Empty;
        suppressCommandSuggestionRefresh = false;
        CloseCommandSuggestions();
    }

    [RelayCommand]
    private void ClearSearchText()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            return;
        }

        SearchText = string.Empty;
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
#if MACCATALYST
        if (!global::Praxis.App.IsMacApplicationActive())
        {
            return;
        }
#endif
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

        if (CommandSuggestionVisibilityPolicy.ShouldCloseOnContextMenuOpen(IsCommandSuggestionOpen))
        {
            CloseCommandSuggestions();
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

}
