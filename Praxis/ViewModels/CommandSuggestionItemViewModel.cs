using CommunityToolkit.Mvvm.ComponentModel;

namespace Praxis.ViewModels;

public partial class CommandSuggestionItemViewModel : ObservableObject
{
    public CommandSuggestionItemViewModel(LauncherButtonItemViewModel source)
    {
        Source = source;
    }

    public LauncherButtonItemViewModel Source { get; }
    [ObservableProperty] private bool isSelected;
    public string Command => Source.Command;
    public string ButtonText => Source.ButtonText;
    public string ToolArguments
    {
        get
        {
            var tool = Source.Tool?.Trim() ?? string.Empty;
            var args = Source.Arguments?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tool))
            {
                return args;
            }

            return string.IsNullOrWhiteSpace(args) ? tool : $"{tool} {args}";
        }
    }
}
