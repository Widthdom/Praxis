namespace Praxis.Core.Logic;

public static class CommandLineBuilder
{
    public static string Build(string tool, string arguments)
    {
        var normalizedTool = NormalizeTool(tool);
        var normalizedArgs = arguments?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedTool))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(normalizedArgs)
            ? normalizedTool
            : $"{normalizedTool} {normalizedArgs}";
    }

    private static string NormalizeTool(string? tool)
    {
        var trimmed = tool?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        var stripped = trimmed.Trim('"', '\'').Trim();
        return string.IsNullOrWhiteSpace(stripped)
            ? string.Empty
            : trimmed;
    }
}
