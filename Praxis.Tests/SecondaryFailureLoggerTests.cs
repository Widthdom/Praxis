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

    private sealed class ThrowingMessageException : Exception
    {
        public override string Message => throw new InvalidOperationException("message getter failure");
    }

    private sealed class ThrowingStackTraceException : Exception
    {
        public override string? StackTrace => throw new InvalidOperationException("stack trace getter failure");
    }
}
