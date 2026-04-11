namespace Praxis.Core.Logic;

public static class CommandWorkingDirectoryPolicy
{
    public static bool RequiresUserProfileWorkingDirectory(string? tool, bool isWindows)
    {
        if (!isWindows)
        {
            return false;
        }

        var normalized = NormalizeToolFileName(tool);
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        return normalized is "cmd" or "cmd.exe" or
               "powershell" or "powershell.exe" or
               "pwsh" or "pwsh.exe" or
               "wt" or "wt.exe";
    }

    private static string NormalizeToolFileName(string? tool)
    {
        var trimmed = (tool ?? string.Empty).Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        var lastSeparator = Math.Max(trimmed.LastIndexOf('/'), trimmed.LastIndexOf('\\'));
        return lastSeparator >= 0 ? trimmed[(lastSeparator + 1)..] : trimmed;
    }
}
