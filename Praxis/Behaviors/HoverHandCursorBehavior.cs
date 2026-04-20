using System.Runtime.InteropServices;

using Microsoft.Maui.Controls;

using Praxis.Core.Logic;
using Praxis.Services;

namespace Praxis.Behaviors;

public sealed class HoverHandCursorBehavior : Behavior<View>
{
    private readonly PointerGestureRecognizer pointer = new();

#if MACCATALYST
    private static readonly IntPtr nsCursorClass = ObjcGetClass("NSCursor");
    private static readonly IntPtr pointingHandCursorSelector = SelRegisterName("pointingHandCursor");
    private static readonly IntPtr arrowCursorSelector = SelRegisterName("arrowCursor");
    private static readonly IntPtr setCursorSelector = SelRegisterName("set");
#endif

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        pointer.PointerEntered += OnPointerEntered;
        pointer.PointerExited += OnPointerExited;
        bindable.GestureRecognizers.Add(pointer);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.GestureRecognizers.Remove(pointer);
        pointer.PointerEntered -= OnPointerEntered;
        pointer.PointerExited -= OnPointerExited;
        base.OnDetachingFrom(bindable);
    }

    private static void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        SetHandCursor(sender, useHandCursor: true);
    }

    private static void OnPointerExited(object? sender, PointerEventArgs e)
    {
        SetHandCursor(sender, useHandCursor: false);
    }

    private static void SetHandCursor(object? sender, bool useHandCursor)
    {
#if WINDOWS
        if (sender is VisualElement element &&
            element.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement frameworkElement)
        {
            var cursor = useHandCursor
                ? Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand)
                : null;
            NonPublicPropertySetter.TrySet(frameworkElement, "ProtectedCursor", cursor);
        }
#endif

#if MACCATALYST
        if (nsCursorClass == IntPtr.Zero)
        {
            return;
        }

        var cursorSelector = useHandCursor ? pointingHandCursorSelector : arrowCursorSelector;
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
