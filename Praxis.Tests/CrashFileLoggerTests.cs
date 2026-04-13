using System.Reflection;
using Praxis.Services;

namespace Praxis.Tests;

public class CrashFileLoggerTests
{
    [Fact]
    public void LogFilePath_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(CrashFileLogger.LogFilePath));
    }

    [Fact]
    public void LogFilePath_EndsWith_CrashLog()
    {
        Assert.EndsWith("crash.log", CrashFileLogger.LogFilePath);
    }

    [Fact]
    public void LogFilePath_ContainsPraxisDirectory()
    {
        Assert.Contains("Praxis", CrashFileLogger.LogFilePath);
    }

    [Fact]
    public void WriteException_DoesNotThrow_WithNullException()
    {
        var ex = Record.Exception(() => CrashFileLogger.WriteException("test-source", null));
        Assert.Null(ex);
    }

    [Fact]
    public void WriteException_DoesNotThrow_WithValidException()
    {
        var ex = Record.Exception(() =>
            CrashFileLogger.WriteException("test-source", new InvalidOperationException("test")));
        Assert.Null(ex);
    }

    [Fact]
    public void WriteException_DoesNotThrow_WithInnerException()
    {
        var inner = new NullReferenceException("inner message");
        var outer = new InvalidOperationException("outer message", inner);

        var ex = Record.Exception(() => CrashFileLogger.WriteException("test-source", outer));
        Assert.Null(ex);
    }

    [Fact]
    public void WriteException_DoesNotThrow_WithAggregateException()
    {
        var agg = new AggregateException("aggregate",
            new InvalidOperationException("first"),
            new ArgumentException("second"));

        var ex = Record.Exception(() => CrashFileLogger.WriteException("test-source", agg));
        Assert.Null(ex);
    }

    [Fact]
    public void WriteInfo_DoesNotThrow()
    {
        var ex = Record.Exception(() => CrashFileLogger.WriteInfo("test-source", "info message"));
        Assert.Null(ex);
    }

    [Fact]
    public void WriteWarning_DoesNotThrow()
    {
        var ex = Record.Exception(() => CrashFileLogger.WriteWarning("test-source", "warn message"));
        Assert.Null(ex);
    }

    [Fact]
    public void WriteException_WritesToFile()
    {
        var marker = $"CrashFileLoggerTests-{Guid.NewGuid()}";
        CrashFileLogger.WriteException(marker, new Exception("test write"));

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(marker, content);
    }

    [Fact]
    public void WriteInfo_WritesToFile()
    {
        var marker = $"InfoTest-{Guid.NewGuid()}";
        CrashFileLogger.WriteInfo("test", marker);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(marker, content);
    }

    [Fact]
    public void WriteWarning_WritesToFile()
    {
        var marker = $"WarnTest-{Guid.NewGuid()}";
        CrashFileLogger.WriteWarning("test", marker);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(marker, content);
    }

    [Fact]
    public void WriteException_CapturesInnerExceptionDetails()
    {
        var marker = $"InnerTest-{Guid.NewGuid()}";
        var inner = new NullReferenceException(marker);
        var outer = new InvalidOperationException("outer", inner);

        CrashFileLogger.WriteException("test", outer);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(marker, content);
        Assert.Contains("NullReferenceException", content);
        Assert.Contains("Inner Exception", content);
    }

    [Fact]
    public void WriteException_CapturesAggregateExceptionChildren()
    {
        var marker1 = $"Agg1-{Guid.NewGuid()}";
        var marker2 = $"Agg2-{Guid.NewGuid()}";
        var agg = new AggregateException("aggregate",
            new Exception(marker1),
            new Exception(marker2));

        CrashFileLogger.WriteException("test", agg);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(marker1, content);
        Assert.Contains(marker2, content);
        Assert.Contains("AggregateException[0]", content);
        Assert.Contains("AggregateException[1]", content);
    }

    [Fact]
    public void WriteException_WideAggregate_UsesBoundedSummaryAndTailSampling()
    {
        var middleMarker = $"AggMiddle-{Guid.NewGuid()}";
        var tailMarker = $"AggTail-{Guid.NewGuid()}";
        var children = Enumerable.Range(0, 5000)
            .Select(i => (Exception)new InvalidOperationException(
                i == 4500 ? middleMarker :
                i == 4999 ? tailMarker :
                $"AggChild-{i}"))
            .ToArray();
        var agg = new AggregateException("wide aggregate", children);

        CrashFileLogger.WriteException("test", agg);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("top-level summary omitted", content);
        Assert.Contains("middle child(ren) not fully scanned", content);
        Assert.Contains("sampling last", content);
        Assert.Contains(tailMarker, content);
        Assert.DoesNotContain(middleMarker, content);
    }

    [Fact]
    public void WriteException_CapturesExceptionData()
    {
        var ex = new Exception("with data");
        var dataKey = $"key-{Guid.NewGuid()}";
        var dataValue = $"value-{Guid.NewGuid()}";
        ex.Data[dataKey] = dataValue;

        CrashFileLogger.WriteException("test", ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(dataKey, content);
        Assert.Contains(dataValue, content);
    }

    [Fact]
    public async Task WriteException_IsThreadSafe()
    {
        var exceptions = Enumerable.Range(0, 20)
            .Select(i => new Exception($"thread-safe-test-{i}"))
            .ToList();

        var tasks = exceptions.Select(e =>
            Task.Run(() => CrashFileLogger.WriteException("concurrent", e)));

        await Task.WhenAll(tasks);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        foreach (var e in exceptions)
        {
            Assert.Contains(e.Message, content);
        }
    }

    [Fact]
    public void WriteException_DoesNotStackOverflow_OnDeepInnerExceptionChain()
    {
        // Build a chain deeper than the configured cap so the depth guard must kick in.
        Exception current = new InvalidOperationException("leaf");
        for (var i = 0; i < 200; i++)
        {
            current = new InvalidOperationException($"wrap-{i}", current);
        }

        var marker = $"DeepChain-{Guid.NewGuid()}";
        var ex = Record.Exception(() => CrashFileLogger.WriteException(marker, current));
        Assert.Null(ex);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(marker, content);
        Assert.Contains("Exception chain truncated", content);
    }

    [Fact]
    public void ResolveCrashLogDirectory_UsesTrimmedLocalAppData_OnWindows()
    {
        var result = InvokeResolveCrashLogDirectory(
            "  \"/tmp/praxis-local\"  ",
            null,
            null,
            "/tmp/fallback",
            isWindows: true,
            isMacLike: false);

        Assert.Equal("/tmp/praxis-local/Praxis", result);
    }

    [Fact]
    public void ResolveCrashLogDirectory_UsesMacApplicationSupportRoot_WhenMacLike()
    {
        var result = InvokeResolveCrashLogDirectory(
            localAppDataOverride: null,
            userProfileOverride: "  '/Users/tester'  ",
            localAppDataFolderOverride: "/tmp/localappdata",
            currentDirectory: "/tmp/current",
            isWindows: false,
            isMacLike: true);

        Assert.Equal("/Users/tester/Library/Application Support/Praxis", result);
    }

    [Fact]
    public void ResolveCrashLogDirectory_FallsBackToLocalApplicationData_WhenMacHomeIsInvalid()
    {
        var result = InvokeResolveCrashLogDirectory(
            localAppDataOverride: null,
            userProfileOverride: "relative/home",
            localAppDataFolderOverride: "\"/tmp/localappdata\"",
            currentDirectory: "/tmp/current",
            isWindows: false,
            isMacLike: true);

        Assert.Equal("/tmp/localappdata/Praxis", result);
    }

    [Fact]
    public void ResolveCrashLogDirectory_FallsBackToCurrentDirectory_WhenNoAbsolutePlatformDirectoryExists()
    {
        var result = InvokeResolveCrashLogDirectory(
            null,
            null,
            "relative/localappdata",
            "/tmp/current",
            isWindows: false,
            isMacLike: false);

        Assert.Equal("/tmp/current/Praxis", result);
    }

    private static string InvokeResolveCrashLogDirectory(
        string? localAppDataOverride,
        string? userProfileOverride,
        string? localAppDataFolderOverride,
        string currentDirectory,
        bool isWindows,
        bool isMacLike)
    {
        var method = typeof(CrashFileLogger).GetMethod(
            "ResolveCrashLogDirectory",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            [typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool)],
            modifiers: null);
        Assert.NotNull(method);

        var result = method.Invoke(null, [localAppDataOverride, userProfileOverride, localAppDataFolderOverride, currentDirectory, isWindows, isMacLike]);
        return Assert.IsType<string>(result);
    }
}
