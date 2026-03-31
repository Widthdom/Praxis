using System.Collections.Concurrent;
using Praxis.Core.Models;

namespace Praxis.Services;

public sealed class DbErrorLogger : IErrorLogger
{
    private const int RetentionDays = 30;

    private readonly IAppRepository repository;
    private readonly ConcurrentQueue<ErrorLogEntry> pendingWrites = new();
    private volatile int drainRunning;

    public DbErrorLogger(IAppRepository repository)
    {
        this.repository = repository;
    }

    public void Log(Exception exception, string context)
    {
        // 1. Write to crash file synchronously — survives abrupt termination.
        CrashFileLogger.WriteException($"ERROR [{context}]", exception);

        // 2. Build a rich entry that captures the full exception chain.
        var entry = new ErrorLogEntry
        {
            Level = "Error",
            Context = context,
            ExceptionType = BuildExceptionTypeChain(exception),
            Message = BuildFullMessage(exception),
            StackTrace = BuildFullStackTrace(exception),
            TimestampUtc = DateTime.UtcNow,
        };

        // 3. Enqueue for async DB write (non-blocking).
        pendingWrites.Enqueue(entry);
        _ = DrainQueueAsync();
    }

    public void LogWarning(string message, string context)
    {
        CrashFileLogger.WriteWarning(context, message);

        var entry = new ErrorLogEntry
        {
            Level = "Warning",
            Context = context,
            ExceptionType = string.Empty,
            Message = message,
            StackTrace = string.Empty,
            TimestampUtc = DateTime.UtcNow,
        };

        pendingWrites.Enqueue(entry);
        _ = DrainQueueAsync();
    }

    public void LogInfo(string message, string context)
    {
        CrashFileLogger.WriteInfo(context, message);

        var entry = new ErrorLogEntry
        {
            Level = "Info",
            Context = context,
            ExceptionType = string.Empty,
            Message = message,
            StackTrace = string.Empty,
            TimestampUtc = DateTime.UtcNow,
        };

        pendingWrites.Enqueue(entry);
        _ = DrainQueueAsync();
    }

    public async Task FlushAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            while (!pendingWrites.IsEmpty && !cts.Token.IsCancellationRequested)
            {
                if (!pendingWrites.TryDequeue(out var entry))
                {
                    break;
                }

                await WriteToDatabaseAsync(entry);
            }
        }
        catch
        {
            // Best-effort flush — don't crash the shutdown path.
        }
    }

    private async Task DrainQueueAsync()
    {
        // Ensure only one drain loop runs at a time.
        if (Interlocked.CompareExchange(ref drainRunning, 1, 0) != 0)
        {
            return;
        }

        try
        {
            while (pendingWrites.TryDequeue(out var entry))
            {
                await WriteToDatabaseAsync(entry);
            }
        }
        catch
        {
            // Swallow write failures to avoid infinite logging loops.
            // The crash file already has the data.
        }
        finally
        {
            Interlocked.Exchange(ref drainRunning, 0);

            // Re-check: items may have been enqueued while we were exiting.
            if (!pendingWrites.IsEmpty)
            {
                _ = DrainQueueAsync();
            }
        }
    }

    private async Task WriteToDatabaseAsync(ErrorLogEntry entry)
    {
        try
        {
            await repository.AddErrorLogAsync(entry);

            // Purge only on Error-level writes to avoid excessive cleanup.
            if (entry.Level == "Error")
            {
                await repository.PurgeOldErrorLogsAsync(RetentionDays);
            }
        }
        catch
        {
            // Swallow — the crash file already has the record.
        }
    }

    /// <summary>
    /// Builds a string showing the full type chain, e.g.
    /// "InvalidOperationException -> NullReferenceException".
    /// </summary>
    private static string BuildExceptionTypeChain(Exception ex)
    {
        var parts = new List<string>();

        if (ex is AggregateException agg)
        {
            parts.Add(nameof(AggregateException));
            foreach (var inner in agg.InnerExceptions)
            {
                parts.Add(inner.GetType().FullName ?? inner.GetType().Name);
            }
        }
        else
        {
            var current = ex;
            while (current is not null)
            {
                parts.Add(current.GetType().FullName ?? current.GetType().Name);
                current = current.InnerException;
            }
        }

        return string.Join(" -> ", parts);
    }

    /// <summary>
    /// Concatenates messages from the full exception chain.
    /// </summary>
    private static string BuildFullMessage(Exception ex)
    {
        if (ex is AggregateException agg)
        {
            var messages = new List<string> { ex.Message };
            for (var i = 0; i < agg.InnerExceptions.Count; i++)
            {
                messages.Add($"[{i}] {agg.InnerExceptions[i].Message}");
            }

            return string.Join(" | ", messages);
        }

        var parts = new List<string>();
        var current = ex;
        while (current is not null)
        {
            parts.Add(current.Message);
            current = current.InnerException;
        }

        return string.Join(" -> ", parts);
    }

    /// <summary>
    /// Builds the full stack trace including inner exceptions.
    /// </summary>
    private static string BuildFullStackTrace(Exception ex)
    {
        // ex.ToString() already includes the full chain with stack traces.
        return ex.ToString();
    }
}
