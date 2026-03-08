namespace Praxis.Core.Logic;

public static class WindowsModalFocusRestorePolicy
{
    public static bool ShouldRestoreEditorFocus(
        bool isWindows,
        bool isEditorOpen,
        bool isConflictDialogOpen,
        bool hasEditorFocus)
    {
        return isWindows && isEditorOpen && !isConflictDialogOpen && !hasEditorFocus;
    }

    public static bool ShouldRestoreConflictDialogFocus(
        bool isWindows,
        bool isConflictDialogOpen,
        bool hasConflictButtonFocus)
    {
        return isWindows && isConflictDialogOpen && !hasConflictButtonFocus;
    }
}
