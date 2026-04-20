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
            $"{prefix} (ThrowingMessageException) (failed to read exception message: System.InvalidOperationException: message getter failure)",
            result);
    }

    [Fact]
    public void BuildSyncWarningMessage_WhenExceptionMessageIsMultiline_CollapsesToSingleLine()
    {
        var prefix = $"sync-warning-{Guid.NewGuid():N}:";
        var markerA = $"sync-a-{Guid.NewGuid():N}";
        var markerB = $"sync-b-{Guid.NewGuid():N}";

        var result = InvokeBuildSyncWarningMessage(prefix, new MultilineMessageException($"{markerA}\r\n{markerB}"));

        Assert.Equal($"{prefix} (MultilineMessageException) {markerA} {markerB}", result);
    }

    [Fact]
    public void BuildSyncWarningMessage_WhenExceptionMessageIsWhitespace_UsesEmptyMarker()
    {
        var prefix = $"sync-warning-{Guid.NewGuid():N}:";

        var result = InvokeBuildSyncWarningMessage(prefix, new WhitespaceMessageException());

        Assert.Equal($"{prefix} (WhitespaceMessageException) (empty)", result);
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

    [Fact]
    public void BuildSyncWarningMessage_CanIncludeNormalizedSignalPathPrefix()
    {
        var pathMarkerA = $"sync-path-a-{Guid.NewGuid():N}";
        var pathMarkerB = $"sync-path-b-{Guid.NewGuid():N}";
        var normalizedPath = InvokeNormalizePayloadForLog($"/tmp/{pathMarkerA}\r\n{pathMarkerB}/buttons.sync");
        var prefix = $"Failed to read sync payload '{normalizedPath}' after retries:";
        var markerA = $"sync-read-a-{Guid.NewGuid():N}";
        var markerB = $"sync-read-b-{Guid.NewGuid():N}";

        var result = InvokeBuildSyncWarningMessage(prefix, new MultilineMessageException($"{markerA}\r\n{markerB}"));

        Assert.Equal(
            $"Failed to read sync payload '/tmp/{pathMarkerA} {pathMarkerB}/buttons.sync' after retries: (MultilineMessageException) {markerA} {markerB}",
            result);
    }

    [Fact]
    public void BuildMalformedPayloadWarning_NormalizesPathAndPayload()
    {
        var pathMarkerA = $"sync-path-a-{Guid.NewGuid():N}";
        var pathMarkerB = $"sync-path-b-{Guid.NewGuid():N}";
        var payloadMarkerA = $"sync-payload-a-{Guid.NewGuid():N}";
        var payloadMarkerB = $"sync-payload-b-{Guid.NewGuid():N}";

        var result = InvokeBuildMalformedPayloadWarning(
            $"/tmp/{pathMarkerA}\r\n{pathMarkerB}/buttons.sync",
            $"{payloadMarkerA}\r\n{payloadMarkerB}");

        Assert.Equal(
            $"Ignored malformed sync payload from '/tmp/{pathMarkerA} {pathMarkerB}/buttons.sync': \"{payloadMarkerA} {payloadMarkerB}\"",
            result);
    }

    [Fact]
    public void BuildMalformedPayloadWarning_UsesPlaceholderForWhitespaceInputs()
    {
        var result = InvokeBuildMalformedPayloadWarning(" \r\n\t ", " \r\n\t ");

        Assert.Equal(
            $"Ignored malformed sync payload from '{CrashFileLogger.MissingMessagePayloadPlaceholder}': \"{CrashFileLogger.MissingMessagePayloadPlaceholder}\"",
            result);
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

    private static string InvokeBuildMalformedPayloadWarning(string signalPath, string payload)
    {
        var method = typeof(FileStateSyncNotifier).GetMethod("BuildMalformedPayloadWarning", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [signalPath, payload]);
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
