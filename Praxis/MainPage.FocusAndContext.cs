using System.Reflection;
using Praxis.Core.Logic;
using Praxis.Services;
#if MACCATALYST
using UIKit;
#endif

namespace Praxis;

public partial class MainPage
{
    private void ContextActionButton_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        if (sender is Button focusedButton)
        {
            SyncMacContextMenuPseudoFocusFromButton(focusedButton);
        }
#endif
        ApplyContextActionButtonFocusVisuals();
    }

    private void ContextActionButton_HandlerChanged(object? sender, EventArgs e)
    {
        ApplyContextActionButtonPlatformFocusSettings();
        ApplyContextActionButtonFocusVisuals();
    }

    private void ContextActionButton_Unfocused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        if (sender is Button unfocusedButton)
        {
            var target = GetContextMenuTarget(unfocusedButton);
            if (target is not null && macPseudoFocusedContextMenuTarget == target)
            {
                ClearMacContextMenuPseudoFocus();
            }
        }
#endif
        ApplyContextActionButtonFocusVisuals();
    }

    private void FocusContextActionButton(Button button)
    {
#if MACCATALYST
        ResignMainInputFirstResponder();
#endif
        button.Focus();
#if MACCATALYST
        if (button.Handler?.PlatformView is UIResponder responder &&
            responder.CanBecomeFirstResponder &&
            !responder.IsFirstResponder)
        {
            responder.BecomeFirstResponder();
        }
        SyncMacContextMenuPseudoFocusFromButton(button);
#endif
    }

    private void MoveContextMenuFocus(bool forward)
    {
        if (!viewModel.IsContextMenuOpen)
        {
            return;
        }

        var order = new[] { ContextEditButton, ContextDeleteButton };
        var currentIndex = GetCurrentContextMenuFocusIndex(order);
        if (currentIndex < 0)
        {
            FocusContextActionButton(order[0]);
            ApplyContextActionButtonFocusVisuals();
            return;
        }

        var nextIndex = FocusRingNavigator.GetNextIndex(currentIndex, order.Length, forward);
        if (nextIndex < 0)
        {
            return;
        }

        FocusContextActionButton(order[nextIndex]);
        ApplyContextActionButtonFocusVisuals();
    }

    private int GetCurrentContextMenuFocusIndex(Button[] buttons)
    {
#if MACCATALYST
        if (macPseudoFocusedContextMenuTarget is ContextMenuFocusTarget pseudoTarget)
        {
            return pseudoTarget == ContextMenuFocusTarget.Edit ? 0 : 1;
        }
#endif

        for (var i = 0; i < buttons.Length; i++)
        {
            if (IsButtonFocused(buttons[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private void ApplyContextActionButtonFocusVisuals()
    {
        if (!viewModel.IsContextMenuOpen)
        {
            ApplyButtonFocusVisual(ContextEditButton, false);
            ApplyButtonFocusVisual(ContextDeleteButton, false);
            return;
        }

#if MACCATALYST
        if (macPseudoFocusedContextMenuTarget is ContextMenuFocusTarget pseudoTarget)
        {
            ApplyButtonFocusVisual(ContextEditButton, pseudoTarget == ContextMenuFocusTarget.Edit);
            ApplyButtonFocusVisual(ContextDeleteButton, pseudoTarget == ContextMenuFocusTarget.Delete);
            return;
        }
#endif

        ApplyButtonFocusVisual(ContextEditButton, IsButtonFocused(ContextEditButton));
        ApplyButtonFocusVisual(ContextDeleteButton, IsButtonFocused(ContextDeleteButton));
    }

#if MACCATALYST
    private void SyncMacContextMenuPseudoFocusFromButton(Button button)
    {
        var target = GetContextMenuTarget(button);
        if (target is null)
        {
            return;
        }

        SetMacContextMenuPseudoFocus(target.Value);
    }

    private ContextMenuFocusTarget? GetContextMenuTarget(Button button)
    {
        if (ReferenceEquals(button, ContextEditButton))
        {
            return ContextMenuFocusTarget.Edit;
        }

        if (ReferenceEquals(button, ContextDeleteButton))
        {
            return ContextMenuFocusTarget.Delete;
        }

        return null;
    }

    private void SetMacContextMenuPseudoFocus(ContextMenuFocusTarget target)
    {
        macPseudoFocusedContextMenuTarget = target;
    }

    private void ClearMacContextMenuPseudoFocus()
    {
        macPseudoFocusedContextMenuTarget = null;
    }
#endif

    private void ApplyContextActionButtonPlatformFocusSettings()
    {
#if WINDOWS
        DisableWindowsSystemFocusVisual(ContextEditButton);
        DisableWindowsSystemFocusVisual(ContextDeleteButton);
#endif
    }

    private void ConflictActionButton_Focused(object? sender, FocusEventArgs e)
    {
        if (sender is Button focusedButton)
        {
            SyncConflictDialogPseudoFocusFromButton(focusedButton);
        }

        ApplyConflictActionButtonFocusVisuals();
    }

    private void ConflictActionButton_HandlerChanged(object? sender, EventArgs e)
    {
        ApplyConflictActionButtonPlatformFocusSettings();
        ApplyConflictActionButtonFocusVisuals();
    }

    private void ConflictActionButton_Unfocused(object? sender, FocusEventArgs e)
    {
        if (sender is Button unfocusedButton)
        {
            var target = GetConflictDialogTarget(unfocusedButton);
            if (target is not null &&
                conflictDialogPseudoFocusedTarget == target &&
                !IsConflictDialogOpen())
            {
                ClearConflictDialogPseudoFocus();
            }
        }

        ApplyConflictActionButtonFocusVisuals();
#if WINDOWS
        QueueWindowsConflictDialogFocusRestore();
#endif
    }

    private void ApplyConflictActionButtonPlatformFocusSettings()
    {
#if WINDOWS
        DisableWindowsSystemFocusVisual(ConflictReloadButton);
        DisableWindowsSystemFocusVisual(ConflictOverwriteButton);
        DisableWindowsSystemFocusVisual(ConflictCancelButton);
#endif
    }

    private void ApplyConflictActionButtonFocusVisuals()
    {
        if (!IsConflictDialogOpen())
        {
            ApplyButtonFocusVisual(ConflictReloadButton, false);
            ApplyButtonFocusVisual(ConflictOverwriteButton, false);
            ApplyButtonFocusVisual(ConflictCancelButton, false);
            return;
        }

        if (conflictDialogPseudoFocusedTarget is ConflictDialogFocusTarget pseudoTarget)
        {
            ApplyButtonFocusVisual(ConflictReloadButton, pseudoTarget == ConflictDialogFocusTarget.Reload);
            ApplyButtonFocusVisual(ConflictOverwriteButton, pseudoTarget == ConflictDialogFocusTarget.Overwrite);
            ApplyButtonFocusVisual(ConflictCancelButton, pseudoTarget == ConflictDialogFocusTarget.Cancel);
            return;
        }

        ApplyButtonFocusVisual(ConflictReloadButton, IsButtonFocused(ConflictReloadButton));
        ApplyButtonFocusVisual(ConflictOverwriteButton, IsButtonFocused(ConflictOverwriteButton));
        ApplyButtonFocusVisual(ConflictCancelButton, IsButtonFocused(ConflictCancelButton));
    }

    private void SyncConflictDialogPseudoFocusFromButton(Button button)
    {
        var target = GetConflictDialogTarget(button);
        if (target is null)
        {
            return;
        }

        SetConflictDialogPseudoFocus(target.Value);
    }

    private ConflictDialogFocusTarget? GetConflictDialogTarget(Button button)
    {
        if (ReferenceEquals(button, ConflictReloadButton))
        {
            return ConflictDialogFocusTarget.Reload;
        }

        if (ReferenceEquals(button, ConflictOverwriteButton))
        {
            return ConflictDialogFocusTarget.Overwrite;
        }

        if (ReferenceEquals(button, ConflictCancelButton))
        {
            return ConflictDialogFocusTarget.Cancel;
        }

        return null;
    }

    private void SetConflictDialogPseudoFocus(ConflictDialogFocusTarget target)
    {
        conflictDialogPseudoFocusedTarget = target;
    }

    private void ClearConflictDialogPseudoFocus()
    {
        conflictDialogPseudoFocusedTarget = null;
    }

#if WINDOWS
    private static void DisableWindowsSystemFocusVisual(Button button)
    {
        if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Control control)
        {
            return;
        }

        var prop = control.GetType().GetProperty("UseSystemFocusVisuals", BindingFlags.Public | BindingFlags.Instance);
        if (prop?.CanWrite == true)
        {
            var controlType = control.GetType().Name;
            try
            {
                prop.SetValue(control, false);
            }
            catch (Exception ex)
            {
                var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
                CrashFileLogger.WriteWarning(nameof(DisableWindowsSystemFocusVisual), $"Failed to disable UseSystemFocusVisuals on {controlType}: {safeMessage}");
            }
        }
    }
#endif

    private void ApplyButtonFocusVisual(Button button, bool focused)
    {
        var dark = IsDarkThemeActive();
        button.BorderColor = Color.FromArgb(ButtonFocusVisualPolicy.ResolveBorderColorHex(focused, dark));
        button.BorderWidth = ButtonFocusVisualPolicy.ResolveBorderWidth();
    }

    private static bool IsButtonFocused(Button button)
    {
        if (button.IsFocused)
        {
            return true;
        }

#if WINDOWS
        if (button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Control control)
        {
            return control.FocusState != Microsoft.UI.Xaml.FocusState.Unfocused;
        }
#endif

#if MACCATALYST
        if (button.Handler?.PlatformView is UIResponder responder)
        {
            return responder.IsFirstResponder;
        }
#endif

        return false;
    }

    private bool IsMainCommandEntryActive()
    {
#if MACCATALYST
        if (MainCommandEntry.Handler?.PlatformView is UIResponder responder)
        {
            var firstResponder = GetCurrentMacFirstResponder();
            if (firstResponder is not null)
            {
                if (ReferenceEquals(firstResponder, responder))
                {
                    return true;
                }

                if (firstResponder is UIView firstResponderView &&
                    responder is UIView responderView &&
                    firstResponderView.IsDescendantOfView(responderView))
                {
                    return true;
                }
            }

            return responder.IsFirstResponder;
        }
#else
        if (MainCommandEntry.IsFocused)
        {
            return true;
        }
#endif

        return false;
    }

    private bool IsMainSearchEntryActive()
    {
#if MACCATALYST
        if (MainSearchEntry.Handler?.PlatformView is UIResponder responder)
        {
            var firstResponder = GetCurrentMacFirstResponder();
            if (firstResponder is not null)
            {
                if (ReferenceEquals(firstResponder, responder))
                {
                    return true;
                }

                if (firstResponder is UIView firstResponderView &&
                    responder is UIView responderView &&
                    firstResponderView.IsDescendantOfView(responderView))
                {
                    return true;
                }
            }

            return responder.IsFirstResponder;
        }
#endif
        return MainSearchEntry.IsFocused;
    }

    private void ApplyTabPolicy()
    {
#if WINDOWS
        var editorOpen = viewModel.IsEditorOpen;
        var contextOpen = viewModel.IsContextMenuOpen;
        var conflictOpen = IsConflictDialogOpen();
        var mainEnabled = !editorOpen && !contextOpen && !conflictOpen;
        var contextEnabled = contextOpen && !conflictOpen;
        var editorEnabled = editorOpen && !conflictOpen;

        // Main area: only Command and Search are tabbable when modal is closed.
        SetTabStop(MainCommandEntry, mainEnabled);
        SetTabStop(MainSearchEntry, mainEnabled);
        SetTabStop(CreateButton, false);

        // Context menu area: loop only Edit/Delete.
        SetTabStop(ContextEditButton, contextEnabled);
        SetTabStop(ContextDeleteButton, contextEnabled);

        // Modal editor area: keep tab navigation inside modal when open.
        SetTabStop(ModalGuidEntry, editorEnabled);
        SetTabStop(ModalCommandEntry, editorEnabled);
        SetTabStop(ModalButtonTextEntry, editorEnabled);
        SetTabStop(ModalToolEntry, editorEnabled);
        SetTabStop(ModalArgumentsEntry, editorEnabled);
        SetTabStop(ModalClipWordEditor, editorEnabled);
        SetTabStop(ModalNoteEditor, editorEnabled);
        SetTabStop(ModalInvertThemeCheckBox, editorEnabled);
        SetTabStop(ModalCancelButton, editorEnabled);
        SetTabStop(ModalSaveButton, editorEnabled);

        // Conflict dialog area: keep tab focus only within conflict action buttons.
        SetTabStop(ConflictReloadButton, conflictOpen);
        SetTabStop(ConflictOverwriteButton, conflictOpen);
        SetTabStop(ConflictCancelButton, conflictOpen);

        // Copy buttons are clickable, but excluded from tab traversal.
        SetTabStop(CopyGuidButton, false);
        SetTabStop(CopyCommandButton, false);
        SetTabStop(CopyButtonTextButton, false);
        SetTabStop(CopyToolButton, false);
        SetTabStop(CopyArgumentsButton, false);
        SetTabStop(CopyClipWordButton, false);
        SetTabStop(CopyNoteButton, false);

        ApplyContextActionButtonFocusVisuals();
        ApplyConflictActionButtonFocusVisuals();
#endif
    }

}
