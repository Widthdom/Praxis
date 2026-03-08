# Testing Guide

## Scope
This document is the test-specific source of truth for Praxis.
Use it for test execution, coverage checks, and file-by-file intent.

## Test Stack
- Framework: xUnit
- Runner: `Microsoft.NET.Test.Sdk`
- Coverage collector: `coverlet.collector` (`XPlat Code Coverage`)
- Test project: `Praxis.Tests/Praxis.Tests.csproj`
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
- `UnitTest1.cs` (`CoreLogicTests`): baseline checks for command-line build, snapping, search matching, and retention.
- `CoreLogicEdgeCaseTests.cs`: parser/snapper/matcher/retention edge cases.
- `CoreLogicPerformanceSafetyTests.cs`: regression-safety checks for defaults, bounds, and conflict detection.
- `PolicyTruthTableTests.cs`: full truth-table validation for focus-related policy combinations.

### Models / Defaults
- `ModelDefaultsTests.cs`: default values and initialization guarantees for `LauncherButtonRecord` and `LaunchLogEntry`.

### Command Execution / Matching / Suggestions
- `CommandLineBuilderTests.cs`: null/whitespace handling and normalization for command-line construction.
- `CommandRecordMatcherTests.cs`: exact command matching rules, null guards, and case/trim behavior.
- `CommandSuggestionVisibilityPolicyTests.cs`: close policy when context menu opens.
- `CommandSuggestionRowColorPolicyTests.cs`: selected/unselected row color decisions per theme.
- `CommandNotFoundRefocusPolicyTests.cs`: refocus decision for `Command not found:` status.
- `StatusFlashErrorPolicyTests.cs`: status classification for error flash behavior.

### Input / Keyboard / Focus Policies
- `WindowActivationCommandFocusPolicyTests.cs`: activation-time command focus gating.
- `SearchFocusGuardPolicyTests.cs`: macOS search-focus guard decision rules.
- `AsciiInputFilterTests.cs`: ASCII filtering rules used by macOS command input paths.
- `MacCommandInputSourcePolicyTests.cs`: macOS ASCII input-source enforcement gating (first-responder + key-window + app-active) for focus-time enforcement without background re-apply.
- `WindowsCommandInputImePolicyTests.cs`: Windows IME/input-scope enforcement and caret clamp logic.
- `WindowsModalFocusRestorePolicyTests.cs`: Windows editor/conflict focus restore conditions.
- `ConflictDialogFocusRestorePolicyTests.cs`: editor focus restore condition after conflict dialog close.
- `EditorShortcutActionResolverTests.cs`: key-to-action mapping for modal/context/conflict shortcuts.
- `EditorShortcutScopeResolverTests.cs`: active scope decision when overlays are open/closed.
- `EditorTabInsertionResolverTests.cs`: tab-character fallback detection and navigation mapping.
- `FocusRingNavigatorTests.cs`: wrap-around navigation index behavior.

### UI-Agnostic Visual / Layout Policies
- `InputClearButtonVisibilityPolicyTests.cs`: clear button visibility rule.
- `ClearButtonGlyphAlignmentPolicyTests.cs`: clear glyph translation policy.
- `ClearButtonRefocusPolicyTests.cs`: clear-button focus retry schedule by platform.
- `ButtonFocusVisualPolicyTests.cs`: focus-border style resolution.
- `ModalEditorHeightResolverTests.cs`: multiline editor height calculation and clamping.
- `ModalEditorScrollHeightResolverTests.cs`: modal scroll height clamping.
- `ThemeTextColorPolicyTests.cs`: theme text color policy.
- `ThemeDarkStateResolverTests.cs`: effective dark-mode resolution.
- `ThemeShortcutModeResolverTests.cs`: macOS key-input to theme-mode mapping.
- `TextCaretPositionResolverTests.cs`: caret-tail placement resolution.

### Launch / Path / Storage / Reflection Utilities
- `LaunchTargetResolverTests.cs`: HTTP(S)/file/path fallback target resolution and env expansion.
- `AppStoragePathLayoutResolverTests.cs`: platform-specific storage layout policy.
- `NonPublicPropertySetterTests.cs`: reflection-based writable property assignment behavior.

## CI Alignment
- CI (`.github/workflows/ci.yml`) executes `dotnet test Praxis.Tests/Praxis.Tests.csproj`.
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
- テストプロジェクト: `Praxis.Tests/Praxis.Tests.csproj`
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
- `UnitTest1.cs`（`CoreLogicTests`）: コマンドライン生成、スナップ、検索一致、保持期間の基本確認。
- `CoreLogicEdgeCaseTests.cs`: パーサ/スナッパ/マッチャー/保持期間の境界ケース。
- `CoreLogicPerformanceSafetyTests.cs`: 既定値・境界・競合判定の回帰安全性確認。
- `PolicyTruthTableTests.cs`: フォーカス系ポリシーの真理値表を網羅検証。

### モデル / 既定値
- `ModelDefaultsTests.cs`: `LauncherButtonRecord` / `LaunchLogEntry` の既定値・初期化保証。

### コマンド実行 / 一致 / 候補
- `CommandLineBuilderTests.cs`: コマンドライン構築の null/空白処理と正規化。
- `CommandRecordMatcherTests.cs`: command 完全一致規則、null ガード、trim/大小文字非依存。
- `CommandSuggestionVisibilityPolicyTests.cs`: コンテキストメニュー表示時の候補クローズ判定。
- `CommandSuggestionRowColorPolicyTests.cs`: テーマ別の候補行背景色判定。
- `CommandNotFoundRefocusPolicyTests.cs`: `Command not found:` 時の再フォーカス判定。
- `StatusFlashErrorPolicyTests.cs`: ステータスのエラーフラッシュ分類判定。

### 入力 / キーボード / フォーカス
- `WindowActivationCommandFocusPolicyTests.cs`: ウィンドウ再アクティブ時の command フォーカス可否。
- `SearchFocusGuardPolicyTests.cs`: macOS の Search フォーカスガード判定。
- `AsciiInputFilterTests.cs`: macOS command 入力経路で使う ASCII フィルタ判定。
- `MacCommandInputSourcePolicyTests.cs`: macOS ASCII 入力ソース強制の適用条件（first responder / キーウィンドウ / アプリ active）。フォーカス時適用で、バックグラウンド再強制をしない前提を検証。
- `WindowsCommandInputImePolicyTests.cs`: Windows IME/InputScope 強制とキャレット補正。
- `WindowsModalFocusRestorePolicyTests.cs`: Windows 編集モーダル/競合ダイアログのフォーカス復帰条件。
- `ConflictDialogFocusRestorePolicyTests.cs`: 競合ダイアログ閉鎖後の編集フォーカス復帰条件。
- `EditorShortcutActionResolverTests.cs`: モーダル/コンテキスト/競合のキー操作マッピング。
- `EditorShortcutScopeResolverTests.cs`: オーバーレイ表示状態のショートカット有効範囲判定。
- `EditorTabInsertionResolverTests.cs`: タブ文字フォールバック検知と遷移方向判定。
- `FocusRingNavigatorTests.cs`: ラップ付きフォーカスインデックス遷移。

### UI 非依存の見た目 / レイアウトポリシー
- `InputClearButtonVisibilityPolicyTests.cs`: クリアボタン表示条件。
- `ClearButtonGlyphAlignmentPolicyTests.cs`: クリアボタン `x` の座標補正。
- `ClearButtonRefocusPolicyTests.cs`: クリア後フォーカス復帰リトライ間隔。
- `ButtonFocusVisualPolicyTests.cs`: フォーカス枠スタイル判定。
- `ModalEditorHeightResolverTests.cs`: 複数行エディタ高さ算出とクランプ。
- `ModalEditorScrollHeightResolverTests.cs`: モーダル項目スクロール高さクランプ。
- `ThemeTextColorPolicyTests.cs`: テーマ連動文字色判定。
- `ThemeDarkStateResolverTests.cs`: 実効ダーク判定。
- `ThemeShortcutModeResolverTests.cs`: macOS キー入力からテーマモード解決。
- `TextCaretPositionResolverTests.cs`: キャレット末尾配置判定。

### 起動 / パス / ストレージ / リフレクション補助
- `LaunchTargetResolverTests.cs`: HTTP(S)/ファイル/パスのフォールバック起動先解決と環境変数展開。
- `AppStoragePathLayoutResolverTests.cs`: プラットフォーム別ストレージ配置ルール。
- `NonPublicPropertySetterTests.cs`: リフレクションによる書き込み可能プロパティ設定。

## CI との整合
- CI（`.github/workflows/ci.yml`）は `dotnet test Praxis.Tests/Praxis.Tests.csproj` を実行します。
- テストファイルの追加・削除・改名時は本ガイドも更新してください。
