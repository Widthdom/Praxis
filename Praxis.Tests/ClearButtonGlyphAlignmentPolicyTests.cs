using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ClearButtonGlyphAlignmentPolicyTests
{
    [Theory]
    [InlineData(true, -0.5)]
    [InlineData(false, 0)]
    public void ResolveTranslation_ReturnsExpectedOffset(bool isWindows, double expected)
    {
        var translation = ClearButtonGlyphAlignmentPolicy.ResolveTranslation(isWindows);
        Assert.Equal(expected, translation);
    }

    [Fact]
    public void ResolveTranslation_IsStableAcrossCalls()
    {
        var first = ClearButtonGlyphAlignmentPolicy.ResolveTranslation(isWindows: true);
        var second = ClearButtonGlyphAlignmentPolicy.ResolveTranslation(isWindows: true);

        Assert.Equal(first, second);
    }
}
