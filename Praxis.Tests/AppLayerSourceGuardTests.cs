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
    public void App_FlushFailures_AreWarningLogged_DuringUnhandledExceptionAndProcessExit()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("TryFlushLogs(TimeSpan.FromSeconds(2), \"AppDomain.UnhandledException\");", source);
        Assert.Contains("TryFlushLogs(TimeSpan.FromSeconds(3), \"App.ProcessExit\");", source);
        Assert.Contains("CrashFileLogger.WriteException(context, ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(context, $\"Log flush failed: {ex.Message}\");", source);
    }

    [Fact]
    public void App_GlobalExceptionHandlers_AreRegisteredOnlyOnce()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("private static int globalExceptionHandlersRegistered;", source);
        Assert.Contains("if (Interlocked.Exchange(ref globalExceptionHandlersRegistered, 1) == 0)", source);
    }

    [Fact]
    public void App_StaticEventDispatchers_UseSharedLoggedRaiseHelpers()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("private static void TryRaise(Action? handler, string context)", source);
        Assert.Contains("private static void TryRaise<T>(Action<T>? handler, T argument, string context)", source);
        Assert.Contains("TryRaise(MacApplicationDeactivating, nameof(RaiseMacApplicationDeactivating));", source);
        Assert.Contains("TryRaise(MacApplicationActivated, nameof(RaiseMacApplicationActivated));", source);
        Assert.Contains("TryRaise(ThemeShortcutRequested, mode, nameof(RaiseThemeShortcut));", source);
        Assert.Contains("TryRaise(EditorShortcutRequested, action, nameof(RaiseEditorShortcut));", source);
        Assert.Contains("TryRaise(CommandInputShortcutRequested, action, nameof(RaiseCommandInputShortcut));", source);
        Assert.Contains("TryRaise(HistoryShortcutRequested, action, nameof(RaiseHistoryShortcut));", source);
        Assert.Contains("TryRaise(MiddleMouseClickRequested, nameof(RaiseMiddleMouseClick));", source);
        Assert.Equal(2, CountOccurrences(source, "errorLogger?.Log(ex, context);"));
    }

    [Fact]
    public void FileStateSyncNotifier_ReadRetryExhaustion_IsWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileStateSyncNotifier.cs");

        Assert.Contains("Exception? readFailure = null;", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileStateSyncNotifier), $\"Failed to read sync payload after retries: {readFailure.Message}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileStateSyncNotifier), $\"Unexpected sync publish failure: {ex.Message}\");", source);
    }

    [Fact]
    public void FileStateSyncNotifier_WriteFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileStateSyncNotifier.cs");

        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileStateSyncNotifier), $\"Failed to write sync payload '{signalPath}': {ex.Message}\");", source);
    }

    [Fact]
    public void FileStateSyncNotifier_IgnoresNotifyRequestsAfterDispose()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileStateSyncNotifier.cs");
        Assert.Contains("if (disposed)", source);
    }

    [Fact]
    public void FileStateSyncNotifier_Dispose_DisablesWatcherBeforeReleasingIt()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileStateSyncNotifier.cs");
        Assert.Contains("watcher.EnableRaisingEvents = false;", source);
    }

    [Fact]
    public void CommandExecutor_ExpandsHomePathBeforeCheckingToolUsability()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "CommandExecutor.cs");
        Assert.Contains("var normalizedTool = ExpandHomePath(NormalizeToolPath(tool));", source);
    }

    [Fact]
    public void CommandExecutor_FailureBreadcrumbs_ArePresent_ForProcessAndResolutionFailures()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "CommandExecutor.cs");

        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandExecutor), $\"Launch target resolution failed for '{arguments}': {ex.Message}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandExecutor), $\"{failurePrefix} {ex.Message}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandExecutor), $\"{failurePrefix} No process handle was returned.\");", source);
    }

    [Fact]
    public void DbErrorLogger_WritesCrashFileBeforeEnqueueingDatabaseWrites()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "DbErrorLogger.cs");

        AssertMethodContainsInOrder(source,
            "public void Log(Exception exception, string context)",
            "var normalizedContext = CrashFileLogger.NormalizeContext(context);",
            "CrashFileLogger.WriteException($\"ERROR [{normalizedContext}]\", exception);",
            "pendingWrites.Enqueue(entry);");
        AssertMethodContainsInOrder(source,
            "public void LogWarning(string message, string context)",
            "var normalizedContext = CrashFileLogger.NormalizeContext(context);",
            "CrashFileLogger.WriteWarning(normalizedContext, normalizedMessage);",
            "pendingWrites.Enqueue(entry);");
        Assert.Contains("var normalizedMessage = NormalizeMessagePayload(message);", source);
        AssertMethodContainsInOrder(source,
            "public void LogInfo(string message, string context)",
            "var normalizedContext = CrashFileLogger.NormalizeContext(context);",
            "CrashFileLogger.WriteInfo(normalizedContext, normalizedMessage);",
            "pendingWrites.Enqueue(entry);");
        Assert.Contains("var normalizedMessage = NormalizeMessagePayload(message);", source);
    }

    [Fact]
    public void DbErrorLogger_DrainFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "DbErrorLogger.cs");

        Assert.Contains("CrashFileLogger.WriteException(nameof(DbErrorLogger), ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $\"Drain loop failed unexpectedly: {ex.Message}\");", source);
    }

    [Fact]
    public void WindowsCommandEntryHandler_DisablesInputScopeAfterCompatibilityException()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "Windows", "Handlers", "CommandEntryHandler.cs");

        Assert.Contains("catch (Exception ex) when (WindowsInputScopeCompatibilityPolicy.ShouldDisableInputScopeOnException(ex))", source);
        Assert.Contains("catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)", source);
        Assert.Contains("inputScopeUnsupported = true;", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $\"InputScope assignment disabled after compatibility failure: {ex.Message}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $\"InputScope assignment failed unexpectedly: {ex.Message}\");", source);
        Assert.Contains("catch", source);
        Assert.Contains("return false;", source);
    }

    [Fact]
    public void MainPage_WindowsReflectionAndFocusFallbackFailures_AreWarningLogged()
    {
        var focusSource = ReadRepositoryFile("Praxis", "MainPage.FocusAndContext.cs");
        var pointerSource = ReadRepositoryFile("Praxis", "MainPage.PointerAndSelection.cs");
        var layoutSource = ReadRepositoryFile("Praxis", "MainPage.LayoutUtilities.cs");

        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DisableWindowsSystemFocusVisual), $\"Failed to disable UseSystemFocusVisuals: {ex.Message}\");", focusSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FocusModalPrimaryEditorField), $\"Failed to focus modal ButtonText entry: {ex.Message}\");", pointerSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(SetTabStop), $\"Failed to set IsTabStop={isTabStop}: {ex.Message}\");", layoutSource);
    }

    [Fact]
    public void WindowsStartupLog_UsesNormalizedAppStorageRoot_AndGuardsDuplicateHookRegistration()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "Windows", "App.xaml.cs");

        Assert.Contains("AppStoragePaths.WindowsLocalAppDataRoot", source);
        Assert.Contains("private static bool globalExceptionLoggingHooked;", source);
        Assert.Contains("if (globalExceptionLoggingHooked)", source);
        Assert.Contains("CrashFileLogger.WriteException(nameof(App), ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(App), $\"Failed to create startup log directory '{startupLogDirectory}': {ex.Message}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(App), $\"Failed to append startup log '{StartupLogPath}': {ex.Message}\");", source);
    }

    [Fact]
    public void MacAppDelegate_GuardsDuplicateGlobalExceptionHookRegistration()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "AppDelegate.cs");

        Assert.Contains("private static bool globalExceptionLoggingHooked;", source);
        Assert.Contains("if (globalExceptionLoggingHooked)", source);
        Assert.Contains("globalExceptionLoggingHooked = true;", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(AppDelegate), $\"Failed to hook MarshalManagedException: {ex.Message}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(AppDelegate), $\"Failed to prioritize key command '{selectorName}': {ex.Message}\");", source);
    }

    [Fact]
    public void MainViewModel_ExternalThemeSync_DispatchFailuresAreLogged()
    {
        var source = ReadRepositoryFile("Praxis", "ViewModels", "MainViewModel.cs");

        Assert.Contains("errorLogger.Log(ex, nameof(SyncThemeFromExternalChangeAsync));", source);
        Assert.Contains("External theme sync dispatch failed:", source);
        Assert.Contains("TaskCreationOptions.RunContinuationsAsynchronously", source);
    }

    [Fact]
    public void MainViewModel_CommandSuggestionDispatchFailures_AreLogged()
    {
        var source = ReadRepositoryFile("Praxis", "ViewModels", "MainViewModel.CommandSuggestions.cs");

        Assert.Contains("errorLogger.Log(ex, nameof(DebouncedRefreshCommandSuggestionsAsync));", source);
        Assert.Contains("errorLogger.Log(ex, nameof(RefreshCommandSuggestionsOnMainThread));", source);
        Assert.Contains("Command suggestion close dispatch failed:", source);
        Assert.Contains("Command suggestion refresh dispatch failed:", source);
        Assert.Contains("catch (OperationCanceledException) when (token.IsCancellationRequested)", source);
    }

    [Fact]
    public void AppStoragePaths_LegacyMigrationFailures_AreWarningLogged_AndSkipped()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "AppStoragePaths.cs");

        Assert.Equal(2, CountOccurrences(source, "CrashFileLogger.WriteWarning(nameof(AppStoragePaths), $\"Legacy database migration failed from '{sourcePath}': {ex.Message}\");"));
    }

    [Fact]
    public void FileAppConfigService_FallsBackOnUnauthorizedAccess()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileAppConfigService.cs");
        Assert.Contains("catch (UnauthorizedAccessException ex)", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileAppConfigService), $\"Skipping config '{path}': {ex.Message}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileAppConfigService), $\"Skipping config '{path}' because it does not specify a valid theme.\");", source);
    }

    [Fact]
    public void MauiThemeService_SkipsNoOpApplies_AndCrashLogsDispatchFailures()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "MauiThemeService.cs");

        Assert.Contains("if (Application.Current.UserAppTheme == appTheme)", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MauiThemeService), $\"ApplyMacWindowStyle dispatch failed: {ex.Message}\");", source);
    }

    [Fact]
    public void MacProgram_RelayFailures_AreWarningLogged_AndNullProcessIsRejected()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Program.cs");

        Assert.Contains("var process = Process.Start(startInfo);", source);
        Assert.Contains("if (process is null)", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(Program), $\"LaunchServices relay returned no process for bundle '{bundlePath}'.\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(Program), $\"LaunchServices relay failed for bundle '{bundlePath}': {ex.Message}\");", source);
    }

    [Fact]
    public void MacHandlers_KeyInputResolutionFailures_AreWarningLogged_AndFallBack()
    {
        var macEntrySource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "MacEntryHandler.cs");
        var macEditorSource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "MacEditorHandler.cs");
        var commandEntrySource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "CommandEntryHandler.cs");

        Assert.Contains("return TryResolveKeyInput(inputName) ?? fallback;", macEntrySource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MacEntryHandler), $\"Failed to resolve UIKeyCommand input '{inputName}': {ex.Message}\");", macEntrySource);

        Assert.Contains("return TryResolveKeyInput(inputName) ?? fallback;", macEditorSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MacEditorHandler), $\"Failed to resolve UIKeyCommand input '{inputName}': {ex.Message}\");", macEditorSource);

        Assert.Contains("return TryResolveKeyInput(inputName) ?? fallback;", commandEntrySource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $\"Failed to resolve UIKeyCommand input '{inputName}': {ex.Message}\");", commandEntrySource);
    }

    [Fact]
    public void MacMiddleClickAndKeyCommandFallbackFailures_AreWarningLogged()
    {
        var behaviorSource = ReadRepositoryFile("Praxis", "Behaviors", "MiddleClickBehavior.cs");
        var macSource = ReadRepositoryFile("Praxis", "MainPage.MacCatalystBehavior.cs");

        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MiddleClickBehavior), $\"Failed to set buttonMaskRequired={mask}: {ex.Message}\");", behaviorSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MiddleClickBehavior), $\"Deferred middle-click execution failed: {ex.Message}\");", behaviorSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(TryCreateMacEditorKeyCommand), $\"Failed to create Mac editor key command '{selectorName}' for input '{keyInput}': {ex.Message}\");", macSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(IsMacMiddleButtonCurrentlyDown), $\"Failed to query middle button state from CoreGraphics: {ex.Message}\");", macSource);
    }

    [Fact]
    public void MainPage_CopyNoticeAnimationFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Contains("catch (OperationCanceledException) when (token.IsCancellationRequested)", source);
        Assert.Contains("CrashFileLogger.WriteWarning(\"MainPage.CopyIconButton_Clicked\", $\"Copy notice animation failed: {ex.Message}\");", source);
    }

    [Fact]
    public void MainPage_StatusFlashAnimationFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.StatusAndTheme.cs");
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(TriggerStatusFlash), $\"Status flash animation failed: {ex.Message}\");", source);
    }

    [Fact]
    public void MainPage_DockHoverExitFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.DockAndQuickLook.cs");
        Assert.Contains("catch (OperationCanceledException) when (token.IsCancellationRequested)", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(HideDockScrollBarAfterExitDelayAsync), $\"Dock hover-exit hide failed: {ex.Message}\");", source);
        Assert.True(
            source.IndexOf("SetDockScrollBarVisibility(isPointerOverDockRegion: false);", StringComparison.Ordinal)
            < source.IndexOf("CrashFileLogger.WriteWarning(nameof(HideDockScrollBarAfterExitDelayAsync), $\"Dock hover-exit hide failed: {ex.Message}\");", StringComparison.Ordinal));
    }

    [Fact]
    public void MainPage_QuickLookShowFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.DockAndQuickLook.cs");
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(ShowQuickLookAfterDelayAsync), $\"Quick Look show failed: {ex.Message}\");", source);
        Assert.True(
            source.IndexOf("QuickLookPopup.CancelAnimations();", StringComparison.Ordinal)
            < source.IndexOf("CrashFileLogger.WriteWarning(nameof(ShowQuickLookAfterDelayAsync), $\"Quick Look show failed: {ex.Message}\");", StringComparison.Ordinal));
    }

    [Fact]
    public void MainPage_QuickLookHideFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.DockAndQuickLook.cs");
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(HideQuickLookAfterDelayAsync), $\"Quick Look hide failed: {ex.Message}\");", source);
        Assert.True(
            source.IndexOf("QuickLookPopup.IsVisible = false;", StringComparison.Ordinal)
            < source.IndexOf("CrashFileLogger.WriteWarning(nameof(HideQuickLookAfterDelayAsync), $\"Quick Look hide failed: {ex.Message}\");", StringComparison.Ordinal));
    }

    [Fact]
    public void MainPage_ButtonTapExecutionFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.EditorAndInput.cs");
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(Draggable_Tapped), $\"Button tap execution failed for '{item.ButtonText}': {ex.Message}\");", source);
    }

    [Fact]
    public void MainPage_SecondaryTapCreateFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.PointerAndSelection.cs");
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(PlacementCanvas_SecondaryTapped), $\"Secondary-tap create flow failed: {ex.Message}\");", source);
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

    private static void AssertMethodContainsInOrder(string text, string methodSignature, params string[] markers)
    {
        var methodStart = text.IndexOf(methodSignature, StringComparison.Ordinal);
        Assert.True(methodStart >= 0, $"Missing method: {methodSignature}");
        Assert.True(markers.Length >= 2, "At least two ordered markers are required.");

        var methodBody = text[methodStart..];
        var previousIndex = -1;
        string? previousMarker = null;
        foreach (var marker in markers)
        {
            var currentIndex = methodBody.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(currentIndex >= 0, $"Missing marker: {marker}");
            if (previousMarker is not null)
            {
                Assert.True(currentIndex > previousIndex, $"Expected '{previousMarker}' to appear before '{marker}'.");
            }

            previousIndex = currentIndex;
            previousMarker = marker;
        }
    }
}
