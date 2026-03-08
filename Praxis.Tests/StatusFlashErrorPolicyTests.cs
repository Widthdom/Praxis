using Praxis.Core.Logic;

namespace Praxis.Tests;

public class StatusFlashErrorPolicyTests
{
    [Fact]
    public void IsErrorStatus_ReturnsTrue_ForCommandNotFoundMessage()
    {
        var isError = StatusFlashErrorPolicy.IsErrorStatus("Command not found: xxx");
        Assert.True(isError);
    }

    [Fact]
    public void IsErrorStatus_ReturnsTrue_ForKnownErrorPrefixes()
    {
        Assert.True(StatusFlashErrorPolicy.IsErrorStatus("Failed: launch failed"));
        Assert.True(StatusFlashErrorPolicy.IsErrorStatus("FAILED: launch failed"));
        Assert.True(StatusFlashErrorPolicy.IsErrorStatus("NOT FOUND"));
        Assert.True(StatusFlashErrorPolicy.IsErrorStatus("Unhandled exception"));
        Assert.True(StatusFlashErrorPolicy.IsErrorStatus("Unhandled ERROR"));
    }

    [Fact]
    public void IsErrorStatus_ReturnsFalse_ForNonErrorMessage()
    {
        var isError = StatusFlashErrorPolicy.IsErrorStatus("Executed.");
        Assert.False(isError);
    }

    [Fact]
    public void IsErrorStatus_ReturnsFalse_ForNullOrWhitespace()
    {
        Assert.False(StatusFlashErrorPolicy.IsErrorStatus(null));
        Assert.False(StatusFlashErrorPolicy.IsErrorStatus("   "));
    }
}
