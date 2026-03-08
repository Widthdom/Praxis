#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Praxis.Core.Logic;
using WinRT.Interop;

namespace Praxis.Controls;

public class CommandEntryHandler : EntryHandler
{
    private static readonly InputScope AlphanumericHalfWidthInputScope = CreateAlphanumericHalfWidthInputScope();
    private bool inputScopeUnsupported;

    protected override void ConnectHandler(TextBox platformView)
    {
        base.ConnectHandler(platformView);
        TryApplyAlphanumericInputScope(platformView);
        platformView.GotFocus += PlatformView_GotFocus;
    }

    protected override void DisconnectHandler(TextBox platformView)
    {
        platformView.GotFocus -= PlatformView_GotFocus;
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
            return false;
        }
        catch
        {
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
        ForceAsciiImeModeOnce(textBox);
    }

    private static void ForceAsciiImeModeOnce(TextBox textBox)
    {
        if (!WindowsCommandInputImePolicy.ShouldForceAsciiImeMode(textBox.FocusState != FocusState.Unfocused))
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
#endif
