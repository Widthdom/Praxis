using Praxis.Core.Logic;

namespace Praxis.Tests;

public class DockScrollBarVisibilityPolicyTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void ShouldShowHorizontalScrollBar_MatchesPointerHoverState(bool isPointerOverDockRegion, bool expected)
    {
        Assert.Equal(expected, DockScrollBarVisibilityPolicy.ShouldShowHorizontalScrollBar(isPointerOverDockRegion));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ShouldShowScrollBarMask_IsInverseOfScrollBarVisibility(bool showHorizontalScrollBar, bool expectedMaskVisible)
    {
        Assert.Equal(expectedMaskVisible, DockScrollBarVisibilityPolicy.ShouldShowScrollBarMask(showHorizontalScrollBar));
    }
}
