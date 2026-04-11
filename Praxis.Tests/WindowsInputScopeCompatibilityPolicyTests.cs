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
    public void ShouldDisableInputScopeOnException_ReturnsTrue_ForDerivedArgumentExceptions()
    {
        Assert.True(WindowsInputScopeCompatibilityPolicy.ShouldDisableInputScopeOnException(
            new ArgumentNullException("value")));
    }

    [Fact]
    public void ShouldDisableInputScopeOnException_ReturnsFalse_ForOtherExceptions()
    {
        Assert.False(WindowsInputScopeCompatibilityPolicy.ShouldDisableInputScopeOnException(
            new InvalidOperationException("unexpected state")));
        Assert.False(WindowsInputScopeCompatibilityPolicy.ShouldDisableInputScopeOnException(
            new ApplicationException("generic failure")));
    }

    [Fact]
    public void ShouldDisableInputScopeOnException_DoesNotInspectInnerExceptions()
    {
        var wrapped = new InvalidOperationException(
            "outer",
            new ArgumentException("inner"));

        Assert.False(WindowsInputScopeCompatibilityPolicy.ShouldDisableInputScopeOnException(wrapped));
    }
}
