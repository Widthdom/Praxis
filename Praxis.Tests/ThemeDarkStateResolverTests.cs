using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class ThemeDarkStateResolverTests
{
    [Theory]
    [InlineData(ThemeMode.Dark, false, null, true)]
    [InlineData(ThemeMode.Light, true, null, false)]
    [InlineData(ThemeMode.System, false, null, false)]
    [InlineData(ThemeMode.System, true, null, true)]
    [InlineData(ThemeMode.System, false, true, true)]
    [InlineData(ThemeMode.System, true, false, false)]
    public void Resolve_ReturnsExpectedThemeDarkState(
        ThemeMode selectedTheme,
        bool requestedThemeDark,
        bool? platformTraitDark,
        bool expected)
    {
        var actual = ThemeDarkStateResolver.Resolve(selectedTheme, requestedThemeDark, platformTraitDark);
        Assert.Equal(expected, actual);
    }
}
