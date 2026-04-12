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
    public void LogInfo_DoesNotThrow()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var ex = Record.Exception(() => logger.LogInfo("info message", "context"));
        Assert.Null(ex);
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
        Assert.Contains($"Failed to purge old error logs after persisting '{context}': Simulated purge failure", content);
        Assert.Contains("Type: System.Exception", content);
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
}
