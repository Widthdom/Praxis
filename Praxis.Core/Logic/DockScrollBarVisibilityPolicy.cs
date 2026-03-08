namespace Praxis.Core.Logic;

public static class DockScrollBarVisibilityPolicy
{
    public static bool ShouldShowHorizontalScrollBar(bool isPointerOverDockRegion)
        => isPointerOverDockRegion;

    public static bool ShouldShowScrollBarMask(bool showHorizontalScrollBar)
        => !showHorizontalScrollBar;
}
