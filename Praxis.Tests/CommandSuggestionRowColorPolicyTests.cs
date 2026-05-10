using Praxis.Core.Logic;

namespace Praxis.Tests;

public class CommandSuggestionRowColorPolicyTests
{
    [Theory]
    [InlineData(false, false, "#00000000")]
    [InlineData(false, true, "#00000000")]
    [InlineData(true, false, "#E6E6E6")]
    [InlineData(true, true, "#3D3D3D")]
    public void ResolveBackgroundHex_ReturnsExpectedColor(bool selected, bool isDarkTheme, string expected)
    {
        var actual = CommandSuggestionRowColorPolicy.ResolveBackgroundHex(selected, isDarkTheme);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResolveBackgroundHex_ReturnsTransparent_WhenRowIsNotSelected(bool isDarkTheme)
    {
        var actual = CommandSuggestionRowColorPolicy.ResolveBackgroundHex(selected: false, isDarkTheme);
        Assert.Equal("#00000000", actual);
    }

    [Fact]
    public void ResolveBackgroundHex_ReturnsDistinctSelectedColors_ForLightAndDarkThemes()
    {
        var light = CommandSuggestionRowColorPolicy.ResolveBackgroundHex(selected: true, isDarkTheme: false);
        var dark = CommandSuggestionRowColorPolicy.ResolveBackgroundHex(selected: true, isDarkTheme: true);

        Assert.NotEqual(light, dark);
        Assert.Equal("#E6E6E6", light);
        Assert.Equal("#3D3D3D", dark);
    }
}
