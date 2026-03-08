using Praxis.Core.Logic;

namespace Praxis.Tests;

public class CommandEntryBehaviorPolicyTests
{
    [Fact]
    public void ShouldHandleCommandNavigationShortcuts_ReturnsInputValue()
    {
        Assert.True(CommandEntryBehaviorPolicy.ShouldHandleCommandNavigationShortcuts(enableCommandNavigationShortcuts: true));
        Assert.False(CommandEntryBehaviorPolicy.ShouldHandleCommandNavigationShortcuts(enableCommandNavigationShortcuts: false));
    }

    [Fact]
    public void ShouldTrackActivationFocusTarget_ReturnsInputValue()
    {
        Assert.True(CommandEntryBehaviorPolicy.ShouldTrackActivationFocusTarget(enableNativeActivationFocus: true));
        Assert.False(CommandEntryBehaviorPolicy.ShouldTrackActivationFocusTarget(enableNativeActivationFocus: false));
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, false)]
    public void ShouldApplyNativeActivationFocus_MatchesExpected(
        bool enableNativeActivationFocus,
        bool isEditorOpen,
        bool isConflictDialogOpen,
        bool expected)
    {
        var actual = CommandEntryBehaviorPolicy.ShouldApplyNativeActivationFocus(
            enableNativeActivationFocus,
            isEditorOpen,
            isConflictDialogOpen);

        Assert.Equal(expected, actual);
    }
}
