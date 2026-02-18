namespace Praxis.Core.Logic;

public static class EditorShortcutScopeResolver
{
    public static bool IsEditorShortcutScopeActive(bool isConflictDialogOpen, bool isContextMenuOpen, bool isEditorOpen)
    {
        return isConflictDialogOpen || isContextMenuOpen || isEditorOpen;
    }
}
