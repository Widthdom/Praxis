using Praxis.Core.Logic;

namespace Praxis.Tests;

public class TextCaretPositionResolverTests
{
    [Fact]
    public void ResolveTailOffset_ReturnsZero_WhenTextIsNull()
    {
        var offset = TextCaretPositionResolver.ResolveTailOffset(null);
        Assert.Equal(0, offset);
    }

    [Fact]
    public void ResolveTailOffset_ReturnsZero_WhenTextIsEmpty()
    {
        var offset = TextCaretPositionResolver.ResolveTailOffset(string.Empty);
        Assert.Equal(0, offset);
    }

    [Fact]
    public void ResolveTailOffset_ReturnsLength_ForAsciiText()
    {
        var offset = TextCaretPositionResolver.ResolveTailOffset("command --flag");
        Assert.Equal("command --flag".Length, offset);
    }

    [Fact]
    public void ResolveTailOffset_ReturnsLength_ForMultibyteText()
    {
        var offset = TextCaretPositionResolver.ResolveTailOffset("テスト😀");
        Assert.Equal("テスト😀".Length, offset);
    }
}
