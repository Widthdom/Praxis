# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [Unreleased]

### Added
- add .codex & CLAUDE.md

### Fixed
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

### [1.1.3] - 2026-04-05

### Added
- Automatic GitHub Release creation on `v*` tag push — delivery workflow now zips OS-specific artifacts (Windows / macOS) and publishes them with auto-generated release notes

### Fixed
- .NET 10 preview runtime package restore failure (`NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64 with version (= 10.0.5)`) in CI and Delivery workflows — replaced pinned `dotnet-version: 10.0.100` with `10.0.x` + `dotnet-quality: preview` to enable the preview NuGet feed

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

### 追加
- .codex および CLAUDE.md を追加

### 修正
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

### [1.1.3] - 2026-04-05

### 追加
- `v*` タグ push 時に GitHub Release を自動作成 — delivery ワークフローが OS 別アーティファクト（Windows / macOS）を zip 化し、自動生成リリースノート付きで公開

### 修正
- CI および Delivery ワークフローで .NET 10 プレビューランタイムパッケージの復元が失敗する問題を修正（`NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64 with version (= 10.0.5)`）— 固定指定の `dotnet-version: 10.0.100` を `10.0.x` + `dotnet-quality: preview` に変更しプレビュー NuGet フィードを有効化

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

[Unreleased]: https://github.com/Widthdom/Praxis/compare/v1.1.3...HEAD
[1.1.3]: https://github.com/Widthdom/Praxis/compare/v1.1.2...v1.1.3
[1.1.2]: https://github.com/Widthdom/Praxis/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/Widthdom/Praxis/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/Widthdom/Praxis/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Widthdom/Praxis/tree/v1.0.0
