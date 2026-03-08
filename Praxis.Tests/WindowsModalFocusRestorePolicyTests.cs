using Praxis.Core.Logic;

namespace Praxis.Tests;

public class WindowsModalFocusRestorePolicyTests
{
    [Fact]
    public void ShouldRestoreEditorFocus_ReturnsTrue_WhenWindowsEditorIsOpenAndFocusIsLost()
    {
        var restore = WindowsModalFocusRestorePolicy.ShouldRestoreEditorFocus(
            isWindows: true,
            isEditorOpen: true,
            isConflictDialogOpen: false,
            hasEditorFocus: false);

        Assert.True(restore);
    }

    [Fact]
    public void ShouldRestoreEditorFocus_ReturnsFalse_WhenEditorStillHasFocus()
    {
        var restore = WindowsModalFocusRestorePolicy.ShouldRestoreEditorFocus(
            isWindows: true,
            isEditorOpen: true,
            isConflictDialogOpen: false,
            hasEditorFocus: true);

        Assert.False(restore);
    }

    [Fact]
    public void ShouldRestoreEditorFocus_ReturnsFalse_WhenNotWindows()
    {
        var restore = WindowsModalFocusRestorePolicy.ShouldRestoreEditorFocus(
            isWindows: false,
            isEditorOpen: true,
            isConflictDialogOpen: false,
            hasEditorFocus: false);

        Assert.False(restore);
    }

    [Fact]
    public void ShouldRestoreConflictDialogFocus_ReturnsTrue_WhenWindowsConflictDialogIsOpenAndNoButtonFocused()
    {
        var restore = WindowsModalFocusRestorePolicy.ShouldRestoreConflictDialogFocus(
            isWindows: true,
            isConflictDialogOpen: true,
            hasConflictButtonFocus: false);

        Assert.True(restore);
    }

    [Fact]
    public void ShouldRestoreConflictDialogFocus_ReturnsFalse_WhenConflictButtonStillFocused()
    {
        var restore = WindowsModalFocusRestorePolicy.ShouldRestoreConflictDialogFocus(
            isWindows: true,
            isConflictDialogOpen: true,
            hasConflictButtonFocus: true);

        Assert.False(restore);
    }

    [Fact]
    public void ShouldRestoreConflictDialogFocus_ReturnsFalse_WhenDialogIsClosed()
    {
        var restore = WindowsModalFocusRestorePolicy.ShouldRestoreConflictDialogFocus(
            isWindows: true,
            isConflictDialogOpen: false,
            hasConflictButtonFocus: false);

        Assert.False(restore);
    }
}
