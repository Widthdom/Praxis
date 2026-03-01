namespace Praxis.Core.Logic;

public static class WindowActivationCommandFocusPolicy
{
    public static bool ShouldFocusMainCommand(bool isEditorOpen, bool isConflictDialogOpen)
        => !isEditorOpen && !isConflictDialogOpen;
}
