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

    [Fact]
    public void ApplyWorkingDirectoryOverride_SetsUserProfile_ForEnvExpandedWindowsShellTool()
    {
        const string variableName = "PRAXIS_TEST_COMMAND_EXECUTOR_SHELL";
        var previous = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, "\"C:\\Program Files\\PowerShell\\7\\pwsh.exe\"");

            var startInfo = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                UseShellExecute = true,
            };

            InvokeApplyWorkingDirectoryOverride(startInfo, $"%{variableName}%", isWindows: true);

            Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), startInfo.WorkingDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previous);
        }
    }

    [Theory]
    [InlineData("\"C:\\Program Files\\App\\app.exe\"", "C:\\Program Files\\App\\app.exe")]
    [InlineData("'C:\\Program Files\\App\\app.exe'", "C:\\Program Files\\App\\app.exe")]
    [InlineData("  pwsh  ", "pwsh")]
    [InlineData("\"\"", "")]
    [InlineData("'   '", "")]
    public void NormalizeToolPath_TrimsWrappingQuotesAndWhitespace(string value, string expected)
    {
        var result = InvokeNormalizeToolPath(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeToolPath_ExpandsEnvironmentVariables_AndTrimsExpandedQuotes()
    {
        const string variableName = "PRAXIS_TEST_COMMAND_EXECUTOR_TOOL";
        var previous = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, "\"C:\\Program Files\\App\\tool.exe\"");

            var result = InvokeNormalizeToolPath($"%{variableName}%");

            Assert.Equal("C:\\Program Files\\App\\tool.exe", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previous);
        }
    }

    [Fact]
    public void NormalizeToolPath_WhenValueIsNull_ReturnsEmptyString()
    {
        var result = InvokeNormalizeToolPath(null);

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("pwsh", true)]
    [InlineData("\"C:\\Program Files\\App\\app.exe\"", true)]
    [InlineData("\"\"", false)]
    [InlineData("'   '", false)]
    [InlineData("   ", false)]
    public void HasUsableTool_UsesNormalizedToolEmptiness(string value, bool expected)
    {
        var result = InvokeHasUsableTool(InvokeNormalizeToolPath(value));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void HasUsableTool_WhenValueIsNull_ReturnsFalse()
    {
        var result = InvokeHasUsableTool(null);

        Assert.False(result);
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

    [Fact]
    public void StartProcess_WarningLogsFailure_WhenProcessStartThrows()
    {
        var failurePrefix = $"CommandExecutor failure {Guid.NewGuid():N}";
        var startInfo = new ProcessStartInfo
        {
            FileName = string.Empty,
            UseShellExecute = true,
        };

        var result = InvokeStartProcess(startInfo, "Executed.", failurePrefix);

        Assert.False(result.Success);
        Assert.StartsWith(failurePrefix, result.Message, StringComparison.Ordinal);

        var content = File.ReadAllText(CrashFileLogger.LogFilePath);
        Assert.Contains(failurePrefix, content);
    }

    [Fact]
    public void BuildFailureMessage_WhenExceptionMessageGetterThrows_UsesFallbackMarker()
    {
        var prefix = $"CommandExecutor failure {Guid.NewGuid():N}";

        var result = InvokeBuildFailureMessage(prefix, new ThrowingMessageException());

        Assert.Equal(
            $"{prefix} (failed to read exception message: System.InvalidOperationException: message getter failure)",
            result);
    }

    [Fact]
    public void BuildFailureMessage_WhenExceptionMessageIsMultiline_CollapsesToSingleLine()
    {
        var prefix = $"CommandExecutor failure {Guid.NewGuid():N}";
        var markerA = $"command-a-{Guid.NewGuid():N}";
        var markerB = $"command-b-{Guid.NewGuid():N}";

        var result = InvokeBuildFailureMessage(prefix, new MultilineMessageException($"{markerA}\r\n{markerB}"));

        Assert.Equal($"{prefix} {markerA} {markerB}", result);
    }

    [Fact]
    public void BuildFailureMessage_WhenExceptionMessageIsWhitespace_UsesEmptyMarker()
    {
        var prefix = $"CommandExecutor failure {Guid.NewGuid():N}";

        var result = InvokeBuildFailureMessage(prefix, new WhitespaceMessageException());

        Assert.Equal($"{prefix} (empty)", result);
    }

    [Fact]
    public void NormalizeTargetForLog_WhenValueIsMultiline_CollapsesToSingleLine()
    {
        var markerA = $"target-a-{Guid.NewGuid():N}";
        var markerB = $"target-b-{Guid.NewGuid():N}";

        var result = InvokeNormalizeTargetForLog($"{markerA}\r\n{markerB}");

        Assert.Equal($"{markerA} {markerB}", result);
    }

    [Fact]
    public void NormalizeTargetForLog_WhenValueIsWhitespace_UsesPlaceholder()
    {
        var result = InvokeNormalizeTargetForLog(" \r\n\t ");

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
    }

    [Fact]
    public void NormalizeTargetForLog_WhenValueIsNull_UsesPlaceholder()
    {
        var result = InvokeNormalizeTargetForLog(null);

        Assert.Equal(CrashFileLogger.MissingMessagePayloadPlaceholder, result);
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

    private static string InvokeNormalizeToolPath(string? value)
    {
        var method = typeof(CommandExecutor).GetMethod("NormalizeToolPath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [value]);
        return Assert.IsType<string>(result);
    }

    private static bool InvokeHasUsableTool(string? value)
    {
        var method = typeof(CommandExecutor).GetMethod("HasUsableTool", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [value]);
        return Assert.IsType<bool>(result);
    }

    private static (bool Success, string Message) InvokeStartProcess(ProcessStartInfo startInfo, string successMessage, string failurePrefix)
    {
        var method = typeof(CommandExecutor).GetMethod(
            "StartProcess",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            [typeof(ProcessStartInfo), typeof(string), typeof(string)],
            modifiers: null);
        Assert.NotNull(method);

        var result = method.Invoke(null, [startInfo, successMessage, failurePrefix]);
        return Assert.IsType<(bool Success, string Message)>(result);
    }

    private static string InvokeBuildFailureMessage(string prefix, Exception ex)
    {
        var method = typeof(CommandExecutor).GetMethod("BuildFailureMessage", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [prefix, ex]);
        return Assert.IsType<string>(result);
    }

    private static string InvokeNormalizeTargetForLog(string? value)
    {
        var method = typeof(CommandExecutor).GetMethod("NormalizeTargetForLog", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [value]);
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
