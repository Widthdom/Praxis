using Praxis.Core.Logic;

namespace Praxis.Tests;

public class AsciiInputFilterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("abc123", false)]
    [InlineData("ABC xyz", false)]
    [InlineData("あ", true)]
    [InlineData("abcあ", true)]
    public void ShouldBlockMarkedText_ReturnsExpectedValue(string? markedText, bool expected)
    {
        Assert.Equal(expected, AsciiInputFilter.ShouldBlockMarkedText(markedText));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("command", true)]
    [InlineData("a1-_ ", true)]
    [InlineData("漢字", false)]
    [InlineData("x漢", false)]
    public void IsAsciiOnly_ReturnsExpectedValue(string? value, bool expected)
    {
        Assert.Equal(expected, AsciiInputFilter.IsAsciiOnly(value));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData("aあbいc", "abc")]
    [InlineData("１２３ABC", "ABC")]
    [InlineData("日本語", "")]
    public void FilterToAscii_ReturnsAsciiOnlyText(string? value, string expected)
    {
        Assert.Equal(expected, AsciiInputFilter.FilterToAscii(value));
    }
}
