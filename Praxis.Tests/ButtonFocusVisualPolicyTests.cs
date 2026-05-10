using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ButtonFocusVisualPolicyTests
{
    [Fact]
    public void ResolveBorderWidth_IsZero_SoFocusVisualNeverTriggersLabelJitter()
    {
        var width = ButtonFocusVisualPolicy.ResolveBorderWidth();

        Assert.Equal(0, width);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ResolveBorderColorHex_IsAlwaysTransparent(bool focused, bool isDarkTheme)
    {
        var color = ButtonFocusVisualPolicy.ResolveBorderColorHex(focused, isDarkTheme);

        Assert.Equal("#00000000", color);
    }

    [Theory]
    [InlineData(false, false, "#00000000")]
    [InlineData(false, true, "#00000000")]
    [InlineData(true, false, "#E6E6E6")]
    [InlineData(true, true, "#3D3D3D")]
    public void ResolveBackgroundColorHex_TintsTheButtonOnlyWhenFocused(bool focused, bool isDarkTheme, string expected)
    {
        var color = ButtonFocusVisualPolicy.ResolveBackgroundColorHex(focused, isDarkTheme);

        Assert.Equal(expected, color);
    }
}
