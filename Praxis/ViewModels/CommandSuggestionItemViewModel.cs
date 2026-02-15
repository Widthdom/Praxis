namespace Praxis.ViewModels;

public sealed class CommandSuggestionItemViewModel
{
    public CommandSuggestionItemViewModel(LauncherButtonItemViewModel source)
    {
        Source = source;
    }

    public LauncherButtonItemViewModel Source { get; }
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
