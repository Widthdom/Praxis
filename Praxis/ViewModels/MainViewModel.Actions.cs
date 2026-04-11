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
        errorLogger.LogInfo($"Editor opened for existing button: \"{item.ButtonText}\" [{item.Id}] command=\"{item.Command}\"", nameof(OpenEditor));
        Editor = ButtonEditorViewModel.FromRecord(item.Snapshot(), isExistingRecord: true);
        IsEditorOpen = true;
        IsContextMenuOpen = false;
    }

    [RelayCommand]
    private async Task DeleteButtonAsync(LauncherButtonItemViewModel? item)
    {
        if (item is null) return;
        await DeleteButtonsAsync([item], statusForSuccess: "Button deleted.");
        IsContextMenuOpen = false;
        ContextMenuTarget = null;
    }

    private async Task DeleteButtonsAsync(IReadOnlyList<LauncherButtonItemViewModel> items, string statusForSuccess)
    {
        if (items.Count == 0)
        {
            return;
        }

        var names = string.Join(", ", items.Select(x => $"\"{x.ButtonText}\" [{x.Id}]"));
        errorLogger.LogInfo($"Button(s) deleted: {names}", nameof(DeleteButtonsAsync));
        var dockOrderBefore = await repository.GetDockButtonIdsAsync();
        var snapshots = items
            .Select(x => x.Snapshot())
            .ToList();

        foreach (var item in items)
        {
            await repository.DeleteButtonAsync(item.Id);
            allButtons.Remove(item);
            filteredButtons.Remove(item);
            VisibleButtons.Remove(item);
            DockButtons.Remove(item);
        }

        UpdateCanvasSize();
        await PersistDockAsync();
        await TryNotifyButtonsChangedAsync(nameof(DeleteButtonsAsync), "Delete");
        RecordHistoryAction("delete", snapshots.Select(x => new ButtonHistoryMutation
        {
            Id = x.Id,
            Before = x.Clone(),
            After = null,
        }),
        dockOrderBefore: dockOrderBefore,
        dockOrderAfter: DockButtons.Select(x => x.Id).ToList());
        SetStatus(statusForSuccess);
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
            await DeleteButtonsAsync(selected, selected.Count == 1 ? "Button deleted." : $"{selected.Count} buttons deleted.");
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
            ? await TryGetClipboardTextAsync(nameof(OpenCreateEditorAtAsync), "Clipboard read for create")
            : string.Empty;

        OpenCreateEditor(x, y, args);
    }

    private void OpenCreateEditor(double x, double y, string arguments)
    {
        errorLogger.LogInfo($"Editor opened for new button at ({x:F0}, {y:F0})", nameof(OpenCreateEditor));
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
            UseInvertedThemeColors = false,
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

        var label = Editor.IsExistingRecord
            ? $"\"{Editor.ButtonText}\" [{Editor.Id}]"
            : $"\"{Editor.ButtonText}\" (new)";
        errorLogger.LogInfo($"Editor canceled: {label}", nameof(CancelEditor));
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
        var existing = allButtons.FirstOrDefault(x => x.Id == record.Id);
        var beforeSnapshot = existing?.Snapshot();

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
            else if (RecordVersionComparer.HasConflict(Editor.OriginalUpdatedAtUtc, latest.UpdatedAtUtc) &&
                     (beforeSnapshot is null || RecordVersionComparer.HasConflict(beforeSnapshot, latest)))
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

        if (existing is null)
        {
            errorLogger.LogInfo($"Button created: \"{record.ButtonText}\" [{record.Id}] command=\"{record.Command}\" tool={record.Tool} args=\"{record.Arguments}\"", nameof(SaveEditorAsync));
            allButtons.Add(new LauncherButtonItemViewModel(record));
        }
        else
        {
            var diff = BuildButtonDiff(beforeSnapshot, record);
            errorLogger.LogInfo($"Button updated: \"{record.ButtonText}\" [{record.Id}]{diff}", nameof(SaveEditorAsync));
            existing.Overwrite(record);
        }

        IsEditorOpen = false;
        ApplyFilter();
        UpdateCanvasSize();
        await TryNotifyButtonsChangedAsync(nameof(SaveEditorAsync), "Save");
        RecordHistoryAction(existing is null ? "create" : "edit", [new ButtonHistoryMutation
        {
            Id = record.Id,
            Before = beforeSnapshot?.Clone(),
            After = record.Clone(),
        }]);
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
            var resolution = await ResolveEditorConflictAsync.Invoke(context);
            errorLogger.LogInfo($"Conflict dialog: type={context.ConflictType}, resolution={resolution}, button=\"{context.EditingRecord.ButtonText}\" [{context.EditingRecord.Id}]", nameof(ResolveConflictAsync));
            return resolution;
        }
        catch (Exception ex)
        {
            errorLogger.LogWarning($"Conflict resolution callback failed: {ex.Message}", nameof(ResolveConflictAsync));
            return EditorConflictResolution.Cancel;
        }
    }

    [RelayCommand]
    private async Task CopyFieldAsync(string? value)
    {
        if (await TrySetClipboardTextAsync(value ?? string.Empty, nameof(CopyFieldAsync), "Clipboard copy"))
        {
            SetStatus("Copied to clipboard.");
            return;
        }

        SetStatus("Clipboard copy failed.");
    }

    [RelayCommand]
    private async Task SetThemeAsync(string? mode)
    {
        var parsed = ThemeModeParser.ParseOrDefault(mode, ThemeMode.System);

        errorLogger.LogInfo($"Theme set to {parsed}", nameof(SetThemeAsync));
        SelectedTheme = parsed;
        themeService.Apply(parsed);
        await repository.SetThemeAsync(parsed);
        await TryNotifyButtonsChangedAsync(nameof(SetThemeAsync), "Theme change");
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
                dragStart[target.Id] = target.Snapshot();
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
            var mutations = new List<ButtonHistoryMutation>();
            foreach (var target in dragTargets)
            {
                if (!dragStart.TryGetValue(target.Id, out var before))
                {
                    continue;
                }

                var after = target.ToRecord();
                await repository.UpsertButtonAsync(after);
                target.Overwrite(after);
                if (before.X != after.X || before.Y != after.Y)
                {
                    mutations.Add(new ButtonHistoryMutation
                    {
                        Id = target.Id,
                        Before = before.Clone(),
                        After = after.Clone(),
                    });
                }
            }

            await TryNotifyButtonsChangedAsync(nameof(HandleDragAsync), "Move");
            if (mutations.Count > 0)
            {
                RecordHistoryAction("move", mutations);
            }

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

    [RelayCommand]
    private async Task UndoAsync()
    {
        if (!actionHistory.TryBeginUndo(out var action))
        {
            return;
        }

        var applied = await ApplyHistoryActionAsync(action, isUndo: true);
        actionHistory.CompleteUndo(action, applied);
        errorLogger.LogInfo($"Undo: {action.Description}, applied={applied}", nameof(UndoAsync));
        SetStatus(applied
            ? $"Undid {action.Description}."
            : "Undo canceled: affected buttons changed in another window.");
    }

    [RelayCommand]
    private async Task RedoAsync()
    {
        if (!actionHistory.TryBeginRedo(out var action))
        {
            return;
        }

        var applied = await ApplyHistoryActionAsync(action, isUndo: false);
        actionHistory.CompleteRedo(action, applied);
        errorLogger.LogInfo($"Redo: {action.Description}, applied={applied}", nameof(RedoAsync));
        SetStatus(applied
            ? $"Redid {action.Description}."
            : "Redo canceled: affected buttons changed in another window.");
    }

    private void RecordHistoryAction(
        string description,
        IEnumerable<ButtonHistoryMutation> mutations,
        IReadOnlyList<Guid>? dockOrderBefore = null,
        IReadOnlyList<Guid>? dockOrderAfter = null)
    {
        var normalized = mutations
            .Select(x => new ButtonHistoryMutation
            {
                Id = x.Id,
                Before = x.Before?.Clone(),
                After = x.After?.Clone(),
            })
            .ToList();
        if (normalized.Count == 0)
        {
            return;
        }

        actionHistory.Push(new ButtonHistoryAction
        {
            Description = description,
            Mutations = normalized,
            DockOrderBefore = dockOrderBefore?.ToList(),
            DockOrderAfter = dockOrderAfter?.ToList(),
        });
    }

    private async Task<bool> ApplyHistoryActionAsync(ButtonHistoryAction action, bool isUndo)
    {
        foreach (var mutation in action.Mutations)
        {
            var expected = isUndo ? mutation.After : mutation.Before;
            var current = await repository.GetByIdAsync(mutation.Id, forceReload: true);
            if (!ButtonHistoryConsistencyPolicy.MatchesExpectedVersion(expected, current))
            {
                return false;
            }
        }

        foreach (var mutation in action.Mutations)
        {
            var target = isUndo ? mutation.Before : mutation.After;
            if (target is null)
            {
                await repository.DeleteButtonAsync(mutation.Id);
                continue;
            }

            await repository.UpsertButtonAsync(target.Clone());
        }

        var dockOrder = isUndo ? action.DockOrderBefore : action.DockOrderAfter;
        if (dockOrder is not null)
        {
            await repository.SetDockButtonIdsAsync(dockOrder);
        }

        await LoadButtonsFromRepositoryAsync(forceReload: true);
        await RestoreDockAsync();
        await TryNotifyButtonsChangedAsync(nameof(ApplyHistoryActionAsync), isUndo ? "Undo" : "Redo");

        if (!string.IsNullOrWhiteSpace(CommandInput))
        {
            RefreshCommandSuggestions(CommandInput, IsCommandSuggestionOpen);
        }

        return true;
    }

    private async Task<(bool Success, string Message)> ExecuteRecordAsync(LauncherButtonRecord record, bool fromButton, bool updateStatus = true)
    {
        var source = fromButton ? "button" : "command";
        errorLogger.LogInfo(
            $"Execution requested ({source}): \"{record.ButtonText}\" [{record.Id}] tool={record.Tool} args=\"{record.Arguments}\" clipTextPresent={!string.IsNullOrWhiteSpace(record.ClipText)}",
            nameof(ExecuteRecordAsync));
        var result = await commandExecutor.ExecuteAsync(record.Tool, record.Arguments);

        if (!string.IsNullOrWhiteSpace(record.ClipText))
        {
            if (await TrySetClipboardTextAsync(record.ClipText, nameof(ExecuteRecordAsync), $"Clipboard update for \"{record.ButtonText}\" [{record.Id}]"))
            {
                errorLogger.LogInfo($"Clipboard updated for \"{record.ButtonText}\" [{record.Id}]", nameof(ExecuteRecordAsync));
            }
        }

        errorLogger.LogInfo(
            $"Executed ({source}): \"{record.ButtonText}\" [{record.Id}] tool={record.Tool} args=\"{record.Arguments}\" succeeded={result.Success}" +
            (result.Success ? string.Empty : $" error=\"{result.Message}\""),
            nameof(ExecuteRecordAsync));

        await repository.AddLogAsync(new LaunchLogEntry
        {
            ButtonId = record.Id,
            Source = source,
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
        await TryNotifyButtonsChangedAsync(nameof(AddToDockAsync), "Dock update");
    }

    private async Task<string> TryGetClipboardTextAsync(string context, string operation)
    {
        try
        {
            return await clipboardService.GetTextAsync() ?? string.Empty;
        }
        catch (Exception ex)
        {
            errorLogger.LogWarning($"{operation} failed: {ex.Message}", context);
            return string.Empty;
        }
    }

    private async Task<bool> TrySetClipboardTextAsync(string text, string context, string operation)
    {
        try
        {
            await clipboardService.SetTextAsync(text);
            return true;
        }
        catch (Exception ex)
        {
            errorLogger.LogWarning($"{operation} failed: {ex.Message}", context);
            return false;
        }
    }

    private async Task<bool> TryNotifyButtonsChangedAsync(string context, string operation)
    {
        try
        {
            await stateSyncNotifier.NotifyButtonsChangedAsync();
            return true;
        }
        catch (Exception ex)
        {
            errorLogger.LogWarning($"{operation} completed locally, but window sync notification failed: {ex.Message}", context);
            return false;
        }
    }

    private static string BuildButtonDiff(LauncherButtonRecord? before, LauncherButtonRecord after)
    {
        if (before is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (before.ButtonText != after.ButtonText) parts.Add($"ButtonText: \"{before.ButtonText}\"→\"{after.ButtonText}\"");
        if (before.Command != after.Command) parts.Add($"Command: \"{before.Command}\"→\"{after.Command}\"");
        if (before.Tool != after.Tool) parts.Add($"Tool: {before.Tool}→{after.Tool}");
        if (before.Arguments != after.Arguments) parts.Add($"Arguments: \"{before.Arguments}\"→\"{after.Arguments}\"");
        if (before.ClipText != after.ClipText) parts.Add("ClipText changed");
        if (before.Note != after.Note) parts.Add("Note changed");
        if (before.X != after.X || before.Y != after.Y) parts.Add($"Position: ({before.X:F0},{before.Y:F0})→({after.X:F0},{after.Y:F0})");
        if (before.UseInvertedThemeColors != after.UseInvertedThemeColors) parts.Add($"UseInvertedThemeColors: {before.UseInvertedThemeColors}→{after.UseInvertedThemeColors}");
        return parts.Count == 0 ? " (no changes)" : ": " + string.Join(", ", parts);
    }

    private async Task PersistDockAsync()
        => await repository.SetDockButtonIdsAsync(DockButtons.Select(x => x.Id).ToList());

    private async Task RestoreDockAsync()
    {
        var order = await repository.GetDockButtonIdsAsync();
        DockButtons.Clear();
        if (order.Count == 0)
        {
            return;
        }

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
