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
        Assert.True(delays[0] > TimeSpan.Zero);
        Assert.True(delays[1] > delays[0]);
    }

    [Fact]
    public void ResolveRetryDelays_ReturnsImmediateAndDelayedAttempts_ForWindows()
    {
        var delays = ClearButtonRefocusPolicy.ResolveRetryDelays(isWindows: true, isMacCatalyst: false);

        Assert.Equal(2, delays.Count);
        Assert.Equal(TimeSpan.Zero, delays[0]);
        Assert.True(delays[1] > TimeSpan.Zero);
    }
}
