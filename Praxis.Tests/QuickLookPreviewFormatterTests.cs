using Praxis.Core.Logic;

namespace Praxis.Tests;

public class QuickLookPreviewFormatterTests
{
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

        Assert.Equal("abcdefghi...", QuickLookPreviewFormatter.FormatValue(value, maxLength: 10));
    }

    [Fact]
    public void BuildLine_ComposesLabelAndFormattedValue()
    {
        var line = QuickLookPreviewFormatter.BuildLine("Tool", "  git  ");

        Assert.Equal("Tool: git", line);
    }
}
