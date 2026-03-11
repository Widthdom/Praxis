namespace Praxis.Core.Logic;

public static class DatabaseSchemaVersionPolicy
{
    public const int InitialVersion = 0;
    public const int CurrentVersion = 3;

    public static IReadOnlyList<int> ResolvePendingUpgradeVersions(int currentVersion)
    {
        if (currentVersion < InitialVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(currentVersion), "Schema version cannot be negative.");
        }

        if (currentVersion > CurrentVersion)
        {
            throw new NotSupportedException(
                $"Database schema version {currentVersion} is newer than supported version {CurrentVersion}.");
        }

        if (currentVersion == CurrentVersion)
        {
            return [];
        }

        var versions = new List<int>(CurrentVersion - currentVersion);
        for (var version = currentVersion + 1; version <= CurrentVersion; version++)
        {
            versions.Add(version);
        }

        return versions;
    }
}
