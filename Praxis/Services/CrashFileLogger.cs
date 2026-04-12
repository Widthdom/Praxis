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
                var visited = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
                AppendExceptionChain(sb, exception, depth: 0, visited);
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

    private static void AppendExceptionChain(StringBuilder sb, Exception ex, int depth, HashSet<Exception> visited)
    {
        var indent = new string(' ', (depth + 1) * 2);

        if (depth >= MaxExceptionChainDepth)
        {
            sb.AppendLine($"{indent}--- Exception chain truncated at depth {depth} (max {MaxExceptionChainDepth}) ---");
            return;
        }

        if (!visited.Add(ex))
        {
            sb.AppendLine($"{indent}--- Already serialized: {ex.GetType().FullName ?? ex.GetType().Name} (shared/cyclic reference) ---");
            return;
        }

        if (depth > 0)
        {
            sb.AppendLine($"{indent}--- Inner Exception (depth {depth}) ---");
        }

        sb.AppendLine($"{indent}Type: {ex.GetType().FullName ?? ex.GetType().Name}");
        sb.AppendLine($"{indent}Message: {ex.Message}");

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
            for (var i = 0; i < agg.InnerExceptions.Count; i++)
            {
                sb.AppendLine($"{indent}--- AggregateException[{i}] ---");
                AppendExceptionChain(sb, agg.InnerExceptions[i], depth + 1, visited);
            }
        }
        else if (ex.InnerException is not null)
        {
            AppendExceptionChain(sb, ex.InnerException, depth + 1, visited);
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
