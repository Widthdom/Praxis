using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Maui.Controls;

using Praxis.Core.Logic;
using Praxis.Services;

namespace Praxis.Behaviors;

public sealed class GrabHandCursorBehavior : Behavior<View>
{
    private static readonly object activeGrabLock = new();
    private static GrabHandCursorBehavior? activeGrabBehavior;

    private readonly PointerGestureRecognizer pointer = new();
    private bool isGrabbing;
    private View? attachedView;

#if MACCATALYST
    private static readonly IntPtr nsCursorClass = ObjcGetClass("NSCursor");
    private static readonly IntPtr closedHandCursorSelector = SelRegisterName("closedHandCursor");
    private static readonly IntPtr arrowCursorSelector = SelRegisterName("arrowCursor");
    private static readonly IntPtr setCursorSelector = SelRegisterName("set");
#endif

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        attachedView = bindable;
        pointer.PointerPressed += OnPointerPressed;
        pointer.PointerReleased += OnPointerReleased;
        pointer.PointerMoved += OnPointerMoved;
        pointer.PointerEntered += OnPointerEntered;
        pointer.PointerExited += OnPointerExited;
        bindable.GestureRecognizers.Add(pointer);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        // If the view is torn down (filtering / template churn / deletion) while the
        // grab cursor is still active, restore the default cursor first so the native
        // cursor state does not leak across Mac windows or the Windows ProtectedCursor
        // does not stay wired to a detached platform view.
        if (ReferenceEquals(GetActiveGrabBehavior(), this) && isGrabbing)
        {
            ClearActiveGrab();
        }

        bindable.GestureRecognizers.Remove(pointer);
        pointer.PointerPressed -= OnPointerPressed;
        pointer.PointerReleased -= OnPointerReleased;
        pointer.PointerMoved -= OnPointerMoved;
        pointer.PointerEntered -= OnPointerEntered;
        pointer.PointerExited -= OnPointerExited;
        isGrabbing = false;
        attachedView = null;
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

        SetActiveGrab(this, sender);
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        if (ReferenceEquals(GetActiveGrabBehavior(), this))
        {
            ClearActiveGrab();
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        // Windows and macOS can miss PointerReleased when the pointer is released outside the
        // element. Mirror the drag-end fallback used by MainPage.Draggable_PointerMoved so the
        // grab cursor is cleared as soon as the primary button is no longer down.
        if (!IsAnyPrimaryPointerStillPressed(e))
        {
            ClearActiveGrab();
        }
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (ReferenceEquals(GetActiveGrabBehavior(), this))
        {
            SetGrabCursor(sender, useGrabCursor: true);
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (!IsAnyPrimaryPointerStillPressed(e))
        {
            ClearActiveGrab();
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
        // Delegate to the shared classifier so this behavior honors the same reflection
        // rules (Type / PressedButton / Button / Buttons / ButtonMask / ButtonNumber /
        // CurrentEvent) that MainPage.PointerAndSelection.cs uses for drag dispatch.
        return PointerButtonClassifier.IsPrimaryOnly(e.PlatformArgs);
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
#elif MACCATALYST
        return PointerButtonClassifier.IsPrimaryPressed(e.PlatformArgs);
#else
        // Non-Windows platforms dispatch a reliable PointerReleased / exit, so there is no
        // Moved-based fallback to run here.
        return true;
#endif
    }

    private static GrabHandCursorBehavior? GetActiveGrabBehavior()
    {
        lock (activeGrabLock)
        {
            return activeGrabBehavior;
        }
    }

    private static void SetActiveGrab(GrabHandCursorBehavior behavior, object? sender)
    {
        lock (activeGrabLock)
        {
            activeGrabBehavior = behavior;
        }

        behavior.isGrabbing = true;
        SetGrabCursor(sender, useGrabCursor: true);
    }

    private static void ClearActiveGrab()
    {
        GrabHandCursorBehavior? activeBehavior;
        lock (activeGrabLock)
        {
            activeBehavior = activeGrabBehavior;
            activeGrabBehavior = null;
        }

        if (activeBehavior is null)
        {
            return;
        }

        activeBehavior.isGrabbing = false;
        SetGrabCursor(activeBehavior.attachedView, useGrabCursor: false);
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
