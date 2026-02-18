using Praxis.Core.Logic;

namespace Praxis.Tests;

public class EditorShortcutActionResolverTests
{
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
}
