using Praxis.Core.Logic;

namespace Praxis.Tests;

public class WindowsCommandInputImePolicyTests
{
    [Fact]
    public void FocusedInputScopeEnforcementInterval_IsPositive()
    {
        Assert.True(WindowsCommandInputImePolicy.FocusedInputScopeEnforcementInterval > TimeSpan.Zero);
    }

    [Fact]
    public void ShouldEnforceInputScope_ReturnsTrue_WhenFocused()
    {
        Assert.True(WindowsCommandInputImePolicy.ShouldEnforceInputScope(isFocused: true));
    }

    [Fact]
    public void ShouldEnforceInputScope_ReturnsFalse_WhenNotFocused()
    {
        Assert.False(WindowsCommandInputImePolicy.ShouldEnforceInputScope(isFocused: false));
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(-2, 5, 0)]
    [InlineData(0, 5, 0)]
    [InlineData(2, 5, 2)]
    [InlineData(5, 5, 5)]
    [InlineData(8, 5, 5)]
    public void ClampSelectionStart_ReturnsExpectedValue(int selectionStart, int textLength, int expected)
    {
        var clamped = WindowsCommandInputImePolicy.ClampSelectionStart(selectionStart, textLength);

        Assert.Equal(expected, clamped);
    }
}
