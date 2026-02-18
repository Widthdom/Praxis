using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ModalEditorHeightResolverTests
{
    [Fact]
    public void ResolveHeight_ReturnsSingleLineHeight_WhenTextIsNull()
    {
        var height = ModalEditorHeightResolver.ResolveHeight(null);
        Assert.Equal(40, height);
    }

    [Fact]
    public void ResolveHeight_ReturnsSingleLineHeight_WhenTextHasNoLineBreak()
    {
        var height = ModalEditorHeightResolver.ResolveHeight("single line");
        Assert.Equal(40, height);
    }

    [Fact]
    public void ResolveHeight_ReturnsExpandedHeight_WhenTextHasMultipleLines()
    {
        var height = ModalEditorHeightResolver.ResolveHeight("line1\nline2\nline3");
        Assert.Equal(88, height);
    }

    [Fact]
    public void ResolveHeight_ClampsAtMaxHeight_WhenTextHasManyLines()
    {
        var height = ModalEditorHeightResolver.ResolveHeight(string.Join('\n', Enumerable.Repeat("line", 20)));
        Assert.Equal(220, height);
    }
}
