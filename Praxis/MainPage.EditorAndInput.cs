using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

using Praxis.Behaviors;
using Praxis.Core.Logic;
using Praxis.Core.Models;
using Praxis.ViewModels;
#if MACCATALYST
using UIKit;
#endif

namespace Praxis;

public partial class MainPage
{
    private void ModalNoteEditor_HandlerChanged(object? sender, EventArgs e)
    {
        ApplyModalEditorThemeTextColors();
#if MACCATALYST
        if (ModalNoteEditor.Handler?.PlatformView is UITextView textView)
        {
            textView.BackgroundColor = UIColor.Clear;
            textView.Layer.BorderWidth = 0;
            textView.Layer.CornerRadius = 0;
        }
        ApplyMacNoteEditorVisualState();
        ModalNoteFocusUnderline.IsVisible = ModalNoteEditor.IsFocused;
        ApplyMacEditorKeyCommands();
#endif
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
        UpdateModalEditorHeights();
    }

    private void ModalClipWordEditor_HandlerChanged(object? sender, EventArgs e)
    {
        ApplyModalEditorThemeTextColors();
#if MACCATALYST
        if (ModalClipWordEditor.Handler?.PlatformView is UITextView textView)
        {
            textView.BackgroundColor = UIColor.Clear;
            textView.Layer.BorderWidth = 0;
            textView.Layer.CornerRadius = 0;
        }
        ApplyMacClipWordEditorVisualState();
        ModalClipWordFocusUnderline.IsVisible = ModalClipWordEditor.IsFocused;
        ApplyMacEditorKeyCommands();
#endif
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
        UpdateModalEditorHeights();
    }

    private void ModalClipWordEditor_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ClearMacModalPseudoFocus();
        ModalClipWordFocusUnderline.IsVisible = true;
#endif
    }

    private void ModalClipWordEditor_Unfocused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ModalClipWordFocusUnderline.IsVisible = false;
#endif
    }

    private void ModalNoteEditor_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ClearMacModalPseudoFocus();
        ModalNoteFocusUnderline.IsVisible = true;
#endif
    }

    private void ModalNoteEditor_Unfocused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ModalNoteFocusUnderline.IsVisible = false;
#endif
    }

    private void ModalActionButton_Unfocused(object? sender, FocusEventArgs e)
    {
#if WINDOWS
        QueueWindowsEditorFocusRestore();
#endif
    }

    private void ModalInvertThemeToggle_Tapped(object? sender, TappedEventArgs e)
    {
        viewModel.Editor.UseInvertedThemeColors = !viewModel.Editor.UseInvertedThemeColors;
        ModalInvertThemeCheckBox.Focus();
    }

    private void ModalGuidEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if MACCATALYST
        EnsureMacGuidEntryReadOnlyBehavior();
#endif
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
    }

    private void ModalTextInput_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
    }

    private void ApplyModalEditorThemeTextColors()
    {
        var dark = IsDarkThemeActive();
        var textColor = Color.FromArgb(ThemeTextColorPolicy.ResolveTextColorHex(dark));
        ModalClipWordEditor.TextColor = textColor;
        ModalNoteEditor.TextColor = textColor;
    }

    private void ModalEditorField_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        if (ReferenceEquals(sender, ModalGuidEntry))
        {
            EnsureMacGuidEntryReadOnlyBehavior();
        }

        ClearMacModalPseudoFocus();
#endif
    }

    private void ModalNoteEditor_TextChanged(object? sender, TextChangedEventArgs e)
    {
#if MACCATALYST
        TryHandleMacEditorTabTextInsertion(sender, e);
#endif
        UpdateModalEditorHeights(noteTextOverride: e.NewTextValue);
    }

    private void ModalClipWordEditor_TextChanged(object? sender, TextChangedEventArgs e)
    {
#if MACCATALYST
        TryHandleMacEditorTabTextInsertion(sender, e);
#endif
        UpdateModalEditorHeights(clipTextOverride: e.NewTextValue);
    }

    private void UpdateModalEditorHeights(string? clipTextOverride = null, string? noteTextOverride = null)
    {
        if (!xamlLoaded || EditorFieldsScrollView is null)
        {
            return;
        }

        var clipText = clipTextOverride ?? ModalClipWordEditor.Text ?? viewModel.Editor.ClipText;
        var noteText = noteTextOverride ?? ModalNoteEditor.Text ?? viewModel.Editor.Note;
        var clipHeight = ModalEditorHeightResolver.ResolveHeight(clipText);
        var noteHeight = ModalEditorHeightResolver.ResolveHeight(noteText);

        UpdateEditorHeight(ModalClipWordEditor, ModalClipWordContainer, CopyClipWordButton, clipHeight);
        UpdateEditorHeight(ModalNoteEditor, ModalNoteContainer, CopyNoteButton, noteHeight);

        var contentHeight = ResolveModalEditorScrollContentHeight(clipHeight, noteHeight);
        var maxHeight = ResolveModalEditorScrollMaxHeight();
        EditorFieldsScrollView.HeightRequest = ModalEditorScrollHeightResolver.Resolve(contentHeight, maxHeight);
        EditorFieldsScrollView.InvalidateMeasure();

        ModalClipWordContainer.InvalidateMeasure();
        ModalNoteContainer.InvalidateMeasure();
        EditorOverlay.InvalidateMeasure();
    }

    private static void UpdateEditorHeight(Editor editor, Border container, Button? copyButton, double targetHeight)
    {
        editor.HeightRequest = targetHeight;
        container.HeightRequest = targetHeight;
        if (copyButton is not null)
        {
            copyButton.HeightRequest = targetHeight;
        }
    }

    private static double ResolveModalEditorScrollContentHeight(double clipHeight, double noteHeight)
    {
        var dynamicHeight = clipHeight + noteHeight;
        var staticRowsHeight = ModalStaticRows * ModalSingleLineRowHeight;
        var totalSpacing = (ModalTotalRows - 1) * ModalRowSpacing;
        return staticRowsHeight + dynamicHeight + totalSpacing;
    }

    private double ResolveModalEditorScrollMaxHeight()
    {
        var hostHeight = RootGrid.Height > 0 ? RootGrid.Height : Height;
        if (hostHeight <= 0)
        {
            return ModalScrollMaxHeightFallback;
        }

        return Math.Max(180, hostHeight - ModalScrollVerticalReserve);
    }

    private void Draggable_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
#if !WINDOWS
        if (sender is not BindableObject bindable)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                panDragItem = bindable.BindingContext;
                panDragLastDx = 0;
                panDragLastDy = 0;
                ExecuteDragFromItem(panDragItem, GestureStatus.Started, 0, 0);
                break;
            case GestureStatus.Running:
                panDragLastDx = e.TotalX;
                panDragLastDy = e.TotalY;
                ExecuteDragFromItem(panDragItem ?? bindable.BindingContext, GestureStatus.Running, panDragLastDx, panDragLastDy);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
            {
                var dx = e.TotalX;
                var dy = e.TotalY;
                if (Math.Abs(dx) < 0.5 && Math.Abs(panDragLastDx) >= 0.5)
                {
                    dx = panDragLastDx;
                }

                if (Math.Abs(dy) < 0.5 && Math.Abs(panDragLastDy) >= 0.5)
                {
                    dy = panDragLastDy;
                }

                ExecuteDragFromItem(panDragItem ?? bindable.BindingContext, GestureStatus.Completed, dx, dy);
                panDragItem = null;
                panDragLastDx = 0;
                panDragLastDy = 0;
                break;
            }
            default:
                break;
        }
#endif
    }

    private async void Draggable_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

        if (suppressTapExecuteForItemId == item.Id)
        {
            suppressTapExecuteForItemId = null;
            return;
        }

        if (viewModel.ExecuteButtonCommand.CanExecute(item))
        {
            await viewModel.ExecuteButtonCommand.ExecuteAsync(item);
        }
    }

#if WINDOWS
    private void TryCapturePointer(object? sender, PointerEventArgs e)
    {
        var element = (sender as VisualElement)?.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (element is null)
        {
            return;
        }

        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        var pointer = routed?.Pointer;
        if (pointer is null)
        {
            return;
        }

        var captureMethod = element.GetType().GetMethod("CapturePointer");
        if (captureMethod?.Invoke(element, [pointer]) is bool captured && captured)
        {
            capturedElement = element;
        }
    }

    private void ReleaseCapturedPointer()
    {
        if (capturedElement is null)
        {
            return;
        }

        var releaseAllMethod = capturedElement.GetType().GetMethod("ReleasePointerCaptures", Type.EmptyTypes);
        releaseAllMethod?.Invoke(capturedElement, null);
        capturedElement = null;
    }
#else
    private void ReleaseCapturedPointer()
    {
    }
#endif

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Editor))
        {
            AttachEditorPropertyChanged(viewModel.Editor);
            UpdateModalEditorHeights();
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.SelectedTheme))
        {
            ApplyNeutralStatusBackground();
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.StatusRevision))
        {
            TriggerStatusFlash(viewModel.StatusText);
#if MACCATALYST
            RefocusMainCommandAfterCommandNotFound(viewModel.StatusText);
#endif
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.IsCommandSuggestionOpen))
        {
            Dispatcher.Dispatch(UpdateCommandSuggestionPopupPlacement);
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.IsContextMenuOpen))
        {
            App.SetContextMenuOpenState(viewModel.IsContextMenuOpen);
            if (viewModel.IsContextMenuOpen)
            {
                CloseCommandSuggestionPopup();
                HideQuickLookPopup();
#if MACCATALYST
                ResignMainInputFirstResponder();
#endif
            }
            ApplyTabPolicy();
            if (viewModel.IsContextMenuOpen)
            {
#if MACCATALYST
                SetMacContextMenuPseudoFocus(ContextMenuFocusTarget.Edit);
#endif
                Dispatcher.DispatchDelayed(UiTimingPolicy.ContextMenuFocusInitialDelay, () =>
                {
                    if (!viewModel.IsContextMenuOpen || viewModel.IsEditorOpen || IsConflictDialogOpen())
                    {
                        return;
                    }

                    FocusContextActionButton(ContextEditButton);
#if MACCATALYST
                    EnsureMacFirstResponder();
#endif
                    ApplyContextActionButtonFocusVisuals();
                });
                Dispatcher.DispatchDelayed(UiTimingPolicy.ContextMenuFocusRetryDelay, () =>
                {
                    if (!viewModel.IsContextMenuOpen || IsButtonFocused(ContextEditButton))
                    {
                        return;
                    }

                    FocusContextActionButton(ContextEditButton);
                    ApplyContextActionButtonFocusVisuals();
                });
            }
            else
            {
#if MACCATALYST
                ClearMacContextMenuPseudoFocus();
#endif
            }

            ApplyContextActionButtonFocusVisuals();

            return;
        }

        if (e.PropertyName != nameof(MainViewModel.IsEditorOpen) || !viewModel.IsEditorOpen)
        {
            if (e.PropertyName == nameof(MainViewModel.IsEditorOpen))
            {
                App.SetEditorOpenState(viewModel.IsEditorOpen);
                ApplyTabPolicy();
                UpdateModalEditorHeights();
#if MACCATALYST
                if (!viewModel.IsEditorOpen)
                {
                    ClearMacModalPseudoFocus();
                    macGuidLockedText = string.Empty;
                }
#endif
            }
            return;
        }

        App.SetEditorOpenState(true);
        HideQuickLookPopup();
        ApplyTabPolicy();
        UpdateModalEditorHeights();
#if MACCATALYST
        macGuidLockedText = viewModel.Editor.GuidText ?? string.Empty;
#endif
        Dispatcher.DispatchDelayed(UiTimingPolicy.EditorOpenFocusDelay, () =>
        {
            if (!viewModel.IsEditorOpen || IsConflictDialogOpen())
            {
                return;
            }

            FocusModalCommandEntryForOpen();
#if MACCATALYST
            EnsureMacFirstResponder();
            ApplyMacEditorKeyCommands();
            Dispatcher.DispatchDelayed(UiTimingPolicy.MacEditorKeyCommandsRetryDelay, ApplyMacEditorKeyCommands);
#endif
        });
    }

    private void AttachEditorPropertyChanged(ButtonEditorViewModel? editorViewModel)
    {
        if (ReferenceEquals(observedEditorViewModel, editorViewModel))
        {
            return;
        }

        if (observedEditorViewModel is not null)
        {
            observedEditorViewModel.PropertyChanged -= EditorViewModelOnPropertyChanged;
        }

        observedEditorViewModel = editorViewModel;
        if (observedEditorViewModel is not null)
        {
            observedEditorViewModel.PropertyChanged += EditorViewModelOnPropertyChanged;
        }
    }

    private void DetachEditorPropertyChanged()
    {
        if (observedEditorViewModel is null)
        {
            return;
        }

        observedEditorViewModel.PropertyChanged -= EditorViewModelOnPropertyChanged;
        observedEditorViewModel = null;
    }

    private void EditorViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(ButtonEditorViewModel.ClipText) or nameof(ButtonEditorViewModel.Note) or ""))
        {
            return;
        }

        Dispatcher.Dispatch(() => UpdateModalEditorHeights());
    }

    private async void TriggerStatusFlash(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || string.Equals(message, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        statusFlashCts?.Cancel();
        statusFlashCts?.Dispose();
        statusFlashCts = new CancellationTokenSource();
        var token = statusFlashCts.Token;
        var neutral = GetNeutralStatusBackgroundColor();
        var flash = StatusFlashErrorPolicy.IsErrorStatus(message)
            ? Color.FromArgb("#D94A4A")
            : Color.FromArgb("#4AAE6A");

        try
        {
            await AnimateStatusBackgroundAsync(neutral, flash, UiTimingPolicy.StatusFlashInDurationMs, Easing.CubicOut, token);
            token.ThrowIfCancellationRequested();
            await AnimateStatusBackgroundAsync(flash, neutral, UiTimingPolicy.StatusFlashOutDurationMs, Easing.CubicIn, token);
            token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            ResetStatusBarBackgroundToThemeBinding();
        }
    }

#if MACCATALYST
    private void RefocusMainCommandAfterCommandNotFound(string? statusMessage)
    {
        if (!CommandNotFoundRefocusPolicy.ShouldRefocusMainCommand(statusMessage))
        {
            return;
        }

        Dispatcher.DispatchDelayed(UiTimingPolicy.CommandNotFoundRefocusDelay, () =>
        {
            if (!xamlLoaded || viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
            {
                return;
            }

            ApplyEntryFocusAfterClearButtonTap(MainCommandEntry);
            EnsureMacFirstResponder();
        });
    }
#endif

    private async Task AnimateStatusBackgroundAsync(Color from, Color to, uint durationMs, Easing easing, CancellationToken token)
    {
        const int steps = 10;
        for (var i = 1; i <= steps; i++)
        {
            token.ThrowIfCancellationRequested();
            var t = (double)i / steps;
            var eased = easing.Ease(t);
            StatusBarBorder.BackgroundColor = LerpColor(from, to, eased);
            await Task.Delay((int)Math.Max(1, durationMs / steps), token);
        }
    }

    private static Color LerpColor(Color from, Color to, double t)
    {
        static float Lerp(float a, float b, double ratio) => (float)(a + (b - a) * ratio);
        return new Color(
            Lerp(from.Red, to.Red, t),
            Lerp(from.Green, to.Green, t),
            Lerp(from.Blue, to.Blue, t),
            Lerp(from.Alpha, to.Alpha, t));
    }

    private Color GetNeutralStatusBackgroundColor()
    {
#if WINDOWS
        if (pageNativeElement is Microsoft.UI.Xaml.FrameworkElement fe)
        {
            var actual = fe.ActualTheme;
            if (actual == Microsoft.UI.Xaml.ElementTheme.Dark)
            {
                return Color.FromArgb("#1E1E1E");
            }

            if (actual == Microsoft.UI.Xaml.ElementTheme.Light)
            {
                return Color.FromArgb("#F2F2F2");
            }
        }
#endif
        var theme = viewModel.SelectedTheme switch
        {
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            _ => Application.Current?.RequestedTheme ?? AppTheme.Unspecified,
        };

        return theme == AppTheme.Dark
            ? Color.FromArgb("#1E1E1E")
            : Color.FromArgb("#F2F2F2");
    }

    private bool IsDarkThemeActive()
    {
        var selectedTheme = Application.Current?.UserAppTheme switch
        {
            AppTheme.Dark => ThemeMode.Dark,
            AppTheme.Light => ThemeMode.Light,
            _ => ThemeMode.System,
        };

        var requestedThemeDark = Application.Current?.RequestedTheme == AppTheme.Dark;
#if MACCATALYST
        bool? traitDark = (Window?.Handler?.PlatformView as UIWindow)?.TraitCollection?.UserInterfaceStyle switch
        {
            UIUserInterfaceStyle.Dark => true,
            UIUserInterfaceStyle.Light => false,
            _ => null,
        };
#else
        bool? traitDark = null;
#endif
        return ThemeDarkStateResolver.Resolve(selectedTheme, requestedThemeDark, traitDark);
    }

    private void ApplyNeutralStatusBackground()
    {
        ResetStatusBarBackgroundToThemeBinding();
    }

    private void ResetStatusBarBackgroundToThemeBinding()
    {
        StatusBarBorder.ClearValue(VisualElement.BackgroundColorProperty);
    }

    private void MainCommandEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsKeyHooks();
        EnsureWindowsTextBoxHooks();
#endif
#if MACCATALYST
        ApplyMacCommandSuggestionKeyCommands();
#endif
        UpdateCommandSuggestionPopupPlacement();
    }

    private void MainCommandEntry_Focused(object? sender, FocusEventArgs e)
    {
        ReopenCommandSuggestionsFromEntry();
    }

    private void MainCommandEntry_PointerPressed(object? sender, PointerEventArgs e)
    {
        ReopenCommandSuggestionsFromEntry();
    }

    private void MainCommandEntry_Tapped(object? sender, TappedEventArgs e)
    {
        ReopenCommandSuggestionsFromEntry();
    }

    private void MainSearchEntry_Focused(object? sender, FocusEventArgs e)
    {
        CloseCommandSuggestionPopup();
    }

    private void MainSearchEntry_PointerPressed(object? sender, PointerEventArgs e)
    {
#if MACCATALYST
        MarkMacSearchFocusUserIntent("MainSearchEntry.PointerPressed");
#endif
    }

    private void MainSearchEntry_Tapped(object? sender, TappedEventArgs e)
    {
#if MACCATALYST
        MarkMacSearchFocusUserIntent("MainSearchEntry.Tapped");
#endif
    }

    private void MainSearchEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsKeyHooks();
        EnsureWindowsTextBoxHooks();
#endif
    }

    private void DockRegion_PointerEntered(object? sender, PointerEventArgs e)
    {
        CancelDockHoverExitHide();
        SetDockScrollBarVisibility(isPointerOverDockRegion: true);
    }

    private void DockRegion_PointerExited(object? sender, PointerEventArgs e)
    {
        ScheduleDockHoverExitHide();
    }

    private void DockScroll_HandlerChanged(object? sender, EventArgs e)
    {
        RefreshDockScrollBarVisibility();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), () =>
        {
            RefreshDockScrollBarVisibility();
        });
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), () =>
        {
            RefreshDockScrollBarVisibility();
        });
    }

    private void SetDockScrollBarVisibility(bool isPointerOverDockRegion)
    {
        isDockPointerHovering = isPointerOverDockRegion;
        var hasHorizontalOverflow = ResolveDockHasHorizontalOverflow();
        var showHorizontalScrollBar = DockScrollBarVisibilityPolicy.ShouldShowHorizontalScrollBar(
            isPointerOverDockRegion: isPointerOverDockRegion,
            hasHorizontalOverflow: hasHorizontalOverflow);
        DockScrollBarMask.IsVisible = DockScrollBarVisibilityPolicy.ShouldShowScrollBarMask(showHorizontalScrollBar);
        ApplyNativeDockScrollBarVisibility(showHorizontalScrollBar);
    }

    private void RefreshDockScrollBarVisibility()
    {
        SetDockScrollBarVisibility(isDockPointerHovering);
    }

    private void ScheduleDockHoverExitHide()
    {
        CancelDockHoverExitHide();
        var cts = new CancellationTokenSource();
        dockHoverExitCts = cts;
        _ = HideDockScrollBarAfterExitDelayAsync(cts.Token);
    }

    private void CancelDockHoverExitHide()
    {
        dockHoverExitCts?.Cancel();
        dockHoverExitCts?.Dispose();
        dockHoverExitCts = null;
    }

    private async Task HideDockScrollBarAfterExitDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(DockHoverExitHideDelayMs, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        SetDockScrollBarVisibility(isPointerOverDockRegion: false);
    }

    private void ApplyNativeDockScrollBarVisibility(bool showHorizontalScrollBar)
    {
#if WINDOWS
        if (DockScroll.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer)
        {
            scrollViewer.HorizontalScrollBarVisibility = showHorizontalScrollBar
                ? Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Visible
                : Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Hidden;
        }
#endif
#if MACCATALYST
        if (TryResolveMacDockScrollView() is UIScrollView scrollView)
        {
            scrollView.ShowsHorizontalScrollIndicator = showHorizontalScrollBar;
            scrollView.SetNeedsLayout();
            scrollView.LayoutIfNeeded();
        }
#endif
    }

    private bool ResolveDockHasHorizontalOverflow()
    {
#if WINDOWS
        if (DockScroll.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer &&
            scrollViewer.ExtentWidth > 0 &&
            scrollViewer.ViewportWidth > 0)
        {
            return scrollViewer.ExtentWidth > scrollViewer.ViewportWidth + 0.5;
        }
#endif
        if (DockScroll.Width <= 0 || DockButtonsStack.Width <= 0)
        {
            return false;
        }

        return DockButtonsStack.Width > DockScroll.Width + 0.5;
    }

#if MACCATALYST
    private UIScrollView? TryResolveMacDockScrollView()
    {
        if (DockScroll.Handler?.PlatformView is not UIView platformView)
        {
            return null;
        }

        return platformView as UIScrollView ?? FindFirstScrollView(platformView);
    }

    private static UIScrollView? FindFirstScrollView(UIView root)
    {
        foreach (var subview in root.Subviews)
        {
            if (subview is UIScrollView scrollView)
            {
                return scrollView;
            }

            var nested = FindFirstScrollView(subview);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
#endif

    private void ButtonQuickLook_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not VisualElement anchor || anchor.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

        QueueQuickLookPopup(item, anchor);
    }

    private void ButtonQuickLook_PointerExited(object? sender, PointerEventArgs e)
    {
        ScheduleQuickLookHide();
    }

    private void QueueQuickLookPopup(LauncherButtonItemViewModel item, VisualElement anchor)
    {
        if (!xamlLoaded || viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
        {
            return;
        }

        CancelQuickLookHide();
        CancelQuickLookShow();
        quickLookPendingItemId = item.Id;
        quickLookPendingAnchor = anchor;

        var cts = new CancellationTokenSource();
        quickLookShowCts = cts;
        _ = ShowQuickLookAfterDelayAsync(item, anchor, cts.Token);
    }

    private async Task ShowQuickLookAfterDelayAsync(LauncherButtonItemViewModel item, VisualElement anchor, CancellationToken token)
    {
        try
        {
            await Task.Delay(QuickLookShowDelayMs, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested ||
            quickLookPendingItemId != item.Id ||
            !ReferenceEquals(quickLookPendingAnchor, anchor))
        {
            return;
        }

        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
        {
            return;
        }

        QuickLookCommandLabel.Text = QuickLookPreviewFormatter.BuildLine("Command", item.Command);
        QuickLookToolLabel.Text = QuickLookPreviewFormatter.BuildLine("Tool", item.Tool);
        QuickLookArgumentsLabel.Text = QuickLookPreviewFormatter.BuildLine("Arguments", item.Arguments);
        QuickLookClipWordLabel.Text = QuickLookPreviewFormatter.BuildLine("Clip Word", item.ClipText);
        QuickLookNoteLabel.Text = QuickLookPreviewFormatter.BuildLine("Note", item.Note);
        PositionQuickLookPopup(anchor);

        QuickLookPopup.CancelAnimations();
        if (!QuickLookPopup.IsVisible)
        {
            QuickLookPopup.Opacity = 0;
            QuickLookPopup.IsVisible = true;
        }

        if (QuickLookPopup.Opacity < 1)
        {
            await QuickLookPopup.FadeToAsync(1, QuickLookFadeDurationMs, Easing.CubicOut);
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

    }

    private void ScheduleQuickLookHide()
    {
        CancelQuickLookShow();
        CancelQuickLookHide();
        var cts = new CancellationTokenSource();
        quickLookHideCts = cts;
        _ = HideQuickLookAfterDelayAsync(cts.Token);
    }

    private async Task HideQuickLookAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(QuickLookHideDelayMs, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested || !xamlLoaded || !QuickLookPopup.IsVisible)
        {
            return;
        }

        QuickLookPopup.CancelAnimations();
        if (QuickLookPopup.Opacity > 0)
        {
            await QuickLookPopup.FadeToAsync(0, QuickLookFadeDurationMs, Easing.CubicIn);
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        QuickLookPopup.IsVisible = false;
        QuickLookPopup.Opacity = 0;
    }

    private void PositionQuickLookPopup(VisualElement anchor)
    {
        if (RootGrid.Width <= 0 || RootGrid.Height <= 0 || anchor.Width <= 0 || anchor.Height <= 0)
        {
            return;
        }

        var anchorPos = GetPositionRelativeToAncestor(anchor, RootGrid);
        var popupWidth = QuickLookPopup.WidthRequest > 0 ? QuickLookPopup.WidthRequest : Math.Min(420, RootGrid.Width * 0.5);
        var popupHeight = Math.Max(140, QuickLookPopup.Height);

        var targetX = anchorPos.X + anchor.Width + QuickLookOffsetX;
        var targetY = anchorPos.Y + QuickLookOffsetY;

        var maxX = Math.Max(QuickLookViewportMargin, RootGrid.Width - popupWidth - QuickLookViewportMargin);
        var maxY = Math.Max(QuickLookViewportMargin, RootGrid.Height - popupHeight - QuickLookViewportMargin);

        if (targetX > maxX)
        {
            targetX = anchorPos.X - popupWidth - QuickLookOffsetX;
        }

        QuickLookPopup.TranslationX = Math.Clamp(targetX, QuickLookViewportMargin, maxX);
        QuickLookPopup.TranslationY = Math.Clamp(targetY, QuickLookViewportMargin, maxY);
    }

    private void HideQuickLookPopup()
    {
        CancelQuickLookShow();
        CancelQuickLookHide();
        quickLookPendingItemId = null;
        quickLookPendingAnchor = null;
        if (!xamlLoaded)
        {
            return;
        }

        QuickLookPopup.CancelAnimations();
        QuickLookPopup.Opacity = 0;
        QuickLookPopup.IsVisible = false;
    }

    private void CancelQuickLookShow()
    {
        quickLookShowCts?.Cancel();
        quickLookShowCts?.Dispose();
        quickLookShowCts = null;
    }

    private void CancelQuickLookHide()
    {
        quickLookHideCts?.Cancel();
        quickLookHideCts?.Dispose();
        quickLookHideCts = null;
    }

    private void ApplyClearButtonGlyphAlignmentTuning()
    {
        var translation = ClearButtonGlyphAlignmentPolicy.ResolveTranslation(OperatingSystem.IsWindows());
        CommandClearGlyph.TranslationX = translation;
        CommandClearGlyph.TranslationY = translation;
        SearchClearGlyph.TranslationX = translation;
        SearchClearGlyph.TranslationY = translation;
    }

    private void CommandClearButton_Tapped(object? sender, TappedEventArgs e)
    {
        if (viewModel.ClearCommandInputCommand.CanExecute(null))
        {
            viewModel.ClearCommandInputCommand.Execute(null);
        }

        FocusEntryAfterClearButtonTap(MainCommandEntry);
    }

    private void SearchClearButton_Tapped(object? sender, TappedEventArgs e)
    {
#if MACCATALYST
        MarkMacSearchFocusUserIntent("SearchClearButton.Tapped");
#endif
        if (viewModel.ClearSearchTextCommand.CanExecute(null))
        {
            viewModel.ClearSearchTextCommand.Execute(null);
        }

        FocusEntryAfterClearButtonTap(MainSearchEntry);
    }

    private void FocusEntryAfterClearButtonTap(Entry entry)
    {
        var retryDelays = ClearButtonRefocusPolicy.ResolveRetryDelays(OperatingSystem.IsWindows());
#if WINDOWS
        EnsureWindowsTextBoxHooks();
        windowsSelectAllOnTabNavigationPending = false;
#endif
        foreach (var delay in retryDelays)
        {
            if (delay <= TimeSpan.Zero)
            {
                Dispatcher.Dispatch(() => ApplyEntryFocusAfterClearButtonTap(entry));
                continue;
            }

            Dispatcher.DispatchDelayed(delay, () => ApplyEntryFocusAfterClearButtonTap(entry));
        }
    }

    private void ApplyEntryFocusAfterClearButtonTap(Entry entry)
    {
        entry.Focus();
#if MACCATALYST
        PlaceMacEntryCaretAtEnd(entry);
#endif
#if WINDOWS
        windowsSelectAllOnTabNavigationPending = false;
        var textBox = ResolveWindowsTextBoxForEntry(entry);
        if (textBox is not null)
        {
            textBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            PlaceWindowsTextBoxCaretAtEnd(textBox);
        }
#endif
    }

#if WINDOWS
    private Microsoft.UI.Xaml.Controls.TextBox? ResolveWindowsTextBoxForEntry(Entry entry)
    {
        if (ReferenceEquals(entry, MainCommandEntry))
        {
            return commandTextBox ?? entry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
        }

        if (ReferenceEquals(entry, MainSearchEntry))
        {
            return searchTextBox ?? entry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
        }

        return entry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
    }

    private static void PlaceWindowsTextBoxCaretAtEnd(Microsoft.UI.Xaml.Controls.TextBox textBox)
    {
        var caretPosition = TextCaretPositionResolver.ResolveTailOffset(textBox.Text);
        textBox.Select(caretPosition, 0);
    }
#endif

    private void ClearButton_PointerEntered(object? sender, PointerEventArgs e)
    {
        SetClearButtonHandCursor(sender, useHandCursor: true);
    }

    private void ClearButton_PointerExited(object? sender, PointerEventArgs e)
    {
        SetClearButtonHandCursor(sender, useHandCursor: false);
    }

    private static void SetClearButtonHandCursor(object? sender, bool useHandCursor)
    {
#if WINDOWS
        if (sender is VisualElement element &&
            element.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement frameworkElement)
        {
            var cursor = useHandCursor
                ? Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand)
                : null;
            NonPublicPropertySetter.TrySet(frameworkElement, "ProtectedCursor", cursor);
        }
#endif

#if MACCATALYST
        if (nsCursorClass == IntPtr.Zero)
        {
            return;
        }

        var cursorSelector = useHandCursor ? pointingHandCursorSelector : arrowCursorSelector;
        var cursor = ObjcMsgSendIntPtr(nsCursorClass, cursorSelector);
        if (cursor != IntPtr.Zero)
        {
            ObjcMsgSendVoid(cursor, setCursorSelector);
        }
#endif
    }

#if MACCATALYST
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjcGetClass(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendIntPtr(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void ObjcMsgSendVoid(IntPtr receiver, IntPtr selector);
#endif

#if WINDOWS
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
        if (!windowsSelectAllOnTabNavigationPending)
        {
            return;
        }

        windowsSelectAllOnTabNavigationPending = false;
        if (sender is Microsoft.UI.Xaml.Controls.TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void WindowsTextBox_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        windowsSelectAllOnTabNavigationPending = false;
    }

    private void WindowsModalInput_LostFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        QueueWindowsEditorFocusRestore();
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

        ModalCommandEntry.Focus();
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
        var shiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

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

#endif
}
