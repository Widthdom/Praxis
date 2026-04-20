using Praxis.Core.Models;
#if MACCATALYST
using Microsoft.Maui.ApplicationModel;
using UIKit;
#endif

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

        var appTheme = mode switch
        {
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };

        if (Application.Current.UserAppTheme == appTheme)
        {
            return;
        }

        Application.Current.UserAppTheme = appTheme;
#if MACCATALYST
        ApplyMacWindowStyle(appTheme);
#endif
    }

#if MACCATALYST
    private static void ApplyMacWindowStyle(AppTheme appTheme)
    {
        void apply()
        {
            var style = appTheme switch
            {
                AppTheme.Light => UIUserInterfaceStyle.Light,
                AppTheme.Dark => UIUserInterfaceStyle.Dark,
                _ => UIUserInterfaceStyle.Unspecified,
            };

            foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
            {
                if (scene is not UIWindowScene windowScene)
                {
                    continue;
                }

                foreach (var window in windowScene.Windows)
                {
                    window.OverrideUserInterfaceStyle = style;
                }
            }
        }

        if (MainThread.IsMainThread)
        {
            apply();
            return;
        }

        try
        {
            MainThread.BeginInvokeOnMainThread(apply);
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
            CrashFileLogger.WriteWarning(nameof(MauiThemeService), $"ApplyMacWindowStyle dispatch failed for theme '{appTheme}' while currentTheme='{currentTheme}': {safeMessage}");
        }
    }
#endif
}
