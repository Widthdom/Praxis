namespace Praxis.Core.Logic;

public static class WindowsPathPolicy
{
    public static bool IsUncPath(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.StartsWith(@"\\", StringComparison.Ordinal);
    }
}
