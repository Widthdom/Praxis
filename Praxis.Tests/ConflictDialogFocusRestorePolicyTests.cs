using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ConflictDialogFocusRestorePolicyTests
{
    [Fact]
    public void ShouldRestoreEditorFocus_ReturnsTrue_WhenEditorIsOpenAndConflictDialogIsClosed()
    {
        var restore = ConflictDialogFocusRestorePolicy.ShouldRestoreEditorFocus(
            isEditorOpen: true,
            isConflictDialogOpen: false);
        Assert.True(restore);
    }

    [Fact]
    public void ShouldRestoreEditorFocus_ReturnsFalse_WhenEditorIsClosed()
    {
        var restore = ConflictDialogFocusRestorePolicy.ShouldRestoreEditorFocus(
            isEditorOpen: false,
            isConflictDialogOpen: false);
        Assert.False(restore);
    }

    [Fact]
    public void ShouldRestoreEditorFocus_ReturnsFalse_WhenConflictDialogIsStillOpen()
    {
        var restore = ConflictDialogFocusRestorePolicy.ShouldRestoreEditorFocus(
            isEditorOpen: true,
            isConflictDialogOpen: true);
        Assert.False(restore);
    }
}
