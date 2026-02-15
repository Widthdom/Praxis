namespace Praxis.Core.Logic;

public static class CommandLineBuilder
{
    public static string Build(string tool, string arguments)
    {
        var normalizedTool = tool?.Trim() ?? string.Empty;
        var normalizedArgs = arguments?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedTool))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(normalizedArgs)
            ? normalizedTool
            : $"{normalizedTool} {normalizedArgs}";
    }
}
