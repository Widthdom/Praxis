using Foundation;
using ObjCRuntime;
using UIKit;
using Praxis.Controls;
using Praxis.Core.Logic;

namespace Praxis;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private static readonly UIKeyCommand ThemeDarkCommand = CreateThemeKeyCommand("d", "handleThemeDark:");
    private static readonly UIKeyCommand ThemeLightCommand = CreateThemeKeyCommand("l", "handleThemeLight:");
    private static readonly UIKeyCommand ThemeSystemCommand = CreateThemeKeyCommand("h", "handleThemeSystem:");
    private NSObject? didBecomeActiveObserver;
    private NSObject? willEnterForegroundObserver;
    private NSObject? windowDidBecomeKeyObserver;

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool CanBecomeFirstResponder => true;

    public override UIKeyCommand[] KeyCommands => [ThemeLightCommand, ThemeDarkCommand, ThemeSystemCommand];

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);
        AttachActivationObservers();
        return result;
    }

    public override void OnResignActivation(UIApplication application)
    {
        base.OnResignActivation(application);
        App.SetMacApplicationActive(false);
        App.RaiseMacApplicationDeactivating();
    }

    public override void WillEnterForeground(UIApplication application)
    {
        base.WillEnterForeground(application);
        App.RecordActivation();
        App.SetMacApplicationActive(true);
        App.RaiseMacApplicationActivated();
        CommandEntryHandler.RequestNativeActivationFocus("AppDelegate.WillEnterForeground");
        MainThread.BeginInvokeOnMainThread(() => MainPage.RequestMacCommandFocusFromNativeActivation("AppDelegate.WillEnterForeground"));
    }

    public override void OnActivated(UIApplication application)
    {
        base.OnActivated(application);
        App.RecordActivation();
        App.SetMacApplicationActive(true);
        App.RaiseMacApplicationActivated();
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

    private static UIKeyCommand CreateThemeKeyCommand(string keyInput, string selectorName)
    {
        var command = UIKeyCommand.Create(new NSString(keyInput), UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, new Selector(selectorName));
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
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            App.RaiseMacApplicationActivated();
            CommandEntryHandler.RequestNativeActivationFocus("Observer.DidBecomeActive");
            MainThread.BeginInvokeOnMainThread(() => MainPage.RequestMacCommandFocusFromNativeActivation("Observer.DidBecomeActive"));
        });
        willEnterForegroundObserver = center.AddObserver(UIApplication.WillEnterForegroundNotification, _ =>
        {
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            App.RaiseMacApplicationActivated();
            CommandEntryHandler.RequestNativeActivationFocus("Observer.WillEnterForeground");
            MainThread.BeginInvokeOnMainThread(() => MainPage.RequestMacCommandFocusFromNativeActivation("Observer.WillEnterForeground"));
        });
        windowDidBecomeKeyObserver = center.AddObserver(UIWindow.DidBecomeKeyNotification, _ =>
        {
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            App.RaiseMacApplicationActivated();
            CommandEntryHandler.RequestNativeActivationFocus("Observer.WindowDidBecomeKey");
            MainThread.BeginInvokeOnMainThread(() => MainPage.RequestMacCommandFocusFromNativeActivation("Observer.WindowDidBecomeKey"));
        });
    }
}
