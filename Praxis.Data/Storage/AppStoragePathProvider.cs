using Praxis.Core.Logic;

namespace Praxis.Data.Storage;

public sealed class AppStoragePathProvider
{
    public const string AppDataDirectoryEnvironmentVariable = "PRAXIS_APP_DATA_DIR";
    public const string LegacyDatabaseFileName = "praxis.db";

    private readonly string? appDataDirectoryOverride;

    public AppStoragePathProvider()
    {
    }

    public AppStoragePathProvider(string appDataDirectory)
    {
        if (string.IsNullOrWhiteSpace(appDataDirectory))
        {
            throw new ArgumentException("App data directory cannot be blank.", nameof(appDataDirectory));
        }

        appDataDirectoryOverride = appDataDirectory;
    }

    public string AppDataDirectory => appDataDirectoryOverride ?? ResolveAppDataDirectory();

    public string DatabasePath
    {
        get
        {
            var defaultPath = Path.Combine(AppDataDirectory, AppStoragePathLayoutResolver.DatabaseFileName);
            if (File.Exists(defaultPath))
            {
                return defaultPath;
            }

            var legacyPath = Path.Combine(AppDataDirectory, LegacyDatabaseFileName);
            return File.Exists(legacyPath) ? legacyPath : defaultPath;
        }
    }

    public void PrepareStorage()
    {
        Directory.CreateDirectory(AppDataDirectory);
    }

    private static string ResolveAppDataDirectory()
    {
        var configuredAppDataDirectory = NormalizeAbsoluteDirectory(
            Environment.GetEnvironmentVariable(AppDataDirectoryEnvironmentVariable));
        if (!string.IsNullOrWhiteSpace(configuredAppDataDirectory))
        {
            return configuredAppDataDirectory;
        }

        if (OperatingSystem.IsWindows())
        {
            var localAppData = NormalizeAbsoluteDirectory(Environment.GetEnvironmentVariable("LOCALAPPDATA"));
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                return AppStoragePathLayoutResolver.ResolveSyncDirectory(localAppData, string.Empty, isWindows: true);
            }

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfile))
            {
                return AppStoragePathLayoutResolver.ResolveSyncDirectory(
                    Path.Combine(userProfile, "AppData", "Local"),
                    string.Empty,
                    isWindows: true);
            }
        }

        if (OperatingSystem.IsMacOS())
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appSupport = string.IsNullOrWhiteSpace(userProfile)
                ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                : Path.Combine(userProfile, "Library", "Application Support");
            return AppStoragePathLayoutResolver.ResolveSyncDirectory(string.Empty, appSupport, isWindows: false);
        }

        var xdgDataHome = NormalizeAbsoluteDirectory(Environment.GetEnvironmentVariable("XDG_DATA_HOME"));
        if (!string.IsNullOrWhiteSpace(xdgDataHome))
        {
            return AppStoragePathLayoutResolver.ResolveSyncDirectory(string.Empty, xdgDataHome, isWindows: false);
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dataHome = string.IsNullOrWhiteSpace(home)
            ? Environment.CurrentDirectory
            : Path.Combine(home, ".local", "share");
        return AppStoragePathLayoutResolver.ResolveSyncDirectory(string.Empty, dataHome, isWindows: false);
    }

    private static string? NormalizeAbsoluteDirectory(string? path)
    {
        var trimmed = path?.Trim().Trim('"', '\'');
        return string.IsNullOrWhiteSpace(trimmed) || !Path.IsPathRooted(trimmed) ? null : trimmed;
    }
}
