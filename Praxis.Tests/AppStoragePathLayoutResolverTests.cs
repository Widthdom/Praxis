using Praxis.Core.Logic;

namespace Praxis.Tests;

public class AppStoragePathLayoutResolverTests
{
    private static string NormalizeSeparators(string path) =>
        path.Replace('\\', '/');

    [Fact]
    public void ResolveDatabasePath_UsesLocalAppDataRoot_OnWindows()
    {
        var path = AppStoragePathLayoutResolver.ResolveDatabasePath(
            windowsLocalAppDataRoot: @"C:\Users\tester\AppData\Local",
            nonWindowsBasePath: @"/Users/tester/Library/Application Support",
            isWindows: true);

        Assert.Equal(
            NormalizeSeparators(@"C:\Users\tester\AppData\Local\praxis.db3"),
            NormalizeSeparators(path));
    }

    [Fact]
    public void ResolveDatabasePath_UsesAppDataDirectoryPraxis_OnNonWindows()
    {
        var path = AppStoragePathLayoutResolver.ResolveDatabasePath(
            windowsLocalAppDataRoot: @"C:\Users\tester\AppData\Local",
            nonWindowsBasePath: @"/Users/tester/Library/Application Support",
            isWindows: false);

        Assert.Equal(
            @"/Users/tester/Library/Application Support/Praxis/praxis.db3",
            path);
    }

    [Fact]
    public void ResolveSyncPath_UsesPraxisSubfolder_OnWindows()
    {
        var path = AppStoragePathLayoutResolver.ResolveSyncPath(
            windowsLocalAppDataRoot: @"C:\Users\tester\AppData\Local",
            nonWindowsBasePath: @"/Users/tester/Library/Application Support",
            isWindows: true);

        Assert.Equal(
            NormalizeSeparators(@"C:\Users\tester\AppData\Local\Praxis\buttons.sync"),
            NormalizeSeparators(path));
    }

    [Fact]
    public void ResolveSyncPath_UsesPraxisSubfolder_OnNonWindows()
    {
        var path = AppStoragePathLayoutResolver.ResolveSyncPath(
            windowsLocalAppDataRoot: @"C:\Users\tester\AppData\Local",
            nonWindowsBasePath: @"/Users/tester/Library/Application Support",
            isWindows: false);

        Assert.Equal(
            @"/Users/tester/Library/Application Support/Praxis/buttons.sync",
            path);
    }
}
