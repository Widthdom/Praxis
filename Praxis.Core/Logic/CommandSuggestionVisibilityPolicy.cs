namespace Praxis.Core.Logic;

public static class CommandSuggestionVisibilityPolicy
{
    public static bool ShouldCloseOnContextMenuOpen(bool isSuggestionOpen)
        => isSuggestionOpen;
}
