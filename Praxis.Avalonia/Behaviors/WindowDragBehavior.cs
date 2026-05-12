using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Praxis.Avalonia.Behaviors;

public sealed class WindowDragBehavior
{
    private WindowDragBehavior()
    {
    }

    public static readonly AttachedProperty<bool> IsDragAreaProperty =
        AvaloniaProperty.RegisterAttached<WindowDragBehavior, InputElement, bool>("IsDragArea");

    static WindowDragBehavior()
    {
        IsDragAreaProperty.Changed.AddClassHandler<InputElement>(OnIsDragAreaChanged);
    }

    public static bool GetIsDragArea(InputElement element)
        => element.GetValue(IsDragAreaProperty);

    public static void SetIsDragArea(InputElement element, bool value)
        => element.SetValue(IsDragAreaProperty, value);

    private static void OnIsDragAreaChanged(InputElement element, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.GetNewValue<bool>())
        {
            element.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        }
        else
        {
            element.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        }
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not InputElement dragArea || IsInteractiveChild(e.Source, dragArea))
        {
            return;
        }

        var point = e.GetCurrentPoint(dragArea);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (TopLevel.GetTopLevel(dragArea) is Window window)
        {
            if (e.ClickCount == 2)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                e.Handled = true;
                return;
            }

            window.BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    private static bool IsInteractiveChild(object? source, InputElement dragArea)
    {
        if (ReferenceEquals(source, dragArea))
        {
            return false;
        }

        var current = source as Control;
        while (current is not null && !ReferenceEquals(current, dragArea))
        {
            if (current is Button or TextBox or SelectingItemsControl
                || current is Control { Focusable: true })
            {
                return true;
            }

            current = current.Parent as Control;
        }

        return false;
    }
}
