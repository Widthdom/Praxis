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
            return Path.Combine(windowsLocalAppDataRoot, AppDataFolderName);
        }

        return Path.Combine(nonWindowsBasePath, AppDataFolderName);
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
            return Path.Combine(windowsLocalAppDataRoot, AppDataFolderName);
        }

        return Path.Combine(nonWindowsBasePath, AppDataFolderName);
    }
}
