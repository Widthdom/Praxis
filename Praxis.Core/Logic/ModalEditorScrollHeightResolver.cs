namespace Praxis.Core.Logic;

public static class ModalEditorScrollHeightResolver
{
    public static double Resolve(double contentHeight, double maxHeight)
    {
        var safeContent = Math.Max(0, double.IsFinite(contentHeight) ? contentHeight : 0);
        var safeMax = Math.Max(0, double.IsFinite(maxHeight) ? maxHeight : 0);
        if (safeMax == 0)
        {
            return safeContent;
        }

        return Math.Min(safeContent, safeMax);
    }
}
