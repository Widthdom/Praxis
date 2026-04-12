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

    /// <summary>
    /// Per-<see cref="AggregateException"/> child-edge scan cap, independent of the
    /// node budget. Matches <see cref="CrashFileLogger.MaxAggregateChildEdgeScan"/>.
    /// Bounds synchronous work at O(budget) even when an aggregate contains
    /// millions of repeated references.
    /// </summary>
    private const int MaxAggregateChildEdgeScan = 4096;

    /// <summary>
    /// Suffix sample size. Matches <see cref="CrashFileLogger.MaxAggregateChildTailSample"/>.
    /// Ensures a far-tail distinct exception is still persisted when the prefix
    /// scan fills up with duplicate noise, without growing synchronous work
    /// beyond the combined prefix + tail budget.
    /// </summary>
    private const int MaxAggregateChildTailSample = 128;

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
        // Label is an optional "[i]" / "[i] (tail sample)" marker prefixed to the
        // child's header so operators can correlate a stack with its slot inside
        // the parent AggregateException — matching CrashFileLogger's shape.
        var stack = new Stack<(Exception Exception, int Depth, string? Label)>();
        visited.Add(ex);
        stack.Push((ex, 0, null));
        var remainingNodes = MaxExceptionNodes;

        while (stack.Count > 0)
        {
            var (current, depth, label) = stack.Pop();

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

            if (!string.IsNullOrEmpty(label))
            {
                sb.Append(label);
            }

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
                if (depth + 1 >= MaxExceptionChainDepth)
                {
                    sb.AppendLine($"...({agg.InnerExceptions.Count} child(ren) not enqueued: would exceed depth cap {MaxExceptionChainDepth})");
                    continue;
                }

                var toEnqueue = new List<(Exception Child, string Label)>();
                var duplicateCount = 0;
                var truncated = false;
                var count = agg.InnerExceptions.Count;
                var scanPrefix = Math.Min(count, MaxAggregateChildEdgeScan);
                var tailStart = Math.Max(scanPrefix, count - MaxAggregateChildTailSample);
                var middleSkipped = tailStart - scanPrefix;

                for (var i = 0; i < scanPrefix; i++)
                {
                    var child = agg.InnerExceptions[i];
                    if (!visited.Add(child))
                    {
                        duplicateCount++;
                        continue;
                    }

                    if (toEnqueue.Count >= remainingNodes)
                    {
                        visited.Remove(child);
                        sb.AppendLine($"...({count - i} aggregate child(ren) not enqueued: node budget reached)");
                        truncated = true;
                        break;
                    }

                    toEnqueue.Add((child, $"[{i}] "));
                }

                if (!truncated && middleSkipped > 0)
                {
                    sb.AppendLine($"...({middleSkipped} middle child(ren) not scanned; sampling last {count - tailStart})");
                    for (var i = tailStart; i < count; i++)
                    {
                        var child = agg.InnerExceptions[i];
                        if (!visited.Add(child))
                        {
                            duplicateCount++;
                            continue;
                        }

                        if (toEnqueue.Count >= remainingNodes)
                        {
                            visited.Remove(child);
                            sb.AppendLine($"...({count - i} tail child(ren) not enqueued: node budget reached)");
                            truncated = true;
                            break;
                        }

                        toEnqueue.Add((child, $"[{i}] (tail sample) "));
                    }
                }

                if (duplicateCount > 0)
                {
                    sb.AppendLine(truncated
                        ? $"...({duplicateCount} duplicate child reference(s) also skipped)"
                        : $"...({duplicateCount} duplicate aggregate child reference(s) skipped)");
                }

                for (var i = toEnqueue.Count - 1; i >= 0; i--)
                {
                    stack.Push((toEnqueue[i].Child, depth + 1, toEnqueue[i].Label));
                }
            }
            else if (current.InnerException is not null)
            {
                if (visited.Add(current.InnerException))
                {
                    stack.Push((current.InnerException, depth + 1, "--> "));
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
            if (agg.InnerExceptions.Count <= CrashFileLogger.SmallAggregateMessageThreshold &&
                !HasNestedAggregate(agg))
            {
                return agg.Message;
            }

            return $"AggregateException ({agg.InnerExceptions.Count} inner exceptions; top-level summary omitted — wide/nested aggregate)";
        }

        return ex.Message;
    }

    private static bool HasNestedAggregate(AggregateException agg)
    {
        for (var i = 0; i < agg.InnerExceptions.Count; i++)
        {
            if (agg.InnerExceptions[i] is AggregateException)
            {
                return true;
            }
        }

        return false;
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
            // Short-circuit: if child recursion would immediately hit the depth cap,
            // skip sibling scanning entirely so a deep aggregate cannot emit one
            // "...(truncated at depth N)" segment per child up to the edge-scan cap.
            if (depth + 1 >= MaxExceptionChainDepth)
            {
                parts.Add($"...({agg.InnerExceptions.Count} child(ren) not serialized: would exceed depth cap {MaxExceptionChainDepth})");
                return;
            }

            var duplicateCount = 0;
            var count = agg.InnerExceptions.Count;
            var scanPrefix = Math.Min(count, MaxAggregateChildEdgeScan);
            var tailStart = Math.Max(scanPrefix, count - MaxAggregateChildTailSample);
            var middleSkipped = tailStart - scanPrefix;

            for (var i = 0; i < scanPrefix; i++)
            {
                var child = agg.InnerExceptions[i];
                if (budget.Visited.Contains(child))
                {
                    duplicateCount++;
                    continue;
                }

                if (budget.RemainingNodes <= 0)
                {
                    parts.Add($"...({count - i} aggregate child(ren) omitted after node budget)");
                    if (duplicateCount > 0)
                    {
                        parts.Add($"...({duplicateCount} duplicate child reference(s) also skipped)");
                    }

                    return;
                }

                AppendExceptionTypes(parts, child, depth + 1, budget);
            }

            if (middleSkipped > 0)
            {
                parts.Add($"...({middleSkipped} middle child(ren) not scanned; sampling last {count - tailStart})");
                for (var i = tailStart; i < count; i++)
                {
                    var child = agg.InnerExceptions[i];
                    if (budget.Visited.Contains(child))
                    {
                        duplicateCount++;
                        continue;
                    }

                    if (budget.RemainingNodes <= 0)
                    {
                        parts.Add($"...({count - i} tail child(ren) omitted after node budget)");
                        if (duplicateCount > 0)
                        {
                            parts.Add($"...({duplicateCount} duplicate child reference(s) also skipped)");
                        }

                        return;
                    }

                    AppendExceptionTypes(parts, child, depth + 1, budget);
                }
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
            if (depth + 1 >= MaxExceptionChainDepth)
            {
                parts.Add($"...({agg.InnerExceptions.Count} child(ren) not serialized: would exceed depth cap {MaxExceptionChainDepth})");
                return;
            }

            var duplicateCount = 0;
            var count = agg.InnerExceptions.Count;
            var scanPrefix = Math.Min(count, MaxAggregateChildEdgeScan);
            var tailStart = Math.Max(scanPrefix, count - MaxAggregateChildTailSample);
            var middleSkipped = tailStart - scanPrefix;

            for (var i = 0; i < scanPrefix; i++)
            {
                var child = agg.InnerExceptions[i];
                if (budget.Visited.Contains(child))
                {
                    duplicateCount++;
                    continue;
                }

                if (budget.RemainingNodes <= 0)
                {
                    parts.Add($"...({count - i} aggregate child(ren) omitted after node budget)");
                    if (duplicateCount > 0)
                    {
                        parts.Add($"...({duplicateCount} duplicate child reference(s) also skipped)");
                    }

                    return;
                }

                AppendExceptionMessages(parts, child, $"[{i}] ", depth + 1, budget);
            }

            if (middleSkipped > 0)
            {
                parts.Add($"...({middleSkipped} middle child(ren) not scanned; sampling last {count - tailStart})");
                for (var i = tailStart; i < count; i++)
                {
                    var child = agg.InnerExceptions[i];
                    if (budget.Visited.Contains(child))
                    {
                        duplicateCount++;
                        continue;
                    }

                    if (budget.RemainingNodes <= 0)
                    {
                        parts.Add($"...({count - i} tail child(ren) omitted after node budget)");
                        if (duplicateCount > 0)
                        {
                            parts.Add($"...({duplicateCount} duplicate child reference(s) also skipped)");
                        }

                        return;
                    }

                    AppendExceptionMessages(parts, child, $"[{i}] ", depth + 1, budget);
                }
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
