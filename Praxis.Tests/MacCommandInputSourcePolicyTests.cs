using Praxis.Core.Logic;

namespace Praxis.Tests;

public class MacCommandInputSourcePolicyTests
{
    [Fact]
    public void FocusedInputSourceEnforcementInterval_IsPositive()
    {
        Assert.True(MacCommandInputSourcePolicy.FocusedInputSourceEnforcementInterval > TimeSpan.Zero);
    }

    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, true, false, false, false)]
    [InlineData(false, true, true, false, false)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, false, true, false, false)]
    [InlineData(true, true, false, false, false)]
    [InlineData(true, true, true, false, false)]
    [InlineData(true, true, true, true, true)]
    public void ShouldForceAsciiInputSource_MatchesExpectedTruthTable(
        bool isFirstResponder,
        bool isWindowKey,
        bool isAppActive,
        bool enforceAsciiInput,
        bool expected)
    {
        var actual = MacCommandInputSourcePolicy.ShouldForceAsciiInputSource(
            isFirstResponder,
            isWindowKey,
            isAppActive,
            enforceAsciiInput);

        Assert.Equal(expected, actual);
    }
}
