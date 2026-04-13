using System.Collections;
using System.Text;

namespace Praxis.Services;

/// <summary>
/// Synchronous file-based logger that persists crash information immediately to disk.
/// Used as a fallback when the async DB logger may not complete before process termination.
/// </summary>
public static class CrashFileLogger
{
    private static readonly object WriteLock = new();

    private static readonly string CrashLogPath = Path.Combine(
        ResolveCrashLogDirectory(),
        "crash.log");

    /// <summary>
    /// Maximum size in bytes before the crash log is rotated (~512 KB).
    /// </summary>
    private const long MaxLogSize = 512 * 1024;

    /// <summary>
    /// Maximum depth of inner-exception recursion when serializing an exception chain.
    /// Protects the last-resort logger from StackOverflow on pathological chains
    /// (e.g. a self-referential AggregateException).
    /// </summary>
    internal const int MaxExceptionChainDepth = 32;

    /// <summary>
    /// Total number of exception nodes the last-resort logger will serialize
    /// in a single call. Guards against wide <see cref="AggregateException"/>
    /// fan-out (thousands of task failures under one aggregate) synchronously
    /// blocking the UI or ballooning crash.log.
    /// </summary>
    internal const int MaxExceptionNodes = 256;

    /// <summary>
    /// Per-<see cref="AggregateException"/> child-edge scan cap, independent of the
    /// node budget. Bounds synchronous work at the logger level: without this cap,
    /// an aggregate of N repeated references would iterate N hashset lookups (cheap
    /// per-op but O(N)) on the caller thread, four times over (two DB builders +
    /// stack-trace + crash-file). With the cap, work is O(budget) regardless of
    /// aggregate width. A later distinct tail beyond this cap is intentionally not
    /// preserved on the synchronous error path — bounded logging is the higher
    /// priority when the app is already in distress.
    /// </summary>
    internal const int MaxAggregateChildEdgeScan = 4096;

    /// <summary>
    /// Size of the suffix sample scanned after the prefix edge-scan cap kicks in.
    /// Ensures a far-tail distinct exception (e.g. the actual root cause at the
    /// end of a huge duplicate-noise aggregate) still reaches persisted logs
    /// instead of being silently discarded. Total synchronous scan positions per
    /// aggregate stay bounded at roughly <see cref="MaxAggregateChildEdgeScan"/> +
    /// <see cref="MaxAggregateChildTailSample"/>.
    /// </summary>
    internal const int MaxAggregateChildTailSample = 128;

    /// <summary>
    /// Minimum number of node-budget slots reserved for the tail sample before
    /// the prefix loop starts consuming budget. Guarantees a far-tail distinct
    /// exception survives even on an all-unique wide aggregate where the prefix
    /// alone would otherwise exhaust the budget.
    /// </summary>
    internal const int AggregateChildTailReserve = 16;

    /// <summary>
    /// Evenly-spaced sample points taken from the middle region (between prefix
    /// scan and tail sample) so a distinct interior child is not deterministically
    /// lost on very wide aggregates.
    /// </summary>
    internal const int MaxAggregateChildMiddleSample = 8;
    internal const string MissingSourcePlaceholder = "(unknown source)";
    internal const string MissingContextPlaceholder = "(unknown context)";
    internal const string MissingMessagePayloadPlaceholder = "(no message payload)";

    private sealed class TraversalBudget
    {
        public int RemainingNodes = MaxExceptionNodes;
        public readonly HashSet<Exception> Visited = new(ReferenceEqualityComparer.Instance);
    }

    public static string LogFilePath => CrashLogPath;

    /// <summary>
    /// Writes an exception synchronously to the crash log file.
    /// This method never throws.
    /// </summary>
    public static void WriteException(string source, Exception? exception)
    {
        try
        {
            var normalizedSource = NormalizeSource(source);
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {normalizedSource}");
            sb.Append(FormatExceptionPayload(exception));
            sb.AppendLine(new string('-', 80));
            WriteToDisk(sb.ToString());
        }
        catch
        {
            // Must never throw — this is the last-resort logger.
        }
    }

    /// <summary>
    /// Writes an informational message synchronously to the crash log file.
    /// This method never throws.
    /// </summary>
    public static void WriteInfo(string source, string message)
    {
        try
        {
            var normalizedSource = NormalizeSource(source);
            var normalizedMessage = NormalizeMessagePayload(message);
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] INFO {normalizedSource}");
            sb.AppendLine($"  {normalizedMessage}");
            sb.AppendLine(new string('-', 80));
            WriteToDisk(sb.ToString());
        }
        catch
        {
            // Must never throw.
        }
    }

    /// <summary>
    /// Writes a warning message synchronously to the crash log file.
    /// This method never throws.
    /// </summary>
    public static void WriteWarning(string source, string message)
    {
        try
        {
            var normalizedSource = NormalizeSource(source);
            var normalizedMessage = NormalizeMessagePayload(message);
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] WARN {normalizedSource}");
            sb.AppendLine($"  {normalizedMessage}");
            sb.AppendLine(new string('-', 80));
            WriteToDisk(sb.ToString());
        }
        catch
        {
            // Must never throw.
        }
    }

    internal static string NormalizeSource(string? source)
        => NormalizeInlineField(source, MissingSourcePlaceholder);

    internal static string NormalizeContext(string? context)
        => NormalizeInlineField(context, MissingContextPlaceholder);

    internal static string NormalizeMessagePayload(string? message)
        => NormalizeInlineField(message, MissingMessagePayloadPlaceholder);

    internal static string NormalizeExceptionMessage(string? message)
        => string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : message.ReplaceLineEndings(" ").Trim();

    internal static string SafeExceptionMessage(Exception ex)
    {
        try
        {
            return NormalizeExceptionMessage(ex.Message);
        }
        catch (Exception getterEx)
        {
            return $"(failed to read exception message: {DescribeLoggingFailure(getterEx)})";
        }
    }

    internal static string SafeExceptionStackTrace(Exception ex)
    {
        try
        {
            return ex.StackTrace ?? string.Empty;
        }
        catch (Exception getterEx)
        {
            return $"(failed to read stack trace: {DescribeLoggingFailure(getterEx)})";
        }
    }

    internal static string FormatExceptionPayload(Exception? exception)
    {
        try
        {
            var sb = new StringBuilder();
            if (exception is null)
            {
                sb.AppendLine("  (no exception payload)");
            }
            else
            {
                AppendExceptionChain(sb, exception, depth: 0, new TraversalBudget());
            }

            return sb.ToString();
        }
        catch (Exception formatterEx)
        {
            return $"  (failed to format exception payload: {DescribeLoggingFailure(formatterEx)}){Environment.NewLine}";
        }
    }

    private static string NormalizeInlineField(string? value, string placeholder)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return placeholder;
        }

        var normalized = value.ReplaceLineEndings(" ").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? placeholder : normalized;
    }

    /// <summary>
    /// Returns a message suitable for logging without triggering
    /// <see cref="AggregateException.Message"/>'s linear expansion over every
    /// inner exception (which would blow up on wide aggregates). The inner
    /// messages are still captured below through the bounded child traversal.
    /// </summary>
    /// <summary>
    /// Size below which <see cref="AggregateException.Message"/> is cheap enough
    /// to invoke directly (it enumerates every inner exception). Above the
    /// threshold we fall back to a synthetic bounded summary, trading the
    /// caller-supplied top-level text for guaranteed O(1) work and no dependence
    /// on private framework fields.
    /// </summary>
    internal const int SmallAggregateMessageThreshold = 8;

    /// <summary>
    /// Upper bound on total descendants a `.Message` invocation may safely
    /// expand. Allows nested aggregates whose full tree is still small.
    /// </summary>
    internal const int AggregateMessageExpansionCap = SmallAggregateMessageThreshold * 4;

    private static string SafeMessage(Exception ex)
    {
        if (ex is AggregateException agg)
        {
            if (TryGetAggregateTopLevelSummary(agg, out var summary))
            {
                return summary;
            }

            return $"AggregateException ({agg.InnerExceptions.Count} inner exceptions; top-level summary omitted — wide/nested aggregate)";
        }

        return SafeExceptionMessage(ex);
    }

    /// <summary>
    /// Returns <see cref="AggregateException.Message"/> only after confirming the
    /// total descendant count is bounded, so invoking <c>.Message</c> (which
    /// expands every inner exception recursively) cannot explode on a large
    /// nested graph. Uses a bounded BFS that bails as soon as the cap would be
    /// exceeded — a wide or deeply-nested wrapper exits after inspecting at most
    /// <see cref="AggregateMessageExpansionCap"/> nodes.
    /// </summary>
    internal static bool TryGetAggregateTopLevelSummary(AggregateException agg, out string summary)
    {
        var queue = new Queue<Exception>();
        queue.Enqueue(agg);
        var visited = 0;

        while (queue.Count > 0)
        {
            if (visited + queue.Count > AggregateMessageExpansionCap)
            {
                summary = string.Empty;
                return false;
            }

            var e = queue.Dequeue();
            visited++;

            if (e is AggregateException a)
            {
                if (visited + queue.Count + a.InnerExceptions.Count > AggregateMessageExpansionCap)
                {
                    summary = string.Empty;
                    return false;
                }

                for (var i = 0; i < a.InnerExceptions.Count; i++)
                {
                    queue.Enqueue(a.InnerExceptions[i]);
                }
            }
            else if (e.InnerException is not null)
            {
                queue.Enqueue(e.InnerException);
            }
        }

        // Safe: total tree size <= cap, so .Message expansion is bounded.
        summary = SafeExceptionMessage(agg);
        return true;
    }

    private static void AppendExceptionChain(StringBuilder sb, Exception ex, int depth, TraversalBudget budget)
    {
        var indent = new string(' ', (depth + 1) * 2);

        if (depth >= MaxExceptionChainDepth)
        {
            sb.AppendLine($"{indent}--- Exception chain truncated at depth {depth} (max {MaxExceptionChainDepth}) ---");
            return;
        }

        if (budget.RemainingNodes <= 0)
        {
            sb.AppendLine($"{indent}--- Exception graph truncated after {MaxExceptionNodes} total nodes ---");
            return;
        }

        if (!budget.Visited.Add(ex))
        {
            sb.AppendLine($"{indent}--- Already serialized: {ex.GetType().FullName ?? ex.GetType().Name} (shared/cyclic reference) ---");
            return;
        }

        budget.RemainingNodes--;

        if (depth > 0)
        {
            sb.AppendLine($"{indent}--- Inner Exception (depth {depth}) ---");
        }

        sb.AppendLine($"{indent}Type: {ex.GetType().FullName ?? ex.GetType().Name}");
        sb.AppendLine($"{indent}Message: {SafeMessage(ex)}");

        var stackTrace = SafeExceptionStackTrace(ex);
        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            sb.AppendLine($"{indent}StackTrace:");
            foreach (var line in stackTrace.Split('\n'))
            {
                sb.AppendLine($"{indent}  {line.TrimEnd()}");
            }
        }

        AppendExceptionData(sb, ex, indent);

        if (ex is AggregateException agg)
        {
            // If child recursion would hit the depth cap, skip the entire sibling scan
            // and emit one marker. Otherwise every child returns via the depth guard
            // before consuming budget or being marked visited, so the loop would still
            // iterate up to MaxAggregateChildEdgeScan and emit one marker per child.
            if (depth + 1 >= MaxExceptionChainDepth)
            {
                sb.AppendLine($"{indent}--- AggregateException: {agg.InnerExceptions.Count} child(ren) not serialized (would exceed depth cap {MaxExceptionChainDepth}) ---");
                return;
            }

            // Scan every child position so a later distinct exception after a run of
            // duplicates is still serialized while node budget remains. Duplicates are
            // cheap (one hashset lookup) and collapsed into a single summary line, so
            // a 100k repeated-ref aggregate still produces O(1) output.
            var duplicateCount = 0;
            var count = agg.InnerExceptions.Count;
            var scanPrefix = Math.Min(count, MaxAggregateChildEdgeScan);
            var tailStart = Math.Max(scanPrefix, count - MaxAggregateChildTailSample);
            var middleSkipped = tailStart - scanPrefix;
            var hasTail = tailStart < count;

            // Reserve budget so prefix exhaustion cannot starve the tail / interior
            // sample regions. Ordering: prefix (bounded), interior samples, tail.
            var reservedForTail = hasTail ? Math.Min(count - tailStart, AggregateChildTailReserve) : 0;
            var reservedForMiddle = middleSkipped > 0 ? Math.Min(middleSkipped, MaxAggregateChildMiddleSample) : 0;
            var prefixBudget = Math.Max(0, budget.RemainingNodes - reservedForTail - reservedForMiddle);

            var prefixProcessed = 0;
            for (var i = 0; i < scanPrefix; i++)
            {
                var child = agg.InnerExceptions[i];
                if (budget.Visited.Contains(child))
                {
                    duplicateCount++;
                    continue;
                }

                if (prefixProcessed >= prefixBudget)
                {
                    sb.AppendLine($"{indent}--- AggregateException: prefix capped at {prefixProcessed} distinct child(ren) to reserve budget for interior/tail samples ---");
                    break;
                }

                sb.AppendLine($"{indent}--- AggregateException[{i}] ---");
                AppendExceptionChain(sb, child, depth + 1, budget);
                prefixProcessed++;
            }

            if (reservedForMiddle > 0)
            {
                sb.AppendLine($"{indent}--- AggregateException: {middleSkipped} middle child(ren) not fully scanned; sampling {reservedForMiddle} evenly spaced ---");
                for (var s = 0; s < reservedForMiddle; s++)
                {
                    var mIdx = scanPrefix + (int)((long)middleSkipped * s / reservedForMiddle);
                    var child = agg.InnerExceptions[mIdx];
                    if (budget.Visited.Contains(child))
                    {
                        duplicateCount++;
                        continue;
                    }

                    if (budget.RemainingNodes <= reservedForTail)
                    {
                        break;
                    }

                    sb.AppendLine($"{indent}--- AggregateException[{mIdx}] (middle sample) ---");
                    AppendExceptionChain(sb, child, depth + 1, budget);
                }
            }

            if (hasTail)
            {
                var tailBegin = Math.Max(tailStart, count - reservedForTail);
                sb.AppendLine($"{indent}--- AggregateException: sampling last {count - tailBegin} tail child(ren) ---");
                for (var i = tailBegin; i < count; i++)
                {
                    var child = agg.InnerExceptions[i];
                    if (budget.Visited.Contains(child))
                    {
                        duplicateCount++;
                        continue;
                    }

                    sb.AppendLine($"{indent}--- AggregateException[{i}] (tail sample) ---");
                    AppendExceptionChain(sb, child, depth + 1, budget);
                }
            }

            if (duplicateCount > 0)
            {
                sb.AppendLine($"{indent}--- AggregateException: {duplicateCount} duplicate child reference(s) skipped ---");
            }
        }
        else if (ex.InnerException is not null)
        {
            AppendExceptionChain(sb, ex.InnerException, depth + 1, budget);
        }
    }

    private static void WriteToDisk(string content)
    {
        lock (WriteLock)
        {
            var dir = Path.GetDirectoryName(CrashLogPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            RotateIfNeeded();
            File.AppendAllText(CrashLogPath, content, Encoding.UTF8);
        }
    }

    private static void RotateIfNeeded()
    {
        try
        {
            if (!File.Exists(CrashLogPath))
            {
                return;
            }

            var info = new FileInfo(CrashLogPath);
            if (info.Length < MaxLogSize)
            {
                return;
            }

            var rotatedPath = CrashLogPath + ".old";
            if (File.Exists(rotatedPath))
            {
                File.Delete(rotatedPath);
            }

            File.Move(CrashLogPath, rotatedPath);
        }
        catch
        {
            // Rotation failure should not block logging.
        }
    }

    private static string ResolveCrashLogDirectory()
    {
        return ResolveCrashLogDirectory(
            Environment.GetEnvironmentVariable("LOCALAPPDATA"),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.CurrentDirectory,
            OperatingSystem.IsWindows(),
            OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS());
    }

    private static string ResolveCrashLogDirectory(
        string? localAppDataOverride,
        string? userProfileOverride,
        string? localAppDataFolderOverride,
        string currentDirectory,
        bool isWindows,
        bool isMacLike)
    {
        try
        {
            if (isWindows)
            {
                var localAppData = NormalizeAbsoluteDirectory(localAppDataOverride);
                if (!string.IsNullOrWhiteSpace(localAppData))
                {
                    return Path.Combine(localAppData, "Praxis");
                }
            }

            if (isMacLike)
            {
                var home = NormalizeAbsoluteDirectory(userProfileOverride);
                if (!string.IsNullOrWhiteSpace(home))
                {
                    return Path.Combine(home, "Library", "Application Support", "Praxis");
                }
            }

            // Fallback: use local application data.
            var appData = NormalizeAbsoluteDirectory(localAppDataFolderOverride);
            if (!string.IsNullOrWhiteSpace(appData))
            {
                return Path.Combine(appData, "Praxis");
            }

            return Path.Combine(currentDirectory, "Praxis");
        }
        catch
        {
            return Path.Combine(currentDirectory, "Praxis");
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

    private static void AppendExceptionData(StringBuilder sb, Exception ex, string indent)
    {
        try
        {
            if (ex.Data.Count == 0)
            {
                return;
            }

            sb.AppendLine($"{indent}Data:");
            foreach (DictionaryEntry entry in ex.Data)
            {
                var safeKey = FormatExceptionDataItem(entry.Key, "key");
                var safeValue = FormatExceptionDataItem(entry.Value, "value");
                sb.AppendLine($"{indent}  {safeKey} = {safeValue}");
            }
        }
        catch (Exception dataEx)
        {
            sb.AppendLine($"{indent}Data: (failed to enumerate Exception.Data: {DescribeLoggingFailure(dataEx)})");
        }
    }

    private static string FormatExceptionDataItem(object? value, string role)
    {
        if (value is null)
        {
            return "(null)";
        }

        try
        {
            var text = NormalizeExceptionMessage(value.ToString());
            return string.IsNullOrEmpty(text) ? "(empty)" : text;
        }
        catch (Exception formatEx)
        {
            return $"(failed to format data {role}: {DescribeLoggingFailure(formatEx)})";
        }
    }

    private static string DescribeLoggingFailure(Exception ex)
    {
        var typeName = ex.GetType().FullName ?? ex.GetType().Name;
        string? message;
        try
        {
            message = ex.Message;
        }
        catch
        {
            message = null;
        }

        var normalizedMessage = NormalizeExceptionMessage(message);
        return string.IsNullOrEmpty(normalizedMessage)
            ? typeName
            : $"{typeName}: {normalizedMessage}";
    }
}
