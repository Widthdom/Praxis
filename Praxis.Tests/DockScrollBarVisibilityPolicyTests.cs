using Praxis.Core.Logic;

namespace Praxis.Tests;

public class DockScrollBarVisibilityPolicyTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void ShouldShowHorizontalScrollBar_RequiresHoverAndHorizontalOverflow(
        bool isPointerOverDockRegion,
        bool hasHorizontalOverflow,
        bool expected)
    {
        Assert.Equal(expected, DockScrollBarVisibilityPolicy.ShouldShowHorizontalScrollBar(isPointerOverDockRegion, hasHorizontalOverflow));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ShouldShowScrollBarMask_IsInverseOfScrollBarVisibility(bool showHorizontalScrollBar, bool expectedMaskVisible)
    {
        Assert.Equal(expectedMaskVisible, DockScrollBarVisibilityPolicy.ShouldShowScrollBarMask(showHorizontalScrollBar));
    }
}
