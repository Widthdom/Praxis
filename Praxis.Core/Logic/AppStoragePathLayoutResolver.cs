namespace Praxis.Core.Logic;

public static class AppStoragePathLayoutResolver
{
    public const string AppDataFolderName = "Praxis";
    public const string DatabaseFileName = "praxis.db3";
    public const string ButtonsSyncFileName = "buttons.sync";

    public static string ResolveDatabasePath(string windowsLocalAppDataRoot, string nonWindowsBasePath, bool isWindows)
    {
        var basePath = ResolveStorageBasePath(windowsLocalAppDataRoot, nonWindowsBasePath, isWindows);
        return Path.Combine(basePath, DatabaseFileName);
    }

    public static string ResolveSyncDirectory(string windowsLocalAppDataRoot, string nonWindowsBasePath, bool isWindows)
    {
        if (isWindows)
        {
            return Path.Combine(NormalizeRootPath(windowsLocalAppDataRoot), AppDataFolderName);
        }

        return Path.Combine(NormalizeRootPath(nonWindowsBasePath), AppDataFolderName);
    }

    public static string ResolveSyncPath(string windowsLocalAppDataRoot, string nonWindowsBasePath, bool isWindows)
    {
        var directoryPath = ResolveSyncDirectory(windowsLocalAppDataRoot, nonWindowsBasePath, isWindows);
        return Path.Combine(directoryPath, ButtonsSyncFileName);
    }

    private static string ResolveStorageBasePath(string windowsLocalAppDataRoot, string nonWindowsBasePath, bool isWindows)
    {
        if (isWindows)
        {
            return Path.Combine(NormalizeRootPath(windowsLocalAppDataRoot), AppDataFolderName);
        }

        return Path.Combine(NormalizeRootPath(nonWindowsBasePath), AppDataFolderName);
    }

    private static string NormalizeRootPath(string? rootPath)
    {
        var trimmed = (rootPath ?? string.Empty).Trim();
        if (trimmed.Length >= 2)
        {
            var first = trimmed[0];
            var last = trimmed[^1];
            if ((first == '"' || first == '\'') && last == first)
            {
                trimmed = trimmed[1..^1].Trim();
            }
        }

        return trimmed;
    }
}
