#if MACCATALYST
using Foundation;
using Microsoft.Maui.ApplicationModel;
using Praxis.Core.Logic;
using UIKit;

namespace Praxis.Controls;

public class CommandEntryHandler : MacEntryHandler
{
    private static readonly string UpArrowKeyInput = ResolveKeyInput("InputUpArrow", "\uF700");
    private static readonly string DownArrowKeyInput = ResolveKeyInput("InputDownArrow", "\uF701");
    private static readonly string LeftArrowKeyInput = ResolveKeyInput("InputLeftArrow", "\uF702");
    private static readonly string RightArrowKeyInput = ResolveKeyInput("InputRightArrow", "\uF703");
    private static readonly string TabKeyInput = ResolveKeyInput("InputTab", "\t");
    private static readonly string EscapeKeyInput = ResolveKeyInput("InputEscape", "\u001B");
    private static readonly string ReturnKeyInput = ResolveKeyInput("InputReturn", "\r");
    private static readonly string? EnterKeyInput = TryResolveKeyInput("InputEnter");
    private static WeakReference<CommandEntryTextField>? lastCommandField;

    public static void RequestNativeActivationFocus(string source)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (lastCommandField is null || !lastCommandField.TryGetTarget(out var field))
            {
                return;
            }

            field.TryApplyNativeActivationFocus();
            NSTimer.CreateScheduledTimer(0.12, _ => field.TryApplyNativeActivationFocus());
        });
    }

    protected override MacEntryTextField CreatePlatformView()
    {
        return new CommandEntryTextField();
    }

    private sealed class CommandEntryTextField : MacEntryTextField
    {
        private NSObject? windowDidBecomeKeyObserver;
        private NSObject? didBecomeActiveObserver;

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent? evt)
        {
            if (TryHandleEditorShortcuts(presses))
            {
                return;
            }

            if (TryHandleSuggestionNavigation(presses))
            {
                return;
            }

            if (TryHandleSearchFocusTabNavigation(presses))
            {
                return;
            }

            base.PressesBegan(presses, evt);
        }

        public override void MovedToWindow()
        {
            base.MovedToWindow();
            if (Window is not null)
            {
                lastCommandField = new WeakReference<CommandEntryTextField>(this);
            }

            AttachWindowDidBecomeKeyObserver();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DetachWindowDidBecomeKeyObserver();
                DetachDidBecomeActiveObserver();
                if (lastCommandField is not null &&
                    lastCommandField.TryGetTarget(out var activeField) &&
                    ReferenceEquals(activeField, this))
                {
                    lastCommandField = null;
                }
            }

            base.Dispose(disposing);
        }

        private static bool TryHandleEditorShortcuts(NSSet<UIPress> presses)
        {
            foreach (var pressObject in presses)
            {
                if (pressObject is not UIPress press)
                {
                    continue;
                }

                var key = press.Key;
                if (key is null)
                {
                    continue;
                }

                var modifiers = key.ModifierFlags;
                var shiftDown = (modifiers & UIKeyModifierFlags.Shift) != 0;
                var commandDown = (modifiers & UIKeyModifierFlags.Command) != 0;

                if (App.IsContextMenuOpen)
                {
                    if (IsArrowPress(press, UpArrowKeyInput, "UpArrow", 82))
                    {
                        var action = EditorShortcutActionResolver.ResolveContextMenuArrowNavigationAction(downArrow: false);
                        MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                        return true;
                    }

                    if (IsArrowPress(press, DownArrowKeyInput, "DownArrow", 81))
                    {
                        var action = EditorShortcutActionResolver.ResolveContextMenuArrowNavigationAction(downArrow: true);
                        MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                        return true;
                    }
                }

                if (App.IsConflictDialogOpen)
                {
                    if (IsArrowPress(press, LeftArrowKeyInput, "LeftArrow", 80))
                    {
                        var action = EditorShortcutActionResolver.ResolveConflictDialogArrowNavigationAction(rightArrow: false);
                        MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                        return true;
                    }

                    if (IsArrowPress(press, RightArrowKeyInput, "RightArrow", 79))
                    {
                        var action = EditorShortcutActionResolver.ResolveConflictDialogArrowNavigationAction(rightArrow: true);
                        MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                        return true;
                    }
                }

                if (IsKeyInput(key, TabKeyInput) &&
                    EditorShortcutScopeResolver.IsEditorShortcutScopeActive(App.IsConflictDialogOpen, App.IsContextMenuOpen, App.IsEditorOpen))
                {
                    var action = EditorShortcutActionResolver.ResolveTabNavigationAction(shiftDown);
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                    return true;
                }

                if (IsKeyInput(key, EscapeKeyInput) &&
                    EditorShortcutScopeResolver.IsEditorShortcutScopeActive(App.IsConflictDialogOpen, App.IsContextMenuOpen, App.IsEditorOpen))
                {
                    var action = EditorShortcutActionResolver.ResolveCancelAction();
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                    return true;
                }

                if (App.IsEditorOpen && !App.IsConflictDialogOpen && commandDown && IsKeyInput(key, "s"))
                {
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("Save"));
                    return true;
                }

                if (EditorShortcutScopeResolver.IsEditorShortcutScopeActive(App.IsConflictDialogOpen, App.IsContextMenuOpen, App.IsEditorOpen) &&
                    (IsKeyInput(key, ReturnKeyInput) ||
                     (!string.IsNullOrEmpty(EnterKeyInput) && IsKeyInput(key, EnterKeyInput!))))
                {
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("PrimaryAction"));
                    return true;
                }
            }

            return false;
        }

        private static bool TryHandleSuggestionNavigation(NSSet<UIPress> presses)
        {
            foreach (var pressObject in presses)
            {
                if (pressObject is not UIPress press)
                {
                    continue;
                }

                if (IsArrowPress(press, UpArrowKeyInput, "UpArrow", 82))
                {
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseCommandInputShortcut("Up"));
                    return true;
                }

                if (IsArrowPress(press, DownArrowKeyInput, "DownArrow", 81))
                {
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseCommandInputShortcut("Down"));
                    return true;
                }
            }

            return false;
        }

        private static bool TryHandleSearchFocusTabNavigation(NSSet<UIPress> presses)
        {
            if (EditorShortcutScopeResolver.IsEditorShortcutScopeActive(
                    App.IsConflictDialogOpen,
                    App.IsContextMenuOpen,
                    App.IsEditorOpen))
            {
                return false;
            }

            foreach (var pressObject in presses)
            {
                if (pressObject is not UIPress press)
                {
                    continue;
                }

                var key = press.Key;
                if (key is null || !IsKeyInput(key, TabKeyInput))
                {
                    continue;
                }

                var modifiers = key.ModifierFlags;
                if ((modifiers & UIKeyModifierFlags.Shift) != 0)
                {
                    continue;
                }

                if ((modifiers & (UIKeyModifierFlags.Command | UIKeyModifierFlags.Control | UIKeyModifierFlags.Alternate)) != 0)
                {
                    continue;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                    MainPage.FocusMacSearchEntryFromCommandTab("CommandEntry.Tab"));
                return true;
            }

            return false;
        }

        private void AttachWindowDidBecomeKeyObserver()
        {
            DetachWindowDidBecomeKeyObserver();
            var currentWindow = Window;
            if (currentWindow is null)
            {
                return;
            }

            windowDidBecomeKeyObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                UIWindow.DidBecomeKeyNotification,
                notification => OnWindowDidBecomeKey(notification));
            AttachDidBecomeActiveObserver();
        }

        private void DetachWindowDidBecomeKeyObserver()
        {
            if (windowDidBecomeKeyObserver is null)
            {
                return;
            }

            NSNotificationCenter.DefaultCenter.RemoveObserver(windowDidBecomeKeyObserver);
            windowDidBecomeKeyObserver.Dispose();
            windowDidBecomeKeyObserver = null;
        }

        private void AttachDidBecomeActiveObserver()
        {
            DetachDidBecomeActiveObserver();
            didBecomeActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.DidBecomeActiveNotification,
                _ => OnDidBecomeActive());
        }

        private void DetachDidBecomeActiveObserver()
        {
            if (didBecomeActiveObserver is null)
            {
                return;
            }

            NSNotificationCenter.DefaultCenter.RemoveObserver(didBecomeActiveObserver);
            didBecomeActiveObserver.Dispose();
            didBecomeActiveObserver = null;
        }

        private void OnWindowDidBecomeKey(NSNotification _)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TryApplyNativeActivationFocus();
                NSTimer.CreateScheduledTimer(0.12, _ => TryApplyNativeActivationFocus());
            });
        }

        private void OnDidBecomeActive()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TryApplyNativeActivationFocus();
                NSTimer.CreateScheduledTimer(0.12, _ => TryApplyNativeActivationFocus());
            });
        }

        internal void TryApplyNativeActivationFocus()
        {
            var currentWindow = Window;
            if (currentWindow is null)
            {
                return;
            }

            if (!currentWindow.IsKeyWindow)
            {
                return;
            }

            if (App.IsEditorOpen || App.IsConflictDialogOpen)
            {
                return;
            }

            if (!IsFirstResponder && CanBecomeFirstResponder)
            {
                BecomeFirstResponder();
            }

            SelectAllText();
        }

        private void SelectAllText()
        {
            var allTextRange = GetTextRange(BeginningOfDocument, EndOfDocument);
            if (allTextRange is not null)
            {
                SelectedTextRange = allTextRange;
            }
        }

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

        private static bool IsKeyInput(UIKey key, string input)
        {
            return string.Equals(key.CharactersIgnoringModifiers, input, StringComparison.Ordinal) ||
                   string.Equals(key.Characters, input, StringComparison.Ordinal);
        }
    }

    private static string ResolveKeyInput(string inputName, string fallback)
    {
        return TryResolveKeyInput(inputName) ?? fallback;
    }

    private static string? TryResolveKeyInput(string inputName)
    {
        var keyInputProperty = typeof(UIKeyCommand).GetProperty(inputName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (keyInputProperty?.GetValue(null) is NSString nsInput)
        {
            return nsInput.ToString();
        }

        if (keyInputProperty?.GetValue(null) is string inputText && !string.IsNullOrEmpty(inputText))
        {
            return inputText;
        }

        var keyInputField = typeof(UIKeyCommand).GetField(inputName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (keyInputField?.GetValue(null) is NSString nsInputField)
        {
            return nsInputField.ToString();
        }

        if (keyInputField?.GetValue(null) is string inputFieldText && !string.IsNullOrEmpty(inputFieldText))
        {
            return inputFieldText;
        }

        return null;
    }
}
#endif
