using System.Collections.Concurrent;
using Praxis.Core.Models;

namespace Praxis.Services;

public sealed class DbErrorLogger : IErrorLogger
{
    private const int RetentionDays = 30;
    private const int FlushPollIntervalMs = 10;

    /// <summary>
    /// Maximum depth of inner-exception recursion when serializing a chain.
    /// Matches <see cref="CrashFileLogger.MaxExceptionChainDepth"/>.
    /// </summary>
    private const int MaxExceptionChainDepth = 32;

    /// <summary>
    /// Total number of exception nodes serialized per log call. Matches
    /// <see cref="CrashFileLogger.MaxExceptionNodes"/> and applies to
    /// type-chain, message, and stack-trace builders so a wide
    /// <see cref="AggregateException"/> cannot explode DB payload size or
    /// stall the logging path.
    /// </summary>
    private const int MaxExceptionNodes = 256;

    private sealed class Budget
    {
        public int RemainingNodes = MaxExceptionNodes;
        public readonly HashSet<Exception> Visited = new(ReferenceEqualityComparer.Instance);
    }

    private readonly IAppRepository repository;
    private readonly ConcurrentQueue<ErrorLogEntry> pendingWrites = new();
    private volatile int drainRunning;
    private int activeWrites;

    public DbErrorLogger(IAppRepository repository)
    {
        this.repository = repository;
    }

    public void Log(Exception exception, string context)
    {
        if (exception is null)
        {
            CrashFileLogger.WriteException($"ERROR [{context}]", null);

            var nullEntry = new ErrorLogEntry
            {
                Level = "Error",
                Context = context,
                ExceptionType = string.Empty,
                Message = "(no exception payload)",
                StackTrace = string.Empty,
                TimestampUtc = DateTime.UtcNow,
            };

            pendingWrites.Enqueue(nullEntry);
            _ = DrainQueueAsync();
            return;
        }

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
            while ((!pendingWrites.IsEmpty || Volatile.Read(ref activeWrites) > 0) &&
                   !cts.Token.IsCancellationRequested)
            {
                if (pendingWrites.TryDequeue(out var entry))
                {
                    await WriteToDatabaseAsync(entry, cts.Token);
                    continue;
                }

                await Task.Delay(FlushPollIntervalMs, cts.Token);
            }
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            CrashFileLogger.WriteWarning(
                nameof(DbErrorLogger),
                $"Flush timed out after {timeout.TotalMilliseconds:0} ms with {pendingWrites.Count} queued and {Volatile.Read(ref activeWrites)} active log writes.");
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteException(nameof(DbErrorLogger), ex);
            CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $"Flush failed unexpectedly: {ex.Message}");
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
                await WriteToDatabaseAsync(entry, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteException(nameof(DbErrorLogger), ex);
            CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $"Drain loop failed unexpectedly: {ex.Message}");
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

    private async Task WriteToDatabaseAsync(ErrorLogEntry entry, CancellationToken cancellationToken)
    {
        var entryWritten = false;
        Interlocked.Increment(ref activeWrites);

        try
        {
            await repository.AddErrorLogAsync(entry, cancellationToken);
            entryWritten = true;

            // Purge only on Error-level writes to avoid excessive cleanup.
            if (entry.Level == "Error")
            {
                try
                {
                    await repository.PurgeOldErrorLogsAsync(RetentionDays, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    CrashFileLogger.WriteException(nameof(DbErrorLogger), ex);
                    CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $"Failed to purge old error logs after persisting '{entry.Context}': {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (!entryWritten)
            {
                pendingWrites.Enqueue(entry);
            }
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteException(nameof(DbErrorLogger), ex);
            CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $"Failed to persist {entry.Level} log for '{entry.Context}': {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref activeWrites);
        }
    }

    /// <summary>
    /// Builds a string showing the full type chain, e.g.
    /// "InvalidOperationException -> NullReferenceException".
    /// </summary>
    private static string BuildExceptionTypeChain(Exception ex)
    {
        var parts = new List<string>();
        AppendExceptionTypes(parts, ex, depth: 0, new Budget());

        return string.Join(" -> ", parts);
    }

    /// <summary>
    /// Concatenates messages from the full exception chain.
    /// </summary>
    private static string BuildFullMessage(Exception ex)
    {
        var parts = new List<string>();
        AppendExceptionMessages(parts, ex, prefix: string.Empty, depth: 0, new Budget());

        return string.Join(" -> ", parts);
    }

    /// <summary>
    /// Builds the full stack trace including inner exceptions, bounded to
    /// <see cref="MaxExceptionChainDepth"/> and cycle-safe. Uses an iterative
    /// traversal so pathological graphs cannot stack-overflow the logger itself,
    /// and <see cref="Exception.StackTrace"/> per frame instead of
    /// <see cref="Exception.ToString"/> which recurses into inner exceptions.
    /// </summary>
    private static string BuildFullStackTrace(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        // Populate `visited` at enqueue-time so sibling duplicates within the same
        // AggregateException's InnerExceptions can be recognised before they are
        // pushed onto the traversal stack (at pop-time it would be too late — the
        // shared child would already have been queued for output twice).
        var visited = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<(Exception Exception, int Depth)>();
        visited.Add(ex);
        stack.Push((ex, 0));
        var remainingNodes = MaxExceptionNodes;

        while (stack.Count > 0)
        {
            var (current, depth) = stack.Pop();

            if (remainingNodes <= 0)
            {
                sb.AppendLine($"...(graph truncated after {MaxExceptionNodes} total nodes, {stack.Count + 1} remaining)");
                break;
            }

            if (depth >= MaxExceptionChainDepth)
            {
                sb.AppendLine($"...(truncated at depth {depth})");
                continue;
            }

            remainingNodes--;

            sb.Append(current.GetType().FullName ?? current.GetType().Name);
            var currentMsg = SafeMessage(current);
            if (!string.IsNullOrEmpty(currentMsg))
            {
                sb.Append(": ").Append(currentMsg);
            }

            sb.AppendLine();
            if (!string.IsNullOrEmpty(current.StackTrace))
            {
                sb.AppendLine(current.StackTrace);
            }

            if (current is AggregateException agg)
            {
                var toEnqueue = new List<Exception>();
                var duplicateCount = 0;
                var truncated = false;
                for (var i = 0; i < agg.InnerExceptions.Count; i++)
                {
                    var child = agg.InnerExceptions[i];
                    if (!visited.Add(child))
                    {
                        duplicateCount++;
                        continue;
                    }

                    if (toEnqueue.Count >= remainingNodes)
                    {
                        // Revert: we can't process this one but it isn't a duplicate either.
                        visited.Remove(child);
                        sb.AppendLine($"...({agg.InnerExceptions.Count - i} aggregate child(ren) not enqueued: node budget reached)");
                        truncated = true;
                        break;
                    }

                    toEnqueue.Add(child);
                }

                if (duplicateCount > 0)
                {
                    sb.AppendLine(truncated
                        ? $"...({duplicateCount} duplicate child reference(s) also skipped)"
                        : $"...({duplicateCount} duplicate aggregate child reference(s) skipped)");
                }

                // Push in reverse so natural order is preserved in output.
                for (var i = toEnqueue.Count - 1; i >= 0; i--)
                {
                    stack.Push((toEnqueue[i], depth + 1));
                }
            }
            else if (current.InnerException is not null)
            {
                if (visited.Add(current.InnerException))
                {
                    stack.Push((current.InnerException, depth + 1));
                }
                else
                {
                    sb.AppendLine($"...(cycle detected at {current.InnerException.GetType().FullName ?? current.InnerException.GetType().Name})");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Wraps <see cref="AggregateException.Message"/> which expands linearly over
    /// every inner exception — so a wide aggregate would allocate megabytes on a
    /// single property read. Inner messages are still captured by the bounded
    /// child traversal that follows.
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

    private static void AppendExceptionTypes(List<string> parts, Exception ex, int depth, Budget budget)
    {
        if (depth >= MaxExceptionChainDepth)
        {
            parts.Add($"...(truncated at depth {depth})");
            return;
        }

        if (budget.RemainingNodes <= 0)
        {
            parts.Add($"...(node budget {MaxExceptionNodes} exhausted)");
            return;
        }

        if (!budget.Visited.Add(ex))
        {
            parts.Add($"...(shared {ex.GetType().FullName ?? ex.GetType().Name})");
            return;
        }

        budget.RemainingNodes--;
        parts.Add(ex.GetType().FullName ?? ex.GetType().Name);

        if (ex is AggregateException agg)
        {
            // Scan every child position so later distinct exceptions after duplicates are
            // still serialized while node budget remains. Duplicates are O(1) each and
            // collapsed into one summary marker per aggregate.
            var duplicateCount = 0;
            for (var i = 0; i < agg.InnerExceptions.Count; i++)
            {
                var child = agg.InnerExceptions[i];
                if (budget.Visited.Contains(child))
                {
                    duplicateCount++;
                    continue;
                }

                if (budget.RemainingNodes <= 0)
                {
                    parts.Add($"...({agg.InnerExceptions.Count - i} aggregate child(ren) omitted after node budget)");
                    if (duplicateCount > 0)
                    {
                        parts.Add($"...({duplicateCount} duplicate child reference(s) also skipped)");
                    }

                    return;
                }

                AppendExceptionTypes(parts, child, depth + 1, budget);
            }

            if (duplicateCount > 0)
            {
                parts.Add($"...({duplicateCount} duplicate aggregate child reference(s) skipped)");
            }

            return;
        }

        if (ex.InnerException is not null)
        {
            AppendExceptionTypes(parts, ex.InnerException, depth + 1, budget);
        }
    }

    private static void AppendExceptionMessages(List<string> parts, Exception ex, string prefix, int depth, Budget budget)
    {
        if (depth >= MaxExceptionChainDepth)
        {
            parts.Add($"...(truncated at depth {depth})");
            return;
        }

        if (budget.RemainingNodes <= 0)
        {
            parts.Add($"{prefix}...(node budget {MaxExceptionNodes} exhausted)");
            return;
        }

        if (!budget.Visited.Add(ex))
        {
            parts.Add($"{prefix}...(shared reference)");
            return;
        }

        budget.RemainingNodes--;
        var msg = SafeMessage(ex);
        parts.Add(string.IsNullOrEmpty(prefix) ? msg : $"{prefix}{msg}");

        if (ex is AggregateException agg)
        {
            var duplicateCount = 0;
            for (var i = 0; i < agg.InnerExceptions.Count; i++)
            {
                var child = agg.InnerExceptions[i];
                if (budget.Visited.Contains(child))
                {
                    duplicateCount++;
                    continue;
                }

                if (budget.RemainingNodes <= 0)
                {
                    parts.Add($"...({agg.InnerExceptions.Count - i} aggregate child(ren) omitted after node budget)");
                    if (duplicateCount > 0)
                    {
                        parts.Add($"...({duplicateCount} duplicate child reference(s) also skipped)");
                    }

                    return;
                }

                AppendExceptionMessages(parts, child, $"[{i}] ", depth + 1, budget);
            }

            if (duplicateCount > 0)
            {
                parts.Add($"...({duplicateCount} duplicate aggregate child reference(s) skipped)");
            }

            return;
        }

        if (ex.InnerException is not null)
        {
            AppendExceptionMessages(parts, ex.InnerException, prefix: string.Empty, depth + 1, budget);
        }
    }
}
