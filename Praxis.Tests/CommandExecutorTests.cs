using System.Diagnostics;
using System.Reflection;
using Praxis.Services;

namespace Praxis.Tests;

public class CommandExecutorTests
{
    [Fact]
    public void ApplyWorkingDirectoryOverride_SetsUserProfile_ForWindowsShellTool()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            UseShellExecute = true,
        };

        InvokeApplyWorkingDirectoryOverride(startInfo, "cmd.exe", isWindows: true);

        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), startInfo.WorkingDirectory);
    }

    [Theory]
    [InlineData("\"C:\\Program Files\\App\\app.exe\"", "C:\\Program Files\\App\\app.exe")]
    [InlineData("'C:\\Program Files\\App\\app.exe'", "C:\\Program Files\\App\\app.exe")]
    [InlineData("  pwsh  ", "pwsh")]
    public void NormalizeToolPath_TrimsWrappingQuotesAndWhitespace(string value, string expected)
    {
        var result = InvokeNormalizeToolPath(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExpandHomePath_ExpandsBareTilde_ToUserProfile()
    {
        var result = InvokeExpandHomePath("~");

        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), result);
    }

    [Theory]
    [InlineData("~/Documents", "Documents")]
    [InlineData("~\\Documents", "Documents")]
    public void ExpandHomePath_ExpandsTildePrefixedPath(string value, string relativeSegment)
    {
        var result = InvokeExpandHomePath(value);

        Assert.Equal(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), relativeSegment),
            result);
    }

    [Fact]
    public void ExpandHomePath_LeavesNonHomePathUntouched()
    {
        const string value = "docs/readme.txt";

        var result = InvokeExpandHomePath(value);

        Assert.Equal(value, result);
    }

    private static string InvokeExpandHomePath(string value)
    {
        var method = typeof(CommandExecutor).GetMethod("ExpandHomePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [value]);
        return Assert.IsType<string>(result);
    }

    private static void InvokeApplyWorkingDirectoryOverride(ProcessStartInfo startInfo, string tool, bool isWindows)
    {
        var method = typeof(CommandExecutor).GetMethod(
            "ApplyWorkingDirectoryOverride",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            [typeof(ProcessStartInfo), typeof(string), typeof(bool)],
            modifiers: null);
        Assert.NotNull(method);

        method.Invoke(null, [startInfo, tool, isWindows]);
    }

    private static string InvokeNormalizeToolPath(string value)
    {
        var method = typeof(CommandExecutor).GetMethod("NormalizeToolPath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [value]);
        return Assert.IsType<string>(result);
    }
}
