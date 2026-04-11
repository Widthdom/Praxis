using Praxis.Core.Logic;
using Praxis.Core.Models;
#if MACCATALYST
using UIKit;
#endif

namespace Praxis;

public partial class MainPage
{
    private async void TriggerStatusFlash(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || string.Equals(message, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        statusFlashCts?.Cancel();
        statusFlashCts?.Dispose();
        statusFlashCts = new CancellationTokenSource();
        var token = statusFlashCts.Token;
        var neutral = GetNeutralStatusBackgroundColor();
        var flash = StatusFlashErrorPolicy.IsErrorStatus(message)
            ? Color.FromArgb("#D94A4A")
            : Color.FromArgb("#4AAE6A");

        try
        {
            await AnimateStatusBackgroundAsync(neutral, flash, UiTimingPolicy.StatusFlashInDurationMs, Easing.CubicOut, token);
            token.ThrowIfCancellationRequested();
            await AnimateStatusBackgroundAsync(flash, neutral, UiTimingPolicy.StatusFlashOutDurationMs, Easing.CubicIn, token);
            token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(TriggerStatusFlash), $"Status flash animation failed: {ex.Message}");
        }
        finally
        {
            ResetStatusBarBackgroundToThemeBinding();
        }
    }

#if MACCATALYST
    private void RefocusMainCommandAfterCommandNotFound(string? statusMessage)
    {
        if (!CommandNotFoundRefocusPolicy.ShouldRefocusMainCommand(statusMessage))
        {
            return;
        }

        Dispatcher.DispatchDelayed(UiTimingPolicy.CommandNotFoundRefocusDelay, () =>
        {
            if (!xamlLoaded || viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
            {
                return;
            }

            ApplyEntryFocusAfterClearButtonTap(MainCommandEntry);
            EnsureMacFirstResponder();
        });
    }
#endif

    private async Task AnimateStatusBackgroundAsync(Color from, Color to, uint durationMs, Easing easing, CancellationToken token)
    {
        const int steps = 10;
        for (var i = 1; i <= steps; i++)
        {
            token.ThrowIfCancellationRequested();
            var t = (double)i / steps;
            var eased = easing.Ease(t);
            StatusBarBorder.BackgroundColor = LerpColor(from, to, eased);
            await Task.Delay((int)Math.Max(1, durationMs / steps), token);
        }
    }

    private static Color LerpColor(Color from, Color to, double t)
    {
        static float Lerp(float a, float b, double ratio) => (float)(a + (b - a) * ratio);
        return new Color(
            Lerp(from.Red, to.Red, t),
            Lerp(from.Green, to.Green, t),
            Lerp(from.Blue, to.Blue, t),
            Lerp(from.Alpha, to.Alpha, t));
    }

    private Color GetNeutralStatusBackgroundColor()
    {
#if WINDOWS
        if (pageNativeElement is Microsoft.UI.Xaml.FrameworkElement fe)
        {
            var actual = fe.ActualTheme;
            if (actual == Microsoft.UI.Xaml.ElementTheme.Dark)
            {
                return Color.FromArgb("#1E1E1E");
            }

            if (actual == Microsoft.UI.Xaml.ElementTheme.Light)
            {
                return Color.FromArgb("#F2F2F2");
            }
        }
#endif
        var theme = viewModel.SelectedTheme switch
        {
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            _ => Application.Current?.RequestedTheme ?? AppTheme.Unspecified,
        };

        return theme == AppTheme.Dark
            ? Color.FromArgb("#1E1E1E")
            : Color.FromArgb("#F2F2F2");
    }

    private bool IsDarkThemeActive()
    {
        var selectedTheme = Application.Current?.UserAppTheme switch
        {
            AppTheme.Dark => ThemeMode.Dark,
            AppTheme.Light => ThemeMode.Light,
            _ => ThemeMode.System,
        };

        var requestedThemeDark = Application.Current?.RequestedTheme == AppTheme.Dark;
#if MACCATALYST
        bool? traitDark = (Window?.Handler?.PlatformView as UIWindow)?.TraitCollection?.UserInterfaceStyle switch
        {
            UIUserInterfaceStyle.Dark => true,
            UIUserInterfaceStyle.Light => false,
            _ => null,
        };
#else
        bool? traitDark = null;
#endif
        return ThemeDarkStateResolver.Resolve(selectedTheme, requestedThemeDark, traitDark);
    }

    private void ApplyNeutralStatusBackground()
    {
        ResetStatusBarBackgroundToThemeBinding();
    }

    private void ResetStatusBarBackgroundToThemeBinding()
    {
        StatusBarBorder.ClearValue(VisualElement.BackgroundColorProperty);
    }
}
