using Praxis.Core.Logic;
using Praxis.ViewModels;
#if MACCATALYST
using UIKit;
#endif

namespace Praxis;

public partial class MainPage
{
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
}
