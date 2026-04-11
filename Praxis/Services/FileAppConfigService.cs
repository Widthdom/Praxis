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
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return ThemeMode.System;
    }

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

        return trimmed;
    }

    private sealed class AppConfigFile
    {
        public string? Theme { get; set; }
    }
}
