using System.Reflection;
using Praxis.ViewModels;

namespace Praxis.Tests;

public class MainViewModelWarningMessageTests
{
    [Fact]
    public void BuildSafeWarningMessage_WithPrefix_WhenExceptionMessageIsMultiline_CollapsesToSingleLine()
    {
        var markerA = $"warning-a-{Guid.NewGuid():N}";
        var markerB = $"warning-b-{Guid.NewGuid():N}";

        var result = InvokeBuildSafeWarningMessage("External reload failed", new MultilineMessageException($"{markerA}\r\n{markerB}"));

        Assert.Equal($"External reload failed: {markerA} {markerB}", result);
    }

    [Fact]
    public void BuildSafeWarningMessage_WithPrefix_WhenExceptionMessageIsWhitespace_UsesEmptyMarker()
    {
        var result = InvokeBuildSafeWarningMessage("External reload failed", new WhitespaceMessageException());

        Assert.Equal("External reload failed: (empty)", result);
    }

    [Fact]
    public void BuildSafeWarningMessage_WithFactory_WhenFactoryThrows_UsesSafeFallbackMessage()
    {
        var marker = $"factory-warning-{Guid.NewGuid():N}";

        var result = InvokeBuildSafeWarningMessage(
            _ => throw new InvalidOperationException($"{marker}\r\nsuffix"),
            new WhitespaceMessageException());

        Assert.Contains("Failed to build warning message for", result);
        Assert.Contains($"{marker} suffix", result);
        Assert.Contains("original exception message: (empty)", result);
    }

    private static string InvokeBuildSafeWarningMessage(string prefix, Exception exception)
    {
        var method = typeof(MainViewModel).GetMethod(
            "BuildSafeWarningMessage",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            [typeof(string), typeof(Exception)],
            modifiers: null);
        Assert.NotNull(method);

        var result = method.Invoke(null, [prefix, exception]);
        return Assert.IsType<string>(result);
    }

    private static string InvokeBuildSafeWarningMessage(Func<Exception, string> warningFactory, Exception exception)
    {
        var method = typeof(MainViewModel).GetMethod(
            "BuildSafeWarningMessage",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            [typeof(Func<Exception, string>), typeof(Exception)],
            modifiers: null);
        Assert.NotNull(method);

        var result = method.Invoke(null, [warningFactory, exception]);
        return Assert.IsType<string>(result);
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
