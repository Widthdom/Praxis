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
    private readonly ActionHistory<ButtonHistoryAction> actionHistory = new(capacity: 120);

    private readonly ObservableCollection<LauncherButtonItemViewModel> allButtons = [];
    private readonly List<LauncherButtonItemViewModel> filteredButtons = [];
    private readonly Dictionary<Guid, LauncherButtonRecord> dragStart = [];
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
    [ObservableProperty] private bool isRatingPromptVisible;
    public Func<EditorConflictContext, Task<EditorConflictResolution>>? ResolveEditorConflictAsync { get; set; }

    public ObservableCollection<LauncherButtonItemViewModel> VisibleButtons { get; } = [];
    public ObservableCollection<LauncherButtonItemViewModel> DockButtons { get; } = [];
    public ObservableCollection<CommandSuggestionItemViewModel> CommandSuggestions { get; } = [];
    public bool IsCommandInputClearVisible => InputClearButtonVisibilityPolicy.ShouldShow(CommandInput);
    public bool IsSearchTextClearVisible => InputClearButtonVisibilityPolicy.ShouldShow(SearchText);

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
        OnPropertyChanged(nameof(IsSearchTextClearVisible));
        ClearSelectionState();
        ApplyFilter();
    }

    partial void OnCommandInputChanged(string value)
    {
        OnPropertyChanged(nameof(IsCommandInputClearVisible));
        if (suppressCommandSuggestionRefresh)
        {
            return;
        }

        DebounceRefreshCommandSuggestions(value);
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
