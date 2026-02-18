namespace Praxis.Core.Logic;

public static class ConflictDialogFocusRestorePolicy
{
    public static bool ShouldRestoreEditorFocus(bool isEditorOpen, bool isConflictDialogOpen)
        => isEditorOpen && !isConflictDialogOpen;
}
