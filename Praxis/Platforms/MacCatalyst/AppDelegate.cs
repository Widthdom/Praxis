using Foundation;
using ObjCRuntime;
using UIKit;
using Praxis.Core.Logic;

namespace Praxis;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private static readonly UIKeyCommand ThemeDarkCommand = CreateThemeKeyCommand("d", "handleThemeDark:");
    private static readonly UIKeyCommand ThemeLightCommand = CreateThemeKeyCommand("l", "handleThemeLight:");
    private static readonly UIKeyCommand ThemeSystemCommand = CreateThemeKeyCommand("h", "handleThemeSystem:");

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool CanBecomeFirstResponder => true;

    public override UIKeyCommand[] KeyCommands => [ThemeLightCommand, ThemeDarkCommand, ThemeSystemCommand];

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (CanBecomeFirstResponder)
            {
                BecomeFirstResponder();
            }
        });
        return result;
    }

    public override void OnActivated(UIApplication application)
    {
        base.OnActivated(application);
        if (CanBecomeFirstResponder)
        {
            BecomeFirstResponder();
        }
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
}
