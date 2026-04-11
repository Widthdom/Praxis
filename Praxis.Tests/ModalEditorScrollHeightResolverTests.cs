using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ModalEditorScrollHeightResolverTests
{
    [Fact]
    public void Resolve_ReturnsContentHeight_WhenContentIsBelowMax()
    {
        var height = ModalEditorScrollHeightResolver.Resolve(contentHeight: 180, maxHeight: 300);
        Assert.Equal(180, height);
    }

    [Fact]
    public void Resolve_ReturnsMaxHeight_WhenContentExceedsMax()
    {
        var height = ModalEditorScrollHeightResolver.Resolve(contentHeight: 420, maxHeight: 300);
        Assert.Equal(300, height);
    }

    [Fact]
    public void Resolve_ReturnsCollapsedHeight_WhenContentShrinksAfterExpansion()
    {
        var expanded = ModalEditorScrollHeightResolver.Resolve(contentHeight: 420, maxHeight: 300);
        var collapsed = ModalEditorScrollHeightResolver.Resolve(contentHeight: 128, maxHeight: 300);

        Assert.Equal(300, expanded);
        Assert.Equal(128, collapsed);
    }

    [Fact]
    public void Resolve_ReturnsZero_WhenBothContentAndMaxAreNegative()
    {
        var height = ModalEditorScrollHeightResolver.Resolve(contentHeight: -20, maxHeight: -1);
        Assert.Equal(0, height);
    }

    [Fact]
    public void Resolve_ReturnsContentHeight_WhenMaxHeightIsZeroOrNegative()
    {
        Assert.Equal(180, ModalEditorScrollHeightResolver.Resolve(contentHeight: 180, maxHeight: 0));
        Assert.Equal(180, ModalEditorScrollHeightResolver.Resolve(contentHeight: 180, maxHeight: -1));
    }

    [Fact]
    public void Resolve_TreatsOnlyNonFiniteContent_AsZero_BeforeApplyingFiniteMax()
    {
        Assert.Equal(0, ModalEditorScrollHeightResolver.Resolve(double.NaN, 200));
        Assert.Equal(0, ModalEditorScrollHeightResolver.Resolve(double.PositiveInfinity, 200));
    }

    [Fact]
    public void Resolve_TreatsOnlyNonFiniteMax_AsUnbounded()
    {
        Assert.Equal(120, ModalEditorScrollHeightResolver.Resolve(120, double.NaN));
        Assert.Equal(120, ModalEditorScrollHeightResolver.Resolve(120, double.PositiveInfinity));
    }

    [Fact]
    public void Resolve_TreatsNonFiniteInputs_AsZero()
    {
        Assert.Equal(0, ModalEditorScrollHeightResolver.Resolve(double.NaN, double.NaN));
        Assert.Equal(0, ModalEditorScrollHeightResolver.Resolve(double.PositiveInfinity, double.NegativeInfinity));
    }
}
