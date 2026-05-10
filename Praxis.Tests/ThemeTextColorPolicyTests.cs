using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ThemeTextColorPolicyTests
{
    [Theory]
    [InlineData(false, "#111111")]
    [InlineData(true, "#F2F2F2")]
    public void ResolveTextColorHex_ReturnsExpectedPaletteValue(bool isDarkTheme, string expected)
    {
        var hex = ThemeTextColorPolicy.ResolveTextColorHex(isDarkTheme);
        Assert.Equal(expected, hex);
    }

    [Fact]
    public void ResolveTextColorHex_ReturnsDistinctColors_ForLightAndDarkThemes()
    {
        var light = ThemeTextColorPolicy.ResolveTextColorHex(isDarkTheme: false);
        var dark = ThemeTextColorPolicy.ResolveTextColorHex(isDarkTheme: true);

        Assert.NotEqual(light, dark);
        Assert.StartsWith("#", light);
        Assert.StartsWith("#", dark);
        Assert.Equal(7, light.Length);
        Assert.Equal(7, dark.Length);
    }
}
