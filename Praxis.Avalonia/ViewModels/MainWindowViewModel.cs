using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Praxis.Core.Models;
using Praxis.Core.Services;

namespace Praxis.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IStateSyncNotifier? stateSyncNotifier;

    public MainWindowViewModel(MainModel model, IStateSyncNotifier? stateSyncNotifier = null)
    {
        Model = model;
        this.stateSyncNotifier = stateSyncNotifier;
        if (this.stateSyncNotifier is not null)
        {
            this.stateSyncNotifier.ButtonsChanged += StateSyncNotifierOnButtonsChanged;
        }

        Model.PropertyChanged += ModelOnPropertyChanged;
        Model.Status.PropertyChanged += StatusOnPropertyChanged;
        InitializeCommand = new AsyncRelayCommand(Model.InitializeAsync);
        AddButtonCommand = new AsyncRelayCommand(Model.AddButtonAsync);
        ExecuteButtonCommand = new AsyncRelayCommand<LauncherButtonModel>(Model.ExecuteButtonAsync);
        ExecuteDockButtonCommand = new AsyncRelayCommand<LauncherButtonModel>(Model.ExecuteDockButtonAsync);
        ExecuteCommandInputCommand = new AsyncRelayCommand(Model.ExecuteCommandInputAsync);
        PickSuggestionCommand = new AsyncRelayCommand<CommandSuggestionModel>(Model.PickSuggestionAsync);
        DeleteButtonCommand = new AsyncRelayCommand<LauncherButtonModel>(Model.DeleteButtonAsync);
        DragButtonCommand = new AsyncRelayCommand<ButtonDragPayload>(Model.HandleButtonDragAsync);
        MoveSuggestionUpCommand = new RelayCommand(Model.MoveSuggestionUp);
        MoveSuggestionDownCommand = new RelayCommand(Model.MoveSuggestionDown);
        MoveSuggestionDownFromInputCommand = new RelayCommand(Model.MoveSuggestionDownFromInput);
        SelectSuggestionCommand = new RelayCommand<CommandSuggestionModel>(Model.SelectSuggestion);
        CloseCommandSuggestionsCommand = new RelayCommand(Model.CloseCommandSuggestions);
        ClearCommandTextCommand = new RelayCommand(Model.ClearCommandText);
        ClearSearchTextCommand = new RelayCommand(Model.ClearSearchText);
        ToggleSelectionCommand = new RelayCommand<LauncherButtonModel>(Model.ToggleSelection);
        ApplySelectionCommand = new RelayCommand<SelectionPayload>(Model.ApplySelection);
        OpenContextMenuCommand = new RelayCommand<LauncherButtonModel>(Model.OpenContextMenu);
        CloseContextMenuCommand = new RelayCommand(Model.CloseContextMenu);
        OpenEditorCommand = new RelayCommand<LauncherButtonModel>(Model.OpenEditor);
        OpenNewButtonEditorCommand = new RelayCommand<NewButtonPayload>(Model.OpenNewButtonEditor);
        CancelEditorCommand = new RelayCommand(Model.CancelEditor);
        SaveEditorCommand = new AsyncRelayCommand(Model.SaveEditorAsync);
        UndoCommand = new AsyncRelayCommand(Model.UndoAsync);
        RedoCommand = new AsyncRelayCommand(Model.RedoAsync);
        ApplyThemeCommand = new RelayCommand<ThemeMode>(Model.ApplyTheme);
        ReloadConflictCommand = new RelayCommand(Model.ReloadConflict);
        OverwriteConflictCommand = new RelayCommand(Model.OverwriteConflict);
        CancelConflictCommand = new RelayCommand(Model.CancelConflict);
    }

    internal MainModel Model { get; }

    public string CommandText
    {
        get => Model.CommandText;
        set => Model.CommandText = value;
    }

    public string SearchText
    {
        get => Model.SearchText;
        set => Model.SearchText = value;
    }

    public bool IsCommandSuggestionOpen => Model.IsCommandSuggestionOpen;

    public ObservableCollection<CommandSuggestionModel> CommandSuggestions => Model.CommandSuggestions;

    public ObservableCollection<LauncherButtonModel> VisibleButtons => Model.VisibleButtons;

    public ObservableCollection<LauncherButtonModel> RecentButtons => Model.RecentButtons;

    public StatusModel Status => Model.Status;

    public ThemeMode SelectedTheme => Model.SelectedTheme;

    public bool IsContextMenuOpen => Model.IsContextMenuOpen;

    public LauncherButtonModel? ContextMenuTarget => Model.ContextMenuTarget;

    public bool IsEditorOpen => Model.IsEditorOpen;

    public LauncherButtonModel? EditorButton => Model.EditorButton;

    public bool IsEditorCreatingNewButton => Model.IsEditorCreatingNewButton;

    public bool IsConflictDialogOpen => Model.IsConflictDialogOpen;

    public string ConflictTitle => Model.ConflictTitle;

    public string ConflictMessage => Model.ConflictMessage;

    public double PlacementSurfaceWidth => Model.PlacementSurfaceWidth;

    public double PlacementSurfaceHeight => Model.PlacementSurfaceHeight;

    public IAsyncRelayCommand InitializeCommand { get; }

    public IAsyncRelayCommand AddButtonCommand { get; }

    public IAsyncRelayCommand<LauncherButtonModel> ExecuteButtonCommand { get; }

    public IAsyncRelayCommand<LauncherButtonModel> ExecuteDockButtonCommand { get; }

    public IAsyncRelayCommand ExecuteCommandInputCommand { get; }

    public IAsyncRelayCommand<CommandSuggestionModel> PickSuggestionCommand { get; }

    public IAsyncRelayCommand<LauncherButtonModel> DeleteButtonCommand { get; }

    public IAsyncRelayCommand<ButtonDragPayload> DragButtonCommand { get; }

    public IRelayCommand MoveSuggestionUpCommand { get; }

    public IRelayCommand MoveSuggestionDownCommand { get; }

    public IRelayCommand MoveSuggestionDownFromInputCommand { get; }

    public IRelayCommand<CommandSuggestionModel> SelectSuggestionCommand { get; }

    public IRelayCommand CloseCommandSuggestionsCommand { get; }

    public IRelayCommand ClearCommandTextCommand { get; }

    public IRelayCommand ClearSearchTextCommand { get; }

    public IRelayCommand<LauncherButtonModel> ToggleSelectionCommand { get; }

    public IRelayCommand<SelectionPayload> ApplySelectionCommand { get; }

    public IRelayCommand<LauncherButtonModel> OpenContextMenuCommand { get; }

    public IRelayCommand CloseContextMenuCommand { get; }

    public IRelayCommand<LauncherButtonModel> OpenEditorCommand { get; }

    public IRelayCommand<NewButtonPayload> OpenNewButtonEditorCommand { get; }

    public IRelayCommand CancelEditorCommand { get; }

    public IAsyncRelayCommand SaveEditorCommand { get; }

    public IAsyncRelayCommand UndoCommand { get; }

    public IAsyncRelayCommand RedoCommand { get; }

    public IRelayCommand<ThemeMode> ApplyThemeCommand { get; }

    public IRelayCommand ReloadConflictCommand { get; }

    public IRelayCommand OverwriteConflictCommand { get; }

    public IRelayCommand CancelConflictCommand { get; }

    public Task InitializeAsync()
        => InitializeCommand.ExecuteAsync(null);

    public void UpdatePlacementViewport(double scrollX, double scrollY, double width, double height)
        => Model.UpdateViewport(scrollX, scrollY, width, height);

    public void RefreshThemeBindings()
    {
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(VisibleButtons));
        OnPropertyChanged(nameof(RecentButtons));
        OnPropertyChanged(nameof(CommandSuggestions));
        OnPropertyChanged(nameof(Status));
    }

    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            OnPropertyChanged(string.Empty);
            return;
        }

        OnPropertyChanged(e.PropertyName);
    }

    private void StatusOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Status));
    }

    private void StateSyncNotifierOnButtonsChanged(object? sender, StateSyncChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(async () => await Model.ReloadFromExternalChangeAsync());
    }
}
