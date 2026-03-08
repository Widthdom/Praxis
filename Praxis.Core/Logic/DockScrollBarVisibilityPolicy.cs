namespace Praxis.Core.Logic;

public static class DockScrollBarVisibilityPolicy
{
    public static bool ShouldShowHorizontalScrollBar(bool isPointerOverDockRegion, bool hasHorizontalOverflow)
        => isPointerOverDockRegion && hasHorizontalOverflow;

    public static bool ShouldShowScrollBarMask(bool showHorizontalScrollBar)
        => !showHorizontalScrollBar;
}
