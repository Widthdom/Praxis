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
    private const string User32Library = "user32.dll";
    private static readonly TimeSpan DoubleClickWindow = TimeSpan.FromMilliseconds(800);
    private const double DoubleClickDistance = 24;
    private const double MoveDragThreshold = 12;
    private const double WindowsSnapThreshold = 48;
    private static readonly Dictionary<IPointer, MacMoveDragState> MacMoveDrags = [];
    private static readonly Dictionary<IPointer, WindowsMoveDragState> WindowsMoveDrags = [];
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

            // On Windows the titlebar visual is marked with WindowDecorationProperties.ElementRole=TitleBar.
            // Let Avalonia/Win32 perform the non-client move so Aero Snap and system animations stay intact.
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
        if (WindowsMoveDrags.TryGetValue(e.Pointer, out var windowsState))
        {
            MoveWindowsDrag(e, windowsState);
            return;
        }

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
        if (WindowsMoveDrags.Remove(e.Pointer, out var windowsState))
        {
            e.Pointer.Capture(null);
            if (windowsState.IsMoving)
            {
                ApplyWindowsSnap(windowsState.Window, GetWindowsMousePosition());
            }

            e.Handled = true;
            return;
        }

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

    private static void PrepareWindowsMoveDrag(InputElement dragArea, Window window, PointerPressedEventArgs e)
    {
        WindowsMoveDrags[e.Pointer] = new WindowsMoveDragState(
            dragArea,
            window,
            window.Position,
            GetWindowsMousePosition());
        e.Pointer.Capture(dragArea);
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

    private static void MoveWindowsDrag(PointerEventArgs e, WindowsMoveDragState state)
    {
        var currentMouse = GetWindowsMousePosition();
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
            if (state.Window.WindowState == WindowState.Maximized)
            {
                state.Window.WindowState = WindowState.Normal;
                state.StartPosition = state.Window.Position;
                state.StartMouse = currentMouse;
            }
        }

        state.Window.Position = new PixelPoint(
            state.StartPosition.X + (int)Math.Round(currentMouse.X - state.StartMouse.X),
            state.StartPosition.Y + (int)Math.Round(currentMouse.Y - state.StartMouse.Y));
        e.Handled = true;
    }

    private static void ApplyWindowsSnap(Window window, Point mousePosition)
    {
        var screen = window.Screens.ScreenFromWindow(window);
        if (screen is null)
        {
            return;
        }

        var area = screen.WorkingArea;
        var nearLeft = mousePosition.X <= area.X + WindowsSnapThreshold;
        var nearRight = mousePosition.X >= area.X + area.Width - WindowsSnapThreshold;
        var nearTop = mousePosition.Y <= area.Y + WindowsSnapThreshold;

        if (nearTop && !nearLeft && !nearRight)
        {
            MainWindowInteractionBehavior.CaptureNormalBoundsBeforeMaximize(window);
            window.WindowState = WindowState.Maximized;
            return;
        }

        if (!nearLeft && !nearRight)
        {
            return;
        }

        var targetPixelWidth = area.Width / 2;
        var targetDipWidth = targetPixelWidth / screen.Scaling;
        var targetDipHeight = area.Height / screen.Scaling;
        window.WindowState = WindowState.Normal;
        window.MinWidth = Math.Min(window.MinWidth, targetDipWidth);
        window.MinHeight = Math.Min(window.MinHeight, targetDipHeight);
        window.Position = new PixelPoint(
            nearLeft ? area.X : area.X + targetPixelWidth,
            area.Y);
        window.Width = targetDipWidth;
        window.Height = targetDipHeight;
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

    private static Point GetWindowsMousePosition()
    {
        return GetCursorPos(out var point)
            ? new Point(point.X, point.Y)
            : default;
    }

    private sealed record MacMoveDragState(InputElement DragArea, Window Window, PixelPoint StartPosition, Point StartMouse, Point WindowPoint)
    {
        public bool IsMoving { get; set; }
    }

    private sealed class WindowsMoveDragState(InputElement dragArea, Window window, PixelPoint startPosition, Point startMouse)
    {
        public InputElement DragArea { get; } = dragArea;
        public Window Window { get; } = window;
        public PixelPoint StartPosition { get; set; } = startPosition;
        public Point StartMouse { get; set; } = startMouse;
        public bool IsMoving { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct CGPoint
    {
        public readonly double X;
        public readonly double Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct POINT
    {
        public readonly int X;
        public readonly int Y;
    }

    [DllImport(CoreGraphicsLibrary)]
    private static extern IntPtr CGEventCreate(IntPtr source);

    [DllImport(CoreGraphicsLibrary)]
    private static extern CGPoint CGEventGetLocation(IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    [DllImport(User32Library)]
    private static extern bool GetCursorPos(out POINT point);
}
