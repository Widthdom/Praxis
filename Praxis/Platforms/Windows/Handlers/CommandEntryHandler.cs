#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
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
    private DispatcherQueueTimer? inputScopeEnforcementTimer;
    private TextBox? timerAttachedTextBox;
    private bool filteringText;

    protected override void ConnectHandler(TextBox platformView)
    {
        base.ConnectHandler(platformView);
        ApplyAlphanumericInputScope(platformView);
        platformView.GotFocus += PlatformView_GotFocus;
        platformView.LostFocus += PlatformView_LostFocus;
        platformView.TextChanging += PlatformView_TextChanging;
    }

    protected override void DisconnectHandler(TextBox platformView)
    {
        platformView.GotFocus -= PlatformView_GotFocus;
        platformView.LostFocus -= PlatformView_LostFocus;
        platformView.TextChanging -= PlatformView_TextChanging;
        StopInputScopeEnforcementTimer();
        base.DisconnectHandler(platformView);
    }

    private static InputScope CreateAlphanumericHalfWidthInputScope()
    {
        var scope = new InputScope();
        scope.Names.Add(new InputScopeName(InputScopeNameValue.AlphanumericHalfWidth));
        return scope;
    }

    private static void ApplyAlphanumericInputScope(TextBox textBox)
    {
        textBox.InputScope = AlphanumericHalfWidthInputScope;
    }

    private void PlatformView_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        ApplyAlphanumericInputScope(textBox);
        ForceAsciiImeMode(textBox);
        StartInputScopeEnforcementTimer(textBox);
    }

    private void PlatformView_LostFocus(object sender, RoutedEventArgs e)
    {
        StopInputScopeEnforcementTimer();
    }

    private void PlatformView_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        ApplyAlphanumericInputScope(sender);
        ForceAsciiImeMode(sender);

        if (filteringText)
        {
            return;
        }

        var current = sender.Text ?? string.Empty;
        if (AsciiInputFilter.IsAsciiOnly(current))
        {
            return;
        }

        var filtered = AsciiInputFilter.FilterToAscii(current);
        filteringText = true;
        try
        {
            sender.Text = filtered;
            sender.SelectionStart = WindowsCommandInputImePolicy.ClampSelectionStart(sender.SelectionStart, filtered.Length);
            sender.SelectionLength = 0;
        }
        finally
        {
            filteringText = false;
        }
    }

    private void StartInputScopeEnforcementTimer(TextBox textBox)
    {
        StopInputScopeEnforcementTimer();
        timerAttachedTextBox = textBox;
        var dispatcherQueue = textBox.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
        if (dispatcherQueue is null)
        {
            return;
        }

        var timer = dispatcherQueue.CreateTimer();
        timer.Interval = WindowsCommandInputImePolicy.FocusedInputScopeEnforcementInterval;
        timer.IsRepeating = true;
        timer.Tick += InputScopeEnforcementTimer_Tick;
        timer.Start();
        inputScopeEnforcementTimer = timer;
    }

    private void StopInputScopeEnforcementTimer()
    {
        var timer = inputScopeEnforcementTimer;
        if (timer is not null)
        {
            timer.Tick -= InputScopeEnforcementTimer_Tick;
            timer.Stop();
        }

        inputScopeEnforcementTimer = null;
        timerAttachedTextBox = null;
    }

    private void InputScopeEnforcementTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        var textBox = timerAttachedTextBox;
        if (textBox is null || !WindowsCommandInputImePolicy.ShouldEnforceInputScope(textBox.FocusState != FocusState.Unfocused))
        {
            StopInputScopeEnforcementTimer();
            return;
        }

        ApplyAlphanumericInputScope(textBox);
        ForceAsciiImeMode(textBox);
    }

    private static void ForceAsciiImeMode(TextBox textBox)
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
                    var matchedWindowHandle = WindowNative.GetWindowHandle(nativeWindow);
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
                    var fallbackWindowHandle = WindowNative.GetWindowHandle(nativeWindow);
                    if (fallbackWindowHandle != IntPtr.Zero)
                    {
                        return fallbackWindowHandle;
                    }
                }
            }
        }

        return IntPtr.Zero;
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
