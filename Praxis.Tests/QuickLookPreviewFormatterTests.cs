using Praxis.Core.Logic;

namespace Praxis.Tests;

public class QuickLookPreviewFormatterTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FormatValue_ThrowsOutOfRange_WhenMaxLengthIsNotPositive(int maxLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QuickLookPreviewFormatter.FormatValue("value", maxLength));
    }

    [Fact]
    public void FormatValue_ReturnsPlaceholder_ForNullOrWhitespace()
    {
        Assert.Equal("-", QuickLookPreviewFormatter.FormatValue(null));
        Assert.Equal("-", QuickLookPreviewFormatter.FormatValue("   "));
    }

    [Fact]
    public void FormatValue_NormalizesWhitespaceAndNewlines()
    {
        var value = "  alpha\n beta\t gamma  ";

        Assert.Equal("alpha beta gamma", QuickLookPreviewFormatter.FormatValue(value));
    }

    [Fact]
    public void FormatValue_TruncatesWithEllipsis_WhenTooLong()
    {
        var value = "abcdefghijklmnopqrstuvwxyz";

        Assert.Equal("abcdefg...", QuickLookPreviewFormatter.FormatValue(value, maxLength: 10));
    }

    [Fact]
    public void FormatValue_KeepsTruncationWithinRequestedMaxLength()
    {
        var value = "abcdefghijklmnopqrstuvwxyz";

        Assert.Equal(".", QuickLookPreviewFormatter.FormatValue(value, maxLength: 1));
        Assert.Equal("..", QuickLookPreviewFormatter.FormatValue(value, maxLength: 2));
        Assert.Equal("...", QuickLookPreviewFormatter.FormatValue(value, maxLength: 3));
    }

    [Fact]
    public void FormatValue_DoesNotTruncate_WhenNormalizedLengthMatchesMaxLength()
    {
        var value = "  alpha\nbeta  ";

        Assert.Equal("alpha beta", QuickLookPreviewFormatter.FormatValue(value, maxLength: 10));
    }

    [Fact]
    public void BuildLine_ThrowsArgumentNullException_ForNullLabel()
    {
        Assert.Throws<ArgumentNullException>(() => QuickLookPreviewFormatter.BuildLine(null!, "git"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildLine_ThrowsArgumentException_ForWhitespaceLabel(string label)
    {
        Assert.Throws<ArgumentException>(() => QuickLookPreviewFormatter.BuildLine(label, "git"));
    }

    [Fact]
    public void BuildLine_ComposesLabelAndFormattedValue()
    {
        var line = QuickLookPreviewFormatter.BuildLine("Tool", "  git  ");

        Assert.Equal("Tool: git", line);
    }

    [Fact]
    public void BuildLine_KeepsEntireLineWithinRequestedMaxLength()
    {
        var line = QuickLookPreviewFormatter.BuildLine("Tool", "abcdefghijklmnopqrstuvwxyz", maxLength: 10);

        Assert.Equal("Tool: a...", line);
        Assert.Equal(10, line.Length);
    }

    [Fact]
    public void BuildLine_TruncatesPrefix_WhenMaxLengthIsShorterThanLabelPrefix()
    {
        var line = QuickLookPreviewFormatter.BuildLine("Tool", "git", maxLength: 3);

        Assert.Equal("Too", line);
    }
}
