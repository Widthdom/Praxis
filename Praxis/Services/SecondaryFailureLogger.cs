using System.Text;

namespace Praxis.Services;

internal static class SecondaryFailureLogger
{
    private static readonly object WriteLock = new();
    internal const string FallbackLogFileName = "secondary-failures.log";

    internal static void ReportStartupLogFailure(
        string source,
        string targetPath,
        string operationDescription,
        Exception failureException,
        Exception? originalException = null,
        string? originalMessage = null)
    {
        _ = TryReportStartupLogFailure(
            source,
            targetPath,
            operationDescription,
            failureException,
            originalException,
            originalMessage,
            out _);
    }

    internal static bool TryReportStartupLogFailure(
        string source,
        string targetPath,
        string operationDescription,
        Exception failureException,
        Exception? originalException,
        string? originalMessage,
        out string? fallbackPath,
        Func<string, Exception?, bool>? tryWriteException = null,
        Func<string, string, bool>? tryWriteWarning = null,
        string? tempRootOverride = null,
        string? currentDirectoryOverride = null)
    {
        fallbackPath = null;

        try
        {
            tryWriteException ??= CrashFileLogger.TryWriteException;
            tryWriteWarning ??= CrashFileLogger.TryWriteWarning;

            var normalizedSource = CrashFileLogger.NormalizeSource(source);
            var normalizedTargetPath = NormalizePathForLog(targetPath);
            var normalizedOperation = NormalizeOperationForLog(operationDescription);
            var safeMessage = CrashFileLogger.SafeExceptionMessage(failureException);
            var warningMessage = $"{normalizedOperation} '{normalizedTargetPath}': {safeMessage}";

            var wroteException = tryWriteException(normalizedSource, failureException);
            var wroteWarning = tryWriteWarning(normalizedSource, warningMessage);
            if (wroteException && wroteWarning)
            {
                return true;
            }

            var content = BuildStartupLogFailureFallbackContent(
                normalizedSource,
                normalizedTargetPath,
                normalizedOperation,
                warningMessage,
                wroteException,
                wroteWarning,
                failureException,
                originalException,
                originalMessage);

            return TryAppendFallbackContent(
                content,
                out fallbackPath,
                tempRootOverride,
                currentDirectoryOverride);
        }
        catch
        {
            fallbackPath = null;
            return false;
        }
    }

    internal static bool TryAppendFallbackContent(
        string content,
        out string? writtenPath,
        string? tempRootOverride = null,
        string? currentDirectoryOverride = null)
    {
        writtenPath = null;

        foreach (var candidatePath in EnumerateFallbackPaths(tempRootOverride, currentDirectoryOverride))
        {
            try
            {
                var directory = Path.GetDirectoryName(candidatePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                lock (WriteLock)
                {
                    File.AppendAllText(candidatePath, content, Encoding.UTF8);
                }

                writtenPath = candidatePath;
                return true;
            }
            catch
            {
                // Try the next independent fallback sink.
            }
        }

        return false;
    }

    private static string BuildStartupLogFailureFallbackContent(
        string source,
        string targetPath,
        string operationDescription,
        string warningMessage,
        bool wroteException,
        bool wroteWarning,
        Exception failureException,
        Exception? originalException,
        string? originalMessage)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] FALLBACK {source}");
            sb.AppendLine($"  TargetPath: {targetPath}");
            sb.AppendLine($"  Operation: {operationDescription}");
            sb.AppendLine($"  PrimaryCrashLogExceptionWriteSucceeded: {wroteException}");
            sb.AppendLine($"  PrimaryCrashLogWarningWriteSucceeded: {wroteWarning}");
            sb.AppendLine($"  Warning: {warningMessage}");

            if (originalException is not null)
            {
                sb.AppendLine("  Original startup exception payload:");
                sb.Append(CrashFileLogger.FormatExceptionPayload(originalException));
            }
            else if (originalMessage is not null)
            {
                sb.AppendLine($"  Original startup message: {CrashFileLogger.NormalizeMessagePayload(originalMessage)}");
            }

            sb.AppendLine("  Failure while persisting startup diagnostics:");
            sb.Append(CrashFileLogger.FormatExceptionPayload(failureException));
            sb.AppendLine(new string('-', 80));
            return sb.ToString();
        }
        catch (Exception formatterEx)
        {
            var safeFormatterMessage = CrashFileLogger.SafeExceptionMessage(formatterEx);
            return
                $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] FALLBACK {source}{Environment.NewLine}" +
                $"  Failed to format fallback diagnostics for '{targetPath}': {safeFormatterMessage}{Environment.NewLine}" +
                $"{new string('-', 80)}{Environment.NewLine}";
        }
    }

    private static string NormalizePathForLog(string path)
        => CrashFileLogger.NormalizeMessagePayload(path);

    private static string NormalizeOperationForLog(string value)
        => CrashFileLogger.NormalizeMessagePayload(value);

    private static IEnumerable<string> EnumerateFallbackPaths(string? tempRootOverride, string? currentDirectoryOverride)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in new[]
                 {
                     tempRootOverride ?? GetTempPathSafe(),
                     currentDirectoryOverride ?? GetCurrentDirectorySafe()
                 })
        {
            if (!TryBuildFallbackPath(root, out var candidatePath) || candidatePath is null)
            {
                continue;
            }

            if (seen.Add(candidatePath))
            {
                yield return candidatePath;
            }
        }
    }

    private static bool TryBuildFallbackPath(string? root, out string? candidatePath)
    {
        candidatePath = null;
        if (string.IsNullOrWhiteSpace(root))
        {
            return false;
        }

        try
        {
            candidatePath = Path.Combine(root, "Praxis", FallbackLogFileName);
            return true;
        }
        catch
        {
            candidatePath = null;
            return false;
        }
    }

    private static string? GetTempPathSafe()
    {
        try
        {
            return Path.GetTempPath();
        }
        catch
        {
            return null;
        }
    }

    private static string? GetCurrentDirectorySafe()
    {
        try
        {
            return Environment.CurrentDirectory;
        }
        catch
        {
            return null;
        }
    }
}
