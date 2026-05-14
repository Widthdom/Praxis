using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Praxis.Avalonia.Behaviors;

public enum WindowChromeAction
{
    None,
    Minimize,
    ToggleMaximize,
    Close,
}

public sealed class WindowChromeButtonBehavior
{
    private const int ShowWindowMaximize = 3;
    private const int ShowWindowMinimize = 6;
    private const int ShowWindowRestore = 9;
    private const string User32Library = "user32.dll";

    private WindowChromeButtonBehavior()
    {
    }

    public static readonly AttachedProperty<WindowChromeAction> ActionProperty =
        AvaloniaProperty.RegisterAttached<WindowChromeButtonBehavior, Button, WindowChromeAction>("Action");

    static WindowChromeButtonBehavior()
    {
        ActionProperty.Changed.AddClassHandler<Button>(OnActionChanged);
    }

    public static WindowChromeAction GetAction(Button button)
        => button.GetValue(ActionProperty);

    public static void SetAction(Button button, WindowChromeAction value)
        => button.SetValue(ActionProperty, value);

    private static void OnActionChanged(Button button, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.GetOldValue<WindowChromeAction>() != WindowChromeAction.None)
        {
            button.RemoveHandler(Button.ClickEvent, OnClick);
        }

        if (args.GetNewValue<WindowChromeAction>() != WindowChromeAction.None)
        {
            button.AddHandler(Button.ClickEvent, OnClick);
        }
    }

    private static void OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || TopLevel.GetTopLevel(button) is not Window window)
        {
            return;
        }

        switch (GetAction(button))
        {
            case WindowChromeAction.Minimize:
                if (!TryShowWindowsWindow(window, ShowWindowMinimize))
                {
                    window.WindowState = WindowState.Minimized;
                }

                break;
            case WindowChromeAction.ToggleMaximize:
                if (OperatingSystem.IsMacOS())
                {
                    MainWindowInteractionBehavior.ToggleMacFullScreen(window);
                    break;
                }

                if (window.WindowState == WindowState.Normal)
                {
                    MainWindowInteractionBehavior.CaptureNormalBoundsBeforeMaximize(window);
                }

                if (OperatingSystem.IsWindows())
                {
                    var command = window.WindowState == WindowState.Maximized
                        ? ShowWindowRestore
                        : ShowWindowMaximize;
                    if (TryShowWindowsWindow(window, command))
                    {
                        break;
                    }
                }

                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                break;
            case WindowChromeAction.Close:
                window.Close();
                break;
        }

        e.Handled = true;
    }

    private static bool TryShowWindowsWindow(Window window, int command)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var handle = window.TryGetPlatformHandle();
        return handle is not null
            && handle.Handle != IntPtr.Zero
            && ShowWindow(handle.Handle, command);
    }

    [DllImport(User32Library)]
    private static extern bool ShowWindow(IntPtr hwnd, int command);
}
