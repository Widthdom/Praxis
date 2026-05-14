using Praxis.Core.Logic;
using Praxis.Data.Storage;

namespace Praxis.Tests;

public class AppStoragePathProviderTests
{
    [Fact]
    public void AppDataDirectory_UsesEnvironmentOverride_WhenAbsolute()
    {
        var previous = Environment.GetEnvironmentVariable(AppStoragePathProvider.AppDataDirectoryEnvironmentVariable);
        var directory = CreateTempDirectory();
        try
        {
            Environment.SetEnvironmentVariable(AppStoragePathProvider.AppDataDirectoryEnvironmentVariable, directory);

            var provider = new AppStoragePathProvider();

            Assert.Equal(directory, provider.AppDataDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AppStoragePathProvider.AppDataDirectoryEnvironmentVariable, previous);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DatabasePath_ReturnsDefaultPraxisDb3_WhenNoDatabaseExists()
    {
        var directory = CreateTempDirectory();
        try
        {
            var provider = new AppStoragePathProvider(directory);

            Assert.Equal(
                Path.Combine(directory, AppStoragePathLayoutResolver.DatabaseFileName),
                provider.DatabasePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DatabasePath_ReturnsLegacyPraxisDb_WhenOnlyLegacyDatabaseExists()
    {
        var directory = CreateTempDirectory();
        try
        {
            var legacyPath = Path.Combine(directory, AppStoragePathProvider.LegacyDatabaseFileName);
            File.WriteAllText(legacyPath, string.Empty);

            var provider = new AppStoragePathProvider(directory);

            Assert.Equal(legacyPath, provider.DatabasePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DatabasePath_PrefersPraxisDb3_WhenBothDatabaseNamesExist()
    {
        var directory = CreateTempDirectory();
        try
        {
            var defaultPath = Path.Combine(directory, AppStoragePathLayoutResolver.DatabaseFileName);
            File.WriteAllText(defaultPath, string.Empty);
            File.WriteAllText(Path.Combine(directory, AppStoragePathProvider.LegacyDatabaseFileName), string.Empty);

            var provider = new AppStoragePathProvider(directory);

            Assert.Equal(defaultPath, provider.DatabasePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"praxis-storage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
