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

    private static string InvokeBuildSyncWarningMessage(string prefix, Exception ex)
    {
        var method = typeof(FileStateSyncNotifier).GetMethod("BuildSyncWarningMessage", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [prefix, ex]);
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
