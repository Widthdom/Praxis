using Praxis.Core.Logic;

namespace Praxis.Tests;

public class CommandWorkingDirectoryPolicyTests
{
    [Theory]
    [InlineData("cmd")]
    [InlineData("cmd.exe")]
    [InlineData("CMD.EXE")]
    [InlineData("powershell")]
    [InlineData("powershell.exe")]
    [InlineData("PowerShell.EXE")]
    [InlineData("pwsh")]
    [InlineData("pwsh.exe")]
    [InlineData("PWSH.EXE")]
    [InlineData("wt")]
    [InlineData("wt.exe")]
    [InlineData("WT.EXE")]
    [InlineData("\"C:\\Windows\\System32\\cmd.exe\"")]
    [InlineData("\"C:\\Windows\\System32\\CMD.EXE\"")]
    [InlineData(" C:\\Program Files\\PowerShell\\7\\pwsh.exe ")]
    public void RequiresUserProfileWorkingDirectory_ReturnsTrue_ForWindowsShellTools(string tool)
    {
        var result = CommandWorkingDirectoryPolicy.RequiresUserProfileWorkingDirectory(tool, isWindows: true);

        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("explorer.exe")]
    [InlineData("notepad.exe")]
    [InlineData("bash")]
    public void RequiresUserProfileWorkingDirectory_ReturnsFalse_ForOtherTools(string? tool)
    {
        var result = CommandWorkingDirectoryPolicy.RequiresUserProfileWorkingDirectory(tool, isWindows: true);

        Assert.False(result);
    }

    [Theory]
    [InlineData("cmd.exe")]
    [InlineData("powershell.exe")]
    [InlineData("pwsh.exe")]
    [InlineData("wt.exe")]
    public void RequiresUserProfileWorkingDirectory_ReturnsFalse_OnNonWindows(string tool)
    {
        var result = CommandWorkingDirectoryPolicy.RequiresUserProfileWorkingDirectory(tool, isWindows: false);

        Assert.False(result);
    }

    [Fact]
    public void RequiresUserProfileWorkingDirectory_ReturnsTrue_ForExpandedEnvironmentShellPath()
    {
        const string variableName = "PRAXIS_TEST_SHELL_PATH";
        var original = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, "\"C:\\Program Files\\PowerShell\\7\\pwsh.exe\"");

            var result = CommandWorkingDirectoryPolicy.RequiresUserProfileWorkingDirectory($"%{variableName}%", isWindows: true);

            Assert.True(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, original);
        }
    }

    [Fact]
    public void RequiresUserProfileWorkingDirectory_ReturnsTrue_ForExpandedUppercaseEnvironmentShellPath()
    {
        const string variableName = "PRAXIS_TEST_SHELL_PATH_UPPER";
        var original = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, "\"C:\\Windows\\System32\\CMD.EXE\"");

            var result = CommandWorkingDirectoryPolicy.RequiresUserProfileWorkingDirectory($"%{variableName}%", isWindows: true);

            Assert.True(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, original);
        }
    }
}
