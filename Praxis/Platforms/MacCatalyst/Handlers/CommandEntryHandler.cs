#if MACCATALYST
using System.Runtime.InteropServices;

using CoreGraphics;
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
        protected override nfloat TextInsetRight => 40;
        private bool _filteringText;
        private NSObject? windowDidBecomeKeyObserver;
        private NSObject? windowDidResignKeyObserver;
        private NSObject? didBecomeActiveObserver;
        private NSObject? willResignActiveObserver;
        private NSObject? didEnterBackgroundObserver;
        private NSTimer? _asciiEnforcementTimer;
        private bool appLifecycleSubscribed;

        public CommandEntryTextField()
        {
            KeyboardType = UIKeyboardType.ASCIICapable;
            AddTarget(OnTextEditingChanged, UIControlEvent.EditingChanged);
        }

        // [Export] is required so the ObjC runtime dispatches through our override
        [Export("setMarkedText:selectedRange:")]
        public override void SetMarkedText(string? markedText, NSRange selectedRange)
        {
            // Block non-ASCII IME composition (prevents Japanese/CJK input)
            if (AsciiInputFilter.ShouldBlockMarkedText(markedText))
            {
                EnforceAsciiInputSourceIfNeeded();
                return;
            }

            base.SetMarkedText(markedText ?? string.Empty, selectedRange);
        }

        [Export("insertText:")]
        public override void InsertText(string text)
        {
            if (text.Length == 0)
            {
                base.InsertText(text);
                return;
            }

            var filtered = AsciiInputFilter.FilterToAscii(text);
            if (filtered.Length > 0)
            {
                base.InsertText(filtered);
            }
        }

        // Safety net: strip any non-ASCII that slipped through via other code paths
        private void OnTextEditingChanged(object? sender, EventArgs e)
        {
            if (_filteringText) return;
            var current = Text ?? string.Empty;
            if (AsciiInputFilter.IsAsciiOnly(current)) return;

            var filtered = AsciiInputFilter.FilterToAscii(current);
            _filteringText = true;
            Text = filtered;
            SendActionForControlEvents(UIControlEvent.EditingChanged);
            _filteringText = false;
            var endPos = EndOfDocument;
            SelectedTextRange = GetTextRange(endPos, endPos);
        }

        public override bool BecomeFirstResponder()
        {
            var result = base.BecomeFirstResponder();
            if (result)
            {
                RefreshInputSourceEnforcementState();
            }
            return result;
        }

        public override bool ResignFirstResponder()
        {
            DetachInputSourceObserver();
            return base.ResignFirstResponder();
        }

        private static void SwitchToAsciiInputSource()
        {
            var source = TISCopyCurrentASCIICapableKeyboardInputSource();
            if (source == IntPtr.Zero) return;
            TISSelectInputSource(source);
            CFRelease(source);
        }

        private void EnforceAsciiInputSourceIfNeeded()
        {
            var currentWindow = Window;
            var shouldForce = MacCommandInputSourcePolicy.ShouldForceAsciiInputSource(
                IsFirstResponder,
                currentWindow?.IsKeyWindow == true,
                IsApplicationForegroundActive(currentWindow));

            if (!shouldForce)
            {
                return;
            }

            SwitchToAsciiInputSource();
        }

        private void RefreshInputSourceEnforcementState()
        {
            var currentWindow = Window;
            var shouldForce = MacCommandInputSourcePolicy.ShouldForceAsciiInputSource(
                IsFirstResponder,
                currentWindow?.IsKeyWindow == true,
                IsApplicationForegroundActive(currentWindow));

            if (!shouldForce)
            {
                DetachInputSourceObserver();
                return;
            }

            EnforceAsciiInputSourceIfNeeded();
            AttachInputSourceObserver();
        }

        private void AttachInputSourceObserver()
        {
            if (_asciiEnforcementTimer is not null)
            {
                return;
            }

            _asciiEnforcementTimer = NSTimer.CreateRepeatingScheduledTimer(
                MacCommandInputSourcePolicy.FocusedInputSourceEnforcementInterval.TotalSeconds,
                _ => RefreshInputSourceEnforcementState());
        }

        private void DetachInputSourceObserver()
        {
            _asciiEnforcementTimer?.Invalidate();
            _asciiEnforcementTimer?.Dispose();
            _asciiEnforcementTimer = null;
        }

        [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
        private static extern IntPtr TISCopyCurrentASCIICapableKeyboardInputSource();

        [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
        private static extern void TISSelectInputSource(IntPtr inputSource);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr cf);

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
            AttachAppLifecycleHandlers();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RemoveTarget(OnTextEditingChanged, UIControlEvent.EditingChanged);
                DetachInputSourceObserver();
                DetachWindowDidBecomeKeyObserver();
                DetachDidBecomeActiveObserver();
                DetachAppLifecycleHandlers();
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
            windowDidResignKeyObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                UIWindow.DidResignKeyNotification,
                notification => OnWindowDidResignKey(notification));
            AttachDidBecomeActiveObserver();
        }

        private void DetachWindowDidBecomeKeyObserver()
        {
            if (windowDidBecomeKeyObserver is not null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(windowDidBecomeKeyObserver);
                windowDidBecomeKeyObserver.Dispose();
                windowDidBecomeKeyObserver = null;
            }

            if (windowDidResignKeyObserver is not null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(windowDidResignKeyObserver);
                windowDidResignKeyObserver.Dispose();
                windowDidResignKeyObserver = null;
            }

            DetachInputSourceObserver();
            DetachDidBecomeActiveObserver();
        }

        private void AttachDidBecomeActiveObserver()
        {
            DetachDidBecomeActiveObserver();
            didBecomeActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.DidBecomeActiveNotification,
                _ => OnDidBecomeActive());
            willResignActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.WillResignActiveNotification,
                _ => OnWillResignActive());
            didEnterBackgroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.DidEnterBackgroundNotification,
                _ => OnDidEnterBackground());
        }

        private void DetachDidBecomeActiveObserver()
        {
            if (didBecomeActiveObserver is not null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(didBecomeActiveObserver);
                didBecomeActiveObserver.Dispose();
                didBecomeActiveObserver = null;
            }

            if (willResignActiveObserver is not null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(willResignActiveObserver);
                willResignActiveObserver.Dispose();
                willResignActiveObserver = null;
            }

            if (didEnterBackgroundObserver is not null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(didEnterBackgroundObserver);
                didEnterBackgroundObserver.Dispose();
                didEnterBackgroundObserver = null;
            }

            DetachInputSourceObserver();
        }

        private void OnWindowDidBecomeKey(NSNotification _)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TryApplyNativeActivationFocus();
                RefreshInputSourceEnforcementState();
                NSTimer.CreateScheduledTimer(0.12, _ =>
                {
                    TryApplyNativeActivationFocus();
                    RefreshInputSourceEnforcementState();
                });
            });
        }

        private void OnDidBecomeActive()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TryApplyNativeActivationFocus();
                RefreshInputSourceEnforcementState();
                NSTimer.CreateScheduledTimer(0.12, _ =>
                {
                    TryApplyNativeActivationFocus();
                    RefreshInputSourceEnforcementState();
                });
            });
        }

        private void OnWindowDidResignKey(NSNotification _)
        {
            DeactivateInputSourceEnforcement();
        }

        private void OnWillResignActive()
        {
            DeactivateInputSourceEnforcement();
        }

        private void OnDidEnterBackground()
        {
            DeactivateInputSourceEnforcement();
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
            RefreshInputSourceEnforcementState();
        }

        private bool IsApplicationForegroundActive(UIWindow? currentWindow)
        {
            if (!App.IsMacApplicationActive())
            {
                return false;
            }

            var applicationActive = UIApplication.SharedApplication.ApplicationState == UIApplicationState.Active;
            var sceneForegroundActive = currentWindow?.WindowScene?.ActivationState switch
            {
                UISceneActivationState.ForegroundActive => true,
                UISceneActivationState.ForegroundInactive => false,
                UISceneActivationState.Background => false,
                _ => applicationActive,
            };

            return applicationActive && sceneForegroundActive;
        }

        private void AttachAppLifecycleHandlers()
        {
            if (appLifecycleSubscribed)
            {
                return;
            }

            App.MacApplicationDeactivating += OnMacApplicationDeactivating;
            appLifecycleSubscribed = true;
        }

        private void DetachAppLifecycleHandlers()
        {
            if (!appLifecycleSubscribed)
            {
                return;
            }

            App.MacApplicationDeactivating -= OnMacApplicationDeactivating;
            appLifecycleSubscribed = false;
        }

        private void OnMacApplicationDeactivating()
        {
            MainThread.BeginInvokeOnMainThread(DeactivateInputSourceEnforcement);
        }

        private void DeactivateInputSourceEnforcement()
        {
            DetachInputSourceObserver();

            if (IsFirstResponder)
            {
                ResignFirstResponder();
            }
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
