#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using Praxis.Services;

namespace Praxis;

public partial class App
{
    static partial void ApplyPlatformWindowBackdrop(Microsoft.Maui.Controls.Window window)
    {
        ApplyWindowsWindowBackdrop(window);
    }

    private static void ApplyWindowsWindowBackdrop(Microsoft.Maui.Controls.Window window)
    {
        try
        {
            if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
            {
                return;
            }

            var hwnd = WindowNative.GetWindowHandle(nativeWindow);
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            ApplyWindowsRootTransparency(nativeWindow);
            ApplyWindowsTitleBarTransparency(nativeWindow);
            ApplyWindowsAcrylicBackdrop(hwnd);
            ApplyWindowsRoundedCorners(hwnd);
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to apply Windows glass backdrop: {safeMessage}");
        }
    }

    private static void ApplyWindowsRootTransparency(Microsoft.UI.Xaml.Window nativeWindow)
    {
        var transparentBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));

        if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Panel panel)
        {
            panel.Background = transparentBrush;
        }

        if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Control control)
        {
            control.Background = transparentBrush;
        }

        if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Border border)
        {
            border.Background = transparentBrush;
        }
    }

    private static void ApplyWindowsTitleBarTransparency(Microsoft.UI.Xaml.Window nativeWindow)
    {
        try
        {
            var transparent = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
            var titleBar = nativeWindow.AppWindow.TitleBar;
            titleBar.BackgroundColor = transparent;
            titleBar.InactiveBackgroundColor = transparent;
            titleBar.ButtonBackgroundColor = transparent;
            titleBar.ButtonInactiveBackgroundColor = transparent;
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to make Windows title bar transparent: {safeMessage}");
        }
    }

    private static void ApplyWindowsAcrylicBackdrop(IntPtr hwnd)
    {
        var isDark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
        var tintColor = isDark ? 0xC8161B20u : 0xD9F7FBFFu;
        var accent = new AccentPolicy
        {
            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = 2,
            GradientColor = unchecked((int)tintColor),
            AnimationId = 0
        };

        var accentStructSize = Marshal.SizeOf(accent);
        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        try
        {
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };
            SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }
    }

    private static void ApplyWindowsRoundedCorners(IntPtr hwnd)
    {
        var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(uint));
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_ENABLE_HOSTBACKDROP = 5,
        ACCENT_ENABLE_ACRYLIC = 6,
        ACCENT_ENABLE_HOSTBACKDROP_ACRYLIC = 7
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }

    public enum DWMWINDOWATTRIBUTE
    {
        DWMWA_WINDOW_CORNER_PREFERENCE = 33
    }

    public enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }

    [DllImport("user32.dll")]
    internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    internal static extern void DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute, uint cbAttribute);
}
#endif
