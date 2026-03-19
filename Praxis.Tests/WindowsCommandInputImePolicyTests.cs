using Praxis.Core.Logic;

namespace Praxis.Tests;

public class WindowsCommandInputImePolicyTests
{
    [Fact]
    public void ShouldForceAsciiImeMode_ReturnsTrue_WhenFocusedAndEnabled()
    {
        Assert.True(WindowsCommandInputImePolicy.ShouldForceAsciiImeMode(
            isFocused: true,
            enforceAsciiInput: true));
    }

    [Fact]
    public void ShouldForceAsciiImeMode_ReturnsFalse_WhenNotFocused()
    {
        Assert.False(WindowsCommandInputImePolicy.ShouldForceAsciiImeMode(
            isFocused: false,
            enforceAsciiInput: true));
    }

    [Fact]
    public void ShouldForceAsciiImeMode_ReturnsFalse_WhenDisabled()
    {
        Assert.False(WindowsCommandInputImePolicy.ShouldForceAsciiImeMode(
            isFocused: true,
            enforceAsciiInput: false));
    }

    [Fact]
    public void ResolveAsciiImeNudgeDelays_ReturnsImmediateAndDelayedAttempts_WhenFocusedAndEnabled()
    {
        var delays = WindowsCommandInputImePolicy.ResolveAsciiImeNudgeDelays(
            isFocused: true,
            enforceAsciiInput: true);

        Assert.Equal(2, delays.Count);
        Assert.Equal(TimeSpan.Zero, delays[0]);
        Assert.True(delays[1] > TimeSpan.Zero);
    }

    [Fact]
    public void ResolveAsciiImeNudgeDelays_ReturnsNoAttempts_WhenNotFocused()
    {
        var delays = WindowsCommandInputImePolicy.ResolveAsciiImeNudgeDelays(
            isFocused: false,
            enforceAsciiInput: true);

        Assert.Empty(delays);
    }

    [Fact]
    public void ResolveAsciiImeNudgeDelays_ReturnsNoAttempts_WhenDisabled()
    {
        var delays = WindowsCommandInputImePolicy.ResolveAsciiImeNudgeDelays(
            isFocused: true,
            enforceAsciiInput: false);

        Assert.Empty(delays);
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, false)]
    public void ShouldReassertAsciiImeMode_ReturnsExpectedValue(
        bool isFocused,
        bool keepAsciiImeWhileFocused,
        bool enforceAsciiInput,
        bool expected)
    {
        var result = WindowsCommandInputImePolicy.ShouldReassertAsciiImeMode(
            isFocused: isFocused,
            keepAsciiImeWhileFocused: keepAsciiImeWhileFocused,
            enforceAsciiInput: enforceAsciiInput);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveAsciiImeReassertInterval_ReturnsPositiveInterval()
    {
        var interval = WindowsCommandInputImePolicy.ResolveAsciiImeReassertInterval();

        Assert.True(interval > TimeSpan.Zero);
    }

    [Theory]
    [InlineData(0x0000u, 0x0000u)]
    [InlineData(0x0001u, 0x0000u)]
    [InlineData(0x0002u, 0x0000u)]
    [InlineData(0x0008u, 0x0000u)]
    [InlineData(0x0020u, 0x0000u)]
    [InlineData(0x0040u, 0x0000u)]
    [InlineData(0x0010u, 0x0010u)]
    [InlineData(0x0019u, 0x0010u)]
    public void ResolveAsciiConversionMode_RemovesNativeConversionFlags(uint currentMode, uint expected)
    {
        var resolved = WindowsCommandInputImePolicy.ResolveAsciiConversionMode(currentMode);

        Assert.Equal(expected, resolved);
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
