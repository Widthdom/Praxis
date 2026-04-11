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
    public void PathsEqual_ReturnsFalse_ForInvalidPathInput_InsteadOfThrowing()
    {
        var invalid = $"bad{'\0'}path";

        var result = InvokePathsEqual(invalid, "/tmp/praxis.db3");

        Assert.False(result);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains("Ignoring invalid migration path comparison", content);
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
}
