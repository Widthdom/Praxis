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
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {source}");

            if (exception is null)
            {
                sb.AppendLine("  (no exception payload)");
            }
            else
            {
                AppendExceptionChain(sb, exception, depth: 0, new TraversalBudget());
            }

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
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] INFO {source}");
            sb.AppendLine($"  {message}");
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
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] WARN {source}");
            sb.AppendLine($"  {message}");
            sb.AppendLine(new string('-', 80));
            WriteToDisk(sb.ToString());
        }
        catch
        {
            // Must never throw.
        }
    }

    /// <summary>
    /// Returns a message suitable for logging without triggering
    /// <see cref="AggregateException.Message"/>'s linear expansion over every
    /// inner exception (which would blow up on wide aggregates). The inner
    /// messages are still captured below through the bounded child traversal.
    /// </summary>
    private static string SafeMessage(Exception ex)
    {
        if (ex is AggregateException agg)
        {
            var baseField = typeof(Exception).GetField("_message",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var baseMessage = baseField?.GetValue(agg) as string ?? string.Empty;
            return $"AggregateException ({agg.InnerExceptions.Count} inner): {baseMessage}";
        }

        return ex.Message;
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

        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            sb.AppendLine($"{indent}StackTrace:");
            foreach (var line in ex.StackTrace.Split('\n'))
            {
                sb.AppendLine($"{indent}  {line.TrimEnd()}");
            }
        }

        if (ex.Data.Count > 0)
        {
            sb.AppendLine($"{indent}Data:");
            foreach (var key in ex.Data.Keys)
            {
                sb.AppendLine($"{indent}  {key} = {ex.Data[key]}");
            }
        }

        if (ex is AggregateException agg)
        {
            // Hard-cap the number of child edges we iterate, not just the number of
            // distinct nodes we visit. Otherwise an aggregate built from N repeated
            // references (e.g. Enumerable.Repeat(sharedEx, 50_000)) would visit-cache
            // on every duplicate and still iterate N times emitting a shared-reference
            // marker per edge, stalling the synchronous crash-log path and ballooning
            // crash.log by ~N lines.
            var maxEdges = Math.Min(agg.InnerExceptions.Count, Math.Max(0, budget.RemainingNodes));
            for (var i = 0; i < maxEdges; i++)
            {
                if (budget.RemainingNodes <= 0)
                {
                    sb.AppendLine($"{indent}--- AggregateException truncated: {agg.InnerExceptions.Count - i} more child(ren) omitted after reaching node budget ---");
                    return;
                }

                sb.AppendLine($"{indent}--- AggregateException[{i}] ---");
                AppendExceptionChain(sb, agg.InnerExceptions[i], depth + 1, budget);
            }

            if (maxEdges < agg.InnerExceptions.Count)
            {
                sb.AppendLine($"{indent}--- AggregateException truncated: {agg.InnerExceptions.Count - maxEdges} more child edge(s) not iterated (node budget reached) ---");
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
}
