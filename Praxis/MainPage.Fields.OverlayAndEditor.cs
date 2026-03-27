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

    // Overlay/popup state.
    private CancellationTokenSource? copyNoticeCts;
    private CancellationTokenSource? statusFlashCts;
    private CancellationTokenSource? quickLookShowCts;
    private CancellationTokenSource? quickLookHideCts;
    private CancellationTokenSource? dockHoverExitCts;
    private TaskCompletionSource<EditorConflictResolution>? editorConflictTcs;
    private Guid? quickLookPendingItemId;
    private VisualElement? quickLookPendingAnchor;
    private bool modalPrimaryFieldSelectAllPending;

    // Conflict dialog pseudo focus state.
    private ConflictDialogFocusTarget? conflictDialogPseudoFocusedTarget;
}
