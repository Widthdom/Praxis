using Praxis.Core.Logic;

namespace Praxis;

public partial class MainPage
{
#if WINDOWS
    private enum WindowsModalActionFocusTarget
    {
        Cancel,
        Save
    }

    private WindowsModalActionFocusTarget? windowsModalActionFocusTarget;

    private void CommandTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (TryHandleThemeShortcutFromKey(e))
        {
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Windows.System.VirtualKey.Up:
                if (viewModel.MoveSuggestionUpCommand.CanExecute(null))
                {
                    viewModel.MoveSuggestionUpCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Windows.System.VirtualKey.Down:
                if (viewModel.MoveSuggestionDownCommand.CanExecute(null))
                {
                    viewModel.MoveSuggestionDownCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Windows.System.VirtualKey.Escape:
                if (viewModel.CloseCommandSuggestionsCommand.CanExecute(null))
                {
                    viewModel.CloseCommandSuggestionsCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }

    private void SearchTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Tab)
        {
            windowsSelectAllOnTabNavigationPending = true;
        }

        if (TryHandleThemeShortcutFromKey(e))
        {
            e.Handled = true;
        }
    }

    private void CommandTextBox_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        windowsSelectAllOnTabNavigationPending = false;
        if (viewModel.ReopenCommandSuggestionsCommand.CanExecute(null))
        {
            viewModel.ReopenCommandSuggestionsCommand.Execute(null);
        }
    }

    private void WindowsTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Tab)
        {
            if (viewModel.IsEditorOpen &&
                TryMoveWindowsModalEditorFocusFromTextBox(sender, forward: !IsWindowsShiftDown()))
            {
                e.Handled = true;
                return;
            }

            windowsSelectAllOnTabNavigationPending = true;
        }
    }

    private void WindowsTextBox_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (!TryGetWindowsHistoryShortcutAction(e, out var action))
        {
            return;
        }

        if (sender is Microsoft.UI.Xaml.Controls.TextBox textBox &&
            ShouldPreferTextHistoryShortcut(textBox) &&
            HasWindowsTextHistoryShortcut(textBox, action))
        {
            return;
        }

        if (TryExecuteHistoryShortcut(action))
        {
            e.Handled = true;
        }
    }

    private void WindowsTextBox_GotFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ClearWindowsModalActionFocus();
        var focusedTextBox = sender as Microsoft.UI.Xaml.Controls.TextBox;
        if (focusedTextBox is not null)
        {
            ConfigureWindowsTextSelectionAndCaret(focusedTextBox);
            if (ReferenceEquals(focusedTextBox, commandTextBox) ||
                ReferenceEquals(focusedTextBox, searchTextBox))
            {
                Praxis.Controls.WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(focusedTextBox);
            }
        }

        if (!windowsSelectAllOnTabNavigationPending)
        {
            return;
        }

        windowsSelectAllOnTabNavigationPending = false;
        focusedTextBox?.SelectAll();
        if (focusedTextBox is not null)
        {
            ConfigureWindowsTextSelectionAndCaret(focusedTextBox);
            if (ReferenceEquals(focusedTextBox, commandTextBox) ||
                ReferenceEquals(focusedTextBox, searchTextBox))
            {
                Praxis.Controls.WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(focusedTextBox);
            }
        }
    }

    private void WindowsTextBox_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        windowsSelectAllOnTabNavigationPending = false;
        ClearWindowsModalActionFocus();
    }

    private void WindowsTextBox_SelectionChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not Microsoft.UI.Xaml.Controls.TextBox textBox)
        {
            return;
        }

        ConfigureWindowsTextSelectionAndCaret(textBox);
        if (ReferenceEquals(textBox, commandTextBox) ||
            ReferenceEquals(textBox, searchTextBox))
        {
            Praxis.Controls.WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(textBox);
        }
    }

    private void WindowsModalInput_LostFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ClearWindowsTextSelection(sender);
        QueueWindowsEditorFocusRestore();
    }

    private static void ClearWindowsTextSelection(object sender)
    {
        if (sender is not Microsoft.UI.Xaml.Controls.TextBox textBox ||
            textBox.SelectionLength <= 0)
        {
            return;
        }

        textBox.SelectionStart += textBox.SelectionLength;
        textBox.SelectionLength = 0;
    }

    private void QueueWindowsEditorFocusRestore()
    {
        if (windowsEditorFocusRestorePending)
        {
            return;
        }

        windowsEditorFocusRestorePending = true;
        Dispatcher.DispatchDelayed(windowsFocusRestorePrimaryDelay, () => TryRestoreWindowsEditorFocus(isFinalAttempt: false));
        Dispatcher.DispatchDelayed(windowsFocusRestoreSecondaryDelay, () => TryRestoreWindowsEditorFocus(isFinalAttempt: true));
    }

    private void QueueWindowsConflictDialogFocusRestore()
    {
        if (windowsConflictFocusRestorePending)
        {
            return;
        }

        windowsConflictFocusRestorePending = true;
        Dispatcher.DispatchDelayed(windowsFocusRestorePrimaryDelay, () => TryRestoreWindowsConflictDialogFocus(isFinalAttempt: false));
        Dispatcher.DispatchDelayed(windowsFocusRestoreSecondaryDelay, () => TryRestoreWindowsConflictDialogFocus(isFinalAttempt: true));
    }

    private void TryRestoreWindowsEditorFocus(bool isFinalAttempt)
    {
        if (!windowsEditorFocusRestorePending)
        {
            return;
        }

        var hasEditorFocus = HasWindowsEditorModalFocus();
        if (!WindowsModalFocusRestorePolicy.ShouldRestoreEditorFocus(
                isWindows: true,
                isEditorOpen: viewModel.IsEditorOpen,
                isConflictDialogOpen: IsConflictDialogOpen(),
                hasEditorFocus: hasEditorFocus))
        {
            windowsEditorFocusRestorePending = false;
            return;
        }

        FocusModalPrimaryEditorField();
        EnsureWindowsKeyHooks();
        var restored = HasWindowsEditorModalFocus();
        if (restored || isFinalAttempt)
        {
            windowsEditorFocusRestorePending = false;
        }
    }

    private void TryRestoreWindowsConflictDialogFocus(bool isFinalAttempt)
    {
        if (!windowsConflictFocusRestorePending)
        {
            return;
        }

        var hasConflictFocus = HasWindowsConflictDialogButtonFocus();
        if (!WindowsModalFocusRestorePolicy.ShouldRestoreConflictDialogFocus(
                isWindows: true,
                isConflictDialogOpen: IsConflictDialogOpen(),
                hasConflictButtonFocus: hasConflictFocus))
        {
            windowsConflictFocusRestorePending = false;
            return;
        }

        var target = conflictDialogPseudoFocusedTarget ?? ConflictDialogFocusTarget.Cancel;
        var button = target switch
        {
            ConflictDialogFocusTarget.Reload => ConflictReloadButton,
            ConflictDialogFocusTarget.Overwrite => ConflictOverwriteButton,
            _ => ConflictCancelButton,
        };

        FocusConflictDialogActionButton(button, target);
        EnsureWindowsKeyHooks();
        var restored = HasWindowsConflictDialogButtonFocus();
        if (restored || isFinalAttempt)
        {
            windowsConflictFocusRestorePending = false;
        }
    }

    private bool HasWindowsEditorModalFocus()
    {
        return IsWindowsTextBoxFocused(modalGuidTextBox) ||
            IsWindowsTextBoxFocused(modalCommandTextBox) ||
            IsWindowsTextBoxFocused(modalButtonTextTextBox) ||
            IsWindowsTextBoxFocused(modalToolTextBox) ||
            IsWindowsTextBoxFocused(modalArgumentsTextBox) ||
            IsWindowsTextBoxFocused(modalClipWordTextBox) ||
            IsWindowsTextBoxFocused(modalNoteTextBox) ||
            IsCheckBoxFocused(ModalInvertThemeCheckBox) ||
            windowsModalActionFocusTarget is not null ||
            IsButtonFocused(ModalCancelButton) ||
            IsButtonFocused(ModalSaveButton);
    }

    private bool HasWindowsConflictDialogButtonFocus()
    {
        return IsButtonFocused(ConflictReloadButton) ||
            IsButtonFocused(ConflictOverwriteButton) ||
            IsButtonFocused(ConflictCancelButton);
    }

    private static bool IsWindowsTextBoxFocused(Microsoft.UI.Xaml.Controls.TextBox? textBox)
    {
        return textBox is not null &&
            textBox.FocusState != Microsoft.UI.Xaml.FocusState.Unfocused;
    }

    private static bool IsCheckBoxFocused(Microsoft.Maui.Controls.CheckBox checkBox)
    {
        if (checkBox.IsFocused)
        {
            return true;
        }

#if WINDOWS
        if (checkBox.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Control control)
        {
            return control.FocusState != Microsoft.UI.Xaml.FocusState.Unfocused;
        }
#endif

        return false;
    }

    private void PageNativeElement_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (TryHandleThemeShortcutFromKey(e))
        {
            e.Handled = true;
            return;
        }

        var ctrlDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shiftDown = IsWindowsShiftDown();

        if (TryGetWindowsHistoryShortcutAction(e, out var historyAction) &&
            TryExecuteHistoryShortcut(historyAction))
        {
            e.Handled = true;
            return;
        }

        if (IsConflictDialogOpen())
        {
            if (e.Key == Windows.System.VirtualKey.Left)
            {
                MoveConflictDialogFocus(forward: false);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Right)
            {
                MoveConflictDialogFocus(forward: true);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                MoveConflictDialogFocus(forward: !shiftDown);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                TryInvokeConflictDialogPrimaryAction();
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                TryInvokeConflictDialogCancelAction();
                e.Handled = true;
                return;
            }
        }

        if (viewModel.IsContextMenuOpen)
        {
            if (e.Key == Windows.System.VirtualKey.Up)
            {
                MoveContextMenuFocus(forward: false);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Down)
            {
                MoveContextMenuFocus(forward: true);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                MoveContextMenuFocus(forward: !shiftDown);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                if (viewModel.CloseContextMenuCommand.CanExecute(null))
                {
                    viewModel.CloseContextMenuCommand.Execute(null);
                    e.Handled = true;
                }
                return;
            }
        }

        if (!viewModel.IsEditorOpen)
        {
            return;
        }

        if (ctrlDown && e.Key == Windows.System.VirtualKey.S)
        {
            if (viewModel.SaveEditorCommand.CanExecute(null))
            {
                viewModel.SaveEditorCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Tab &&
            TryMoveWindowsModalEditorFocus(forward: !shiftDown))
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Enter &&
            TryInvokeWindowsModalAction())
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            if (viewModel.CancelEditorCommand.CanExecute(null))
            {
                viewModel.CancelEditorCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private bool TryHandleThemeShortcutFromKey(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var ctrlDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (!ctrlDown || !shiftDown)
        {
            return false;
        }

        if (e.Key == Windows.System.VirtualKey.L)
        {
            ApplyThemeShortcut("Light");
            return true;
        }

        if (e.Key == Windows.System.VirtualKey.D)
        {
            ApplyThemeShortcut("Dark");
            return true;
        }

        if (e.Key == Windows.System.VirtualKey.H)
        {
            ApplyThemeShortcut("System");
            return true;
        }

        return false;
    }

    private static bool TryGetWindowsHistoryShortcutAction(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e, out string action)
    {
        action = string.Empty;
        var ctrlDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (!ctrlDown)
        {
            return false;
        }

        if (!shiftDown && e.Key == Windows.System.VirtualKey.Z)
        {
            action = "Undo";
            return true;
        }

        if ((!shiftDown && e.Key == Windows.System.VirtualKey.Y) ||
            (shiftDown && e.Key == Windows.System.VirtualKey.Z))
        {
            action = "Redo";
            return true;
        }

        return false;
    }

    private static bool IsWindowsShiftDown()
    {
        return Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }

    private bool TryMoveWindowsModalEditorFocusFromTextBox(object sender, bool forward)
    {
        if (sender is not Microsoft.UI.Xaml.Controls.TextBox textBox)
        {
            return false;
        }

        if (ReferenceEquals(textBox, modalNoteTextBox))
        {
            FocusWindowsModalActionButton(ModalCancelButton);
            return true;
        }

        if (!forward && ReferenceEquals(textBox, modalGuidTextBox))
        {
            FocusWindowsModalActionButton(ModalSaveButton);
            return true;
        }

        return false;
    }

    private bool TryMoveWindowsModalEditorFocus(bool forward)
    {
        if (forward)
        {
            if (windowsModalActionFocusTarget == WindowsModalActionFocusTarget.Cancel ||
                IsButtonFocused(ModalCancelButton))
            {
                FocusWindowsModalActionButton(ModalSaveButton);
                return true;
            }

            if (windowsModalActionFocusTarget == WindowsModalActionFocusTarget.Save ||
                IsButtonFocused(ModalSaveButton))
            {
                FocusWindowsModalTextBox(modalGuidTextBox, ModalGuidEntry);
                return true;
            }
        }
        else
        {
            if (windowsModalActionFocusTarget == WindowsModalActionFocusTarget.Save ||
                IsButtonFocused(ModalSaveButton))
            {
                FocusWindowsModalActionButton(ModalCancelButton);
                return true;
            }

            if (windowsModalActionFocusTarget == WindowsModalActionFocusTarget.Cancel ||
                IsButtonFocused(ModalCancelButton))
            {
                FocusWindowsModalTextBox(modalNoteTextBox, ModalNoteEditor);
                return true;
            }
        }

        return false;
    }

    private void FocusWindowsModalActionButton(VisualElement button)
    {
        windowsEditorFocusRestorePending = false;
        windowsModalActionFocusTarget = ReferenceEquals(button, ModalSaveButton)
            ? WindowsModalActionFocusTarget.Save
            : WindowsModalActionFocusTarget.Cancel;
        ApplyWindowsModalActionFocusVisuals();
        button.Focus();
        if (button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Control control)
        {
            control.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
        }
    }

    private void FocusWindowsModalTextBox(Microsoft.UI.Xaml.Controls.TextBox? textBox, VisualElement fallback)
    {
        windowsSelectAllOnTabNavigationPending = true;
        ClearWindowsModalActionFocus();
        if (textBox is not null)
        {
            textBox.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
            return;
        }

        fallback.Focus();
    }

    private bool TryInvokeWindowsModalAction()
    {
        if (windowsModalActionFocusTarget == WindowsModalActionFocusTarget.Cancel)
        {
            if (viewModel.CancelEditorCommand.CanExecute(null))
            {
                viewModel.CancelEditorCommand.Execute(null);
                return true;
            }

            return false;
        }

        if (windowsModalActionFocusTarget == WindowsModalActionFocusTarget.Save)
        {
            if (viewModel.SaveEditorCommand.CanExecute(null))
            {
                viewModel.SaveEditorCommand.Execute(null);
                return true;
            }

            return false;
        }

        return false;
    }

    private void ClearWindowsModalActionFocus()
    {
        if (windowsModalActionFocusTarget is null)
        {
            return;
        }

        windowsModalActionFocusTarget = null;
        ApplyWindowsModalActionFocusVisuals();
    }

    private void ApplyWindowsModalActionFocusVisuals()
    {
        ApplyWindowsModalActionFocusVisual(
            ModalCancelButton,
            windowsModalActionFocusTarget == WindowsModalActionFocusTarget.Cancel);
        ApplyWindowsModalActionFocusVisual(
            ModalSaveButton,
            windowsModalActionFocusTarget == WindowsModalActionFocusTarget.Save);
    }

    private static void ApplyWindowsModalActionFocusVisual(Border button, bool focused)
    {
        button.Stroke = focused
            ? new SolidColorBrush(ResolveWindowsModalActionFocusStrokeColor())
            : Brush.Transparent;
        button.StrokeThickness = focused ? 2 : 0;
    }

    private static Color ResolveWindowsModalActionFocusStrokeColor()
    {
        return IsWindowsTextDarkThemeActive()
            ? Color.FromArgb("#FFD5D9DF")
            : Color.FromArgb("#FF5F6974");
    }

    private bool ShouldPreferTextHistoryShortcut(Microsoft.UI.Xaml.Controls.TextBox textBox)
    {
        return ReferenceEquals(textBox, commandTextBox) ||
            ReferenceEquals(textBox, searchTextBox);
    }

    private static bool HasWindowsTextHistoryShortcut(Microsoft.UI.Xaml.Controls.TextBox textBox, string action)
    {
        if (string.Equals(action, "Undo", StringComparison.OrdinalIgnoreCase))
        {
            return textBox.CanUndo;
        }

        if (string.Equals(action, "Redo", StringComparison.OrdinalIgnoreCase))
        {
            return textBox.CanRedo;
        }

        return false;
    }

    private void EnsureWindowsKeyHooks()
    {
        var current = Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (current is null)
        {
            return;
        }

        if (!ReferenceEquals(pageNativeElement, current))
        {
            if (pageNativeElement is not null && pageKeyDownHandler is not null)
            {
                pageNativeElement.RemoveHandler(Microsoft.UI.Xaml.UIElement.KeyDownEvent, pageKeyDownHandler);
            }

            pageNativeElement = current;
            pageKeyDownHandler ??= new Microsoft.UI.Xaml.Input.KeyEventHandler(PageNativeElement_KeyDown);
            pageNativeElement.AddHandler(Microsoft.UI.Xaml.UIElement.KeyDownEvent, pageKeyDownHandler, true);
        }
    }

    private void EnsureWindowsTextBoxHooks()
    {
        SyncWindowsTextBoxHooks(
            ref commandTextBox,
            MainCommandEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            CommandTextBox_KeyDown,
            CommandTextBox_PointerPressed);

        SyncWindowsTextBoxHooks(
            ref searchTextBox,
            MainSearchEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            SearchTextBox_KeyDown);

        SyncWindowsTextBoxHooks(
            ref modalGuidTextBox,
            ModalGuidEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalCommandTextBox,
            ModalCommandEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalButtonTextTextBox,
            ModalButtonTextEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalToolTextBox,
            ModalToolEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalArgumentsTextBox,
            ModalArgumentsEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalClipWordTextBox,
            ModalClipWordEditor.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalNoteTextBox,
            ModalNoteEditor.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);

        ConfigureWindowsMultilineEditor(modalClipWordTextBox);
        ConfigureWindowsMultilineEditor(modalNoteTextBox);
        ConfigureWindowsTextSelectionAndCaret(commandTextBox);
        ConfigureWindowsTextSelectionAndCaret(searchTextBox);
        ConfigureWindowsTextSelectionAndCaret(modalGuidTextBox);
        ConfigureWindowsTextSelectionAndCaret(modalCommandTextBox);
        ConfigureWindowsTextSelectionAndCaret(modalButtonTextTextBox);
        ConfigureWindowsTextSelectionAndCaret(modalToolTextBox);
        ConfigureWindowsTextSelectionAndCaret(modalArgumentsTextBox);
        ConfigureWindowsTextSelectionAndCaret(modalClipWordTextBox);
        ConfigureWindowsTextSelectionAndCaret(modalNoteTextBox);
    }

    private void SyncWindowsTextBoxHooks(
        ref Microsoft.UI.Xaml.Controls.TextBox? slot,
        Microsoft.UI.Xaml.Controls.TextBox? current,
        Microsoft.UI.Xaml.Input.KeyEventHandler? extraKeyDown = null,
        Microsoft.UI.Xaml.Input.PointerEventHandler? extraPointerPressed = null,
        Microsoft.UI.Xaml.RoutedEventHandler? extraLostFocus = null)
    {
        if (ReferenceEquals(slot, current))
        {
            return;
        }

        if (slot is not null)
        {
            slot.PreviewKeyDown -= WindowsTextBox_PreviewKeyDown;
            slot.KeyDown -= WindowsTextBox_KeyDown;
            slot.GotFocus -= WindowsTextBox_GotFocus;
            slot.SelectionChanged -= WindowsTextBox_SelectionChanged;
            slot.PointerPressed -= WindowsTextBox_PointerPressed;
            if (extraKeyDown is not null)
            {
                slot.KeyDown -= extraKeyDown;
            }

            if (extraPointerPressed is not null)
            {
                slot.PointerPressed -= extraPointerPressed;
            }

            if (extraLostFocus is not null)
            {
                slot.LostFocus -= extraLostFocus;
            }
        }

        slot = current;
        if (slot is null)
        {
            return;
        }

        slot.PreviewKeyDown += WindowsTextBox_PreviewKeyDown;
        slot.KeyDown += WindowsTextBox_KeyDown;
        slot.GotFocus += WindowsTextBox_GotFocus;
        slot.SelectionChanged += WindowsTextBox_SelectionChanged;
        slot.PointerPressed += WindowsTextBox_PointerPressed;
        if (extraKeyDown is not null)
        {
            slot.KeyDown += extraKeyDown;
        }

        if (extraPointerPressed is not null)
        {
            slot.PointerPressed += extraPointerPressed;
        }

        if (extraLostFocus is not null)
        {
            slot.LostFocus += extraLostFocus;
        }
    }

    private static void ConfigureWindowsMultilineEditor(Microsoft.UI.Xaml.Controls.TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        textBox.AcceptsReturn = true;
        textBox.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap;
        Microsoft.UI.Xaml.Controls.ScrollViewer.SetVerticalScrollBarVisibility(
            textBox,
            Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto);
        Microsoft.UI.Xaml.Controls.ScrollViewer.SetVerticalScrollMode(
            textBox,
            Microsoft.UI.Xaml.Controls.ScrollMode.Auto);
    }

    private static void ConfigureWindowsTextSelectionAndCaret(Microsoft.UI.Xaml.Controls.TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        App.RefreshWindowsTextSelectionResources();
        var selectionBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveWindowsTextSelectionAccentColor());
        var caretBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveWindowsTextCaretAccentColor());
        var foregroundBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveWindowsTextForegroundColor());
        var selectionForegroundColor = ResolveWindowsTextSelectionForegroundColor();
        var selectionForegroundBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(selectionForegroundColor);
        var chromeBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
        textBox.RequestedTheme = IsWindowsTextDarkThemeActive()
            ? Microsoft.UI.Xaml.ElementTheme.Dark
            : Microsoft.UI.Xaml.ElementTheme.Light;
        textBox.Background = chromeBrush;
        textBox.BorderBrush = chromeBrush;
        textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        textBox.Foreground = foregroundBrush;
        textBox.SelectionHighlightColor = selectionBrush;
        textBox.SelectionHighlightColorWhenNotFocused = selectionBrush;
        TrySetWindowsTextControlResource(textBox, "TextControlBackground", chromeBrush);
        TrySetWindowsTextControlResource(textBox, "TextControlBackgroundPointerOver", chromeBrush);
        TrySetWindowsTextControlResource(textBox, "TextControlBackgroundFocused", chromeBrush);
        TrySetWindowsTextControlResource(textBox, "TextControlBorderBrush", chromeBrush);
        TrySetWindowsTextControlResource(textBox, "TextControlBorderBrushPointerOver", chromeBrush);
        TrySetWindowsTextControlResource(textBox, "TextControlBorderBrushFocused", chromeBrush);
        TrySetWindowsTextControlResource(textBox, "TextControlCaretBrush", caretBrush);
        TrySetWindowsTextControlResource(textBox, "TextControlCaretBrushFocused", caretBrush);
        TrySetWindowsTextControlResource(textBox, "TextControlSelectionHighlightColor", selectionBrush);
        TrySetWindowsTextControlResource(textBox, "TextControlSelectionHighlightForeground", selectionForegroundBrush);
        TrySetWindowsTextControlResource(textBox, "TextSelectionHighlightColorThemeBrush", selectionBrush);
        TrySetWindowsTextControlResource(textBox, "AccentFillColorSelectedTextBackgroundBrush", selectionBrush);
        TrySetWindowsTextControlResource(textBox, "TextOnAccentFillColorSelectedText", selectionForegroundColor);
        TrySetWindowsTextControlResource(textBox, "TextOnAccentFillColorSelectedTextBrush", selectionForegroundBrush);
        TrySetWindowsTextControlResource(textBox, "SystemColorHighlightTextColor", selectionForegroundColor);
        TrySetWindowsTextControlResource(textBox, "SystemColorHighlightTextColorBrush", selectionForegroundBrush);
    }

    private static void TrySetWindowsTextControlResource(
        Microsoft.UI.Xaml.Controls.TextBox textBox,
        string key,
        object value)
    {
        try
        {
            textBox.Resources[key] = value;
        }
        catch (Exception ex)
        {
            var safeResourceMessage = Praxis.Services.CrashFileLogger.SafeExceptionMessage(ex);
            Praxis.Services.CrashFileLogger.WriteWarning(nameof(MainPage), $"Failed to set {key}: {safeResourceMessage}");
        }
    }

    private static global::Windows.UI.Color ResolveWindowsTextSelectionAccentColor()
    {
        return IsWindowsTextDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(255, 82, 58, 124)
            : global::Windows.UI.Color.FromArgb(255, 107, 75, 176);
    }

    private static global::Windows.UI.Color ResolveWindowsTextCaretAccentColor()
    {
        return IsWindowsTextDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(255, 245, 247, 250)
            : global::Windows.UI.Color.FromArgb(255, 0, 0, 0);
    }

    private static global::Windows.UI.Color ResolveWindowsTextForegroundColor()
    {
        return IsWindowsTextDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(255, 245, 247, 250)
            : global::Windows.UI.Color.FromArgb(255, 0, 0, 0);
    }

    private static global::Windows.UI.Color ResolveWindowsTextSelectionForegroundColor()
    {
        return global::Windows.UI.Color.FromArgb(255, 245, 247, 250);
    }

    private static bool IsWindowsTextDarkThemeActive()
    {
        return Application.Current?.UserAppTheme == AppTheme.Dark ||
            Application.Current?.UserAppTheme == AppTheme.Unspecified &&
            Application.Current?.RequestedTheme == AppTheme.Dark;
    }
#endif
}
