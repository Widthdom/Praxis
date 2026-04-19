using System.Text.Json;
using Microsoft.Maui.Storage;
using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Services;

public sealed class FileAppConfigService : IAppConfigService
{
    private const string ConfigFileName = "praxis.config.json";
    private readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    public ThemeMode GetThemeMode()
    {
        var candidatePaths = EnumerateCandidatePaths(AppContext.BaseDirectory, FileSystem.Current.AppDataDirectory);
        return ResolveThemeModeFromCandidates(candidatePaths, options);
    }

    private static IReadOnlyList<string> EnumerateCandidatePaths(string baseDirectory, string appDataDirectory)
    {
        var candidates = new List<string>();
        AppendCandidatePath(candidates, baseDirectory);
        AppendCandidatePath(candidates, appDataDirectory);
        return candidates;
    }

    private static ThemeMode ResolveThemeModeFromCandidates(IEnumerable<string> candidatePaths, JsonSerializerOptions options)
    {
        foreach (var path in candidatePaths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<AppConfigFile>(json, options);
                if (ThemeModeParser.TryParse(config?.Theme, out var themeMode))
                {
                    return themeMode;
                }

                var normalizedPath = NormalizePathForLog(path);
                var normalizedTheme = NormalizeThemeForLog(config?.Theme);
                CrashFileLogger.WriteWarning(
                    nameof(FileAppConfigService),
                    $"Skipping config '{normalizedPath}' because it does not specify a valid theme. Value='{normalizedTheme}'.");
            }
            catch (IOException ex)
            {
                WriteSkippedConfigWarning(path, ex);
                continue;
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteSkippedConfigWarning(path, ex);
                continue;
            }
            catch (JsonException ex)
            {
                WriteSkippedConfigWarning(path, ex);
                continue;
            }
        }

        return ThemeMode.System;
    }

    private static void WriteSkippedConfigWarning(string path, Exception ex)
    {
        var normalizedPath = NormalizePathForLog(path);
        var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
        CrashFileLogger.WriteWarning(nameof(FileAppConfigService), $"Skipping config '{normalizedPath}': {safeMessage}");
    }

    private static string NormalizePathForLog(string path)
        => CrashFileLogger.NormalizeMessagePayload(path);

    private static string NormalizeThemeForLog(string? theme)
        => CrashFileLogger.NormalizeMessagePayload(theme);

    private static void AppendCandidatePath(List<string> candidates, string? directory)
    {
        var normalizedDirectory = NormalizeAbsoluteDirectory(directory);
        if (string.IsNullOrWhiteSpace(normalizedDirectory))
        {
            return;
        }

        var path = Path.Combine(normalizedDirectory, ConfigFileName);
        if (!candidates.Contains(path, StringComparer.Ordinal))
        {
            candidates.Add(path);
        }
    }

    private static string? NormalizeAbsoluteDirectory(string? path)
    {
        var trimmed = path?.Trim().Trim('"', '\'');
        if (string.IsNullOrWhiteSpace(trimmed) || !Path.IsPathRooted(trimmed))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(trimmed);
        }
        catch
        {
            return null;
        }
    }

    private sealed class AppConfigFile
    {
        public string? Theme { get; set; }
    }
}
