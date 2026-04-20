using Praxis.Core.Models;
using Praxis.Services;

namespace Praxis.Tests;

public class DbErrorLoggerTests
{
    [Fact]
    public void Log_DoesNotThrow()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var ex = Record.Exception(() => logger.Log(new InvalidOperationException("test"), "context"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task Log_DoesNotThrow_WhenExceptionPayloadIsNull()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var ex = Record.Exception(() => logger.Log(null!, "null-context"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Error", entry.Level);
        Assert.Equal("null-context", entry.Context);
        Assert.Equal("(no exception payload)", entry.Message);
        Assert.Equal(string.Empty, entry.ExceptionType);
        Assert.Equal(string.Empty, entry.StackTrace);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("ERROR [null-context]", content);
        Assert.Contains("(no exception payload)", content);
    }

    [Fact]
    public async Task Log_BlankContext_UsesPlaceholderInFileAndDatabase()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);
        var marker = $"blank-context-{Guid.NewGuid():N}";

        logger.Log(new InvalidOperationException(marker), " \r\n ");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Error", entry.Level);
        Assert.Equal(CrashFileLogger.MissingContextPlaceholder, entry.Context);
        Assert.Contains(marker, entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"ERROR [{CrashFileLogger.MissingContextPlaceholder}]", content);
        Assert.Contains(marker, content);
    }

    [Fact]
    public void LogInfo_DoesNotThrow()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var ex = Record.Exception(() => logger.LogInfo("info message", "context"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LogInfo_NullMessage_UsesPlaceholderInFileAndDatabase()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"info-null-{Guid.NewGuid():N}";

        logger.LogInfo(null!, context);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Info", entry.Level);
        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(CrashFileLogger.MissingMessagePayloadPlaceholder, content);
        Assert.Contains(context, content);
    }

    [Fact]
    public async Task LogInfo_MultilineMessage_IsCollapsedToSingleLine()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);
        var first = $"info-a-{Guid.NewGuid():N}";
        var second = $"info-b-{Guid.NewGuid():N}";

        logger.LogInfo($"{first}\r\n{second}", "info-multiline");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Info", entry.Level);
        Assert.Equal($"{first} {second}", entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"{first} {second}", content);
    }

    [Fact]
    public void LogWarning_DoesNotThrow()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var ex = Record.Exception(() => logger.LogWarning("warn message", "context"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LogWarning_NullMessage_UsesPlaceholderInFileAndDatabase()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"warn-null-{Guid.NewGuid():N}";

        logger.LogWarning(null!, context);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Warning", entry.Level);
        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(CrashFileLogger.MissingMessagePayloadPlaceholder, content);
        Assert.Contains(context, content);
    }

    [Fact]
    public async Task LogWarning_BlankMessage_AndMultilineContext_AreNormalizedInFileAndDatabase()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"warn-{Guid.NewGuid():N}";

        logger.LogWarning(" \r\n ", $"  {context}\r\nchild  ");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Warning", entry.Level);
        Assert.Equal($"{context} child", entry.Context);
        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"WARN {context} child", content);
        Assert.Contains(CrashFileLogger.MissingMessagePayloadPlaceholder, content);
    }

    [Fact]
    public async Task FlushAsync_WritesPendingEntriesToRepository()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.LogInfo("flush test", "context");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        Assert.Contains(repo.ErrorLogs, e => e.Message == "flush test" && e.Level == "Info");
    }

    [Fact]
    public async Task FlushAsync_WritesErrorEntriesToRepository()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.Log(new InvalidOperationException("error test"), "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        Assert.Contains(repo.ErrorLogs, e => e.Level == "Error" && e.Message.Contains("error test"));
    }

    [Fact]
    public async Task Log_DoesNotStackOverflow_OnDeepInnerExceptionChain()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Chain depth well beyond the internal cap so truncation must kick in.
        Exception current = new InvalidOperationException("leaf");
        for (var i = 0; i < 200; i++)
        {
            current = new InvalidOperationException($"wrap-{i}", current);
        }

        var ex = Record.Exception(() => logger.Log(current, "deep-chain"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Error", entry.Level);
        Assert.Contains("truncated at depth", entry.ExceptionType);
        Assert.Contains("truncated at depth", entry.Message);
        Assert.Contains("truncated at depth", entry.StackTrace);
    }

    [Fact]
    public async Task Log_DoesNotStackOverflow_OnCyclicInnerExceptionGraph()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Construct a self-referential inner-exception cycle via reflection.
        var a = new InvalidOperationException("a");
        var b = new InvalidOperationException("b", a);
        var innerField = typeof(Exception).GetField("_innerException",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(innerField);
        innerField!.SetValue(a, b);

        var ex = Record.Exception(() => logger.Log(b, "cycle"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Error", entry.Level);
        Assert.Contains("cycle detected", entry.StackTrace);
    }

    [Fact]
    public async Task Log_MultilineExceptionMessages_AreCollapsedInDatabaseAndCrashFile()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);
        var first = $"outer-a-{Guid.NewGuid():N}";
        var second = $"outer-b-{Guid.NewGuid():N}";
        var third = $"inner-a-{Guid.NewGuid():N}";
        var fourth = $"inner-b-{Guid.NewGuid():N}";
        var exception = new InvalidOperationException($"{first}\r\n{second}",
            new ArgumentException($"{third}\n{fourth}"));

        logger.Log(exception, "multiline-message");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains($"{first} {second}", entry.Message);
        Assert.Contains($"{third} {fourth}", entry.Message);
        Assert.DoesNotContain($"{first}\n{second}", entry.Message, StringComparison.Ordinal);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Message: {first} {second}", content);
        Assert.Contains($"Message: {third} {fourth}", content);
    }

    [Fact]
    public async Task Log_WhenExceptionMessageGetterThrows_PersistsFailureMarker()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.Log(new ThrowingMessageException(), "message-getter");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("failed to read exception message: System.InvalidOperationException: message getter failure", entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("failed to read exception message: System.InvalidOperationException: message getter failure", content);
    }

    [Fact]
    public async Task Log_WhenExceptionMessageIsWhitespace_PersistsEmptyMarker()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.Log(new WhitespaceMessageException(), "message-whitespace");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("(empty)", entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("Message: (empty)", content);
    }

    [Fact]
    public async Task Log_WhenExceptionStackTraceGetterThrows_PersistsFailureMarker()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.Log(new ThrowingStackTraceException(), "stacktrace-getter");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("failed to read stack trace: System.InvalidOperationException: stack trace getter failure", entry.StackTrace);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("failed to read stack trace: System.InvalidOperationException: stack trace getter failure", content);
    }

    [Fact]
    public async Task Log_WideAggregateFanOut_IsBoundedByNodeBudget()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // 5000 distinct task failures under one aggregate. Without a node budget
        // this would synchronously serialize thousands of nodes on the crash path
        // and balloon the DB payload.
        var children = new Exception[5000];
        for (var i = 0; i < children.Length; i++)
        {
            children[i] = new InvalidOperationException($"child-{i}");
        }
        var agg = new AggregateException("wide", children);

        var ex = Record.Exception(() => logger.Log(agg, "wide-fanout"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Error", entry.Level);

        // Deterministic proof that the cap engaged: truncation markers present AND
        // each field's length is bounded by O(MaxExceptionNodes), not O(children).
        Assert.Contains("prefix capped at", entry.ExceptionType);
        Assert.Contains("prefix capped at", entry.Message);
        Assert.Contains("prefix capped at", entry.StackTrace);
        Assert.True(entry.ExceptionType.Length < 100_000, $"ExceptionType grew to {entry.ExceptionType.Length} chars — cap did not engage.");
        Assert.True(entry.Message.Length < 100_000, $"Message grew to {entry.Message.Length} chars — cap did not engage.");

        // crash.log should also show the per-call truncation marker, not every child.
        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("prefix capped at", content);
    }

    [Fact]
    public async Task Log_VeryWideAggregate_BoundsTraversalAllocationsNotJustOutput()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // 50k distinct failures under one aggregate. If BuildFullStackTrace enqueued all
        // children before applying the node budget, this would allocate ~50k traversal
        // entries synchronously. With pre-enqueue capping it allocates ≤ MaxExceptionNodes.
        var children = new Exception[50_000];
        for (var i = 0; i < children.Length; i++)
        {
            children[i] = new InvalidOperationException($"c{i}");
        }
        var agg = new AggregateException("storm", children);

        var ex = Record.Exception(() => logger.Log(agg, "failure-storm"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);

        // Deterministic proof of bounded traversal: StackTrace field length is O(budget),
        // not O(children). 50k children without the cap would produce multi-MB output.
        Assert.Contains("prefix capped at", entry.StackTrace);
        Assert.True(entry.StackTrace.Length < 200_000, $"StackTrace grew to {entry.StackTrace.Length} chars — traversal-stack growth not bounded.");
    }

    [Fact]
    public async Task Log_RepeatedReferenceAggregate_IteratesAtMostNodeBudgetEdges()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Same reference repeated 100_000 times. Without an edge cap, each builder
        // iterates 100k times emitting shared-reference markers per duplicate,
        // bloating ExceptionType/Message/StackTrace and stalling the caller thread.
        var shared = new InvalidOperationException("shared-leaf");
        var repeated = new Exception[100_000];
        for (var i = 0; i < repeated.Length; i++) repeated[i] = shared;
        var agg = new AggregateException("repeated-refs", repeated);

        var ex = Record.Exception(() => logger.Log(agg, "repeated-refs"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);

        // Deterministic proof that the edge cap engaged: truncation markers present AND
        // persisted field lengths are O(budget), not O(100k).
        // For a pure repeated-reference aggregate, duplicates don't drain the node budget,
        // so we expect a consolidated duplicate-summary marker rather than budget truncation.
        Assert.Contains("duplicate aggregate child reference(s) skipped", entry.ExceptionType);
        Assert.Contains("duplicate aggregate child reference(s) skipped", entry.Message);
        Assert.Contains("duplicate aggregate child reference(s) skipped", entry.StackTrace);
        Assert.True(entry.ExceptionType.Length < 100_000, $"ExceptionType grew to {entry.ExceptionType.Length} chars — duplicates not consolidated.");
        Assert.True(entry.Message.Length < 100_000, $"Message grew to {entry.Message.Length} chars — duplicates not consolidated.");
        Assert.True(entry.StackTrace.Length < 200_000, $"StackTrace grew to {entry.StackTrace.Length} chars — duplicates not consolidated.");

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("duplicate child reference(s) skipped", content);
    }

    [Fact]
    public async Task Log_SmallAggregate_PreservesTopLevelMessageViaPublicApi()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Under the small-aggregate threshold (8): .Message is cheap, so the caller-
        // supplied top-level summary must be preserved in persisted records without
        // any reflection on private framework fields.
        var agg = new AggregateException(
            "top-level-operator-hint",
            new InvalidOperationException("child-a"),
            new NullReferenceException("child-b"));

        logger.Log(agg, "small-agg");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("top-level-operator-hint", entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("top-level-operator-hint", content);
    }

    [Fact]
    public async Task Log_NestedAggregateWrapper_PreservesTopLevelSummary()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Common shape: an outer AggregateException wraps another small AggregateException
        // that wraps a leaf. The wrapper summary ("sync user-42") is often the only
        // operation-specific breadcrumb, so it must survive in both persisted stores.
        // Total graph size here is 3 nodes — well under the expansion cap.
        var leaf = new InvalidOperationException("leaf");
        var innerAgg = new AggregateException("child", leaf);
        var outerAgg = new AggregateException("sync user-42", innerAgg);

        logger.Log(outerAgg, "nested-wrapper");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("sync user-42", entry.Message);
        Assert.Contains("sync user-42", entry.StackTrace);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("sync user-42", content);
    }

    [Fact]
    public async Task Log_StackTrace_IncludesPerChildIndexLabels()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Real multi-child aggregate with distinct stacks. BuildFullStackTrace must
        // emit explicit [i] labels so an operator can correlate each stack with its
        // slot in the aggregate — matching CrashFileLogger's "AggregateException[i]"
        // shape.
        Exception ThrowAndCatch(string message)
        {
            try { throw new InvalidOperationException(message); }
            catch (Exception caught) { return caught; }
        }

        var agg = new AggregateException("multi-child",
            ThrowAndCatch("alpha"),
            ThrowAndCatch("bravo"),
            ThrowAndCatch("charlie"));

        logger.Log(agg, "stack-labels");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);

        // Each child's stack must be prefixed with its aggregate slot index.
        Assert.Contains("[0] System.InvalidOperationException: alpha", entry.StackTrace);
        Assert.Contains("[1] System.InvalidOperationException: bravo", entry.StackTrace);
        Assert.Contains("[2] System.InvalidOperationException: charlie", entry.StackTrace);
    }

    [Fact]
    public async Task Log_StackTrace_TailSampleCarriesTailSampleLabel()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Far-tail distinct exception should surface via tail sample AND carry the
        // "(tail sample)" label so operators know the stack came from the sampled
        // tail, not the prefix.
        var shared = new InvalidOperationException("noise");
        var rootCauseMarker = Guid.NewGuid().ToString("N");
        var rootCause = new NullReferenceException($"root-cause-{rootCauseMarker}");
        var children = new Exception[100_000];
        for (var i = 0; i < children.Length - 1; i++) children[i] = shared;
        children[^1] = rootCause;
        var agg = new AggregateException("storm", children);

        logger.Log(agg, "tail-label");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("(tail sample)", entry.StackTrace);
        Assert.Contains(rootCauseMarker, entry.StackTrace);
    }

    [Fact]
    public async Task Log_AllUniqueWideAggregate_PreservesActionableTailViaReservedBudget()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Adversarial: 100_000 *distinct* failures with the actionable one at the
        // final index. Without a reserved tail budget the prefix alone would
        // exhaust the 256-node budget long before the tail window runs, so the
        // root cause disappears from all persisted fields. With the reservation
        // the last-index exception must survive in DB fields and crash.log.
        var rootCauseMarker = Guid.NewGuid().ToString("N");
        var children = new Exception[100_000];
        for (var i = 0; i < children.Length - 1; i++)
        {
            children[i] = new InvalidOperationException($"u-{i}");
        }
        children[^1] = new NullReferenceException($"actionable-{rootCauseMarker}");
        var agg = new AggregateException("all-unique-storm", children);

        logger.Log(agg, "all-unique-tail");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("NullReferenceException", entry.ExceptionType);
        Assert.Contains(rootCauseMarker, entry.Message);
        Assert.Contains(rootCauseMarker, entry.StackTrace);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(rootCauseMarker, content);
    }

    [Fact]
    public async Task Log_NestedWideAggregates_KeepPendingQueueWithinBudget()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Nested wide: outer has 5 wide children, each of which is itself a wide
        // aggregate with 1000 children. Without stack.Count accounting in
        // BuildFullStackTrace, every outer child would queue up to MaxExceptionNodes
        // new frames on top of already-pending siblings, blowing past the cap.
        AggregateException MakeInner(int seed)
        {
            var inner = new Exception[1000];
            for (var i = 0; i < inner.Length; i++) inner[i] = new InvalidOperationException($"seed{seed}-i{i}");
            return new AggregateException($"inner-{seed}", inner);
        }

        var outer = new AggregateException("outer",
            MakeInner(1), MakeInner(2), MakeInner(3), MakeInner(4), MakeInner(5));

        logger.Log(outer, "nested-wide");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        // Bounded persisted stack-trace length — worst case without the bound was
        // ~5 × 1000 = 5000 child serialisations; with the bound output stays well
        // under that.
        Assert.True(entry.StackTrace.Length < 200_000, $"StackTrace grew to {entry.StackTrace.Length} chars — nested-wide bound missing.");
        Assert.True(entry.Message.Length < 200_000, $"Message grew to {entry.Message.Length} chars — nested-wide bound missing.");
    }

    [Fact]
    public async Task Log_DeepAggregateWithWideFanOut_StaysBoundedAtDepthCap()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Wrap a 4000-child aggregate at depth 31. Without the pre-enqueue depth
        // guard, each child would recurse to depth 32, return immediately via the
        // depth cap without consuming node budget, and let the sibling loop emit
        // one "...(truncated at depth 32)" marker per child — i.e. O(4000) markers
        // per builder. With the guard, the entire sibling scan is short-circuited
        // and exactly one "would exceed depth cap" marker is emitted per aggregate
        // frame that hits the depth limit.
        var wide = new Exception[4000];
        for (var i = 0; i < wide.Length; i++) wide[i] = new InvalidOperationException($"leaf-{i}");
        var deepWide = new AggregateException("deep-wide", wide);
        Exception current = deepWide;
        for (var i = 0; i < 31; i++)
        {
            current = new AggregateException($"level-{i}", current);
        }

        var ex = Record.Exception(() => logger.Log(current, "deep-wide"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);

        // Bounded field sizes: without the fix these would grow O(MaxAggregateChildEdgeScan).
        Assert.True(entry.ExceptionType.Length < 10_000, $"ExceptionType grew to {entry.ExceptionType.Length} chars — depth guard not engaged.");
        Assert.True(entry.Message.Length < 10_000, $"Message grew to {entry.Message.Length} chars — depth guard not engaged.");

        // Exactly one depth-cap marker per builder (no O(N) repetition).
        Assert.Contains("would exceed depth cap", entry.ExceptionType);
        Assert.Contains("would exceed depth cap", entry.Message);
        Assert.Contains("would exceed depth cap", entry.StackTrace);
        Assert.Equal(1, CountSubstring(entry.ExceptionType, "would exceed depth cap"));
        Assert.Equal(1, CountSubstring(entry.Message, "would exceed depth cap"));
        Assert.Equal(1, CountSubstring(entry.StackTrace, "would exceed depth cap"));
    }

    private static int CountSubstring(string haystack, string needle)
    {
        var count = 0;
        var i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }
        return count;
    }

    private static void AssertPersistFailureExceptionLogged(string content, string level, string context)
    {
        Assert.Contains($"Failed to persist {level} log for '{context}': Simulated DB failure", content);
        Assert.Contains("Type: System.Exception", content);
        Assert.Contains("Message: Simulated DB failure", content);
    }

    [Fact]
    public async Task Log_AggregateScanIsCapped_ButSamplesHeadAndTail()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // 100_000 children with a unique needle placed in the MIDDLE region —
        // outside both the prefix edge-scan cap (4096) and the tail sample
        // (128). The middle must NOT appear (proving synchronous work is
        // O(budget), not O(child count)), BUT the "middle not scanned" marker
        // must appear so operators know the graph was clipped. A regression that
        // removed the scan cap would iterate all 100k slots and make the middle
        // needle appear here.
        var shared = new InvalidOperationException("shared-leaf");
        var middleMarker = Guid.NewGuid().ToString("N");
        var middleNeedle = new NullReferenceException($"middle-needle-{middleMarker}");
        var children = new Exception[100_000];
        for (var i = 0; i < children.Length; i++) children[i] = shared;
        children[50_000] = middleNeedle;
        var agg = new AggregateException("scan-cap-guard", children);

        var ex = Record.Exception(() => logger.Log(agg, "scan-cap"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);

        Assert.DoesNotContain(middleMarker, entry.ExceptionType);
        Assert.DoesNotContain(middleMarker, entry.Message);
        Assert.DoesNotContain(middleMarker, entry.StackTrace);

        Assert.Contains("middle child(ren) not fully scanned", entry.ExceptionType);
        Assert.Contains("middle child(ren) not fully scanned", entry.Message);
        Assert.Contains("middle child(ren) not fully scanned", entry.StackTrace);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("middle child(ren) not fully scanned", content);
        Assert.DoesNotContain(middleMarker, content);
    }

    [Fact]
    public async Task Log_FarTailDistinctException_IsCapturedByTailSample()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // 100_000 children where the actionable root cause sits at the very end —
        // the realistic "failure storm ends with the real error" shape. The tail
        // sample window (128) MUST surface it in all three DB fields and in
        // crash.log even though position 99_999 is far beyond the prefix cap.
        var shared = new InvalidOperationException("noise");
        var rootCauseMarker = Guid.NewGuid().ToString("N");
        var rootCause = new NullReferenceException($"root-cause-{rootCauseMarker}");
        var children = new Exception[100_000];
        for (var i = 0; i < children.Length - 1; i++) children[i] = shared;
        children[^1] = rootCause;
        var agg = new AggregateException("failure-storm-with-root-cause", children);

        var ex = Record.Exception(() => logger.Log(agg, "tail-sample"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);

        // Tail sample must have surfaced the root cause.
        Assert.Contains("NullReferenceException", entry.ExceptionType);
        Assert.Contains(rootCauseMarker, entry.Message);
        Assert.Contains(rootCauseMarker, entry.StackTrace);

        // And the truncation marker must still document the scan policy.
        Assert.Contains("middle child(ren) not fully scanned", entry.ExceptionType);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(rootCauseMarker, content);
        Assert.Contains("sampling last", content);
    }

    [Fact]
    public async Task Log_MixedAggregate_StillLogsUniqueTailAfterDuplicates()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // [shared, shared, ..., shared, uniqueTail] — codex adversarial regression.
        // Duplicates must not exhaust the iteration budget before the unique tail is
        // reached, otherwise the persisted record hides the failure that explains the incident.
        var shared = new InvalidOperationException("shared-leaf");
        var uniqueTail = new NullReferenceException("unique-tail-that-explains-the-incident");
        // Tail position kept within the per-aggregate edge-scan cap (4096) so the
        // unique failure is preserved on the synchronous logging path. Scans beyond
        // the cap are covered by Log_AggregateScanIsCapped_IndependentOfChildCount.
        var mixed = new Exception[3000];
        for (var i = 0; i < mixed.Length - 1; i++) mixed[i] = shared;
        mixed[^1] = uniqueTail;
        var agg = new AggregateException("mixed-dup-then-unique", mixed);

        var ex = Record.Exception(() => logger.Log(agg, "mixed-agg"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);

        // Both the duplicate summary AND the unique tail must appear in the persisted record.
        Assert.Contains("NullReferenceException", entry.ExceptionType);
        Assert.Contains("unique-tail-that-explains-the-incident", entry.Message);
        Assert.Contains("NullReferenceException", entry.StackTrace);
        Assert.Contains("duplicate aggregate child reference(s) skipped", entry.ExceptionType);

        // crash.log must also contain the unique tail.
        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("unique-tail-that-explains-the-incident", content);
        Assert.Contains("NullReferenceException", content);
    }

    [Fact]
    public async Task Log_SharedAggregateSubtree_IsSerializedLinearlyInDistinctNodes()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // A shared inner subtree referenced from multiple parent slots would expand
        // exponentially without reference-equality tracking. Build a fan-out of depth 10
        // where each level re-uses the same shared child in two slots — 2^10 = 1024
        // serialized paths without tracking, 11 distinct nodes with it.
        var shared = new InvalidOperationException("leaf");
        Exception current = shared;
        for (var i = 0; i < 10; i++)
        {
            current = new AggregateException($"level-{i}", current, current);
        }

        var ex = Record.Exception(() => logger.Log(current, "shared-subtree"));
        Assert.Null(ex);

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Error", entry.Level);

        // Deterministic proof: with reference tracking the fan-out of depth 10 (1024 paths
        // untracked) collapses to a linear list of distinct nodes plus shared-reference
        // markers. ExceptionType serializes ≤ ~50 type-name entries; without tracking it
        // would serialize ~1024 entries separated by " -> ".
        Assert.Contains("duplicate aggregate child reference(s) skipped", entry.ExceptionType);
        Assert.Contains("duplicate aggregate child reference(s) skipped", entry.Message);
        Assert.Contains("duplicate aggregate child reference(s) skipped", entry.StackTrace);
        Assert.True(entry.ExceptionType.Split(" -> ").Length < 64, $"ExceptionType contains {entry.ExceptionType.Split(" -> ").Length} segments — suggests exponential expansion.");
    }

    [Fact]
    public async Task FlushAsync_WritesWarningEntriesToRepository()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.LogWarning("warn test", "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        Assert.Contains(repo.ErrorLogs, e => e.Level == "Warning" && e.Message == "warn test");
    }

    [Fact]
    public async Task FlushAsync_WaitsForInFlightWriteBeforeReturning()
    {
        var repo = new BlockingAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.LogInfo("blocked write", "ctx");
        await repo.AddStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var flushTask = logger.FlushAsync(TimeSpan.FromSeconds(2));

        await Task.Delay(50);
        Assert.False(flushTask.IsCompleted);

        repo.ReleaseWrite();
        await flushTask;

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Info", entry.Level);
        Assert.Equal("blocked write", entry.Message);
    }

    [Fact]
    public async Task FlushAsync_TimesOut_WhenInFlightWriteDoesNotFinish()
    {
        var repo = new BlockingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"timeout-{Guid.NewGuid():N}";

        logger.LogInfo("timeout write", context);
        await repo.AddStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var flushTask = logger.FlushAsync(TimeSpan.FromMilliseconds(100));

        await Task.Delay(30);
        Assert.False(flushTask.IsCompleted);

        await flushTask;
        Assert.Empty(repo.ErrorLogs);

        repo.ReleaseWrite();
        await logger.FlushAsync(TimeSpan.FromSeconds(2));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("timeout write", entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("Flush timed out after 100 ms", content);
        Assert.Contains("1 active log writes", content);
    }

    [Fact]
    public async Task Log_CapturesExceptionTypeChain()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var inner = new NullReferenceException("inner");
        var outer = new InvalidOperationException("outer", inner);
        logger.Log(outer, "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("InvalidOperationException", entry.ExceptionType);
        Assert.Contains("NullReferenceException", entry.ExceptionType);
        Assert.Contains("->", entry.ExceptionType);
    }

    [Fact]
    public async Task Log_CapturesFullMessageChain()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var inner = new NullReferenceException("inner msg");
        var outer = new InvalidOperationException("outer msg", inner);
        logger.Log(outer, "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("outer msg", entry.Message);
        Assert.Contains("inner msg", entry.Message);
    }

    [Fact]
    public async Task Log_CapturesFullStackTrace()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        Exception caught;
        try { throw new InvalidOperationException("stack test"); }
        catch (Exception ex) { caught = ex; }

        logger.Log(caught, "ctx");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("InvalidOperationException", entry.StackTrace);
        Assert.Contains("stack test", entry.StackTrace);
    }

    [Fact]
    public async Task Log_AggregateException_CapturesAllInnerTypes()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var agg = new AggregateException("agg",
            new InvalidOperationException("first"),
            new ArgumentException("second"));
        logger.Log(agg, "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("AggregateException", entry.ExceptionType);
        Assert.Contains("InvalidOperationException", entry.ExceptionType);
        Assert.Contains("ArgumentException", entry.ExceptionType);
    }

    [Fact]
    public async Task Log_AggregateException_CapturesNestedInnerTypeChain()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var nested = new InvalidOperationException("outer child", new NullReferenceException("nested child"));
        var agg = new AggregateException("agg", nested);
        logger.Log(agg, "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("AggregateException", entry.ExceptionType);
        Assert.Contains("InvalidOperationException", entry.ExceptionType);
        Assert.Contains("NullReferenceException", entry.ExceptionType);
    }

    [Fact]
    public async Task Log_AggregateException_CapturesNestedInnerMessages()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var nested = new InvalidOperationException("outer child", new NullReferenceException("nested child"));
        var agg = new AggregateException("agg", nested);
        logger.Log(agg, "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("agg", entry.Message);
        Assert.Contains("outer child", entry.Message);
        Assert.Contains("nested child", entry.Message);
    }

    [Fact]
    public async Task FlushAsync_PurgesOldErrorLogs_OnlyForErrorEntries()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.LogInfo("info", "ctx");
        logger.LogWarning("warn", "ctx");
        logger.Log(new InvalidOperationException("error"), "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, repo.PurgeCallCount);
    }

    [Fact]
    public async Task FlushAsync_DoesNotPurgeOldErrorLogs_ForInfoAndWarningOnly()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.LogInfo("info only", "info-ctx");
        logger.LogWarning("warn only", "warn-ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, repo.PurgeCallCount);
        Assert.Contains(repo.ErrorLogs, x => x.Level == "Info" && x.Context == "info-ctx" && x.ExceptionType == string.Empty && x.StackTrace == string.Empty);
        Assert.Contains(repo.ErrorLogs, x => x.Level == "Warning" && x.Context == "warn-ctx" && x.ExceptionType == string.Empty && x.StackTrace == string.Empty);
    }

    [Fact]
    public async Task FlushAsync_RespectsTimeout()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Should complete quickly even with a very short timeout when queue is empty.
        await logger.FlushAsync(TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Log_DoesNotThrow_WhenRepositoryFails()
    {
        var repo = new FailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"repo-fail-{Guid.NewGuid():N}";

        logger.Log(new Exception("repo fail test"), context);

        // Should not throw even though the repository fails.
        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Error log for '{context}': Simulated DB failure", content);
        Assert.Contains("Type: System.Exception", content);
    }

    [Fact]
    public async Task Log_NormalizesMultilineContext_InPersistFailureBreadcrumb()
    {
        var repo = new FailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"repo-fail-context-{Guid.NewGuid():N}";

        logger.Log(new Exception("repo fail test"), $"  {context}\r\nchild  ");

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Error log for '{context} child': Simulated DB failure", content);
    }

    [Fact]
    public async Task LogWarning_DoesNotThrow_WhenRepositoryFails()
    {
        var repo = new FailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"warn-repo-fail-{Guid.NewGuid():N}";
        var message = $"warn repo fail test {Guid.NewGuid():N}";

        logger.LogWarning(message, context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(message, content);
        AssertPersistFailureExceptionLogged(content, "Warning", context);
    }

    [Fact]
    public async Task LogWarning_NormalizesMultilineContext_InPersistFailureBreadcrumb()
    {
        var repo = new FailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"warn-repo-context-{Guid.NewGuid():N}";
        var message = $"warn repo fail test {Guid.NewGuid():N}";

        logger.LogWarning(message, $"  {context}\r\nchild  ");

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Warning log for '{context} child': Simulated DB failure", content);
    }

    [Fact]
    public async Task LogWarning_NormalizesWhitespaceRepositoryFailureMessage_ToEmptyMarker()
    {
        var repo = new WhitespaceMessageFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"warn-repo-fail-empty-{Guid.NewGuid():N}";
        var message = $"warn repo fail test {Guid.NewGuid():N}";

        logger.LogWarning(message, context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Warning log for '{context}': (empty)", content);
    }

    [Fact]
    public async Task LogWarning_NormalizesMultilineRepositoryFailureMessage_InCrashBreadcrumb()
    {
        var repo = new MultilineMessageFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"warn-repo-fail-multiline-{Guid.NewGuid():N}";
        var message = $"warn repo fail test {Guid.NewGuid():N}";
        var markerA = $"warn-a-{Guid.NewGuid():N}";
        var markerB = $"warn-b-{Guid.NewGuid():N}";

        repo.ExceptionMessage = $"{markerA}\r\n{markerB}";
        logger.LogWarning(message, context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Warning log for '{context}': {markerA} {markerB}", content);
    }

    [Fact]
    public async Task LogInfo_DoesNotThrow_WhenRepositoryFails()
    {
        var repo = new FailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"info-repo-fail-{Guid.NewGuid():N}";
        var message = $"info repo fail test {Guid.NewGuid():N}";

        logger.LogInfo(message, context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(message, content);
        AssertPersistFailureExceptionLogged(content, "Info", context);
    }

    [Fact]
    public async Task LogInfo_NormalizesMultilineContext_InPersistFailureBreadcrumb()
    {
        var repo = new FailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"info-repo-context-{Guid.NewGuid():N}";
        var message = $"info repo fail test {Guid.NewGuid():N}";

        logger.LogInfo(message, $"  {context}\r\nchild  ");

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Info log for '{context} child': Simulated DB failure", content);
    }

    [Fact]
    public async Task LogInfo_NormalizesWhitespaceRepositoryFailureMessage_ToEmptyMarker()
    {
        var repo = new WhitespaceMessageFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"info-repo-fail-empty-{Guid.NewGuid():N}";
        var message = $"info repo fail test {Guid.NewGuid():N}";

        logger.LogInfo(message, context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Info log for '{context}': (empty)", content);
    }

    [Fact]
    public async Task LogInfo_NormalizesMultilineRepositoryFailureMessage_InCrashBreadcrumb()
    {
        var repo = new MultilineMessageFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"info-repo-fail-multiline-{Guid.NewGuid():N}";
        var message = $"info repo fail test {Guid.NewGuid():N}";
        var markerA = $"info-a-{Guid.NewGuid():N}";
        var markerB = $"info-b-{Guid.NewGuid():N}";

        repo.ExceptionMessage = $"{markerA}\r\n{markerB}";
        logger.LogInfo(message, context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Info log for '{context}': {markerA} {markerB}", content);
    }

    [Fact]
    public async Task Log_DoesNotThrow_WhenRepositoryFailureMessageGetterThrows()
    {
        var repo = new ThrowingMessageFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"repo-fail-throwing-message-{Guid.NewGuid():N}";

        logger.Log(new Exception("repo fail test"), context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Error log for '{context}': (failed to read exception message: System.InvalidOperationException: message getter failure)", content);
        Assert.Contains("ThrowingMessageException", content);
    }

    [Fact]
    public async Task Log_NormalizesRepositoryFailureMultilineMessage_InCrashBreadcrumb()
    {
        var repo = new MultilineMessageFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"repo-fail-multiline-{Guid.NewGuid():N}";
        var markerA = $"db-a-{Guid.NewGuid():N}";
        var markerB = $"db-b-{Guid.NewGuid():N}";

        repo.ExceptionMessage = $"{markerA}\r\n{markerB}";
        logger.Log(new Exception("repo fail test"), context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Error log for '{context}': {markerA} {markerB}", content);
    }

    [Fact]
    public async Task Log_NormalizesRepositoryFailureWhitespaceMessage_ToEmptyMarker()
    {
        var repo = new WhitespaceMessageFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"repo-fail-empty-{Guid.NewGuid():N}";

        logger.Log(new Exception("repo fail test"), context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to persist Error log for '{context}': (empty)", content);
    }

    [Fact]
    public async Task Log_PreservesErrorContext_OnPersistedEntry()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.Log(new InvalidOperationException("ctx test"), "ctx-value");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Error", entry.Level);
        Assert.Equal("ctx-value", entry.Context);
    }

    [Fact]
    public async Task FlushAsync_WhenPurgeFails_WarningLogsFailureButKeepsPersistedEntry()
    {
        var repo = new PurgeFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"purge-fail-{Guid.NewGuid():N}";

        logger.Log(new InvalidOperationException("purge fail test"), context);

        await logger.FlushAsync(TimeSpan.FromSeconds(2));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Error", entry.Level);
        Assert.Contains("purge fail test", entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to purge old error logs after persisting '{context}' (Exception): Simulated purge failure", content);
        Assert.Contains("Type: System.Exception", content);
    }

    [Fact]
    public async Task FlushAsync_NormalizesMultilineContext_InPurgeFailureBreadcrumb()
    {
        var repo = new PurgeFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"purge-context-{Guid.NewGuid():N}";

        logger.Log(new InvalidOperationException("purge fail test"), $"  {context}\r\nchild  ");

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to purge old error logs after persisting '{context} child' (Exception): Simulated purge failure", content);
    }

    [Fact]
    public async Task FlushAsync_WhenPurgeFailureMessageGetterThrows_LogsFallbackMarkerButDoesNotThrow()
    {
        var repo = new ThrowingMessagePurgeFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"purge-fail-throwing-message-{Guid.NewGuid():N}";

        logger.Log(new InvalidOperationException("purge fail test"), context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Equal("Error", entry.Level);
        Assert.Contains("purge fail test", entry.Message);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to purge old error logs after persisting '{context}' (ThrowingMessageException): (failed to read exception message: System.InvalidOperationException: message getter failure)", content);
        Assert.Contains("ThrowingMessageException", content);
    }

    [Fact]
    public async Task FlushAsync_NormalizesPurgeFailureMultilineMessage_InCrashBreadcrumb()
    {
        var repo = new MultilineMessagePurgeFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"purge-fail-multiline-{Guid.NewGuid():N}";
        var markerA = $"purge-a-{Guid.NewGuid():N}";
        var markerB = $"purge-b-{Guid.NewGuid():N}";

        repo.ExceptionMessage = $"{markerA}\r\n{markerB}";
        logger.Log(new InvalidOperationException("purge fail test"), context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to purge old error logs after persisting '{context}' (MultilineMessageException): {markerA} {markerB}", content);
    }

    [Fact]
    public async Task FlushAsync_NormalizesPurgeFailureWhitespaceMessage_ToEmptyMarker()
    {
        var repo = new WhitespaceMessagePurgeFailingAppRepository();
        var logger = new DbErrorLogger(repo);
        var context = $"purge-fail-empty-{Guid.NewGuid():N}";

        logger.Log(new InvalidOperationException("purge fail test"), context);

        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains($"Failed to purge old error logs after persisting '{context}' (WhitespaceMessageException): (empty)", content);
    }

    private sealed class FakeAppRepository : IAppRepository
    {
        public List<ErrorLogEntry> ErrorLogs { get; } = [];
        public int PurgeCallCount { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
        {
            ErrorLogs.Add(entry);
            return Task.CompletedTask;
        }

        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            PurgeCallCount++;
            return Task.CompletedTask;
        }

        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class BlockingAppRepository : IAppRepository
    {
        private readonly TaskCompletionSource addStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<ErrorLogEntry> ErrorLogs { get; } = [];
        public TaskCompletionSource AddStarted => addStarted;

        public void ReleaseWrite() => releaseWrite.TrySetResult();

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
        {
            addStarted.TrySetResult();
            await releaseWrite.Task.WaitAsync(cancellationToken);
            ErrorLogs.Add(entry);
        }

        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FailingAppRepository : IAppRepository
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default) => throw new Exception("Simulated DB failure");
        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PurgeFailingAppRepository : IAppRepository
    {
        public List<ErrorLogEntry> ErrorLogs { get; } = [];

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
        {
            ErrorLogs.Add(entry);
            return Task.CompletedTask;
        }

        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
            => throw new Exception("Simulated purge failure");

        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ThrowingMessageFailingAppRepository : IAppRepository
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default) => throw new ThrowingMessageException();
        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ThrowingMessagePurgeFailingAppRepository : IAppRepository
    {
        public List<ErrorLogEntry> ErrorLogs { get; } = [];

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
        {
            ErrorLogs.Add(entry);
            return Task.CompletedTask;
        }

        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
            => throw new ThrowingMessageException();

        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MultilineMessageFailingAppRepository : IAppRepository
    {
        public string ExceptionMessage { get; set; } = "multiline failure";

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default) => throw new MultilineMessageException(ExceptionMessage);
        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class WhitespaceMessageFailingAppRepository : IAppRepository
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default) => throw new WhitespaceMessageException();
        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MultilineMessagePurgeFailingAppRepository : IAppRepository
    {
        public List<ErrorLogEntry> ErrorLogs { get; } = [];
        public string ExceptionMessage { get; set; } = "multiline purge failure";

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
        {
            ErrorLogs.Add(entry);
            return Task.CompletedTask;
        }

        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
            => throw new MultilineMessageException(ExceptionMessage);

        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class WhitespaceMessagePurgeFailingAppRepository : IAppRepository
    {
        public List<ErrorLogEntry> ErrorLogs { get; } = [];

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
        {
            ErrorLogs.Add(entry);
            return Task.CompletedTask;
        }

        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
            => throw new WhitespaceMessageException();

        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ThrowingMessageException : Exception
    {
        public override string Message => throw new InvalidOperationException("message getter failure");
    }

    private sealed class MultilineMessageException(string value) : Exception
    {
        public override string Message => value;
    }

    private sealed class WhitespaceMessageException : Exception
    {
        public override string Message => " \r\n\t ";
    }

    private sealed class ThrowingStackTraceException : Exception
    {
        public override string? StackTrace => throw new InvalidOperationException("stack trace getter failure");
    }
}
