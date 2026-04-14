# Testing Guide

## Scope
This document is the test-specific source of truth for Praxis.
Use it for test execution, coverage checks, and file-by-file intent.

## Test Stack
- Framework: xUnit
- Runner: `Microsoft.NET.Test.Sdk`
- Coverage collector: `coverlet.collector` (`XPlat Code Coverage`)
- Test project: [`Praxis.Tests/Praxis.Tests.csproj`](../Praxis.Tests/Praxis.Tests.csproj)
- Primary target: `net10.0`

## How To Run
Run all tests:
```bash
dotnet test Praxis.Tests/Praxis.Tests.csproj
```

Run all tests from solution entry:
```bash
dotnet test Praxis.slnx
```

Collect coverage (Cobertura XML):
```bash
dotnet test Praxis.Tests/Praxis.Tests.csproj --collect:"XPlat Code Coverage"
```

## Testing Conventions
- Keep UI-agnostic rules in `Praxis.Core/Logic` and test them in `Praxis.Tests`.
- Prefer deterministic test data (fixed `DateTime`, explicit inputs/outputs).
- Cover both positive and negative branches for policy/resolver methods.
- For platform policies, test by explicit boolean inputs (`isWindows`, `isFocused`, etc.) instead of runtime platform checks.
- Treat README behavior claims as user-facing only; implementation assertions belong in tests and developer docs.

## Test Inventory (File By File)

### Baseline / Cross-Cutting
- [`UnitTest1.cs`](../Praxis.Tests/UnitTest1.cs) (`CoreLogicTests`): baseline checks for command-line build, snapping, search matching, and retention.
- [`CoreLogicEdgeCaseTests.cs`](../Praxis.Tests/CoreLogicEdgeCaseTests.cs): parser/snapper/matcher/retention edge cases, including invalid `ThemeMode` enum normalization, invalid fallback-default sanitization back to `System` for both enum and string parse entrypoints, and non-finite grid-snapping safety.
- [`RepositoryEncodingPolicyTests.cs`](../Praxis.Tests/RepositoryEncodingPolicyTests.cs): repository encoding guard for source files that must remain BOM-free, currently protecting [`Praxis/Platforms/MacCatalyst/Program.cs`](../Praxis/Platforms/MacCatalyst/Program.cs) because `cdidx validate` flags UTF-8 BOMs as index-quality issues.
- [`StateSyncPayloadParserTests.cs`](../Praxis.Tests/StateSyncPayloadParserTests.cs): sync-signal payload parsing coverage for valid payloads plus malformed, double-separator, blank-source, non-numeric, and out-of-range timestamp rejection.
- [`CoreLogicPerformanceSafetyTests.cs`](../Praxis.Tests/CoreLogicPerformanceSafetyTests.cs): regression-safety checks for defaults, bounds, conflict detection, null-safe search/retention helpers, and timestamp-only drift vs material-content conflict cases.
- [`PolicyTruthTableTests.cs`](../Praxis.Tests/PolicyTruthTableTests.cs): full truth-table validation for focus-related policy combinations, including command-entry native-activation focus and Windows native-focus safety gates.
- [`MainPageStructureTests.cs`](../Praxis.Tests/MainPageStructureTests.cs): source-structure guard for the `MainPage` partial split (field declarations stay in `MainPage.Fields.*.cs`, while behavior remains separated into concern-based files such as [`MainPage.ModalEditor.cs`](../Praxis/MainPage.ModalEditor.cs), [`MainPage.StatusAndTheme.cs`](../Praxis/MainPage.StatusAndTheme.cs), [`MainPage.DockAndQuickLook.cs`](../Praxis/MainPage.DockAndQuickLook.cs), and [`MainPage.WindowsInput.cs`](../Praxis/MainPage.WindowsInput.cs) instead of drifting back into [`MainPage.xaml.cs`](../Praxis/MainPage.xaml.cs)), plus XAML/source guards for placement/dock button-label font sizing (`12` on all platforms), editor-modal field order/default focus alignment (`ButtonText` appears before `Command` and is the default modal focus target), new-button initial `ButtonText` select-all on Windows/macOS, modal-only ASCII-enforcement opt-in (`ModalCommandEntry` sets `EnforceAsciiInput="True"` while `MainCommandEntry` does not), and inverted-theme UI wiring (`UseInvertedThemeColors` triggers + editor checkbox binding + flat-square checkbox rendering hooks + text-field-matched border/background colors + equal-thickness edge line settings + polyline checkmark settings + label tap handler so clicking the "Use opposite theme colors for this button" text also toggles the checkbox). Also guards suggestion-row mouse interaction wiring: `CommandSuggestionScrollView` is named in XAML, middle/secondary-click gesture handlers are present in both [`MainPage.ShortcutsAndConflict.cs`](../Praxis/MainPage.ShortcutsAndConflict.cs) (non-Mac rebuild path) and [`MainPage.MacCatalystBehavior.cs`](../Praxis/MainPage.MacCatalystBehavior.cs) (Mac rebuild path), `TryGetSuggestionItemAtRootPoint` appears before `TryGetPlacementButtonAtRootPoint` in `HandleMacMiddleClick`, and `CommandSuggestionScrollView.ScrollY` is used for scroll-offset-aware hit testing.
- [`AppLayerSourceGuardTests.cs`](../Praxis.Tests/AppLayerSourceGuardTests.cs): source-level regression guards for app-layer failure handling that is hard to compile directly into the lightweight test host. Verifies `MainPage` writes XAML/initialization/display-alert failures as crash-log exceptions while also routing fallback UI text through `CrashFileLogger.SafeExceptionMessage(...)`, and records copy-notice-animation/status-flash/Dock/Quick Look/button-tap/secondary-tap failures as crash-log warnings, with copy notice, status flash, Dock hover-exit hide, Quick Look show/hide, button tap execution, secondary-tap create flow, modal primary-focus fallback, `UseSystemFocusVisuals` fallback, and `IsTabStop` fallback now routed through `CrashFileLogger.SafeExceptionMessage(...)`; flips `initialized` only after successful startup, detaches activation hooks on disappear, and warning-logs Windows reflection/tab-stop/modal-primary-focus fallback failures; `MauiClipboardService` honors cancellation tokens for both read/write paths; `SqliteAppRepository` only publishes its shared SQLite connection after schema/bootstrap load succeeds; `DbErrorLogger` preserves the file-first-then-DB logging order and warning-logs unexpected drain-loop failures; base `App` does not cache fallback error pages, registers process-wide exception hooks only once, records shutdown flush failures as full crash-log exceptions plus warning breadcrumbs, records Windows startup-log directory/build/append failures through `SecondaryFailureLogger`, and keeps all static event-dispatch helpers routed through shared exception-logging wrappers; platform startup entrypoints guard duplicate global exception-hook registration and Windows startup-log path resolution, and Mac `AppDelegate` warning-logs failures to hook `MarshalManagedException` plus key-command priority fallback failures through `CrashFileLogger.SafeExceptionMessage(...)`; Windows `CommandEntryHandler` disables unsupported `InputScope` writes after compatibility exceptions and warning-logs both compatibility-triggered and unexpected assignment failures through `CrashFileLogger.SafeExceptionMessage(...)`; `CommandExecutor` now keeps process-start and launch-target-resolution breadcrumbs while normalizing logged tool / URL / path / argument fragments before interpolation; `MainViewModel` source guards now cover safe warning-message construction for external-theme dispatch failures, command-suggestion debounce/dispatch/lookup failures, and the shared warning-factory helper used by conflict callbacks plus clipboard/sync/persistence follow-up logging, keeps debounce cancellation limited to its own token, and uses asynchronous continuation bridging for external reload; `AppStoragePaths` source guards now cover safe warning-message construction for legacy migration failures and invalid path comparisons, with migration source/comparison path fragments normalized before they are interpolated into warning prefixes; `FileAppConfigService` warning-logs skipped config candidates through `CrashFileLogger.SafeExceptionMessage(...)`, normalizes logged config paths before interpolation, and falls back on unauthorized or invalid config reads; `SecondaryFailureLogger` now keeps `originalMessage` diagnostics even when the startup payload is whitespace-only by normalizing it through `CrashFileLogger.NormalizeMessagePayload(...)`, while also normalizing fallback target-path/operation fragments before they are interpolated into the secondary diagnostics file; `MauiThemeService` skips no-op applies and records Mac dispatch failures via the crash log; Mac open-relay startup plus middle-click/key-command/CoreGraphics fallback paths and deferred middle-click execution now record crash-log warnings, with LaunchServices relay, `buttonMaskRequired`, deferred middle-click execution, Mac editor key-command creation, and CoreGraphics middle-button-state failures also routed through `CrashFileLogger.SafeExceptionMessage(...)`, and Mac LaunchServices bundle-path fragments normalized before interpolation; Mac entry/editor/command handlers also warning-log `UIKeyCommand` key-input reflection failures through `CrashFileLogger.SafeExceptionMessage(...)` while falling back to baked key literals; and `FileStateSyncNotifier` warning-logs sync-file write/read failures plus unexpected publish failures while ignoring notify requests after disposal and disabling event raising during teardown, with malformed payload breadcrumbs, sync-file path fragments, and observed-source info normalized before interpolation into the crash log.
- [`MainViewModelWarningMessageTests.cs`](../Praxis.Tests/MainViewModelWarningMessageTests.cs): helper-level coverage for `MainViewModel.BuildSafeWarningMessage(...)`, locking the shared warning formatter to collapse multiline exception messages, map whitespace-only messages to `(empty)`, and preserve a safe fallback body when either the original exception or the warning factory failure itself carries hostile, multiline, or empty payloads.
- [`MainViewModelWorkflowIntegrationTests.cs`](../Praxis.Tests/MainViewModelWorkflowIntegrationTests.cs): workflow integration tests for `create -> edit -> execute -> external sync` plus command-suggestion behavior (`popup opens with no preselected row`, first `Down` selects index `0`, and pick executes/closes correctly) using linked [`MainViewModel`](../Praxis/ViewModels/MainViewModel.cs) sources and test doubles for repository/executor/sync services; also covers initialization fallback for theme/dock read failures, full exception + warning logging for initial/external theme and dock reload failures, hostile / multiline / whitespace-only exception-message payloads on external theme sync / command lookup fallback / conflict callback / clipboard follow-up / sync notification / theme persistence warning paths, undo/redo for create and delete history (including dock-order restoration), undo cancellation when an affected button changes in another window, external empty-dock sync, deferred external sync while the editor is open, command-input multi-match execution success/partial-failure aggregation, execution-request/clipboard/completion info logs, clipboard read/write fallback with persisted exceptions, non-fatal sync-notifier failures after local save/delete/theme changes with persisted exceptions, non-fatal launch-log/dock/theme persistence failures after local success with persisted exceptions, full exception + warning logging for command lookup fallback failure handling, and full exception + warning logging when the conflict-dialog callback throws.
- [`CiCoverageWorkflowPolicyTests.cs`](../Praxis.Tests/CiCoverageWorkflowPolicyTests.cs): workflow-configuration guard that verifies the CI test step collects `XPlat Code Coverage` and uploads the Cobertura artifact, plus documented CI/delivery invariants — `fetch-depth: 0` for Nerdbank.GitVersioning, `dotnet-quality: ga` SDK pinning, `dotnet workload install maui` without `--skip-manifest-update`, `sudo xcodebuild -runFirstLaunch` plus the Xcode compatibility gate, the expected Windows/Mac Catalyst target frameworks with disabled Mac signing, and the intentionally empty Windows RID in `delivery.yml`.

### Models / Defaults
- [`ModelDefaultsTests.cs`](../Praxis.Tests/ModelDefaultsTests.cs): default values and initialization guarantees for `LauncherButtonRecord`, `LaunchLogEntry`, and `ErrorLogEntry`, including unique default IDs, copy-constructor null guards, and clone instance independence.
  - Includes copy-constructor / `Clone()` full-field copy regression checks for `LauncherButtonRecord` (including `UseInvertedThemeColors`).
- [`LauncherButtonOrderPolicyTests.cs`](../Praxis.Tests/LauncherButtonOrderPolicyTests.cs): placement-order normalization (`Y`, then `X`) while preserving stable order for ties, skipping null entries safely, handling empty/null inputs correctly, and returning a new list independent from the source collection.

### Logging / Crash Safety
- [`CrashFileLoggerTests.cs`](../Praxis.Tests/CrashFileLoggerTests.cs): synchronous crash file logger behavior — Windows/macOS path resolution, quoted/invalid platform-directory fallback handling, no-throw guarantees for all write methods (`WriteException`, `WriteInfo`, `WriteWarning`), direct helper coverage for source/context normalization and `NormalizeMessagePayload(...)`, blank/null source and info/warning payload placeholder handling, single-line normalization for breadcrumb headers/messages and exception messages, `(empty)` normalization for whitespace-only exception/object payloads both in helpers and in persisted exception bodies, inner exception chain capture, `AggregateException` child capture, bounded wide-aggregate summary fallback plus middle/tail sampling markers, `Exception.Data` capture, fallback markers when custom `Message` / `StackTrace` getters or `Exception.Data` key/value `ToString()` implementations throw, including multiline/whitespace-only getter-failure messages inside those fallback markers, file write verification, and thread safety under concurrent writes without blocking-task analyzer warnings.
- [`DbErrorLoggerTests.cs`](../Praxis.Tests/DbErrorLoggerTests.cs): database error logger behavior — no-throw guarantees for `Log`/`LogInfo`/`LogWarning`, null exception payload placeholder persistence, blank/null context and info/warning payload normalization across crash-file and DB persistence, single-line exception-message normalization, `(empty)` persistence for whitespace-only exception messages, fallback-marker persistence when custom `Message` / `StackTrace` getters throw, `FlushAsync` verification for queued and already-in-flight writes, persisted context/level fields for Error/Info/Warning entries, full exception type chain capture (including `AggregateException`), bounded stack-trace capture with aggregate index labels, timeout behavior, Error-only retention purge behavior, and crash-log breadcrumbs plus exception bodies for repository write, purge, or timeout failures across Error/Warning/Info persistence paths, including multiline/whitespace-only repository exception messages and normalized multiline contexts inside persist-failure breadcrumbs.
- [`SecondaryFailureLoggerTests.cs`](../Praxis.Tests/SecondaryFailureLoggerTests.cs): independent fallback-diagnostics sink behavior — when the normal `%LOCALAPPDATA%\\Praxis` crash/startup roots are unavailable, startup-log diagnostics fall back to temp/current-directory files, keep the original hostile-exception payload markers, normalize multiline/whitespace-only failure messages inside the fallback warning body, normalize original startup messages (multiline to single-line, whitespace-only to the standard placeholder), normalize logged startup target-path/operation fragments before they reach the fallback file, and still persist the secondary write-failure context.
- [`AppStoragePathsTests.cs`](../Praxis.Tests/AppStoragePathsTests.cs): storage-path helper behavior — quoted `LOCALAPPDATA` normalization, absolute/relative legacy path handling, invalid path-comparison fallback logging, and legacy-migration warning construction that collapses multiline exception messages, maps whitespace-only messages to `(empty)`, and tolerates hostile exception-message getters.

### Command Execution / Matching / Suggestions
- [`CommandLineBuilderTests.cs`](../Praxis.Tests/CommandLineBuilderTests.cs): null/whitespace handling, quoted-empty tool normalization, and command-line construction.
- [`CommandExecutorTests.cs`](../Praxis.Tests/CommandExecutorTests.cs): linked-source coverage for home-path expansion helpers (`~`, `~/...`, `~\\...`), non-home relative path pass-through, quoted-tool normalization, env-expanded quoted tool normalization, normalized empty/quoted-empty tool detection, env-expanded Windows shell launch working-directory override to the user profile, crash-log breadcrumbs when native process launch throws, and launch-failure message construction that collapses multiline exception messages, maps whitespace-only messages to `(empty)`, tolerates hostile exception-message getters, and normalizes logged tool/path/URL/argument fragments before they are interpolated into warning prefixes.
- [`CommandRecordMatcherTests.cs`](../Praxis.Tests/CommandRecordMatcherTests.cs): exact command matching rules, null guards including null collection entries, and case/trim behavior.
- [`CommandSuggestionVisibilityPolicyTests.cs`](../Praxis.Tests/CommandSuggestionVisibilityPolicyTests.cs): close policy when context menu opens, locked as a direct mirror of current suggestion-open state.
- [`CommandSuggestionRowColorPolicyTests.cs`](../Praxis.Tests/CommandSuggestionRowColorPolicyTests.cs): selected/unselected row color decisions per theme, including transparent unselected rows and distinct selected palettes for light vs dark themes.
- [`CommandWorkingDirectoryPolicyTests.cs`](../Praxis.Tests/CommandWorkingDirectoryPolicyTests.cs): pure policy coverage for which Windows shell-like tools (`cmd`, `powershell`, `pwsh`, `wt`) should start from the user-profile working directory, including mixed-case executable names and env-expanded quoted tool paths.
- [`CommandNotFoundRefocusPolicyTests.cs`](../Praxis.Tests/CommandNotFoundRefocusPolicyTests.cs): refocus decision for `Command not found:` status, including acceptance after leading whitespace and rejection when the colon is missing or the phrase is not message-leading after trimming.
- [`StatusFlashErrorPolicyTests.cs`](../Praxis.Tests/StatusFlashErrorPolicyTests.cs): status classification for error flash behavior, including embedded `error` / `exception` / `not found` terms and neutral-message rejection.
- [`QuickLookPreviewFormatterTests.cs`](../Praxis.Tests/QuickLookPreviewFormatterTests.cs): quick-look preview text normalization, max-length-safe truncation, exact-length pass-through, full-line length enforcement for labeled output, and argument guards for invalid lengths or blank labels.

### Undo / Redo
- [`ActionHistoryTests.cs`](../Praxis.Tests/ActionHistoryTests.cs): command-pattern history stack behavior (constructor guards, undo/redo transitions, failed-apply recovery on both stacks, `Clear()`, and capacity trimming).
- [`ButtonHistoryConsistencyPolicyTests.cs`](../Praxis.Tests/ButtonHistoryConsistencyPolicyTests.cs): optimistic-lock version-match checks used when applying undo/redo mutations (including mismatch detection when `UseInvertedThemeColors` differs).

### Input / Keyboard / Focus Policies
- [`CommandEntryBehaviorPolicyTests.cs`](../Praxis.Tests/CommandEntryBehaviorPolicyTests.cs): command entry role flags for navigation shortcuts and activation-time native refocus, with full enabled/editor/conflict combination coverage.
- [`WindowActivationCommandFocusPolicyTests.cs`](../Praxis.Tests/WindowActivationCommandFocusPolicyTests.cs): activation-time command focus gating.
- [`SearchFocusGuardPolicyTests.cs`](../Praxis.Tests/SearchFocusGuardPolicyTests.cs): macOS search-focus guard decision rules.
- [`AsciiInputFilterTests.cs`](../Praxis.Tests/AsciiInputFilterTests.cs): ASCII filtering rules used by macOS modal command input paths.
- [`MacCommandInputSourcePolicyTests.cs`](../Praxis.Tests/MacCommandInputSourcePolicyTests.cs): macOS ASCII input-source enforcement gating (first-responder + key-window + app-active + per-entry opt-in) and focused re-apply interval safety.
- [`WindowsCommandInputImePolicyTests.cs`](../Praxis.Tests/WindowsCommandInputImePolicyTests.cs): Windows IME ASCII-mode gating (`ShouldForceAsciiImeMode` with per-entry opt-in), focus-time ASCII nudge retry schedule, focused-state ASCII reassert gating/interval, conversion-mode normalization, and caret clamp logic.
- [`WindowsInputScopeCompatibilityPolicyTests.cs`](../Praxis.Tests/WindowsInputScopeCompatibilityPolicyTests.cs): fallback trigger rules when native `InputScope` assignment fails (`ArgumentException`), including derived-argument exceptions and rejection of non-argument wrappers.
- [`WindowsModalFocusRestorePolicyTests.cs`](../Praxis.Tests/WindowsModalFocusRestorePolicyTests.cs): Windows editor/conflict focus restore conditions.
- [`ConflictDialogFocusRestorePolicyTests.cs`](../Praxis.Tests/ConflictDialogFocusRestorePolicyTests.cs): editor focus restore condition after conflict dialog close.
- [`EditorShortcutActionResolverTests.cs`](../Praxis.Tests/EditorShortcutActionResolverTests.cs): key-to-action mapping for modal/context/conflict shortcuts.
- [`EditorShortcutScopeResolverTests.cs`](../Praxis.Tests/EditorShortcutScopeResolverTests.cs): active scope decision when overlays are open/closed.
- [`EditorTabInsertionResolverTests.cs`](../Praxis.Tests/EditorTabInsertionResolverTests.cs): tab-character fallback detection and navigation mapping, including start-of-text insertions, backward-tab handling, null-input no-op paths, and overload parity.
- [`FocusRingNavigatorTests.cs`](../Praxis.Tests/FocusRingNavigatorTests.cs): wrap-around navigation index behavior, including negative item counts, four-item rings, and far-out-of-range index normalization.

### UI-Agnostic Visual / Layout Policies
- [`InputClearButtonVisibilityPolicyTests.cs`](../Praxis.Tests/InputClearButtonVisibilityPolicyTests.cs): clear button visibility rule, including whitespace/control-character visibility and the exact `!string.IsNullOrEmpty` contract.
- [`DockScrollBarVisibilityPolicyTests.cs`](../Praxis.Tests/DockScrollBarVisibilityPolicyTests.cs): dock scrollbar visibility rule from pointer hover state + horizontal-overflow state, plus full-combination verification that mask visibility remains the inverse of scrollbar visibility.
- [`ClearButtonGlyphAlignmentPolicyTests.cs`](../Praxis.Tests/ClearButtonGlyphAlignmentPolicyTests.cs): clear glyph translation policy, including per-platform offsets and repeat-call stability.
- [`ClearButtonRefocusPolicyTests.cs`](../Praxis.Tests/ClearButtonRefocusPolicyTests.cs): clear-button focus retry schedule by platform, including exact Windows/Mac Catalyst delays and Windows precedence when both platform flags are set.
- [`WindowsNativeFocusSafetyPolicyTests.cs`](../Praxis.Tests/WindowsNativeFocusSafetyPolicyTests.cs): guard conditions for applying native WinUI `TextBox` refocus/caret restore only to live controls, with exhaustive three-flag truth-table coverage.
- [`ButtonFocusVisualPolicyTests.cs`](../Praxis.Tests/ButtonFocusVisualPolicyTests.cs): focus-border style resolution, including positive constant width and transparent unfocused color regardless of theme.
- [`ModalEditorHeightResolverTests.cs`](../Praxis.Tests/ModalEditorHeightResolverTests.cs): multiline editor height calculation and clamping, including trailing CRLF handling and preserved blank lines.
- [`ModalEditorScrollHeightResolverTests.cs`](../Praxis.Tests/ModalEditorScrollHeightResolverTests.cs): modal scroll height clamping with non-finite input safety plus zero/negative-max fallback and finite-side preservation.
- [`ThemeTextColorPolicyTests.cs`](../Praxis.Tests/ThemeTextColorPolicyTests.cs): theme text color policy, including palette stability and distinct light/dark outputs.
- [`ThemeDarkStateResolverTests.cs`](../Praxis.Tests/ThemeDarkStateResolverTests.cs): effective dark-mode resolution, including explicit dark/light precedence over requested/platform values and undefined-enum fallback behavior.
- [`ThemeShortcutModeResolverTests.cs`](../Praxis.Tests/ThemeShortcutModeResolverTests.cs): macOS key-input to theme-mode mapping, including rejection of multi-character or trim-dependent inputs.
- [`TextCaretPositionResolverTests.cs`](../Praxis.Tests/TextCaretPositionResolverTests.cs): caret-tail placement resolution.
- [`UiTimingPolicyTests.cs`](../Praxis.Tests/UiTimingPolicyTests.cs): named UI timing constants (focus restore, activation windows, polling interval) and ordering constraints.

### Launch / Path / Storage / Reflection Utilities
- [`LaunchTargetResolverTests.cs`](../Praxis.Tests/LaunchTargetResolverTests.cs): HTTP(S)/file/path fallback target resolution, env expansion, single/double-quoted env-expansion normalization for rooted/home and relative path prefixes, preservation of valid quote-boundary POSIX path names, relative-path detection (including `.` / `..`), malformed quoted URL rejection, and bare-tilde home-path detection.
- [`WindowsPathPolicyTests.cs`](../Praxis.Tests/WindowsPathPolicyTests.cs): UNC (`\\\\server\\share`) path detection used by Windows auth-first launch flow, including quoted UNC inputs and rejection of `\\\\?\\` / `\\\\.\\` local-device prefixes.
- [`AppStoragePathLayoutResolverTests.cs`](../Praxis.Tests/AppStoragePathLayoutResolverTests.cs): platform-specific storage layout policy, including trimming quoted storage roots.
- [`AppStoragePathsTests.cs`](../Praxis.Tests/AppStoragePathsTests.cs): linked-source coverage for quoted `%LOCALAPPDATA%` normalization, single/double-quoted absolute migration roots, equivalent-path comparison, rejection of blank/relative legacy migration roots, and invalid legacy-path comparison safety.
- [`FileAppConfigServiceTests.cs`](../Praxis.Tests/FileAppConfigServiceTests.cs): linked-source coverage for config-path candidate enumeration, crash-log breadcrumbs for skipped config candidates, and fallback to later valid config files when earlier JSON is malformed, missing a `theme`, or specifies an invalid theme value.
- [`DockOrderValueCodecTests.cs`](../Praxis.Tests/DockOrderValueCodecTests.cs): dock-order CSV parsing/serialization, including duplicate/empty/invalid GUID filtering plus whole-CSV/per-entry quote trimming while preserving first occurrence order.
- [`DatabaseSchemaVersionPolicyTests.cs`](../Praxis.Tests/DatabaseSchemaVersionPolicyTests.cs): schema-version upgrade-path resolution (`PRAGMA user_version` migration sequencing, unsupported/future version rejection), including v1->v2->v3->v4 and unversioned->current multi-step upgrades.
- [`NonPublicPropertySetterTests.cs`](../Praxis.Tests/NonPublicPropertySetterTests.cs): reflection-based writable property assignment behavior.

## CI Alignment
- CI ([`.github/workflows/ci.yml`](../.github/workflows/ci.yml)) executes tests with coverage collection:
  - `dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --no-restore -v minimal --collect:"XPlat Code Coverage" --results-directory ./TestResults`
- CI uploads Cobertura XML artifact from `TestResults/**/coverage.cobertura.xml`.
- Keep this guide aligned when test files are added/removed/renamed.

---

# テストガイド（日本語）

## 対象範囲
本ドキュメントは Praxis のテスト専用ドキュメントです。
テスト実行手順、カバレッジ確認方法、テストファイルごとの意図を定義します。

## テスト構成
- テストフレームワーク: xUnit
- テストランナー: `Microsoft.NET.Test.Sdk`
- カバレッジ収集: `coverlet.collector`（`XPlat Code Coverage`）
- テストプロジェクト: [`Praxis.Tests/Praxis.Tests.csproj`](../Praxis.Tests/Praxis.Tests.csproj)
- 主ターゲット: `net10.0`

## 実行方法
全テスト実行:
```bash
dotnet test Praxis.Tests/Praxis.Tests.csproj
```

ソリューション経由で実行:
```bash
dotnet test Praxis.slnx
```

カバレッジ収集（Cobertura XML）:
```bash
dotnet test Praxis.Tests/Praxis.Tests.csproj --collect:"XPlat Code Coverage"
```

## テスト方針
- UI 非依存のルールは `Praxis.Core/Logic` に置き、`Praxis.Tests` で検証する。
- `DateTime` などは固定値を使い、再現性のあるテストにする。
- ポリシー/リゾルバは正常系・異常系の分岐を両方検証する。
- プラットフォーム依存ポリシーは、実 OS 判定ではなく入力フラグ（`isWindows`、`isFocused` など）で検証する。
- README のユーザー向け説明と、実装仕様の断言（テスト/開発ドキュメント）を混同しない。

## テスト一覧（ファイル別）

### 基本 / 横断
- [`UnitTest1.cs`](../Praxis.Tests/UnitTest1.cs)（`CoreLogicTests`）: コマンドライン生成、スナップ、検索一致、保持期間の基本確認。
- [`CoreLogicEdgeCaseTests.cs`](../Praxis.Tests/CoreLogicEdgeCaseTests.cs): パーサ/スナッパ/マッチャー/保持期間の境界ケース。範囲外 `ThemeMode` enum 値の正規化に加え、不正な fallback default も `System` へ安全化されることと、非有限値の grid snap 安全性も検証する。
- [`RepositoryEncodingPolicyTests.cs`](../Praxis.Tests/RepositoryEncodingPolicyTests.cs): BOM-free を維持すべきソースの encoding guard。現在は `cdidx validate` の品質警告対策として [`Praxis/Platforms/MacCatalyst/Program.cs`](../Praxis/Platforms/MacCatalyst/Program.cs) に UTF-8 BOM が混入しないことを保護する。
- [`StateSyncPayloadParserTests.cs`](../Praxis.Tests/StateSyncPayloadParserTests.cs): 同期シグナル payload の解析テスト。正常 payload に加えて、壊れた形式、二重区切り、空 source、非数値、範囲外 timestamp を拒否することを検証。
- [`CoreLogicPerformanceSafetyTests.cs`](../Praxis.Tests/CoreLogicPerformanceSafetyTests.cs): 既定値・境界・競合判定に加え、null-safe な検索/保持期間 helper を含む回帰安全性確認（時刻差分のみは非競合、内容差分は競合）。
- [`PolicyTruthTableTests.cs`](../Praxis.Tests/PolicyTruthTableTests.cs): フォーカス系ポリシーの真理値表を網羅検証。command entry の activation focus と Windows native focus safety gate も含む。
- [`MainPageStructureTests.cs`](../Praxis.Tests/MainPageStructureTests.cs): `MainPage` の partial 分割構造を保護するソース構成テスト。フィールド宣言を `MainPage.Fields.*.cs` に集約し、挙動コードも [`MainPage.ModalEditor.cs`](../Praxis/MainPage.ModalEditor.cs)、[`MainPage.StatusAndTheme.cs`](../Praxis/MainPage.StatusAndTheme.cs)、[`MainPage.DockAndQuickLook.cs`](../Praxis/MainPage.DockAndQuickLook.cs)、[`MainPage.WindowsInput.cs`](../Praxis/MainPage.WindowsInput.cs) などの責務別 partial に維持して [`MainPage.xaml.cs`](../Praxis/MainPage.xaml.cs) へ戻さないことを検証する。加えて、配置領域/Dock ボタン文言フォント（全プラットフォームで `12`）、編集モーダルの欄順と既定フォーカス整合（`ButtonText` が `Command` より上にあり、既定フォーカス対象でもあること）、新規ボタン作成時の `ButtonText` 初回全選択（Windows/macOS）、モーダル専用の ASCII 強制 opt-in（`ModalCommandEntry` のみ `EnforceAsciiInput="True"` を持ち、`MainCommandEntry` は持たないこと）、反転配色UI配線（`UseInvertedThemeColors` トリガー + 編集モーダルチェックボックス binding + フラット正方形チェック表示フック + テキスト入力欄準拠の枠/背景色 + 四辺同一太さの線設定 + ポリラインチェックマーク設定 + ラベルタップハンドラにより "Use opposite theme colors for this button" テキストのクリックでもチェックボックスを切り替え）の XAML/ソース仕様ガードも行う。候補行のマウス操作配線も検証: `CommandSuggestionScrollView` が XAML で命名されていること、ミドル/セカンダリクリックのジェスチャーハンドラが [`MainPage.ShortcutsAndConflict.cs`](../Praxis/MainPage.ShortcutsAndConflict.cs)（非 Mac rebuild パス）と [`MainPage.MacCatalystBehavior.cs`](../Praxis/MainPage.MacCatalystBehavior.cs)（Mac rebuild パス）の両方に存在すること、`HandleMacMiddleClick` 内で `TryGetSuggestionItemAtRootPoint` が `TryGetPlacementButtonAtRootPoint` より先に呼ばれること、スクロールオフセット考慮のヒットテストで `CommandSuggestionScrollView.ScrollY` を参照していること。
- [`AppLayerSourceGuardTests.cs`](../Praxis.Tests/AppLayerSourceGuardTests.cs): 軽量テストホストへ直接コンパイルしにくい app-layer の失敗処理を、ソースレベルで回帰保護するテスト。`MainPage` が XAML 読込失敗・初期化失敗・エラー表示失敗を crash-log の例外として残し、copy notice animation・status flash・Dock hover-exit hide・Quick Look show・Quick Look hide・button tap execution・secondary-tap create flow 失敗は crash-log warning として残すことを確認する。copy notice と Dock hover-exit hide は自分の通知トークン起因キャンセルだけ静かに吸収し、起動成功後にのみ `initialized` を立て、非表示化時に activation hook を解除し、Windows の reflection/tab-stop/モーダル初期フォーカス fallback 失敗も warning 記録すること、`MauiClipboardService` が clipboard 読み書きの両方で `CancellationToken` を尊重すること、`SqliteAppRepository` がスキーマ/bootstrap 読込成功後にのみ共有接続を公開すること、`DbErrorLogger` が file-first / DB-second の順序契約を守り drain loop の予期しない失敗も warning 記録すること、ベース `App` が fallback エラーページをキャッシュせず process-wide 例外 hook を一度だけ登録し shutdown flush 失敗を完全な crash-log 例外 + warning breadcrumb として残し、Windows startup-log directory/build/append 失敗は `SecondaryFailureLogger` 経由で `%LOCALAPPDATA%\\Praxis` 以外の fallback diagnostics sink まで含めて扱うこと、non-`Exception` `AppDomain.UnhandledException` payload は base `App` / Windows app / Mac `AppDelegate` のすべてで `CrashFileLogger.SafeObjectDescription(...)` を通すこと、static event dispatcher 群を共有の例外ログ helper に統一していること、platform startup class がグローバル例外 hook の多重登録を防ぐこと、Windows startup-log path が正規化済み共有保存先を使うことに加え、Windows `CommandEntryHandler` が互換性例外後に `InputScope` を無効化し、focused-state IME 再強制ループでも自分のキャンセルトークンだけを正常終了として扱い、互換性由来/予期しない代入失敗の両方を warning 記録すること、Mac `AppDelegate` が `MarshalManagedException` hook 失敗と key command 優先化 fallback 失敗も warning 記録すること、`CommandExecutor` が process start / launch-target-resolution の breadcrumb を維持すること、`MainViewModel` ソースガードが external-theme dispatch failure、command suggestion debounce/dispatch/lookup failure、競合 callback と clipboard/sync/persistence follow-up logging が safe warning-message helper を通ることを固定し、debounce キャンセルを自分の token に限定したうえで外部 reload bridge を安全化していること、`AppStoragePaths` が legacy migration 失敗を warning 記録してスキップすること、`FileAppConfigService` がスキップした config 候補を warning 記録しつつ unauthorized / invalid config を後続候補へフォールバックすること、`SecondaryFailureLogger` が whitespace-only original startup message も `CrashFileLogger.NormalizeMessagePayload(...)` 経由で保持すること、`MauiThemeService` が no-op apply を避け Mac dispatch 失敗も `CrashFileLogger.SafeExceptionMessage(...)` 経由で crash-log に残すこと、Mac open-relay startup に加えて middle-click/key-command/CoreGraphics fallback 失敗と deferred middle-click 実行失敗、および Mac entry/editor/command handler の `UIKeyCommand` 入力解決 reflection 失敗も crash-log warning として残すこと、`FileStateSyncNotifier` が sync ファイル書込/読込/予期しない publish 失敗を `BuildSyncWarningMessage(...)` 経由で warning 記録し dispose 後 notify を無視し teardown 前に event raising を止めることを検証する。
- [`MainViewModelWarningMessageTests.cs`](../Praxis.Tests/MainViewModelWarningMessageTests.cs): `MainViewModel.BuildSafeWarningMessage(...)` の helper-level テスト。共有 warning formatter が複数行 exception message を単一行化し、空白-only message を `(empty)` へ正規化し、warning factory 側または original exception 側の payload が hostile / 複数行 / 空でも安全な fallback 本文を返すことを固定する。
- [`MainViewModelWorkflowIntegrationTests.cs`](../Praxis.Tests/MainViewModelWorkflowIntegrationTests.cs): `create -> edit -> execute -> external sync` を通すワークフロー統合テストに加え、command 候補の選択仕様（ポップアップ表示直後は未選択、最初の `↓` で index `0` 選択、pick 時に close と実行が両立すること）も検証する。[`MainViewModel`](../Praxis/ViewModels/MainViewModel.cs) の実ソースをリンクし、リポジトリ/実行器/同期通知はテストダブルで結合検証する。初期化時の theme/dock 読込失敗フォールバック、初回/外部同期の theme と dock reload 失敗での完全な例外本体保持、external theme sync / command lookup fallback / 競合 callback / clipboard follow-up / sync notification / theme persistence の hostile / multiline / whitespace-only exception-message payload、create/delete 履歴の undo/redo と Dock 順序復元、他ウィンドウ変更が入ったときの undo キャンセル、外部同期で空 Dock 順序が来たときの Dock クリア、エディタ表示中の deferred sync、command input の複数一致一括実行成功/部分失敗ステータス、実行開始/clipboard 反映/完了の info ログ、clipboard read/write 失敗フォールバック時の完全な例外本体保持、ローカル save/delete/theme 完了後の sync notifier 失敗分離と完全な例外本体保持、launch log/purge・dock 永続化・theme 永続化失敗の分離と完全な例外本体保持、command lookup fallback 失敗の完全な例外本体保持、競合ダイアログ callback 例外時の完全な例外本体保持もここで回帰保護する。
- [`CiCoverageWorkflowPolicyTests.cs`](../Praxis.Tests/CiCoverageWorkflowPolicyTests.cs): CI のテスト手順が `XPlat Code Coverage` 収集と Cobertura アーティファクト出力を維持していることに加え、ドキュメント化済みの CI / 配布ワークフロー不変条件（Nerdbank.GitVersioning 用の `fetch-depth: 0`、`dotnet-quality: ga` SDK 固定、`--skip-manifest-update` を付けない `dotnet workload install maui`、`sudo xcodebuild -runFirstLaunch` と Xcode 互換判定、Windows / Mac Catalyst 各ターゲットフレームワークと Mac 署名無効化、`delivery.yml` における意図的な空の Windows RID）も検証するワークフロー設定ガード。

### モデル / 既定値
- [`ModelDefaultsTests.cs`](../Praxis.Tests/ModelDefaultsTests.cs): [`LauncherButtonRecord`](../Praxis.Core/Models/LauncherButtonRecord.cs) / [`LaunchLogEntry`](../Praxis.Core/Models/LaunchLogEntry.cs) / [`ErrorLogEntry`](../Praxis.Core/Models/ErrorLogEntry.cs) の既定値・初期化保証。既定 ID の一意性、copy constructor の null guard、clone の独立インスタンス性も検証する。
  - `LauncherButtonRecord` のコピーコンストラクタ / `Clone()` が全フィールド（`UseInvertedThemeColors` 含む）を複製することを回帰検証。
- [`LauncherButtonOrderPolicyTests.cs`](../Praxis.Tests/LauncherButtonOrderPolicyTests.cs): 配置順（`Y`、次に `X`）への正規化、同位置時の安定順序維持、null 要素の安全なスキップに加え、empty/null 入力と source collection から独立した新規 list 返却も検証する。

### ログ / クラッシュ安全性
- [`CrashFileLoggerTests.cs`](../Praxis.Tests/CrashFileLoggerTests.cs): 同期クラッシュファイルロガーの挙動 — Windows/macOS のパス解決、quote 付き/無効なプラットフォーム保存先からのフォールバック、全書き込みメソッド（`WriteException`、`WriteInfo`、`WriteWarning`）の例外非送出保証、null の Info/Warning payload を `(no message payload)` へ正規化する挙動、複数行 exception message の単一行化、空白-only exception/object payload の `(empty)` 正規化を helper と実際の exception body 出力の両方で確認すること、InnerException チェーン記録、`AggregateException` 子例外記録、wide aggregate に対する有界サマリ fallback と middle/tail sampling マーカー、`Exception.Data` 記録、`Message` / `StackTrace` getter や object `ToString()`、`Exception.Data` key/value `ToString()` が投げる custom payload に対する fallback marker、ファイル書き込み検証、並行書き込み時のスレッドセーフティ、および blocking task analyzer 警告を出さない非同期検証。
- [`DbErrorLoggerTests.cs`](../Praxis.Tests/DbErrorLoggerTests.cs): DB エラーロガーの挙動 — `Log`/`LogInfo`/`LogWarning` の例外非送出保証、null 例外 payload のプレースホルダ永続化、null の Info/Warning payload のプレースホルダ永続化、例外 message の単一行正規化、空白-only exception message の `(empty)` 永続化、`Message` / `StackTrace` getter が投げる custom exception に対する fallback marker 永続化、`FlushAsync` による保留キューと in-flight 書き込みの両方の待機検証、Error/Info/Warning 各 entry の context/level 永続化、完全な例外型チェーン記録（`AggregateException` 含む）、aggregate index ラベル付きの有界スタック出力、タイムアウト挙動、Error のみ保持期間 purge を発火すること、Error/Warning/Info 各永続化失敗と purge 失敗・timeout の crash-log breadcrumb と例外本体記録に加え、repository 側の複数行/空白-only exception message も warning 本文で正規化されることを確認する。
- [`SecondaryFailureLoggerTests.cs`](../Praxis.Tests/SecondaryFailureLoggerTests.cs): 独立 fallback diagnostics sink の挙動 — 通常の `%LOCALAPPDATA%\\Praxis` crash/startup root が壊れていても、startup-log 診断を temp/current directory 配下へ退避し、hostile exception payload の fallback marker に加えて fallback warning 本文内の複数行/空白-only failure message と、original startup message 側の複数行/空白-only payload も正規化したうえで secondary write failure context を残せることを検証する。

### コマンド実行 / 一致 / 候補
- [`CommandLineBuilderTests.cs`](../Praxis.Tests/CommandLineBuilderTests.cs): コマンドライン構築の null/空白処理、quoted-empty tool 正規化、組み立て結果。
- [`CommandRecordMatcherTests.cs`](../Praxis.Tests/CommandRecordMatcherTests.cs): command 完全一致規則、null コレクション要素も含む null ガード、trim/大小文字非依存。
- [`CommandSuggestionVisibilityPolicyTests.cs`](../Praxis.Tests/CommandSuggestionVisibilityPolicyTests.cs): コンテキストメニュー表示時の候補クローズ判定。候補表示状態との 1:1 対応を固定する。
- [`CommandSuggestionRowColorPolicyTests.cs`](../Praxis.Tests/CommandSuggestionRowColorPolicyTests.cs): テーマ別の候補行背景色判定。未選択の透明維持と、light/dark で異なる選択色も固定する。
- [`CommandNotFoundRefocusPolicyTests.cs`](../Praxis.Tests/CommandNotFoundRefocusPolicyTests.cs): `Command not found:` 時の再フォーカス判定。先頭空白を含む入力の受理、コロン欠落拒否、trim 後も文頭でないケースの拒否を検証する。
- [`StatusFlashErrorPolicyTests.cs`](../Praxis.Tests/StatusFlashErrorPolicyTests.cs): ステータスのエラーフラッシュ分類判定。埋め込み `error` / `exception` / `not found` と中立メッセージ拒否も含む。
- [`QuickLookPreviewFormatterTests.cs`](../Praxis.Tests/QuickLookPreviewFormatterTests.cs): Quick Look 表示文字列の正規化・最大長を超えない省略・ちょうど上限長の素通し・ラベル付き行全体の長さ上限制御・不正長/空ラベルの引数ガード。

### Undo / Redo
- [`ActionHistoryTests.cs`](../Praxis.Tests/ActionHistoryTests.cs): コマンドパターン履歴スタックの挙動（constructor ガード、Undo/Redo 遷移、両スタックの失敗時ロールバック、`Clear()`、容量トリム）。
- [`ButtonHistoryConsistencyPolicyTests.cs`](../Praxis.Tests/ButtonHistoryConsistencyPolicyTests.cs): Undo/Redo 適用時に使う楽観的ロック版一致判定（`UpdatedAtUtc`）。`UseInvertedThemeColors` 差分を内容差分として正しく不一致判定することも検証。

### 入力 / キーボード / フォーカス
- [`CommandEntryBehaviorPolicyTests.cs`](../Praxis.Tests/CommandEntryBehaviorPolicyTests.cs): command 入力欄の候補ショートカット有効化/アクティブ化時ネイティブ再フォーカス有効化ポリシー。enable/editor/conflict の全組み合わせを検証する。
- [`WindowActivationCommandFocusPolicyTests.cs`](../Praxis.Tests/WindowActivationCommandFocusPolicyTests.cs): ウィンドウ再アクティブ時の command フォーカス可否。
- [`SearchFocusGuardPolicyTests.cs`](../Praxis.Tests/SearchFocusGuardPolicyTests.cs): macOS の Search フォーカスガード判定。
- [`AsciiInputFilterTests.cs`](../Praxis.Tests/AsciiInputFilterTests.cs): macOS モーダル command 入力経路で使う ASCII フィルタ判定。
- [`MacCommandInputSourcePolicyTests.cs`](../Praxis.Tests/MacCommandInputSourcePolicyTests.cs): macOS ASCII 入力ソース強制の適用条件（first responder / キーウィンドウ / アプリ active / 欄ごとの opt-in）と、フォーカス中の再強制間隔の安全性を検証。
- [`WindowsCommandInputImePolicyTests.cs`](../Praxis.Tests/WindowsCommandInputImePolicyTests.cs): Windows IME の ASCII モード適用判定（欄ごとの opt-in を含む `ShouldForceAsciiImeMode`）、フォーカス時 ASCII 補正の再試行スケジュール、フォーカス中英字再強制の適用条件/間隔、変換モード正規化、キャレット補正。
- [`WindowsInputScopeCompatibilityPolicyTests.cs`](../Praxis.Tests/WindowsInputScopeCompatibilityPolicyTests.cs): ネイティブ `InputScope` 設定失敗時（`ArgumentException`）のフォールバック判定。派生 `ArgumentException` と、inner に持つだけの非対象例外も区別して検証する。
- [`WindowsModalFocusRestorePolicyTests.cs`](../Praxis.Tests/WindowsModalFocusRestorePolicyTests.cs): Windows 編集モーダル/競合ダイアログのフォーカス復帰条件。
- [`ConflictDialogFocusRestorePolicyTests.cs`](../Praxis.Tests/ConflictDialogFocusRestorePolicyTests.cs): 競合ダイアログ閉鎖後の編集フォーカス復帰条件。
- [`EditorShortcutActionResolverTests.cs`](../Praxis.Tests/EditorShortcutActionResolverTests.cs): モーダル/コンテキスト/競合のキー操作マッピング。
- [`EditorShortcutScopeResolverTests.cs`](../Praxis.Tests/EditorShortcutScopeResolverTests.cs): オーバーレイ表示状態のショートカット有効範囲判定。
- [`EditorTabInsertionResolverTests.cs`](../Praxis.Tests/EditorTabInsertionResolverTests.cs): タブ文字フォールバック検知と遷移方向判定。先頭挿入・逆タブ・null 入力 no-op・overload 間整合も検証する。
- [`FocusRingNavigatorTests.cs`](../Praxis.Tests/FocusRingNavigatorTests.cs): ラップ付きフォーカスインデックス遷移。負の item 数、4 要素リング、極端な範囲外 index 正規化も検証する。

### UI 非依存の見た目 / レイアウトポリシー
- [`InputClearButtonVisibilityPolicyTests.cs`](../Praxis.Tests/InputClearButtonVisibilityPolicyTests.cs): クリアボタン表示条件。空白/制御文字の表示と `!string.IsNullOrEmpty` 契約も固定する。
- [`DockScrollBarVisibilityPolicyTests.cs`](../Praxis.Tests/DockScrollBarVisibilityPolicyTests.cs): ポインターホバー状態 + 横オーバーフロー状態に基づく Dock スクロールバー表示判定と、全組み合わせでのマスク表示反転ルール判定。
- [`ClearButtonGlyphAlignmentPolicyTests.cs`](../Praxis.Tests/ClearButtonGlyphAlignmentPolicyTests.cs): クリアボタン `x` の座標補正。プラットフォーム別オフセットと再呼び出し安定性も検証する。
- [`ClearButtonRefocusPolicyTests.cs`](../Praxis.Tests/ClearButtonRefocusPolicyTests.cs): クリア後フォーカス復帰リトライ間隔。Windows/Mac Catalyst の正確な遅延値と、両フラグ指定時の Windows 優先も検証する。
- [`WindowsNativeFocusSafetyPolicyTests.cs`](../Praxis.Tests/WindowsNativeFocusSafetyPolicyTests.cs): WinUI の native `TextBox` 再フォーカス/キャレット復帰を live control に限定する安全条件を検証する。3 フラグの全組み合わせも固定する。
- [`ButtonFocusVisualPolicyTests.cs`](../Praxis.Tests/ButtonFocusVisualPolicyTests.cs): フォーカス枠スタイル判定。正の固定線幅と、非フォーカス時にテーマ非依存で透明色になることも検証する。
- [`ModalEditorHeightResolverTests.cs`](../Praxis.Tests/ModalEditorHeightResolverTests.cs): 複数行エディタ高さ算出とクランプ。末尾 CRLF と空行保持も検証する。
- [`ModalEditorScrollHeightResolverTests.cs`](../Praxis.Tests/ModalEditorScrollHeightResolverTests.cs): モーダル項目スクロール高さクランプと非有限入力の安全化に加え、`maxHeight <= 0` フォールバックと片側のみ非有限な入力の扱いも検証する。
- [`ThemeTextColorPolicyTests.cs`](../Praxis.Tests/ThemeTextColorPolicyTests.cs): テーマ連動文字色判定。パレット固定と light/dark の差異も検証する。
- [`ThemeDarkStateResolverTests.cs`](../Praxis.Tests/ThemeDarkStateResolverTests.cs): 実効ダーク判定。Dark/Light の優先、requested/platform のフォールバック、未定義 enum の扱いも検証する。
- [`ThemeShortcutModeResolverTests.cs`](../Praxis.Tests/ThemeShortcutModeResolverTests.cs): macOS キー入力からテーマモード解決。複数文字や trim 前提の入力を拒否することも検証する。
- [`TextCaretPositionResolverTests.cs`](../Praxis.Tests/TextCaretPositionResolverTests.cs): キャレット末尾配置判定。
- [`UiTimingPolicyTests.cs`](../Praxis.Tests/UiTimingPolicyTests.cs): フォーカス復帰・アクティベーション・ポーリングの UI タイミング定数と順序条件。

### 起動 / パス / ストレージ / リフレクション補助
- [`LaunchTargetResolverTests.cs`](../Praxis.Tests/LaunchTargetResolverTests.cs): HTTP(S)/ファイル/パスのフォールバック起動先解決、環境変数展開、single/double quote を含む展開後値の正規化、相対パス判定（`.` / `..` 含む）、bare `~` のホームパス判定。
- [`CommandExecutorTests.cs`](../Praxis.Tests/CommandExecutorTests.cs): linked-source で取り込んだ `CommandExecutor` のホームパス展開 helper テスト。`~`、`~/...`、`~\\...` を展開し、通常の相対パスはそのまま残すこと、quoted tool path と env 展開後 quoted tool path の正規化、正規化後に空になる tool 値の検出、環境変数展開後も含む Windows シェル起動時の作業ディレクトリ上書き、native process 起動例外時の crash-log breadcrumb、さらに launch-failure message 構築時に複数行 exception message を単一行化し、空白-only message を `(empty)` へ正規化し、hostile exception `Message` getter でも fallback marker を返すことに加え、logged tool / path / URL / arguments 断片も warning prefix に入る前に正規化されることを確認。
- [`CommandWorkingDirectoryPolicyTests.cs`](../Praxis.Tests/CommandWorkingDirectoryPolicyTests.cs): `cmd`、`powershell`、`pwsh`、`wt` をユーザープロファイル起点で開く対象として判定する純粋ポリシーテスト。環境変数展開後の quoted path も含む。
- [`WindowsPathPolicyTests.cs`](../Praxis.Tests/WindowsPathPolicyTests.cs): Windows の認証先行起動フローで使う UNC（`\\\\server\\share`）判定。quote 付き UNC と `\\\\?\\` / `\\\\.\\` ローカルデバイス接頭辞の除外も検証する。
- [`AppStoragePathLayoutResolverTests.cs`](../Praxis.Tests/AppStoragePathLayoutResolverTests.cs): プラットフォーム別ストレージ配置ルール。quote 付き保存先 root の正規化も検証する。
- [`AppStoragePathsTests.cs`](../Praxis.Tests/AppStoragePathsTests.cs): linked-source の `AppStoragePaths` テスト。quote 付き `%LOCALAPPDATA%` 正規化、single/double quote 付き absolute migration root、等価 path 比較、空/相対 legacy migration root の除外、壊れた legacy path 比較入力を安全に無視すること、さらに legacy migration warning 構築時に複数行 exception message を単一行化し、空白-only message を `(empty)` へ正規化し、hostile exception `Message` getter でも fallback marker を返すこと、加えて migration source / comparison path 断片も `CrashFileLogger.NormalizeMessagePayload(...)` 経由で warning prefix へ入る前に正規化されることを確認。
- [`FileAppConfigServiceTests.cs`](../Praxis.Tests/FileAppConfigServiceTests.cs): linked-source の `FileAppConfigService` テスト。config 候補列挙、スキップした候補の crash-log breadcrumb、先頭候補の JSON が壊れている場合、`theme` が欠落している場合、不正な theme 値を持つ場合に後続の正常設定へフォールバックすること、さらに skipped-config warning が複数行 exception message を単一行化し、空白-only message を `(empty)` へ正規化しつつ、exception `Message` getter 自体が壊れていても fallback marker を残して再throwしないこと、加えて logged config path 断片も `CrashFileLogger.NormalizeMessagePayload(...)` 経由で正規化されてから breadcrumb へ入ることを確認。
- [`FileStateSyncNotifierTests.cs`](../Praxis.Tests/FileStateSyncNotifierTests.cs): linked-source の `FileStateSyncNotifier` テスト。sync payload write/read/unexpected publish warning が共有 safe exception-message helper を通り、複数行 exception message を単一行化し、空白-only message を `(empty)` へ正規化しつつ、exception `Message` getter 自体が壊れていても fallback marker を返すこと、さらに malformed / observed sync payload 断片と sync-file path 断片も `CrashFileLogger.NormalizeMessagePayload(...)` 経由で正規化されてから warning/info log 行へ入ることを確認。
- [`DockOrderValueCodecTests.cs`](../Praxis.Tests/DockOrderValueCodecTests.cs): Dock 順序 CSV の解析/直列化テスト。重複/空/不正 GUID の除外に加え、CSV 全体や各 GUID を囲む quote を外したうえで最初の有効順序を維持することを確認。
- [`DatabaseSchemaVersionPolicyTests.cs`](../Praxis.Tests/DatabaseSchemaVersionPolicyTests.cs): スキーマバージョンのアップグレード経路解決（`PRAGMA user_version` の段階適用順序、未対応/未来バージョン拒否）を検証。`v1 -> v2 -> v3 -> v4` と `未バージョン -> 現行` の段階適用も確認する。
- [`NonPublicPropertySetterTests.cs`](../Praxis.Tests/NonPublicPropertySetterTests.cs): リフレクションによる書き込み可能プロパティ設定。

## CI との整合
- CI（[`.github/workflows/ci.yml`](../.github/workflows/ci.yml)）は、次のコマンドでテスト＋カバレッジ収集を実行します。
  - `dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --no-restore -v minimal --collect:"XPlat Code Coverage" --results-directory ./TestResults`
- CI は `TestResults/**/coverage.cobertura.xml` を Cobertura アーティファクトとして保存します。
- テストファイルの追加・削除・改名時は本ガイドも更新してください。
