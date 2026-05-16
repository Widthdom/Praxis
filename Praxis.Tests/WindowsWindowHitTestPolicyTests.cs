using Praxis.Core.Logic;

namespace Praxis.Tests;

public class WindowsWindowHitTestPolicyTests
{
    [Theory]
    [InlineData(100, 0)]
    [InlineData(100, 5)]
    public void ResolveTopBandHit_ReturnsTop_ForTopResizeBand(int x, int y)
    {
        var zone = WindowsWindowHitTestPolicy.ResolveTopBandHit(
            localX: x,
            localY: y,
            windowWidth: 1000,
            captionHeight: 32,
            captionButtonsWidth: 108,
            topResizeHeight: 6,
            topCornerWidth: 10);

        Assert.Equal(WindowsWindowHitTestZone.Top, zone);
    }

    [Theory]
    [InlineData(0, WindowsWindowHitTestZone.TopLeft)]
    [InlineData(9, WindowsWindowHitTestZone.TopLeft)]
    [InlineData(990, WindowsWindowHitTestZone.TopRight)]
    [InlineData(999, WindowsWindowHitTestZone.TopRight)]
    public void ResolveTopBandHit_ReturnsCornerZones_AtTopCorners(
        int x,
        WindowsWindowHitTestZone expected)
    {
        var zone = WindowsWindowHitTestPolicy.ResolveTopBandHit(
            localX: x,
            localY: 0,
            windowWidth: 1000,
            captionHeight: 32,
            captionButtonsWidth: 108,
            topResizeHeight: 6,
            topCornerWidth: 10);

        Assert.Equal(expected, zone);
    }

    [Fact]
    public void ResolveTopBandHit_PrioritizesTopResizeOverCaption()
    {
        var zone = WindowsWindowHitTestPolicy.ResolveTopBandHit(
            localX: 100,
            localY: 3,
            windowWidth: 1000,
            captionHeight: 32,
            captionButtonsWidth: 108,
            topResizeHeight: 6,
            topCornerWidth: 10);

        Assert.Equal(WindowsWindowHitTestZone.Top, zone);
    }

    [Fact]
    public void ResolveTopBandHit_ReturnsCaption_BelowResizeBandAndBeforeCaptionButtons()
    {
        var zone = WindowsWindowHitTestPolicy.ResolveTopBandHit(
            localX: 100,
            localY: 10,
            windowWidth: 1000,
            captionHeight: 32,
            captionButtonsWidth: 108,
            topResizeHeight: 6,
            topCornerWidth: 10);

        Assert.Equal(WindowsWindowHitTestZone.Caption, zone);
    }

    [Fact]
    public void ResolveTopBandHit_DoesNotReturnCaption_OverCaptionButtons()
    {
        var zone = WindowsWindowHitTestPolicy.ResolveTopBandHit(
            localX: 940,
            localY: 10,
            windowWidth: 1000,
            captionHeight: 32,
            captionButtonsWidth: 108,
            topResizeHeight: 6,
            topCornerWidth: 10);

        Assert.Equal(WindowsWindowHitTestZone.None, zone);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1000, 0)]
    [InlineData(10, -1)]
    [InlineData(10, 40)]
    public void ResolveTopBandHit_ReturnsNone_OutsideTopBand(int x, int y)
    {
        var zone = WindowsWindowHitTestPolicy.ResolveTopBandHit(
            localX: x,
            localY: y,
            windowWidth: 1000,
            captionHeight: 32,
            captionButtonsWidth: 108,
            topResizeHeight: 6,
            topCornerWidth: 10);

        Assert.Equal(WindowsWindowHitTestZone.None, zone);
    }
}
