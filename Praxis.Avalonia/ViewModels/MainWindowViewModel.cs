using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Praxis.Core.Models;

namespace Praxis.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(MainModel model)
    {
        Model = model;
        Model.PropertyChanged += ModelOnPropertyChanged;
        Model.Status.PropertyChanged += StatusOnPropertyChanged;
        InitializeCommand = new AsyncRelayCommand(Model.InitializeAsync);
        AddButtonCommand = new AsyncRelayCommand(Model.AddButtonAsync);
        ExecuteButtonCommand = new AsyncRelayCommand<LauncherButtonModel>(Model.ExecuteButtonAsync);
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

    public IAsyncRelayCommand InitializeCommand { get; }

    public IAsyncRelayCommand AddButtonCommand { get; }

    public IAsyncRelayCommand<LauncherButtonModel> ExecuteButtonCommand { get; }

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

    public Task InitializeAsync()
        => InitializeCommand.ExecuteAsync(null);

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
}
