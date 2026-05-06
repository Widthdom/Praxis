#if MACCATALYST
using CoreAnimation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using System.Runtime.InteropServices;
using UIKit;
using Praxis.Services;

namespace Praxis;

public partial class App
{
    private const nint MacFullSizeContentViewMask = (nint)(1 << 15);
    private const nint MacTexturedBackgroundMask = (nint)(1 << 8);
    private const nint MacNativeSubviewBelow = -1;
    private const int MacNativePopoverMaterial = 6;
    private const int MacNativeRootGlassAutoresizingMask = 18;
    private const double MacNativeRootGlassLightAlpha = 1d;
    private const double MacNativeRootGlassDarkAlpha = 1d;
    private const double MacNativeRootGlassOverscan = 1d;
    private static readonly NSString MacNativeNsDummyRootIdentifier = new("PraxisNativeDummyRootGlass");
    private static int macBackdropDiagnosticsLogged;

    static partial void ApplyPlatformWindowBackdrop(Microsoft.Maui.Controls.Window window)
    {
        ApplyMacWindowBackdrop(window);
    }

    internal static void RefreshMacWindowBackdropForConnectedScenes()
    {
        try
        {
            if (UIApplication.SharedApplication.ConnectedScenes is null)
            {
                return;
            }

            foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
            {
                if (scene is not UIWindowScene windowScene)
                {
                    continue;
                }

                foreach (var window in windowScene.Windows)
                {
                    ApplyMacWindowBackdrop(window);
                }
            }
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to refresh Mac window backdrop for connected scenes: {safeMessage}");
        }
    }

    private static void ApplyMacWindowBackdrop(Microsoft.Maui.Controls.Window window)
    {
        try
        {
            if (window.Handler?.PlatformView is not UIWindow nativeWindow)
            {
                return;
            }

            ApplyMacWindowBackdrop(nativeWindow);
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to apply Mac glass backdrop: {safeMessage}");
        }
    }

    private static void ApplyMacWindowBackdrop(UIWindow nativeWindow)
    {
        try
        {
            nativeWindow.Opaque = false;
            nativeWindow.BackgroundColor = UIColor.Clear;
            if (nativeWindow.Layer is not null)
            {
                nativeWindow.Layer.Opaque = false;
                nativeWindow.Layer.BackgroundColor = UIColor.Clear.CGColor;
                nativeWindow.Layer.BorderWidth = 0f;
                nativeWindow.Layer.BorderColor = UIColor.Clear.CGColor;
                nativeWindow.Layer.MasksToBounds = false;
            }
            ClearMacViewTree(nativeWindow);

            if (nativeWindow.RootViewController?.View is UIView rootView)
            {
                ClearMacViewTree(rootView);
            }

            if (nativeWindow.WindowScene?.Titlebar is UITitlebar titlebar)
            {
                titlebar.TitleVisibility = UITitlebarTitleVisibility.Hidden;
                titlebar.Toolbar = null;
                if (OperatingSystem.IsMacOSVersionAtLeast(12, 0))
                {
                    titlebar.ToolbarStyle = UITitlebarToolbarStyle.UnifiedCompact;
                    titlebar.SeparatorStyle = UITitlebarSeparatorStyle.None;
                }
            }

            if (TryGetNativeWindow(nativeWindow) is NSObject nativeMacWindow)
            {
                LogMacBackdropDiagnostics(nativeWindow, nativeMacWindow);
                TrySetBool(nativeMacWindow, "opaque", false);
                TrySetObject(nativeMacWindow, "backgroundColor", CreateNativeColor("clearColor") ?? UIColor.Clear);
                TrySetBool(nativeMacWindow, "titlebarAppearsTransparent", true);
                TryUpdateStyleMask(nativeMacWindow);
                ClearNativeWindowChrome(nativeMacWindow, "contentView");
                ClearNativeWindowChrome(nativeMacWindow, "titlebarView");
                ClearNativeWindowChrome(nativeMacWindow, "toolbarView");
                ClearNativeWindowChrome(nativeMacWindow, "titlebarContainerView");
                ClearNativeWindowChrome(nativeMacWindow, "toolbarContainerView");
                ClearNativeWindowChrome(nativeMacWindow, "frameView");
                ClearNativeFrameChrome(nativeMacWindow);
                EnsureNativeNsDummyRootGlass(
                    nativeMacWindow,
                    nativeWindow.Bounds,
                    nativeWindow.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark);
            }
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to apply Mac glass backdrop: {safeMessage}");
        }
    }

    private static void EnsureNativeNsDummyRootGlass(NSObject nativeMacWindow, CGRect uiBounds, bool isDark)
    {
        try
        {
            if (nativeMacWindow.ValueForKey(new NSString("contentView")) is not NSObject contentView)
            {
                return;
            }

            var hostView = ResolveNativeRootGlassHostView(contentView);
            RemoveStaleNativeRootGlass(contentView, hostView);

            var frame = ExpandNativeRootGlassFrame(ResolveNativeContentBounds(hostView, uiBounds));
            var dummyRoot = FindNativeNsDummyRootGlass(hostView) ?? CreateNativeNsDummyRootGlass(frame);
            if (dummyRoot is null)
            {
                return;
            }

            TrySetNativeFrame(dummyRoot, frame);
            TrySetBool(dummyRoot, "hidden", false);
            TrySetBool(dummyRoot, "wantsLayer", true);
            TrySetObject(dummyRoot, "identifier", MacNativeNsDummyRootIdentifier);
            TrySetInt(dummyRoot, "autoresizingMask", MacNativeRootGlassAutoresizingMask);
            TrySetInt(dummyRoot, "blendingMode", 0);
            TrySetInt(dummyRoot, "material", MacNativePopoverMaterial);
            TrySetInt(dummyRoot, "state", 1);
            TrySetBool(dummyRoot, "emphasized", true);
            var rootGlassAlpha = isDark ? MacNativeRootGlassDarkAlpha : MacNativeRootGlassLightAlpha;
            TrySetDouble(dummyRoot, "alphaValue", rootGlassAlpha);
            TrySendDouble(dummyRoot, "setAlphaValue:", rootGlassAlpha);

            if (dummyRoot.ValueForKey(new NSString("layer")) is CALayer layer)
            {
                layer.Opaque = false;
                layer.BackgroundColor = UIColor.Clear.CGColor;
                layer.CornerRadius = 0f;
                layer.MasksToBounds = false;
                layer.BorderWidth = 0f;
                layer.BorderColor = UIColor.Clear.CGColor;
            }

            InsertNativeSubviewBelowContent(hostView, dummyRoot, contentView);
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to apply native NS dummy root glass: {safeMessage}");
        }
    }

    private static NSObject ResolveNativeRootGlassHostView(NSObject contentView)
    {
        try
        {
            if (contentView.ValueForKey(new NSString("superview")) is NSObject frameView)
            {
                return frameView;
            }
        }
        catch
        {
        }

        return contentView;
    }

    private static void RemoveStaleNativeRootGlass(NSObject contentView, NSObject hostView)
    {
        if (contentView.Handle == hostView.Handle)
        {
            return;
        }

        if (FindNativeNsDummyRootGlass(contentView) is NSObject staleRoot)
        {
            RemoveNativeSubview(staleRoot);
        }
    }

    private static CGRect ResolveNativeContentBounds(NSObject contentView, CGRect fallbackBounds)
    {
        try
        {
            if (contentView.ValueForKey(new NSString("bounds")) is NSValue value)
            {
                var bounds = value.CGRectValue;
                if (bounds.Width > 0d && bounds.Height > 0d)
                {
                    return new CGRect(0d, 0d, bounds.Width, bounds.Height);
                }
            }
        }
        catch
        {
        }

        return new CGRect(0d, 0d, fallbackBounds.Width, fallbackBounds.Height);
    }

    private static CGRect ExpandNativeRootGlassFrame(CGRect frame)
    {
        return new CGRect(
            frame.X - MacNativeRootGlassOverscan,
            frame.Y - MacNativeRootGlassOverscan,
            frame.Width + (MacNativeRootGlassOverscan * 2d),
            frame.Height + (MacNativeRootGlassOverscan * 2d));
    }

    private static void InsertNativeSubviewBelowContent(NSObject hostView, NSObject dummyRoot, NSObject? relativeTo)
    {
        if (dummyRoot.ValueForKey(new NSString("superview")) is not null)
        {
            RemoveNativeSubview(dummyRoot);
        }

        ObjcMsgSendAddSubviewPositioned(
            hostView.Handle,
            SelRegisterName("addSubview:positioned:relativeTo:"),
            dummyRoot.Handle,
            MacNativeSubviewBelow,
            relativeTo?.Handle ?? IntPtr.Zero);
    }

    private static void RemoveNativeSubview(NSObject view)
    {
        ObjcMsgSendVoid(view.Handle, SelRegisterName("removeFromSuperview"));
    }

    private static NSObject? CreateNativeNsDummyRootGlass(CGRect frame)
    {
        try
        {
            var viewClass = Class.GetHandle("NSVisualEffectView");
            if (viewClass == IntPtr.Zero)
            {
                viewClass = Class.GetHandle("NSView");
            }

            if (viewClass == IntPtr.Zero)
            {
                return null;
            }

            var allocated = ObjcMsgSendIntPtr(viewClass, SelRegisterName("alloc"));
            var initialized = ObjcMsgSendCGRect(allocated, SelRegisterName("initWithFrame:"), frame);
            return Runtime.GetNSObject(initialized);
        }
        catch
        {
            return null;
        }
    }

    private static void TrySetNativeFrame(NSObject target, CGRect frame)
    {
        try
        {
            target.SetValueForKey(NSValue.FromCGRect(frame), new NSString("frame"));
        }
        catch
        {
        }
    }

    private static void TrySetInt(NSObject target, string key, int value)
    {
        try
        {
            target.SetValueForKey(NSNumber.FromInt32(value), new NSString(key));
        }
        catch
        {
        }
    }

    private static void TrySetDouble(NSObject target, string key, double value)
    {
        try
        {
            target.SetValueForKey(NSNumber.FromDouble(value), new NSString(key));
        }
        catch
        {
        }
    }

    private static void TrySendDouble(NSObject target, string selectorName, double value)
    {
        try
        {
            ObjcMsgSendDouble(target.Handle, SelRegisterName(selectorName), value);
        }
        catch
        {
        }
    }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendIntPtr(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendCGRect(IntPtr receiver, IntPtr selector, CGRect frame);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void ObjcMsgSendVoid(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void ObjcMsgSendDouble(IntPtr receiver, IntPtr selector, double value);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void ObjcMsgSendNInt(IntPtr receiver, IntPtr selector, nint value);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void ObjcMsgSendAddSubviewPositioned(IntPtr receiver, IntPtr selector, IntPtr subview, nint positioned, IntPtr relativeTo);

    private static NSObject? FindNativeNsDummyRootGlass(NSObject contentView)
    {
        try
        {
            if (contentView.ValueForKey(new NSString("subviews")) is not NSArray subviews)
            {
                return null;
            }

            foreach (var child in subviews)
            {
                if (child is not NSObject childObject)
                {
                    continue;
                }

                var identifier = childObject.ValueForKey(new NSString("identifier"))?.ToString();
                if (string.Equals(identifier, MacNativeNsDummyRootIdentifier.ToString(), StringComparison.Ordinal))
                {
                    return childObject;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static void LogMacBackdropDiagnostics(UIWindow nativeWindow, NSObject nativeMacWindow)
    {
        if (Interlocked.Exchange(ref macBackdropDiagnosticsLogged, 1) != 0)
        {
            return;
        }

        try
        {
            var titlebar = nativeWindow.WindowScene?.Titlebar;
            var nativeClass = nativeMacWindow.GetType().FullName ?? nativeMacWindow.Class.Name;
            var sceneClass = nativeWindow.WindowScene?.GetType().FullName ?? "(null)";
            var titlebarClass = titlebar?.GetType().FullName ?? "(null)";
            var titlebarStyle = titlebar is not null && OperatingSystem.IsMacOSVersionAtLeast(12, 0)
                ? titlebar.ToolbarStyle.ToString()
                : "(null)";
            var opaque = SafeDescribeNativeBool(nativeMacWindow, "opaque");
            var styleMask = nativeMacWindow.ValueForKey(new NSString("styleMask")) as NSNumber;
            var contentView = nativeMacWindow.ValueForKey(new NSString("contentView")) as NSObject;
            var titlebarAppearsTransparent = SafeDescribeNativeBool(nativeMacWindow, "titlebarAppearsTransparent");
            var styleMaskText = styleMask?.LongValue.ToString() ?? "(null)";
            var contentViewText = contentView?.GetType().FullName ?? "(null)";
            var titlebarView = SafeDescribeNativeKey(nativeMacWindow, "titlebarView");
            var toolbarView = SafeDescribeNativeKey(nativeMacWindow, "toolbarView");
            var titlebarContainer = SafeDescribeNativeKey(nativeMacWindow, "titlebarContainerView");
            var toolbarContainer = SafeDescribeNativeKey(nativeMacWindow, "toolbarContainerView");

            CrashFileLogger.WriteInfo(
                nameof(App),
                $"MacBackdrop diagnostics: nativeWindow={nativeWindow.GetType().FullName}; nativeMacWindow={nativeClass}; scene={sceneClass}; titlebar={titlebarClass}; titlebarStyle={titlebarStyle}; opaque={opaque}; titlebarAppearsTransparent={titlebarAppearsTransparent}; styleMask={styleMaskText}; contentView={contentViewText}; titlebarView={titlebarView}; toolbarView={toolbarView}; titlebarContainerView={titlebarContainer}; toolbarContainerView={toolbarContainer}");
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to log Mac backdrop diagnostics: {safeMessage}");
        }
    }

    private static string SafeDescribeNativeKey(NSObject target, string key)
    {
        try
        {
            var value = target.ValueForKey(new NSString(key));
            return value?.GetType().FullName ?? "(null)";
        }
        catch
        {
            return "(error)";
        }
    }

    private static string SafeDescribeNativeBool(NSObject target, string key)
    {
        try
        {
            if (target.ValueForKey(new NSString(key)) is NSNumber number)
            {
                return number.BoolValue.ToString();
            }

            return "(null)";
        }
        catch
        {
            return "(error)";
        }
    }

    private static NSObject? TryGetNativeWindow(UIWindow nativeWindow)
    {
        try
        {
            if (TryGetNativeWindowFromAppDelegate(nativeWindow) is NSObject appDelegateWindow)
            {
                return appDelegateWindow;
            }

            if (TryGetNativeWindowFromApplicationWindows(nativeWindow) is NSObject applicationWindow)
            {
                return applicationWindow;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static NSObject? TryGetNativeWindowFromAppDelegate(UIWindow nativeWindow)
    {
        var delegateObject = UIApplication.SharedApplication.Delegate as NSObject;
        if (delegateObject is null)
        {
            if (Interlocked.Exchange(ref macBackdropDiagnosticsLogged, 1) == 0)
            {
                CrashFileLogger.WriteWarning(nameof(App), "MacBackdrop diagnostics: UIApplication delegate was null.");
            }
            return null;
        }

        var selector = new Selector("hostWindowForUIWindow:");
        if (!delegateObject.RespondsToSelector(selector))
        {
            if (Interlocked.Exchange(ref macBackdropDiagnosticsLogged, 1) == 0)
            {
                var delegateType = delegateObject.GetType().FullName ?? delegateObject.Class.Name;
                CrashFileLogger.WriteWarning(nameof(App), $"MacBackdrop diagnostics: delegate '{delegateType}' does not respond to hostWindowForUIWindow:.");
            }
            return null;
        }

        var result = delegateObject.PerformSelector(selector, nativeWindow);
        if (result is null && Interlocked.Exchange(ref macBackdropDiagnosticsLogged, 1) == 0)
        {
            var delegateType = delegateObject.GetType().FullName ?? delegateObject.Class.Name;
            CrashFileLogger.WriteWarning(nameof(App), $"MacBackdrop diagnostics: delegate '{delegateType}' returned null from hostWindowForUIWindow:.");
        }

        return result;
    }

    private static NSObject? TryGetNativeWindowFromApplicationWindows(UIWindow nativeWindow)
    {
        try
        {
            var applicationClass = Runtime.GetNSObject(Class.GetHandle("NSApplication"));
            if (applicationClass is null)
            {
                return null;
            }

            var nsWindows = applicationClass.ValueForKeyPath(new NSString("sharedApplication.windows")) as NSArray;
            if (nsWindows is null)
            {
                if (Interlocked.Exchange(ref macBackdropDiagnosticsLogged, 1) == 0)
                {
                    CrashFileLogger.WriteWarning(nameof(App), "MacBackdrop diagnostics: sharedApplication.windows was null.");
                }
                return null;
            }

            foreach (var candidate in nsWindows)
            {
                if (candidate is not NSObject nsWindow)
                {
                    continue;
                }

                if (WindowMatchesUiWindow(nsWindow, nativeWindow))
                {
                    return nsWindow;
                }
            }

            if (Interlocked.Exchange(ref macBackdropDiagnosticsLogged, 1) == 0)
            {
                CrashFileLogger.WriteWarning(nameof(App), "MacBackdrop diagnostics: no NSWindow matched the current UIWindow.");
            }
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"MacBackdrop diagnostics: failed to inspect Application windows: {safeMessage}");
        }

        return null;
    }

    private static bool WindowMatchesUiWindow(NSObject nsWindow, UIWindow nativeWindow)
    {
        try
        {
            if (nsWindow.ValueForKey(new NSString("uiWindows")) is not NSArray uiWindows)
            {
                return false;
            }

            foreach (var candidate in uiWindows)
            {
                if (candidate is UIWindow uiWindow && ReferenceEquals(uiWindow, nativeWindow))
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private static void TrySetBool(NSObject target, string key, bool value)
    {
        try
        {
            target.SetValueForKey(NSNumber.FromBoolean(value), new NSString(key));
        }
        catch
        {
        }
    }

    private static void TrySetObject(NSObject target, string key, NSObject value)
    {
        try
        {
            target.SetValueForKey(value, new NSString(key));
        }
        catch
        {
        }
    }

    private static void TryUpdateStyleMask(NSObject nativeWindow)
    {
        try
        {
            var styleMaskValue = nativeWindow.ValueForKey(new NSString("styleMask")) as NSNumber;
            if (styleMaskValue is null)
            {
                return;
            }

            var updated = styleMaskValue.Int64Value | MacFullSizeContentViewMask | MacTexturedBackgroundMask;
            nativeWindow.SetValueForKey(NSNumber.FromInt64(updated), new NSString("styleMask"));
            ObjcMsgSendNInt(nativeWindow.Handle, SelRegisterName("setStyleMask:"), (nint)updated);
        }
        catch
        {
        }
    }

    private static void ClearMacViewTree(UIView view)
    {
        view.Opaque = false;
        view.BackgroundColor = UIColor.Clear;
        if (view.Layer is not null)
        {
            view.Layer.Opaque = false;
            view.Layer.BackgroundColor = UIColor.Clear.CGColor;
            view.Layer.BorderWidth = 0f;
            view.Layer.BorderColor = UIColor.Clear.CGColor;
            view.Layer.MasksToBounds = false;
        }

        foreach (var child in view.Subviews)
        {
            ClearMacViewTree(child);
        }
    }

    private static void ClearNativeWindowChrome(NSObject nativeWindow, string key)
    {
        try
        {
            if (nativeWindow.ValueForKey(new NSString(key)) is NSObject chromeObject)
            {
                ClearNativeObjectTree(chromeObject);
            }
        }
        catch
        {
        }
    }

    private static void ClearNativeFrameChrome(NSObject nativeWindow)
    {
        try
        {
            if (nativeWindow.ValueForKey(new NSString("contentView")) is not NSObject contentView)
            {
                return;
            }

            if (contentView.ValueForKey(new NSString("superview")) is NSObject frameView)
            {
                ClearNativeObjectTree(frameView);
            }
        }
        catch
        {
        }
    }

    private static void ClearNativeObjectTree(NSObject target)
    {
        try
        {
            TrySetBool(target, "wantsLayer", true);
            TrySetBool(target, "opaque", false);
            TrySetObject(target, "backgroundColor", CreateNativeColor("clearColor") ?? UIColor.Clear);
            if (target.ValueForKey(new NSString("layer")) is CALayer layer)
            {
                layer.Opaque = false;
                layer.BackgroundColor = UIColor.Clear.CGColor;
                layer.BorderWidth = 0f;
                layer.BorderColor = UIColor.Clear.CGColor;
                layer.MasksToBounds = false;
            }

            if (target.ValueForKey(new NSString("subviews")) is NSArray subviews)
            {
                foreach (var child in subviews)
                {
                    if (child is NSObject childObject)
                    {
                        ClearNativeObjectTree(childObject);
                    }
                }
            }
        }
        catch
        {
        }
    }

    private static NSObject? CreateNativeColor(string selectorName)
    {
        try
        {
            var colorClass = Runtime.GetNSObject(Class.GetHandle("NSColor"));
            if (colorClass is null)
            {
                return null;
            }

            return colorClass.PerformSelector(new Selector(selectorName));
        }
        catch
        {
            return null;
        }
    }
}
#endif
