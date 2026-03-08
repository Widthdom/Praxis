using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ClearButtonGlyphAlignmentPolicyTests
{
    [Fact]
    public void ResolveTranslation_ReturnsHalfPixelOffset_ForWindows()
    {
        var translation = ClearButtonGlyphAlignmentPolicy.ResolveTranslation(isWindows: true);

        Assert.Equal(-0.5, translation);
    }

    [Fact]
    public void ResolveTranslation_ReturnsNoOffset_ForNonWindows()
    {
        var translation = ClearButtonGlyphAlignmentPolicy.ResolveTranslation(isWindows: false);

        Assert.Equal(0, translation);
    }
}
