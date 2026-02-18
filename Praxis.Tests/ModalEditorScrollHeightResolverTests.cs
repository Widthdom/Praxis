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
}
