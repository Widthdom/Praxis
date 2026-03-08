namespace Praxis;

public partial class MainPage
{
    private bool pointerDragging;
    private Point pointerStart = Point.Zero;
    private double pointerLastDx;
    private double pointerLastDy;
#if !WINDOWS
    private double panDragLastDx;
    private double panDragLastDy;
    private object? panDragItem;
#endif
    private bool selectionDragging;
    private Guid? suppressTapExecuteForItemId;
    private bool middlePointerPressReceived;
    private Point selectionStartCanvas;
    private Point selectionStartViewport;
    private Point selectionLastCanvas;
    private Point selectionLastViewport;
    private Point? lastPointerOnRoot;
    private bool isDockPointerHovering;
#if MACCATALYST
    private bool selectionPanPrimed;
#endif
}
