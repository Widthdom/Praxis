using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class ThemeDarkStateResolverTests
{
    [Theory]
    [InlineData(ThemeMode.Dark, false, null, true)]
    [InlineData(ThemeMode.Dark, false, false, true)]
    [InlineData(ThemeMode.Dark, true, true, true)]
    [InlineData(ThemeMode.Light, true, null, false)]
    [InlineData(ThemeMode.Light, true, true, false)]
    [InlineData(ThemeMode.Light, false, false, false)]
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

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(true, null, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    public void Resolve_TreatsUndefinedThemeMode_AsSystemFallback(bool requestedThemeDark, bool? platformTraitDark, bool expected)
    {
        var actual = ThemeDarkStateResolver.Resolve((ThemeMode)999, requestedThemeDark, platformTraitDark);
        Assert.Equal(expected, actual);
    }
}
