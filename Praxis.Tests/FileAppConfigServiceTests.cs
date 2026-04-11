using System.Reflection;
using System.Text.Json;
using Praxis.Core.Models;
using Praxis.Services;

namespace Praxis.Tests;

public class FileAppConfigServiceTests
{
    [Fact]
    public void ResolveThemeModeFromCandidates_FallsBackToLaterValidConfig_WhenEarlierJsonIsMalformed()
    {
        var root = Path.Combine(Path.GetTempPath(), $"praxis-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var basePath = Path.Combine(root, "base");
            var appDataPath = Path.Combine(root, "appdata");
            Directory.CreateDirectory(basePath);
            Directory.CreateDirectory(appDataPath);

            File.WriteAllText(Path.Combine(basePath, "praxis.config.json"), "{ invalid");
            File.WriteAllText(Path.Combine(appDataPath, "praxis.config.json"), "{\"theme\":\"Dark\"}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = InvokeResolveThemeModeFromCandidates(
                [Path.Combine(basePath, "praxis.config.json"), Path.Combine(appDataPath, "praxis.config.json")],
                options);

            Assert.Equal(ThemeMode.Dark, result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveThemeModeFromCandidates_FallsBackToLaterValidConfig_WhenEarlierThemeIsMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), $"praxis-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var basePath = Path.Combine(root, "base");
            var appDataPath = Path.Combine(root, "appdata");
            Directory.CreateDirectory(basePath);
            Directory.CreateDirectory(appDataPath);

            File.WriteAllText(Path.Combine(basePath, "praxis.config.json"), "{}");
            File.WriteAllText(Path.Combine(appDataPath, "praxis.config.json"), "{\"theme\":\"Dark\"}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = InvokeResolveThemeModeFromCandidates(
                [Path.Combine(basePath, "praxis.config.json"), Path.Combine(appDataPath, "praxis.config.json")],
                options);

            Assert.Equal(ThemeMode.Dark, result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveThemeModeFromCandidates_FallsBackToLaterValidConfig_WhenEarlierThemeIsInvalid()
    {
        var root = Path.Combine(Path.GetTempPath(), $"praxis-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var basePath = Path.Combine(root, "base");
            var appDataPath = Path.Combine(root, "appdata");
            Directory.CreateDirectory(basePath);
            Directory.CreateDirectory(appDataPath);

            File.WriteAllText(Path.Combine(basePath, "praxis.config.json"), "{\"theme\":\"Bogus\"}");
            File.WriteAllText(Path.Combine(appDataPath, "praxis.config.json"), "{\"theme\":\"Dark\"}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = InvokeResolveThemeModeFromCandidates(
                [Path.Combine(basePath, "praxis.config.json"), Path.Combine(appDataPath, "praxis.config.json")],
                options);

            Assert.Equal(ThemeMode.Dark, result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveThemeModeFromCandidates_WarningLogsSkippedInvalidCandidatePath()
    {
        var root = Path.Combine(Path.GetTempPath(), $"praxis-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var invalidPath = Path.Combine(root, "invalid", "praxis.config.json");
            var validPath = Path.Combine(root, "valid", "praxis.config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(invalidPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(validPath)!);

            File.WriteAllText(invalidPath, "{\"theme\":\"Bogus\"}");
            File.WriteAllText(validPath, "{\"theme\":\"Dark\"}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = InvokeResolveThemeModeFromCandidates([invalidPath, validPath], options);

            Assert.Equal(ThemeMode.Dark, result);

            var content = File.ReadAllText(CrashFileLogger.LogFilePath);
            Assert.Contains($"Skipping config '{invalidPath}' because it does not specify a valid theme.", content);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EnumerateCandidatePaths_DeduplicatesEquivalentDirectories()
    {
        var path = "/tmp/praxis-config";

        var result = InvokeEnumerateCandidatePaths(path, $"  \"{path}\"  ");

        Assert.Single(result);
        Assert.Equal("/tmp/praxis-config/praxis.config.json", result[0]);
    }

    private static ThemeMode InvokeResolveThemeModeFromCandidates(IEnumerable<string> candidatePaths, JsonSerializerOptions options)
    {
        var method = typeof(FileAppConfigService).GetMethod("ResolveThemeModeFromCandidates", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [candidatePaths, options]);
        return Assert.IsType<ThemeMode>(result);
    }

    private static IReadOnlyList<string> InvokeEnumerateCandidatePaths(string baseDirectory, string appDataDirectory)
    {
        var method = typeof(FileAppConfigService).GetMethod("EnumerateCandidatePaths", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [baseDirectory, appDataDirectory]);
        return Assert.IsAssignableFrom<IReadOnlyList<string>>(result);
    }
}
