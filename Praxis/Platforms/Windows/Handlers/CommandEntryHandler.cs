#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Praxis.Core.Logic;
using Praxis.Services;
using WinRT.Interop;

namespace Praxis.Controls;

public class CommandEntryHandler : EntryHandler
{
    private static readonly InputScope AlphanumericHalfWidthInputScope = CreateAlphanumericHalfWidthInputScope();
    private bool inputScopeUnsupported;
    private CancellationTokenSource? focusedAsciiImeReassertCts;

    protected override void ConnectHandler(TextBox platformView)
    {
        base.ConnectHandler(platformView);
        WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(platformView);
        TryApplyAlphanumericInputScope(platformView);
        platformView.GotFocus += PlatformView_GotFocus;
        platformView.LostFocus += PlatformView_LostFocus;
        platformView.SelectionChanged += PlatformView_SelectionChanged;
    }

    protected override void DisconnectHandler(TextBox platformView)
    {
        platformView.GotFocus -= PlatformView_GotFocus;
        platformView.LostFocus -= PlatformView_LostFocus;
        platformView.SelectionChanged -= PlatformView_SelectionChanged;
        StopFocusedAsciiImeReassert();
        base.DisconnectHandler(platformView);
    }

    private static InputScope CreateAlphanumericHalfWidthInputScope()
    {
        var scope = new InputScope();
        scope.Names.Add(new InputScopeName(InputScopeNameValue.AlphanumericHalfWidth));
        return scope;
    }

    private bool TryApplyAlphanumericInputScope(TextBox textBox)
    {
        if (!IsAsciiInputEnforcementEnabled())
        {
            return false;
        }

        if (inputScopeUnsupported)
        {
            return false;
        }

        try
        {
            textBox.InputScope = AlphanumericHalfWidthInputScope;
            return true;
        }
        catch (Exception ex) when (WindowsInputScopeCompatibilityPolicy.ShouldDisableInputScopeOnException(ex))
        {
            // Some WinUI environments throw (for example E_RUNTIME_SETVALUE) when InputScope is assigned.
            // Mark as unsupported so later focuses skip InputScope writes and rely only on imm32 IME fallback.
            inputScopeUnsupported = true;
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            var enforceAsciiInput = (VirtualView as CommandEntry)?.EnforceAsciiInput ?? false;
            var textBoxType = textBox.GetType().Name;
            CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $"InputScope assignment disabled after compatibility failure while enforceAsciiInput={enforceAsciiInput} textBoxType={textBoxType}: {safeMessage}");
            return false;
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            var enforceAsciiInput = (VirtualView as CommandEntry)?.EnforceAsciiInput ?? false;
            var textBoxType = textBox.GetType().Name;
            CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $"InputScope assignment failed unexpectedly while enforceAsciiInput={enforceAsciiInput} textBoxType={textBoxType}: {safeMessage}");
            return false;
        }
    }

    private void PlatformView_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        _ = TryApplyAlphanumericInputScope(textBox);
        WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(textBox);
        ApplyAsciiImeModeForFocus(textBox);
        TryStartFocusedAsciiImeReassert(textBox);
    }

    private void PlatformView_LostFocus(object sender, RoutedEventArgs e)
    {
        StopFocusedAsciiImeReassert();
        if (sender is TextBox textBox)
        {
            WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(textBox);
        }
    }

    private static void PlatformView_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(textBox);
        }
    }

    private void ApplyAsciiImeModeForFocus(TextBox textBox)
    {
        var delays = WindowsCommandInputImePolicy.ResolveAsciiImeNudgeDelays(
            isFocused: textBox.FocusState != FocusState.Unfocused,
            enforceAsciiInput: IsAsciiInputEnforcementEnabled());

        foreach (var delay in delays)
        {
            if (delay <= TimeSpan.Zero)
            {
                ForceAsciiImeModeOnce(textBox);
                continue;
            }

            _ = QueueAsciiImeModeNudgeAsync(textBox, delay);
        }
    }

    private async Task QueueAsciiImeModeNudgeAsync(TextBox textBox, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay).ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        var dispatcherQueue = textBox.DispatcherQueue;
        if (dispatcherQueue is null)
        {
            return;
        }

        _ = dispatcherQueue.TryEnqueue(() =>
        {
            if (textBox.FocusState == FocusState.Unfocused)
            {
                return;
            }

            ForceAsciiImeModeOnce(textBox);
        });
    }

    private void TryStartFocusedAsciiImeReassert(TextBox textBox)
    {
        StopFocusedAsciiImeReassert();
        var keepAsciiImeWhileFocused = (VirtualView as CommandEntry)?.KeepAsciiImeWhileFocused ?? false;
        if (!WindowsCommandInputImePolicy.ShouldReassertAsciiImeMode(
                isFocused: textBox.FocusState != FocusState.Unfocused,
                keepAsciiImeWhileFocused: keepAsciiImeWhileFocused,
                enforceAsciiInput: IsAsciiInputEnforcementEnabled()))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        focusedAsciiImeReassertCts = cts;
        _ = RunFocusedAsciiImeReassertLoopAsync(textBox, cts.Token);
    }

    private void StopFocusedAsciiImeReassert()
    {
        focusedAsciiImeReassertCts?.Cancel();
        focusedAsciiImeReassertCts?.Dispose();
        focusedAsciiImeReassertCts = null;
    }

    private async Task RunFocusedAsciiImeReassertLoopAsync(TextBox textBox, CancellationToken cancellationToken)
    {
        var interval = WindowsCommandInputImePolicy.ResolveAsciiImeReassertInterval();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var dispatcherQueue = textBox.DispatcherQueue;
            if (dispatcherQueue is null)
            {
                return;
            }

            var enqueued = dispatcherQueue.TryEnqueue(() =>
            {
                if (textBox.FocusState == FocusState.Unfocused)
                {
                    return;
                }

                ForceAsciiImeModeOnce(textBox);
            });

            if (!enqueued)
            {
                return;
            }
        }
    }

    private void ForceAsciiImeModeOnce(TextBox textBox)
    {
        if (!WindowsCommandInputImePolicy.ShouldForceAsciiImeMode(
                textBox.FocusState != FocusState.Unfocused,
                IsAsciiInputEnforcementEnabled()))
        {
            return;
        }

        var windowHandle = ResolveWindowHandle(textBox);
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        var imeContext = ImmGetContext(windowHandle);
        if (imeContext == IntPtr.Zero)
        {
            return;
        }

        try
        {
            if (ImmGetOpenStatus(imeContext))
            {
                _ = ImmSetOpenStatus(imeContext, false);
            }

            if (ImmGetConversionStatus(imeContext, out var currentConversionMode, out var sentenceMode))
            {
                var asciiConversionMode = WindowsCommandInputImePolicy.ResolveAsciiConversionMode(currentConversionMode);
                if (asciiConversionMode != currentConversionMode)
                {
                    _ = ImmSetConversionStatus(imeContext, asciiConversionMode, sentenceMode);
                }
            }
        }
        finally
        {
            _ = ImmReleaseContext(windowHandle, imeContext);
        }
    }

    private static IntPtr ResolveWindowHandle(TextBox textBox)
    {
        var xamlRoot = textBox.XamlRoot;
        if (Microsoft.Maui.Controls.Application.Current?.Windows is { Count: > 0 } windows)
        {
            foreach (var mauiWindow in windows)
            {
                if (mauiWindow.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
                {
                    continue;
                }

                if (nativeWindow.Content is FrameworkElement rootElement &&
                    ReferenceEquals(rootElement.XamlRoot, xamlRoot))
                {
                    var matchedWindowHandle = TryGetWindowHandle(nativeWindow);
                    if (matchedWindowHandle != IntPtr.Zero)
                    {
                        return matchedWindowHandle;
                    }
                }
            }

            foreach (var mauiWindow in windows)
            {
                if (mauiWindow.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    var fallbackWindowHandle = TryGetWindowHandle(nativeWindow);
                    if (fallbackWindowHandle != IntPtr.Zero)
                    {
                        return fallbackWindowHandle;
                    }
                }
            }
        }

        return IntPtr.Zero;
    }

    private bool IsAsciiInputEnforcementEnabled()
        => (VirtualView as CommandEntry)?.EnforceAsciiInput ?? false;

    private static IntPtr TryGetWindowHandle(Microsoft.UI.Xaml.Window nativeWindow)
    {
        try
        {
            return WindowNative.GetWindowHandle(nativeWindow);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetContext(IntPtr hWnd);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmGetOpenStatus(IntPtr hIMC);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmSetOpenStatus(IntPtr hIMC, [MarshalAs(UnmanagedType.Bool)] bool open);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmGetConversionStatus(IntPtr hIMC, out uint conversionMode, out uint sentenceMode);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmSetConversionStatus(IntPtr hIMC, uint conversionMode, uint sentenceMode);
}

internal static class WindowsTextBoxVisualPolicy
{
    public static void ApplyInputCaretContrast(TextBox textBox)
    {
        Praxis.App.RefreshWindowsTextSelectionResources();
        var foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveInputForegroundColor());
        var selection = new Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveSelectionColor());
        var selectionForegroundColor = ResolveSelectionForegroundColor();
        var selectionForeground = new Microsoft.UI.Xaml.Media.SolidColorBrush(selectionForegroundColor);
        var caret = new Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveCaretColor());
        var chrome = new Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
        textBox.RequestedTheme = IsInputDarkThemeActive()
            ? ElementTheme.Dark
            : ElementTheme.Light;
        textBox.Background = chrome;
        textBox.BorderBrush = chrome;
        textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        textBox.Foreground = foreground;
        textBox.SelectionHighlightColor = selection;
        textBox.SelectionHighlightColorWhenNotFocused = selection;
        ApplyTextControlResources(textBox, foreground, selection, selectionForegroundColor, selectionForeground, caret, chrome);

        _ = textBox.DispatcherQueue.TryEnqueue(() =>
        {
            textBox.RequestedTheme = IsInputDarkThemeActive()
                ? ElementTheme.Dark
                : ElementTheme.Light;
            textBox.Background = chrome;
            textBox.BorderBrush = chrome;
            textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            var refreshedForeground = new Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveInputForegroundColor());
            var refreshedSelection = new Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveSelectionColor());
            textBox.Foreground = refreshedForeground;
            textBox.SelectionHighlightColor = refreshedSelection;
            textBox.SelectionHighlightColorWhenNotFocused = refreshedSelection;
            ApplyTextControlResources(textBox, refreshedForeground, refreshedSelection, selectionForegroundColor, selectionForeground, caret, chrome);
        });
    }

    private static void ApplyTextControlResources(
        TextBox textBox,
        Microsoft.UI.Xaml.Media.Brush foreground,
        Microsoft.UI.Xaml.Media.Brush selection,
        global::Windows.UI.Color selectionForegroundColor,
        Microsoft.UI.Xaml.Media.Brush selectionForeground,
        Microsoft.UI.Xaml.Media.Brush caret,
        Microsoft.UI.Xaml.Media.Brush chrome)
    {
        TrySetTextControlResource(textBox, "TextControlBackground", chrome);
        TrySetTextControlResource(textBox, "TextControlBackgroundPointerOver", chrome);
        TrySetTextControlResource(textBox, "TextControlBackgroundFocused", chrome);
        TrySetTextControlResource(textBox, "TextControlBorderBrush", chrome);
        TrySetTextControlResource(textBox, "TextControlBorderBrushPointerOver", chrome);
        TrySetTextControlResource(textBox, "TextControlBorderBrushFocused", chrome);
        TrySetTextControlResource(textBox, "TextControlForeground", foreground);
        TrySetTextControlResource(textBox, "TextControlForegroundPointerOver", foreground);
        TrySetTextControlResource(textBox, "TextControlForegroundFocused", foreground);
        TrySetTextControlResource(textBox, "TextControlSelectionHighlightColor", selection);
        TrySetTextControlResource(textBox, "TextControlSelectionHighlightForeground", selectionForeground);
        TrySetTextControlResource(textBox, "TextSelectionHighlightColorThemeBrush", selection);
        TrySetTextControlResource(textBox, "AccentFillColorSelectedTextBackgroundBrush", selection);
        TrySetTextControlResource(textBox, "TextOnAccentFillColorSelectedText", selectionForegroundColor);
        TrySetTextControlResource(textBox, "TextOnAccentFillColorSelectedTextBrush", selectionForeground);
        TrySetTextControlResource(textBox, "SystemColorHighlightTextColor", selectionForegroundColor);
        TrySetTextControlResource(textBox, "SystemColorHighlightTextColorBrush", selectionForeground);
        TrySetTextControlResource(textBox, "TextControlCaretBrush", caret);
        TrySetTextControlResource(textBox, "TextControlCaretBrushFocused", caret);
    }

    private static void TrySetTextControlResource(TextBox textBox, string key, object value)
    {
        try
        {
            textBox.Resources[key] = value;
        }
        catch (Exception ex)
        {
            var safeResourceMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(WindowsTextBoxVisualPolicy), $"Failed to set {key}: {safeResourceMessage}");
        }
    }

    private static global::Windows.UI.Color ResolveInputForegroundColor()
    {
        return IsInputDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(255, 245, 247, 250)
            : global::Windows.UI.Color.FromArgb(255, 0, 0, 0);
    }

    private static global::Windows.UI.Color ResolveSelectionColor()
    {
        return IsInputDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(255, 82, 58, 124)
            : global::Windows.UI.Color.FromArgb(255, 107, 75, 176);
    }

    private static global::Windows.UI.Color ResolveSelectionForegroundColor()
    {
        return global::Windows.UI.Color.FromArgb(255, 245, 247, 250);
    }

    private static global::Windows.UI.Color ResolveCaretColor()
    {
        return IsInputDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(255, 245, 247, 250)
            : global::Windows.UI.Color.FromArgb(255, 0, 0, 0);
    }

    private static bool IsInputDarkThemeActive()
    {
        return Microsoft.Maui.Controls.Application.Current?.UserAppTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark ||
            Microsoft.Maui.Controls.Application.Current?.UserAppTheme == Microsoft.Maui.ApplicationModel.AppTheme.Unspecified &&
            Microsoft.Maui.Controls.Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
    }
}
#endif
