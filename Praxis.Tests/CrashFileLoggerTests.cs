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
}
