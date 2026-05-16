using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public readonly record struct PlacementSurfaceExtent(double Width, double Height);

public static class PlacementSurfaceExtentPolicy
{
    public static PlacementSurfaceExtent Resolve(
        IEnumerable<LauncherButtonModel> buttons,
        double viewportWidth,
        double viewportHeight)
    {
        ArgumentNullException.ThrowIfNull(buttons);

        var width = Math.Max(0, NormalizeFinite(viewportWidth));
        var height = Math.Max(0, NormalizeFinite(viewportHeight));

        foreach (var button in buttons)
        {
            var right = Math.Max(0, NormalizeFinite(button.X))
                + Math.Max(0, NormalizeFinite(button.Width));
            var bottom = Math.Max(0, NormalizeFinite(button.Y))
                + Math.Max(0, NormalizeFinite(button.Height));

            width = Math.Max(width, right);
            height = Math.Max(height, bottom);
        }

        return new PlacementSurfaceExtent(width, height);
    }

    private static double NormalizeFinite(double value)
        => double.IsFinite(value) ? value : 0;
}
