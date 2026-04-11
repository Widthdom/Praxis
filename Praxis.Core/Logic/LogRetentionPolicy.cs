using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public static class LogRetentionPolicy
{
    public static IReadOnlyList<LaunchLogEntry> GetEntriesToDelete(
        IEnumerable<LaunchLogEntry> logs,
        DateTime nowUtc,
        int retentionDays)
    {
        ArgumentNullException.ThrowIfNull(logs);

        if (retentionDays < 1)
        {
            retentionDays = 1;
        }

        var threshold = nowUtc.AddDays(-retentionDays);
        return logs
            .OfType<LaunchLogEntry>()
            .Where(x => x.TimestampUtc < threshold)
            .ToList();
    }
}
