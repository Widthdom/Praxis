#if MACCATALYST
using Foundation;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using Praxis.Core.Logic;
using UIKit;

namespace Praxis.Controls;

public class MacEditorHandler : EditorHandler
{
    private static readonly string TabKeyInput = ResolveKeyInput("InputTab", "\t");
    private static readonly string EscapeKeyInput = ResolveKeyInput("InputEscape", "\u001B");
    private static readonly string LeftArrowKeyInput = ResolveKeyInput("InputLeftArrow", "\uF702");
    private static readonly string RightArrowKeyInput = ResolveKeyInput("InputRightArrow", "\uF703");

    protected override MauiTextView CreatePlatformView()
    {
        return new MacEditorTextView();
    }

    private sealed class MacEditorTextView : MauiTextView
    {
        private UIKeyCommand? tabNextCommand;
        private UIKeyCommand? tabPreviousCommand;
        private UIKeyCommand? cancelCommand;

        public override UIKeyCommand[] KeyCommands
        {
            get
            {
                tabNextCommand ??= CreateEditorKeyCommand(TabKeyInput, 0, "handleEditorTabNext:");
                tabPreviousCommand ??= CreateEditorKeyCommand(TabKeyInput, UIKeyModifierFlags.Shift, "handleEditorTabPrevious:");
                cancelCommand ??= CreateEditorKeyCommand(EscapeKeyInput, 0, "handleEditorCancel:");

                var baseCommands = base.KeyCommands ?? Array.Empty<UIKeyCommand>();
                var commands = new UIKeyCommand[baseCommands.Length + 3];
                commands[0] = tabNextCommand;
                commands[1] = tabPreviousCommand;
                commands[2] = cancelCommand;
                if (baseCommands.Length > 0)
                {
                    Array.Copy(baseCommands, 0, commands, 3, baseCommands.Length);
                }
                return commands;
            }
        }

        [Export("handleEditorTabNext:")]
        private void HandleEditorTabNext(UIKeyCommand command)
        {
            if (!TryDispatchTabNavigation(shiftDown: false))
            {
                base.InsertText(TabKeyInput);
            }
        }

        [Export("handleEditorTabPrevious:")]
        private void HandleEditorTabPrevious(UIKeyCommand command)
        {
            if (!TryDispatchTabNavigation(shiftDown: true))
            {
                base.InsertText(TabKeyInput);
            }
        }

        [Export("handleEditorCancel:")]
        private void HandleEditorCancel(UIKeyCommand command)
        {
            TryDispatchCancelShortcut();
        }

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent? evt)
        {
            if (TryHandleEditorCancelShortcut(presses))
            {
                return;
            }

            if (TryHandleConflictDialogArrowNavigation(presses))
            {
                return;
            }

            if (TryHandleEditorTabNavigation(presses))
            {
                return;
            }

            if (evt is null)
            {
                return;
            }

            base.PressesBegan(presses, evt);
        }

        public override void InsertText(string text)
        {
            if (string.Equals(text, TabKeyInput, StringComparison.Ordinal) && TryDispatchTabNavigation(shiftDown: false))
            {
                return;
            }

            base.InsertText(text);
        }

        private static bool TryHandleEditorTabNavigation(NSSet<UIPress> presses)
        {
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

                var shiftDown = (key.ModifierFlags & UIKeyModifierFlags.Shift) != 0;
                return TryDispatchTabNavigation(shiftDown);
            }

            return false;
        }

        private static bool TryHandleEditorCancelShortcut(NSSet<UIPress> presses)
        {
            foreach (var pressObject in presses)
            {
                if (pressObject is not UIPress press)
                {
                    continue;
                }

                var key = press.Key;
                if (key is null || !IsKeyInput(key, EscapeKeyInput))
                {
                    continue;
                }

                return TryDispatchCancelShortcut();
            }

            return false;
        }

        private static bool TryDispatchCancelShortcut()
        {
            var scopeActive = EditorShortcutScopeResolver.IsEditorShortcutScopeActive(App.IsConflictDialogOpen, App.IsContextMenuOpen, App.IsEditorOpen);
            if (!scopeActive)
            {
                return false;
            }

            var action = EditorShortcutActionResolver.ResolveCancelAction();
            MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
            return true;
        }

        private static UIKeyCommand CreateEditorKeyCommand(string input, UIKeyModifierFlags modifiers, string selectorName)
        {
            var command = UIKeyCommand.Create(new NSString(input), modifiers, new Selector(selectorName));
            TrySetKeyCommandPriorityOverSystem(command);
            return command;
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

        private static bool TryDispatchTabNavigation(bool shiftDown)
        {
            if (!EditorShortcutScopeResolver.IsEditorShortcutScopeActive(App.IsConflictDialogOpen, App.IsContextMenuOpen, App.IsEditorOpen))
            {
                return false;
            }

            var action = EditorShortcutActionResolver.ResolveTabNavigationAction(shiftDown);
            MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
            return true;
        }

        private static bool TryHandleConflictDialogArrowNavigation(NSSet<UIPress> presses)
        {
            if (!App.IsConflictDialogOpen)
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
                if (key is null)
                {
                    continue;
                }

                if (IsArrowPress(key, LeftArrowKeyInput, "LeftArrow", 80))
                {
                    var action = EditorShortcutActionResolver.ResolveConflictDialogArrowNavigationAction(rightArrow: false);
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                    return true;
                }

                if (IsArrowPress(key, RightArrowKeyInput, "RightArrow", 79))
                {
                    var action = EditorShortcutActionResolver.ResolveConflictDialogArrowNavigationAction(rightArrow: true);
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                    return true;
                }
            }

            return false;
        }

        private static bool IsKeyInput(UIKey key, string input)
        {
            return string.Equals(key.CharactersIgnoringModifiers, input, StringComparison.Ordinal) ||
                   string.Equals(key.Characters, input, StringComparison.Ordinal);
        }

        private static bool IsArrowPress(UIKey key, string keyInput, string keyCodeName, int keyCodeNumeric)
        {
            if (IsKeyInput(key, keyInput))
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
