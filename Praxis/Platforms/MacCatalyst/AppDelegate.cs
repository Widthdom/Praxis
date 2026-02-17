using Foundation;
using ObjCRuntime;
using System.Reflection;
using UIKit;

namespace Praxis;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private static readonly string EscapeKeyInput = ResolveKeyInput("InputEscape", "\u001B");
    private static readonly string UpArrowKeyInput = ResolveKeyInput("InputUpArrow", "\uF700");
    private static readonly string DownArrowKeyInput = ResolveKeyInput("InputDownArrow", "\uF701");
    private static readonly UIKeyCommand EscapeCommand = CreateEscapeCommand();
    private static readonly UIKeyCommand CommandSuggestionUpCommand = CreateCommandSuggestionCommand(UpArrowKeyInput, "handleCommandSuggestionUp:");
    private static readonly UIKeyCommand CommandSuggestionDownCommand = CreateCommandSuggestionCommand(DownArrowKeyInput, "handleCommandSuggestionDown:");

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    public override bool CanBecomeFirstResponder => true;

    public override UIKeyCommand[] KeyCommands => new[]
    {
        UIKeyCommand.Create(new NSString("d"), UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, new Selector("handleThemeDark:")),
        UIKeyCommand.Create(new NSString("l"), UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, new Selector("handleThemeLight:")),
        UIKeyCommand.Create(new NSString("h"), UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, new Selector("handleThemeSystem:")),
        UIKeyCommand.Create(new NSString("s"), UIKeyModifierFlags.Command, new Selector("handleEditorSave:")),
        UIKeyCommand.Create(new NSString("."), UIKeyModifierFlags.Command, new Selector("handleEditorCancel:")),
        EscapeCommand,
        CommandSuggestionUpCommand,
        CommandSuggestionDownCommand,
    };

    public override void OnActivated(UIApplication application)
    {
        base.OnActivated(application);
        EnsureKeyWindow(application);
        if (CanBecomeFirstResponder)
        {
            BecomeFirstResponder();
        }
    }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            EnsureKeyWindow(application);
            if (CanBecomeFirstResponder)
            {
                BecomeFirstResponder();
            }
        });

        return result;
    }

    [Export("handleThemeDark:")]
    private void HandleThemeDark(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseThemeShortcut("Dark"));

    [Export("handleThemeLight:")]
    private void HandleThemeLight(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseThemeShortcut("Light"));

    [Export("handleThemeSystem:")]
    private void HandleThemeSystem(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseThemeShortcut("System"));

    [Export("handleEditorSave:")]
    private void HandleEditorSave(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            App.RaiseEditorShortcut("Save");
        });

    [Export("handleEditorCancel:")]
    private void HandleEditorCancel(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            App.RaiseEditorShortcut("Cancel");
        });

    [Export("handleCommandSuggestionUp:")]
    private void HandleCommandSuggestionUp(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseCommandInputShortcut("Up"));

    [Export("handleCommandSuggestionDown:")]
    private void HandleCommandSuggestionDown(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseCommandInputShortcut("Down"));

    [Export("handleEditorTabNext:")]
    private void HandleEditorTabNext(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("TabNext"));

    [Export("handleEditorTabPrevious:")]
    private void HandleEditorTabPrevious(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("TabPrevious"));

    [Export("handleEditorPrimaryAction:")]
    private void HandleEditorPrimaryAction(UIKeyCommand command)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("PrimaryAction"));

    [Export("cancelOperation:")]
    private void CancelOperation(NSObject? sender)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("Cancel"));

    [Export("cancel:")]
    private void Cancel(NSObject? sender)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("Cancel"));

    [Export("dismiss:")]
    private void Dismiss(NSObject? sender)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("Cancel"));

    [Export("save:")]
    private void Save(NSObject? sender)
        => MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("Save"));

    [Export("otherMouseDown:")]
    private void OtherMouseDown(NSObject? evt)
    {
        HandleOtherMouseEvent(evt);
    }

    [Export("otherMouseUp:")]
    private void OtherMouseUp(NSObject? evt)
    {
        HandleOtherMouseEvent(evt);
    }

    [Export("otherMouseDragged:")]
    private void OtherMouseDragged(NSObject? evt)
    {
        HandleOtherMouseEvent(evt);
    }

    public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent? evt)
    {
        if (TryHandleCommandSuggestionNavigationPress(presses))
        {
            return;
        }

        if (TryHandleEscapePress(presses))
        {
            return;
        }

        if (evt is null)
        {
            return;
        }

        base.PressesBegan(presses, evt);
    }

    private static bool TryHandleCommandSuggestionNavigationPress(NSSet<UIPress> presses)
    {
        foreach (var pressObject in presses)
        {
            if (pressObject is not UIPress press)
            {
                continue;
            }

            if (IsUpArrowPress(press))
            {
                MainThread.BeginInvokeOnMainThread(() => App.RaiseCommandInputShortcut("Up"));
                return true;
            }

            if (IsDownArrowPress(press))
            {
                MainThread.BeginInvokeOnMainThread(() => App.RaiseCommandInputShortcut("Down"));
                return true;
            }
        }

        return false;
    }

    private static bool IsUpArrowPress(UIPress press)
        => IsArrowPress(press, UpArrowKeyInput, "UpArrow", 82);

    private static bool IsDownArrowPress(UIPress press)
        => IsArrowPress(press, DownArrowKeyInput, "DownArrow", 81);

    private static bool IsArrowPress(UIPress press, string keyInput, string keyCodeName, int keyCodeNumeric)
    {
        var key = press.Key;
        if (key is null)
        {
            return false;
        }

        if (string.Equals(key.CharactersIgnoringModifiers, keyInput, StringComparison.Ordinal) ||
            string.Equals(key.Characters, keyInput, StringComparison.Ordinal))
        {
            return true;
        }

        var keyCodeProp = key.GetType().GetProperty("KeyCode");
        var keyCodeValue = keyCodeProp?.GetValue(key);
        if (keyCodeValue is null)
        {
            return false;
        }

        var keyCodeText = keyCodeValue.ToString() ?? string.Empty;
        if (keyCodeText.Contains(keyCodeName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(keyCodeText, out var numericCode) && numericCode == keyCodeNumeric;
    }

    private static bool TryHandleEscapePress(NSSet<UIPress> presses)
    {
        foreach (var pressObject in presses)
        {
            if (pressObject is not UIPress press)
            {
                continue;
            }

            if (!IsEscapePress(press))
            {
                continue;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                App.RaiseEditorShortcut("Cancel");
            });
            return true;
        }

        return false;
    }

    private static bool IsEscapePress(UIPress press)
    {
        var key = press.Key;
        if (key is null)
        {
            return false;
        }

        if (string.Equals(key.CharactersIgnoringModifiers, "\u001B", StringComparison.Ordinal) ||
            string.Equals(key.Characters, "\u001B", StringComparison.Ordinal))
        {
            return true;
        }

        var keyCodeProp = key.GetType().GetProperty("KeyCode");
        var keyCodeValue = keyCodeProp?.GetValue(key);
        if (keyCodeValue is null)
        {
            return false;
        }

        var keyCodeText = keyCodeValue.ToString() ?? string.Empty;
        if (keyCodeText.Contains("Escape", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(keyCodeText, out var numericCode) && numericCode == 41;
    }

    private static string ResolveEscapeKeyInput()
        => ResolveKeyInput("InputEscape", "\u001B");

    private static string ResolveKeyInput(string inputName, string fallback)
    {
        var keyInputProperty = typeof(UIKeyCommand).GetProperty(inputName, BindingFlags.Public | BindingFlags.Static);
        if (keyInputProperty?.GetValue(null) is NSString nsInput)
        {
            return nsInput.ToString();
        }

        if (keyInputProperty?.GetValue(null) is string inputText && !string.IsNullOrEmpty(inputText))
        {
            return inputText;
        }

        var keyInputField = typeof(UIKeyCommand).GetField(inputName, BindingFlags.Public | BindingFlags.Static);
        if (keyInputField?.GetValue(null) is NSString nsInputField)
        {
            return nsInputField.ToString();
        }

        if (keyInputField?.GetValue(null) is string inputFieldText && !string.IsNullOrEmpty(inputFieldText))
        {
            return inputFieldText;
        }

        return fallback;
    }

    private static UIKeyCommand CreateEscapeCommand()
    {
        var command = UIKeyCommand.Create(new NSString(EscapeKeyInput), 0, new Selector("handleEditorCancel:"));
        TrySetKeyCommandPriorityOverSystem(command);
        return command;
    }

    private static UIKeyCommand CreateCommandSuggestionCommand(string keyInput, string selectorName)
    {
        var command = UIKeyCommand.Create(new NSString(keyInput), 0, new Selector(selectorName));
        TrySetKeyCommandPriorityOverSystem(command);
        return command;
    }

    private static void TrySetKeyCommandPriorityOverSystem(UIKeyCommand command)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var prop = typeof(UIKeyCommand).GetProperty("WantsPriorityOverSystemBehavior", flags);
        if (prop?.CanWrite == true)
        {
            prop.SetValue(command, true);
            return;
        }

        var method = typeof(UIKeyCommand).GetMethod("SetWantsPriorityOverSystemBehavior", flags);
        method?.Invoke(command, [true]);
    }

    private static void HandleOtherMouseEvent(NSObject? evt)
    {
        if (evt is null)
        {
            return;
        }

        var button = TryGetButtonNumber(evt);
        if (button >= 2)
        {
            MainThread.BeginInvokeOnMainThread(App.RaiseMiddleMouseClick);
        }
    }

    private static nint TryGetButtonNumber(NSObject evt)
    {
        try
        {
            var value = evt.ValueForKey(new NSString("buttonNumber"));
            if (value is NSNumber number)
            {
                return number.NIntValue;
            }
        }
        catch
        {
        }

        return -1;
    }

    private static void EnsureKeyWindow(UIApplication application)
    {
        foreach (var scene in application.ConnectedScenes)
        {
            if (scene is not UIWindowScene windowScene)
            {
                continue;
            }

            foreach (var window in windowScene.Windows)
            {
                if (window.IsKeyWindow)
                {
                    return;
                }

                window.MakeKeyAndVisible();
            }
        }
    }
}
