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
        var value = NormalizeExpandedCandidate(Environment.ExpandEnvironmentVariables((arguments ?? string.Empty).Trim()));
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

        if (LooksLikePath(value))
        {
            return (LaunchTargetKind.FileSystemPath, value);
        }

        return (LaunchTargetKind.None, string.Empty);
    }

    private static bool LooksLikePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (string.Equals(value, "~", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(value, ".", StringComparison.Ordinal) ||
            string.Equals(value, "..", StringComparison.Ordinal))
        {
            return true;
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

        if (value.StartsWith("./", StringComparison.Ordinal) ||
            value.StartsWith("../", StringComparison.Ordinal) ||
            value.StartsWith(".\\", StringComparison.Ordinal) ||
            value.StartsWith("..\\", StringComparison.Ordinal))
        {
            return true;
        }

        if (!value.Contains("://", StringComparison.Ordinal) &&
            (value.Contains('/') || value.Contains('\\')))
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

    private static string NormalizeExpandedCandidate(string value)
    {
        var trimmed = value.Trim();
        var normalized = TrimEnclosingQuotes(trimmed);
        if (!string.Equals(normalized, trimmed, StringComparison.Ordinal))
        {
            return normalized;
        }

        if (TryNormalizeQuotedPathPrefix(trimmed, out var normalizedQuotedPathPrefix))
        {
            return normalizedQuotedPathPrefix;
        }

        return trimmed;
    }

    private static bool TryNormalizeQuotedPathPrefix(string value, out string normalized)
    {
        normalized = string.Empty;
        if (value.Length < 3 || (value[0] != '"' && value[0] != '\''))
        {
            return false;
        }

        var closingQuoteIndex = value.IndexOf(value[0], 1);
        if (closingQuoteIndex <= 0 || closingQuoteIndex >= value.Length - 1)
        {
            return false;
        }

        var separator = value[closingQuoteIndex + 1];
        if (separator != '/' && separator != '\\')
        {
            return false;
        }

        var quotedPrefix = value[1..closingQuoteIndex].Trim();
        if (!LooksLikeExpandablePathPrefix(quotedPrefix))
        {
            return false;
        }

        normalized = quotedPrefix + value[(closingQuoteIndex + 1)..];
        return true;
    }

    private static bool LooksLikeExpandablePathPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Path.IsPathRooted(value) ||
               string.Equals(value, "~", StringComparison.Ordinal) ||
               value.StartsWith("~/", StringComparison.Ordinal) ||
               value.StartsWith("~\\", StringComparison.Ordinal);
    }
}
