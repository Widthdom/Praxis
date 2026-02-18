namespace Praxis.Core.Logic;

public static class ModalEditorScrollHeightResolver
{
    public static double Resolve(double contentHeight, double maxHeight)
    {
        var safeContent = Math.Max(0, contentHeight);
        var safeMax = Math.Max(0, maxHeight);
        if (safeMax == 0)
        {
            return safeContent;
        }

        return Math.Min(safeContent, safeMax);
    }
}
