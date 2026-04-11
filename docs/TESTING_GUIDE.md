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
- [`CoreLogicEdgeCaseTests.cs`](../Praxis.Tests/CoreLogicEdgeCaseTests.cs): parser/snapper/matcher/retention edge cases.
- [`StateSyncPayloadParserTests.cs`](../Praxis.Tests/StateSyncPayloadParserTests.cs): sync-signal payload parsing coverage for valid payloads plus malformed, blank-source, non-numeric, and out-of-range timestamp rejection.
- [`CoreLogicPerformanceSafetyTests.cs`](../Praxis.Tests/CoreLogicPerformanceSafetyTests.cs): regression-safety checks for defaults, bounds, and conflict detection (including timestamp-only drift vs material-content conflict cases).
- [`PolicyTruthTableTests.cs`](../Praxis.Tests/PolicyTruthTableTests.cs): full truth-table validation for focus-related policy combinations.
- [`MainPageStructureTests.cs`](../Praxis.Tests/MainPageStructureTests.cs): source-structure guard for the `MainPage` partial split (field declarations stay in `MainPage.Fields.*.cs`, while behavior remains separated into concern-based files such as [`MainPage.ModalEditor.cs`](../Praxis/MainPage.ModalEditor.cs), [`MainPage.StatusAndTheme.cs`](../Praxis/MainPage.StatusAndTheme.cs), [`MainPage.DockAndQuickLook.cs`](../Praxis/MainPage.DockAndQuickLook.cs), and [`MainPage.WindowsInput.cs`](../Praxis/MainPage.WindowsInput.cs) instead of drifting back into [`MainPage.xaml.cs`](../Praxis/MainPage.xaml.cs)), plus XAML/source guards for placement/dock button-label font sizing (`12` on all platforms), editor-modal field order/default focus alignment (`ButtonText` appears before `Command` and is the default modal focus target), new-button initial `ButtonText` select-all on Windows/macOS, modal-only ASCII-enforcement opt-in (`ModalCommandEntry` sets `EnforceAsciiInput="True"` while `MainCommandEntry` does not), and inverted-theme UI wiring (`UseInvertedThemeColors` triggers + editor checkbox binding + flat-square checkbox rendering hooks + text-field-matched border/background colors + equal-thickness edge line settings + polyline checkmark settings + label tap handler so clicking the "Use opposite theme colors for this button" text also toggles the checkbox). Also guards suggestion-row mouse interaction wiring: `CommandSuggestionScrollView` is named in XAML, middle/secondary-click gesture handlers are present in both [`MainPage.ShortcutsAndConflict.cs`](../Praxis/MainPage.ShortcutsAndConflict.cs) (non-Mac rebuild path) and [`MainPage.MacCatalystBehavior.cs`](../Praxis/MainPage.MacCatalystBehavior.cs) (Mac rebuild path), `TryGetSuggestionItemAtRootPoint` appears before `TryGetPlacementButtonAtRootPoint` in `HandleMacMiddleClick`, and `CommandSuggestionScrollView.ScrollY` is used for scroll-offset-aware hit testing.
- [`MainViewModelWorkflowIntegrationTests.cs`](../Praxis.Tests/MainViewModelWorkflowIntegrationTests.cs): workflow integration tests for `create -> edit -> execute -> external sync` plus command-suggestion selection behavior (`popup opens with no preselected row`, then first `Down` selects index `0`) using linked [`MainViewModel`](../Praxis/ViewModels/MainViewModel.cs) sources and test doubles for repository/executor/sync services.
- [`CiCoverageWorkflowPolicyTests.cs`](../Praxis.Tests/CiCoverageWorkflowPolicyTests.cs): workflow-configuration guard that verifies CI test step collects `XPlat Code Coverage` and uploads Cobertura artifact output.

### Models / Defaults
- [`ModelDefaultsTests.cs`](../Praxis.Tests/ModelDefaultsTests.cs): default values and initialization guarantees for `LauncherButtonRecord`, `LaunchLogEntry`, and `ErrorLogEntry`.
  - Includes copy-constructor / `Clone()` full-field copy regression checks for `LauncherButtonRecord` (including `UseInvertedThemeColors`).

### Logging / Crash Safety
- [`CrashFileLoggerTests.cs`](../Praxis.Tests/CrashFileLoggerTests.cs): synchronous crash file logger behavior — path resolution, no-throw guarantees for all write methods (`WriteException`, `WriteInfo`, `WriteWarning`), inner exception chain capture, `AggregateException` child capture, `Exception.Data` capture, file write verification, and thread safety under concurrent writes without blocking-task analyzer warnings.
- [`DbErrorLoggerTests.cs`](../Praxis.Tests/DbErrorLoggerTests.cs): database error logger behavior — no-throw guarantees for `Log`/`LogInfo`/`LogWarning`, `FlushAsync` verification for queued and already-in-flight writes, full exception type chain capture (including `AggregateException`), message chain concatenation, stack trace capture, timeout behavior, Error-only retention purge behavior, and graceful handling of repository write failures.

### Command Execution / Matching / Suggestions
- [`CommandLineBuilderTests.cs`](../Praxis.Tests/CommandLineBuilderTests.cs): null/whitespace handling and normalization for command-line construction.
- [`CommandExecutorTests.cs`](../Praxis.Tests/CommandExecutorTests.cs): linked-source coverage for home-path expansion in empty-tool fallback launches (`~`, `~/...`, `~\\...`), non-home relative path pass-through, and Windows shell launch working-directory override to the user profile.
- [`CommandRecordMatcherTests.cs`](../Praxis.Tests/CommandRecordMatcherTests.cs): exact command matching rules, null guards, and case/trim behavior.
- [`CommandSuggestionVisibilityPolicyTests.cs`](../Praxis.Tests/CommandSuggestionVisibilityPolicyTests.cs): close policy when context menu opens.
- [`CommandSuggestionRowColorPolicyTests.cs`](../Praxis.Tests/CommandSuggestionRowColorPolicyTests.cs): selected/unselected row color decisions per theme.
- [`CommandWorkingDirectoryPolicyTests.cs`](../Praxis.Tests/CommandWorkingDirectoryPolicyTests.cs): pure policy coverage for which Windows shell-like tools (`cmd`, `powershell`, `pwsh`, `wt`) should start from the user-profile working directory.
- [`CommandNotFoundRefocusPolicyTests.cs`](../Praxis.Tests/CommandNotFoundRefocusPolicyTests.cs): refocus decision for `Command not found:` status.
- [`StatusFlashErrorPolicyTests.cs`](../Praxis.Tests/StatusFlashErrorPolicyTests.cs): status classification for error flash behavior.
- [`QuickLookPreviewFormatterTests.cs`](../Praxis.Tests/QuickLookPreviewFormatterTests.cs): quick-look preview text normalization, truncation, and labeled-line formatting.

### Undo / Redo
- [`ActionHistoryTests.cs`](../Praxis.Tests/ActionHistoryTests.cs): command-pattern history stack behavior (undo/redo transitions, failed-apply recovery, capacity trimming).
- [`ButtonHistoryConsistencyPolicyTests.cs`](../Praxis.Tests/ButtonHistoryConsistencyPolicyTests.cs): optimistic-lock version-match checks used when applying undo/redo mutations (including mismatch detection when `UseInvertedThemeColors` differs).

### Input / Keyboard / Focus Policies
- [`CommandEntryBehaviorPolicyTests.cs`](../Praxis.Tests/CommandEntryBehaviorPolicyTests.cs): command entry role flags for navigation shortcuts and activation-time native refocus.
- [`WindowActivationCommandFocusPolicyTests.cs`](../Praxis.Tests/WindowActivationCommandFocusPolicyTests.cs): activation-time command focus gating.
- [`SearchFocusGuardPolicyTests.cs`](../Praxis.Tests/SearchFocusGuardPolicyTests.cs): macOS search-focus guard decision rules.
- [`AsciiInputFilterTests.cs`](../Praxis.Tests/AsciiInputFilterTests.cs): ASCII filtering rules used by macOS modal command input paths.
- [`MacCommandInputSourcePolicyTests.cs`](../Praxis.Tests/MacCommandInputSourcePolicyTests.cs): macOS ASCII input-source enforcement gating (first-responder + key-window + app-active + per-entry opt-in) and focused re-apply interval safety.
- [`WindowsCommandInputImePolicyTests.cs`](../Praxis.Tests/WindowsCommandInputImePolicyTests.cs): Windows IME ASCII-mode gating (`ShouldForceAsciiImeMode` with per-entry opt-in), focus-time ASCII nudge retry schedule, focused-state ASCII reassert gating/interval, conversion-mode normalization, and caret clamp logic.
- [`WindowsInputScopeCompatibilityPolicyTests.cs`](../Praxis.Tests/WindowsInputScopeCompatibilityPolicyTests.cs): fallback trigger rules when native `InputScope` assignment fails (`ArgumentException`).
- [`WindowsModalFocusRestorePolicyTests.cs`](../Praxis.Tests/WindowsModalFocusRestorePolicyTests.cs): Windows editor/conflict focus restore conditions.
- [`ConflictDialogFocusRestorePolicyTests.cs`](../Praxis.Tests/ConflictDialogFocusRestorePolicyTests.cs): editor focus restore condition after conflict dialog close.
- [`EditorShortcutActionResolverTests.cs`](../Praxis.Tests/EditorShortcutActionResolverTests.cs): key-to-action mapping for modal/context/conflict shortcuts.
- [`EditorShortcutScopeResolverTests.cs`](../Praxis.Tests/EditorShortcutScopeResolverTests.cs): active scope decision when overlays are open/closed.
- [`EditorTabInsertionResolverTests.cs`](../Praxis.Tests/EditorTabInsertionResolverTests.cs): tab-character fallback detection and navigation mapping.
- [`FocusRingNavigatorTests.cs`](../Praxis.Tests/FocusRingNavigatorTests.cs): wrap-around navigation index behavior.

### UI-Agnostic Visual / Layout Policies
- [`InputClearButtonVisibilityPolicyTests.cs`](../Praxis.Tests/InputClearButtonVisibilityPolicyTests.cs): clear button visibility rule.
- [`DockScrollBarVisibilityPolicyTests.cs`](../Praxis.Tests/DockScrollBarVisibilityPolicyTests.cs): dock scrollbar visibility rule from pointer hover state + horizontal-overflow state, and mask-visibility inversion rule.
- [`ClearButtonGlyphAlignmentPolicyTests.cs`](../Praxis.Tests/ClearButtonGlyphAlignmentPolicyTests.cs): clear glyph translation policy.
- [`ClearButtonRefocusPolicyTests.cs`](../Praxis.Tests/ClearButtonRefocusPolicyTests.cs): clear-button focus retry schedule by platform, including deferred retries on Mac Catalyst.
- [`WindowsNativeFocusSafetyPolicyTests.cs`](../Praxis.Tests/WindowsNativeFocusSafetyPolicyTests.cs): guard conditions for applying native WinUI `TextBox` refocus/caret restore only to live controls.
- [`ButtonFocusVisualPolicyTests.cs`](../Praxis.Tests/ButtonFocusVisualPolicyTests.cs): focus-border style resolution.
- [`ModalEditorHeightResolverTests.cs`](../Praxis.Tests/ModalEditorHeightResolverTests.cs): multiline editor height calculation and clamping.
- [`ModalEditorScrollHeightResolverTests.cs`](../Praxis.Tests/ModalEditorScrollHeightResolverTests.cs): modal scroll height clamping.
- [`ThemeTextColorPolicyTests.cs`](../Praxis.Tests/ThemeTextColorPolicyTests.cs): theme text color policy.
- [`ThemeDarkStateResolverTests.cs`](../Praxis.Tests/ThemeDarkStateResolverTests.cs): effective dark-mode resolution.
- [`ThemeShortcutModeResolverTests.cs`](../Praxis.Tests/ThemeShortcutModeResolverTests.cs): macOS key-input to theme-mode mapping.
- [`TextCaretPositionResolverTests.cs`](../Praxis.Tests/TextCaretPositionResolverTests.cs): caret-tail placement resolution.
- [`UiTimingPolicyTests.cs`](../Praxis.Tests/UiTimingPolicyTests.cs): named UI timing constants (focus restore, activation windows, polling interval) and ordering constraints.

### Launch / Path / Storage / Reflection Utilities
- [`LaunchTargetResolverTests.cs`](../Praxis.Tests/LaunchTargetResolverTests.cs): HTTP(S)/file/path fallback target resolution, env expansion, relative-path detection, and bare-tilde home-path detection.
- [`WindowsPathPolicyTests.cs`](../Praxis.Tests/WindowsPathPolicyTests.cs): UNC (`\\\\server\\share`) path detection used by Windows auth-first launch flow.
- [`AppStoragePathLayoutResolverTests.cs`](../Praxis.Tests/AppStoragePathLayoutResolverTests.cs): platform-specific storage layout policy.
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
- [`CoreLogicEdgeCaseTests.cs`](../Praxis.Tests/CoreLogicEdgeCaseTests.cs): パーサ/スナッパ/マッチャー/保持期間の境界ケース。
- [`StateSyncPayloadParserTests.cs`](../Praxis.Tests/StateSyncPayloadParserTests.cs): 同期シグナル payload の解析テスト。正常 payload に加えて、壊れた形式、空 source、非数値、範囲外 timestamp を拒否することを検証。
- [`CoreLogicPerformanceSafetyTests.cs`](../Praxis.Tests/CoreLogicPerformanceSafetyTests.cs): 既定値・境界・競合判定（時刻差分のみは非競合、内容差分は競合）の回帰安全性確認。
- [`PolicyTruthTableTests.cs`](../Praxis.Tests/PolicyTruthTableTests.cs): フォーカス系ポリシーの真理値表を網羅検証。
- [`MainPageStructureTests.cs`](../Praxis.Tests/MainPageStructureTests.cs): `MainPage` の partial 分割構造を保護するソース構成テスト。フィールド宣言を `MainPage.Fields.*.cs` に集約し、挙動コードも [`MainPage.ModalEditor.cs`](../Praxis/MainPage.ModalEditor.cs)、[`MainPage.StatusAndTheme.cs`](../Praxis/MainPage.StatusAndTheme.cs)、[`MainPage.DockAndQuickLook.cs`](../Praxis/MainPage.DockAndQuickLook.cs)、[`MainPage.WindowsInput.cs`](../Praxis/MainPage.WindowsInput.cs) などの責務別 partial に維持して [`MainPage.xaml.cs`](../Praxis/MainPage.xaml.cs) へ戻さないことを検証する。加えて、配置領域/Dock ボタン文言フォント（全プラットフォームで `12`）、編集モーダルの欄順と既定フォーカス整合（`ButtonText` が `Command` より上にあり、既定フォーカス対象でもあること）、新規ボタン作成時の `ButtonText` 初回全選択（Windows/macOS）、モーダル専用の ASCII 強制 opt-in（`ModalCommandEntry` のみ `EnforceAsciiInput="True"` を持ち、`MainCommandEntry` は持たないこと）、反転配色UI配線（`UseInvertedThemeColors` トリガー + 編集モーダルチェックボックス binding + フラット正方形チェック表示フック + テキスト入力欄準拠の枠/背景色 + 四辺同一太さの線設定 + ポリラインチェックマーク設定 + ラベルタップハンドラにより "Use opposite theme colors for this button" テキストのクリックでもチェックボックスを切り替え）の XAML/ソース仕様ガードも行う。候補行のマウス操作配線も検証: `CommandSuggestionScrollView` が XAML で命名されていること、ミドル/セカンダリクリックのジェスチャーハンドラが [`MainPage.ShortcutsAndConflict.cs`](../Praxis/MainPage.ShortcutsAndConflict.cs)（非 Mac rebuild パス）と [`MainPage.MacCatalystBehavior.cs`](../Praxis/MainPage.MacCatalystBehavior.cs)（Mac rebuild パス）の両方に存在すること、`HandleMacMiddleClick` 内で `TryGetSuggestionItemAtRootPoint` が `TryGetPlacementButtonAtRootPoint` より先に呼ばれること、スクロールオフセット考慮のヒットテストで `CommandSuggestionScrollView.ScrollY` を参照していること。
- [`MainViewModelWorkflowIntegrationTests.cs`](../Praxis.Tests/MainViewModelWorkflowIntegrationTests.cs): `create -> edit -> execute -> external sync` を通すワークフロー統合テストに加え、command 候補の選択仕様（ポップアップ表示直後は未選択、最初の `↓` で index `0` 選択）も検証する。[`MainViewModel`](../Praxis/ViewModels/MainViewModel.cs) の実ソースをリンクし、リポジトリ/実行器/同期通知はテストダブルで結合検証する。
- [`CiCoverageWorkflowPolicyTests.cs`](../Praxis.Tests/CiCoverageWorkflowPolicyTests.cs): CI のテスト手順が `XPlat Code Coverage` 収集と Cobertura アーティファクト出力を維持しているかを検証するワークフロー設定ガード。

### モデル / 既定値
- [`ModelDefaultsTests.cs`](../Praxis.Tests/ModelDefaultsTests.cs): [`LauncherButtonRecord`](../Praxis.Core/Models/LauncherButtonRecord.cs) / [`LaunchLogEntry`](../Praxis.Core/Models/LaunchLogEntry.cs) / [`ErrorLogEntry`](../Praxis.Core/Models/ErrorLogEntry.cs) の既定値・初期化保証。
  - `LauncherButtonRecord` のコピーコンストラクタ / `Clone()` が全フィールド（`UseInvertedThemeColors` 含む）を複製することを回帰検証。

### ログ / クラッシュ安全性
- [`CrashFileLoggerTests.cs`](../Praxis.Tests/CrashFileLoggerTests.cs): 同期クラッシュファイルロガーの挙動 — パス解決、全書き込みメソッド（`WriteException`、`WriteInfo`、`WriteWarning`）の例外非送出保証、InnerException チェーン記録、`AggregateException` 子例外記録、`Exception.Data` 記録、ファイル書き込み検証、並行書き込み時のスレッドセーフティ、および blocking task analyzer 警告を出さない非同期検証。
- [`DbErrorLoggerTests.cs`](../Praxis.Tests/DbErrorLoggerTests.cs): DB エラーロガーの挙動 — `Log`/`LogInfo`/`LogWarning` の例外非送出保証、`FlushAsync` による保留キューと in-flight 書き込みの両方の待機検証、完全な例外型チェーン記録（`AggregateException` 含む）、メッセージチェーン連結、スタックトレース記録、タイムアウト挙動、Error のみ保持期間 purge を発火すること、リポジトリ書き込み失敗時のグレースフルハンドリング。

### コマンド実行 / 一致 / 候補
- [`CommandLineBuilderTests.cs`](../Praxis.Tests/CommandLineBuilderTests.cs): コマンドライン構築の null/空白処理と正規化。
- [`CommandRecordMatcherTests.cs`](../Praxis.Tests/CommandRecordMatcherTests.cs): command 完全一致規則、null ガード、trim/大小文字非依存。
- [`CommandSuggestionVisibilityPolicyTests.cs`](../Praxis.Tests/CommandSuggestionVisibilityPolicyTests.cs): コンテキストメニュー表示時の候補クローズ判定。
- [`CommandSuggestionRowColorPolicyTests.cs`](../Praxis.Tests/CommandSuggestionRowColorPolicyTests.cs): テーマ別の候補行背景色判定。
- [`CommandNotFoundRefocusPolicyTests.cs`](../Praxis.Tests/CommandNotFoundRefocusPolicyTests.cs): `Command not found:` 時の再フォーカス判定。
- [`StatusFlashErrorPolicyTests.cs`](../Praxis.Tests/StatusFlashErrorPolicyTests.cs): ステータスのエラーフラッシュ分類判定。
- [`QuickLookPreviewFormatterTests.cs`](../Praxis.Tests/QuickLookPreviewFormatterTests.cs): Quick Look 表示文字列の正規化・省略・ラベル整形。

### Undo / Redo
- [`ActionHistoryTests.cs`](../Praxis.Tests/ActionHistoryTests.cs): コマンドパターン履歴スタックの挙動（Undo/Redo 遷移、失敗時ロールバック、容量トリム）。
- [`ButtonHistoryConsistencyPolicyTests.cs`](../Praxis.Tests/ButtonHistoryConsistencyPolicyTests.cs): Undo/Redo 適用時に使う楽観的ロック版一致判定（`UpdatedAtUtc`）。`UseInvertedThemeColors` 差分を内容差分として正しく不一致判定することも検証。

### 入力 / キーボード / フォーカス
- [`CommandEntryBehaviorPolicyTests.cs`](../Praxis.Tests/CommandEntryBehaviorPolicyTests.cs): command 入力欄の候補ショートカット有効化/アクティブ化時ネイティブ再フォーカス有効化ポリシー。
- [`WindowActivationCommandFocusPolicyTests.cs`](../Praxis.Tests/WindowActivationCommandFocusPolicyTests.cs): ウィンドウ再アクティブ時の command フォーカス可否。
- [`SearchFocusGuardPolicyTests.cs`](../Praxis.Tests/SearchFocusGuardPolicyTests.cs): macOS の Search フォーカスガード判定。
- [`AsciiInputFilterTests.cs`](../Praxis.Tests/AsciiInputFilterTests.cs): macOS モーダル command 入力経路で使う ASCII フィルタ判定。
- [`MacCommandInputSourcePolicyTests.cs`](../Praxis.Tests/MacCommandInputSourcePolicyTests.cs): macOS ASCII 入力ソース強制の適用条件（first responder / キーウィンドウ / アプリ active / 欄ごとの opt-in）と、フォーカス中の再強制間隔の安全性を検証。
- [`WindowsCommandInputImePolicyTests.cs`](../Praxis.Tests/WindowsCommandInputImePolicyTests.cs): Windows IME の ASCII モード適用判定（欄ごとの opt-in を含む `ShouldForceAsciiImeMode`）、フォーカス時 ASCII 補正の再試行スケジュール、フォーカス中英字再強制の適用条件/間隔、変換モード正規化、キャレット補正。
- [`WindowsInputScopeCompatibilityPolicyTests.cs`](../Praxis.Tests/WindowsInputScopeCompatibilityPolicyTests.cs): ネイティブ `InputScope` 設定失敗時（`ArgumentException`）のフォールバック判定。
- [`WindowsModalFocusRestorePolicyTests.cs`](../Praxis.Tests/WindowsModalFocusRestorePolicyTests.cs): Windows 編集モーダル/競合ダイアログのフォーカス復帰条件。
- [`ConflictDialogFocusRestorePolicyTests.cs`](../Praxis.Tests/ConflictDialogFocusRestorePolicyTests.cs): 競合ダイアログ閉鎖後の編集フォーカス復帰条件。
- [`EditorShortcutActionResolverTests.cs`](../Praxis.Tests/EditorShortcutActionResolverTests.cs): モーダル/コンテキスト/競合のキー操作マッピング。
- [`EditorShortcutScopeResolverTests.cs`](../Praxis.Tests/EditorShortcutScopeResolverTests.cs): オーバーレイ表示状態のショートカット有効範囲判定。
- [`EditorTabInsertionResolverTests.cs`](../Praxis.Tests/EditorTabInsertionResolverTests.cs): タブ文字フォールバック検知と遷移方向判定。
- [`FocusRingNavigatorTests.cs`](../Praxis.Tests/FocusRingNavigatorTests.cs): ラップ付きフォーカスインデックス遷移。

### UI 非依存の見た目 / レイアウトポリシー
- [`InputClearButtonVisibilityPolicyTests.cs`](../Praxis.Tests/InputClearButtonVisibilityPolicyTests.cs): クリアボタン表示条件。
- [`DockScrollBarVisibilityPolicyTests.cs`](../Praxis.Tests/DockScrollBarVisibilityPolicyTests.cs): ポインターホバー状態 + 横オーバーフロー状態に基づく Dock スクロールバー表示判定と、マスク表示の反転ルール判定。
- [`ClearButtonGlyphAlignmentPolicyTests.cs`](../Praxis.Tests/ClearButtonGlyphAlignmentPolicyTests.cs): クリアボタン `x` の座標補正。
- [`ClearButtonRefocusPolicyTests.cs`](../Praxis.Tests/ClearButtonRefocusPolicyTests.cs): クリア後フォーカス復帰リトライ間隔。Mac Catalyst の遅延再試行も検証する。
- [`WindowsNativeFocusSafetyPolicyTests.cs`](../Praxis.Tests/WindowsNativeFocusSafetyPolicyTests.cs): WinUI の native `TextBox` 再フォーカス/キャレット復帰を live control に限定する安全条件を検証する。
- [`ButtonFocusVisualPolicyTests.cs`](../Praxis.Tests/ButtonFocusVisualPolicyTests.cs): フォーカス枠スタイル判定。
- [`ModalEditorHeightResolverTests.cs`](../Praxis.Tests/ModalEditorHeightResolverTests.cs): 複数行エディタ高さ算出とクランプ。
- [`ModalEditorScrollHeightResolverTests.cs`](../Praxis.Tests/ModalEditorScrollHeightResolverTests.cs): モーダル項目スクロール高さクランプ。
- [`ThemeTextColorPolicyTests.cs`](../Praxis.Tests/ThemeTextColorPolicyTests.cs): テーマ連動文字色判定。
- [`ThemeDarkStateResolverTests.cs`](../Praxis.Tests/ThemeDarkStateResolverTests.cs): 実効ダーク判定。
- [`ThemeShortcutModeResolverTests.cs`](../Praxis.Tests/ThemeShortcutModeResolverTests.cs): macOS キー入力からテーマモード解決。
- [`TextCaretPositionResolverTests.cs`](../Praxis.Tests/TextCaretPositionResolverTests.cs): キャレット末尾配置判定。
- [`UiTimingPolicyTests.cs`](../Praxis.Tests/UiTimingPolicyTests.cs): フォーカス復帰・アクティベーション・ポーリングの UI タイミング定数と順序条件。

### 起動 / パス / ストレージ / リフレクション補助
- [`LaunchTargetResolverTests.cs`](../Praxis.Tests/LaunchTargetResolverTests.cs): HTTP(S)/ファイル/パスのフォールバック起動先解決、環境変数展開、相対パス判定、bare `~` のホームパス判定。
- [`CommandExecutorTests.cs`](../Praxis.Tests/CommandExecutorTests.cs): linked-source で取り込んだ `CommandExecutor` のホームパス展開テスト。空 `tool` フォールバック時の `~`、`~/...`、`~\\...` を展開し、通常の相対パスはそのまま残すこと、および Windows シェル起動時の作業ディレクトリ上書きを確認。
- [`CommandWorkingDirectoryPolicyTests.cs`](../Praxis.Tests/CommandWorkingDirectoryPolicyTests.cs): `cmd`、`powershell`、`pwsh`、`wt` をユーザープロファイル起点で開く対象として判定する純粋ポリシーテスト。
- [`WindowsPathPolicyTests.cs`](../Praxis.Tests/WindowsPathPolicyTests.cs): Windows の認証先行起動フローで使う UNC（`\\\\server\\share`）判定。
- [`AppStoragePathLayoutResolverTests.cs`](../Praxis.Tests/AppStoragePathLayoutResolverTests.cs): プラットフォーム別ストレージ配置ルール。
- [`DatabaseSchemaVersionPolicyTests.cs`](../Praxis.Tests/DatabaseSchemaVersionPolicyTests.cs): スキーマバージョンのアップグレード経路解決（`PRAGMA user_version` の段階適用順序、未対応/未来バージョン拒否）を検証。`v1 -> v2 -> v3 -> v4` と `未バージョン -> 現行` の段階適用も確認する。
- [`NonPublicPropertySetterTests.cs`](../Praxis.Tests/NonPublicPropertySetterTests.cs): リフレクションによる書き込み可能プロパティ設定。

## CI との整合
- CI（[`.github/workflows/ci.yml`](../.github/workflows/ci.yml)）は、次のコマンドでテスト＋カバレッジ収集を実行します。
  - `dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --no-restore -v minimal --collect:"XPlat Code Coverage" --results-directory ./TestResults`
- CI は `TestResults/**/coverage.cobertura.xml` を Cobertura アーティファクトとして保存します。
- テストファイルの追加・削除・改名時は本ガイドも更新してください。
