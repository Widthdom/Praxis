namespace Praxis.Core.Logic;

public static class ClearButtonRefocusPolicy
{
    private static readonly TimeSpan[] defaultRetryDelays =
    [
        TimeSpan.Zero,
    ];

    private static readonly TimeSpan[] windowsRetryDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(28),
    ];

    public static IReadOnlyList<TimeSpan> ResolveRetryDelays(bool isWindows)
        => isWindows ? windowsRetryDelays : defaultRetryDelays;
}
