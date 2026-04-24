using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Maui.Controls;

using Praxis.Core.Logic;
using Praxis.Services;

namespace Praxis.Behaviors;

public sealed class GrabHandCursorBehavior : Behavior<View>
{
    private readonly PointerGestureRecognizer pointer = new();
    private bool isGrabbing;

#if MACCATALYST
    private static readonly IntPtr nsCursorClass = ObjcGetClass("NSCursor");
    private static readonly IntPtr closedHandCursorSelector = SelRegisterName("closedHandCursor");
    private static readonly IntPtr arrowCursorSelector = SelRegisterName("arrowCursor");
    private static readonly IntPtr setCursorSelector = SelRegisterName("set");
#endif

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        pointer.PointerPressed += OnPointerPressed;
        pointer.PointerReleased += OnPointerReleased;
        pointer.PointerMoved += OnPointerMoved;
        pointer.PointerEntered += OnPointerEntered;
        pointer.PointerExited += OnPointerExited;
        bindable.GestureRecognizers.Add(pointer);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.GestureRecognizers.Remove(pointer);
        pointer.PointerPressed -= OnPointerPressed;
        pointer.PointerReleased -= OnPointerReleased;
        pointer.PointerMoved -= OnPointerMoved;
        pointer.PointerEntered -= OnPointerEntered;
        pointer.PointerExited -= OnPointerExited;
        isGrabbing = false;
        base.OnDetachingFrom(bindable);
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        // Grab cursor belongs to drag-to-reposition only; right-click opens context menu and
        // middle-click opens the editor, so those presses must not hijack the cursor.
        if (!IsPrimaryOnlyPointerPressed(e))
        {
            return;
        }

        isGrabbing = true;
        SetGrabCursor(sender, useGrabCursor: true);
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        if (!isGrabbing)
        {
            return;
        }

        isGrabbing = false;
        SetGrabCursor(sender, useGrabCursor: false);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isGrabbing)
        {
            return;
        }

        // Windows may miss PointerReleased when the pointer is released outside the element.
        // Mirror the drag-end fallback used by MainPage.Draggable_PointerMoved so the grab
        // cursor is cleared as soon as the primary button is no longer down.
        if (!IsAnyPrimaryPointerStillPressed(e))
        {
            isGrabbing = false;
            SetGrabCursor(sender, useGrabCursor: false);
        }
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (isGrabbing)
        {
            SetGrabCursor(sender, useGrabCursor: true);
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (!isGrabbing)
        {
            SetGrabCursor(sender, useGrabCursor: false);
        }
    }

    private static bool IsPrimaryOnlyPointerPressed(PointerEventArgs e)
    {
#if WINDOWS
        var routed = TryGetWindowsRoutedArgs(e);
        var props = routed?.GetCurrentPoint(null).Properties;
        if (props is null)
        {
            return false;
        }

        return props.IsLeftButtonPressed
            && !props.IsRightButtonPressed
            && !props.IsMiddleButtonPressed
            && !props.IsXButton1Pressed
            && !props.IsXButton2Pressed;
#elif MACCATALYST
        var platformArgs = e.PlatformArgs;
        if (platformArgs is null)
        {
            return true;
        }

        return !DescribesNonPrimaryMouseButton(platformArgs);
#else
        return true;
#endif
    }

    private static bool IsAnyPrimaryPointerStillPressed(PointerEventArgs e)
    {
#if WINDOWS
        var routed = TryGetWindowsRoutedArgs(e);
        var props = routed?.GetCurrentPoint(null).Properties;
        return props?.IsLeftButtonPressed == true;
#else
        // Non-Windows platforms dispatch a reliable PointerReleased / exit, so there is no
        // Moved-based fallback to run here.
        return true;
#endif
    }

#if WINDOWS
    private static Microsoft.UI.Xaml.Input.PointerRoutedEventArgs? TryGetWindowsRoutedArgs(PointerEventArgs e)
    {
        var platformArgs = e.PlatformArgs;
        var routedProperty = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs",
            BindingFlags.Public | BindingFlags.Instance);
        return routedProperty?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
    }
#endif

#if MACCATALYST
    private static bool DescribesNonPrimaryMouseButton(object platformArgs)
    {
        var text = platformArgs.ToString() ?? string.Empty;
        if (ContainsNonPrimaryMarker(text))
        {
            return true;
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (nativeEvent is not null && ContainsNonPrimaryMarker(nativeEvent.ToString() ?? string.Empty))
        {
            return true;
        }

        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (gestureRecognizer is not null && ContainsNonPrimaryMarker(gestureRecognizer.ToString() ?? string.Empty))
        {
            return true;
        }

        return false;
    }

    private static bool ContainsNonPrimaryMarker(string text)
    {
        return text.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Secondary", StringComparison.OrdinalIgnoreCase)
            || text.Contains("RightMouse", StringComparison.OrdinalIgnoreCase);
    }

    private static object? TryGetProperty(object source, string propertyName)
    {
        var prop = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(source);
    }
#endif

    private static void SetGrabCursor(object? sender, bool useGrabCursor)
    {
#if WINDOWS
        if (sender is VisualElement element &&
            element.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement frameworkElement)
        {
            var cursor = useGrabCursor
                ? Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeAll)
                : null;
            NonPublicPropertySetter.TrySet(frameworkElement, "ProtectedCursor", cursor);
        }
#endif

#if MACCATALYST
        if (nsCursorClass == IntPtr.Zero)
        {
            return;
        }

        var cursorSelector = useGrabCursor ? closedHandCursorSelector : arrowCursorSelector;
        var cursor = ObjcMsgSendIntPtr(nsCursorClass, cursorSelector);
        if (cursor != IntPtr.Zero)
        {
            ObjcMsgSendVoid(cursor, setCursorSelector);
        }
#endif
    }

#if MACCATALYST
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjcGetClass(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendIntPtr(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void ObjcMsgSendVoid(IntPtr receiver, IntPtr selector);
#endif
}
