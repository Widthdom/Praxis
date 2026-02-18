using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ThemeTextColorPolicyTests
{
    [Fact]
    public void ResolveTextColorHex_ReturnsDarkText_ForLightTheme()
    {
        var hex = ThemeTextColorPolicy.ResolveTextColorHex(isDarkTheme: false);
        Assert.Equal("#111111", hex);
    }

    [Fact]
    public void ResolveTextColorHex_ReturnsLightText_ForDarkTheme()
    {
        var hex = ThemeTextColorPolicy.ResolveTextColorHex(isDarkTheme: true);
        Assert.Equal("#F2F2F2", hex);
    }
}
