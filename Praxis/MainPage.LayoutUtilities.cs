using System.Reflection;
using Praxis.Services;

namespace Praxis;

public partial class MainPage
{
    private static Point GetPositionRelativeToAncestor(VisualElement element, VisualElement ancestor)
    {
        double x = 0;
        double y = 0;

        Element? current = element;
        while (current is VisualElement ve && !ReferenceEquals(ve, ancestor))
        {
            x += ve.X;
            y += ve.Y;
            current = ve.Parent;
        }

        return new Point(x, y);
    }

    private bool IsPointInsideElement(Point p, VisualElement element)
    {
        if (element.Width <= 0 || element.Height <= 0)
        {
            return false;
        }

        var pos = GetPositionRelativeToAncestor(element, RootGrid);
        return p.X >= pos.X &&
               p.X <= pos.X + element.Width &&
               p.Y >= pos.Y &&
               p.Y <= pos.Y + element.Height;
    }

#if WINDOWS
    private static void SetTabStop(VisualElement element, bool isTabStop)
    {
        var platformView = element.Handler?.PlatformView;
        if (platformView is null)
        {
            return;
        }

        var prop = platformView.GetType().GetProperty("IsTabStop", BindingFlags.Public | BindingFlags.Instance);
        if (prop is null || !prop.CanWrite)
        {
            return;
        }

        try
        {
            prop.SetValue(platformView, isTabStop);
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(SetTabStop), $"Failed to set IsTabStop={isTabStop}: {safeMessage}");
        }
    }
#endif
}
