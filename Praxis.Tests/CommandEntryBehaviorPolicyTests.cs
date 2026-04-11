using Praxis.Core.Logic;

namespace Praxis.Tests;

public class CommandEntryBehaviorPolicyTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldHandleCommandNavigationShortcuts_ReturnsInputValue(bool enabled)
    {
        Assert.Equal(enabled, CommandEntryBehaviorPolicy.ShouldHandleCommandNavigationShortcuts(enabled));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldTrackActivationFocusTarget_ReturnsInputValue(bool enabled)
    {
        Assert.Equal(enabled, CommandEntryBehaviorPolicy.ShouldTrackActivationFocusTarget(enabled));
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

    [Fact]
    public void ShouldApplyNativeActivationFocus_RequiresEnabledFeatureAndClosedOverlays_ForAllCombinations()
    {
        foreach (var enableNativeActivationFocus in new[] { false, true })
        {
            foreach (var isEditorOpen in new[] { false, true })
            {
                foreach (var isConflictDialogOpen in new[] { false, true })
                {
                    var actual = CommandEntryBehaviorPolicy.ShouldApplyNativeActivationFocus(
                        enableNativeActivationFocus,
                        isEditorOpen,
                        isConflictDialogOpen);

                    var expected = enableNativeActivationFocus && !isEditorOpen && !isConflictDialogOpen;
                    Assert.Equal(expected, actual);
                }
            }
        }
    }
}
