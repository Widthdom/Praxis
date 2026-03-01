using Praxis.Core.Logic;

namespace Praxis.Tests;

public class WindowActivationCommandFocusPolicyTests
{
    [Fact]
    public void ShouldFocusMainCommand_ReturnsTrue_WhenEditorAndConflictAreClosed()
    {
        var shouldFocus = WindowActivationCommandFocusPolicy.ShouldFocusMainCommand(
            isEditorOpen: false,
            isConflictDialogOpen: false);
        Assert.True(shouldFocus);
    }

    [Fact]
    public void ShouldFocusMainCommand_ReturnsFalse_WhenEditorIsOpen()
    {
        var shouldFocus = WindowActivationCommandFocusPolicy.ShouldFocusMainCommand(
            isEditorOpen: true,
            isConflictDialogOpen: false);
        Assert.False(shouldFocus);
    }

    [Fact]
    public void ShouldFocusMainCommand_ReturnsFalse_WhenConflictDialogIsOpen()
    {
        var shouldFocus = WindowActivationCommandFocusPolicy.ShouldFocusMainCommand(
            isEditorOpen: false,
            isConflictDialogOpen: true);
        Assert.False(shouldFocus);
    }
}
