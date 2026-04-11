namespace Praxis.Core.Logic;

public static class WindowsPathPolicy
{
    public static bool IsUncPath(string? value)
    {
        var normalized = TrimEnclosingQuotes((value ?? string.Empty).Trim());
        if (normalized.StartsWith(@"\\?\", StringComparison.Ordinal) ||
            normalized.StartsWith(@"\\.\", StringComparison.Ordinal))
        {
            return false;
        }

        return normalized.StartsWith(@"\\", StringComparison.Ordinal);
    }

    private static string TrimEnclosingQuotes(string value)
    {
        if (value.Length >= 2)
        {
            var first = value[0];
            var last = value[^1];
            if ((first == '"' || first == '\'') && last == first)
            {
                return value[1..^1].Trim();
            }
        }

        return value;
    }
}
