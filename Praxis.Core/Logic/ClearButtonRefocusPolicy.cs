namespace Praxis.Core.Logic;

public static class ClearButtonRefocusPolicy
{
    private static readonly TimeSpan[] defaultRetryDelays =
    [
        TimeSpan.Zero,
    ];

    private static readonly TimeSpan[] macCatalystRetryDelays =
    [
        TimeSpan.FromMilliseconds(16),
        TimeSpan.FromMilliseconds(48),
    ];

    private static readonly TimeSpan[] windowsRetryDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(28),
    ];

    public static IReadOnlyList<TimeSpan> ResolveRetryDelays(bool isWindows, bool isMacCatalyst = false)
    {
        if (isWindows)
        {
            return windowsRetryDelays;
        }

        if (isMacCatalyst)
        {
            return macCatalystRetryDelays;
        }

        return defaultRetryDelays;
    }
}
