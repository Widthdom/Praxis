using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ClearButtonRefocusPolicyTests
{
    [Fact]
    public void ResolveRetryDelays_ReturnsSingleImmediateAttempt_ForNonWindows()
    {
        var delays = ClearButtonRefocusPolicy.ResolveRetryDelays(isWindows: false, isMacCatalyst: false);

        Assert.Single(delays);
        Assert.Equal(TimeSpan.Zero, delays[0]);
    }

    [Fact]
    public void ResolveRetryDelays_ReturnsDeferredRetries_ForMacCatalyst()
    {
        var delays = ClearButtonRefocusPolicy.ResolveRetryDelays(isWindows: false, isMacCatalyst: true);

        Assert.Equal(2, delays.Count);
        Assert.Equal(TimeSpan.FromMilliseconds(16), delays[0]);
        Assert.Equal(TimeSpan.FromMilliseconds(48), delays[1]);
    }

    [Fact]
    public void ResolveRetryDelays_ReturnsImmediateAndDelayedAttempts_ForWindows()
    {
        var delays = ClearButtonRefocusPolicy.ResolveRetryDelays(isWindows: true, isMacCatalyst: false);

        Assert.Equal(2, delays.Count);
        Assert.Equal(TimeSpan.Zero, delays[0]);
        Assert.Equal(TimeSpan.FromMilliseconds(28), delays[1]);
    }

    [Fact]
    public void ResolveRetryDelays_PrefersWindowsSchedule_WhenBothWindowsAndMacFlagsAreSet()
    {
        var delays = ClearButtonRefocusPolicy.ResolveRetryDelays(isWindows: true, isMacCatalyst: true);

        Assert.Equal(2, delays.Count);
        Assert.Equal(TimeSpan.Zero, delays[0]);
        Assert.Equal(TimeSpan.FromMilliseconds(28), delays[1]);
    }
}
