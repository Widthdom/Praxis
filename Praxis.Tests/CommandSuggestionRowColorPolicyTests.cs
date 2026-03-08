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
}
