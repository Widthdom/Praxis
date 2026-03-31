using Foundation;
using ObjCRuntime;
using UIKit;
using Praxis.Controls;
using Praxis.Core.Logic;
using Praxis.Services;

namespace Praxis;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private static readonly UIKeyCommand ThemeDarkCommand = CreateThemeKeyCommand("d", "handleThemeDark:");
    private static readonly UIKeyCommand ThemeLightCommand = CreateThemeKeyCommand("l", "handleThemeLight:");
    private static readonly UIKeyCommand ThemeSystemCommand = CreateThemeKeyCommand("h", "handleThemeSystem:");
    private static readonly UIKeyCommand UndoHistoryCommand = CreateHistoryKeyCommand("z", UIKeyModifierFlags.Command, "handleHistoryUndo:");
    private static readonly UIKeyCommand RedoHistoryCommand = CreateHistoryKeyCommand("z", UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, "handleHistoryRedo:");
    private NSObject? didBecomeActiveObserver;
    private NSObject? willEnterForegroundObserver;
    private NSObject? windowDidBecomeKeyObserver;
    private NSObject? willResignActiveObserver;
    private NSObject? didEnterBackgroundObserver;
    private NSObject? windowDidResignKeyObserver;
    private NSObject? sceneWillDeactivateObserver;

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    private static void HookGlobalExceptionLogging()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            CrashFileLogger.WriteException(
                $"Mac.AppDomain.UnhandledException (IsTerminating={e.IsTerminating})",
                e.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            CrashFileLogger.WriteException("Mac.TaskScheduler.UnobservedTaskException", e.Exception);
        };

        try
        {
            ObjCRuntime.Runtime.MarshalManagedException += (_, args) =>
            {
                CrashFileLogger.WriteException("Mac.ObjCRuntime.MarshalManagedException", args.Exception);
            };
        }
        catch
        {
            // Not available on all runtimes — ignore.
        }
    }

    public override bool CanBecomeFirstResponder => true;

    public override UIKeyCommand[] KeyCommands => [ThemeLightCommand, ThemeDarkCommand, ThemeSystemCommand, UndoHistoryCommand, RedoHistoryCommand];

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        HookGlobalExceptionLogging();
        var result = base.FinishedLaunching(application, launchOptions);
        AttachActivationObservers();
        return result;
    }

    public override void OnResignActivation(UIApplication application)
    {
        base.OnResignActivation(application);
        MarkMacAppInactive();
    }

    public override void WillEnterForeground(UIApplication application)
    {
        base.WillEnterForeground(application);
        MarkMacAppActive();
        CommandEntryHandler.RequestNativeActivationFocus("AppDelegate.WillEnterForeground");
        MainThread.BeginInvokeOnMainThread(() => MainPage.RequestMacCommandFocusFromNativeActivation("AppDelegate.WillEnterForeground"));
    }

    public override void OnActivated(UIApplication application)
    {
        base.OnActivated(application);
        MarkMacAppActive();
        CommandEntryHandler.RequestNativeActivationFocus("AppDelegate.OnActivated");
        MainThread.BeginInvokeOnMainThread(() => MainPage.RequestMacCommandFocusFromNativeActivation("AppDelegate.OnActivated"));
    }

    [Export("handleThemeDark:")]
    private void HandleThemeDark(UIKeyCommand command)
    {
        RaiseThemeShortcutByMacKeyInput("d");
    }

    [Export("handleThemeLight:")]
    private void HandleThemeLight(UIKeyCommand command)
    {
        RaiseThemeShortcutByMacKeyInput("l");
    }

    [Export("handleThemeSystem:")]
    private void HandleThemeSystem(UIKeyCommand command)
    {
        RaiseThemeShortcutByMacKeyInput("h");
    }

    [Export("handleHistoryUndo:")]
    private static void HandleHistoryUndo(UIKeyCommand command)
    {
        MainThread.BeginInvokeOnMainThread(() => App.RaiseHistoryShortcut("Undo"));
    }

    [Export("handleHistoryRedo:")]
    private static void HandleHistoryRedo(UIKeyCommand command)
    {
        MainThread.BeginInvokeOnMainThread(() => App.RaiseHistoryShortcut("Redo"));
    }

    private static UIKeyCommand CreateThemeKeyCommand(string keyInput, string selectorName)
    {
        var command = UIKeyCommand.Create(new NSString(keyInput), UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, new Selector(selectorName));
        TrySetKeyCommandPriorityOverSystem(command);
        return command;
    }

    private static UIKeyCommand CreateHistoryKeyCommand(string keyInput, UIKeyModifierFlags modifierFlags, string selectorName)
    {
        var command = UIKeyCommand.Create(new NSString(keyInput), modifierFlags, new Selector(selectorName));
        TrySetKeyCommandPriorityOverSystem(command);
        return command;
    }

    private static void RaiseThemeShortcutByMacKeyInput(string keyInput)
    {
        if (!ThemeShortcutModeResolver.TryResolveModeFromMacKeyInput(keyInput, out var mode))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => App.RaiseThemeShortcut(mode));
    }

    private static void TrySetKeyCommandPriorityOverSystem(UIKeyCommand command)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        var prop = typeof(UIKeyCommand).GetProperty("WantsPriorityOverSystemBehavior", flags);
        if (prop?.CanWrite == true)
        {
            prop.SetValue(command, true);
            return;
        }

        var method = typeof(UIKeyCommand).GetMethod("SetWantsPriorityOverSystemBehavior", flags);
        method?.Invoke(command, [true]);
    }

    private void AttachActivationObservers()
    {
        if (didBecomeActiveObserver is not null)
        {
            return;
        }

        var center = NSNotificationCenter.DefaultCenter;
        didBecomeActiveObserver = center.AddObserver(UIApplication.DidBecomeActiveNotification, _ =>
        {
            MarkMacAppActive();
            CommandEntryHandler.RequestNativeActivationFocus("Observer.DidBecomeActive");
            MainThread.BeginInvokeOnMainThread(() => MainPage.RequestMacCommandFocusFromNativeActivation("Observer.DidBecomeActive"));
        });
        willEnterForegroundObserver = center.AddObserver(UIApplication.WillEnterForegroundNotification, _ =>
        {
            MarkMacAppActive();
            CommandEntryHandler.RequestNativeActivationFocus("Observer.WillEnterForeground");
            MainThread.BeginInvokeOnMainThread(() => MainPage.RequestMacCommandFocusFromNativeActivation("Observer.WillEnterForeground"));
        });
        windowDidBecomeKeyObserver = center.AddObserver(UIWindow.DidBecomeKeyNotification, _ =>
        {
            if (UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active)
            {
                return;
            }

            MarkMacAppActive();
            CommandEntryHandler.RequestNativeActivationFocus("Observer.WindowDidBecomeKey");
            MainThread.BeginInvokeOnMainThread(() => MainPage.RequestMacCommandFocusFromNativeActivation("Observer.WindowDidBecomeKey"));
        });
        willResignActiveObserver = center.AddObserver(UIApplication.WillResignActiveNotification, _ => MarkMacAppInactive());
        didEnterBackgroundObserver = center.AddObserver(UIApplication.DidEnterBackgroundNotification, _ => MarkMacAppInactive());
        windowDidResignKeyObserver = center.AddObserver(UIWindow.DidResignKeyNotification, _ => MarkMacAppInactive());
        sceneWillDeactivateObserver = center.AddObserver(UIScene.WillDeactivateNotification, _ => MarkMacAppInactive());
    }

    private static void MarkMacAppActive()
    {
        App.RecordActivation();
        App.SetMacApplicationActive(true);
        App.RaiseMacApplicationActivated();
    }

    private static void MarkMacAppInactive()
    {
        App.SetMacApplicationActive(false);
        App.RaiseMacApplicationDeactivating();
    }
}
