namespace Praxis.Tests;

public class AppLayerSourceGuardTests
{
    [Fact]
    public void MainPage_XamlLoadFailure_IsCrashLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Contains("CrashFileLogger.WriteException(\"MainPage.InitializeComponent\", ex);", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("new Label { Text = safeMessage },", source);
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
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("await DisplayAlertAsync(\"Initialization Error\", safeMessage, \"OK\");", source);
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
    public void HoverHandCursorBehavior_SetsPlatformCursor_OnPointerEnterAndExit()
    {
        var source = ReadRepositoryFile("Praxis", "Behaviors", "HoverHandCursorBehavior.cs");

        Assert.Contains("public sealed class HoverHandCursorBehavior : Behavior<View>", source);
        Assert.Contains("private readonly PointerGestureRecognizer pointer = new();", source);
        Assert.Contains("pointer.PointerEntered += OnPointerEntered;", source);
        Assert.Contains("pointer.PointerExited += OnPointerExited;", source);
        Assert.Contains("SetHandCursor(sender, useHandCursor: true);", source);
        Assert.Contains("SetHandCursor(sender, useHandCursor: false);", source);
        Assert.Contains("NonPublicPropertySetter.TrySet(frameworkElement, \"ProtectedCursor\", cursor);", source);
        Assert.Contains("var cursorSelector = useHandCursor ? pointingHandCursorSelector : arrowCursorSelector;", source);
        Assert.Contains("ObjcMsgSendVoid(cursor, setCursorSelector);", source);
    }

    [Fact]
    public void MainPage_InteractiveButtons_UseHoverHandCursorBehavior()
    {
        var xaml = ReadRepositoryFile("Praxis", "MainPage.xaml");

        Assert.Equal(16, CountOccurrences(xaml, "<behaviors:HoverHandCursorBehavior />"));
        Assert.Contains("<Border x:Name=\"CreateButton\"", xaml);
        Assert.Contains("<behaviors:HoverHandCursorBehavior />\n                                        <behaviors:MiddleClickBehavior", xaml);
        Assert.Contains("<Button x:Name=\"ContextEditButton\"", xaml);
        Assert.Contains("<Button x:Name=\"ContextDeleteButton\"", xaml);
        Assert.Contains("<Button x:Name=\"CopyClipWordButton\"", xaml);
        Assert.Contains("<Button x:Name=\"CopyNoteButton\"", xaml);
        Assert.Contains("<Button x:Name=\"ModalCancelButton\"", xaml);
        Assert.Contains("<Button x:Name=\"ModalSaveButton\"", xaml);
        Assert.Contains("<Button x:Name=\"ConflictReloadButton\"", xaml);
        Assert.Contains("<Button x:Name=\"ConflictOverwriteButton\"", xaml);
        Assert.Contains("<Button x:Name=\"ConflictCancelButton\"", xaml);
    }

    [Fact]
    public void GrabHandCursorBehavior_SetsPlatformGrabCursor_WhilePointerIsPressed()
    {
        var source = ReadRepositoryFile("Praxis", "Behaviors", "GrabHandCursorBehavior.cs");

        Assert.Contains("public sealed class GrabHandCursorBehavior : Behavior<View>", source);
        Assert.Contains("private static readonly object activeGrabLock = new();", source);
        Assert.Contains("private static GrabHandCursorBehavior? activeGrabBehavior;", source);
        Assert.Contains("private readonly PointerGestureRecognizer pointer = new();", source);
        Assert.Contains("private bool isGrabbing;", source);
        Assert.Contains("pointer.PointerPressed += OnPointerPressed;", source);
        Assert.Contains("pointer.PointerReleased += OnPointerReleased;", source);
        Assert.Contains("pointer.PointerMoved += OnPointerMoved;", source);
        Assert.Contains("pointer.PointerEntered += OnPointerEntered;", source);
        Assert.Contains("pointer.PointerExited += OnPointerExited;", source);
        Assert.Contains("private static void SetActiveGrab(GrabHandCursorBehavior behavior, object? sender)", source);
        Assert.Contains("private static void ClearActiveGrab()", source);
        Assert.Contains("if (ReferenceEquals(GetActiveGrabBehavior(), this))", source);
        Assert.Contains("private void OnPointerExited(object? sender, PointerEventArgs e)", source);
        Assert.Contains("NonPublicPropertySetter.TrySet(frameworkElement, \"ProtectedCursor\", cursor);", source);
        Assert.Contains("Microsoft.UI.Input.InputSystemCursorShape.SizeAll", source);
        Assert.Contains("var cursorSelector = useGrabCursor ? closedHandCursorSelector : arrowCursorSelector;", source);
        Assert.Contains("ObjcMsgSendVoid(cursor, setCursorSelector);", source);
    }

    [Fact]
    public void GrabHandCursorBehavior_IgnoresSecondaryAndMiddlePress_AndClearsGrabOnMoveWhenPrimaryReleased()
    {
        var source = ReadRepositoryFile("Praxis", "Behaviors", "GrabHandCursorBehavior.cs");

        Assert.Contains("if (!IsPrimaryOnlyPointerPressed(e))", source);
        Assert.Contains("private static bool IsPrimaryOnlyPointerPressed(PointerEventArgs e)", source);
        Assert.Contains("private static bool IsAnyPrimaryPointerStillPressed(PointerEventArgs e)", source);
        Assert.Contains("private void OnPointerMoved(object? sender, PointerEventArgs e)", source);
        Assert.Contains("if (!IsAnyPrimaryPointerStillPressed(e))", source);
        Assert.Contains("props.IsLeftButtonPressed", source);
        Assert.Contains("!props.IsRightButtonPressed", source);
        Assert.Contains("!props.IsMiddleButtonPressed", source);
        // Mac path delegates to the shared classifier so secondary/middle detection
        // is not reduced to substring inspection of platform-args text.
        Assert.Contains("PointerButtonClassifier.IsPrimaryOnly(e.PlatformArgs)", source);
        Assert.Contains("PointerButtonClassifier.IsPrimaryPressed(e.PlatformArgs)", source);
    }

    [Fact]
    public void GrabHandCursorBehavior_OnDetachingFrom_ClearsGrabCursor_IfStillGrabbing()
    {
        var source = ReadRepositoryFile("Praxis", "Behaviors", "GrabHandCursorBehavior.cs");

        Assert.Contains("protected override void OnDetachingFrom(View bindable)", source);
        var detachIndex = source.IndexOf("protected override void OnDetachingFrom(View bindable)", StringComparison.Ordinal);
        Assert.True(detachIndex >= 0, "OnDetachingFrom must exist.");

        var detachBody = source[detachIndex..];
        var restoreIndex = detachBody.IndexOf("ClearActiveGrab();", StringComparison.Ordinal);
        var removeIndex = detachBody.IndexOf("bindable.GestureRecognizers.Remove(pointer);", StringComparison.Ordinal);

        Assert.True(restoreIndex >= 0, "OnDetachingFrom must restore the cursor when detaching while grabbing.");
        Assert.True(removeIndex > restoreIndex, "Cursor restore should run before gesture recognizers are removed.");
        Assert.Contains("ReferenceEquals(GetActiveGrabBehavior(), this) && isGrabbing", detachBody);
    }

    [Fact]
    public void MainPage_PlacementAreaButtons_UseGrabHandCursorBehavior_InsteadOfHoverHand()
    {
        var xaml = ReadRepositoryFile("Praxis", "MainPage.xaml");

        Assert.Equal(1, CountOccurrences(xaml, "<behaviors:GrabHandCursorBehavior />"));
        Assert.Contains("<behaviors:GrabHandCursorBehavior />\n                                        <behaviors:MiddleClickBehavior", xaml);
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
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("new Label { Text = safeMessage },", source);
    }

    [Fact]
    public void App_CreateWindow_HandlerChangeFailures_WarningLogRootPageType()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("errorLogger?.Log(ex, \"Window.HandlerChanged\");", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("var platformViewType = window.Handler?.PlatformView?.GetType().Name ?? \"(null)\";", source);
        Assert.Contains("errorLogger?.LogWarning(", source);
        Assert.Contains("\"Window handler activation failed for root page '{page.GetType().Name}' with platformView='{platformViewType}': {safeMessage}\"", source);
        Assert.Contains("\"Window.HandlerChanged\");", source);
    }

    [Fact]
    public void App_FlushFailures_AreWarningLogged_DuringUnhandledExceptionAndProcessExit()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("TryFlushLogs(TimeSpan.FromSeconds(2), \"AppDomain.UnhandledException\");", source);
        Assert.Contains("TryFlushLogs(TimeSpan.FromSeconds(3), \"App.ProcessExit\");", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("CrashFileLogger.WriteException(context, ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(context, $\"Log flush failed: {safeMessage}\");", source);
    }

    [Fact]
    public void App_GlobalExceptionHandlers_AreRegisteredOnlyOnce()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("private static int globalExceptionHandlersRegistered;", source);
        Assert.Contains("if (Interlocked.Exchange(ref globalExceptionHandlersRegistered, 1) == 0)", source);
        Assert.Contains("var safePayload = CrashFileLogger.SafeObjectDescription(e.ExceptionObject);", source);
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
        Assert.Contains("private static string BuildSyncWarningMessage(string prefix, Exception ex)", source);
        Assert.Contains("return $\"{prefix} ({ex.GetType().Name}) {safeMessage}\";", source);
        Assert.Contains("private static string NormalizePayloadForLog(string payload)", source);
        Assert.Contains("var normalizedPayload = NormalizePayloadForLog(payload);", source);
        Assert.Contains("var normalizedSource = NormalizePayloadForLog(source);", source);
        Assert.Contains("BuildSyncWarningMessage($\"Failed to read sync payload '{normalizedSignalPath}' after retries:\", readFailure)", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileStateSyncNotifier), BuildSyncWarningMessage(\"Unexpected sync publish failure:\", ex));", source);
        Assert.Contains("var warningMessage = BuildMalformedPayloadWarning(signalPath, payload);", source);
        Assert.Contains("private static string BuildMalformedPayloadWarning(string signalPath, string payload)", source);
        Assert.Contains("var normalizedSignalPath = NormalizePayloadForLog(signalPath);", source);
        Assert.Contains("var normalizedPayload = NormalizePayloadForLog(payload);", source);
        Assert.Contains("return $\"Ignored malformed sync payload from '{normalizedSignalPath}': \\\"{normalizedPayload}\\\"\";", source);
        Assert.Contains("CrashFileLogger.WriteInfo(nameof(FileStateSyncNotifier), $\"Signal observed. Source={normalizedSource} TimestampUtc={timestamp:O}\");", source);
    }

    [Fact]
    public void FileStateSyncNotifier_WriteFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileStateSyncNotifier.cs");

        Assert.Contains("var normalizedSignalPath = NormalizePayloadForLog(signalPath);", source);
        Assert.Contains("CrashFileLogger.WriteInfo(nameof(FileStateSyncNotifier), $\"Signal written. Source={instanceId} Path={normalizedSignalPath}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileStateSyncNotifier), BuildSyncWarningMessage($\"Failed to write sync payload '{normalizedSignalPath}':\", ex));", source);
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
        Assert.Contains("private static string NormalizeTargetForLog(string value)", source);
        Assert.Contains("var normalizedToolForLog = NormalizeTargetForLog(tool);", source);
        Assert.Contains("var normalizedUrlForLog = NormalizeTargetForLog(url);", source);
        Assert.Contains("var normalizedArgumentsForLog = NormalizeTargetForLog(arguments);", source);
        Assert.Contains("var normalizedExpandedForLog = NormalizeTargetForLog(expanded);", source);
        Assert.Contains("var pathRooted = Path.IsPathRooted(expanded);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandExecutor), $\"Path not found for '{normalizedExpandedForLog}' while rooted={pathRooted}.\");", source);
        Assert.Contains("private static string BuildFailureMessage(string prefix, Exception ex)", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
    }

    [Fact]
    public void CommandExecutor_FailureBreadcrumbs_ArePresent_ForProcessAndResolutionFailures()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "CommandExecutor.cs");

        Assert.Contains("var warningMessage = BuildFailureMessage($\"Launch target resolution failed for '{normalizedArgumentsForLog}':\", ex);", source);
        Assert.Contains("var resultMessage = BuildFailureMessage(\"Launch target resolution failed:\", ex);", source);
        Assert.Contains("var failureMessage = BuildFailureMessage(failurePrefix, ex);", source);
        Assert.Contains("return Task.FromResult(StartProcess(psi, \"Executed.\", $\"Process launch failed for tool '{normalizedToolForLog}'.\"));", source);
        Assert.Contains("var failureMessage = BuildNoProcessHandleMessage(failurePrefix, startInfo.FileName, startInfo.UseShellExecute);", source);
        Assert.Contains("private static string BuildNoProcessHandleMessage(string failurePrefix, string fileName, bool useShellExecute)", source);
        Assert.Contains("var normalizedFileName = NormalizeTargetForLog(fileName);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandExecutor), warningMessage);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandExecutor), failureMessage);", source);
        Assert.Contains("return $\"{failurePrefix} No process handle was returned for '{normalizedFileName}' while useShellExecute={useShellExecute}.\";", source);
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

        Assert.Equal(4, CountOccurrences(source, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("CrashFileLogger.WriteException(nameof(DbErrorLogger), ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $\"Drain loop failed unexpectedly ({ex.GetType().Name}): {safeMessage}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $\"Flush failed unexpectedly ({ex.GetType().Name}): {safeMessage}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $\"Failed to purge old error logs after persisting '{entry.Context}' ({ex.GetType().Name}): {safeMessage}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $\"Failed to persist {entry.Level} log for '{entry.Context}': {safeMessage}\");", source);
    }

    [Fact]
    public void WindowsCommandEntryHandler_DisablesInputScopeAfterCompatibilityException()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "Windows", "Handlers", "CommandEntryHandler.cs");

        Assert.Contains("catch (Exception ex) when (WindowsInputScopeCompatibilityPolicy.ShouldDisableInputScopeOnException(ex))", source);
        Assert.Contains("catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)", source);
        Assert.Contains("inputScopeUnsupported = true;", source);
        Assert.Equal(2, CountOccurrences(source, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Equal(2, CountOccurrences(source, "var enforceAsciiInput = (VirtualView as CommandEntry)?.EnforceAsciiInput ?? false;"));
        Assert.Equal(2, CountOccurrences(source, "var textBoxType = textBox.GetType().Name;"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $\"InputScope assignment disabled after compatibility failure while enforceAsciiInput={enforceAsciiInput} textBoxType={textBoxType}: {safeMessage}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $\"InputScope assignment failed unexpectedly while enforceAsciiInput={enforceAsciiInput} textBoxType={textBoxType}: {safeMessage}\");", source);
        Assert.Contains("catch", source);
        Assert.Contains("return false;", source);
    }

    [Fact]
    public void MainPage_WindowsReflectionAndFocusFallbackFailures_AreWarningLogged()
    {
        var focusSource = ReadRepositoryFile("Praxis", "MainPage.FocusAndContext.cs");
        var pointerSource = ReadRepositoryFile("Praxis", "MainPage.PointerAndSelection.cs");
        var layoutSource = ReadRepositoryFile("Praxis", "MainPage.LayoutUtilities.cs");

        Assert.Contains("var controlType = control.GetType().Name;", focusSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", focusSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DisableWindowsSystemFocusVisual), $\"Failed to disable UseSystemFocusVisuals on {controlType}: {safeMessage}\");", focusSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", pointerSource);
        Assert.Contains("var shouldSelectAll = modalPrimaryFieldSelectAllPending;", pointerSource);
        Assert.Contains("var modalVisible = EditorOverlay.IsVisible;", pointerSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FocusModalPrimaryEditorField), $\"Failed to focus modal ButtonText entry while shouldSelectAll={shouldSelectAll} modalVisible={modalVisible}: {safeMessage}\");", pointerSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", layoutSource);
        Assert.Contains("var targetType = platformView.GetType().Name;", layoutSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(SetTabStop), $\"Failed to set IsTabStop={isTabStop} on {targetType}: {safeMessage}\");", layoutSource);
    }

    [Fact]
    public void WindowsStartupLog_UsesNormalizedAppStorageRoot_AndGuardsDuplicateHookRegistration()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "Windows", "App.xaml.cs");

        Assert.Contains("AppStoragePaths.WindowsLocalAppDataRoot", source);
        Assert.Contains("private static bool globalExceptionLoggingHooked;", source);
        Assert.Contains("if (globalExceptionLoggingHooked)", source);
        Assert.Contains("var safePayload = CrashFileLogger.SafeObjectDescription(e.ExceptionObject);", source);
        Assert.Contains("var payloadType = e.ExceptionObject?.GetType().FullName ?? \"null\";", source);
        Assert.Contains("var payload = $\"Non-Exception object thrown (IsTerminating={e.IsTerminating}, Type={payloadType}): {safePayload}\";", source);
        Assert.Contains("var content = BuildStartupExceptionLogContent(source, exception);", source);
        Assert.Contains("var content = BuildStartupMessageLogContent(source, message);", source);
        Assert.Contains("sb.Append(CrashFileLogger.FormatExceptionPayload(exception));", source);
        Assert.Contains("private static string BuildStartupMessageLogContent(string source, string message)", source);
        Assert.Contains("AppendStartupLogContent(content);", source);
        Assert.DoesNotContain("exception.ToString()", source, StringComparison.Ordinal);
        Assert.Equal(5, CountOccurrences(source, "SecondaryFailureLogger.ReportStartupLogFailure("));
        Assert.Contains("SecondaryFailureLogger.ReportStartupLogFailure(", source);
        Assert.Contains("\"Failed to create startup log directory\"", source);
        Assert.Contains("\"Failed to append startup log\"", source);
        Assert.Contains("\"Failed to build startup log payload for\"", source);
        Assert.DoesNotContain("CrashFileLogger.WriteWarning(nameof(App), $\"Failed to append startup log", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SecondaryFailureLogger_PreservesOriginalStartupMessages_EvenWhenWhitespaceOnly()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "SecondaryFailureLogger.cs");

        Assert.Contains("private static string NormalizePathForLog(string path)", source);
        Assert.Contains("private static string NormalizeOperationForLog(string value)", source);
        Assert.Contains("var normalizedTargetPath = NormalizePathForLog(targetPath);", source);
        Assert.Contains("var normalizedOperation = NormalizeOperationForLog(operationDescription);", source);
        Assert.Contains("var warningMessage = $\"{normalizedOperation} '{normalizedTargetPath}': {safeMessage}\";", source);
        Assert.Contains("else if (originalMessage is not null)", source);
        Assert.Contains("CrashFileLogger.NormalizeMessagePayload(originalMessage)", source);
        Assert.DoesNotContain("else if (!string.IsNullOrWhiteSpace(originalMessage))", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MacAppDelegate_GuardsDuplicateGlobalExceptionHookRegistration()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "AppDelegate.cs");

        Assert.Contains("private static bool globalExceptionLoggingHooked;", source);
        Assert.Contains("if (globalExceptionLoggingHooked)", source);
        Assert.Contains("globalExceptionLoggingHooked = true;", source);
        Assert.Contains("var safePayload = CrashFileLogger.SafeObjectDescription(e.ExceptionObject);", source);
        Assert.Contains("var payloadType = e.ExceptionObject?.GetType().FullName ?? \"null\";", source);
        Assert.Equal(2, CountOccurrences(source, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("\"Non-Exception object thrown (IsTerminating={e.IsTerminating}, Type={payloadType}): {safePayload}\"", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(AppDelegate), $\"Failed to hook MarshalManagedException: {safeMessage}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(AppDelegate), $\"Failed to prioritize key command '{selectorName}': {safeMessage}\");", source);
    }

    [Fact]
    public void MainViewModel_ExternalThemeSync_DispatchFailuresAreLogged()
    {
        var source = ReadRepositoryFile("Praxis", "ViewModels", "MainViewModel.cs");

        Assert.Contains("var currentThemeForLog = SelectedTheme;", source);
        Assert.Contains("errorLogger.Log(ex, nameof(SyncThemeFromExternalChangeAsync));", source);
        Assert.Contains("BuildSafeWarningMessage($\"External theme sync dispatch failed for theme {latestTheme}\", ex)", source);
        Assert.Contains("BuildSafeWarningMessage($\"External theme sync failed for theme {currentThemeForLog}\", ex)", source);
        Assert.Contains("BuildSafeWarningMessage(\"External reload failed\", ex)", source);
        Assert.Contains("TaskCreationOptions.RunContinuationsAsynchronously", source);
        Assert.Contains("private static string BuildSafeWarningMessage(string prefix, Exception ex)", source);
        Assert.Contains("private static string BuildSafeWarningMessage(Func<Exception, string> warningFactory, Exception ex)", source);
    }

    [Fact]
    public void MainViewModel_CommandSuggestionDispatchFailures_AreLogged()
    {
        var source = ReadRepositoryFile("Praxis", "ViewModels", "MainViewModel.CommandSuggestions.cs");

        Assert.Contains("errorLogger.Log(ex, nameof(DebouncedRefreshCommandSuggestionsAsync));", source);
        Assert.Contains("errorLogger.Log(ex, nameof(RefreshCommandSuggestionsOnMainThread));", source);
        Assert.Contains("var commandInputLength = CommandInput?.Length ?? 0;", source);
        Assert.Contains("BuildSafeWarningMessage($\"Debounced command suggestion refresh failed for input length {commandInputLength}\", ex)", source);
        Assert.Contains("BuildSafeWarningMessage($\"Command suggestion close dispatch failed for input length {commandInputLength}\", dispatchEx)", source);
        Assert.Contains("var valueLength = value?.Length ?? 0;", source);
        Assert.Contains("BuildSafeWarningMessage($\"Command suggestion refresh dispatch failed for input length {valueLength}\", ex)", source);
        Assert.Contains("BuildSafeWarningMessage($\"Command suggestion refresh failed for input length {value?.Length ?? 0}\", ex)", source);
        Assert.Contains("BuildSafeWarningMessage($\"Command lookup fallback failed for input length {cmd.Length}\", ex)", source);
        Assert.Contains("catch (OperationCanceledException) when (token.IsCancellationRequested)", source);
    }

    [Fact]
    public void MainViewModel_ActionsWarningHelpers_UseSafeWarningMessageBuilder()
    {
        var source = ReadRepositoryFile("Praxis", "ViewModels", "MainViewModel.Actions.cs");

        Assert.Contains("private async Task<string> TryGetClipboardTextAsync(string context, string operation)", source);
        Assert.Contains("async () => await clipboardService.GetTextAsync() ?? string.Empty,", source);
        Assert.Contains("BuildSafeWarningMessage(\"Conflict resolution callback failed\", ex)", source);
        Assert.Contains("ex => BuildSafeWarningMessage($\"{operation} failed\", ex)", source);
        Assert.Contains("ex => BuildSafeWarningMessage($\"{operation} completed locally, but window sync notification failed\", ex)", source);
        Assert.Contains("ex => BuildSafeWarningMessage($\"{operation} applied locally, but theme persistence failed\", ex)", source);
        Assert.Contains("ex => BuildSafeWarningMessage($\"{operation} completed locally, but dock persistence failed\", ex)", source);
        Assert.Equal(2, CountOccurrences(source, "errorLogger.LogWarning(BuildSafeWarningMessage(warningFactory, ex), context);"));
    }

    [Fact]
    public void AppStoragePaths_LegacyMigrationFailures_AreWarningLogged_AndSkipped()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "AppStoragePaths.cs");

        Assert.Contains("private static string NormalizePathForLog(string path)", source);
        Assert.Contains("var normalizedSourcePath = NormalizePathForLog(sourcePath);", source);
        Assert.Contains("var normalizedLeft = NormalizePathForLog(left);", source);
        Assert.Contains("var normalizedRight = NormalizePathForLog(right);", source);
        Assert.Contains("private static string BuildSafeWarningMessage(string prefix, Exception ex)", source);
        Assert.Contains("=> $\"{prefix} ({ex.GetType().Name}): {CrashFileLogger.SafeExceptionMessage(ex)}\";", source);
        Assert.Equal(2, CountOccurrences(source, "CrashFileLogger.WriteWarning(nameof(AppStoragePaths), BuildSafeWarningMessage($\"Legacy database migration failed from '{normalizedSourcePath}'\", ex));"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(AppStoragePaths), BuildSafeWarningMessage($\"Ignoring invalid migration path comparison between '{normalizedLeft}' and '{normalizedRight}'\", ex));", source);
    }

    [Fact]
    public void FileAppConfigService_FallsBackOnUnauthorizedAccess()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileAppConfigService.cs");
        Assert.Contains("catch (UnauthorizedAccessException ex)", source);
        Assert.Contains("private static string NormalizePathForLog(string path)", source);
        Assert.Contains("var normalizedPath = NormalizePathForLog(path);", source);
        Assert.Contains("private static void WriteSkippedConfigWarning(string path, Exception ex)", source);
        Assert.Contains("var exceptionType = ex.GetType().Name;", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileAppConfigService), $\"Skipping config '{normalizedPath}' after {exceptionType}: {safeMessage}\");", source);
        Assert.Contains("var normalizedTheme = NormalizeThemeForLog(config?.Theme);", source);
        Assert.Contains("$\"Skipping config '{normalizedPath}' because it does not specify a valid theme. Value='{normalizedTheme}'.\"", source);
    }

    [Fact]
    public void MauiThemeService_SkipsNoOpApplies_AndCrashLogsDispatchFailures()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "MauiThemeService.cs");

        Assert.Contains("if (Application.Current.UserAppTheme == appTheme)", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MauiThemeService), $\"ApplyMacWindowStyle dispatch failed for theme '{appTheme}' while currentTheme='{currentTheme}': {safeMessage}\");", source);
    }

    [Fact]
    public void MacProgram_RelayFailures_AreWarningLogged_AndNullProcessIsRejected()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Program.cs");

        Assert.Contains("private static string NormalizePathForLog(string path)", source);
        Assert.Equal(2, CountOccurrences(source, "var normalizedBundlePath = NormalizePathForLog(bundlePath);"));
        Assert.Equal(2, CountOccurrences(source, "var normalizedRelayExecutable = NormalizePathForLog(relayExecutable);"));
        Assert.Contains("var process = Process.Start(startInfo);", source);
        Assert.Contains("if (process is null)", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(Program), $\"LaunchServices relay returned no process for bundle '{normalizedBundlePath}' via '{normalizedRelayExecutable}'.\");", source);
        Assert.Contains("var normalizedRelayArg = NormalizePathForLog(OpenRelayArg);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(Program), $\"LaunchServices relay failed for bundle '{normalizedBundlePath}' via '{normalizedRelayExecutable}' with relayArg='{normalizedRelayArg}': {safeMessage}\");", source);
    }

    [Fact]
    public void MacHandlers_KeyInputResolutionFailures_AreWarningLogged_AndFallBack()
    {
        var macEntrySource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "MacEntryHandler.cs");
        var macEditorSource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "MacEditorHandler.cs");
        var commandEntrySource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "CommandEntryHandler.cs");

        Assert.Contains("return TryResolveKeyInput(inputName, fallback) ?? fallback;", macEntrySource);
        Assert.Contains("var normalizedInputName = CrashFileLogger.NormalizeMessagePayload(inputName);", macEntrySource);
        Assert.Contains("var normalizedFallback = DescribeKeyInputFallbackForLog(fallbackForLog);", macEntrySource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", macEntrySource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MacEntryHandler), $\"Failed to resolve UIKeyCommand input '{normalizedInputName}' with fallback '{normalizedFallback}': {safeMessage}\");", macEntrySource);
        Assert.Contains("\"\\t\" => \"Tab\",", macEntrySource);
        Assert.Contains("\"\\u001B\" => \"Escape\",", macEntrySource);
        Assert.Contains("\"\\r\" => \"Return\",", macEntrySource);
        Assert.Contains("\"\\uF700\" => \"UpArrow\",", macEntrySource);
        Assert.Contains("\"\\uF701\" => \"DownArrow\",", macEntrySource);
        Assert.Contains("\"\\uF702\" => \"LeftArrow\",", macEntrySource);
        Assert.Contains("\"\\uF703\" => \"RightArrow\",", macEntrySource);

        Assert.Contains("return TryResolveKeyInput(inputName, fallback) ?? fallback;", macEditorSource);
        Assert.Contains("var normalizedInputName = CrashFileLogger.NormalizeMessagePayload(inputName);", macEditorSource);
        Assert.Contains("var normalizedFallback = DescribeKeyInputFallbackForLog(fallbackForLog);", macEditorSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", macEditorSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MacEditorHandler), $\"Failed to resolve UIKeyCommand input '{normalizedInputName}' with fallback '{normalizedFallback}': {safeMessage}\");", macEditorSource);
        Assert.Contains("\"\\t\" => \"Tab\",", macEditorSource);
        Assert.Contains("\"\\u001B\" => \"Escape\",", macEditorSource);
        Assert.Contains("\"\\uF702\" => \"LeftArrow\",", macEditorSource);
        Assert.Contains("\"\\uF703\" => \"RightArrow\",", macEditorSource);

        Assert.Contains("return TryResolveKeyInput(inputName, fallback) ?? fallback;", commandEntrySource);
        Assert.Contains("var normalizedInputName = CrashFileLogger.NormalizeMessagePayload(inputName);", commandEntrySource);
        Assert.Contains("var normalizedFallback = DescribeKeyInputFallbackForLog(fallbackForLog);", commandEntrySource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", commandEntrySource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $\"Failed to resolve UIKeyCommand input '{normalizedInputName}' with fallback '{normalizedFallback}': {safeMessage}\");", commandEntrySource);
    }

    [Fact]
    public void MacMiddleClickAndKeyCommandFallbackFailures_AreWarningLogged()
    {
        var behaviorSource = ReadRepositoryFile("Praxis", "Behaviors", "MiddleClickBehavior.cs");
        var macSource = ReadRepositoryFile("Praxis", "MainPage.MacCatalystBehavior.cs");

        Assert.Equal(2, CountOccurrences(behaviorSource, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MiddleClickBehavior), $\"Failed to set buttonMaskRequired={mask}: {safeMessage}\");", behaviorSource);
        Assert.Contains("var isContextMenuOpen = IsContextMenuCurrentlyOpen();", behaviorSource);
        Assert.Contains("var hasCommand = Command is not null;", behaviorSource);
        Assert.Contains("var associatedObjectType = attachedView?.GetType().Name ?? \"(null)\";", behaviorSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MiddleClickBehavior), $\"Deferred middle-click execution failed while contextMenuOpen={isContextMenuOpen} hasCommand={hasCommand} associatedObjectType={associatedObjectType}: {safeMessage}\");", behaviorSource);
        Assert.Equal(2, CountOccurrences(macSource, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(TryCreateMacEditorKeyCommand), $\"Failed to create Mac editor key command '{selectorName}' for input '{keyInput}': {safeMessage}\");", macSource);
        Assert.Contains("var isActive = App.IsMacApplicationActive();", macSource);
        Assert.Contains("var activationSuppressed = App.IsActivationSuppressionActive();", macSource);
        Assert.Contains("var pointerKnown = macLastActivePage is not null &&", macSource);
        Assert.Contains("page.lastPointerOnRoot is not null;", macSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(IsMacMiddleButtonCurrentlyDown), $\"Failed to query middle button state from CoreGraphics while isActive={isActive} activationSuppressed={activationSuppressed} pointerKnown={pointerKnown}: {safeMessage}\");", macSource);
    }

    [Fact]
    public void MainPage_CopyNoticeAnimationFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Contains("catch (OperationCanceledException) when (token.IsCancellationRequested)", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(\"MainPage.CopyIconButton_Clicked\", $\"Copy notice animation failed while overlayVisible={CopyNoticeOverlay.IsVisible} tokenCanceled={token.IsCancellationRequested}: {safeMessage}\");", source);
    }

    [Fact]
    public void MainPage_StatusFlashAnimationFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.StatusAndTheme.cs");
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("var messageLength = message?.Length ?? 0;", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(TriggerStatusFlash), $\"Status flash animation failed for message length {messageLength} (isError={StatusFlashErrorPolicy.IsErrorStatus(message)}): {safeMessage}\");", source);
    }

    [Fact]
    public void MainPage_DockHoverExitFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.DockAndQuickLook.cs");
        Assert.Contains("catch (OperationCanceledException) when (token.IsCancellationRequested)", source);
        Assert.Equal(3, CountOccurrences(source, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(HideDockScrollBarAfterExitDelayAsync), $\"Dock hover-exit hide failed while pointerHover={isDockPointerHovering}: {safeMessage}\");", source);
        Assert.True(
            source.IndexOf("SetDockScrollBarVisibility(isPointerOverDockRegion: false);", StringComparison.Ordinal)
            < source.IndexOf("CrashFileLogger.WriteWarning(nameof(HideDockScrollBarAfterExitDelayAsync), $\"Dock hover-exit hide failed while pointerHover={isDockPointerHovering}: {safeMessage}\");", StringComparison.Ordinal));
    }

    [Fact]
    public void MainPage_QuickLookShowFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.DockAndQuickLook.cs");
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(ShowQuickLookAfterDelayAsync), $\"Quick Look show failed for item '{item.Id}' while popupVisible={QuickLookPopup.IsVisible}: {safeMessage}\");", source);
        Assert.True(
            source.IndexOf("QuickLookPopup.CancelAnimations();", StringComparison.Ordinal)
            < source.IndexOf("CrashFileLogger.WriteWarning(nameof(ShowQuickLookAfterDelayAsync), $\"Quick Look show failed for item '{item.Id}' while popupVisible={QuickLookPopup.IsVisible}: {safeMessage}\");", StringComparison.Ordinal));
    }

    [Fact]
    public void MainPage_QuickLookHideFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.DockAndQuickLook.cs");
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(HideQuickLookAfterDelayAsync), $\"Quick Look hide failed for pending item '{quickLookPendingItemId}': {safeMessage}\");", source);
        Assert.True(
            source.IndexOf("QuickLookPopup.IsVisible = false;", StringComparison.Ordinal)
            < source.IndexOf("CrashFileLogger.WriteWarning(nameof(HideQuickLookAfterDelayAsync), $\"Quick Look hide failed for pending item '{quickLookPendingItemId}': {safeMessage}\");", StringComparison.Ordinal));
    }

    [Fact]
    public void MainPage_ButtonTapExecutionFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.EditorAndInput.cs");
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(Draggable_Tapped), $\"Button tap execution failed for '{item.ButtonText}' ({item.Id}): {safeMessage}\");", source);
    }

    [Fact]
    public void MainPage_SecondaryTapCreateFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.PointerAndSelection.cs");
        Assert.Equal(2, CountOccurrences(source, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(PlacementCanvas_SecondaryTapped), $\"Secondary-tap create flow failed at ({canvasPoint.X:0.##}, {canvasPoint.Y:0.##}): {safeMessage}\");", source);
        Assert.Contains("var modalVisible = EditorOverlay.IsVisible;", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FocusModalPrimaryEditorField), $\"Failed to focus modal ButtonText entry while shouldSelectAll={shouldSelectAll} modalVisible={modalVisible}: {safeMessage}\");", source);
    }

    [Fact]
    public void MainPage_WindowsFocusFallbackFailures_AreWarningLogged()
    {
        var focusSource = ReadRepositoryFile("Praxis", "MainPage.FocusAndContext.cs");
        var layoutSource = ReadRepositoryFile("Praxis", "MainPage.LayoutUtilities.cs");

        Assert.Contains("var controlType = control.GetType().Name;", focusSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", focusSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DisableWindowsSystemFocusVisual), $\"Failed to disable UseSystemFocusVisuals on {controlType}: {safeMessage}\");", focusSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", layoutSource);
        Assert.Contains("var targetType = platformView.GetType().Name;", layoutSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(SetTabStop), $\"Failed to set IsTabStop={isTabStop} on {targetType}: {safeMessage}\");", layoutSource);
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
