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
        var lineCount = Math.Max(1, CountLines(value));
        if (lineCount <= 1)
        {
            return singleLineHeight;
        }

        return Math.Min(maxHeight, basePadding + (lineCount * perLineHeight));
    }

    private static int CountLines(string text)
    {
        if (text.Length == 0)
        {
            return 1;
        }

        var lines = 1;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\n')
            {
                lines++;
                continue;
            }

            if (c != '\r')
            {
                continue;
            }

            lines++;
            if (i + 1 < text.Length && text[i + 1] == '\n')
            {
                i++;
            }
        }

        return lines;
    }
}
