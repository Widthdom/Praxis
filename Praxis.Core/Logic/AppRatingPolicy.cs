namespace Praxis.Core.Logic;

public static class AppRatingPolicy
{
    public const int PromptThreshold = 10;
    public const int DeferralReLaunchCount = 20;

    public static bool ShouldPrompt(int launchCount, AppRatingState state, int deferredAtCount)
    {
        if (state == AppRatingState.Done) return false;
        if (launchCount < PromptThreshold) return false;
        if (state == AppRatingState.Deferred && launchCount < deferredAtCount + DeferralReLaunchCount) return false;
        return true;
    }

    public static string SerializeState(AppRatingState state) => state.ToString().ToLowerInvariant();

    public static AppRatingState ParseState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return AppRatingState.None;
        if (string.Equals(value, "deferred", StringComparison.OrdinalIgnoreCase)) return AppRatingState.Deferred;
        if (string.Equals(value, "done", StringComparison.OrdinalIgnoreCase)) return AppRatingState.Done;
        return AppRatingState.None;
    }
}

public enum AppRatingState
{
    None,
    Deferred,
    Done,
}
