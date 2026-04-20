using System.Reflection;
using Praxis.Services;

namespace Praxis.Tests;

public class AppStoragePathsTests
{
    [Fact]
    public void WindowsLocalAppDataRoot_TrimsWrappingQuotes_FromEnvironment()
    {
        var previous = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        try
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", "  \"/tmp/praxis-local\"  ");

            Assert.Equal("/tmp/praxis-local", AppStoragePaths.WindowsLocalAppDataRoot);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", previous);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative/path")]
    public void CombineAbsoluteFilePath_ReturnsNull_ForBlankOrRelativeDirectory(string? value)
    {
        var result = InvokeCombineAbsoluteFilePath(value, "praxis.db3");

        Assert.Null(result);
    }

    [Fact]
    public void CombineAbsoluteFilePath_TrimsQuotes_ForAbsoluteDirectory()
    {
        var result = InvokeCombineAbsoluteFilePath("  \"/tmp/praxis data\"  ", "praxis.db3");

        Assert.Equal("/tmp/praxis data/praxis.db3", result);
    }

    [Fact]
    public void CombineAbsoluteFilePath_TrimsSingleQuotes_ForAbsoluteDirectory()
    {
        var result = InvokeCombineAbsoluteFilePath("  '/tmp/praxis single data'  ", "praxis.db3");

        Assert.Equal("/tmp/praxis single data/praxis.db3", result);
    }

    [Fact]
    public void PathsEqual_ReturnsTrue_ForEquivalentAbsolutePaths()
    {
        var result = InvokePathsEqual("/tmp/../tmp/praxis.db3", "/tmp/praxis.db3");

        Assert.True(result);
    }

    [Fact]
    public void PathsEqual_ReturnsFalse_ForInvalidPathInput_InsteadOfThrowing()
    {
        var invalid = $"bad{'\0'}path";

        var result = InvokePathsEqual(invalid, "/tmp/praxis.db3");

        Assert.False(result);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("Ignoring invalid migration path comparison", content);
    }

    [Fact]
    public void BuildSafeWarningMessage_WhenMessageGetterThrows_UsesFallbackMarker()
    {
        var result = InvokeBuildSafeWarningMessage("Legacy database migration failed from '/tmp/legacy.db3'", new ThrowingMessageIOException());

        Assert.Contains("Legacy database migration failed from '/tmp/legacy.db3' (ThrowingMessageIOException)", result);
        Assert.Contains("failed to read exception message", result);
    }

    [Fact]
    public void BuildSafeWarningMessage_WhenMessageIsMultiline_CollapsesToSingleLine()
    {
        var markerA = $"storage-a-{Guid.NewGuid():N}";
        var markerB = $"storage-b-{Guid.NewGuid():N}";

        var result = InvokeBuildSafeWarningMessage(
            "Legacy database migration failed from '/tmp/legacy.db3'",
            new MultilineIOException($"{markerA}\r\n{markerB}"));

        Assert.Equal($"Legacy database migration failed from '/tmp/legacy.db3' (MultilineIOException): {markerA} {markerB}", result);
    }

    [Fact]
    public void BuildSafeWarningMessage_WhenMessageIsWhitespace_UsesEmptyMarker()
    {
        var result = InvokeBuildSafeWarningMessage(
            "Legacy database migration failed from '/tmp/legacy.db3'",
            new WhitespaceIOException());

        Assert.Equal("Legacy database migration failed from '/tmp/legacy.db3' (WhitespaceIOException): (empty)", result);
    }

    [Fact]
    public void NormalizePathForLog_WhenPathIsMultiline_CollapsesToSingleLine()
    {
        var markerA = $"storage-path-a-{Guid.NewGuid():N}";
        var markerB = $"storage-path-b-{Guid.NewGuid():N}";

        var result = InvokeNormalizePathForLog($"/tmp/{markerA}\r\n{markerB}/praxis.db3");

        Assert.Equal($"/tmp/{markerA} {markerB}/praxis.db3", result);
    }

    [Fact]
    public void NormalizePathForLog_WhenPathIsWhitespace_UsesPlaceholder()
    {
        var result = InvokeNormalizePathForLog(" \r\n\t ");

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    [Fact]
    public void NormalizePathForLog_WhenPathIsNull_UsesPlaceholder()
    {
        var result = InvokeNormalizePathForLog(null);

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    private static string? InvokeCombineAbsoluteFilePath(string? directoryPath, string fileName)
    {
        var method = typeof(AppStoragePaths).GetMethod("CombineAbsoluteFilePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return method.Invoke(null, [directoryPath, fileName]) as string;
    }

    private static bool InvokePathsEqual(string left, string right)
    {
        var method = typeof(AppStoragePaths).GetMethod("PathsEqual", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [left, right]);
        return Assert.IsType<bool>(result);
    }

    private static string InvokeBuildSafeWarningMessage(string prefix, Exception exception)
    {
        var method = typeof(AppStoragePaths).GetMethod("BuildSafeWarningMessage", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [prefix, exception]);
        return Assert.IsType<string>(result);
    }

    private static string InvokeNormalizePathForLog(string? path)
    {
        var method = typeof(AppStoragePaths).GetMethod("NormalizePathForLog", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [path]);
        return Assert.IsType<string>(result);
    }

    private sealed class ThrowingMessageIOException : IOException
    {
        public override string Message => throw new InvalidOperationException("message getter failure");
    }

    private sealed class MultilineIOException(string value) : IOException
    {
        public override string Message => value;
    }

    private sealed class WhitespaceIOException : IOException
    {
        public override string Message => " \r\n\t ";
    }
}
