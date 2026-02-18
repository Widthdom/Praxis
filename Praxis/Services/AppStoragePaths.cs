using Microsoft.Maui.Storage;
using Praxis.Core.Logic;

namespace Praxis.Services;

public static class AppStoragePaths
{
    public const string AppDataFolderName = AppStoragePathLayoutResolver.AppDataFolderName;
    public const string DatabaseFileName = AppStoragePathLayoutResolver.DatabaseFileName;
    public const string ButtonsSyncFileName = AppStoragePathLayoutResolver.ButtonsSyncFileName;

    public static string WindowsLocalAppDataRoot => ResolveWindowsLocalAppDataRoot();

    public static string AppDataDirectory =>
        FileSystem.Current.AppDataDirectory;

    public static string MacApplicationSupportRoot =>
        ResolveMacApplicationSupportRoot();

    public static string DatabasePath =>
        OperatingSystem.IsWindows()
            ? AppStoragePathLayoutResolver.ResolveDatabasePath(WindowsLocalAppDataRoot, string.Empty, isWindows: true)
            : AppStoragePathLayoutResolver.ResolveDatabasePath(WindowsLocalAppDataRoot, MacApplicationSupportRoot, isWindows: false);

    public static string AppDataFolderPath =>
        OperatingSystem.IsWindows()
            ? AppStoragePathLayoutResolver.ResolveSyncDirectory(WindowsLocalAppDataRoot, string.Empty, isWindows: true)
            : AppStoragePathLayoutResolver.ResolveSyncDirectory(WindowsLocalAppDataRoot, MacApplicationSupportRoot, isWindows: false);

    public static string ButtonsSyncPath =>
        OperatingSystem.IsWindows()
            ? AppStoragePathLayoutResolver.ResolveSyncPath(WindowsLocalAppDataRoot, string.Empty, isWindows: true)
            : AppStoragePathLayoutResolver.ResolveSyncPath(WindowsLocalAppDataRoot, MacApplicationSupportRoot, isWindows: false);

    public static void PrepareStorage()
    {
        EnsureDirectory(Path.GetDirectoryName(DatabasePath));
        EnsureDirectory(AppDataFolderPath);
        TryMigrateLegacyDatabase();
    }

    private static void TryMigrateLegacyDatabase()
    {
        if (File.Exists(DatabasePath))
        {
            return;
        }

        foreach (var sourcePath in EnumerateLegacyDatabasePaths())
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                continue;
            }

            if (PathsEqual(sourcePath, DatabasePath) || !IsSafeMigrationSource(sourcePath))
            {
                continue;
            }

            if (!File.Exists(sourcePath))
            {
                continue;
            }

            File.Copy(sourcePath, DatabasePath, overwrite: false);
            return;
        }
    }

    private static IEnumerable<string> EnumerateLegacyDatabasePaths()
    {
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFileName);
        if (!OperatingSystem.IsWindows())
        {
            yield return Path.Combine(AppDataDirectory, AppDataFolderName, DatabaseFileName);
            yield return Path.Combine(AppDataDirectory, DatabaseFileName);
        }
    }

    private static bool IsSafeMigrationSource(string sourcePath)
    {
        if (!OperatingSystem.IsMacCatalyst())
        {
            return true;
        }

        var marker = $"{Path.DirectorySeparatorChar}Documents{Path.DirectorySeparatorChar}";
        return sourcePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static bool PathsEqual(string left, string right)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), comparison);
    }

    private static void EnsureDirectory(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static string ResolveWindowsLocalAppDataRoot()
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            return localAppData;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            return Path.Combine(userProfile, "AppData", "Local");
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    private static string ResolveMacApplicationSupportRoot()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            return Path.Combine(userProfile, "Library", "Application Support");
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrWhiteSpace(appData))
        {
            return appData;
        }

        return AppDataDirectory;
    }
}
