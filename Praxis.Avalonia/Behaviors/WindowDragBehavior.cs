using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Praxis.Avalonia.Behaviors;

public sealed class WindowDragBehavior
{
    private const string CoreGraphicsLibrary = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private static readonly TimeSpan DoubleClickWindow = TimeSpan.FromMilliseconds(800);
    private const double DoubleClickDistance = 24;
    private const double MoveDragThreshold = 12;
    private static readonly Dictionary<IPointer, MacMoveDragState> MacMoveDrags = [];
    private static DateTimeOffset lastClickTime;
    private static Window? lastClickWindow;
    private static Point lastClickPosition;

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
            element.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
            element.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        }
        else
        {
            element.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            element.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
            element.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
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
            var windowPoint = e.GetPosition(window);
            if (IsTitleBarDoubleClick(window, windowPoint))
            {
                ToggleNormalMaximize(window);
                ClearTitleBarClick();
                e.Handled = true;
                return;
            }

            RecordTitleBarClick(window, windowPoint);
            if (OperatingSystem.IsMacOS())
            {
                PrepareMacMoveDrag(dragArea, window, e, windowPoint);
                return;
            }

            e.Handled = true;
            window.BeginMoveDrag(e);
        }
    }

    private static bool IsTitleBarDoubleClick(Window window, Point position)
    {
        var now = DateTimeOffset.UtcNow;
        if (!ReferenceEquals(lastClickWindow, window)
            || now - lastClickTime > DoubleClickWindow)
        {
            return false;
        }

        var delta = position - lastClickPosition;
        return Math.Abs(delta.X) <= DoubleClickDistance && Math.Abs(delta.Y) <= DoubleClickDistance;
    }

    private static void RecordTitleBarClick(Window window, Point position)
    {
        lastClickWindow = window;
        lastClickPosition = position;
        lastClickTime = DateTimeOffset.UtcNow;
    }

    private static void ClearTitleBarClick()
    {
        lastClickWindow = null;
        lastClickPosition = default;
        lastClickTime = default;
    }

    private static void ToggleNormalMaximize(Window window)
    {
        if (OperatingSystem.IsMacOS())
        {
            MainWindowInteractionBehavior.ToggleMacNormalMaximize(window);
            return;
        }

        if (window.WindowState == WindowState.Normal)
        {
            MainWindowInteractionBehavior.CaptureNormalBoundsBeforeMaximize(window);
        }

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private static void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!MacMoveDrags.TryGetValue(e.Pointer, out var state))
        {
            return;
        }

        var currentMouse = GetGlobalMousePosition();
        if (!state.IsMoving)
        {
            var movement = currentMouse - state.StartMouse;
            if (Math.Abs(movement.X) <= MoveDragThreshold && Math.Abs(movement.Y) <= MoveDragThreshold)
            {
                return;
            }

            state.IsMoving = true;
            e.Pointer.Capture(state.DragArea);
            ClearTitleBarClick();
            MainWindowInteractionBehavior.NotifyMoveDragStarted(state.Window);
        }

        state.Window.Position = new PixelPoint(
            state.StartPosition.X + (int)Math.Round(currentMouse.X - state.StartMouse.X),
            state.StartPosition.Y + (int)Math.Round(currentMouse.Y - state.StartMouse.Y));
        e.Handled = true;
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!MacMoveDrags.Remove(e.Pointer, out var state))
        {
            return;
        }

        e.Pointer.Capture(null);
        if (state.IsMoving)
        {
            MainWindowInteractionBehavior.NotifyMoveDragCompleted(state.Window, GetGlobalMousePosition());
        }

        e.Handled = true;
    }

    private static void PrepareMacMoveDrag(InputElement dragArea, Window window, PointerPressedEventArgs e, Point windowPoint)
    {
        MacMoveDrags[e.Pointer] = new MacMoveDragState(
            dragArea,
            window,
            window.Position,
            GetGlobalMousePosition(),
            windowPoint);
        e.Handled = true;
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

    private static Point GetGlobalMousePosition()
    {
        var currentEvent = CGEventCreate(IntPtr.Zero);
        if (currentEvent == IntPtr.Zero)
        {
            return default;
        }

        try
        {
            var point = CGEventGetLocation(currentEvent);
            return new Point(point.X, point.Y);
        }
        finally
        {
            CFRelease(currentEvent);
        }
    }

    private sealed record MacMoveDragState(InputElement DragArea, Window Window, PixelPoint StartPosition, Point StartMouse, Point WindowPoint)
    {
        public bool IsMoving { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct CGPoint
    {
        public readonly double X;
        public readonly double Y;
    }

    [DllImport(CoreGraphicsLibrary)]
    private static extern IntPtr CGEventCreate(IntPtr source);

    [DllImport(CoreGraphicsLibrary)]
    private static extern CGPoint CGEventGetLocation(IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);
}
