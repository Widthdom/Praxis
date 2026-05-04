#if MACCATALYST
using CoreAnimation;
using Foundation;
using ObjCRuntime;
using UIKit;
using Praxis.Services;

namespace Praxis;

public partial class App
{
    private const nint MacFullSizeContentViewMask = (nint)(1 << 15);
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
            }
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to apply Mac glass backdrop: {safeMessage}");
        }
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
            TrySetObject(target, "backgroundColor", CreateNativeColor("clearColor") ?? UIColor.Clear);
            if (target.ValueForKey(new NSString("layer")) is CALayer layer)
            {
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
