using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public static class RecordVersionComparer
{
    public static bool HasConflict(DateTime originalUpdatedAtUtc, DateTime latestUpdatedAtUtc)
        => originalUpdatedAtUtc != latestUpdatedAtUtc;

    public static bool HasConflict(LauncherButtonRecord original, LauncherButtonRecord latest)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(latest);

        if (!HasConflict(original.UpdatedAtUtc, latest.UpdatedAtUtc))
        {
            return false;
        }

        return !ButtonHistoryConsistencyPolicy.MatchesExpectedVersion(original, latest);
    }
}
