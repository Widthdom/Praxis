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
    public void GetNextIndex_ReturnsMinusOne_WhenItemCountIsNegative()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: 0, itemCount: -3, forward: false);
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

    [Fact]
    public void GetNextIndex_AdvancesInThreeItemRing_WhenForward()
    {
        // Conflict dialog order: Reload(0) -> Overwrite(1) -> Cancel(2) -> Reload(0)
        var fromReload = FocusRingNavigator.GetNextIndex(currentIndex: 0, itemCount: 3, forward: true);
        var fromOverwrite = FocusRingNavigator.GetNextIndex(currentIndex: 1, itemCount: 3, forward: true);
        var fromCancel = FocusRingNavigator.GetNextIndex(currentIndex: 2, itemCount: 3, forward: true);

        Assert.Equal(1, fromReload);
        Assert.Equal(2, fromOverwrite);
        Assert.Equal(0, fromCancel);
    }

    [Fact]
    public void GetNextIndex_RewindsInThreeItemRing_WhenBackward()
    {
        // Conflict dialog reverse order: Reload(0) <- Overwrite(1) <- Cancel(2) <- Reload(0)
        var fromReload = FocusRingNavigator.GetNextIndex(currentIndex: 0, itemCount: 3, forward: false);
        var fromOverwrite = FocusRingNavigator.GetNextIndex(currentIndex: 1, itemCount: 3, forward: false);
        var fromCancel = FocusRingNavigator.GetNextIndex(currentIndex: 2, itemCount: 3, forward: false);

        Assert.Equal(2, fromReload);
        Assert.Equal(0, fromOverwrite);
        Assert.Equal(1, fromCancel);
    }

    [Fact]
    public void GetNextIndex_StartsAtFirst_WhenCurrentIndexIsInvalid_InThreeItemRing_AndForward()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: -1, itemCount: 3, forward: true);
        Assert.Equal(0, index);
    }

    [Fact]
    public void GetNextIndex_StartsAtLast_WhenCurrentIndexIsInvalid_InThreeItemRing_AndBackward()
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex: -1, itemCount: 3, forward: false);
        Assert.Equal(2, index);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 0)]
    public void GetNextIndex_AdvancesAcrossFourItemRing_WhenForward(int currentIndex, int expected)
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex, itemCount: 4, forward: true);
        Assert.Equal(expected, index);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public void GetNextIndex_RewindsAcrossFourItemRing_WhenBackward(int currentIndex, int expected)
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex, itemCount: 4, forward: false);
        Assert.Equal(expected, index);
    }

    [Theory]
    [InlineData(-5, true, 0)]
    [InlineData(-5, false, 3)]
    [InlineData(9, true, 0)]
    [InlineData(9, false, 3)]
    public void GetNextIndex_NormalizesFarOutOfRangeIndexes(int currentIndex, bool forward, int expected)
    {
        var index = FocusRingNavigator.GetNextIndex(currentIndex, itemCount: 4, forward);
        Assert.Equal(expected, index);
    }
}
