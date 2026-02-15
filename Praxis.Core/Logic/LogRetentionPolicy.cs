using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public static class LogRetentionPolicy
{
    public static IReadOnlyList<LaunchLogEntry> GetEntriesToDelete(
        IEnumerable<LaunchLogEntry> logs,
        DateTime nowUtc,
        int retentionDays)
    {
        if (retentionDays < 1)
        {
            retentionDays = 1;
        }

        var threshold = nowUtc.AddDays(-retentionDays);
        return logs.Where(x => x.TimestampUtc < threshold).ToList();
    }
}
