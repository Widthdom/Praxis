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
            Assert.Contains($"Skipping config '{invalidPath}' because it does not specify a valid theme. Value='Bogus'.", content);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveThemeModeFromCandidates_NormalizesInvalidThemeValue_InWarningBreadcrumb()
    {
        var root = Path.Combine(Path.GetTempPath(), $"praxis-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var invalidPath = Path.Combine(root, "invalid", "praxis.config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(invalidPath)!);

            var markerA = $"bogus-a-{Guid.NewGuid():N}";
            var markerB = $"bogus-b-{Guid.NewGuid():N}";
            File.WriteAllText(invalidPath, $$"""
                {"theme":"{{markerA}}\n{{markerB}}"}
                """);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = InvokeResolveThemeModeFromCandidates([invalidPath], options);

            Assert.Equal(ThemeMode.System, result);

            var content = File.ReadAllText(CrashFileLogger.LogFilePath);
            Assert.Contains(
                $"Skipping config '{invalidPath}' because it does not specify a valid theme. Value='{markerA} {markerB}'.",
                content);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WriteSkippedConfigWarning_WhenExceptionMessageGetterThrows_UsesFallbackMarker()
    {
        var marker = $"config-warning-{Guid.NewGuid():N}";
        var path = $"/tmp/{marker}/praxis.config.json";

        InvokeWriteSkippedConfigWarning(path, new ThrowingUnauthorizedAccessException());

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Skipping config '{path}' after ThrowingUnauthorizedAccessException: (failed to read exception message: System.InvalidOperationException: message getter failure)", content);
    }

    [Fact]
    public void WriteSkippedConfigWarning_WhenExceptionMessageIsMultiline_CollapsesToSingleLine()
    {
        var marker = $"config-warning-{Guid.NewGuid():N}";
        var path = $"/tmp/{marker}/praxis.config.json";
        var markerA = $"config-a-{Guid.NewGuid():N}";
        var markerB = $"config-b-{Guid.NewGuid():N}";

        InvokeWriteSkippedConfigWarning(path, new MultilineUnauthorizedAccessException($"{markerA}\r\n{markerB}"));

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Skipping config '{path}' after MultilineUnauthorizedAccessException: {markerA} {markerB}", content);
    }

    [Fact]
    public void WriteSkippedConfigWarning_WhenExceptionMessageIsWhitespace_UsesEmptyMarker()
    {
        var marker = $"config-warning-{Guid.NewGuid():N}";
        var path = $"/tmp/{marker}/praxis.config.json";

        InvokeWriteSkippedConfigWarning(path, new WhitespaceUnauthorizedAccessException());

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Skipping config '{path}' after WhitespaceUnauthorizedAccessException: (empty)", content);
    }

    [Fact]
    public void NormalizePathForLog_WhenPathIsMultiline_CollapsesToSingleLine()
    {
        var markerA = $"config-path-a-{Guid.NewGuid():N}";
        var markerB = $"config-path-b-{Guid.NewGuid():N}";

        var result = InvokeNormalizePathForLog($"/tmp/{markerA}\r\n{markerB}/praxis.config.json");

        Assert.Equal($"/tmp/{markerA} {markerB}/praxis.config.json", result);
    }

    [Fact]
    public void NormalizePathForLog_WhenPathIsWhitespace_UsesPlaceholder()
    {
        var result = InvokeNormalizePathForLog(" \r\n\t ");

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    [Fact]
    public void NormalizePathForLog_WhenPathIsNull_UsesPlaceholder()
    {
        var result = InvokeNormalizePathForLog(null);

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    [Fact]
    public void NormalizeThemeForLog_WhenThemeIsWhitespace_UsesPlaceholder()
    {
        var result = InvokeNormalizeThemeForLog(" \r\n\t ");

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    [Fact]
    public void EnumerateCandidatePaths_DeduplicatesEquivalentDirectories()
    {
        var path = "/tmp/praxis-config";

        var result = InvokeEnumerateCandidatePaths(path, $"  \"{path}\"  ");

        Assert.Single(result);
        Assert.Equal("/tmp/praxis-config/praxis.config.json", result[0]);
    }

    [Fact]
    public void EnumerateCandidatePaths_DeduplicatesCanonicalizedAbsoluteDirectories()
    {
        var root = Path.Combine(Path.GetTempPath(), $"praxis-config-{Guid.NewGuid():N}");
        var canonical = Path.Combine(root, "actual");
        var equivalent = Path.Combine(root, "nested", "..", "actual");

        Directory.CreateDirectory(canonical);
        try
        {
            var result = InvokeEnumerateCandidatePaths(canonical, equivalent);

            Assert.Single(result);
            Assert.Equal(Path.Combine(canonical, "praxis.config.json"), result[0]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EnumerateCandidatePaths_IgnoresBlankOrRelativeDirectories()
    {
        var result = InvokeEnumerateCandidatePaths("relative/config", " \r\n\t ");

        Assert.Empty(result);
    }

    [Fact]
    public void NormalizeAbsoluteDirectory_WhenValueIsNull_ReturnsNull()
    {
        var result = InvokeNormalizeAbsoluteDirectory(null);

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeAbsoluteDirectory_WhenQuotedRelativeValue_ReturnsNull()
    {
        var result = InvokeNormalizeAbsoluteDirectory("  \"relative/config\"  ");

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeAbsoluteDirectory_WhenValueContainsDotSegments_ReturnsCanonicalPath()
    {
        var root = Path.Combine(Path.GetTempPath(), $"praxis-config-{Guid.NewGuid():N}");
        var canonical = Path.Combine(root, "actual");
        var equivalent = Path.Combine(root, "nested", "..", "actual");

        Directory.CreateDirectory(canonical);
        try
        {
            var result = InvokeNormalizeAbsoluteDirectory(equivalent);

            Assert.Equal(Path.GetFullPath(canonical), result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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

    private static void InvokeWriteSkippedConfigWarning(string path, Exception ex)
    {
        var method = typeof(FileAppConfigService).GetMethod("WriteSkippedConfigWarning", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var invocation = Record.Exception(() => method.Invoke(null, [path, ex]));
        Assert.Null(invocation);
    }

    private static string InvokeNormalizePathForLog(string? path)
    {
        var method = typeof(FileAppConfigService).GetMethod("NormalizePathForLog", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [path]);
        return Assert.IsType<string>(result);
    }

    private static string? InvokeNormalizeAbsoluteDirectory(string? path)
    {
        var method = typeof(FileAppConfigService).GetMethod("NormalizeAbsoluteDirectory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return method.Invoke(null, [path]) as string;
    }

    private static string InvokeNormalizeThemeForLog(string? theme)
    {
        var method = typeof(FileAppConfigService).GetMethod("NormalizeThemeForLog", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [theme]);
        return Assert.IsType<string>(result);
    }

    private sealed class ThrowingUnauthorizedAccessException : UnauthorizedAccessException
    {
        public override string Message => throw new InvalidOperationException("message getter failure");
    }

    private sealed class MultilineUnauthorizedAccessException(string value) : UnauthorizedAccessException
    {
        public override string Message => value;
    }

    private sealed class WhitespaceUnauthorizedAccessException : UnauthorizedAccessException
    {
        public override string Message => " \r\n\t ";
    }
}
