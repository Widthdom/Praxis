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
    public void IsErrorStatus_ReturnsTrue_ForEmbeddedErrorTerms()
    {
        Assert.True(StatusFlashErrorPolicy.IsErrorStatus("Launch error recovered"));
        Assert.True(StatusFlashErrorPolicy.IsErrorStatus("Clipboard exception occurred"));
        Assert.True(StatusFlashErrorPolicy.IsErrorStatus("Binary was not found on disk"));
    }

    [Fact]
    public void IsErrorStatus_ReturnsFalse_ForNonErrorMessage()
    {
        var isError = StatusFlashErrorPolicy.IsErrorStatus("Executed.");
        Assert.False(isError);
    }

    [Fact]
    public void IsErrorStatus_ReturnsFalse_ForMessagesWithoutTrackedTerms()
    {
        Assert.False(StatusFlashErrorPolicy.IsErrorStatus("Completed with warnings"));
        Assert.False(StatusFlashErrorPolicy.IsErrorStatus("Found cached result"));
    }

    [Fact]
    public void IsErrorStatus_ReturnsFalse_ForNullOrWhitespace()
    {
        Assert.False(StatusFlashErrorPolicy.IsErrorStatus(null));
        Assert.False(StatusFlashErrorPolicy.IsErrorStatus("   "));
    }
}
