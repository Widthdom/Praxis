namespace Praxis.Core.Logic;

public static class ModalEditorHeightResolver
{
    public static double ResolveHeight(string? text)
    {
        const double singleLineHeight = 40;
        const double maxHeight = 220;
        const double perLineHeight = 24;
        const double basePadding = 16;

        var value = text ?? string.Empty;
        var lineCount = Math.Max(1, value.Count(c => c == '\n') + 1);
        if (lineCount <= 1)
        {
            return singleLineHeight;
        }

        return Math.Min(maxHeight, basePadding + (lineCount * perLineHeight));
    }
}
