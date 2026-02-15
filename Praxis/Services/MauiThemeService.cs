using Praxis.Core.Models;

namespace Praxis.Services;

public sealed class MauiThemeService : IThemeService
{
    public ThemeMode Current
    {
        get
        {
            var theme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
            return theme switch
            {
                AppTheme.Light => ThemeMode.Light,
                AppTheme.Dark => ThemeMode.Dark,
                _ => ThemeMode.System,
            };
        }
    }

    public void Apply(ThemeMode mode)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.UserAppTheme = mode switch
        {
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };
    }
}
