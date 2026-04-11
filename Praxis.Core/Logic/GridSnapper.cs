namespace Praxis.Core.Logic;

public static class GridSnapper
{
    public static double Snap(double value, int unit = 10)
    {
        if (unit <= 0)
        {
            unit = 10;
        }

        value = NormalizeFinite(value);
        return Math.Round(value / unit, MidpointRounding.AwayFromZero) * unit;
    }

    public static (double X, double Y) ClampWithinArea(
        double x,
        double y,
        double width,
        double height,
        double areaWidth,
        double areaHeight,
        int unit = 10)
    {
        var safeWidth = Math.Max(0, NormalizeFinite(width));
        var safeHeight = Math.Max(0, NormalizeFinite(height));
        var safeAreaWidth = Math.Max(0, NormalizeFinite(areaWidth));
        var safeAreaHeight = Math.Max(0, NormalizeFinite(areaHeight));
        var snappedX = Snap(x, unit);
        var snappedY = Snap(y, unit);

        var maxX = Math.Max(0, safeAreaWidth - safeWidth);
        var maxY = Math.Max(0, safeAreaHeight - safeHeight);

        return (
            Math.Clamp(snappedX, 0, maxX),
            Math.Clamp(snappedY, 0, maxY));
    }

    private static double NormalizeFinite(double value)
        => double.IsFinite(value) ? value : 0;
}
