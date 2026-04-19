# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [Unreleased]

### Fixed
- `SecondaryFailureLogger` now normalizes secondary fallback sink roots to absolute directories before combining the `Praxis/secondary-failures.log` path, so quoted absolute overrides still work while blank or relative roots are ignored instead of creating accidental relative diagnostics paths
- `FileStateSyncNotifier` now includes the normalized sync-file path in the read-after-retries warning breadcrumb, so exhausted watcher read retries identify which `buttons.sync` target failed instead of logging only the exception summary
- `MauiThemeService` now includes the target `AppTheme` in the Mac dispatch-failure warning breadcrumb, so a failed Light/Dark/System transition leaves more specific diagnostics than a generic window-style warning
- `FileAppConfigService` now includes the normalized raw `theme` value in invalid-theme warning breadcrumbs, so broken `praxis.config.json` files leave evidence of the actual bad value instead of only saying the theme was invalid
- `FileAppConfigService` now canonicalizes absolute config candidate roots before deduplicating them, so equivalent paths with `.` or `..` segments do not probe the same `praxis.config.json` twice
- `App.xaml.cs` now warning-logs `Window.HandlerChanged` failures with the root page type, so Windows activation-hook failures identify which shell failed instead of leaving only an exception row
- `MainPage` now includes the hovered button `item.Id` in Quick Look delayed-show warning breadcrumbs, so failed preview popups identify which button context faulted instead of logging only the exception summary
- `CommandExecutor` now warning-logs normalized missing filesystem targets before returning `Path not found: ...`, so empty-tool fallback misses still leave a crash-log breadcrumb
- `MainViewModel.CommandSuggestions` now includes the current command-input length in debounce/close-dispatch warning breadcrumbs, so degraded suggestion refreshes leave context without persisting the full input text
- `MainPage` now includes the pending button `item.Id` in Quick Look delayed-hide warning breadcrumbs, so popup teardown failures keep the same button-level context as delayed-show failures
- `MainPage` now includes the current Dock hover flag in hover-exit hide warning breadcrumbs, so delayed scrollbar teardown failures show whether the pointer had already re-entered
- `MainPage.CopyIconButton_Clicked` now includes the copy-notice overlay visibility in animation warning breadcrumbs, so failed notification teardown leaves a quick state hint instead of only the exception summary
- `MainPage.TriggerStatusFlash` now includes the status-text length and error classification in animation warning breadcrumbs, so degraded flash paths keep lightweight context without logging the full message
- `MainViewModel` now includes the target theme in external theme-sync warning breadcrumbs, so dispatch or apply failures keep the intended `ThemeMode`
- `MainViewModel.CommandSuggestions` now includes input length in refresh-dispatch warning breadcrumbs too, so all dispatch-side suggestion warnings use the same lightweight context pattern

### Tests
- Expanded `SecondaryFailureLoggerTests` to cover quoted absolute fallback roots and rejection of relative fallback roots before the startup-diagnostics file path is built
- Expanded `FileStateSyncNotifierTests` and `AppLayerSourceGuardTests` to cover the normalized sync-file path prefix in read-retry exhaustion warnings
- Expanded `AppLayerSourceGuardTests` to lock the `MauiThemeService` Mac dispatch-failure breadcrumb to the requested `AppTheme`
- Expanded `FileAppConfigServiceTests` and `AppLayerSourceGuardTests` to cover normalized invalid-theme values in skipped-config warnings
- Expanded `FileAppConfigServiceTests` to cover canonical deduplication of equivalent absolute config roots and dot-segment normalization in `NormalizeAbsoluteDirectory(...)`
- Expanded `AppLayerSourceGuardTests` to lock `App.xaml.cs` `Window.HandlerChanged` warnings to the root page type
- Expanded `AppLayerSourceGuardTests` to lock Quick Look delayed-show warnings to the hovered `item.Id`
- Expanded `CommandExecutorTests` and `AppLayerSourceGuardTests` to cover missing-path warning breadcrumbs in the empty-tool fallback path
- Expanded `AppLayerSourceGuardTests` to lock command-suggestion debounce/close-dispatch warnings to the current input length
- Expanded `AppLayerSourceGuardTests` to lock Quick Look delayed-hide warnings to the pending `item.Id`
- Expanded `AppLayerSourceGuardTests` to lock Dock hover-exit warnings to the current Dock hover flag
- Expanded `AppLayerSourceGuardTests` to lock copy-notice animation warnings to the overlay visibility state
- Expanded `AppLayerSourceGuardTests` to lock status-flash animation warnings to message-length and error-classification context
- Expanded `AppLayerSourceGuardTests` to lock external theme-sync warnings to the target theme
- Expanded `AppLayerSourceGuardTests` to lock command-suggestion refresh-dispatch warnings to the input length

### [1.1.11] - 2026-04-17

### Changed
- `MainViewModel` now records an explicit `ClearCommandInput` breadcrumb (cleared-length or no-op) and an `ExecuteCommandInputAsync` breadcrumb (command length plus whether a suggestion was selected), so diagnosing a crash that follows the command-input path no longer requires reconstructing intent from the surrounding handler-side tap log
- `MainPage.FocusEntryAfterClearButtonTap` / `ApplyEntryFocusAfterClearButtonTap` now emit entry/retry and target/outcome markers to `crash.log`, and `Entry.Focus()` itself is wrapped in try/catch so a failure inside the MAUI handler surfaces as a crash-file exception instead of a silent process termination
- `MainViewModel.ClearSearchText` now mirrors the `ClearCommandInput` breadcrumb (cleared-length vs. no-op), so the search-side X-button path has the same ViewModel-level evidence as the command-side path when a crash follows the tap

### [1.1.10] - 2026-04-15

### Fixed
- `CommandWorkingDirectoryPolicy` now treats Windows shell executable names case-insensitively, so uppercase or mixed-case `cmd.exe` / `powershell.exe` / `pwsh.exe` / `wt.exe` paths still switch `WorkingDirectory` to the user profile instead of inheriting the Praxis process directory
- `LaunchTargetResolver` now preserves valid path targets whose first or last character is a quote, normalizes env-expanded quoted rooted/home/relative and `file://` path prefixes, and explicitly excludes quoted non-file URI-scheme prefixes so malformed quoted URLs still fail closed
- `CrashFileLogger` now keeps crash-log records alive even when custom exception `Message` / `StackTrace` getters or `Exception.Data` key/value `ToString()` implementations throw, and exception messages are flattened to single-line output so malformed payloads do not corrupt inline log formatting
- `CrashFileLogger.SafeExceptionMessage(...)` now normalizes whitespace-only exception messages to `(empty)`, so warning breadcrumbs no longer degrade into blank trailing separators when the underlying exception body is present but empty
- `DbErrorLogger` now persists the same single-line exception-message normalization and getter-failure fallback markers into `ErrorLogEntity`, and app/process-exit flush failures plus Windows startup-log write failures now record the full exception body before their warning breadcrumb, falling back to an independent temp/current-directory diagnostics file when the normal `%LOCALAPPDATA%\\Praxis` crash sink is unavailable
- `SecondaryFailureLogger` now also normalizes logged startup target-path and operation fragments before interpolating them into fallback warning/body lines, so malformed startup-log metadata cannot break the secondary diagnostics file
- `MainViewModel` warning paths now use the same safe exception-message helper when external theme sync, command-suggestion refresh/lookup, conflict callbacks, clipboard helpers, sync notifications, or local persistence follow-up logging encounter hostile exception `Message` getters, so degraded warning logging no longer rethrows out of those recovery paths
- `AppStoragePaths` now uses the same safe exception-message helper for legacy database migration and invalid-path-comparison warnings, so startup migration keeps skipping bad candidates even when an exception's `Message` getter is hostile
- `AppStoragePaths` now also normalizes logged migration source/comparison path fragments before interpolating them into warning prefixes, so malformed path text cannot break crash-log line structure during legacy migration
- `FileAppConfigService` now uses the same safe exception-message helper when skipped config reads throw `IOException` / `UnauthorizedAccessException` / `JsonException`, so warning logging still persists a breadcrumb even if the exception's `Message` getter is hostile
- `FileAppConfigService` now also normalizes logged config path fragments before interpolating them into invalid-theme / skipped-config breadcrumbs, so newline-bearing candidate paths cannot break crash-log line structure
- `CommandExecutor` now uses the same safe exception-message helper for launch-target resolution and native process-start failure messages, so fallback warning/result construction no longer rethrows on hostile exception `Message` getters
- `CommandExecutor` now also normalizes logged tool / URL / path / argument fragments before interpolating them into failure prefixes, so newline-bearing launch targets cannot break crash-log breadcrumb formatting
- `MauiThemeService` now uses the same safe exception-message helper for Mac dispatch-failure breadcrumbs, so theme-apply warning logging no longer rethrows on hostile exception `Message` getters
- `FileStateSyncNotifier` now routes write/read/unexpected publish warning construction through the same safe exception-message helper, so sync breadcrumbs survive hostile exception `Message` getters
- `FileStateSyncNotifier` now also normalizes malformed payload and observed-source fragments before interpolating them into crash-log warning/info lines, so sync breadcrumbs stay single-line even if the sync file contains embedded newlines or whitespace-only payload markers
- `FileStateSyncNotifier` now also normalizes sync-file path fragments before interpolating them into write-success/write-failure breadcrumbs, so malformed storage paths cannot break crash-log line structure
- `Windows CommandEntryHandler` now uses the same safe exception-message helper for compatibility-triggered and unexpected `InputScope` assignment warnings, so WinUI fallback logging no longer rethrows on hostile exception `Message` getters
- Mac `AppDelegate` and the Mac entry/editor/command handlers now use the same safe exception-message helper for `MarshalManagedException` hook, key-command-priority, and `UIKeyCommand` input-resolution warning breadcrumbs, so those fallback paths no longer rethrow on hostile exception `Message` getters
- Mac entry/editor/command handlers now also normalize reflected `UIKeyCommand` input names before interpolating them into warning breadcrumbs, so malformed reflection metadata cannot break crash-log line structure
- Mac `Program` now uses the same safe exception-message helper for LaunchServices relay failure breadcrumbs, so open-relay warning logging no longer rethrows on hostile exception `Message` getters
- Mac `Program` now also normalizes logged LaunchServices bundle-path fragments before interpolating them into relay breadcrumbs, so malformed bundle paths cannot break crash-log line structure
- `MiddleClickBehavior` and `MainPage.MacCatalystBehavior` now use the same safe exception-message helper for `buttonMaskRequired`, deferred middle-click execution, Mac editor key-command creation, and CoreGraphics fallback warning breadcrumbs, so those degraded Mac input paths no longer rethrow on hostile exception `Message` getters
- `MainPage` now uses the same safe exception-message helper for copy-notice, status-flash, Dock hover-exit, and Quick Look animation warning breadcrumbs, so those non-fatal UI recovery paths no longer rethrow on hostile exception `Message` getters
- `MainPage` now also routes button-tap execution, secondary-tap create flow, modal primary-focus fallback, `UseSystemFocusVisuals`, and `IsTabStop` warning breadcrumbs through the same safe exception-message helper, so more Windows/UI fallback paths no longer rethrow on hostile exception `Message` getters
- `MainPage` and `App` now route fallback initialization UI text through `CrashFileLogger.SafeExceptionMessage(...)`, so hostile exception `Message` getters cannot break the last-resort error page / alert surface itself
- `CrashFileLogger.SafeObjectDescription(...)` now hardens non-`Exception` `AppDomain.UnhandledException` payloads, so hostile object `ToString()` implementations cannot break the last-resort global exception path in base `App`, Windows startup logging, or Mac `AppDelegate`

### Tests
- Expanded `CommandWorkingDirectoryPolicyTests` to cover mixed-case shell executable names and uppercase env-expanded shell paths
- Expanded `LaunchTargetResolverTests` to cover quoted relative/`file://` path prefixes, quoted-boundary path names, and malformed quoted URL handling both before and after env expansion
- Expanded `CrashFileLoggerTests`, `DbErrorLoggerTests`, `SecondaryFailureLoggerTests`, and `AppLayerSourceGuardTests` to cover multiline exception-message normalization, throwing custom exception getters / data formatters, and startup-log failure diagnostics that fall back to an independent file when the primary crash sink cannot be written
- Expanded `CrashFileLoggerTests` to cover direct source/context normalization helpers alongside persisted crash-breadcrumb behavior
- Expanded `CrashFileLoggerTests` to cover direct null `NormalizeSource(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover direct null `NormalizeContext(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover direct `NormalizeMessagePayload(...)` helper behavior alongside persisted crash-breadcrumb behavior
- Expanded `CrashFileLoggerTests` to cover multiline/whitespace-only getter-failure messages inside `SafeExceptionMessage(...)` fallback markers
- Expanded `CrashFileLoggerTests` to cover direct multiline `NormalizeExceptionMessage(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover whitespace-only `NormalizeExceptionMessage(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover null `NormalizeExceptionMessage(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover direct null `SafeObjectDescription(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover whitespace-only object-formatter failure markers inside `SafeObjectDescription(...)`
- Expanded `CrashFileLoggerTests` to cover multiline object-formatter failure markers inside `SafeObjectDescription(...)`
- Expanded `CrashFileLoggerTests` to cover direct empty-stack `SafeExceptionStackTrace(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover multiline stack-trace getter-failure markers inside `SafeExceptionStackTrace(...)`
- Expanded `CrashFileLoggerTests` to cover whitespace-only stack-trace getter-failure markers inside `SafeExceptionStackTrace(...)`
- Expanded `SecondaryFailureLoggerTests` to cover direct null target-path/operation normalization helper behavior
- Expanded `SecondaryFailureLoggerTests` to cover the `false` / null-path result when both fallback sink roots are blocked
- Expanded `DbErrorLoggerTests` to cover normalized multiline contexts inside persist-failure breadcrumbs
- Expanded `DbErrorLoggerTests` to cover normalized multiline contexts inside Warning persist-failure breadcrumbs
- Expanded `DbErrorLoggerTests` to cover normalized multiline contexts inside Info persist-failure breadcrumbs
- Expanded `DbErrorLoggerTests` to cover normalized multiline contexts inside purge-failure breadcrumbs
- Expanded `SecondaryFailureLoggerTests` and `AppLayerSourceGuardTests` to cover startup target-path/operation normalization before those fragments reach fallback diagnostics
- Expanded `SecondaryFailureLoggerTests` to cover direct target-path normalization helper behavior
- Expanded `SecondaryFailureLoggerTests` to cover whitespace-only target-path normalization helper behavior
- Expanded `SecondaryFailureLoggerTests` to cover multiline operation normalization helper behavior
- Expanded `MainViewModelWorkflowIntegrationTests` and `AppLayerSourceGuardTests` to cover hostile exception-message getters on `MainViewModel` warning paths, including external theme sync, command lookup fallback, conflict callbacks, clipboard follow-up logging, sync notifications, and theme persistence
- Expanded `AppLayerSourceGuardTests` to cover normalized reflected `UIKeyCommand` input names before Mac key-input warning breadcrumbs are assembled
- Expanded `AppStoragePathsTests` and `AppLayerSourceGuardTests` to cover hostile exception-message getters on legacy migration warning construction
- Expanded `AppStoragePathsTests` and `AppLayerSourceGuardTests` to cover migration source/comparison path normalization before legacy-migration breadcrumbs are written
- Expanded `AppStoragePathsTests` to cover direct null logged-path normalization inside `NormalizePathForLog(...)`
- Expanded `FileAppConfigServiceTests` and `AppLayerSourceGuardTests` to cover hostile exception-message getters on skipped-config warning construction
- Expanded `FileAppConfigServiceTests` and `AppLayerSourceGuardTests` to cover config-path normalization before invalid-theme / skipped-config breadcrumbs are written
- Expanded `FileAppConfigServiceTests` to cover direct null logged-config-path normalization inside `NormalizePathForLog(...)`
- Expanded `FileAppConfigServiceTests` to cover candidate enumeration rejecting blank/relative config roots
- Expanded `FileAppConfigServiceTests` to cover direct null `NormalizeAbsoluteDirectory(...)` helper behavior
- Expanded `FileAppConfigServiceTests` to cover quoted-relative rejection inside `NormalizeAbsoluteDirectory(...)`
- Expanded `CommandExecutorTests` and `AppLayerSourceGuardTests` to cover hostile exception-message getters on launch failure message construction
- Expanded `CommandExecutorTests` and `AppLayerSourceGuardTests` to cover tool / URL / path / argument normalization before launch-failure breadcrumbs are assembled
- Expanded `CommandExecutorTests` to cover direct null target-fragment normalization inside `NormalizeTargetForLog(...)`
- Expanded `CommandExecutorTests` to cover direct null `NormalizeToolPath(...)` helper behavior
- Expanded `CommandExecutorTests` to cover direct null `HasUsableTool(...)` helper behavior
- Expanded `AppLayerSourceGuardTests` to cover LaunchServices bundle-path normalization before Mac relay breadcrumbs are written
- Added `FileStateSyncNotifierTests` and expanded `AppLayerSourceGuardTests` to cover hostile exception-message getters on sync warning construction
- Expanded `FileStateSyncNotifierTests` and `AppLayerSourceGuardTests` to cover malformed/observed sync payload normalization before those fragments reach warning/info crash-log lines
- Expanded `FileStateSyncNotifierTests` to cover direct null sync-payload normalization inside `NormalizePayloadForLog(...)`
- Expanded `AppLayerSourceGuardTests` to cover sync-file path normalization before write-success/write-failure breadcrumbs are written
### [1.1.9] - 2026-04-14

### Fixed
- `CrashFileLogger` and `DbErrorLogger` now normalize null Warning/Info message payloads to `(no message payload)` before writing to `crash.log` or persisting `ErrorLogEntity` rows, so degraded logging paths keep explicit evidence instead of blank message fields

### Changed
- Repository guidance now tracks the current cdidx workflow more explicitly: `CLAUDE.md` was refreshed for cdidx 1.9.0, and tracked `.claude/settings.json` now denies `rg` / `grep` / `find`-style shell search commands so repo sessions stay on the cdidx-first path by default

### Tests
- Expanded logging regression coverage for persistence-failure breadcrumbs and null Warning/Info payload normalization in `CrashFileLoggerTests` and `DbErrorLoggerTests`
- Updated `AppLayerSourceGuardTests` so the file-first logger ordering guard matches the normalized Warning/Info logging path

### [1.1.8] - 2026-04-13

### Fixed
- `CrashFileLogger.AppendExceptionChain` and `DbErrorLogger`'s exception-type / message builders now cap recursion at depth 32 and emit an explicit truncation marker, protecting the last-resort crash logger from StackOverflow on pathological inner-exception chains
- `DbErrorLogger.BuildFullStackTrace` now traverses the inner-exception graph iteratively with the same depth cap and reference-equality cycle detection instead of delegating to `Exception.ToString()`, so the DB logger's stack-trace field cannot stack-overflow or balloon on pathological or cyclic exception graphs
- `CrashFileLogger.AppendExceptionChain` and `DbErrorLogger`'s exception-type / message builders now also track visited exceptions by reference equality, so a shared `AggregateException` subtree referenced from multiple parent slots is serialized once with a `shared/cyclic reference` marker instead of fanning out exponentially before the depth cap engages
- `CrashFileLogger` and `DbErrorLogger` now cap total exception-node serialization at 256 per call (in addition to depth 32) so a wide `AggregateException` fan-out — thousands of task failures under one aggregate — cannot synchronously stall the UI, balloon `crash.log`, or bloat the DB error-log payload
- `DbErrorLogger.BuildFullStackTrace` now bounds traversal-stack *growth* by the remaining node budget before enqueueing `AggregateException` children, not just after popping them. Previously a 50k-child aggregate still allocated 50k traversal entries synchronously before the cap fired; now allocation stays proportional to the 256-node budget regardless of aggregate width
- `CrashFileLogger.AppendExceptionChain` and `DbErrorLogger`'s type/message builders now hard-cap `AggregateException` child-edge iterations at the remaining node budget so a repeated-reference aggregate (e.g. `Enumerable.Repeat(sharedEx, 100_000)`) cannot iterate O(N) shared-reference markers. Both loggers share a `TryGetAggregateTopLevelSummary` helper that uses a bounded BFS to confirm the full descendant tree is under `AggregateMessageExpansionCap` (32) before invoking `AggregateException.Message`. Small nested wrappers like `new AggregateException("sync user-42", new AggregateException(...))` therefore keep their caller-supplied summary in persisted logs; wide or deep aggregates fall back to a synthetic bounded summary. No private framework fields are inspected
- `AggregateException` traversal now scans every child position so a later distinct exception after duplicated leading children is still serialized while node budget remains. Duplicates cost one hashset lookup each and collapse into a single per-aggregate summary marker. `BuildFullStackTrace` adds to `visited` at enqueue-time (instead of pop-time) so sibling duplicates within the same aggregate are recognised before being pushed onto the traversal stack
- Added a per-aggregate child-edge scan cap (`MaxAggregateChildEdgeScan = 4096`) to both `CrashFileLogger` and `DbErrorLogger` so synchronous work stays O(budget), not O(child count), even for aggregates with millions of repeated references
- Added a suffix sample window (`MaxAggregateChildTailSample = 128`) to both loggers on top of the prefix scan. A far-tail distinct exception — the realistic "failure storm ends with the actionable root cause" shape — is still persisted in all three DB fields and in `crash.log` instead of being silently dropped after the prefix cap. The middle region (between prefix scan and tail sample) is clipped with an explicit `"middle child(ren) not scanned"` marker so operators know a suffix sample was used
- Reserved tail + interior budget (`AggregateChildTailReserve = 16`, `MaxAggregateChildMiddleSample = 8`) before the prefix loop in both loggers. Prefix processing is capped at `remainingNodes − reservedTail − reservedMiddle`, so an all-unique wide aggregate (the realistic `Task.WhenAll` storm) can no longer starve the tail sample; the actionable last-index child survives in both DB fields and `crash.log`. Interior positions are evenly-spaced sampled so a distinct middle-region root cause is not deterministically lost. Tail iteration goes from end inward so the final indices are always preserved
- `DbErrorLogger.BuildFullStackTrace` now accounts for `stack.Count` (ancestor-discovered siblings already pending) in its enqueue cap, so nested-wide aggregates cannot queue `remainingNodes` new frames on top of many already-pending siblings and blow past the 256-node bound
- `DbErrorLogger.BuildFullStackTrace` now emits `[i]` slot labels for every queued `AggregateException` child (and `[i] (tail sample)` for children surfaced through the suffix sample), so the DB stack-trace field correlates 1:1 with `CrashFileLogger`'s `AggregateException[i]` markers on the same failure
- Added a pre-enqueue depth-cap short-circuit in both loggers: when an `AggregateException` sits at `depth == MaxExceptionChainDepth - 1`, its sibling loop is skipped entirely and a single marker is emitted. Previously each depth-truncated child returned before consuming node budget, letting the sibling scan iterate up to the edge-scan cap and emit one truncation marker per child

### Tests
- Added deep inner-exception-chain safety tests for `CrashFileLogger.WriteException` and `DbErrorLogger.Log`
- Added cyclic inner-exception graph regression test for `DbErrorLogger.Log` that asserts stack-trace persistence records a cycle marker rather than recursing
- Added shared-`AggregateException`-subtree regression test for `DbErrorLogger.Log` that asserts a fan-out-of-10 graph (1024 paths untracked → 11 distinct nodes) serializes linearly and logs shared-reference markers across all three fields
- Added wide-`AggregateException`-fan-out regression test (5000 children) that asserts node-budget truncation markers appear on all three DB fields and in `crash.log` and that each persisted field's length stays O(budget), not O(children)
- Added very-wide-`AggregateException` stress regression (50 000 children) that asserts the persisted stack-trace field length stays O(budget), proving traversal-stack growth is bounded (deterministic output-shape check instead of flaky wall-clock assertions)
- Added repeated-reference-`AggregateException` stress regression (100 000 copies of the same leaf) that asserts truncation markers + O(budget) field lengths across all three DB fields and `crash.log`, proving edge-loop iteration is capped regardless of duplicate-reference visit-caching
- Added mixed-`AggregateException` regression (`[shared × 2999, uniqueTail]`, within the 4096 edge-scan cap) that asserts the tail's distinct `NullReferenceException` still appears in all three DB fields and in `crash.log`, guarding against duplicate-heavy aggregates hiding the failure that actually explains an incident
- Added middle-region scan-cap regression (100 000 children with a unique GUID-marked needle at position 50 000) that asserts the needle does NOT appear in any persisted field *and* the `"middle child(ren) not scanned"` truncation marker IS emitted — direct behavioural guard against a regression that removes the per-aggregate edge cap
- Added far-tail root-cause regression (100 000 children with a unique GUID-marked `NullReferenceException` at position 99 999) that asserts the tail sample window surfaces the root cause in all three DB fields and in `crash.log` — guard against a regression that drops the suffix sample and hides the actionable failure
- Added all-unique-wide regression (100 000 distinct children with actionable `NullReferenceException` at the final index) that asserts the reserved tail budget preserves the last-index root cause across all DB fields and `crash.log` — direct guard for the adversarial shape where the prefix alone would exhaust the node budget
- Added nested-wide regression (outer aggregate of 5 inner aggregates of 1 000 children each) that asserts persisted output length stays bounded — guard for the `stack.Count` accounting in `BuildFullStackTrace`
- Added small-aggregate top-level-message regression that asserts a 2-child aggregate's caller-supplied summary is preserved via public API (no reflection) in both persisted stores
- Added nested-wrapper regression (`AggregateException("sync user-42", AggregateException("child", leaf))`) that asserts the outer wrapper summary survives in all DB fields and in `crash.log`
- Added per-child-index-label regressions for `BuildFullStackTrace` that assert `[0]`, `[1]`, `[2]` slot prefixes and a `(tail sample)` annotation on children surfaced through the suffix window
- Added deep-wide regression (4000-child aggregate wrapped at depth 31) that asserts exactly one `"would exceed depth cap"` marker per builder and bounded field lengths — direct guard against a regression that removes the pre-enqueue depth short-circuit
- Expanded `CiCoverageWorkflowPolicyTests` to assert `fetch-depth: 0`, GA SDK pinning, MAUI workload install flags, Xcode first-launch / compatibility gate, platform frameworks, and delivery-workflow Windows RID guard so documented CI/release invariants no longer rely on reviewer memory
- Scoped `CiCoverageWorkflowPolicyTests` assertions to individual jobs (`core-tests`, `windows-build`, `mac-build`, delivery `package` + matrix entries) so a regression dropping an invariant from one job cannot pass green just because a sibling job still contains the same string
- `CiCoverageWorkflowPolicyTests` now parses workflow steps structurally (name/if/run/uses/id) for the critical Xcode-gate invariants. The delivery `Initialize Xcode` / `Check Xcode compatibility` / `Publish app` step guards are asserted against their actual `if:` expressions and `run:` bodies, so a comment or mis-scoped step condition cannot satisfy the test

### [1.1.7] - 2026-04-12

### Fixed
- UI event handlers, sync notifiers, dock/QuickLook timers, command-suggestion debounce, clipboard helpers, and conflict callbacks now persist unhandled exceptions to `crash.log` so crash evidence survives abrupt termination
- Narrowed `OperationCanceledException` handling in command-suggestion debounce and Windows IME reassert paths to avoid swallowing non-cancellation exceptions
- `DbErrorLogger` now preserves inner-exception details when logging persistence failures

### Changed
- Consolidated repeated dispatcher-invoke logging helpers in `MainViewModel` and `App` into shared static methods
- Refreshed cdidx guidance in `CLAUDE.md` for v1.6 features (inspect metadata, `suggest_improvement`, MCP-first SQL fallback policy)

### Tests
- Added undo/redo workflow integration tests for `MainViewModel`
- Expanded `AppLayerSourceGuardTests` to cover newly hardened partial classes
- Added `DbErrorLogger` detail-preservation tests

### [1.1.6] - 2026-04-12

### Fixed
- `ThemeModeParser.ParseOrDefault()` now sanitizes invalid caller-supplied fallback enum values back to `System` instead of returning an out-of-range theme when both the input string and default are invalid
- `CommandExecutor` now expands environment-variable-backed tool paths and trims quotes after expansion, so `%ComSpec%` / quoted `%...%` tools execute with the same normalized path logic as literal tool values
- Windows startup now warning-logs failures to create the `startup.log` directory instead of losing that breadcrumb before the later append path even runs
- `DockOrderValueCodec.Parse()` now trims wrapping quotes from both the whole CSV payload and individual GUID entries so quoted dock-order persistence still restores the intended order
- `WindowsPathPolicy.IsUncPath()` now accepts quoted UNC paths but rejects `\\\\?\\` and `\\\\.\\` local-device prefixes instead of misclassifying them as network shares
- Mac `AppDelegate` now warning-logs failures to prioritize key commands over system behavior instead of letting runtime reflection differences fail silently
- `QuickLookPreviewFormatter.BuildLine()` now keeps the entire labeled quick-look line within the requested `maxLength` instead of letting the `label + ": "` prefix push the final string past the caller's limit
- `CommandNotFoundRefocusPolicy` now ignores leading whitespace before checking the `Command not found:` prefix so refocus still triggers for padded status text without matching embedded phrases
- Mac `AppDelegate` now warning-logs `MarshalManagedException` hook failures instead of swallowing them silently during startup
- `MacEntryHandler`, `MacEditorHandler`, and Mac `CommandEntryHandler` now warning-log `UIKeyCommand` input-reflection failures and fall back to baked key literals instead of letting handler type initialization fail on runtime API differences
- `ThemeModeParser.NormalizeOrDefault` now sanitizes invalid fallback enum values back to `System` instead of returning an out-of-range theme when both the parsed value and caller-supplied default are invalid
- `DbErrorLogger` now warning-logs unexpected drain-loop failures to `crash.log` so background log persistence does not fail silently after enqueue succeeds
- `FileStateSyncNotifier.NotifyButtonsChangedAsync` now warning-logs sync-payload write failures before rethrowing so local save/delete success paths leave a breadcrumb when file signaling breaks
- `MainPage` now warning-logs Windows focus-visual reflection failures, modal primary-editor focus failures, and `IsTabStop` reflection failures instead of swallowing those fallback-path errors silently
- Mac middle-click/button-mask and editor-key-command/CoreGraphics fallback paths now warning-log failures so native pointer or shortcut bridging issues leave local diagnostics instead of quietly degrading
- Windows `CommandEntryHandler` now warning-logs both compatibility-triggered and unexpected `InputScope` assignment failures before disabling or rejecting the write
- `FileAppConfigService` now continues to later config candidates when an earlier config file is readable but omits `theme` or contains an invalid theme value, instead of prematurely defaulting to `System`
- `FileAppConfigService` now warning-logs skipped config candidates so malformed, unreadable, or invalid theme configs leave a crash-log breadcrumb before fallback continues
- `DbErrorLogger` now warning-logs DB persistence and retention-purge failures to `crash.log` so non-fatal repository errors still leave diagnostics during shutdown or degraded logging paths
- `DbErrorLogger.FlushAsync(timeout)` now warning-logs timeout and unexpected flush failures so graceful-shutdown logging gaps leave an explicit breadcrumb instead of failing silently
- `Praxis/Platforms/MacCatalyst/Program.cs` is now UTF-8 BOM-free, and a repository encoding guard test now keeps `cdidx validate` clean for that entrypoint
- `CommandExecutor` now warning-logs native process-start and launch-target-resolution failures to `crash.log` so returned user-facing launch errors also leave a local diagnostic breadcrumb
- `LaunchTargetResolver` now trims wrapping quotes after environment-variable expansion, so quoted env-backed HTTP URLs and filesystem paths still resolve correctly
- `AppStoragePaths` now ignores malformed legacy-path comparisons instead of letting `Path.GetFullPath` abort storage migration checks
- `CommandRecordMatcher` now ignores `null` collection entries instead of throwing while scanning command suggestions
- `StateSyncPayloadParser` now rejects double-separator payloads instead of collapsing empty segments and accepting malformed sync signals
- `QuickLookPreviewFormatter` now keeps ellipsis-truncated output within the requested `maxLength` instead of returning strings longer than the caller asked for
- `ButtonSearchMatcher`, `LogRetentionPolicy`, and `LauncherButtonOrderPolicy` now ignore `null` record entries instead of throwing inside search, retention, or placement normalization helpers
- `CommandWorkingDirectoryPolicy` now expands environment-variable tool paths before shell detection, so `%ComSpec%` / quoted `%...%` Windows shell tools still pick the user-profile working directory
- `CommandLineBuilder` now treats quoted-empty tool values as empty so preview/status command lines stay aligned with execution-time empty-tool handling
- `AppStoragePathLayoutResolver` now trims wrapping quotes from configured storage roots before composing app-data paths
- `GridSnapper` and `ModalEditorScrollHeightResolver` now sanitize non-finite numeric inputs instead of propagating `NaN` / infinity through layout calculations
- Windows startup-log append failures and `MainPage` copy-notice animation failures now leave `crash.log` breadcrumbs instead of being swallowed silently

### [1.1.5] - 2026-04-11

### Fixed
- `MainViewModel.CommandSuggestions` now warning-logs failures to dispatch popup close/refresh work onto the main thread instead of letting those scheduling errors vanish silently
- `MauiThemeService.Apply()` now skips no-op theme reapplication when the target theme is already active, and Mac window-style dispatch failures are crash-logged
- Mac Catalyst open-relay startup now treats `Process.Start(...) == null` as failure and crash-logs LaunchServices relay failures instead of silently falling back
- `FileStateSyncNotifier.Dispose()` now disables `EnableRaisingEvents` before unsubscribing/disposing the watcher to reduce late callback races during teardown
- Base `App` global unhandled-exception/process-exit hooks are now registered only once, preventing duplicate crash/log flush handlers from stacking if app initialization is re-entered
- `MainViewModel.SyncThemeFromExternalChangeAsync()` now warning-logs failures thrown from the dispatched main-thread apply path, and external reload uses `RunContinuationsAsynchronously` for its bridge `TaskCompletionSource`
- `FileStateSyncNotifier.NotifyButtonsChangedAsync()` now no-ops after disposal instead of trying to write stale sync files during teardown
- `AppStoragePaths.TryMigrateLegacyDatabase()` now warning-logs copy failures and continues scanning other legacy candidates instead of aborting migration on the first unreadable source
- `FileAppConfigService` now falls back to later config candidates when an earlier config file is inaccessible with `UnauthorizedAccessException`
- `MainPage.OnDisappearing()` now detaches window-activation hooks, and the detach path also releases Mac activation observers so disappearing pages do not keep stale activation callbacks alive
- `CommandExecutor` now expands home-prefixed tool paths (`~`, `~/...`, `~\\...`) before deciding whether a tool is executable, so direct tool launches can use the same home shorthand as empty-tool path launches
- Windows `startup.log` now uses the normalized shared app-storage root instead of the raw local-app-data special-folder string, avoiding malformed startup-log paths when the environment value is quoted or missing
- Windows and Mac platform startup classes now guard global exception-hook registration so repeated initialization cannot stack duplicate unhandled-exception handlers
- `App.CreateWindow()` no longer caches the fallback error page when `ResolveRootPage()` fails, so later window creation can recover instead of being pinned to the first startup failure page
- `App` now crash-logs log-flush failures during both `AppDomain.UnhandledException` termination handling and `ProcessExit`, instead of suppressing them silently
- `FileStateSyncNotifier` now warning-logs sync-file read retry exhaustion instead of silently dropping unreadable external-sync payloads
- `CommandExecutor` now treats normalized empty/quoted-empty tool values as “no tool” and falls back to URL/path launching instead of trying to execute an empty filename
- `MainPage` now crash-logs XAML load failures, resets its initialization gate when first-load startup fails, and separately crash-logs initialization-alert failures so a broken alert path does not erase the original startup exception
- `MauiClipboardService` now honors cancellation tokens for both clipboard reads and writes instead of ignoring canceled operations
- `SqliteAppRepository` now publishes its shared SQLite connection only after schema upgrade and initial cache load succeed, allowing clean retry after initialization failures
- `DbErrorLogger.FlushAsync(timeout)` now waits for both queued entries and already-dequeued in-flight DB writes up to the timeout instead of returning early once the queue becomes temporarily empty
- Error-log retention purge remains Error-only; added regression coverage so Info/Warning writes do not trigger `PurgeOldErrorLogsAsync`
- `CrashFileLoggerTests` no longer use blocking `Task.WaitAll`, removing the xUnit analyzer warning from Release test runs
- Shared and platform-specific unhandled-exception hooks now keep more diagnostics: non-`Exception` thrown objects are logged as warnings instead of degrading to empty payloads, and Windows/Mac `UnobservedTaskException` handlers now call `SetObserved()`
- `FileStateSyncNotifier` now subscribes before enabling the watcher, recreates the sync directory before writes, ignores malformed/out-of-range payload timestamps safely, and crash-logs event-subscriber failures instead of letting the background task fault silently
- `LaunchTargetResolver` now treats separator-based relative paths and bare `~` as filesystem fallback targets when `Tool` is empty, and `CommandExecutor` now expands bare `~` to the user-profile path before existence checks
- Windows shell launches (`cmd.exe`, `powershell`, `pwsh`, `wt`) now start from the user-profile directory instead of inheriting the Praxis process working directory
- Theme parsing now rejects numeric enum strings in config/repository/ViewModel inputs, `.` / `..` now resolve as filesystem targets, and quoted tool paths are normalized before process launch
- `SqliteAppRepository` now normalizes cached button order to placement order after load/reload/upsert paths, and dock-order persistence now discards duplicate or empty GUIDs while preserving first occurrence order
- Config/storage path handling is stricter: malformed base `praxis.config.json` now falls back to later valid candidates, quoted `%LOCALAPPDATA%` values are normalized, and blank/relative storage roots no longer degrade into working-directory-relative DB/crash-log paths
- `DbErrorLogger` now preserves nested inner exception type/message chains inside `AggregateException` entries instead of truncating at the direct children
- `SqliteAppRepository.SetThemeAsync` now normalizes out-of-range `ThemeMode` values to `System`, and external empty `dock_order` sync now clears stale Dock UI state instead of leaving old buttons visible
- `MainViewModel` now warning-logs external reload/theme sync failures, command-suggestion refresh failures, and conflict-dialog callback failures instead of swallowing them silently
- Windows clear-button native refocus failures now write directly to `crash.log`, improving diagnostics for freeze/abort paths where async DB logging may never complete
- Startup, external sync, execution-request, clipboard-copy, clear-button, and sync-signal boundaries now emit additional low-cost Info breadcrumbs so GUI hangs/aborts leave a clearer last-known-good stage
- Clipboard and sync-notifier failures are now isolated from successful local actions: create-with-clipboard falls back to empty args, copy failures become warning/status feedback, execution still logs after clipboard-copy failure, and save/delete/theme/dock/history operations no longer unwind after post-success sync notification errors
- Launch-log write/purge, dock persistence, undo/redo dock restore, and theme persistence failures are now treated as non-fatal after local success, with warning logs instead of surfacing as user-action exceptions
- Initialization and external reload now tolerate non-critical theme/dock read failures with warning logs and safe fallbacks, and command lookup fallback errors now degrade to `Command not found` instead of bubbling exceptions

### [1.1.4] - 2026-04-09

### Added
- add .codex & CLAUDE.md

### Fixed
- .NET 10 preview runtime package restore failure (`NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64 with version (= 10.0.5)`) in CI and Delivery workflows — replaced pinned `dotnet-version: 10.0.100` with `10.0.x` + `dotnet-quality: preview` to enable the preview NuGet feed

### [1.1.3] - 2026-04-05

### Added
- Automatic GitHub Release creation on `v*` tag push — delivery workflow now zips OS-specific artifacts (Windows / macOS) and publishes them with auto-generated release notes

### [1.1.2] - 2026-03-31

### Added
- Synchronous file-based crash logger (`CrashFileLogger`) that writes to `crash.log` immediately on every log call, surviving abrupt process termination where async DB writes would be lost
  - Cross-platform: Windows `%LOCALAPPDATA%\Praxis\crash.log`, macOS `~/Library/Application Support/Praxis/crash.log`
  - Automatic log rotation at 512 KB
  - Full exception chain output including inner exceptions, `AggregateException` flattening, and `Exception.Data` dictionary
- `IErrorLogger.LogWarning(message, context)` for warning-level log entries
- `IErrorLogger.FlushAsync(timeout)` to drain pending async DB writes during graceful shutdown
- `AppDomain.ProcessExit` handler that flushes logs before process exit
- `UnhandledException` handler now attempts synchronous flush when `IsTerminating=true`
- Mac Catalyst `AppDelegate` crash file logging hooks (`UnhandledException`, `UnobservedTaskException`, `MarshalManagedException`)
- Windows platform exception handlers now write to both `startup.log` and `crash.log`
- Non-Exception thrown objects are now captured in `UnhandledException` handler

### Changed
- `DbErrorLogger` rewritten: all `Log`/`LogInfo`/`LogWarning` calls write to crash file synchronously first, then enqueue for async DB write via `ConcurrentQueue` with single-writer drain loop (replaces fire-and-forget `_ = LogAsync()` pattern)
- Error log entries now capture full exception type chains (e.g. `InvalidOperationException -> NullReferenceException`), concatenated inner messages, and complete stack traces via `Exception.ToString()`
- `ErrorLogEntity.Level` column now accepts `Warning` in addition to `Error` and `Info`
- `ResolveRootPage` failure now logged via `IErrorLogger` (was silently swallowed)

### [1.1.1] - 2026-03-28

### Fixed
- GitHub Actions checkout now fetches full history so Nerdbank.GitVersioning can calculate version height in CI and release packaging jobs
- macOS GitHub Actions jobs initialize Xcode before Mac Catalyst build/publish to avoid `ibtoold` / Xcode plug-in initialization failures on fresh runners

### Changed
- Added README header badges for CI, CodeQL, Delivery, .NET 10, .NET MAUI, supported platforms, SQLite, and MIT license

### [1.1.0] - 2026-03-28

### Added
- Per-button inverted colors with auto DB schema migration (v1 → v2)
- DB-backed error logging (ERROR + INFO levels) with 30-day retention (`ErrorLog` table, schema v3 → v4)
- INFO-level contextual tracing for key user actions: button/command execution, editor open/save/cancel/delete, theme change, undo/redo, conflict resolution, window close
- Undo/Redo for button mutations (move/edit/delete): Ctrl+Z / Ctrl+Y on Windows, Command+Z / Command+Shift+Z on macOS
- Quick Look preview on button hover (Command / Tool / Arguments / Clip Word / Note)
- SQLite schema versioning via `PRAGMA user_version` with sequential auto-migration
- Cross-window sync via `FileSystemWatcher` signal file (`buttons.sync`) with instance-id self-filter
- Conflict detection dialog for concurrent multi-window edits (optimistic locking via `Version` column)
- Execute all matching commands on Enter (not just the first match)
- Clip Word field supports multiline input
- Search text auto-cleared on button create
- Arrow-key focus cycling for context menu and conflict dialog on Windows/macOS
- Middle-click and right-click interactions on command suggestion rows
- Command suggestion debounce increased to 400 ms to reduce noise during fast typing
- First `Down` key selects first candidate; popup no longer auto-selects on open
- CI coverage collection and Cobertura artifact upload (GitHub Actions)
- UNC path fallback via `explorer.exe` on Windows so auth prompt can appear before existence checks succeed
- Dock horizontal scrollbar shown only while hovering the Dock area and horizontal overflow exists
- Invert-theme label is tappable to toggle the checkbox (not just the checkbox itself)

### Fixed
- Command suggestions auto-close when context menu opens
- Command suggestion click runs the command and autofills the command input
- Editor modal default focus now lands on `ButtonText`, matching the field order after the Button Text / Command swap
- New-button create now selects all `ButtonText` text on initial modal focus on both Windows and macOS
- Windows: Tab focus navigation selects all text in input fields
- Clear button focus restore stability after tap (immediate attempt + short delayed retry)
- Windows: top-bar `Command` / `Search` clear-button refocus now skips stale native `TextBox` instances to avoid rare aborts
- macOS: clear-button refocus is deferred to the next frame to avoid responder re-entry during clear-button hide
- Clear-button X glyph vertical centering on Windows
- Command suggestion colors stay theme-synced during live theme switch
- `CommandEntry` / `SearchEntry`: lowercase letters no longer converted to uppercase
- Main command entry no longer attempts to switch IME/input-source mode on focus on Windows or macOS
- Modal `Command` IME / ASCII enforcement:
  - Windows: `InputScopeNameValue.AlphanumericHalfWidth` on focus + `imm32` nudge (immediate + one delayed retry)
  - macOS: `AsciiInputFilter` + `setMarkedText` / `insertText` blocking, detached on app deactivation
- Modal `Command` IME reasserted while focused on Windows (prevents manual IME-mode switching)
- macOS: modal ASCII input source enforced only while field is first responder in active key window
- macOS: after "Command not found", focus is restored to command input for immediate retry
- macOS: ILLink input loss in clear-button path prevented via intermediate assembly copy
- Windows: modal/conflict focus fallback uses 2-stage retry so Esc and Ctrl+S remain responsive
- Windows: `InputScope` `ArgumentException` (E_RUNTIME_SETVALUE) handled gracefully via one-way unsupported flag
- Windows: Ctrl+Z/Y undo granularity preserved; `TextChanging` rewrite disabled for command input
- Single-window false-positive conflict dialog eliminated (instance-id self-filter in sync signal)
- "Command not found" shown as error flash (red) rather than neutral status
- Editor modal re-focus on macOS after returning from another app

### Changed
- Command and Button Text field order swapped in editor modal (Command first)
- UI button font size unified to 12 across all platforms
- UI button padding set to 0 in placement area and Dock
- Dock area height expanded
- New button icon changed from plain `+` to wireframe hex logo (outer hexagon · inscribed circle · inner hexagon · center `+`)
- App icon and splash screen refreshed to hexagon + polygon contrast design with micro-optimized variants
- `MainPage` concern split refined further: `EditorAndInput` was narrowed to shared input behavior, while modal editor, view-model event wiring, status/theme logic, dock/quick-look behavior, and Windows-native input hooks moved into dedicated partial classes
- `SqliteAppRepository` public operations protected with exclusive locking for thread safety
- UI delay values consolidated into `UiTimingPolicy`
- Platform preprocessor blocks consolidated across `MainPage` field files and `MauiProgram` handler registration
- Redundant `using` directives removed and `using` order normalized
- `MainViewModel` and its partial classes annotated with `LogInfo` calls for key lifecycle events

---

# 変更履歴（日本語）

このファイルにはプロジェクトの主な変更をすべて記録します。

形式は Keep a Changelog に準拠し、バージョン管理は Semantic Versioning に従います。

## [Unreleased]

### 修正
- `SecondaryFailureLogger` は二次 fallback sink root を `Praxis/secondary-failures.log` の組み立て前に絶対パスへ正規化するようになり、quote 付き絶対パス override は引き続き使える一方、空や相対 root は誤って相対診断パスを作らないよう無視するよう修正
- `FileStateSyncNotifier` は読込リトライ枯渇 warning breadcrumb にも正規化済み sync-file path を含めるようになり、`buttons.sync` の読込失敗時に例外要約だけでなく対象ファイルも特定できるよう修正
- `MauiThemeService` は Mac の dispatch-failure warning breadcrumb に対象 `AppTheme` も含めるようになり、Light/Dark/System のどのテーマ遷移で失敗したかを generic な window-style warning より具体的に追えるよう修正
- `FileAppConfigService` は invalid theme の skipped-config warning breadcrumb に正規化済みの生 `theme` 値も含めるようになり、壊れた `praxis.config.json` が返した実値を「無効だった」という事実だけでなく具体的に残せるよう修正

### テスト
- `SecondaryFailureLoggerTests` を拡張し、quote 付き絶対パスの fallback root と、相対 fallback root を拒否する挙動を startup diagnostics file パス組み立て前に固定
- `FileStateSyncNotifierTests` と `AppLayerSourceGuardTests` を拡張し、読込リトライ枯渇 warning に入る正規化済み sync-file path prefix を固定
- `AppLayerSourceGuardTests` を拡張し、`MauiThemeService` の Mac dispatch-failure breadcrumb が要求された `AppTheme` を含むことを固定
- `FileAppConfigServiceTests` と `AppLayerSourceGuardTests` を拡張し、invalid theme の skipped-config warning に入る正規化済み `theme` 値を固定
### [1.1.11] - 2026-04-17

### 変更
- `MainViewModel` は `ClearCommandInput` でクリア文字数／no-op を、`ExecuteCommandInputAsync` で実行コマンド長と候補選択フラグを明示的に `LogInfo` するようになった。コマンド入力経路の直後にクラッシュした際に、ハンドラ側のタップログだけから意図を逆算する必要がなくなり、ViewModel 側でも breadcrumb が確実に残る
- `MainViewModel.ClearSearchText` も `ClearCommandInput` と同形の breadcrumb（クリア文字数／no-op）を記録するようになり、検索欄 X ボタンのタップ経路でクラッシュが続いた場合も、コマンド側と同等の ViewModel 側 evidence が残るようになった
- `MainPage.FocusEntryAfterClearButtonTap` / `ApplyEntryFocusAfterClearButtonTap` は対象 Entry と retry 件数、および完了マーカーを `crash.log` に出力し、`Entry.Focus()` 自体も try/catch で包んで MAUI ハンドラ内の失敗を crash-file 例外として顕在化するようになり、クリア直後のフォーカス復帰経路で落ちても無言のプロセス終了にならないようにした

### [1.1.10] - 2026-04-15

### 修正
- `CommandWorkingDirectoryPolicy` は Windows のシェル実行ファイル名を大文字小文字を区別せず判定するようになり、大文字・混在表記の `cmd.exe` / `powershell.exe` / `pwsh.exe` / `wt.exe` パスでも Praxis プロセスディレクトリを継承せず `WorkingDirectory` をユーザープロファイルに切り替えるよう修正
- `LaunchTargetResolver` は先頭/末尾が引用符の有効なパスターゲットを維持し、環境変数展開後の引用符付きルート/ホーム/相対パスおよび `file://` プレフィックスを正規化し、引用符付きの非 file URI スキームプレフィックスは明示的に除外することで、不正な引用符付き URL も確実に fail-closed するよう修正
- `CrashFileLogger` は、カスタム例外の `Message` / `StackTrace` ゲッターや `Exception.Data` のキー/値の `ToString()` が例外を投げた場合でも crash-log レコードを残すよう維持し、例外メッセージを単一行に整形することで、不正なペイロードでインラインログ書式が壊れないよう修正
- `CrashFileLogger.SafeExceptionMessage(...)` は空白のみの例外メッセージを `(empty)` に正規化するようになり、例外本体が存在するが空の場合でも、警告 breadcrumb が末尾区切り子だけの空行にならないよう修正
- `DbErrorLogger` は同じ単一行例外メッセージ正規化とゲッター失敗フォールバックマーカーを `ErrorLogEntity` に永続化するようになり、アプリ/プロセス終了時の flush 失敗や Windows 起動ログ書き込み失敗時にも警告 breadcrumb より先に例外本体を記録し、通常の `%LOCALAPPDATA%\\Praxis` crash sink が使えない場合は独立した temp/カレントディレクトリの診断ファイルへフォールバックするよう修正
- `SecondaryFailureLogger` は、起動時のターゲットパスや操作名の断片もフォールバック警告/本文行に埋め込む前に正規化するようになり、不正な起動ログメタデータが二次診断ファイルを壊さないよう修正
- `MainViewModel` の警告経路は、外部テーマ同期、コマンド候補の更新/参照、競合コールバック、クリップボードヘルパー、同期通知、ローカル永続化のフォローアップログで敵対的な例外 `Message` ゲッターに遭遇した場合に共通の安全な例外メッセージヘルパーを使うよう修正し、劣化時の警告ログがリカバリ経路から再スローしないよう修正
- `AppStoragePaths` はレガシー DB マイグレーションおよび無効パス比較の警告に同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターの場合でも起動時マイグレーションが不正な候補を確実にスキップし続けるよう修正
- `AppStoragePaths` はマイグレーションのソース/比較パス断片も警告プレフィックスに埋め込む前に正規化するようになり、不正なパステキストがレガシーマイグレーション中に crash-log の行構造を壊さないよう修正
- `FileAppConfigService` は、スキップされた設定読み込みが `IOException` / `UnauthorizedAccessException` / `JsonException` を投げた際にも同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターでも警告ログの breadcrumb が確実に残るよう修正
- `FileAppConfigService` は設定パス断片も invalid-theme / skipped-config の breadcrumb に埋め込む前に正規化するようになり、改行を含む候補パスが crash-log の行構造を壊さないよう修正
- `CommandExecutor` は、launch-target 解決およびネイティブプロセス起動失敗メッセージで同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターでフォールバック警告/結果構築が再スローしないよう修正
- `CommandExecutor` はツール / URL / パス / 引数の断片も失敗プレフィックスに埋め込む前に正規化するようになり、改行を含む launch target が crash-log の breadcrumb 書式を壊さないよう修正
- `MauiThemeService` は Mac ディスパッチ失敗の breadcrumb に同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターでテーマ適用警告ログが再スローしないよう修正
- `FileStateSyncNotifier` は書き込み/読み取り/予期しない publish の警告構築を共通の安全な例外メッセージヘルパーへ通すようになり、敵対的な例外 `Message` ゲッターでも同期 breadcrumb が残るよう修正
- `FileStateSyncNotifier` は不正ペイロードや observed-source の断片も crash-log の警告/情報行に埋め込む前に正規化するようになり、同期ファイルに改行や空白のみのペイロードマーカーが含まれていても同期 breadcrumb が単一行を保つよう修正
- `FileStateSyncNotifier` は同期ファイルのパス断片も write-success/write-failure の breadcrumb に埋め込む前に正規化するようになり、不正なストレージパスが crash-log の行構造を壊さないよう修正
- `Windows CommandEntryHandler` は互換性起因および予期しない `InputScope` 割り当て警告で同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターで WinUI フォールバックログが再スローしないよう修正
- Mac の `AppDelegate` および Mac entry/editor/command ハンドラは `MarshalManagedException` フック、key-command 優先度、`UIKeyCommand` 入力解決の警告 breadcrumb で同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターでそれらのフォールバック経路が再スローしないよう修正
- Mac entry/editor/command ハンドラはリフレクションで取得した `UIKeyCommand` 入力名も警告 breadcrumb に埋め込む前に正規化するようになり、不正なリフレクションメタデータが crash-log の行構造を壊さないよう修正
- Mac `Program` は LaunchServices リレー失敗の breadcrumb で同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターで open-relay 警告ログが再スローしないよう修正
- Mac `Program` は LaunchServices バンドルパス断片もリレー breadcrumb に埋め込む前に正規化するようになり、不正なバンドルパスが crash-log の行構造を壊さないよう修正
- `MiddleClickBehavior` と `MainPage.MacCatalystBehavior` は `buttonMaskRequired`、遅延 middle-click 実行、Mac エディタの key-command 作成、CoreGraphics フォールバックの警告 breadcrumb で同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターでそれらの劣化 Mac 入力経路が再スローしないよう修正
- `MainPage` はコピー通知、ステータスフラッシュ、Dock hover-exit、Quick Look アニメーションの警告 breadcrumb で同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターで非致命的な UI リカバリ経路が再スローしないよう修正
- `MainPage` はボタンタップ実行、セカンダリタップの新規作成フロー、モーダル primary フォーカスのフォールバック、`UseSystemFocusVisuals`、`IsTabStop` の警告 breadcrumb も同じ安全な例外メッセージヘルパー経由にするようになり、より多くの Windows/UI フォールバック経路が敵対的な例外 `Message` ゲッターで再スローしないよう修正
- `MainPage` と `App` はフォールバック初期化 UI テキストも `CrashFileLogger.SafeExceptionMessage(...)` 経由で取得するようになり、敵対的な例外 `Message` ゲッターが最終手段のエラーページ / アラート面自体を壊せないよう修正
- `CrashFileLogger.SafeObjectDescription(...)` は `Exception` 以外の `AppDomain.UnhandledException` ペイロードを強化し、敵対的なオブジェクトの `ToString()` 実装が base `App`、Windows 起動ログ、Mac `AppDelegate` の最終手段グローバル例外経路を壊せないよう修正

### テスト
- `CommandWorkingDirectoryPolicyTests` を拡張し、混在表記のシェル実行ファイル名や大文字の環境変数展開シェルパスをカバー
- `LaunchTargetResolverTests` を拡張し、引用符付き相対/`file://` パスプレフィックス、引用符境界のパス名、環境変数展開前後における不正な引用符付き URL の扱いをカバー
- `CrashFileLoggerTests`、`DbErrorLoggerTests`、`SecondaryFailureLoggerTests`、`AppLayerSourceGuardTests` を拡張し、複数行例外メッセージの正規化、カスタム例外のゲッター/data フォーマッタが投げるケース、主要 crash sink が書けないときに独立ファイルへフォールバックする起動ログ失敗診断をカバー
- `CrashFileLoggerTests` を拡張し、source/context 正規化ヘルパーの直接呼び出しと、永続化された crash breadcrumb 挙動を同時にカバー
- `CrashFileLoggerTests` を拡張し、null に対する `NormalizeSource(...)` ヘルパーの直接挙動をカバー
- `CrashFileLoggerTests` を拡張し、null に対する `NormalizeContext(...)` ヘルパーの直接挙動をカバー
- `CrashFileLoggerTests` を拡張し、`NormalizeMessagePayload(...)` ヘルパーの直接挙動と永続化された crash breadcrumb 挙動を同時にカバー
- `CrashFileLoggerTests` を拡張し、`SafeExceptionMessage(...)` フォールバックマーカー内の複数行/空白のみのゲッター失敗メッセージをカバー
- `CrashFileLoggerTests` を拡張し、複数行に対する `NormalizeExceptionMessage(...)` ヘルパーの直接挙動をカバー
- `CrashFileLoggerTests` を拡張し、空白のみに対する `NormalizeExceptionMessage(...)` ヘルパー挙動をカバー
- `CrashFileLoggerTests` を拡張し、null に対する `NormalizeExceptionMessage(...)` ヘルパー挙動をカバー
- `CrashFileLoggerTests` を拡張し、null に対する `SafeObjectDescription(...)` ヘルパーの直接挙動をカバー
- `CrashFileLoggerTests` を拡張し、`SafeObjectDescription(...)` 内の空白のみのオブジェクトフォーマッタ失敗マーカーをカバー
- `CrashFileLoggerTests` を拡張し、`SafeObjectDescription(...)` 内の複数行オブジェクトフォーマッタ失敗マーカーをカバー
- `CrashFileLoggerTests` を拡張し、空スタックに対する `SafeExceptionStackTrace(...)` ヘルパーの直接挙動をカバー
- `CrashFileLoggerTests` を拡張し、`SafeExceptionStackTrace(...)` 内の複数行スタックトレースゲッター失敗マーカーをカバー
- `CrashFileLoggerTests` を拡張し、`SafeExceptionStackTrace(...)` 内の空白のみのスタックトレースゲッター失敗マーカーをカバー
- `SecondaryFailureLoggerTests` を拡張し、null に対するターゲットパス/操作名正規化ヘルパーの直接挙動をカバー
- `SecondaryFailureLoggerTests` を拡張し、両方のフォールバック sink ルートが阻害された場合の `false` / null-path 結果をカバー
- `DbErrorLoggerTests` を拡張し、persist 失敗 breadcrumb 内で正規化された複数行 context をカバー
- `DbErrorLoggerTests` を拡張し、Warning persist 失敗 breadcrumb 内で正規化された複数行 context をカバー
- `DbErrorLoggerTests` を拡張し、Info persist 失敗 breadcrumb 内で正規化された複数行 context をカバー
- `DbErrorLoggerTests` を拡張し、purge 失敗 breadcrumb 内で正規化された複数行 context をカバー
- `SecondaryFailureLoggerTests` と `AppLayerSourceGuardTests` を拡張し、それらの断片がフォールバック診断に到達する前段での起動ターゲットパス/操作名正規化をカバー
- `SecondaryFailureLoggerTests` を拡張し、ターゲットパス正規化ヘルパーの直接挙動をカバー
- `SecondaryFailureLoggerTests` を拡張し、空白のみのターゲットパス正規化ヘルパー挙動をカバー
- `SecondaryFailureLoggerTests` を拡張し、複数行の操作名正規化ヘルパー挙動をカバー
- `MainViewModelWorkflowIntegrationTests` と `AppLayerSourceGuardTests` を拡張し、`MainViewModel` の警告経路（外部テーマ同期、コマンド参照フォールバック、競合コールバック、クリップボードフォローアップログ、同期通知、テーマ永続化）における敵対的な例外メッセージゲッターをカバー
- `AppLayerSourceGuardTests` を拡張し、Mac キー入力警告 breadcrumb 構築前のリフレクション `UIKeyCommand` 入力名正規化をカバー
- `AppStoragePathsTests` と `AppLayerSourceGuardTests` を拡張し、レガシーマイグレーション警告構築における敵対的な例外メッセージゲッターをカバー
- `AppStoragePathsTests` と `AppLayerSourceGuardTests` を拡張し、レガシーマイグレーション breadcrumb 書き込み前のマイグレーションソース/比較パス正規化をカバー
- `AppStoragePathsTests` を拡張し、`NormalizePathForLog(...)` 内の null ログ対象パス正規化の直接挙動をカバー
- `FileAppConfigServiceTests` と `AppLayerSourceGuardTests` を拡張し、skipped-config 警告構築における敵対的な例外メッセージゲッターをカバー
- `FileAppConfigServiceTests` と `AppLayerSourceGuardTests` を拡張し、invalid-theme / skipped-config の breadcrumb 書き込み前の設定パス正規化をカバー
- `FileAppConfigServiceTests` を拡張し、`NormalizePathForLog(...)` 内の null ログ対象設定パス正規化の直接挙動をカバー
- `FileAppConfigServiceTests` を拡張し、空白/相対の設定ルートを候補列挙で拒否する挙動をカバー
- `FileAppConfigServiceTests` を拡張し、null に対する `NormalizeAbsoluteDirectory(...)` ヘルパーの直接挙動をカバー
- `FileAppConfigServiceTests` を拡張し、`NormalizeAbsoluteDirectory(...)` 内の引用符付き相対パス拒否挙動をカバー
- `CommandExecutorTests` と `AppLayerSourceGuardTests` を拡張し、launch 失敗メッセージ構築における敵対的な例外メッセージゲッターをカバー
- `CommandExecutorTests` と `AppLayerSourceGuardTests` を拡張し、launch-failure breadcrumb 構築前のツール / URL / パス / 引数の正規化をカバー
- `CommandExecutorTests` を拡張し、`NormalizeTargetForLog(...)` 内の null ターゲット断片正規化の直接挙動をカバー
- `CommandExecutorTests` を拡張し、null に対する `NormalizeToolPath(...)` ヘルパーの直接挙動をカバー
- `CommandExecutorTests` を拡張し、null に対する `HasUsableTool(...)` ヘルパーの直接挙動をカバー
- `AppLayerSourceGuardTests` を拡張し、Mac リレー breadcrumb 書き込み前の LaunchServices バンドルパス正規化をカバー
- `FileStateSyncNotifierTests` を追加し、`AppLayerSourceGuardTests` を拡張して、sync 警告構築における敵対的な例外メッセージゲッターをカバー
- `FileStateSyncNotifierTests` と `AppLayerSourceGuardTests` を拡張し、それらの断片が警告/情報の crash-log 行に到達する前段での malformed / observed sync ペイロード正規化をカバー
- `FileStateSyncNotifierTests` を拡張し、`NormalizePayloadForLog(...)` 内の null 同期ペイロード正規化の直接挙動をカバー
- `AppLayerSourceGuardTests` を拡張し、write-success/write-failure breadcrumb 書き込み前の sync ファイルパス正規化をカバー

### [1.1.9] - 2026-04-14

### 修正
- `CrashFileLogger` と `DbErrorLogger` は、Warning / Info の message payload が `null` の場合でも `crash.log` と `ErrorLogEntity` へ書き込む前に `(no message payload)` へ正規化するようにし、劣化時のログ経路でも空欄ではなく明示的な診断証跡を残すよう修正

### 変更
- リポジトリ運用ガイダンスを現行の cdidx ワークフローへ追従。`CLAUDE.md` を cdidx 1.9.0 向けに更新し、追跡対象の `.claude/settings.json` では `rg` / `grep` / `find` 系シェル検索を拒否して、既定で cdidx-first の検索経路を維持するように変更

### テスト
- `CrashFileLoggerTests` / `DbErrorLoggerTests` の logging 回帰カバレッジを拡充し、永続化失敗 breadcrumb と Warning / Info の null payload 正規化を検証するよう追加
- `AppLayerSourceGuardTests` を更新し、Warning / Info の正規化後も file-first logging 順序ガードが現在の実装と一致するよう追従

### [1.1.8] - 2026-04-13

### 修正
- `CrashFileLogger.AppendExceptionChain` と `DbErrorLogger` の例外型/メッセージ構築を深さ 32 で上限化し、明示的な truncation マーカーを出力するよう修正。病的に深い inner exception チェーンで last-resort クラッシュロガーが StackOverflow に至らないよう保護

### テスト
- `CrashFileLogger.WriteException` と `DbErrorLogger.Log` に深い inner exception チェーン安全性テストを追加

### [1.1.7] - 2026-04-12

### 修正
- UI イベントハンドラ、同期通知、Dock/QuickLook タイマー、コマンド候補 debounce、クリップボードヘルパー、競合コールバックで未捕捉例外を `crash.log` に永続化するようにし、異常終了時のクラッシュ証跡を確保
- コマンド候補 debounce と Windows IME 再設定パスの `OperationCanceledException` ハンドリングを限定し、キャンセル以外の例外を握りつぶさないよう修正
- `DbErrorLogger` がログ永続化失敗時に inner exception の詳細を保持するよう修正

### 変更
- `MainViewModel` と `App` の重複 dispatcher ログヘルパーを共有 static メソッドに集約
- `CLAUDE.md` の cdidx ガイダンスを v1.6 向けに更新（inspect メタデータ、`suggest_improvement`、MCP 優先 SQL フォールバック方針）

### テスト
- `MainViewModel` の undo/redo ワークフロー統合テストを追加
- `AppLayerSourceGuardTests` を拡充し、新たに例外永続化対応した partial class を網羅
- `DbErrorLogger` の failure detail 保持テストを追加

### [1.1.6] - 2026-04-12

### 修正
- `DockOrderValueCodec.Parse()` は CSV 全体や各 GUID 要素を囲む quote も除去するようにし、quote 付きで保存された Dock 順序でも意図した並びを復元できるよう修正
- `WindowsPathPolicy.IsUncPath()` は quote 付き UNC を受理しつつ `\\\\?\\` と `\\\\.\\` のローカルデバイス接頭辞は共有パスとして誤判定しないよう修正
- Mac `AppDelegate` は key command の system 優先解除失敗も warning 記録するようにし、runtime の reflection 差異が無音にならないよう修正
- `QuickLookPreviewFormatter.BuildLine()` はラベル付き Quick Look 行全体でも要求 `maxLength` を超えないようにし、`label + ": "` 分で最終文字列が上限超過しないよう修正
- `CommandNotFoundRefocusPolicy` は `Command not found:` 判定前に先頭空白を無視するようにし、前置空白つき status でも再フォーカスを維持しつつ埋め込み語句とは誤一致しないよう修正
- Mac `AppDelegate` は `MarshalManagedException` hook 失敗も startup 中に無音で握りつぶさず warning 記録するよう修正
- `MacEntryHandler`、`MacEditorHandler`、Mac の `CommandEntryHandler` は `UIKeyCommand` 入力の reflection 解決失敗も warning 記録したうえで既定キー文字列へフォールバックするようにし、runtime API 差異で handler の型初期化ごと失敗しないよう修正
- `ThemeModeParser.NormalizeOrDefault` は、解析値と呼び出し元既定値の両方が不正 enum でも out-of-range 値を返さず `System` へ安全化するよう修正
- `DbErrorLogger` は background drain loop の予期しない失敗も `crash.log` に warning 記録するようにし、enqueue 済みでも以後のログ永続化失敗が無音にならないよう修正
- `FileStateSyncNotifier.NotifyButtonsChangedAsync` は sync payload の書込失敗も再送出前に warning 記録するようにし、ローカル save/delete 成功後に file signaling が壊れても breadcrumb を残すよう修正
- `MainPage` は Windows の focus-visual reflection 失敗、モーダル primary editor focus 失敗、`IsTabStop` reflection 失敗も warning 記録するようにし、フォールバック経路の silent failure をなくすよう修正
- Mac の middle-click/button-mask と editor key-command/CoreGraphics fallback は失敗時も warning 記録するようにし、native pointer / shortcut bridge 劣化時にローカル診断痕跡を残すよう修正
- Windows `CommandEntryHandler` は `InputScope` 代入の互換性由来失敗と予期しない失敗の両方を warning 記録したうえで無効化または拒否するよう修正
- `FileAppConfigService` は先頭設定ファイルが読めても `theme` 欠落または不正値だった場合にそこで `System` へ確定せず、後続候補へフォールバックするよう修正
- `FileAppConfigService` は壊れた設定・読めない設定・不正な theme 設定をスキップした理由を warning として残すようにし、後続候補へのフォールバック前に `crash.log` に診断 breadcrumb を残すよう修正
- `DbErrorLogger` は DB への永続化失敗や保持期間 purge 失敗も `crash.log` に warning 記録するようにし、非致命なリポジトリエラーでも shutdown / 劣化動作時の診断痕跡を残すよう修正
- `DbErrorLogger.FlushAsync(timeout)` は timeout や予期しない flush 失敗も warning 記録するようにし、graceful shutdown 中のログ欠落が無音にならないよう修正
- `Praxis/Platforms/MacCatalyst/Program.cs` の UTF-8 BOM を除去し、同 entrypoint を BOM-free に保つ repository encoding guard テストを追加して `cdidx validate` をクリーン化
- `CommandExecutor` は native process 起動失敗や launch-target-resolution 失敗も `crash.log` に warning 記録するようにし、ユーザー向け失敗メッセージの裏側にローカル診断 breadcrumb を残すよう修正
- `LaunchTargetResolver` は環境変数展開後の引用符も正規化するようにし、引用符付き env 由来の HTTP URL や filesystem path も正しく解決できるよう修正
- `AppStoragePaths` は壊れた legacy path 比較入力を無視するようにし、`Path.GetFullPath` 例外でストレージ移行チェック全体が止まらないよう修正
- `CommandRecordMatcher` は command 候補走査中に `null` コレクション要素を無視するようにし、候補生成で例外落ちしないよう修正
- `StateSyncPayloadParser` は二重区切り payload を空セグメント圧縮で受理しないようにし、壊れた同期シグナルを拒否するよう修正
- `QuickLookPreviewFormatter` は省略記号つき短縮後も要求 `maxLength` を超えないようにし、呼び出し元の長さ制約を破らないよう修正
- `ButtonSearchMatcher`、`LogRetentionPolicy`、`LauncherButtonOrderPolicy` は `null` 要素を無視するようにし、検索・保持期間計算・配置正規化 helper 内で例外落ちしないよう修正
- `CommandWorkingDirectoryPolicy` は shell 判定前に環境変数つき tool path を展開するようにし、`%ComSpec%` や引用符付き `%...%` Windows shell tool でもユーザープロファイル起点 working directory を選べるよう修正
- `CommandLineBuilder` は quoted-empty の tool 値を空として扱うようにし、実行時の empty-tool 判定と preview/status 表示を整合させるよう修正
- `AppStoragePathLayoutResolver` は app-data path 合成前に保存先 root の外側引用符を除去するよう修正
- `GridSnapper` と `ModalEditorScrollHeightResolver` は非有限数値入力を安全化するようにし、`NaN` / infinity がレイアウト計算へ伝播しないよう修正
- Windows startup-log 追記失敗と `MainPage` copy notice animation 失敗は、無言で握りつぶさず `crash.log` に breadcrumb を残すよう修正

### [1.1.5] - 2026-04-11

### 修正
- `MainViewModel.CommandSuggestions` は候補ポップアップ close / refresh の main-thread dispatch 失敗も warning ログに残すようにし、スケジューリング失敗を無言で消さないよう修正
- `MauiThemeService.Apply()` は適用済み theme への no-op 再適用を避けるようにし、Mac の window-style dispatch 失敗は `crash.log` に残すよう修正
- Mac Catalyst の open relay 起動は `Process.Start(...) == null` も失敗扱いにし、LaunchServices relay 失敗を無言で握り潰さず `crash.log` へ記録するよう修正
- `FileStateSyncNotifier.Dispose()` は watcher の unsubscribe / dispose 前に `EnableRaisingEvents` を false にし、teardown 中の遅延 callback race を減らすよう修正
- ベース `App` のグローバル unhandled-exception / process-exit hook は一度だけ登録するようにし、アプリ初期化が再入した場合でも crash / flush handler が多重化しないよう修正
- `MainViewModel.SyncThemeFromExternalChangeAsync()` は dispatch 先メインスレッド適用で発生した失敗も warning ログ化するようにし、外部 reload の `TaskCompletionSource` には `RunContinuationsAsynchronously` を付けて継続の再入を抑制
- `FileStateSyncNotifier.NotifyButtonsChangedAsync()` は dispose 後は no-op にし、teardown 中に stale な sync file 書き込みを試みないよう修正
- `AppStoragePaths.TryMigrateLegacyDatabase()` はコピー失敗を warning 記録しつつ次の legacy 候補探索を継続するようにし、最初の読めない DB で移行全体が止まらないよう修正
- `FileAppConfigService` は先頭設定ファイルが `UnauthorizedAccessException` で読めない場合でも後続候補へフォールバックするよう修正
- `MainPage.OnDisappearing()` は window activation hook を解除し、解除経路では Mac の activation observer も解放するようにして、非表示ページへ stale な activation callback が残らないよう修正
- `CommandExecutor` は `~` / `~/...` / `~\\...` のような home 省略つき `tool` も実行可否判定前に展開するようにし、empty-tool path launch と同じ shorthand を direct tool launch でも使えるよう修正
- Windows の `startup.log` は raw な local-app-data special folder 文字列ではなく正規化済み共有 app-storage root を使うようにし、quote 付きや欠落した環境値でパスが壊れにくいよう修正
- Windows / Mac の platform startup class はグローバル例外 hook の多重登録を防ぐガードを追加し、初期化の再実行で unhandled-exception handler が重複しないよう修正
- `App.CreateWindow()` は `ResolveRootPage()` 失敗時のエラー表示ページを恒久キャッシュしないようにし、後続ウィンドウ生成で初回失敗ページに固定されず回復できるよう修正
- `App` は `AppDomain.UnhandledException` の終端処理と `ProcessExit` の両方でログ flush 失敗を黙殺せず `crash.log` に残すよう修正
- `FileStateSyncNotifier` は sync ファイルの再読込リトライが尽きた場合に warning を残し、読めない外部同期 payload を無言で捨てないよう修正
- `CommandExecutor` は正規化後に空になる tool 値（空引用符や引用符内空白）を「tool 未指定」として扱い、空ファイル名を実行しようとせず URL / path フォールバックへ回すよう修正
- `MainPage` は XAML 読込失敗を `crash.log` に記録し、初回初期化失敗時は初期化済みフラグを戻して再試行可能にし、初期化エラー表示自体が失敗した場合も元の例外を消さず別途 `crash.log` へ残すよう修正
- `MauiClipboardService` は clipboard 読み書きの両方で `CancellationToken` を尊重し、キャンセル済み操作を無視して走らせないよう修正
- `SqliteAppRepository` はスキーマ更新と初回キャッシュ読込が成功するまで共有 SQLite 接続を公開しないようにし、初期化途中失敗後に安全に再試行できるよう修正
- `SqliteAppRepository.SetThemeAsync` は範囲外の `ThemeMode` 値を `System` へ正規化して保存し、外部同期で `dock_order` が空になった場合は古い Dock 表示を残さず明示的にクリアするよう修正
- `MainViewModel` は外部 reload/theme 同期失敗、command 候補再計算失敗、競合ダイアログ callback 失敗を無言で握り潰さず warning ログへ残すよう修正
- Windows のクリアボタン後 native refocus 失敗は `crash.log` へ直接同期記録するようにし、freeze/abort 系の診断痕跡を残しやすくした
- 起動、外部同期、実行リクエスト、クリップボード反映、クリアボタン、sync signal の境界に低コストな Info breadcrumb を追加し、GUI ハング/abort 時に最後に成功していた段階を追いやすくした
- clipboard と sync notifier の失敗を主処理から分離し、clipboard 引数読込は空文字へフォールバック、コピー失敗は warning/status 化、clipboard 反映失敗後も実行ログを保持し、save/delete/theme/dock/history は同期通知失敗で巻き戻らないようにした

### [1.1.4] - 2026-04-09

### 追加
- .codex および CLAUDE.md を追加

### 修正
- CI および Delivery ワークフローで .NET 10 プレビューランタイムパッケージの復元が失敗する問題を修正（`NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64 with version (= 10.0.5)`）— 固定指定の `dotnet-version: 10.0.100` を `10.0.x` + `dotnet-quality: preview` に変更しプレビュー NuGet フィードを有効化

### [1.1.3] - 2026-04-05

### 追加
- `v*` タグ push 時に GitHub Release を自動作成 — delivery ワークフローが OS 別アーティファクト（Windows / macOS）を zip 化し、自動生成リリースノート付きで公開

### [1.1.2] - 2026-03-31

### 追加
- 同期ファイルベースのクラッシュロガー（`CrashFileLogger`）: 全ログ呼び出しで `crash.log` に即座に同期書き込みし、非同期 DB 書き込みが完了しないまま異常終了してもログを保持
  - クロスプラットフォーム対応: Windows `%LOCALAPPDATA%\Praxis\crash.log`、macOS `~/Library/Application Support/Praxis/crash.log`
  - 512 KB での自動ログローテーション
  - InnerException、`AggregateException` 展開、`Exception.Data` 辞書を含む完全な例外チェーン出力
- `IErrorLogger.LogWarning(message, context)` — 警告レベルのログエントリ追加
- `IErrorLogger.FlushAsync(timeout)` — シャットダウン時に保留中の非同期 DB 書き込みをドレイン
- `AppDomain.ProcessExit` ハンドラでプロセス終了前にログをフラッシュ
- `UnhandledException` ハンドラで `IsTerminating=true` の場合に同期的フラッシュを試行
- Mac Catalyst `AppDelegate` にクラッシュファイルログフック追加（`UnhandledException`、`UnobservedTaskException`、`MarshalManagedException`）
- Windows プラットフォーム例外ハンドラが `startup.log` と `crash.log` の両方に出力
- `UnhandledException` ハンドラで Exception 以外のスローオブジェクトも記録

### 変更
- `DbErrorLogger` を書き換え: 全 `Log`/`LogInfo`/`LogWarning` 呼び出しでまずクラッシュファイルに同期書き込みし、次に `ConcurrentQueue` 経由で非同期 DB 書き込みをキューイング（従来の fire-and-forget `_ = LogAsync()` パターンを置換）
- エラーログエントリが完全な例外型チェーン（例: `InvalidOperationException -> NullReferenceException`）、連結された内部メッセージ、`Exception.ToString()` による完全スタックトレースを記録
- `ErrorLogEntity.Level` 列が `Error`・`Info` に加えて `Warning` を受容
- `ResolveRootPage` の失敗を `IErrorLogger` でログ出力（従来は無言で握り潰し）

### [1.1.1] - 2026-03-28

### 修正
- GitHub Actions の checkout で履歴をフル取得するようにし、CI と配布ジョブで Nerdbank.GitVersioning の version height 計算が shallow clone で失敗しないよう修正
- macOS の GitHub Actions ジョブで Mac Catalyst の build / publish 前に Xcode を初期化し、fresh runner 上の `ibtoold` / Xcode プラグイン初期化失敗を回避

### 変更
- README 冒頭に CI / CodeQL / Delivery / .NET 10 / .NET MAUI / 対応プラットフォーム / SQLite / MIT License のバッジを追加

### [1.1.0] - 2026-03-28

### 追加
- ボタン単位の色反転（インバート）機能、DB スキーマ自動マイグレーション付き（v1 → v2）
- DB バックの ERROR / INFO 2レベルエラーログ（`ErrorLog` テーブル、30日保持、schema v3 → v4）
- 主要ユーザー操作への INFO ログ: ボタン/コマンド実行、エディタ開閉/保存/キャンセル/削除、テーマ変更、Undo/Redo、競合解決、ウィンドウ閉鎖
- ボタン変更（移動/編集/削除）の Undo/Redo: Windows は Ctrl+Z / Ctrl+Y、macOS は Command+Z / Command+Shift+Z
- ボタンホバー時の Quick Look プレビュー（Command / Tool / Arguments / Clip Word / Note）
- `PRAGMA user_version` による SQLite スキーマバージョン管理と順次自動マイグレーション
- `FileSystemWatcher` シグナルファイル（`buttons.sync`）によるウィンドウ間同期（自インスタンス発信は除外）
- 複数ウィンドウ同時編集の競合検出ダイアログ（`Version` 列による楽観的ロック）
- Enter キーで一致するすべてのコマンドを実行（先頭一致だけでなく全一致）
- Clip Word フィールドの複数行入力対応
- ボタン新規作成時に検索テキストを自動クリア
- コンテキストメニュー・競合ダイアログの矢印キーフォーカス循環（Windows/macOS）
- コマンド候補行へのミドルクリック・右クリック操作
- コマンド候補のデバウンスを 400 ms に延長し高速入力時のノイズを軽減
- 候補ポップアップ表示直後は自動選択せず、最初の `↓` キーで先頭候補を選択
- CI カバレッジ収集と Cobertura アーティファクトアップロード（GitHub Actions）
- Windows UNC パスを `explorer.exe` 経由で開き、存在確認前に認証ダイアログを表示可能に
- Dock 横スクロールバーを「Dock 領域ホバー中かつ横オーバーフロー時のみ」表示
- 色反転ラベルをタップしてチェックボックスをトグル可能に（チェックボックス本体以外もタップ可）

### 修正
- コンテキストメニュー表示時にコマンド候補を自動クローズ
- コマンド候補クリックでコマンド実行・入力欄への自動補完
- エディタモーダルの既定フォーカスを、Button Text / Command 入れ替え後の欄順に合わせて `ButtonText` へ修正
- 新規ボタン作成時は、モーダル初回フォーカスの `ButtonText` を Windows / macOS で全選択
- Windows: Tab フォーカス移動時にテキスト入力欄の全選択
- クリアボタンタップ後のフォーカス復帰を安定化（即時試行 + 短遅延リトライ）
- Windows: 上部 `Command` / `Search` のクリア後再フォーカスで stale な native `TextBox` を避け、まれな abort を抑止
- macOS: クリア後再フォーカスを次フレームへ遅延し、クリアボタン非表示切替中の responder 再入を回避
- Windows のクリアボタン X グリフの垂直方向センタリング
- テーマのライブ切替中もコマンド候補の色をテーマに同期
- `CommandEntry` / `SearchEntry` で英小文字が大文字変換される問題
- メインの command 入力欄で、Windows / macOS ともフォーカス時の IME / 入力ソース切替を行わないよう修正
- モーダル `Command` 欄の IME / ASCII 強制:
  - Windows: フォーカス時に `InputScopeNameValue.AlphanumericHalfWidth` + `imm32` ナッジ（即時 + 短遅延リトライ）
  - macOS: `AsciiInputFilter` + `setMarkedText` / `insertText` ブロック（アプリ非アクティブ時は即時解除）
- Windows のモーダル `Command` 欄でフォーカス中も IME を英字に再強制（手動 IME 切替を抑止）
- macOS: モーダル command 欄の ASCII 入力ソース強制をアクティブキーウィンドウのファーストレスポンダ中のみに限定
- macOS: 「Command not found」後にコマンド入力欄へフォーカスを戻し即時リトライ可能に
- macOS: ILLink によるクリアボタンパスの入力欠落を中間アセンブリコピーで防止
- Windows: モーダル/競合フォーカス復帰を 2 段リトライ化し Esc・Ctrl+S の取りこぼしを防止
- Windows: `InputScope` の `ArgumentException`（E_RUNTIME_SETVALUE）を一方向フラグで吸収し IME フォールバックへ継続
- Windows: Ctrl+Z/Y のアンドゥ粒度を保持（コマンド入力の `TextChanging` 書き換えを無効化）
- 単一ウィンドウ編集での競合ダイアログ誤検知を解消（インスタンス ID 自己フィルタ）
- 「Command not found」をニュートラル表示ではなくエラーフラッシュ（赤）として扱う
- macOS で別アプリから戻った後の編集モーダル再フォーカス

### 変更
- エディタモーダルのフィールド順を変更（Command を Button Text より前に）
- UI ボタンのフォント サイズを全プラットフォームで 12 に統一
- 配置領域と Dock のボタンの padding を 0 に統一
- Dock 領域の縦幅を拡張
- 新規ボタンアイコンをプレーンな `+` から線画ヘックスロゴに変更（外六角形・内接円・内六角形・中央 `+`）
- アプリアイコン・スプラッシュを六角形＋ポリゴンコントラストデザインに刷新（マイクロサイズ最適化バリアント付き）
- `MainPage` の責務分割をさらに細分化し、`EditorAndInput` は共有入力処理へ絞り込み、編集モーダル、ViewModel イベント配線、ステータス/テーマ、Dock/Quick Look、Windows ネイティブ入力フックを専用 partial へ分離
- `SqliteAppRepository` の全公開操作を排他制御で保護しスレッドセーフに
- UI 遅延値を `UiTimingPolicy` へ集約
- `MainPage` フィールドファイルと `MauiProgram` ハンドラ登録のプラットフォームプリプロセッサブロックを整理・統合
- 重複 `using` ディレクティブの削除と `using` 順序の正規化
- `MainViewModel` と各 partial クラスに主要ライフサイクルイベントの `LogInfo` 呼び出しを追加

[Unreleased]: https://github.com/Widthdom/Praxis/compare/v1.1.11...HEAD
[1.1.11]: https://github.com/Widthdom/Praxis/compare/v1.1.10...v1.1.11
[1.1.10]: https://github.com/Widthdom/Praxis/compare/v1.1.9...v1.1.10
[1.1.9]: https://github.com/Widthdom/Praxis/compare/v1.1.8...v1.1.9
[1.1.8]: https://github.com/Widthdom/Praxis/compare/v1.1.7...v1.1.8
[1.1.7]: https://github.com/Widthdom/Praxis/compare/v1.1.6...v1.1.7
[1.1.6]: https://github.com/Widthdom/Praxis/compare/v1.1.5...v1.1.6
[1.1.5]: https://github.com/Widthdom/Praxis/compare/v1.1.4...v1.1.5
[1.1.4]: https://github.com/Widthdom/Praxis/compare/v1.1.3...v1.1.4
[1.1.3]: https://github.com/Widthdom/Praxis/compare/v1.1.2...v1.1.3
[1.1.2]: https://github.com/Widthdom/Praxis/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/Widthdom/Praxis/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/Widthdom/Praxis/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Widthdom/Praxis/tree/v1.0.0
