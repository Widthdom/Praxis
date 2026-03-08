using Praxis.Core.Logic;

namespace Praxis.Tests;

public class MacCommandInputSourcePolicyTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void ShouldForceAsciiInputSource_MatchesExpectedTruthTable(
        bool isFirstResponder,
        bool isWindowKey,
        bool isAppActive,
        bool expected)
    {
        var actual = MacCommandInputSourcePolicy.ShouldForceAsciiInputSource(
            isFirstResponder,
            isWindowKey,
            isAppActive);

        Assert.Equal(expected, actual);
    }
}
