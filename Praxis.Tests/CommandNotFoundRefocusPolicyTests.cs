using Praxis.Core.Logic;

namespace Praxis.Tests;

public class CommandNotFoundRefocusPolicyTests
{
    [Fact]
    public void ShouldRefocusMainCommand_ReturnsTrue_ForCommandNotFoundMessage()
    {
        Assert.True(CommandNotFoundRefocusPolicy.ShouldRefocusMainCommand("Command not found: demo"));
    }

    [Fact]
    public void ShouldRefocusMainCommand_ReturnsFalse_ForOtherStatusMessages()
    {
        Assert.False(CommandNotFoundRefocusPolicy.ShouldRefocusMainCommand("Executed."));
        Assert.False(CommandNotFoundRefocusPolicy.ShouldRefocusMainCommand("Failed: launch failed"));
    }

    [Fact]
    public void ShouldRefocusMainCommand_ReturnsFalse_ForNullOrWhitespace()
    {
        Assert.False(CommandNotFoundRefocusPolicy.ShouldRefocusMainCommand(null));
        Assert.False(CommandNotFoundRefocusPolicy.ShouldRefocusMainCommand("   "));
    }
}
