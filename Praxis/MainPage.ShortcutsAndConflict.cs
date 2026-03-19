using System.Collections.Specialized;
using System.ComponentModel;

using Praxis.Behaviors;
using Praxis.Core.Logic;
using Praxis.Core.Models;
using Praxis.ViewModels;
#if MACCATALYST
using Foundation;
using UIKit;
#endif

namespace Praxis;

public partial class MainPage
{
    private void CommandSuggestionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<CommandSuggestionItemViewModel>())
            {
                oldItem.PropertyChanged -= CommandSuggestionItemOnPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<CommandSuggestionItemViewModel>())
            {
                newItem.PropertyChanged += CommandSuggestionItemOnPropertyChanged;
            }
        }

        Dispatcher.Dispatch(RebuildCommandSuggestionStack);
    }

    private void CommandSuggestionItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CommandSuggestionItemViewModel.IsSelected))
        {
            Dispatcher.Dispatch(RebuildCommandSuggestionStack);
        }
    }

    private void OnThemeShortcutRequested(string mode)
    {
        Dispatcher.Dispatch(() => ApplyThemeShortcut(mode));
    }

    private void OnEditorShortcutRequested(string action)
    {
        void HandleShortcut()
        {
            if (IsConflictDialogOpen())
            {
                if (string.Equals(action, "ConflictDialogNext", StringComparison.OrdinalIgnoreCase))
                {
                    MoveConflictDialogFocus(forward: true);
                    return;
                }

                if (string.Equals(action, "ConflictDialogPrevious", StringComparison.OrdinalIgnoreCase))
                {
                    MoveConflictDialogFocus(forward: false);
                    return;
                }

                if (string.Equals(action, "TabNext", StringComparison.OrdinalIgnoreCase))
                {
                    MoveConflictDialogFocus(forward: true);
                    return;
                }

                if (string.Equals(action, "TabPrevious", StringComparison.OrdinalIgnoreCase))
                {
                    MoveConflictDialogFocus(forward: false);
                    return;
                }

                if (string.Equals(action, "Cancel", StringComparison.OrdinalIgnoreCase))
                {
                    TryInvokeConflictDialogCancelAction();
                    return;
                }

                if (string.Equals(action, "PrimaryAction", StringComparison.OrdinalIgnoreCase))
                {
                    TryInvokeConflictDialogPrimaryAction();
                    return;
                }
            }

            if (viewModel.IsContextMenuOpen)
            {
                if (string.Equals(action, "ContextMenuNext", StringComparison.OrdinalIgnoreCase))
                {
                    MoveContextMenuFocus(forward: true);
                    return;
                }

                if (string.Equals(action, "ContextMenuPrevious", StringComparison.OrdinalIgnoreCase))
                {
                    MoveContextMenuFocus(forward: false);
                    return;
                }

                if (string.Equals(action, "TabNext", StringComparison.OrdinalIgnoreCase))
                {
                    MoveContextMenuFocus(forward: true);
                    return;
                }

                if (string.Equals(action, "TabPrevious", StringComparison.OrdinalIgnoreCase))
                {
                    MoveContextMenuFocus(forward: false);
                    return;
                }

                if (string.Equals(action, "Cancel", StringComparison.OrdinalIgnoreCase))
                {
                    if (viewModel.CloseContextMenuCommand.CanExecute(null))
                    {
                        viewModel.CloseContextMenuCommand.Execute(null);
                    }
                    return;
                }

                if (string.Equals(action, "PrimaryAction", StringComparison.OrdinalIgnoreCase))
                {
                    TryInvokeContextMenuPrimaryAction();
                    return;
                }
            }

            if (!viewModel.IsEditorOpen)
            {
                return;
            }

            if (string.Equals(action, "Save", StringComparison.OrdinalIgnoreCase))
            {
#if MACCATALYST
                ClearMacModalPseudoFocus();
#endif
                if (viewModel.SaveEditorCommand.CanExecute(null))
                {
                    viewModel.SaveEditorCommand.Execute(null);
                }

                return;
            }

            if (string.Equals(action, "PrimaryAction", StringComparison.OrdinalIgnoreCase))
            {
#if MACCATALYST
                if (macPseudoFocusedModalTarget == ModalFocusTarget.CancelButton)
                {
                    ClearMacModalPseudoFocus();
                    if (viewModel.CancelEditorCommand.CanExecute(null))
                    {
                        viewModel.CancelEditorCommand.Execute(null);
                    }
                }
                else if (macPseudoFocusedModalTarget == ModalFocusTarget.SaveButton)
                {
                    ClearMacModalPseudoFocus();
                    if (viewModel.SaveEditorCommand.CanExecute(null))
                    {
                        viewModel.SaveEditorCommand.Execute(null);
                    }
                }
#endif
                return;
            }

            if (string.Equals(action, "TabNext", StringComparison.OrdinalIgnoreCase))
            {
#if MACCATALYST
                MoveModalFocus(forward: true);
#endif
                return;
            }

            if (string.Equals(action, "TabPrevious", StringComparison.OrdinalIgnoreCase))
            {
#if MACCATALYST
                MoveModalFocus(forward: false);
#endif
                return;
            }

            if (!string.Equals(action, "Cancel", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

#if MACCATALYST
            ClearMacModalPseudoFocus();
#endif
            var canCancel = viewModel.CancelEditorCommand.CanExecute(null);
            if (canCancel)
            {
                viewModel.CancelEditorCommand.Execute(null);
            }
        }

        if (Dispatcher.IsDispatchRequired)
        {
            Dispatcher.Dispatch(HandleShortcut);
            return;
        }

        HandleShortcut();
    }

    private void TryInvokeContextMenuPrimaryAction()
    {
        if (!viewModel.IsContextMenuOpen)
        {
            return;
        }

#if MACCATALYST
        if (macPseudoFocusedContextMenuTarget == ContextMenuFocusTarget.Delete)
        {
            if (viewModel.ContextDeleteCommand.CanExecute(null))
            {
                viewModel.ContextDeleteCommand.Execute(null);
            }
            return;
        }

        if (macPseudoFocusedContextMenuTarget == ContextMenuFocusTarget.Edit)
        {
            if (viewModel.ContextEditCommand.CanExecute(null))
            {
                viewModel.ContextEditCommand.Execute(null);
            }
            return;
        }
#endif

        if (IsButtonFocused(ContextDeleteButton))
        {
            if (viewModel.ContextDeleteCommand.CanExecute(null))
            {
                viewModel.ContextDeleteCommand.Execute(null);
            }
            return;
        }

        if (viewModel.ContextEditCommand.CanExecute(null))
        {
            viewModel.ContextEditCommand.Execute(null);
        }
    }

    private void OnCommandInputShortcutRequested(string action)
    {
#if MACCATALYST
        Dispatcher.Dispatch(() =>
        {
            if (!IsMainCommandEntryReadyForSuggestionNavigation())
            {
                return;
            }

            if (string.Equals(action, "Up", StringComparison.OrdinalIgnoreCase))
            {
                if (viewModel.MoveSuggestionUpCommand.CanExecute(null))
                {
                    viewModel.MoveSuggestionUpCommand.Execute(null);
                }

                return;
            }

            if (string.Equals(action, "Down", StringComparison.OrdinalIgnoreCase) &&
                viewModel.MoveSuggestionDownCommand.CanExecute(null))
            {
                viewModel.MoveSuggestionDownCommand.Execute(null);
            }
        });
#endif
    }

    private void OnHistoryShortcutRequested(string action)
    {
        Dispatcher.Dispatch(() => TryExecuteHistoryShortcut(action));
    }

    private bool TryExecuteHistoryShortcut(string action)
    {
        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
        {
            return false;
        }

        if (string.Equals(action, "Undo", StringComparison.OrdinalIgnoreCase))
        {
            if (viewModel.UndoCommand.CanExecute(null))
            {
                HideQuickLookPopup();
                viewModel.UndoCommand.Execute(null);
                return true;
            }

            return false;
        }

        if (string.Equals(action, "Redo", StringComparison.OrdinalIgnoreCase))
        {
            if (viewModel.RedoCommand.CanExecute(null))
            {
                HideQuickLookPopup();
                viewModel.RedoCommand.Execute(null);
                return true;
            }

            return false;
        }

        return false;
    }

    private void OnMiddleMouseClickRequested()
    {
#if MACCATALYST
        Dispatcher.Dispatch(() =>
        {
            if (lastPointerOnRoot is null)
            {
                return;
            }

            HandleMacMiddleClick(lastPointerOnRoot.Value);
        });
#endif
    }

#if MACCATALYST
    private void OnMacApplicationActivated()
    {
        if (!xamlLoaded)
        {
            return;
        }

        ScheduleMainCommandFocusAfterActivation("App.MacApplicationActivated");
    }

    private void OnMacApplicationDeactivating()
    {
        lastPointerOnRoot = null;
        macSearchFocusUserIntentUntilUtc = DateTimeOffset.MinValue;
    }

    private void AttachMacActivationObservers()
    {
        if (macDidBecomeActiveObserver is not null)
        {
            return;
        }

        var center = NSNotificationCenter.DefaultCenter;
        macDidBecomeActiveObserver = center.AddObserver(UIApplication.DidBecomeActiveNotification, _ =>
        {
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            ScheduleMainCommandFocusAfterActivation("UIApplication.DidBecomeActiveNotification");
        });
        macWillEnterForegroundObserver = center.AddObserver(UIApplication.WillEnterForegroundNotification, _ =>
        {
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            ScheduleMainCommandFocusAfterActivation("UIApplication.WillEnterForegroundNotification");
        });
        macSceneWillEnterForegroundObserver = center.AddObserver(UIScene.WillEnterForegroundNotification, _ =>
        {
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            ScheduleMainCommandFocusAfterActivation("UIScene.WillEnterForegroundNotification");
        });
        macSceneDidActivateObserver = center.AddObserver(UIScene.DidActivateNotification, _ =>
        {
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            ScheduleMainCommandFocusAfterActivation("UIScene.DidActivateNotification");
        });
        macWindowDidBecomeKeyObserver = center.AddObserver(UIWindow.DidBecomeKeyNotification, _ =>
        {
            if (UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active)
            {
                return;
            }

            App.RecordActivation();
            App.SetMacApplicationActive(true);
            ScheduleMainCommandFocusAfterActivation("UIWindow.DidBecomeKeyNotification");
        });
        macWillResignActiveObserver = center.AddObserver(UIApplication.WillResignActiveNotification, _ =>
        {
            App.SetMacApplicationActive(false);
        });
        macDidEnterBackgroundObserver = center.AddObserver(UIApplication.DidEnterBackgroundNotification, _ =>
        {
            App.SetMacApplicationActive(false);
        });
        macWindowDidResignKeyObserver = center.AddObserver(UIWindow.DidResignKeyNotification, _ =>
        {
            App.SetMacApplicationActive(false);
        });
    }

    private void DetachMacActivationObservers()
    {
        if (macDidBecomeActiveObserver is null &&
            macWillEnterForegroundObserver is null &&
            macSceneDidActivateObserver is null &&
            macSceneWillEnterForegroundObserver is null &&
            macWindowDidBecomeKeyObserver is null &&
            macWillResignActiveObserver is null &&
            macDidEnterBackgroundObserver is null &&
            macWindowDidResignKeyObserver is null)
        {
            return;
        }

        var center = NSNotificationCenter.DefaultCenter;
        DisposeMacObserver(center, ref macDidBecomeActiveObserver);
        DisposeMacObserver(center, ref macWillEnterForegroundObserver);
        DisposeMacObserver(center, ref macSceneDidActivateObserver);
        DisposeMacObserver(center, ref macSceneWillEnterForegroundObserver);
        DisposeMacObserver(center, ref macWindowDidBecomeKeyObserver);
        DisposeMacObserver(center, ref macWillResignActiveObserver);
        DisposeMacObserver(center, ref macDidEnterBackgroundObserver);
        DisposeMacObserver(center, ref macWindowDidResignKeyObserver);
    }

    private static void DisposeMacObserver(NSNotificationCenter center, ref NSObject? observer)
    {
        if (observer is null)
        {
            return;
        }

        center.RemoveObserver(observer);
        observer.Dispose();
        observer = null;
    }

    private bool IsMacSearchFocusUserInitiated()
    {
        return DateTimeOffset.UtcNow <= macSearchFocusUserIntentUntilUtc;
    }

    private bool IsMacActivationFocusSessionInProgress()
    {
        return DateTimeOffset.UtcNow <= macActivationFocusSessionUntilUtc;
    }

    private bool ShouldAllowMacSearchEntryFocusCore()
    {
        var shouldFocusMainCommand = ShouldFocusMainCommandAfterWindowActivation() && IsMacActivationFocusSessionInProgress();
        var isForeground = IsMacAppForegroundActive();
        var userInitiated = IsMacSearchFocusUserInitiated();
        return SearchFocusGuardPolicy.ShouldAllowSearchFocus(
            shouldFocusMainCommand: shouldFocusMainCommand,
            isAppForeground: isForeground,
            isUserInitiated: userInitiated);
    }

    private void MarkMacSearchFocusUserIntent(string _)
    {
        macSearchFocusUserIntentUntilUtc = DateTimeOffset.UtcNow + macSearchFocusUserIntentWindow;
    }

    private void FocusMainSearchEntryFromCommandTabCore()
    {
        if (!xamlLoaded || !initialized || viewModel.IsEditorOpen || IsConflictDialogOpen())
        {
            return;
        }

        MainSearchEntry.Focus();
        if (MainSearchEntry.Handler?.PlatformView is UITextField searchField && searchField.CanBecomeFirstResponder)
        {
            searchField.BecomeFirstResponder();
        }
    }
#endif

    private void ApplyThemeShortcut(string mode)
    {
        if (viewModel.SetThemeCommand.CanExecute(mode))
        {
            viewModel.SetThemeCommand.Execute(mode);
        }
    }

    private async Task<EditorConflictResolution> ResolveEditorConflictAsync(EditorConflictContext context)
    {
        var title = context.ConflictType == EditorConflictType.DeletedByOtherWindow
            ? "Conflict detected (deleted)"
            : "Conflict detected (updated)";
        var message = context.ConflictType == EditorConflictType.DeletedByOtherWindow
            ? "This button was deleted in another window."
            : "This button was updated in another window.";

        return await ShowEditorConflictDialogAsync(title, message);
    }

    private Task<EditorConflictResolution> ShowEditorConflictDialogAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<EditorConflictResolution>(TaskCreationOptions.RunContinuationsAsynchronously);
        editorConflictTcs = tcs;

        Dispatcher.Dispatch(() =>
        {
            HideQuickLookPopup();
            ConflictTitleLabel.Text = title;
            ConflictMessageLabel.Text = message;
            SetConflictDialogPseudoFocus(ConflictDialogFocusTarget.Cancel);
            ConflictOverlay.IsVisible = true;
            App.SetConflictDialogOpenState(true);
            UpdateConflictDialogModalState(isOpen: true);
            CloseCommandSuggestionPopup();
            ApplyTabPolicy();
            ApplyConflictActionButtonFocusVisuals();
            FocusConflictDialogActionButton(ConflictCancelButton, ConflictDialogFocusTarget.Cancel);
            Dispatcher.DispatchDelayed(UiTimingPolicy.ConflictDialogFocusRetryDelay, () =>
            {
                if (!IsConflictDialogOpen() || IsButtonFocused(ConflictCancelButton))
                {
                    return;
                }

                FocusConflictDialogActionButton(ConflictCancelButton, ConflictDialogFocusTarget.Cancel);
            });
        });

        return tcs.Task;
    }

    private void CloseEditorConflictDialog(EditorConflictResolution resolution)
    {
        if (editorConflictTcs is null)
        {
            return;
        }

        var tcs = editorConflictTcs;
        editorConflictTcs = null;
        ConflictOverlay.IsVisible = false;
        ClearConflictDialogPseudoFocus();
        App.SetConflictDialogOpenState(false);
        UpdateConflictDialogModalState(isOpen: false);
        ApplyTabPolicy();
        ApplyConflictActionButtonFocusVisuals();
        tcs.TrySetResult(resolution);
        RestoreEditorFocusAfterConflictDialogClose();
    }

    private void ConflictReloadButton_Clicked(object? sender, EventArgs e)
    {
        CloseEditorConflictDialog(EditorConflictResolution.Reload);
    }

    private void ConflictOverwriteButton_Clicked(object? sender, EventArgs e)
    {
        CloseEditorConflictDialog(EditorConflictResolution.Overwrite);
    }

    private void ConflictCancelButton_Clicked(object? sender, EventArgs e)
    {
        CloseEditorConflictDialog(EditorConflictResolution.Cancel);
    }

    private bool IsConflictDialogOpen()
    {
        if (!xamlLoaded)
        {
            return false;
        }

        return editorConflictTcs is not null && ConflictOverlay.IsVisible;
    }

    private void RestoreEditorFocusAfterConflictDialogClose()
    {
        Dispatcher.DispatchDelayed(UiTimingPolicy.ConflictDialogEditorFocusRestoreDelay, () =>
        {
            if (!ConflictDialogFocusRestorePolicy.ShouldRestoreEditorFocus(viewModel.IsEditorOpen, IsConflictDialogOpen()))
            {
                return;
            }

            FocusModalPrimaryEditorField();
#if WINDOWS
            EnsureWindowsKeyHooks();
#endif
        });
    }

    private void UpdateConflictDialogModalState(bool isOpen)
    {
        if (!xamlLoaded)
        {
            return;
        }

        EditorOverlay.InputTransparent = isOpen;
        EditorOverlay.IsEnabled = !isOpen;
#if MACCATALYST
        if (isOpen)
        {
            ResignModalInputFirstResponder();
            ResignMainInputFirstResponder();
        }
#endif
    }

    private void FocusConflictDialogActionButton(Button button, ConflictDialogFocusTarget target)
    {
        SetConflictDialogPseudoFocus(target);
#if MACCATALYST
        ResignModalInputFirstResponder();
        ResignMainInputFirstResponder();
#endif
        ApplyConflictActionButtonFocusVisuals();
        button.Focus();
#if MACCATALYST
        if (button.Handler?.PlatformView is UIResponder responder &&
            responder.CanBecomeFirstResponder &&
            !responder.IsFirstResponder)
        {
            responder.BecomeFirstResponder();
        }
#endif
        ApplyConflictActionButtonFocusVisuals();
    }

    private int GetCurrentConflictDialogFocusIndex(Button[] buttons)
    {
        for (var i = 0; i < buttons.Length; i++)
        {
            if (IsButtonFocused(buttons[i]))
            {
                return i;
            }
        }

        return conflictDialogPseudoFocusedTarget switch
        {
            ConflictDialogFocusTarget.Reload => 0,
            ConflictDialogFocusTarget.Overwrite => 1,
            ConflictDialogFocusTarget.Cancel => 2,
            _ => -1,
        };
    }

    private void MoveConflictDialogFocus(bool forward)
    {
        if (!IsConflictDialogOpen())
        {
            return;
        }

        var order = new[]
        {
            (Button: ConflictReloadButton, Target: ConflictDialogFocusTarget.Reload),
            (Button: ConflictOverwriteButton, Target: ConflictDialogFocusTarget.Overwrite),
            (Button: ConflictCancelButton, Target: ConflictDialogFocusTarget.Cancel),
        };

        var buttons = new[] { ConflictReloadButton, ConflictOverwriteButton, ConflictCancelButton };
        var currentIndex = GetCurrentConflictDialogFocusIndex(buttons);
        if (currentIndex < 0)
        {
            FocusConflictDialogActionButton(ConflictCancelButton, ConflictDialogFocusTarget.Cancel);
            return;
        }

        var nextIndex = FocusRingNavigator.GetNextIndex(currentIndex, order.Length, forward);
        if (nextIndex < 0)
        {
            return;
        }

        FocusConflictDialogActionButton(order[nextIndex].Button, order[nextIndex].Target);
    }

    private void TryInvokeConflictDialogPrimaryAction()
    {
        if (!IsConflictDialogOpen())
        {
            return;
        }

        var order = new[] { ConflictReloadButton, ConflictOverwriteButton, ConflictCancelButton };
        var currentIndex = GetCurrentConflictDialogFocusIndex(order);
        switch (currentIndex)
        {
            case 0:
                CloseEditorConflictDialog(EditorConflictResolution.Reload);
                return;
            case 1:
                CloseEditorConflictDialog(EditorConflictResolution.Overwrite);
                return;
            case 2:
                CloseEditorConflictDialog(EditorConflictResolution.Cancel);
                return;
            default:
                CloseEditorConflictDialog(EditorConflictResolution.Cancel);
                return;
        }
    }

    private void TryInvokeConflictDialogCancelAction()
    {
        if (!IsConflictDialogOpen())
        {
            return;
        }

        CloseEditorConflictDialog(EditorConflictResolution.Cancel);
    }

    private void RootGrid_PointerPressed(object? sender, PointerEventArgs e)
    {
        if (suppressNextRootSuggestionClose)
        {
            suppressNextRootSuggestionClose = false;
            return;
        }

        HideQuickLookPopup();

        var pointer = e.GetPosition(RootGrid);
        if (pointer is not null)
        {
            lastPointerOnRoot = pointer.Value;
            SyncDockScrollBarVisibility(pointer.Value);
        }

#if MACCATALYST
        if (pointer is not null && IsMiddlePointerPressed(e) && !IsSecondaryPointerPressed(e))
        {
            if (HandleMacMiddleClick(pointer.Value))
            {
                return;
            }
        }
#endif

        if (!viewModel.IsCommandSuggestionOpen)
        {
            return;
        }

        if (pointer is null)
        {
            return;
        }

        if (IsPointInsideElement(pointer.Value, MainCommandEntry) || IsPointInsideElement(pointer.Value, CommandSuggestionPopup))
        {
            return;
        }

        CloseCommandSuggestionPopup();
    }

    private void RootGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        var pointer = e.GetPosition(RootGrid);
        if (pointer is not null)
        {
            lastPointerOnRoot = pointer.Value;
            SyncDockScrollBarVisibility(pointer.Value);
            return;
        }

        lastPointerOnRoot = null;
        CancelDockHoverExitHide();
        SetDockScrollBarVisibility(isPointerOverDockRegion: false);
    }

    private void RootGrid_PointerExited(object? sender, PointerEventArgs e)
    {
        lastPointerOnRoot = null;
        CancelDockHoverExitHide();
        SetDockScrollBarVisibility(isPointerOverDockRegion: false);
    }

    private void SyncDockScrollBarVisibility(Point rootPoint)
    {
        var isPointerOverDockRegion = IsPointInsideElement(rootPoint, DockRegionBorder);
        if (isPointerOverDockRegion)
        {
            CancelDockHoverExitHide();
        }
        SetDockScrollBarVisibility(isPointerOverDockRegion);
    }

#if MACCATALYST
    private bool HandleMacMiddleClick(Point rootPoint)
    {
        if (!App.IsMacApplicationActive() || App.IsActivationSuppressionActive())
        {
            return false;
        }

        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            return false;
        }

        var suggestionHit = TryGetSuggestionItemAtRootPoint(rootPoint);
        if (suggestionHit is not null)
        {
            CloseCommandSuggestionPopup();
            if (viewModel.OpenEditorCommand.CanExecute(suggestionHit.Source))
            {
                viewModel.OpenEditorCommand.Execute(suggestionHit.Source);
                return true;
            }

            return false;
        }

        var hit = TryGetPlacementButtonAtRootPoint(rootPoint) ?? TryGetDockButtonAtRootPoint(rootPoint);
        if (hit is null)
        {
            return false;
        }

        suppressTapExecuteForItemId = hit.Id;
        if (viewModel.OpenEditorCommand.CanExecute(hit))
        {
            viewModel.OpenEditorCommand.Execute(hit);
            return true;
        }

        return false;
    }

    private CommandSuggestionItemViewModel? TryGetSuggestionItemAtRootPoint(Point rootPoint)
    {
        if (!viewModel.IsCommandSuggestionOpen || viewModel.CommandSuggestions.Count == 0)
        {
            return null;
        }

        var localInStack = TryConvertRootPointToElementLocal(rootPoint, CommandSuggestionStack);
        if (localInStack is null)
        {
            return null;
        }

        const double rowHeight = 34;
        const double itemSpacing = 2;
        const double itemStep = rowHeight + itemSpacing;

        var scrollY = CommandSuggestionScrollView.ScrollY;
        var adjustedY = localInStack.Value.Y + scrollY;

        var index = (int)(adjustedY / itemStep);
        if (index < 0 || index >= viewModel.CommandSuggestions.Count)
        {
            return null;
        }

        return viewModel.CommandSuggestions[index];
    }

    private LauncherButtonItemViewModel? TryGetPlacementButtonAtRootPoint(Point rootPoint)
    {
        var local = TryConvertRootPointToElementLocal(rootPoint, PlacementScroll);
        if (local is null)
        {
            return null;
        }

        var canvasPoint = new Point(local.Value.X + PlacementScroll.ScrollX, local.Value.Y + PlacementScroll.ScrollY);
        return viewModel.VisibleButtons.LastOrDefault(item =>
            canvasPoint.X >= item.X &&
            canvasPoint.X <= item.X + item.Width &&
            canvasPoint.Y >= item.Y &&
            canvasPoint.Y <= item.Y + item.Height);
    }

    private LauncherButtonItemViewModel? TryGetDockButtonAtRootPoint(Point rootPoint)
    {
        var local = TryConvertRootPointToElementLocal(rootPoint, DockScroll);
        if (local is null)
        {
            return null;
        }

        var x = local.Value.X + DockScroll.ScrollX;
        var y = local.Value.Y + DockScroll.ScrollY;
        const double spacing = 10;
        var cursor = 0.0;

        foreach (var item in viewModel.DockButtons)
        {
            var width = Math.Max(1, item.Width);
            var height = Math.Max(1, item.Height);
            if (x >= cursor && x <= cursor + width && y >= 0 && y <= height)
            {
                return item;
            }

            cursor += width + spacing;
        }

        return null;
    }

    private Point? TryConvertRootPointToElementLocal(Point rootPoint, VisualElement element)
    {
        if (element.Width <= 0 || element.Height <= 0)
        {
            return null;
        }

        var offset = GetPositionRelativeToAncestor(element, RootGrid);
        var local = new Point(rootPoint.X - offset.X, rootPoint.Y - offset.Y);
        if (local.X < 0 || local.Y < 0 || local.X > element.Width || local.Y > element.Height)
        {
            return null;
        }

        return local;
    }
#endif

    private void UpdateCommandSuggestionPopupPlacement()
    {
        if (MainCommandEntry.Width <= 0 || MainCommandEntry.Height <= 0 || MainSearchEntry.Width <= 0)
        {
            return;
        }

        var commandPos = GetPositionRelativeToAncestor(MainCommandEntry, RootGrid);
        var searchPos = GetPositionRelativeToAncestor(MainSearchEntry, RootGrid);
        var left = commandPos.X - RootGrid.Padding.Left;
        var right = searchPos.X + MainSearchEntry.Width - RootGrid.Padding.Left;
        var top = commandPos.Y - RootGrid.Padding.Top;

        CommandSuggestionPopup.WidthRequest = Math.Max(80, right - left);
        CommandSuggestionPopup.TranslationX = left;
        CommandSuggestionPopup.TranslationY = top + MainCommandEntry.Height + 10;
    }

    private void SyncViewportToViewModel()
    {
        if (PlacementScroll.Width <= 0 || PlacementScroll.Height <= 0)
        {
            return;
        }

        viewModel.UpdateViewport(
            PlacementScroll.ScrollX,
            PlacementScroll.ScrollY,
            PlacementScroll.Width,
            PlacementScroll.Height);
    }

    private void CloseCommandSuggestionPopup()
    {
        if (viewModel.CloseCommandSuggestionsCommand.CanExecute(null))
        {
            viewModel.CloseCommandSuggestionsCommand.Execute(null);
        }
    }

    private void ReopenCommandSuggestionsFromEntry()
    {
        suppressNextRootSuggestionClose = true;
        if (viewModel.ReopenCommandSuggestionsCommand.CanExecute(null))
        {
            viewModel.ReopenCommandSuggestionsCommand.Execute(null);
        }
    }

    private static void ApplyCommandSuggestionRowThemeColors(Grid row, bool selected)
    {
        row.SetAppThemeColor(
            VisualElement.BackgroundColorProperty,
            Color.FromArgb(CommandSuggestionRowColorPolicy.ResolveBackgroundHex(selected, isDarkTheme: false)),
            Color.FromArgb(CommandSuggestionRowColorPolicy.ResolveBackgroundHex(selected, isDarkTheme: true)));
    }

    private static void ApplyCommandSuggestionLabelThemeColors(Label label)
    {
        label.SetAppThemeColor(
            Label.TextColorProperty,
            Color.FromArgb(ThemeTextColorPolicy.ResolveTextColorHex(isDarkTheme: false)),
            Color.FromArgb(ThemeTextColorPolicy.ResolveTextColorHex(isDarkTheme: true)));
    }

#if !MACCATALYST
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
                if (IsMiddlePointerPressed(e))
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
#endif
}
