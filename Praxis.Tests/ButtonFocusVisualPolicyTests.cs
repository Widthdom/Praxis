using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ButtonFocusVisualPolicyTests
{
    [Fact]
    public void ResolveBorderWidth_ReturnsConstantWidth()
    {
        Assert.Equal(1.5, ButtonFocusVisualPolicy.ResolveBorderWidth());
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
}
