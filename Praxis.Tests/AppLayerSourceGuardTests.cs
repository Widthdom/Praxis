namespace Praxis.Tests;

public class AppLayerSourceGuardTests
{
    [Fact]
    public void MainPage_XamlLoadFailure_IsCrashLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Contains("CrashFileLogger.WriteException(\"MainPage.InitializeComponent\", ex);", source);
    }

    [Fact]
    public void MainPage_InitializationFailure_ResetsInitializedAndCrashLogs()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");

        Assert.Contains("CrashFileLogger.WriteException(\"MainPage.OnAppearing.InitializeAsync\", ex);", source);
        Assert.Contains("initialized = false;", source);

        var initializeIndex = source.IndexOf("await viewModel.InitializeAsync();", StringComparison.Ordinal);
        var initializedIndex = source.IndexOf("initialized = true;", StringComparison.Ordinal);

        Assert.True(initializeIndex >= 0, "MainPage should await ViewModel initialization.");
        Assert.True(initializedIndex > initializeIndex, "initialized should only flip true after InitializeAsync succeeds.");
    }

    [Fact]
    public void MainPage_InitializationAlertFailure_IsCrashLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Contains("CrashFileLogger.WriteException(\"MainPage.OnAppearing.DisplayAlertAsync\", alertEx);", source);
    }

    [Fact]
    public void MainPage_OnDisappearing_DetachesWindowActivationHooks()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Contains("DetachWindowActivationHook();", source);
    }

    [Fact]
    public void MainPage_DetachWindowActivationHook_AlsoDetachesMacObservers()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Equal(2, CountOccurrences(source, "DetachMacActivationObservers();"));
    }

    [Fact]
    public void MauiClipboardService_HonorsCancellationTokens()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "MauiClipboardService.cs");

        Assert.Equal(2, CountOccurrences(source, "cancellationToken.ThrowIfCancellationRequested();"));
        Assert.Equal(2, CountOccurrences(source, ".WaitAsync(cancellationToken)"));
    }

    [Fact]
    public void SqliteAppRepository_Initialization_AssignsSharedConnectionOnlyAfterSuccessfulLoad()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "SqliteAppRepository.cs");

        Assert.Contains("var initializedConnection = new SQLiteAsyncConnection(dbPath);", source);
        Assert.Contains("await EnsureSchemaAsync(initializedConnection);", source);
        Assert.Contains("var entities = await initializedConnection.Table<LauncherButtonEntity>().ToListAsync();", source);
        Assert.Contains("connection = initializedConnection;", source);

        var cacheRebuildIndex = source.IndexOf("RebuildCommandCache();", StringComparison.Ordinal);
        var assignmentIndex = source.IndexOf("connection = initializedConnection;", StringComparison.Ordinal);

        Assert.True(cacheRebuildIndex >= 0, "InitializeCoreAsync should rebuild the command cache before publishing the shared connection.");
        Assert.True(assignmentIndex > cacheRebuildIndex, "connection should only be published after schema and cache initialization succeed.");
    }

    [Fact]
    public void App_CreateWindow_DoesNotCacheFallbackErrorPage()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("if (page is MainPage)", source);
        Assert.Contains("rootPage = page;", source);
        Assert.Contains("Root page resolution fell back to an error page; cache not updated.", source);
    }

    [Fact]
    public void App_FlushFailures_AreCrashLogged_DuringUnhandledExceptionAndProcessExit()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("TryFlushLogs(TimeSpan.FromSeconds(2), \"AppDomain.UnhandledException\");", source);
        Assert.Contains("TryFlushLogs(TimeSpan.FromSeconds(3), \"App.ProcessExit\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(context, $\"Log flush failed: {ex.Message}\");", source);
    }

    [Fact]
    public void FileStateSyncNotifier_ReadRetryExhaustion_IsCrashLogged()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileStateSyncNotifier.cs");

        Assert.Contains("Exception? readFailure = null;", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileStateSyncNotifier), $\"Failed to read sync payload after retries: {readFailure.Message}\");", source);
    }

    [Fact]
    public void CommandExecutor_ExpandsHomePathBeforeCheckingToolUsability()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "CommandExecutor.cs");
        Assert.Contains("var normalizedTool = ExpandHomePath(NormalizeToolPath(tool));", source);
    }

    [Fact]
    public void WindowsStartupLog_UsesNormalizedAppStorageRoot_AndGuardsDuplicateHookRegistration()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "Windows", "App.xaml.cs");

        Assert.Contains("AppStoragePaths.WindowsLocalAppDataRoot", source);
        Assert.Contains("private static bool globalExceptionLoggingHooked;", source);
        Assert.Contains("if (globalExceptionLoggingHooked)", source);
    }

    [Fact]
    public void MacAppDelegate_GuardsDuplicateGlobalExceptionHookRegistration()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "AppDelegate.cs");

        Assert.Contains("private static bool globalExceptionLoggingHooked;", source);
        Assert.Contains("if (globalExceptionLoggingHooked)", source);
        Assert.Contains("globalExceptionLoggingHooked = true;", source);
    }

    private static string ReadRepositoryFile(params string[] segments)
        => File.ReadAllText(Path.Combine(ResolveRepositoryRoot(), Path.Combine(segments)));

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Praxis"))
                && Directory.Exists(Path.Combine(current.FullName, "Praxis.Tests")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located from test output path.");
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
