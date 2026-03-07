using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public static class ThemeDarkStateResolver
{
    public static bool Resolve(ThemeMode selectedTheme, bool requestedThemeDark, bool? platformTraitDark = null)
    {
        return selectedTheme switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            _ => platformTraitDark ?? requestedThemeDark,
        };
    }
}
