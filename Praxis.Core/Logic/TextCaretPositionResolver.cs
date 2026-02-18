namespace Praxis.Core.Logic;

public static class TextCaretPositionResolver
{
    // UIKit uses UTF-16 code-unit offsets for text positions.
    public static int ResolveTailOffset(string? text)
    {
        return string.IsNullOrEmpty(text) ? 0 : text.Length;
    }
}
