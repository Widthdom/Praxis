namespace Praxis.Services;

public static class AppStoragePaths
{
    public const string AppDataFolderName = "Praxis";
    public const string DatabaseFileName = "praxis.db3";
    public const string StartupLogFileName = "startup.log";
    public const string ButtonsSyncFileName = "buttons.sync";

    public static string LocalAppDataRoot =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static string AppDataFolderPath =>
        Path.Combine(LocalAppDataRoot, AppDataFolderName);

    // Keep DB location compatible with existing deployments.
    public static string DatabasePath =>
        Path.Combine(LocalAppDataRoot, DatabaseFileName);

    public static string StartupLogPath =>
        Path.Combine(AppDataFolderPath, StartupLogFileName);

    public static string ButtonsSyncPath =>
        Path.Combine(AppDataFolderPath, ButtonsSyncFileName);
}
