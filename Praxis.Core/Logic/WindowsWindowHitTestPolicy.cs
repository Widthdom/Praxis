namespace Praxis.Core.Logic;

public enum WindowsWindowHitTestZone
{
    None,
    Caption,
    Top,
    TopLeft,
    TopRight,
}

public static class WindowsWindowHitTestPolicy
{
    public static WindowsWindowHitTestZone ResolveTopBandHit(
        int localX,
        int localY,
        int windowWidth,
        int captionHeight,
        int captionButtonsWidth,
        int topResizeHeight,
        int topCornerWidth)
    {
        if (windowWidth <= 0
            || localX < 0
            || localX >= windowWidth
            || localY < 0)
        {
            return WindowsWindowHitTestZone.None;
        }

        var safeResizeHeight = Math.Max(0, topResizeHeight);
        var safeCornerWidth = Math.Max(0, topCornerWidth);
        if (localY < safeResizeHeight)
        {
            if (localX < safeCornerWidth)
            {
                return WindowsWindowHitTestZone.TopLeft;
            }

            if (localX >= windowWidth - safeCornerWidth)
            {
                return WindowsWindowHitTestZone.TopRight;
            }

            return WindowsWindowHitTestZone.Top;
        }

        var safeCaptionHeight = Math.Max(0, captionHeight);
        var captionRight = Math.Max(0, windowWidth - Math.Max(0, captionButtonsWidth));
        if (localY < safeCaptionHeight && localX < captionRight)
        {
            return WindowsWindowHitTestZone.Caption;
        }

        return WindowsWindowHitTestZone.None;
    }
}
