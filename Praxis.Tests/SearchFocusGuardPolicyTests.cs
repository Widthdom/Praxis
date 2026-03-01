using Praxis.Core.Logic;

namespace Praxis.Tests;

public class SearchFocusGuardPolicyTests
{
    [Fact]
    public void ShouldAllowSearchFocus_ReturnsFalse_WhenMainCommandShouldStayFocused_AndNotUserInitiated()
    {
        var allowed = SearchFocusGuardPolicy.ShouldAllowSearchFocus(
            shouldFocusMainCommand: true,
            isAppForeground: true,
            isUserInitiated: false);

        Assert.False(allowed);
    }

    [Fact]
    public void ShouldAllowSearchFocus_ReturnsTrue_WhenUserInitiated()
    {
        var allowed = SearchFocusGuardPolicy.ShouldAllowSearchFocus(
            shouldFocusMainCommand: true,
            isAppForeground: true,
            isUserInitiated: true);

        Assert.True(allowed);
    }

    [Fact]
    public void ShouldAllowSearchFocus_ReturnsTrue_WhenMainCommandPolicyIsInactive()
    {
        var allowed = SearchFocusGuardPolicy.ShouldAllowSearchFocus(
            shouldFocusMainCommand: false,
            isAppForeground: true,
            isUserInitiated: false);

        Assert.True(allowed);
    }

    [Fact]
    public void ShouldAllowSearchFocus_ReturnsTrue_WhenAppIsNotForeground()
    {
        var allowed = SearchFocusGuardPolicy.ShouldAllowSearchFocus(
            shouldFocusMainCommand: true,
            isAppForeground: false,
            isUserInitiated: false);

        Assert.True(allowed);
    }
}
