namespace Praxis.Core.Logic;

public static class RecordVersionComparer
{
    public static bool HasConflict(DateTime originalUpdatedAtUtc, DateTime latestUpdatedAtUtc)
        => originalUpdatedAtUtc != latestUpdatedAtUtc;
}
