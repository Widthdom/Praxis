namespace Praxis.Core.Logic;

public static class GridSnapper
{
    public static double Snap(double value, int unit = 10)
    {
        if (unit <= 0)
        {
            unit = 10;
        }

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
        var snappedX = Snap(x, unit);
        var snappedY = Snap(y, unit);

        var maxX = Math.Max(0, areaWidth - width);
        var maxY = Math.Max(0, areaHeight - height);

        return (
            Math.Clamp(snappedX, 0, maxX),
            Math.Clamp(snappedY, 0, maxY));
    }
}
