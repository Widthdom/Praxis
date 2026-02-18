namespace Praxis.Core.Logic;

public static class EditorShortcutActionResolver
{
    public static string ResolveTabNavigationAction(bool shiftDown)
    {
        return shiftDown ? "TabPrevious" : "TabNext";
    }

    public static string ResolveContextMenuArrowNavigationAction(bool downArrow)
    {
        return downArrow ? "ContextMenuNext" : "ContextMenuPrevious";
    }

    public static string ResolveConflictDialogArrowNavigationAction(bool rightArrow)
    {
        return rightArrow ? "ConflictDialogNext" : "ConflictDialogPrevious";
    }
}
