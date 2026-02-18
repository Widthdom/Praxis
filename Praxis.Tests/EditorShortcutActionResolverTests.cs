using Praxis.Core.Logic;

namespace Praxis.Tests;

public class EditorShortcutActionResolverTests
{
    [Fact]
    public void ResolveCancelAction_ReturnsCancel()
    {
        var action = EditorShortcutActionResolver.ResolveCancelAction();
        Assert.Equal("Cancel", action);
    }

    [Fact]
    public void ResolveCancelAction_ReturnsStableValue_OnRepeatedCalls()
    {
        var first = EditorShortcutActionResolver.ResolveCancelAction();
        var second = EditorShortcutActionResolver.ResolveCancelAction();
        Assert.Equal(first, second);
        Assert.False(string.IsNullOrWhiteSpace(first));
    }

    [Fact]
    public void ResolveTabNavigationAction_ReturnsTabNext_WhenShiftIsNotPressed()
    {
        var action = EditorShortcutActionResolver.ResolveTabNavigationAction(shiftDown: false);
        Assert.Equal("TabNext", action);
    }

    [Fact]
    public void ResolveTabNavigationAction_ReturnsTabPrevious_WhenShiftIsPressed()
    {
        var action = EditorShortcutActionResolver.ResolveTabNavigationAction(shiftDown: true);
        Assert.Equal("TabPrevious", action);
    }

    [Fact]
    public void ResolveContextMenuArrowNavigationAction_ReturnsContextMenuPrevious_WhenUpArrow()
    {
        var action = EditorShortcutActionResolver.ResolveContextMenuArrowNavigationAction(downArrow: false);
        Assert.Equal("ContextMenuPrevious", action);
    }

    [Fact]
    public void ResolveContextMenuArrowNavigationAction_ReturnsContextMenuNext_WhenDownArrow()
    {
        var action = EditorShortcutActionResolver.ResolveContextMenuArrowNavigationAction(downArrow: true);
        Assert.Equal("ContextMenuNext", action);
    }

    [Fact]
    public void ResolveConflictDialogArrowNavigationAction_ReturnsConflictDialogPrevious_WhenLeftArrow()
    {
        var action = EditorShortcutActionResolver.ResolveConflictDialogArrowNavigationAction(rightArrow: false);
        Assert.Equal("ConflictDialogPrevious", action);
    }

    [Fact]
    public void ResolveConflictDialogArrowNavigationAction_ReturnsConflictDialogNext_WhenRightArrow()
    {
        var action = EditorShortcutActionResolver.ResolveConflictDialogArrowNavigationAction(rightArrow: true);
        Assert.Equal("ConflictDialogNext", action);
    }
}
