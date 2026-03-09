using Praxis.Core.Logic;

namespace Praxis.Tests;

public class DatabaseSchemaVersionPolicyTests
{
    [Fact]
    public void ResolvePendingUpgradeVersions_ReturnsAllUpgrades_WhenDatabaseIsUnversioned()
    {
        var result = DatabaseSchemaVersionPolicy.ResolvePendingUpgradeVersions(0);

        Assert.Equal([1, 2], result);
    }

    [Fact]
    public void ResolvePendingUpgradeVersions_ReturnsSecondUpgrade_WhenDatabaseIsOnVersionOne()
    {
        var result = DatabaseSchemaVersionPolicy.ResolvePendingUpgradeVersions(1);

        Assert.Equal([2], result);
    }

    [Fact]
    public void ResolvePendingUpgradeVersions_ReturnsEmpty_WhenDatabaseIsAlreadyCurrent()
    {
        var result = DatabaseSchemaVersionPolicy.ResolvePendingUpgradeVersions(DatabaseSchemaVersionPolicy.CurrentVersion);

        Assert.Empty(result);
    }

    [Fact]
    public void ResolvePendingUpgradeVersions_ThrowsArgumentOutOfRange_WhenVersionIsNegative()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DatabaseSchemaVersionPolicy.ResolvePendingUpgradeVersions(-1));

        Assert.Equal("currentVersion", ex.ParamName);
    }

    [Fact]
    public void ResolvePendingUpgradeVersions_ThrowsNotSupported_WhenVersionIsNewerThanApp()
    {
        var ex = Assert.Throws<NotSupportedException>(() =>
            DatabaseSchemaVersionPolicy.ResolvePendingUpgradeVersions(DatabaseSchemaVersionPolicy.CurrentVersion + 1));

        Assert.Contains("newer than supported version", ex.Message, StringComparison.Ordinal);
    }
}
