namespace Praxis.Core.Logic;

public static class ClearButtonGlyphAlignmentPolicy
{
    private const double WindowsGlyphTranslation = -0.5;

    public static double ResolveTranslation(bool isWindows)
        => isWindows ? WindowsGlyphTranslation : 0;
}
