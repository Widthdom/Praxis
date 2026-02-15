namespace Praxis.Core.Logic;

public enum LaunchTargetKind
{
    None = 0,
    HttpUrl = 1,
    FileSystemPath = 2,
}

public static class LaunchTargetResolver
{
    public static (LaunchTargetKind Kind, string Target) Resolve(string? arguments)
    {
        var value = TrimEnclosingQuotes((arguments ?? string.Empty).Trim());
        if (string.IsNullOrWhiteSpace(value))
        {
            return (LaunchTargetKind.None, string.Empty);
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return (LaunchTargetKind.HttpUrl, value);
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var fileUri) && fileUri.IsFile)
        {
            return (LaunchTargetKind.FileSystemPath, fileUri.LocalPath);
        }

        var expanded = Environment.ExpandEnvironmentVariables(value);
        if (LooksLikePath(expanded))
        {
            return (LaunchTargetKind.FileSystemPath, expanded);
        }

        return (LaunchTargetKind.None, string.Empty);
    }

    private static bool LooksLikePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (Path.IsPathRooted(value))
        {
            return true;
        }

        if (value.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return true;
        }

        if (value.StartsWith("~/", StringComparison.Ordinal) || value.StartsWith("~\\", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static string TrimEnclosingQuotes(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1].Trim();
        }

        return value;
    }
}
