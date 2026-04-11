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
    public void ResolveHeight_ReturnsExpandedHeight_WhenTextUsesWindowsLineEndings()
    {
        var height = ModalEditorHeightResolver.ResolveHeight("line1\r\nline2\r\nline3");
        Assert.Equal(88, height);
    }

    [Fact]
    public void ResolveHeight_ReturnsExpandedHeight_WhenTextUsesCarriageReturnOnlyLineEndings()
    {
        var height = ModalEditorHeightResolver.ResolveHeight("line1\rline2\rline3");
        Assert.Equal(88, height);
    }

    [Fact]
    public void ResolveHeight_CountsTrailingLineFeed_AsAdditionalLine()
    {
        var height = ModalEditorHeightResolver.ResolveHeight("line1\n");
        Assert.Equal(64, height);
    }

    [Fact]
    public void ResolveHeight_CountsTrailingCarriageReturn_AsAdditionalLine()
    {
        var height = ModalEditorHeightResolver.ResolveHeight("line1\r");
        Assert.Equal(64, height);
    }

    [Fact]
    public void ResolveHeight_ReturnsExpandedHeight_WhenTextUsesMixedLineEndings()
    {
        var height = ModalEditorHeightResolver.ResolveHeight("line1\r\nline2\rline3\nline4");
        Assert.Equal(112, height);
    }

    [Fact]
    public void ResolveHeight_CountsTrailingCrLf_AsSingleAdditionalLine()
    {
        var height = ModalEditorHeightResolver.ResolveHeight("line1\r\n");
        Assert.Equal(64, height);
    }

    [Fact]
    public void ResolveHeight_PreservesBlankLines_WhenCalculatingHeight()
    {
        var height = ModalEditorHeightResolver.ResolveHeight("line1\n\nline3");
        Assert.Equal(88, height);
    }

    [Fact]
    public void ResolveHeight_ClampsAtMaxHeight_WhenTextHasManyLines()
    {
        var height = ModalEditorHeightResolver.ResolveHeight(string.Join('\n', Enumerable.Repeat("line", 20)));
        Assert.Equal(220, height);
    }

    [Fact]
    public void ResolveHeight_ReturnsSingleLineHeight_AfterPreviouslyMaxHeightTextIsCleared()
    {
        var expanded = ModalEditorHeightResolver.ResolveHeight(string.Join('\n', Enumerable.Repeat("line", 20)));
        var collapsed = ModalEditorHeightResolver.ResolveHeight(string.Empty);

        Assert.Equal(220, expanded);
        Assert.Equal(40, collapsed);
    }
}
