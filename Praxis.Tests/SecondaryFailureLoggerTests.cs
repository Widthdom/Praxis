using Praxis.Services;

namespace Praxis.Tests;

public class SecondaryFailureLoggerTests
{
    [Fact]
    public void TryAppendFallbackContent_UsesCurrentDirectoryFallback_WhenTempRootCannotHostPraxisDirectory()
    {
        var tempRootSentinel = Path.Combine(Path.GetTempPath(), $"secondary-failure-sentinel-{Guid.NewGuid():N}.txt");
        var currentDirectoryRoot = Path.Combine(Path.GetTempPath(), $"secondary-failure-root-{Guid.NewGuid():N}");
        var marker = $"secondary-fallback-{Guid.NewGuid():N}";

        File.WriteAllText(tempRootSentinel, "occupied");
        Directory.CreateDirectory(currentDirectoryRoot);

        try
        {
            var success = SecondaryFailureLogger.TryAppendFallbackContent(
                marker,
                out var writtenPath,
                tempRootSentinel,
                currentDirectoryRoot);

            Assert.True(success);

            var expectedPath = Path.Combine(
                currentDirectoryRoot,
                "Praxis",
                SecondaryFailureLogger.FallbackLogFileName);

            Assert.Equal(expectedPath, writtenPath);
            Assert.Contains(marker, File.ReadAllText(expectedPath));
        }
        finally
        {
            if (File.Exists(tempRootSentinel))
            {
                File.Delete(tempRootSentinel);
            }

            if (Directory.Exists(currentDirectoryRoot))
            {
                Directory.Delete(currentDirectoryRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryAppendFallbackContent_ReturnsFalse_WhenAllFallbackRootsAreBlocked()
    {
        var tempRootSentinel = Path.Combine(Path.GetTempPath(), $"secondary-blocked-temp-{Guid.NewGuid():N}.txt");
        var currentRootSentinel = Path.Combine(Path.GetTempPath(), $"secondary-blocked-current-{Guid.NewGuid():N}.txt");

        File.WriteAllText(tempRootSentinel, "occupied");
        File.WriteAllText(currentRootSentinel, "occupied");

        try
        {
            var success = SecondaryFailureLogger.TryAppendFallbackContent(
                "blocked",
                out var writtenPath,
                tempRootSentinel,
                currentRootSentinel);

            Assert.False(success);
            Assert.Null(writtenPath);
        }
        finally
        {
            if (File.Exists(tempRootSentinel))
            {
                File.Delete(tempRootSentinel);
            }

            if (File.Exists(currentRootSentinel))
            {
                File.Delete(currentRootSentinel);
            }
        }
    }

    [Fact]
    public void TryAppendFallbackContent_AcceptsQuotedAbsoluteFallbackRoot()
    {
        var tempRootSentinel = Path.Combine(Path.GetTempPath(), $"secondary-quoted-temp-{Guid.NewGuid():N}.txt");
        var currentDirectoryRoot = Path.Combine(Path.GetTempPath(), $"secondary-quoted-root-{Guid.NewGuid():N}");
        var marker = $"secondary-quoted-{Guid.NewGuid():N}";

        File.WriteAllText(tempRootSentinel, "occupied");
        Directory.CreateDirectory(currentDirectoryRoot);

        try
        {
            var success = SecondaryFailureLogger.TryAppendFallbackContent(
                marker,
                out var writtenPath,
                tempRootSentinel,
                $"  \"{currentDirectoryRoot}\"  ");

            Assert.True(success);

            var expectedPath = Path.Combine(
                currentDirectoryRoot,
                "Praxis",
                SecondaryFailureLogger.FallbackLogFileName);

            Assert.Equal(expectedPath, writtenPath);
            Assert.Contains(marker, File.ReadAllText(expectedPath));
        }
        finally
        {
            if (File.Exists(tempRootSentinel))
            {
                File.Delete(tempRootSentinel);
            }

            if (Directory.Exists(currentDirectoryRoot))
            {
                Directory.Delete(currentDirectoryRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryAppendFallbackContent_IgnoresRelativeFallbackRoots()
    {
        var success = SecondaryFailureLogger.TryAppendFallbackContent(
            "relative-root",
            out var writtenPath,
            "relative-temp-root",
            "relative-current-root");

        Assert.False(success);
        Assert.Null(writtenPath);
    }

    [Fact]
    public void TryReportStartupLogFailure_WritesFallbackFile_WhenPrimaryCrashSinkFails()
    {
        var tempRootSentinel = Path.Combine(Path.GetTempPath(), $"secondary-report-sentinel-{Guid.NewGuid():N}.txt");
        var currentDirectoryRoot = Path.Combine(Path.GetTempPath(), $"secondary-report-root-{Guid.NewGuid():N}");
        var targetPath = Path.Combine("broken-root", "startup.log");
        var originalMessageMarker = $"startup-message-{Guid.NewGuid():N}";

        File.WriteAllText(tempRootSentinel, "occupied");
        Directory.CreateDirectory(currentDirectoryRoot);

        try
        {
            var success = SecondaryFailureLogger.TryReportStartupLogFailure(
                nameof(SecondaryFailureLoggerTests),
                targetPath,
                "Failed to append startup log",
                new ThrowingStackTraceException(),
                new ThrowingMessageException(),
                originalMessageMarker,
                out var fallbackPath,
                tryWriteException: static (_, _) => false,
                tryWriteWarning: static (_, _) => false,
                tempRootOverride: tempRootSentinel,
                currentDirectoryOverride: currentDirectoryRoot);

            Assert.True(success);
            Assert.NotNull(fallbackPath);

            var content = File.ReadAllText(fallbackPath!);
            Assert.Contains($"Warning: Failed to append startup log '{targetPath}':", content);
            Assert.Contains("PrimaryCrashLogExceptionWriteSucceeded: False", content);
            Assert.Contains("PrimaryCrashLogWarningWriteSucceeded: False", content);
            Assert.Contains("Original startup exception payload:", content);
            Assert.Contains("ThrowingMessageException", content);
            Assert.Contains("failed to read exception message: System.InvalidOperationException: message getter failure", content);
            Assert.Contains("Failure while persisting startup diagnostics:", content);
            Assert.Contains("ThrowingStackTraceException", content);
            Assert.Contains("failed to read stack trace: System.InvalidOperationException: stack trace getter failure", content);
        }
        finally
        {
            if (File.Exists(tempRootSentinel))
            {
                File.Delete(tempRootSentinel);
            }

            if (Directory.Exists(currentDirectoryRoot))
            {
                Directory.Delete(currentDirectoryRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryReportStartupLogFailure_NormalizesMultilineFailureMessages_InFallbackWarning()
    {
        var tempRootSentinel = Path.Combine(Path.GetTempPath(), $"secondary-multiline-sentinel-{Guid.NewGuid():N}.txt");
        var currentDirectoryRoot = Path.Combine(Path.GetTempPath(), $"secondary-multiline-root-{Guid.NewGuid():N}");
        var targetPath = Path.Combine("broken-root", "startup.log");
        var markerA = $"secondary-a-{Guid.NewGuid():N}";
        var markerB = $"secondary-b-{Guid.NewGuid():N}";

        File.WriteAllText(tempRootSentinel, "occupied");
        Directory.CreateDirectory(currentDirectoryRoot);

        try
        {
            var success = SecondaryFailureLogger.TryReportStartupLogFailure(
                nameof(SecondaryFailureLoggerTests),
                targetPath,
                "Failed to append startup log",
                new MultilineMessageException($"{markerA}\r\n{markerB}"),
                originalException: null,
                originalMessage: null,
                out var fallbackPath,
                tryWriteException: static (_, _) => false,
                tryWriteWarning: static (_, _) => false,
                tempRootOverride: tempRootSentinel,
                currentDirectoryOverride: currentDirectoryRoot);

            Assert.True(success);
            Assert.NotNull(fallbackPath);

            var content = File.ReadAllText(fallbackPath!);
            Assert.Contains($"Warning: Failed to append startup log '{targetPath}': {markerA} {markerB}", content);
        }
        finally
        {
            if (File.Exists(tempRootSentinel))
            {
                File.Delete(tempRootSentinel);
            }

            if (Directory.Exists(currentDirectoryRoot))
            {
                Directory.Delete(currentDirectoryRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryReportStartupLogFailure_NormalizesMultilineTargetPathAndOperation_InFallbackContent()
    {
        var tempRootSentinel = Path.Combine(Path.GetTempPath(), $"secondary-target-sentinel-{Guid.NewGuid():N}.txt");
        var currentDirectoryRoot = Path.Combine(Path.GetTempPath(), $"secondary-target-root-{Guid.NewGuid():N}");
        var pathMarkerA = $"startup-path-a-{Guid.NewGuid():N}";
        var pathMarkerB = $"startup-path-b-{Guid.NewGuid():N}";
        var operationMarkerA = $"operation-a-{Guid.NewGuid():N}";
        var operationMarkerB = $"operation-b-{Guid.NewGuid():N}";
        var targetPath = $"/tmp/{pathMarkerA}\r\n{pathMarkerB}/startup.log";
        var operation = $"{operationMarkerA}\r\n{operationMarkerB}";

        File.WriteAllText(tempRootSentinel, "occupied");
        Directory.CreateDirectory(currentDirectoryRoot);

        try
        {
            var success = SecondaryFailureLogger.TryReportStartupLogFailure(
                nameof(SecondaryFailureLoggerTests),
                targetPath,
                operation,
                new InvalidOperationException("append failed"),
                originalException: null,
                originalMessage: null,
                out var fallbackPath,
                tryWriteException: static (_, _) => false,
                tryWriteWarning: static (_, _) => false,
                tempRootOverride: tempRootSentinel,
                currentDirectoryOverride: currentDirectoryRoot);

            Assert.True(success);
            Assert.NotNull(fallbackPath);

            var content = File.ReadAllText(fallbackPath!);
            Assert.Contains($"TargetPath: /tmp/{pathMarkerA} {pathMarkerB}/startup.log", content);
            Assert.Contains($"Operation: {operationMarkerA} {operationMarkerB}", content);
            Assert.Contains($"Warning: {operationMarkerA} {operationMarkerB} '/tmp/{pathMarkerA} {pathMarkerB}/startup.log': append failed", content);
        }
        finally
        {
            if (File.Exists(tempRootSentinel))
            {
                File.Delete(tempRootSentinel);
            }

            if (Directory.Exists(currentDirectoryRoot))
            {
                Directory.Delete(currentDirectoryRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryReportStartupLogFailure_NormalizesWhitespaceFailureMessages_ToEmptyMarker()
    {
        var tempRootSentinel = Path.Combine(Path.GetTempPath(), $"secondary-empty-sentinel-{Guid.NewGuid():N}.txt");
        var currentDirectoryRoot = Path.Combine(Path.GetTempPath(), $"secondary-empty-root-{Guid.NewGuid():N}");
        var targetPath = Path.Combine("broken-root", "startup.log");

        File.WriteAllText(tempRootSentinel, "occupied");
        Directory.CreateDirectory(currentDirectoryRoot);

        try
        {
            var success = SecondaryFailureLogger.TryReportStartupLogFailure(
                nameof(SecondaryFailureLoggerTests),
                targetPath,
                "Failed to append startup log",
                new WhitespaceMessageException(),
                originalException: null,
                originalMessage: null,
                out var fallbackPath,
                tryWriteException: static (_, _) => false,
                tryWriteWarning: static (_, _) => false,
                tempRootOverride: tempRootSentinel,
                currentDirectoryOverride: currentDirectoryRoot);

            Assert.True(success);
            Assert.NotNull(fallbackPath);

            var content = File.ReadAllText(fallbackPath!);
            Assert.Contains($"Warning: Failed to append startup log '{targetPath}': (empty)", content);
        }
        finally
        {
            if (File.Exists(tempRootSentinel))
            {
                File.Delete(tempRootSentinel);
            }

            if (Directory.Exists(currentDirectoryRoot))
            {
                Directory.Delete(currentDirectoryRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryReportStartupLogFailure_NormalizesWhitespaceOriginalMessage_ToPlaceholder()
    {
        var tempRootSentinel = Path.Combine(Path.GetTempPath(), $"secondary-original-empty-sentinel-{Guid.NewGuid():N}.txt");
        var currentDirectoryRoot = Path.Combine(Path.GetTempPath(), $"secondary-original-empty-root-{Guid.NewGuid():N}");
        var targetPath = Path.Combine("broken-root", "startup.log");

        File.WriteAllText(tempRootSentinel, "occupied");
        Directory.CreateDirectory(currentDirectoryRoot);

        try
        {
            var success = SecondaryFailureLogger.TryReportStartupLogFailure(
                nameof(SecondaryFailureLoggerTests),
                targetPath,
                "Failed to append startup log",
                new InvalidOperationException("append failed"),
                originalException: null,
                originalMessage: " \r\n\t ",
                out var fallbackPath,
                tryWriteException: static (_, _) => false,
                tryWriteWarning: static (_, _) => false,
                tempRootOverride: tempRootSentinel,
                currentDirectoryOverride: currentDirectoryRoot);

            Assert.True(success);
            Assert.NotNull(fallbackPath);

            var content = File.ReadAllText(fallbackPath!);
            Assert.Contains($"Original startup message: {CrashFileLogger.MissingMessagePayloadPlaceholder}", content);
        }
        finally
        {
            if (File.Exists(tempRootSentinel))
            {
                File.Delete(tempRootSentinel);
            }

            if (Directory.Exists(currentDirectoryRoot))
            {
                Directory.Delete(currentDirectoryRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void NormalizeOperationForLog_WhenValueIsWhitespace_UsesPlaceholder()
    {
        var result = InvokeNormalizeOperationForLog(" \r\n\t ");

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    [Fact]
    public void NormalizeOperationForLog_WhenValueIsNull_UsesPlaceholder()
    {
        var result = InvokeNormalizeOperationForLog(null);

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    [Fact]
    public void NormalizeOperationForLog_WhenValueIsMultiline_CollapsesToSingleLine()
    {
        var markerA = $"secondary-operation-a-{Guid.NewGuid():N}";
        var markerB = $"secondary-operation-b-{Guid.NewGuid():N}";

        var result = InvokeNormalizeOperationForLog($"{markerA}\r\n{markerB}");

        Assert.Equal($"{markerA} {markerB}", result);
    }

    [Fact]
    public void NormalizePathForLog_WhenValueIsMultiline_CollapsesToSingleLine()
    {
        var markerA = $"secondary-path-a-{Guid.NewGuid():N}";
        var markerB = $"secondary-path-b-{Guid.NewGuid():N}";

        var result = InvokeNormalizePathForLog($"/tmp/{markerA}\r\n{markerB}/startup.log");

        Assert.Equal($"/tmp/{markerA} {markerB}/startup.log", result);
    }

    [Fact]
    public void NormalizePathForLog_WhenValueIsWhitespace_UsesPlaceholder()
    {
        var result = InvokeNormalizePathForLog(" \r\n\t ");

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    [Fact]
    public void NormalizePathForLog_WhenValueIsNull_UsesPlaceholder()
    {
        var result = InvokeNormalizePathForLog(null);

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    [Fact]
    public void TryReportStartupLogFailure_NormalizesMultilineOriginalMessage_ToSingleLine()
    {
        var tempRootSentinel = Path.Combine(Path.GetTempPath(), $"secondary-original-multiline-sentinel-{Guid.NewGuid():N}.txt");
        var currentDirectoryRoot = Path.Combine(Path.GetTempPath(), $"secondary-original-multiline-root-{Guid.NewGuid():N}");
        var targetPath = Path.Combine("broken-root", "startup.log");
        var markerA = $"startup-a-{Guid.NewGuid():N}";
        var markerB = $"startup-b-{Guid.NewGuid():N}";

        File.WriteAllText(tempRootSentinel, "occupied");
        Directory.CreateDirectory(currentDirectoryRoot);

        try
        {
            var success = SecondaryFailureLogger.TryReportStartupLogFailure(
                nameof(SecondaryFailureLoggerTests),
                targetPath,
                "Failed to append startup log",
                new InvalidOperationException("append failed"),
                originalException: null,
                originalMessage: $"{markerA}\r\n{markerB}",
                out var fallbackPath,
                tryWriteException: static (_, _) => false,
                tryWriteWarning: static (_, _) => false,
                tempRootOverride: tempRootSentinel,
                currentDirectoryOverride: currentDirectoryRoot);

            Assert.True(success);
            Assert.NotNull(fallbackPath);

            var content = File.ReadAllText(fallbackPath!);
            Assert.Contains($"Original startup message: {markerA} {markerB}", content);
        }
        finally
        {
            if (File.Exists(tempRootSentinel))
            {
                File.Delete(tempRootSentinel);
            }

            if (Directory.Exists(currentDirectoryRoot))
            {
                Directory.Delete(currentDirectoryRoot, recursive: true);
            }
        }
    }

    private sealed class ThrowingMessageException : Exception
    {
        public override string Message => throw new InvalidOperationException("message getter failure");
    }

    private static string InvokeNormalizeOperationForLog(string? value)
    {
        var method = typeof(SecondaryFailureLogger).GetMethod("NormalizeOperationForLog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [value]);
        return Assert.IsType<string>(result);
    }

    private static string InvokeNormalizePathForLog(string? value)
    {
        var method = typeof(SecondaryFailureLogger).GetMethod("NormalizePathForLog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [value]);
        return Assert.IsType<string>(result);
    }

    private sealed class ThrowingStackTraceException : Exception
    {
        public override string? StackTrace => throw new InvalidOperationException("stack trace getter failure");
    }

    private sealed class MultilineMessageException(string value) : Exception
    {
        public override string Message => value;
    }

    private sealed class WhitespaceMessageException : Exception
    {
        public override string Message => " \r\n\t ";
    }
}
