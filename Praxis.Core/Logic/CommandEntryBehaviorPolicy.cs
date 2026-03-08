namespace Praxis.Core.Logic;

public static class CommandEntryBehaviorPolicy
{
    public static bool ShouldHandleCommandNavigationShortcuts(bool enableCommandNavigationShortcuts)
        => enableCommandNavigationShortcuts;

    public static bool ShouldTrackActivationFocusTarget(bool enableNativeActivationFocus)
        => enableNativeActivationFocus;

    public static bool ShouldApplyNativeActivationFocus(
        bool enableNativeActivationFocus,
        bool isEditorOpen,
        bool isConflictDialogOpen)
        => enableNativeActivationFocus && !isEditorOpen && !isConflictDialogOpen;
}
