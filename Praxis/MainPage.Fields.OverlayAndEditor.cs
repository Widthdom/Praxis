using Praxis.ViewModels;

namespace Praxis;

public partial class MainPage
{
    // Modal editor sizing constants.
    private const double ModalSingleLineRowHeight = 40;
    private const double ModalRowSpacing = 8;
    private const int ModalTotalRows = 8;
    private const int ModalStaticRows = 6;
    private const double ModalScrollMaxHeightFallback = 460;
    private const double ModalScrollVerticalReserve = 260;

    // Overlay/popup behavior constants.
    private const int QuickLookShowDelayMs = 1000;
    private const int QuickLookHideDelayMs = 120;
    private const uint QuickLookFadeDurationMs = 150;
    private const double QuickLookOffsetX = 14;
    private const double QuickLookOffsetY = 2;
    private const double QuickLookViewportMargin = 10;
    private const int DockHoverExitHideDelayMs = 60;

    // Overlay fade animation durations (per-overlay tuned).
    private const uint EditorOverlayFadeInMs = 140;
    private const uint EditorOverlayFadeOutMs = 160;
    private const uint ContextMenuOverlayFadeInMs = 110;
    private const uint ContextMenuOverlayFadeOutMs = 130;
    private const uint ConflictOverlayFadeInMs = 140;
    private const uint ConflictOverlayFadeOutMs = 160;
    private const uint CommandSuggestionOverlayFadeInMs = 90;
    private const uint CommandSuggestionOverlayFadeOutMs = 110;

    // Overlay/popup state.
    private CancellationTokenSource? copyNoticeCts;
    private CancellationTokenSource? statusFlashCts;
    private CancellationTokenSource? quickLookShowCts;
    private CancellationTokenSource? quickLookHideCts;
    private CancellationTokenSource? dockHoverExitCts;
    private CancellationTokenSource? editorOverlayFadeCts;
    private CancellationTokenSource? contextMenuOverlayFadeCts;
    private CancellationTokenSource? conflictOverlayFadeCts;
    private CancellationTokenSource? commandSuggestionOverlayFadeCts;
    private TaskCompletionSource<EditorConflictResolution>? editorConflictTcs;
    private Guid? quickLookPendingItemId;
    private VisualElement? quickLookPendingAnchor;
    private bool modalPrimaryFieldSelectAllPending;

    // Conflict dialog pseudo focus state.
    private ConflictDialogFocusTarget? conflictDialogPseudoFocusedTarget;
}
