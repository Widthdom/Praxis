using System.Reflection;
using Praxis.Services;

namespace Praxis.Tests;

public class FileStateSyncNotifierTests
{
    [Fact]
    public void BuildSyncWarningMessage_WhenExceptionMessageGetterThrows_UsesFallbackMarker()
    {
        var prefix = $"sync-warning-{Guid.NewGuid():N}:";

        var result = InvokeBuildSyncWarningMessage(prefix, new ThrowingMessageException());

        Assert.Equal(
            $"{prefix} (failed to read exception message: System.InvalidOperationException: message getter failure)",
            result);
    }

    [Fact]
    public void BuildSyncWarningMessage_WhenExceptionMessageIsMultiline_CollapsesToSingleLine()
    {
        var prefix = $"sync-warning-{Guid.NewGuid():N}:";
        var markerA = $"sync-a-{Guid.NewGuid():N}";
        var markerB = $"sync-b-{Guid.NewGuid():N}";

        var result = InvokeBuildSyncWarningMessage(prefix, new MultilineMessageException($"{markerA}\r\n{markerB}"));

        Assert.Equal($"{prefix} {markerA} {markerB}", result);
    }

    [Fact]
    public void BuildSyncWarningMessage_WhenExceptionMessageIsWhitespace_UsesEmptyMarker()
    {
        var prefix = $"sync-warning-{Guid.NewGuid():N}:";

        var result = InvokeBuildSyncWarningMessage(prefix, new WhitespaceMessageException());

        Assert.Equal($"{prefix} (empty)", result);
    }

    [Fact]
    public void NormalizePayloadForLog_WhenPayloadIsMultiline_CollapsesToSingleLine()
    {
        var markerA = $"sync-payload-a-{Guid.NewGuid():N}";
        var markerB = $"sync-payload-b-{Guid.NewGuid():N}";

        var result = InvokeNormalizePayloadForLog($"{markerA}\r\n{markerB}");

        Assert.Equal($"{markerA} {markerB}", result);
    }

    [Fact]
    public void NormalizePayloadForLog_WhenPayloadIsWhitespace_UsesPlaceholder()
    {
        var result = InvokeNormalizePayloadForLog(" \r\n\t ");

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    [Fact]
    public void NormalizePayloadForLog_WhenPayloadIsNull_UsesPlaceholder()
    {
        var result = InvokeNormalizePayloadForLog(null);

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    private static string InvokeBuildSyncWarningMessage(string prefix, Exception ex)
    {
        var method = typeof(FileStateSyncNotifier).GetMethod("BuildSyncWarningMessage", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [prefix, ex]);
        return Assert.IsType<string>(result);
    }

    private static string InvokeNormalizePayloadForLog(string? payload)
    {
        var method = typeof(FileStateSyncNotifier).GetMethod("NormalizePayloadForLog", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [payload]);
        return Assert.IsType<string>(result);
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
}
