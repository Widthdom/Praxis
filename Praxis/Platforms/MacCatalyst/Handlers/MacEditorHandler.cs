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

    protected override MauiTextView CreatePlatformView()
    {
        return new MacEditorTextView();
    }

    private sealed class MacEditorTextView : MauiTextView
    {
        private UIKeyCommand? tabNextCommand;
        private UIKeyCommand? tabPreviousCommand;

        public override UIKeyCommand[] KeyCommands
        {
            get
            {
                tabNextCommand ??= UIKeyCommand.Create(new NSString(TabKeyInput), 0, new Selector("handleEditorTabNext:"));
                tabPreviousCommand ??= UIKeyCommand.Create(new NSString(TabKeyInput), UIKeyModifierFlags.Shift, new Selector("handleEditorTabPrevious:"));

                var baseCommands = base.KeyCommands ?? Array.Empty<UIKeyCommand>();
                var commands = new UIKeyCommand[baseCommands.Length + 2];
                commands[0] = tabNextCommand;
                commands[1] = tabPreviousCommand;
                if (baseCommands.Length > 0)
                {
                    Array.Copy(baseCommands, 0, commands, 2, baseCommands.Length);
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

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent? evt)
        {
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
