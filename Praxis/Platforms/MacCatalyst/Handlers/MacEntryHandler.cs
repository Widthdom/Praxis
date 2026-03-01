#if MACCATALYST
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using Praxis.Core.Logic;
using UIKit;

namespace Praxis.Controls;

public class MacEntryHandler : EntryHandler
{
    private static readonly string TabKeyInput = ResolveKeyInput("InputTab", "\t");
    private static readonly string EscapeKeyInput = ResolveKeyInput("InputEscape", "\u001B");
    private static readonly string ReturnKeyInput = ResolveKeyInput("InputReturn", "\r");
    private static readonly string? EnterKeyInput = TryResolveKeyInput("InputEnter");
    private static readonly string UpArrowKeyInput = ResolveKeyInput("InputUpArrow", "\uF700");
    private static readonly string DownArrowKeyInput = ResolveKeyInput("InputDownArrow", "\uF701");
    private static readonly string LeftArrowKeyInput = ResolveKeyInput("InputLeftArrow", "\uF702");
    private static readonly string RightArrowKeyInput = ResolveKeyInput("InputRightArrow", "\uF703");

    protected override MauiTextField CreatePlatformView()
    {
        return new MacEntryTextField();
    }

    public class MacEntryTextField : MauiTextField
    {
        private static readonly CGColor LightBorderColor = UIColor.FromRGB(0xCE, 0xCE, 0xCE).CGColor;
        private static readonly CGColor DarkBorderColor = UIColor.FromRGB(0x4E, 0x4E, 0x4E).CGColor;
        private static readonly CGColor LightFocusUnderlineColor = UIColor.FromRGB(0x4A, 0x4A, 0x4A).CGColor;
        private static readonly CGColor DarkFocusUnderlineColor = UIColor.FromRGB(0xA0, 0xA0, 0xA0).CGColor;
        private static readonly nfloat CornerRadius = 4;
        private static readonly nfloat BorderWidth = 1;
        private static readonly nfloat FocusBorderWidth = 1.5f;
        private static readonly nfloat HorizontalInset = 10;
        private readonly CAShapeLayer borderLayer = new();
        private readonly CAShapeLayer focusBorderLayer = new();
        private readonly CALayer focusBorderMaskLayer = new();
        private bool pseudoFocused;
        private UIKeyCommand? cancelCommand;

        public override UIKeyCommand[] KeyCommands
        {
            get
            {
                var baseCommands = base.KeyCommands ?? Array.Empty<UIKeyCommand>();
                if (!EditorShortcutScopeResolver.IsEditorShortcutScopeActive(App.IsConflictDialogOpen, App.IsContextMenuOpen, App.IsEditorOpen))
                {
                    return baseCommands;
                }

                cancelCommand ??= CreateEntryKeyCommand(EscapeKeyInput, 0, "handleEntryCancel:");
                var commands = new UIKeyCommand[baseCommands.Length + 1];
                commands[0] = cancelCommand;
                if (baseCommands.Length > 0)
                {
                    Array.Copy(baseCommands, 0, commands, 1, baseCommands.Length);
                }

                return commands;
            }
        }

        public MacEntryTextField()
        {
            BorderStyle = UITextBorderStyle.None;
            Layer.CornerRadius = CornerRadius;
            Layer.BorderWidth = 0;
            Layer.MasksToBounds = false;

            borderLayer.FillColor = UIColor.Clear.CGColor;
            borderLayer.LineWidth = BorderWidth;
            Layer.AddSublayer(borderLayer);

            focusBorderLayer.FillColor = UIColor.Clear.CGColor;
            focusBorderLayer.LineWidth = FocusBorderWidth;
            focusBorderLayer.Mask = focusBorderMaskLayer;
            focusBorderLayer.Hidden = true;
            Layer.AddSublayer(focusBorderLayer);
        }

        public override CGRect TextRect(CGRect forBounds)
            => forBounds.Inset(HorizontalInset, 0);

        public override CGRect EditingRect(CGRect forBounds)
            => forBounds.Inset(HorizontalInset, 0);

        public override CGRect PlaceholderRect(CGRect forBounds)
            => forBounds.Inset(HorizontalInset, 0);

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent? evt)
        {
            if (TryHandleEditorShortcuts(presses))
            {
                return;
            }

            if (evt is null)
            {
                TryInsertTextFromNullEventPresses(presses);
                return;
            }

            base.PressesBegan(presses, evt);
        }

        [Export("handleEntryCancel:")]
        private void HandleEntryCancel(UIKeyCommand command)
        {
            TryDispatchCancelShortcut();
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            var halfBorderWidth = BorderWidth / 2;
            var borderRect = Bounds.Inset(halfBorderWidth, halfBorderWidth);
            borderLayer.Path = UIBezierPath.FromRoundedRect(borderRect, CornerRadius).CGPath;

            var focusHalfBorderWidth = FocusBorderWidth / 2;
            var focusRect = Bounds.Inset(focusHalfBorderWidth, focusHalfBorderWidth);
            focusBorderLayer.Path = UIBezierPath.FromRoundedRect(focusRect, CornerRadius).CGPath;
            focusBorderLayer.Frame = Bounds;

            var bottomMaskInset = Math.Max(2, CornerRadius + 1);
            var bottomMaskHeight = Math.Max(1, FocusBorderWidth + 1);
            focusBorderMaskLayer.Frame = new CGRect(
                bottomMaskInset,
                Math.Max(0, Bounds.Height - bottomMaskHeight),
                Math.Max(0, Bounds.Width - (bottomMaskInset * 2)),
                bottomMaskHeight);
            focusBorderMaskLayer.BackgroundColor = UIColor.White.CGColor;
            ApplyFocusVisualState();
        }

        public override bool BecomeFirstResponder()
        {
            var result = base.BecomeFirstResponder();
            ApplyFocusVisualState();
            return result;
        }

        public override bool ResignFirstResponder()
        {
            var result = base.ResignFirstResponder();
            ApplyFocusVisualState();
            return result;
        }

        protected void ApplyFocusVisualState()
        {
            var dark = TraitCollection?.UserInterfaceStyle == UIUserInterfaceStyle.Dark;
            var borderColor = dark ? DarkBorderColor : LightBorderColor;
            var focusColor = dark ? DarkFocusUnderlineColor : LightFocusUnderlineColor;
            TintColor = dark ? UIColor.White : UIColor.Black;
            borderLayer.StrokeColor = borderColor;
            focusBorderLayer.StrokeColor = focusColor;
            focusBorderLayer.Hidden = !(IsFirstResponder || pseudoFocused);
        }

        public void SetPseudoFocus(bool enabled)
        {
            pseudoFocused = enabled;
            ApplyFocusVisualState();
        }

        protected bool TryInsertTextFromNullEventPresses(NSSet<UIPress> presses)
        {
            var inserted = false;
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
                if ((modifiers & (UIKeyModifierFlags.Command | UIKeyModifierFlags.Control | UIKeyModifierFlags.Alternate)) != 0)
                {
                    continue;
                }

                var text = key.Characters;
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                if (string.Equals(text, "\u007F", StringComparison.Ordinal) ||
                    string.Equals(text, "\b", StringComparison.Ordinal))
                {
                    DeleteBackward();
                    inserted = true;
                    continue;
                }

                if (string.Equals(text, "\r", StringComparison.Ordinal) ||
                    string.Equals(text, "\n", StringComparison.Ordinal) ||
                    string.Equals(text, "\t", StringComparison.Ordinal))
                {
                    continue;
                }

                InsertText(text);
                inserted = true;
            }

            return inserted;
        }

        private static bool TryHandleEditorShortcuts(NSSet<UIPress> presses)
        {
            if (!EditorShortcutScopeResolver.IsEditorShortcutScopeActive(App.IsConflictDialogOpen, App.IsContextMenuOpen, App.IsEditorOpen))
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

                var modifiers = key.ModifierFlags;
                var shiftDown = (modifiers & UIKeyModifierFlags.Shift) != 0;
                var commandDown = (modifiers & UIKeyModifierFlags.Command) != 0;

                if (App.IsContextMenuOpen)
                {
                    if (IsArrowPress(key, UpArrowKeyInput, "UpArrow", 82))
                    {
                        var action = EditorShortcutActionResolver.ResolveContextMenuArrowNavigationAction(downArrow: false);
                        MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                        return true;
                    }

                    if (IsArrowPress(key, DownArrowKeyInput, "DownArrow", 81))
                    {
                        var action = EditorShortcutActionResolver.ResolveContextMenuArrowNavigationAction(downArrow: true);
                        MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                        return true;
                    }
                }

                if (App.IsConflictDialogOpen)
                {
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

                if (IsKeyInput(key, TabKeyInput))
                {
                    var action = EditorShortcutActionResolver.ResolveTabNavigationAction(shiftDown);
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut(action));
                    return true;
                }

                if (IsKeyInput(key, EscapeKeyInput))
                {
                    return TryDispatchCancelShortcut();
                }

                if (App.IsEditorOpen && !App.IsConflictDialogOpen && commandDown && IsKeyInput(key, "s"))
                {
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("Save"));
                    return true;
                }

                if (IsKeyInput(key, ReturnKeyInput) ||
                    (!string.IsNullOrEmpty(EnterKeyInput) && IsKeyInput(key, EnterKeyInput!)))
                {
                    MainThread.BeginInvokeOnMainThread(() => App.RaiseEditorShortcut("PrimaryAction"));
                    return true;
                }
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

        private static UIKeyCommand CreateEntryKeyCommand(string input, UIKeyModifierFlags modifiers, string selectorName)
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
