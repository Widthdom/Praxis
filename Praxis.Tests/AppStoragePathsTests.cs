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

    private static string? InvokeCombineAbsoluteFilePath(string? directoryPath, string fileName)
    {
        var method = typeof(AppStoragePaths).GetMethod("CombineAbsoluteFilePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return method.Invoke(null, [directoryPath, fileName]) as string;
    }
}
