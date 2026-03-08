using Praxis.ViewModels;

namespace Praxis;

public partial class MainPage
{
    // Shared lifecycle/orchestration state used across partials.
    private readonly MainViewModel viewModel;
    private ButtonEditorViewModel? observedEditorViewModel;
    private bool xamlLoaded;
    private bool initialized;
    private Window? attachedWindow;
    private bool suppressNextRootSuggestionClose;
}
