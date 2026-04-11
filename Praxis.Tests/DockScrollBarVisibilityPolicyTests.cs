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

    [Fact]
    public void ScrollBarMask_RemainsInverseOfVisibility_ForAllPointerAndOverflowCombinations()
    {
        foreach (var isPointerOverDockRegion in new[] { false, true })
        {
            foreach (var hasHorizontalOverflow in new[] { false, true })
            {
                var showScrollBar = DockScrollBarVisibilityPolicy.ShouldShowHorizontalScrollBar(
                    isPointerOverDockRegion,
                    hasHorizontalOverflow);

                var showMask = DockScrollBarVisibilityPolicy.ShouldShowScrollBarMask(showScrollBar);
                Assert.Equal(!showScrollBar, showMask);
            }
        }
    }
}
