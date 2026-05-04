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
    private const nint MacNativeDummyRootTag = 0x50475852;
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
                EnsureNativeNsDummyRootGlass(nativeMacWindow, nativeWindow.Bounds);
            }

            EnsureNativeDummyRootGlass(nativeWindow);
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to apply Mac glass backdrop: {safeMessage}");
        }
    }

    private static void EnsureNativeDummyRootGlass(UIWindow nativeWindow)
    {
        try
        {
            var dummyRoot = FindNativeDummyRootGlass(nativeWindow) ?? CreateNativeDummyRootGlass();
            if (!ReferenceEquals(dummyRoot.Superview, nativeWindow))
            {
                dummyRoot.RemoveFromSuperview();
                nativeWindow.AddSubview(dummyRoot);
            }

            nativeWindow.BringSubviewToFront(dummyRoot);

            const double inset = 18d;
            var bounds = nativeWindow.Bounds;
            dummyRoot.Frame = new CGRect(
                inset,
                inset,
                Math.Max(0d, bounds.Width - (inset * 2d)),
                Math.Max(0d, bounds.Height - (inset * 2d)));
            dummyRoot.Hidden = false;
            dummyRoot.Alpha = 1f;
            dummyRoot.UserInteractionEnabled = false;
            dummyRoot.BackgroundColor = nativeWindow.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark
                ? UIColor.FromRGBA(33, 38, 45, 0.55f)
                : UIColor.FromRGBA(255, 255, 255, 0.45f);
            dummyRoot.ClipsToBounds = true;
            dummyRoot.Layer.CornerRadius = 28f;
            dummyRoot.Layer.MasksToBounds = true;
            dummyRoot.Layer.BorderWidth = 1f;
            dummyRoot.Layer.BorderColor = nativeWindow.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark
                ? UIColor.FromRGBA(113, 123, 136, 0.60f).CGColor
                : UIColor.FromRGBA(255, 255, 255, 0.80f).CGColor;

            foreach (var subview in dummyRoot.Subviews)
            {
                subview.Frame = dummyRoot.Bounds;
                subview.ClipsToBounds = true;
                subview.Layer.CornerRadius = 28f;
                subview.Layer.MasksToBounds = true;
            }
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to apply native Mac dummy root glass: {safeMessage}");
        }
    }

    private static UIView CreateNativeDummyRootGlass()
    {
        var container = new UIView
        {
            Tag = MacNativeDummyRootTag,
            UserInteractionEnabled = false,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
        };

        container.AddSubview(new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemUltraThinMaterial))
        {
            UserInteractionEnabled = false,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
        });

        return container;
    }

    private static UIView? FindNativeDummyRootGlass(UIWindow nativeWindow)
    {
        foreach (var subview in nativeWindow.Subviews)
        {
            if (subview.Tag == MacNativeDummyRootTag)
            {
                return subview;
            }
        }

        return null;
    }

    private static void EnsureNativeNsDummyRootGlass(NSObject nativeMacWindow, CGRect uiBounds)
    {
        try
        {
            if (nativeMacWindow.ValueForKey(new NSString("contentView")) is not NSObject contentView)
            {
                return;
            }

            const double inset = 18d;
            var frame = new CGRect(
                inset,
                inset,
                Math.Max(0d, uiBounds.Width - (inset * 2d)),
                Math.Max(0d, uiBounds.Height - (inset * 2d)));
            var dummyRoot = FindNativeNsDummyRootGlass(contentView) ?? CreateNativeNsDummyRootGlass(frame);
            if (dummyRoot is null)
            {
                return;
            }

            TrySetNativeFrame(dummyRoot, frame);
            TrySetBool(dummyRoot, "hidden", false);
            TrySetBool(dummyRoot, "wantsLayer", true);
            TrySetObject(dummyRoot, "identifier", MacNativeNsDummyRootIdentifier);
            TrySetInt(dummyRoot, "blendingMode", 0);
            TrySetInt(dummyRoot, "material", 6);
            TrySetInt(dummyRoot, "state", 1);

            if (dummyRoot.ValueForKey(new NSString("layer")) is CALayer layer)
            {
                layer.BackgroundColor = UIColor.FromRGBA(255, 255, 255, 0.42f).CGColor;
                layer.CornerRadius = 28f;
                layer.MasksToBounds = true;
                layer.BorderWidth = 1f;
                layer.BorderColor = UIColor.FromRGBA(255, 255, 255, 0.82f).CGColor;
            }

            if (dummyRoot.ValueForKey(new NSString("superview")) is null)
            {
                contentView.PerformSelector(new Selector("addSubview:"), dummyRoot);
            }
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to apply native NS dummy root glass: {safeMessage}");
        }
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

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendIntPtr(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendCGRect(IntPtr receiver, IntPtr selector, CGRect frame);

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

            var updated = styleMaskValue.Int64Value | MacFullSizeContentViewMask;
            nativeWindow.SetValueForKey(NSNumber.FromInt64(updated), new NSString("styleMask"));
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
