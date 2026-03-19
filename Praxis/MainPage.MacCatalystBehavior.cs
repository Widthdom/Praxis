using System.Reflection;

using Praxis.Behaviors;
using Praxis.Core.Logic;
#if MACCATALYST
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
#endif

namespace Praxis;

public partial class MainPage
{
#if MACCATALYST
    private void MoveModalFocus(bool forward)
    {
        if (ModalFocusOrder.Length == 0)
        {
            return;
        }

        var currentIndex = GetCurrentModalFocusIndex();
        if (currentIndex < 0)
        {
            TryFocusModalPrimaryTarget();
            return;
        }

        var step = forward ? 1 : -1;
        var probe = currentIndex;
        for (var i = 0; i < ModalFocusOrder.Length; i++)
        {
            probe = (probe + step + ModalFocusOrder.Length) % ModalFocusOrder.Length;
            if (TryActivateModalFocusTarget(ModalFocusOrder[probe]))
            {
                return;
            }
        }
    }

    private static bool IsModalFocusTargetActive(VisualElement target)
    {
        if (target.IsFocused)
        {
            return true;
        }

        return target.Handler?.PlatformView is UIResponder responder && responder.IsFirstResponder;
    }

    private static void FocusModalTarget(VisualElement target)
    {
        if (target.Focus())
        {
            return;
        }

        if (target.Handler?.PlatformView is UIResponder responder && responder.CanBecomeFirstResponder)
        {
            responder.BecomeFirstResponder();
        }
    }

    private int GetCurrentModalFocusIndex()
    {
        if (macPseudoFocusedModalTarget is ModalFocusTarget pseudoTarget)
        {
            return Array.IndexOf(ModalFocusOrder, pseudoTarget);
        }

        for (var i = 0; i < ModalFocusOrder.Length; i++)
        {
            if (IsModalTargetActive(ModalFocusOrder[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsModalTargetActive(ModalFocusTarget target)
    {
        return target switch
        {
            ModalFocusTarget.Guid => IsModalFocusTargetActive(ModalGuidEntry),
            ModalFocusTarget.Command => IsModalFocusTargetActive(ModalCommandEntry),
            ModalFocusTarget.ButtonText => IsModalFocusTargetActive(ModalButtonTextEntry),
            ModalFocusTarget.Tool => IsModalFocusTargetActive(ModalToolEntry),
            ModalFocusTarget.Arguments => IsModalFocusTargetActive(ModalArgumentsEntry),
            ModalFocusTarget.ClipWord => IsModalFocusTargetActive(ModalClipWordEditor),
            ModalFocusTarget.Note => IsModalFocusTargetActive(ModalNoteEditor),
            ModalFocusTarget.InvertThemeColors => IsModalFocusTargetActive(ModalInvertThemeCheckBox),
            ModalFocusTarget.CancelButton => macPseudoFocusedModalTarget == ModalFocusTarget.CancelButton,
            ModalFocusTarget.SaveButton => macPseudoFocusedModalTarget == ModalFocusTarget.SaveButton,
            _ => false,
        };
    }

    private bool TryActivateModalFocusTarget(ModalFocusTarget target)
    {
        switch (target)
        {
            case ModalFocusTarget.Guid:
                return TryFocusModalGuidTarget();
            case ModalFocusTarget.Command:
                return TryFocusModalCommandTarget();
            case ModalFocusTarget.ButtonText:
                return TryFocusModalVisual(ModalButtonTextEntry);
            case ModalFocusTarget.Tool:
                return TryFocusModalVisual(ModalToolEntry);
            case ModalFocusTarget.Arguments:
                return TryFocusModalVisual(ModalArgumentsEntry);
            case ModalFocusTarget.ClipWord:
                return TryFocusModalVisual(ModalClipWordEditor);
            case ModalFocusTarget.Note:
                return TryFocusModalVisual(ModalNoteEditor);
            case ModalFocusTarget.InvertThemeColors:
                return TryFocusModalVisual(ModalInvertThemeCheckBox);
            case ModalFocusTarget.CancelButton:
                SetMacModalPseudoFocus(ModalFocusTarget.CancelButton);
                return true;
            case ModalFocusTarget.SaveButton:
                SetMacModalPseudoFocus(ModalFocusTarget.SaveButton);
                return true;
            default:
                return false;
        }
    }

    private bool TryFocusModalGuidTarget()
    {
        ClearMacModalPseudoFocus();
        FocusModalTarget(ModalGuidEntry);
        EnsureMacGuidEntryReadOnlyBehavior();

        if (ModalGuidEntry.Handler?.PlatformView is UITextField textField)
        {
            if (!textField.IsFirstResponder)
            {
                textField.BecomeFirstResponder();
            }

            var allTextRange = textField.GetTextRange(textField.BeginningOfDocument, textField.EndOfDocument);
            if (allTextRange is not null)
            {
                textField.SelectedTextRange = allTextRange;
            }

            return textField.IsFirstResponder;
        }

        return IsModalFocusTargetActive(ModalGuidEntry);
    }

    private bool TryFocusModalVisual(VisualElement target)
    {
        ClearMacModalPseudoFocus();
        FocusModalTarget(target);
        return IsModalFocusTargetActive(target);
    }

    private bool TryFocusModalPrimaryTarget()
    {
        return TryFocusModalVisual(ModalButtonTextEntry);
    }

    private bool TryFocusModalCommandTarget()
    {
        if (!TryFocusModalVisual(ModalCommandEntry))
        {
            return false;
        }

        PlaceMacEntryCaretAtEnd(ModalCommandEntry);
        return IsModalFocusTargetActive(ModalCommandEntry);
    }

    private static void PlaceMacEntryCaretAtEnd(Entry entry)
    {
        if (entry.Handler?.PlatformView is not UITextField textField)
        {
            return;
        }

        if (!textField.IsFirstResponder && textField.CanBecomeFirstResponder)
        {
            textField.BecomeFirstResponder();
        }

        var caretOffset = TextCaretPositionResolver.ResolveTailOffset(textField.Text);
        var caretPosition = textField.GetPosition(textField.BeginningOfDocument, caretOffset) ?? textField.EndOfDocument;
        if (caretPosition is null)
        {
            return;
        }

        var caretRange = textField.GetTextRange(caretPosition, caretPosition);
        if (caretRange is not null)
        {
            textField.SelectedTextRange = caretRange;
        }
    }

    private static void SelectAllMacEntryText(Entry entry)
    {
        if (entry.Handler?.PlatformView is not UITextField textField)
        {
            return;
        }

        if (!textField.IsFirstResponder && textField.CanBecomeFirstResponder)
        {
            textField.BecomeFirstResponder();
        }

        var allTextRange = textField.GetTextRange(textField.BeginningOfDocument, textField.EndOfDocument);
        if (allTextRange is not null)
        {
            textField.SelectedTextRange = allTextRange;
        }
    }

    private void ForceMacCommandFirstResponder()
    {
        if (MainCommandEntry.Handler?.PlatformView is not UITextField commandField)
        {
            return;
        }

        var commandWindow = commandField.Window ?? Window?.Handler?.PlatformView as UIWindow;
        if (commandWindow is not null && !commandWindow.IsKeyWindow)
        {
            commandWindow.MakeKeyAndVisible();
        }

        var firstResponder = GetCurrentMacFirstResponder();
        var commandAlreadyFirst = ReferenceEquals(firstResponder, commandField) || commandField.IsFirstResponder;
        if (commandAlreadyFirst)
        {
            SelectAllMacTextField(commandField);
            return;
        }

        MainCommandEntry.Focus();

        firstResponder = GetCurrentMacFirstResponder();
        if (firstResponder is not null &&
            !ReferenceEquals(firstResponder, commandField) &&
            firstResponder.CanResignFirstResponder)
        {
            firstResponder.ResignFirstResponder();
        }

        if (commandField.CanBecomeFirstResponder)
        {
            commandField.BecomeFirstResponder();
        }

        SelectAllMacTextField(commandField);
    }

    private static void SelectAllMacTextField(UITextField textField)
    {
        var allTextRange = textField.GetTextRange(textField.BeginningOfDocument, textField.EndOfDocument);
        if (allTextRange is not null)
        {
            textField.SelectedTextRange = allTextRange;
        }

        var selectAllSelector = new Selector("selectAll:");
        if (textField.RespondsToSelector(selectAllSelector))
        {
            textField.PerformSelector(selectAllSelector, null, 0);
        }
    }

    private static int GetSelectedLength(UITextField textField)
    {
        var selectedRange = textField.SelectedTextRange;
        if (selectedRange is null)
        {
            return 0;
        }

        return (int)textField.GetOffsetFromPosition(selectedRange.Start, selectedRange.End);
    }

    private bool IsMainCommandSelectAllSatisfied(out int textLength, out int selectedLength)
    {
        if (MainCommandEntry.Handler?.PlatformView is UITextField commandField)
        {
            textLength = commandField.Text?.Length ?? 0;
            selectedLength = GetSelectedLength(commandField);
            return textLength <= 0 || selectedLength >= textLength;
        }

        textLength = MainCommandEntry.Text?.Length ?? 0;
        selectedLength = MainCommandEntry.SelectionLength;
        return textLength <= 0 || selectedLength >= textLength;
    }

    private static UIResponder? GetCurrentMacFirstResponder()
    {
        var keyWindow = GetMacKeyWindow();
        if (keyWindow is not null)
        {
            var keyWindowFirstResponder = FindFirstResponderInWindow(keyWindow);
            if (keyWindowFirstResponder is not null)
            {
                return keyWindowFirstResponder;
            }
        }

        foreach (var window in EnumerateMacWindows())
        {
            var responder = FindFirstResponderInWindow(window);
            if (responder is not null)
            {
                return responder;
            }
        }

        return null;
    }

    private static UIWindow? GetMacKeyWindow()
    {
        foreach (var window in EnumerateMacWindows())
        {
            if (window.IsKeyWindow)
            {
                return window;
            }
        }

        return null;
    }

    private static IEnumerable<UIWindow> EnumerateMacWindows()
    {
        var app = UIApplication.SharedApplication;
        if (app is null)
        {
            yield break;
        }

        var seenWindows = new HashSet<nint>();
        if (app.ConnectedScenes is not null)
        {
            foreach (var scene in app.ConnectedScenes)
            {
                if (scene is not UIWindowScene windowScene)
                {
                    continue;
                }

                foreach (var window in windowScene.Windows)
                {
                    if (window is null || !seenWindows.Add(window.Handle))
                    {
                        continue;
                    }

                    yield return window;
                }
            }
        }

    }

    private static UIResponder? FindFirstResponderInWindow(UIWindow window)
    {
        if (window.IsFirstResponder)
        {
            return window;
        }

        foreach (var subview in window.Subviews)
        {
            if (subview is null)
            {
                continue;
            }

            var responder = FindFirstResponderInView(subview);
            if (responder is not null)
            {
                return responder;
            }
        }

        return null;
    }

    private static UIResponder? FindFirstResponderInView(UIView view)
    {
        if (view.IsFirstResponder)
        {
            return view;
        }

        foreach (var subview in view.Subviews)
        {
            if (subview is null)
            {
                continue;
            }

            var responder = FindFirstResponderInView(subview);
            if (responder is not null)
            {
                return responder;
            }
        }

        return null;
    }

    private void SetMacModalPseudoFocus(ModalFocusTarget target)
    {
        ResignModalInputFirstResponder();
        macPseudoFocusedModalTarget = target;
        ApplyMacModalPseudoFocusVisuals();
        ApplyMacEditorKeyCommands();
    }

    private void ClearMacModalPseudoFocus()
    {
        if (macPseudoFocusedModalTarget is null)
        {
            return;
        }

        macPseudoFocusedModalTarget = null;
        ApplyMacModalPseudoFocusVisuals();
        ApplyMacEditorKeyCommands();
    }

    private void ApplyMacModalPseudoFocusVisuals()
    {
        var dark = IsDarkThemeActive();
        var focusedBorderColor = dark ? Color.FromArgb("#F2F2F2") : Color.FromArgb("#1A1A1A");

        ApplyMacPseudoFocusVisual(ModalCancelButton, macPseudoFocusedModalTarget == ModalFocusTarget.CancelButton, focusedBorderColor);
        ApplyMacPseudoFocusVisual(ModalSaveButton, macPseudoFocusedModalTarget == ModalFocusTarget.SaveButton, focusedBorderColor);
    }

    private static void ApplyMacPseudoFocusVisual(Button button, bool focused, Color focusedBorderColor)
    {
        if (focused)
        {
            button.BorderColor = focusedBorderColor;
            button.BorderWidth = 1.5;
            return;
        }

        button.BorderColor = Colors.Transparent;
        button.BorderWidth = 0;
    }

    private void RebuildCommandSuggestionStack()
    {
        if (!xamlLoaded)
        {
            return;
        }

        CommandSuggestionStack.Children.Clear();
        foreach (var item in viewModel.CommandSuggestions)
        {
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(4, GridUnitType.Star)),
                },
                ColumnSpacing = 10,
                Padding = new Thickness(8, 6),
                MinimumHeightRequest = 34,
                HorizontalOptions = LayoutOptions.Fill,
            };
            ApplyCommandSuggestionRowThemeColors(row, item.IsSelected);

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                if (viewModel.PickSuggestionCommand.CanExecute(item))
                {
                    viewModel.PickSuggestionCommand.Execute(item);
                }
            };
            row.GestureRecognizers.Add(tap);

            var pointer = new PointerGestureRecognizer();
            pointer.PointerPressed += (_, e) =>
            {
                if (!App.IsMacApplicationActive() || App.IsActivationSuppressionActive())
                {
                    return;
                }

                if (IsOtherMouseFromPlatformArgs(e.PlatformArgs) || IsMiddlePointerPressed(e))
                {
                    CloseCommandSuggestionPopup();
                    if (viewModel.OpenEditorCommand.CanExecute(item.Source))
                    {
                        viewModel.OpenEditorCommand.Execute(item.Source);
                    }

                    return;
                }

                if (IsSecondaryPointerPressed(e))
                {
                    CloseCommandSuggestionPopup();
                    if (viewModel.OpenContextMenuCommand.CanExecute(item.Source))
                    {
                        viewModel.OpenContextMenuCommand.Execute(item.Source);
                    }
                }
            };
            row.GestureRecognizers.Add(pointer);

            var secondaryTap = new TapGestureRecognizer { Buttons = ButtonsMask.Secondary };
            secondaryTap.Tapped += (_, _) =>
            {
                CloseCommandSuggestionPopup();
                if (viewModel.OpenContextMenuCommand.CanExecute(item.Source))
                {
                    viewModel.OpenContextMenuCommand.Execute(item.Source);
                }
            };
            row.GestureRecognizers.Add(secondaryTap);

            var commandLabel = new Label
            {
                Text = item.Command,
                LineBreakMode = LineBreakMode.TailTruncation,
            };
            ApplyCommandSuggestionLabelThemeColors(commandLabel);
            row.Children.Add(commandLabel);

            var buttonTextLabel = new Label
            {
                Text = item.ButtonText,
                LineBreakMode = LineBreakMode.TailTruncation,
            };
            ApplyCommandSuggestionLabelThemeColors(buttonTextLabel);
            row.SetColumn(buttonTextLabel, 1);
            row.Children.Add(buttonTextLabel);

            var toolArgsLabel = new Label
            {
                Text = item.ToolArguments,
                LineBreakMode = LineBreakMode.TailTruncation,
            };
            ApplyCommandSuggestionLabelThemeColors(toolArgsLabel);
            row.SetColumn(toolArgsLabel, 2);
            row.Children.Add(toolArgsLabel);

            CommandSuggestionStack.Children.Add(row);
        }
    }

    private void ApplyMacVisualTuning()
    {
        EnsureMacFirstResponder();
        ApplyMacContentScale();
        ApplyMacEntryVisualState();
        ApplyMacClipWordEditorVisualState();
        ApplyMacNoteEditorVisualState();
        ApplyMacModalPseudoFocusVisuals();
        ApplyMacCommandSuggestionKeyCommands();
        ApplyMacEditorKeyCommands();
    }

    private void ApplyMacEntryVisualState()
    {
        RefreshMacEntryVisualState(MainCommandEntry);
        RefreshMacEntryVisualState(MainSearchEntry);
        RefreshMacEntryVisualState(ModalGuidEntry);
        RefreshMacEntryVisualState(ModalCommandEntry);
        RefreshMacEntryVisualState(ModalButtonTextEntry);
        RefreshMacEntryVisualState(ModalToolEntry);
        RefreshMacEntryVisualState(ModalArgumentsEntry);
    }

    private static void RefreshMacEntryVisualState(Entry entry)
    {
        if (entry.Handler?.PlatformView is not UITextField textField)
        {
            return;
        }

        textField.SetNeedsLayout();
        textField.LayoutIfNeeded();
    }

    private void ApplyMacClipWordEditorVisualState()
    {
        if (ModalClipWordEditor.Handler?.PlatformView is not UITextView textView)
        {
            return;
        }

        var dark = IsDarkThemeActive();
        textView.TintColor = dark ? UIColor.White : UIColor.Black;
    }

    private void ApplyMacInitialCommandFocus()
    {
        if (macInitialCommandFocusApplied || !xamlLoaded || viewModel.IsEditorOpen || !IsMacAppForegroundActive())
        {
            return;
        }

        macInitialCommandFocusApplied = true;
        Dispatcher.Dispatch(() => MainCommandEntry.Focus());
        Dispatcher.DispatchDelayed(UiTimingPolicy.MacInitialCommandFocusRetryDelay, () =>
        {
            if (!IsMacAppForegroundActive())
            {
                return;
            }

            if (!MainCommandEntry.IsFocused)
            {
                MainCommandEntry.Focus();
            }
        });
    }

    private void ApplyMacNoteEditorVisualState()
    {
        if (ModalNoteEditor.Handler?.PlatformView is not UITextView textView)
        {
            return;
        }

        var dark = IsDarkThemeActive();
        textView.TintColor = dark ? UIColor.White : UIColor.Black;
    }

    private void TryHandleMacEditorTabTextInsertion(object? sender, TextChangedEventArgs e)
    {
        if (macSuppressEditorTabFallback || !viewModel.IsEditorOpen || sender is not Editor editor || !editor.IsFocused)
        {
            return;
        }

        if (!EditorTabInsertionResolver.TryResolveNavigationAction(e.OldTextValue, e.NewTextValue, out var action, out var insertedIndex))
        {
            return;
        }

        var newText = e.NewTextValue ?? string.Empty;
        var sanitizedText = newText.Remove(insertedIndex, 1);
        try
        {
            macSuppressEditorTabFallback = true;
            editor.Text = sanitizedText;
        }
        finally
        {
            macSuppressEditorTabFallback = false;
        }

        MoveModalFocus(forward: string.Equals(action, "TabNext", StringComparison.Ordinal));
    }

    private bool IsMainCommandEntryReadyForSuggestionNavigation()
    {
        if (!xamlLoaded || viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
        {
            return false;
        }

        if (viewModel.IsCommandSuggestionOpen)
        {
            return true;
        }

        if (MainCommandEntry.IsFocused)
        {
            return true;
        }

        return MainCommandEntry.Handler?.PlatformView is UIResponder responder && responder.IsFirstResponder;
    }

    private void ApplyMacEditorKeyCommands()
    {
        if (!macDynamicKeyCommandRegistrationEnabled)
        {
            return;
        }

        modalEscapeKeyCommand ??= CreateMacEscapeKeyCommand();
        modalSaveKeyCommand ??= UIKeyCommand.Create(new NSString("s"), UIKeyModifierFlags.Command, new Selector("handleEditorSave:"));
        modalTabNextKeyCommand ??= TryCreateMacEditorKeyCommand(macTabKeyInput, 0, "handleEditorTabNext:");
        modalTabPreviousKeyCommand ??= TryCreateMacEditorKeyCommand(macTabKeyInput, UIKeyModifierFlags.Shift, "handleEditorTabPrevious:");
        modalPrimaryActionKeyCommand ??= TryCreateMacEditorKeyCommand(macReturnKeyInput, 0, "handleEditorPrimaryAction:");
        if (!string.IsNullOrEmpty(macEnterKeyInput))
        {
            modalPrimaryActionAlternateKeyCommand ??= TryCreateMacEditorKeyCommand(macEnterKeyInput!, 0, "handleEditorPrimaryAction:");
        }
        EnsureMacGuidEntryReadOnlyBehavior();
        var includePrimaryAction =
            macPseudoFocusedModalTarget is ModalFocusTarget.CancelButton or ModalFocusTarget.SaveButton &&
            modalPrimaryActionKeyCommand is not null;

        var responders = new UIResponder?[]
        {
            ModalCommandEntry.Handler?.PlatformView as UIResponder,
            ModalGuidEntry.Handler?.PlatformView as UIResponder,
            ModalButtonTextEntry.Handler?.PlatformView as UIResponder,
            ModalToolEntry.Handler?.PlatformView as UIResponder,
            ModalArgumentsEntry.Handler?.PlatformView as UIResponder,
            ModalClipWordEditor.Handler?.PlatformView as UIResponder,
            ModalNoteEditor.Handler?.PlatformView as UIResponder,
            ModalInvertThemeCheckBox.Handler?.PlatformView as UIResponder,
            ModalCancelButton.Handler?.PlatformView as UIResponder,
            ModalSaveButton.Handler?.PlatformView as UIResponder,
            UIApplication.SharedApplication.Delegate as UIResponder,
        };

        foreach (var responder in responders)
        {
            if (viewModel.IsEditorOpen)
            {
                RegisterMacEditorCommands(responder, includePrimaryAction);
            }
            else
            {
                UnregisterMacEditorCommands(responder);
            }
        }

        if (Window?.Handler?.PlatformView is UIWindow nativeWindow)
        {
            if (viewModel.IsEditorOpen)
            {
                RegisterMacEditorCommands(nativeWindow, includePrimaryAction);
                RegisterMacEditorCommands(nativeWindow.RootViewController, includePrimaryAction);
            }
            else
            {
                UnregisterMacEditorCommands(nativeWindow);
                UnregisterMacEditorCommands(nativeWindow.RootViewController);
            }
        }
    }

    private void ApplyMacCommandSuggestionKeyCommands()
    {
        if (!macDynamicKeyCommandRegistrationEnabled)
        {
            return;
        }

        commandSuggestionUpKeyCommand ??= CreateMacCommandSuggestionKeyCommand(macUpArrowKeyInput, "handleCommandSuggestionUp:");
        commandSuggestionDownKeyCommand ??= CreateMacCommandSuggestionKeyCommand(macDownArrowKeyInput, "handleCommandSuggestionDown:");

        RegisterMacCommandSuggestionCommands(MainCommandEntry.Handler?.PlatformView as UIResponder);
        if (Window?.Handler?.PlatformView is UIWindow nativeWindow)
        {
            RegisterMacCommandSuggestionCommands(nativeWindow);
            RegisterMacCommandSuggestionCommands(nativeWindow.RootViewController);
        }
    }

    private void RegisterMacCommandSuggestionCommands(UIResponder? responder)
    {
        if (responder is null || commandSuggestionUpKeyCommand is null || commandSuggestionDownKeyCommand is null)
        {
            return;
        }

        InvokeResponderSelector(responder, "removeKeyCommand:", commandSuggestionUpKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", commandSuggestionDownKeyCommand);
        InvokeResponderSelector(responder, "addKeyCommand:", commandSuggestionUpKeyCommand);
        InvokeResponderSelector(responder, "addKeyCommand:", commandSuggestionDownKeyCommand);
    }

    private void RegisterMacEditorCommands(UIResponder? responder, bool includePrimaryAction)
    {
        if (responder is null ||
            modalEscapeKeyCommand is null ||
            modalSaveKeyCommand is null ||
            modalTabNextKeyCommand is null ||
            modalTabPreviousKeyCommand is null ||
            modalPrimaryActionKeyCommand is null)
        {
            return;
        }

        InvokeResponderSelector(responder, "removeKeyCommand:", modalEscapeKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalSaveKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalTabNextKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalTabPreviousKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalPrimaryActionKeyCommand);
        if (modalPrimaryActionAlternateKeyCommand is not null)
        {
            InvokeResponderSelector(responder, "removeKeyCommand:", modalPrimaryActionAlternateKeyCommand);
        }
        InvokeResponderSelector(responder, "addKeyCommand:", modalEscapeKeyCommand);
        InvokeResponderSelector(responder, "addKeyCommand:", modalSaveKeyCommand);
        InvokeResponderSelector(responder, "addKeyCommand:", modalTabNextKeyCommand);
        InvokeResponderSelector(responder, "addKeyCommand:", modalTabPreviousKeyCommand);
        if (includePrimaryAction)
        {
            InvokeResponderSelector(responder, "addKeyCommand:", modalPrimaryActionKeyCommand);
            if (modalPrimaryActionAlternateKeyCommand is not null)
            {
                InvokeResponderSelector(responder, "addKeyCommand:", modalPrimaryActionAlternateKeyCommand);
            }
        }
    }

    private void UnregisterMacEditorCommands(UIResponder? responder)
    {
        if (responder is null ||
            modalEscapeKeyCommand is null ||
            modalSaveKeyCommand is null ||
            modalTabNextKeyCommand is null ||
            modalTabPreviousKeyCommand is null ||
            modalPrimaryActionKeyCommand is null)
        {
            return;
        }

        InvokeResponderSelector(responder, "removeKeyCommand:", modalEscapeKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalSaveKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalTabNextKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalTabPreviousKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalPrimaryActionKeyCommand);
        if (modalPrimaryActionAlternateKeyCommand is not null)
        {
            InvokeResponderSelector(responder, "removeKeyCommand:", modalPrimaryActionAlternateKeyCommand);
        }
    }

    private static void InvokeResponderSelector(UIResponder responder, string selectorName, NSObject argument)
    {
        var selector = new Selector(selectorName);
        if (!responder.RespondsToSelector(selector))
        {
            return;
        }

        responder.PerformSelector(selector, argument, 0);
    }

    private static UIKeyCommand CreateMacEscapeKeyCommand()
    {
        var command = UIKeyCommand.Create(new NSString(macEscapeKeyInput), 0, new Selector("handleEditorCancel:"));
        TrySetKeyCommandPriorityOverSystem(command);
        return command;
    }

    private static UIKeyCommand CreateMacEditorKeyCommand(string keyInput, UIKeyModifierFlags modifiers, string selectorName)
    {
        var command = UIKeyCommand.Create(new NSString(keyInput), modifiers, new Selector(selectorName));
        TrySetKeyCommandPriorityOverSystem(command);
        return command;
    }

    private static UIKeyCommand? TryCreateMacEditorKeyCommand(string keyInput, UIKeyModifierFlags modifiers, string selectorName)
    {
        if (string.IsNullOrEmpty(keyInput))
        {
            return null;
        }

        try
        {
            return CreateMacEditorKeyCommand(keyInput, modifiers, selectorName);
        }
        catch
        {
            return null;
        }
    }

    private static UIKeyCommand CreateMacCommandSuggestionKeyCommand(string keyInput, string selectorName)
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

    private static string ResolveMacKeyInput(string inputName, string fallback)
    {
        return TryResolveMacKeyInput(inputName) ?? fallback;
    }

    private static string? TryResolveMacKeyInput(string inputName)
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

        return null;
    }

    private void EnsureMacFirstResponder()
    {
        if (!IsMacAppForegroundActive())
        {
            return;
        }

        if (Window?.Handler?.PlatformView is UIWindow nativeWindow && !nativeWindow.IsKeyWindow)
        {
            nativeWindow.MakeKeyAndVisible();
        }
    }

    private static bool IsMacAppForegroundActive()
    {
        var app = UIApplication.SharedApplication;
        if (app.ApplicationState == UIApplicationState.Active)
        {
            return true;
        }

        if (app.ApplicationState == UIApplicationState.Background)
        {
            return false;
        }

        foreach (var scene in app.ConnectedScenes)
        {
            if (scene is UIScene uiScene && uiScene.ActivationState == UISceneActivationState.ForegroundActive)
            {
                return true;
            }
        }

        if (app.ApplicationState == UIApplicationState.Inactive)
        {
            foreach (var scene in app.ConnectedScenes)
            {
                if (scene is UIScene uiScene && uiScene.ActivationState == UISceneActivationState.ForegroundInactive)
                {
                    return true;
                }
            }
        }

        return app.ConnectedScenes is null || app.ConnectedScenes.Count == 0;
    }

    private void ApplyMacContentScale()
    {
        if (Handler?.PlatformView is not UIView rootView)
        {
            return;
        }

        var scale = UIScreen.MainScreen.Scale;
        ApplyMacContentScaleRecursive(rootView, scale);
    }

    private static void ApplyMacContentScaleRecursive(UIView view, nfloat scale)
    {
        view.ContentScaleFactor = scale;
        view.Layer.ContentsScale = scale;
        foreach (var subview in view.Subviews)
        {
            ApplyMacContentScaleRecursive(subview, scale);
        }
    }

    private void StartMacMiddleButtonPolling()
    {
        if (macMiddleButtonPollTimer is not null)
        {
            return;
        }

        var dispatcher = Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        macMiddleButtonPollTimer = dispatcher.CreateTimer();
        macMiddleButtonPollTimer.Interval = UiTimingPolicy.MacMiddleButtonPollingInterval;
        macMiddleButtonPollTimer.IsRepeating = true;
        macMiddleButtonPollTimer.Tick += OnMacMiddleButtonPollTick;
        macMiddleButtonWasDown = IsMacMiddleButtonCurrentlyDown();
        macMiddleButtonPollTimer.Start();
    }

    private void StopMacMiddleButtonPolling()
    {
        if (macMiddleButtonPollTimer is null)
        {
            return;
        }

        macMiddleButtonPollTimer.Tick -= OnMacMiddleButtonPollTick;
        macMiddleButtonPollTimer.Stop();
        macMiddleButtonPollTimer = null;
    }

    private void OnMacMiddleButtonPollTick(object? sender, EventArgs e)
    {
        var isDown = IsMacMiddleButtonCurrentlyDown();
        if (isDown && !macMiddleButtonWasDown)
        {
            if (lastPointerOnRoot is Point pointer)
            {
                HandleMacMiddleClick(pointer);
            }
        }

        macMiddleButtonWasDown = isDown;
    }

    private static bool IsMacMiddleButtonCurrentlyDown()
    {
        try
        {
            return CGEventSource.GetButtonState(CGEventSourceStateID.HidSystem, CGMouseButton.Center);
        }
        catch
        {
            try
            {
                return CGEventSource.GetButtonState(CGEventSourceStateID.CombinedSession, CGMouseButton.Center);
            }
            catch
            {
                return false;
            }
        }
    }

    private void EnsureMacGuidEntryReadOnlyBehavior()
    {
        if (ModalGuidEntry.Handler?.PlatformView is not UITextField textField)
        {
            return;
        }

        if (!ReferenceEquals(macGuidNativeTextField, textField))
        {
            DetachMacGuidEntryReadOnlyBehavior();
            macGuidNativeTextField = textField;
        }

        textField.Delegate = macGuidReadOnlyDelegate;
        textField.EditingChanged -= OnMacGuidEntryEditingChanged;
        textField.EditingChanged += OnMacGuidEntryEditingChanged;
        textField.UserInteractionEnabled = true;
        textField.Enabled = true;

        if (string.IsNullOrEmpty(macGuidLockedText))
        {
            macGuidLockedText = ModalGuidEntry.Text ?? string.Empty;
        }

        if (!string.Equals(textField.Text, macGuidLockedText, StringComparison.Ordinal))
        {
            textField.Text = macGuidLockedText;
        }
    }

    private void DetachMacGuidEntryReadOnlyBehavior()
    {
        if (macGuidNativeTextField is null)
        {
            return;
        }

        macGuidNativeTextField.EditingChanged -= OnMacGuidEntryEditingChanged;
        macGuidNativeTextField = null;
    }

    private void OnMacGuidEntryEditingChanged(object? sender, EventArgs e)
    {
        if (macApplyingGuidTextLock || sender is not UITextField textField)
        {
            return;
        }

        if (string.IsNullOrEmpty(macGuidLockedText))
        {
            macGuidLockedText = viewModel.Editor.GuidText ?? string.Empty;
        }

        if (string.Equals(textField.Text, macGuidLockedText, StringComparison.Ordinal))
        {
            return;
        }

        macApplyingGuidTextLock = true;
        try
        {
            textField.Text = macGuidLockedText;
            if (!string.Equals(ModalGuidEntry.Text, macGuidLockedText, StringComparison.Ordinal))
            {
                ModalGuidEntry.Text = macGuidLockedText;
            }

            if (!string.Equals(viewModel.Editor.GuidText, macGuidLockedText, StringComparison.Ordinal))
            {
                viewModel.Editor.GuidText = macGuidLockedText;
            }

            var allTextRange = textField.GetTextRange(textField.BeginningOfDocument, textField.EndOfDocument);
            if (allTextRange is not null)
            {
                textField.SelectedTextRange = allTextRange;
            }
        }
        finally
        {
            macApplyingGuidTextLock = false;
        }
    }

    private void ResignModalInputFirstResponder()
    {
        ModalGuidEntry.Unfocus();
        ModalCommandEntry.Unfocus();
        ModalButtonTextEntry.Unfocus();
        ModalToolEntry.Unfocus();
        ModalArgumentsEntry.Unfocus();
        ModalClipWordEditor.Unfocus();
        ModalNoteEditor.Unfocus();
        ModalInvertThemeCheckBox.Unfocus();

        if (ModalGuidEntry.Handler?.PlatformView is UITextField guidField && guidField.IsFirstResponder)
        {
            guidField.ResignFirstResponder();
        }

        if (ModalCommandEntry.Handler?.PlatformView is UITextField commandField && commandField.IsFirstResponder)
        {
            commandField.ResignFirstResponder();
        }

        if (ModalButtonTextEntry.Handler?.PlatformView is UITextField buttonTextField && buttonTextField.IsFirstResponder)
        {
            buttonTextField.ResignFirstResponder();
        }

        if (ModalToolEntry.Handler?.PlatformView is UITextField toolField && toolField.IsFirstResponder)
        {
            toolField.ResignFirstResponder();
        }

        if (ModalArgumentsEntry.Handler?.PlatformView is UITextField argumentsField && argumentsField.IsFirstResponder)
        {
            argumentsField.ResignFirstResponder();
        }

        if (ModalClipWordEditor.Handler?.PlatformView is UITextView clipWordField && clipWordField.IsFirstResponder)
        {
            clipWordField.ResignFirstResponder();
        }

        if (ModalNoteEditor.Handler?.PlatformView is UITextView noteField && noteField.IsFirstResponder)
        {
            noteField.ResignFirstResponder();
        }

        if (ModalInvertThemeCheckBox.Handler?.PlatformView is UIResponder invertThemeCheckBox &&
            invertThemeCheckBox.IsFirstResponder)
        {
            invertThemeCheckBox.ResignFirstResponder();
        }

        ModalClipWordFocusUnderline.IsVisible = false;
        ModalNoteFocusUnderline.IsVisible = false;
    }

    private void ResignMainInputFirstResponder()
    {
        MainCommandEntry.Unfocus();
        MainSearchEntry.Unfocus();

        if (MainCommandEntry.Handler?.PlatformView is UITextField commandField && commandField.IsFirstResponder)
        {
            commandField.ResignFirstResponder();
        }

        if (MainSearchEntry.Handler?.PlatformView is UITextField searchField && searchField.IsFirstResponder)
        {
            searchField.ResignFirstResponder();
        }
    }

    private sealed class MacGuidReadOnlyTextFieldDelegate : UITextFieldDelegate
    {
        public override bool ShouldBeginEditing(UITextField textField)
            => true;

        public override bool ShouldClear(UITextField textField)
            => false;

        public override bool ShouldChangeCharacters(UITextField textField, NSRange range, string replacementString)
            => false;
    }
#endif
}
