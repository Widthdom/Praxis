using Praxis.Core.Logic;

namespace Praxis.Tests;

public class PolicyTruthTableTests
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public void WindowActivationCommandFocusPolicy_MatchesExpectedTruthTable(
        bool isEditorOpen,
        bool isConflictDialogOpen,
        bool expected)
    {
        var actual = WindowActivationCommandFocusPolicy.ShouldFocusMainCommand(isEditorOpen, isConflictDialogOpen);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void ConflictDialogFocusRestorePolicy_MatchesExpectedTruthTable(
        bool isEditorOpen,
        bool isConflictDialogOpen,
        bool expected)
    {
        var actual = ConflictDialogFocusRestorePolicy.ShouldRestoreEditorFocus(isEditorOpen, isConflictDialogOpen);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, false, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void SearchFocusGuardPolicy_MatchesExpectedTruthTable(
        bool shouldFocusMainCommand,
        bool isAppForeground,
        bool isUserInitiated,
        bool expected)
    {
        var actual = SearchFocusGuardPolicy.ShouldAllowSearchFocus(
            shouldFocusMainCommand,
            isAppForeground,
            isUserInitiated);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, false, true, true, false)]
    [InlineData(false, true, false, false, false)]
    [InlineData(false, true, false, true, false)]
    [InlineData(false, true, true, false, false)]
    [InlineData(false, true, true, true, false)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, false, false, true, false)]
    [InlineData(true, false, true, false, false)]
    [InlineData(true, false, true, true, false)]
    [InlineData(true, true, false, false, true)]
    [InlineData(true, true, false, true, false)]
    [InlineData(true, true, true, false, false)]
    [InlineData(true, true, true, true, false)]
    public void WindowsModalFocusRestorePolicy_EditorFocus_MatchesExpectedTruthTable(
        bool isWindows,
        bool isEditorOpen,
        bool isConflictDialogOpen,
        bool hasEditorFocus,
        bool expected)
    {
        var actual = WindowsModalFocusRestorePolicy.ShouldRestoreEditorFocus(
            isWindows,
            isEditorOpen,
            isConflictDialogOpen,
            hasEditorFocus);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void WindowsModalFocusRestorePolicy_ConflictFocus_MatchesExpectedTruthTable(
        bool isWindows,
        bool isConflictDialogOpen,
        bool hasConflictButtonFocus,
        bool expected)
    {
        var actual = WindowsModalFocusRestorePolicy.ShouldRestoreConflictDialogFocus(
            isWindows,
            isConflictDialogOpen,
            hasConflictButtonFocus);

        Assert.Equal(expected, actual);
    }
}
