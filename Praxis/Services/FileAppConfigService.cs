using System.Text.Json;
using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Services;

public sealed class FileAppConfigService : IAppConfigService
{
    private const string ConfigFileName = "praxis.config.json";
    private readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    public ThemeMode GetThemeMode()
    {
        var path = ResolveConfigPath();
        if (path is null || !File.Exists(path))
        {
            return ThemeMode.System;
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<AppConfigFile>(json, options);
            return ThemeModeParser.ParseOrDefault(config?.Theme, ThemeMode.System);
        }
        catch
        {
            return ThemeMode.System;
        }
    }

    private static string? ResolveConfigPath()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        if (File.Exists(basePath))
        {
            return basePath;
        }

        var appDataPath = Path.Combine(FileSystem.Current.AppDataDirectory, ConfigFileName);
        if (File.Exists(appDataPath))
        {
            return appDataPath;
        }

        return basePath;
    }

    private sealed class AppConfigFile
    {
        public string? Theme { get; set; }
    }
}
