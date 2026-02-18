namespace Praxis.Core.Logic;

public static class EditorTabInsertionResolver
{
    public const char ForwardTabCharacter = '\t';
    public const char BackwardTabCharacter = '\u0019';

    public static bool TryResolveNavigationAction(string? oldText, string? newText, out string action, out int insertedIndex)
    {
        action = string.Empty;
        insertedIndex = -1;
        if (!TryResolveSingleInsertedCharacter(oldText, newText, out insertedIndex, out var insertedChar))
        {
            return false;
        }

        if (insertedChar == ForwardTabCharacter)
        {
            action = EditorShortcutActionResolver.ResolveTabNavigationAction(shiftDown: false);
            return true;
        }

        if (insertedChar == BackwardTabCharacter)
        {
            action = EditorShortcutActionResolver.ResolveTabNavigationAction(shiftDown: true);
            return true;
        }

        insertedIndex = -1;
        return false;
    }

    public static bool TryResolveNavigationAction(string? oldText, string? newText, out string action)
    {
        return TryResolveNavigationAction(oldText, newText, out action, out _);
    }

    private static bool TryResolveSingleInsertedCharacter(string? oldText, string? newText, out int insertedIndex, out char insertedChar)
    {
        oldText ??= string.Empty;
        newText ??= string.Empty;
        insertedIndex = -1;
        insertedChar = '\0';

        if (newText.Length != oldText.Length + 1)
        {
            return false;
        }

        var index = 0;
        while (index < oldText.Length && oldText[index] == newText[index])
        {
            index++;
        }

        insertedIndex = index;
        insertedChar = newText[index];
        return oldText.AsSpan(index).SequenceEqual(newText.AsSpan(index + 1));
    }
}
