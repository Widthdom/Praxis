using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.ViewModels;

public partial class MainViewModel
{
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
    private async Task PickSuggestionAsync(CommandSuggestionItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        errorLogger.LogInfo($"Command suggestion selected: \"{item.Command}\" (button: \"{item.ButtonText}\")", nameof(PickSuggestionAsync));
        suppressCommandSuggestionRefresh = true;
        CommandInput = item.Command;
        suppressCommandSuggestionRefresh = false;
        CloseCommandSuggestions();
        await ExecuteCommandMatchesAsync(item.Command);
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
        catch (Exception ex)
        {
            errorLogger.LogWarning($"Debounced command suggestion refresh failed: {ex.Message}", nameof(DebouncedRefreshCommandSuggestionsAsync));
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
        }
        catch (Exception ex)
        {
            errorLogger.LogWarning($"Command suggestion refresh failed: {ex.Message}", nameof(RefreshCommandSuggestions));
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

    private async Task ExecuteCommandMatchesAsync(string? commandText)
    {
        var cmd = commandText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cmd))
        {
            return;
        }

        var targets = CommandRecordMatcher.FindMatches(allButtons.Select(x => x.ToRecord()), cmd).ToList();
        if (targets.Count == 0)
        {
            LauncherButtonRecord? singleTarget = null;
            try
            {
                singleTarget = await repository.GetByCommandAsync(cmd);
            }
            catch (Exception ex)
            {
                errorLogger.LogWarning($"Command lookup fallback failed: {ex.Message}", nameof(ExecuteCommandMatchesAsync));
            }

            if (singleTarget is not null)
            {
                targets.Add(singleTarget);
            }
        }

        if (targets.Count == 0)
        {
            errorLogger.LogInfo($"Command not found: \"{cmd}\"", nameof(ExecuteCommandMatchesAsync));
            SetStatus($"Command not found: {cmd}");
            return;
        }

        errorLogger.LogInfo($"Command execution resolved {targets.Count} target(s) for \"{cmd}\"", nameof(ExecuteCommandMatchesAsync));

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

}
