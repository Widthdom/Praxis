using Praxis.Core.Logic;

namespace Praxis.Tests;

public class WindowsInputScopeCompatibilityPolicyTests
{
    [Fact]
    public void ShouldDisableInputScopeOnException_ReturnsTrue_ForArgumentException()
    {
        var result = WindowsInputScopeCompatibilityPolicy.ShouldDisableInputScopeOnException(
            new ArgumentException("invalid parameter"));

        Assert.True(result);
    }

    [Fact]
    public void ShouldDisableInputScopeOnException_ReturnsFalse_ForOtherExceptions()
    {
        var result = WindowsInputScopeCompatibilityPolicy.ShouldDisableInputScopeOnException(
            new InvalidOperationException("unexpected state"));

        Assert.False(result);
    }
}
