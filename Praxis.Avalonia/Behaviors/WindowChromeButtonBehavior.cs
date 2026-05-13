using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Praxis.Avalonia.Behaviors;

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
                window.WindowState = WindowState.Minimized;
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

                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                break;
            case WindowChromeAction.Close:
                window.Close();
                break;
        }

        e.Handled = true;
    }
}
