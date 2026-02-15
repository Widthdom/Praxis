using Foundation;
using ObjCRuntime;
using UIKit;

namespace Praxis;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override UIKeyCommand[] KeyCommands => new[]
    {
        UIKeyCommand.Create(new NSString("d"), UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, new Selector("handleThemeDark:")),
        UIKeyCommand.Create(new NSString("l"), UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, new Selector("handleThemeLight:")),
        UIKeyCommand.Create(new NSString("h"), UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, new Selector("handleThemeSystem:")),
    };

    [Export("handleThemeDark:")]
    private void HandleThemeDark(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseThemeShortcut("Dark"));

    [Export("handleThemeLight:")]
    private void HandleThemeLight(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseThemeShortcut("Light"));

    [Export("handleThemeSystem:")]
    private void HandleThemeSystem(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseThemeShortcut("System"));
}
