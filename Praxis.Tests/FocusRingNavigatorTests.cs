using Praxis.Core.Logic;

namespace Praxis.Tests;

public class FocusRingNavigatorTests
{
    [Fact]
    public void GetNextIndex_ReturnsMinusOne_WhenItemCountIsZero()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: 0, itemCount: 0, forward: true);
        Assert.Equal(-1, index);
    }

    [Fact]
    public void GetNextIndex_StartsAtFirst_WhenCurrentIndexIsInvalid_AndForward()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: -1, itemCount: 2, forward: true);
        Assert.Equal(0, index);
    }

    [Fact]
    public void GetNextIndex_StartsAtLast_WhenCurrentIndexIsInvalid_AndBackward()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: -1, itemCount: 2, forward: false);
        Assert.Equal(1, index);
    }

    [Fact]
    public void GetNextIndex_WrapsFromLastToFirst_WhenForward()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: 1, itemCount: 2, forward: true);
        Assert.Equal(0, index);
    }

    [Fact]
    public void GetNextIndex_WrapsFromFirstToLast_WhenBackward()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: 0, itemCount: 2, forward: false);
        Assert.Equal(1, index);
    }

    [Fact]
    public void GetNextIndex_ReturnsZero_WhenCurrentIndexIsTooLarge_AndForward()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: 9, itemCount: 2, forward: true);
        Assert.Equal(0, index);
    }

    [Fact]
    public void GetNextIndex_ReturnsLast_WhenCurrentIndexIsTooLarge_AndBackward()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: 9, itemCount: 2, forward: false);
        Assert.Equal(1, index);
    }

    [Fact]
    public void GetNextIndex_StaysAtZero_WhenOnlyOneItemExists_AndForward()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: 0, itemCount: 1, forward: true);
        Assert.Equal(0, index);
    }

    [Fact]
    public void GetNextIndex_StaysAtZero_WhenOnlyOneItemExists_AndBackward()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: 0, itemCount: 1, forward: false);
        Assert.Equal(0, index);
    }
}
