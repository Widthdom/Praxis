#if MACCATALYST
using Foundation;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace Praxis.Controls;

public class CommandEntryHandler : EntryHandler
{
    protected override MauiTextField CreatePlatformView()
    {
        return new CommandEntryTextField();
    }

    protected override void ConnectHandler(MauiTextField platformView)
    {
        base.ConnectHandler(platformView);
        platformView.BorderStyle = UITextBorderStyle.RoundedRect;
    }

    private sealed class CommandEntryTextField : MauiTextField
    {
        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent? evt)
        {
            if (TryHandleSuggestionNavigation(presses))
            {
                return;
            }

            if (evt is null)
            {
                return;
            }

            base.PressesBegan(presses, evt);
        }

        private static bool TryHandleSuggestionNavigation(NSSet<UIPress> presses)
        {
            foreach (var pressObject in presses)
            {
                if (pressObject is not UIPress press)
                {
                    continue;
                }

                if (IsArrowPress(press, "UpArrow", 82))
                {
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseCommandInputShortcut("Up"));
                    return true;
                }

                if (IsArrowPress(press, "DownArrow", 81))
                {
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseCommandInputShortcut("Down"));
                    return true;
                }
            }

            return false;
        }

        private static bool IsArrowPress(UIPress press, string keyCodeName, int keyCodeNumeric)
        {
            var key = press.Key;
            if (key is null)
            {
                return false;
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
    }
}
#endif
