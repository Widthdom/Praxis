using Praxis.Core.Logic;

namespace Praxis.Tests;

public class WindowsCommandInputImePolicyTests
{
    [Fact]
    public void ShouldForceAsciiImeMode_ReturnsTrue_WhenFocused()
    {
        Assert.True(WindowsCommandInputImePolicy.ShouldForceAsciiImeMode(isFocused: true));
    }

    [Fact]
    public void ShouldForceAsciiImeMode_ReturnsFalse_WhenNotFocused()
    {
        Assert.False(WindowsCommandInputImePolicy.ShouldForceAsciiImeMode(isFocused: false));
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
