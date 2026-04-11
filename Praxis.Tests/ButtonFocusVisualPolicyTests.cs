using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ButtonFocusVisualPolicyTests
{
    [Fact]
    public void ResolveBorderWidth_ReturnsPositiveConstantWidth()
    {
        var first = ButtonFocusVisualPolicy.ResolveBorderWidth();
        var second = ButtonFocusVisualPolicy.ResolveBorderWidth();

        Assert.Equal(1.5, first);
        Assert.Equal(first, second);
        Assert.True(first > 0);
    }

    [Theory]
    [InlineData(false, false, "#00000000")]
    [InlineData(false, true, "#00000000")]
    [InlineData(true, false, "#1A1A1A")]
    [InlineData(true, true, "#F2F2F2")]
    public void ResolveBorderColorHex_ReturnsExpectedColor(bool focused, bool isDarkTheme, string expected)
    {
        var color = ButtonFocusVisualPolicy.ResolveBorderColorHex(focused, isDarkTheme);
        Assert.Equal(expected, color);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResolveBorderColorHex_ReturnsTransparent_WhenNotFocused_RegardlessOfTheme(bool isDarkTheme)
    {
        var color = ButtonFocusVisualPolicy.ResolveBorderColorHex(focused: false, isDarkTheme);
        Assert.Equal("#00000000", color);
    }
}
