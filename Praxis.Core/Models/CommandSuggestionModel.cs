using CommunityToolkit.Mvvm.ComponentModel;

namespace Praxis.Core.Models;

public partial class CommandSuggestionModel : ObservableObject
{
    public CommandSuggestionModel(LauncherButtonModel source)
    {
        Source = source;
    }

    public LauncherButtonModel Source { get; }

    [ObservableProperty]
    private bool isSelected;

    public string Command => Source.Command;

    public string ButtonText => Source.Text;

    public string ToolArguments
    {
        get
        {
            var tool = Source.CommandPath.Trim();
            var args = Source.Arguments.Trim();
            if (string.IsNullOrWhiteSpace(tool))
            {
                return args;
            }

            return string.IsNullOrWhiteSpace(args) ? tool : $"{tool} {args}";
        }
    }
}
