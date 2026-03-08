using Praxis.Core.Logic;

namespace Praxis.Tests;

public class AppRatingPolicyTests
{
    [Fact]
    public void ShouldPrompt_ReturnsFalse_WhenStateIsDone()
    {
        var result = AppRatingPolicy.ShouldPrompt(
            launchCount: 100,
            state: AppRatingState.Done,
            deferredAtCount: 0);

        Assert.False(result);
    }

    [Fact]
    public void ShouldPrompt_ReturnsFalse_WhenLaunchCountBelowThreshold()
    {
        var result = AppRatingPolicy.ShouldPrompt(
            launchCount: AppRatingPolicy.PromptThreshold - 1,
            state: AppRatingState.None,
            deferredAtCount: 0);

        Assert.False(result);
    }

    [Fact]
    public void ShouldPrompt_ReturnsTrue_WhenLaunchCountReachesThreshold()
    {
        var result = AppRatingPolicy.ShouldPrompt(
            launchCount: AppRatingPolicy.PromptThreshold,
            state: AppRatingState.None,
            deferredAtCount: 0);

        Assert.True(result);
    }

    [Fact]
    public void ShouldPrompt_ReturnsFalse_WhenDeferredAndNotEnoughLaunchesSinceDeferral()
    {
        var deferredAt = 10;
        var result = AppRatingPolicy.ShouldPrompt(
            launchCount: deferredAt + AppRatingPolicy.DeferralReLaunchCount - 1,
            state: AppRatingState.Deferred,
            deferredAtCount: deferredAt);

        Assert.False(result);
    }

    [Fact]
    public void ShouldPrompt_ReturnsTrue_WhenDeferredAndEnoughLaunchesSinceDeferral()
    {
        var deferredAt = 10;
        var result = AppRatingPolicy.ShouldPrompt(
            launchCount: deferredAt + AppRatingPolicy.DeferralReLaunchCount,
            state: AppRatingState.Deferred,
            deferredAtCount: deferredAt);

        Assert.True(result);
    }

    [Theory]
    [InlineData("none", AppRatingState.None)]
    [InlineData("deferred", AppRatingState.Deferred)]
    [InlineData("done", AppRatingState.Done)]
    [InlineData("DEFERRED", AppRatingState.Deferred)]
    [InlineData("DONE", AppRatingState.Done)]
    [InlineData("", AppRatingState.None)]
    [InlineData(null, AppRatingState.None)]
    [InlineData("unknown", AppRatingState.None)]
    public void ParseState_ReturnsExpectedState(string? input, AppRatingState expected)
    {
        var result = AppRatingPolicy.ParseState(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(AppRatingState.None, "none")]
    [InlineData(AppRatingState.Deferred, "deferred")]
    [InlineData(AppRatingState.Done, "done")]
    public void SerializeState_ReturnsLowercaseString(AppRatingState state, string expected)
    {
        var result = AppRatingPolicy.SerializeState(state);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShouldPrompt_ReturnsTrue_WhenStateIsNoneAndLaunchCountExceedsThreshold()
    {
        var result = AppRatingPolicy.ShouldPrompt(
            launchCount: AppRatingPolicy.PromptThreshold + 50,
            state: AppRatingState.None,
            deferredAtCount: 0);

        Assert.True(result);
    }
}
