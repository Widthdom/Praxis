# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [Unreleased]

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
- Windows: Tab focus navigation selects all text in input fields
- Clear button focus restore stability after tap (immediate attempt + short delayed retry)
- Clear-button X glyph vertical centering on Windows
- Command suggestion colors stay theme-synced during live theme switch
- `CommandEntry` / `SearchEntry`: lowercase letters no longer converted to uppercase
- IME / ASCII enforcement in command entry:
  - Windows: `InputScopeNameValue.AlphanumericHalfWidth` on focus + `imm32` nudge (immediate + one delayed retry)
  - macOS: `AsciiInputFilter` + `setMarkedText` / `insertText` blocking, detached on app deactivation
- Modal `Command` IME reasserted while focused on Windows (prevents manual IME-mode switching)
- macOS: ASCII input source enforced only while field is first responder in active key window
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
- `MainPage` refactored into 12 concern-based partial classes (`PointerAndSelection`, `FocusAndContext`, `EditorAndInput`, `ShortcutsAndConflict`, `MacCatalystBehavior`, `LayoutUtilities`, and field partials)
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
- Windows: Tab フォーカス移動時にテキスト入力欄の全選択
- クリアボタンタップ後のフォーカス復帰を安定化（即時試行 + 短遅延リトライ）
- Windows のクリアボタン X グリフの垂直方向センタリング
- テーマのライブ切替中もコマンド候補の色をテーマに同期
- `CommandEntry` / `SearchEntry` で英小文字が大文字変換される問題
- コマンド入力欄の IME / ASCII 強制:
  - Windows: フォーカス時に `InputScopeNameValue.AlphanumericHalfWidth` + `imm32` ナッジ（即時 + 短遅延リトライ）
  - macOS: `AsciiInputFilter` + `setMarkedText` / `insertText` ブロック（アプリ非アクティブ時は即時解除）
- Windows のモーダル `Command` 欄でフォーカス中も IME を英字に再強制（手動 IME 切替を抑止）
- macOS: ASCII 入力ソース強制をアクティブキーウィンドウのファーストレスポンダ中のみに限定
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
- `MainPage` を責務別 12 個の partial クラスに分割（`PointerAndSelection`、`FocusAndContext`、`EditorAndInput`、`ShortcutsAndConflict`、`MacCatalystBehavior`、`LayoutUtilities` およびフィールド partial 群）
- `SqliteAppRepository` の全公開操作を排他制御で保護しスレッドセーフに
- UI 遅延値を `UiTimingPolicy` へ集約
- `MainPage` フィールドファイルと `MauiProgram` ハンドラ登録のプラットフォームプリプロセッサブロックを整理・統合
- 重複 `using` ディレクティブの削除と `using` 順序の正規化
- `MainViewModel` と各 partial クラスに主要ライフサイクルイベントの `LogInfo` 呼び出しを追加
