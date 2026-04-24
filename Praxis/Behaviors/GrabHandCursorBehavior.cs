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
        pointer.PointerEntered += OnPointerEntered;
        pointer.PointerExited += OnPointerExited;
        bindable.GestureRecognizers.Add(pointer);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.GestureRecognizers.Remove(pointer);
        pointer.PointerPressed -= OnPointerPressed;
        pointer.PointerReleased -= OnPointerReleased;
        pointer.PointerEntered -= OnPointerEntered;
        pointer.PointerExited -= OnPointerExited;
        isGrabbing = false;
        base.OnDetachingFrom(bindable);
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        isGrabbing = true;
        SetGrabCursor(sender, useGrabCursor: true);
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        isGrabbing = false;
        SetGrabCursor(sender, useGrabCursor: false);
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
