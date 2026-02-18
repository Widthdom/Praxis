# Developer Guide

## Scope
This document is for developers working on Praxis.
README is user-facing summary; this guide is the implementation-level source of truth.

## Tech Stack
- UI/App: .NET MAUI (`Praxis`)
- Core logic: .NET class library (`Praxis.Core`)
- Tests: xUnit (`Praxis.Tests`)
- Persistence: SQLite (`sqlite-net-pcl`)
- MVVM tooling: `CommunityToolkit.Mvvm`

## Target Platforms
- Windows: `net10.0-windows10.0.19041.0`
- macOS (Mac Catalyst): `net10.0-maccatalyst`

## Architecture Rules
- Strict MVVM in app layer
- No feature logic in code-behind
- Keep business logic in `Praxis.Core` when possible
- UI services (clipboard/theme/process) stay in `Praxis/Services`

## Main Components
- `ViewModels/MainViewModel.cs`
  - Orchestrates command execution, filtering, edit modal, drag/save, dock, and theme apply
  - Persists dock order through repository when dock contents change
  - Handles cross-window sync reload with diff-apply when external button/dock changes are notified
  - Applies cross-window theme sync by reloading persisted theme on external notifications
  - Refreshes command suggestions on external sync when command input is active
  - Defers sync reload while editor modal is open and applies it after close
  - On editor save, performs optimistic conflict check (`UpdatedAtUtc`) against latest DB value and resolves by `Reload latest` / `Overwrite mine` / `Cancel`
- `Services/SqliteAppRepository.cs`
  - Tables: button definitions, logs, app settings
  - Includes simple in-memory cache for button reads
  - Provides `ReloadButtonsAsync` for cross-window sync paths to force-refresh cache from SQLite
  - Provides `GetByIdAsync(id, forceReload: true)` for save-time conflict checks against latest persisted row
  - Uses case-insensitive command cache for fast `GetByCommandAsync`
  - Purges old logs with one SQL `DELETE ... WHERE TimestampUtc < threshold`
  - Stores dock order in `AppSettingEntity` key `dock_order` (comma-separated GUID list)
- `Services/CommandExecutor.cs`
  - Launches tool + arguments with shell execution
  - If `tool` is empty, falls back to `Arguments` target resolution:
    - `http/https` => default browser
    - file path => default associated app (for example `.pdf`)
    - directory path => file manager (`Explorer`/`Finder`)
- `Services/FileStateSyncNotifier.cs`
  - Uses a local signal file (`buttons.sync`) and `FileSystemWatcher` for multi-window notifications
  - Payload includes instance id and timestamp; self-origin events are ignored
- `Services/AppStoragePaths.cs`
  - Centralizes shared local-storage constants/paths (DB, sync signal)
- `Controls/CommandEntry.cs` / `Platforms/MacCatalyst/Handlers/CommandEntryHandler.cs`
  - macOS command input uses a dedicated control/handler so `Up/Down` suggestion navigation is handled reliably at native `UITextField` level
- `Praxis.Core/Logic/*.cs`
  - Search matcher, command line builder, grid snap, log retention, launch target resolver, button layout defaults, record version comparer

## Development Workflow
1. Implement/modify pure logic in `Praxis.Core` first.
2. Add/adjust unit tests in `Praxis.Tests`.
3. Wire app behavior in `Praxis` ViewModel/Services.
4. Verify with:
   - `dotnet test Praxis.slnx`

## CI/CD (GitHub Actions)
- `ci.yml`
  - Push/PR on `main`/`master`
  - Executes:
    - `dotnet test Praxis.Tests/Praxis.Tests.csproj`
    - Windows app build (`net10.0-windows10.0.19041.0`)
    - Mac Catalyst app build (`net10.0-maccatalyst`)
- `delivery.yml`
  - Manual run or `v*` tag push
  - Publishes Windows/Mac Catalyst outputs and uploads them as workflow artifacts

## Coding Conventions
- Prefer nullable-safe code and explicit guards
- Keep methods small; isolate side effects
- Preserve ASCII unless file already requires Unicode
- Use descriptive names for commands and services

## Adding New Features
- Add domain types or logic in `Praxis.Core` if testable without MAUI
- Add interfaces in `Praxis/Services` for platform-bound concerns
- Inject dependencies through DI in `MauiProgram.cs`
- Add tests for any new non-UI logic

## Current UI Notes
- Main modal copy buttons trigger a center overlay notification animation in `MainPage.xaml(.cs)`.
- Icon glyphs are platform-mapped (`OnPlatform`): WinUI uses `Segoe MDL2 Assets`, macOS uses fallback symbols.
- Modal footer action buttons (`Cancel`/`Save`) are centered and use equal width for visual balance.
- Dock item visuals are intentionally matched to placement-area button visuals.
- Middle click edit is implemented via `Behaviors/MiddleClickBehavior.cs` plus macOS fallbacks in `MainPage.xaml.cs` / `Platforms/MacCatalyst/AppDelegate.cs`.
- Tab focus policy is applied in `MainPage.xaml.cs` (`ApplyTabPolicy`) by toggling native `IsTabStop`.
- Selection rectangle is rendered as `SelectionRect` in `MainPage.xaml` with gray stroke/fill.
- Selection toggle modifier handling is centralized in `MainPage.xaml.cs`:
  - Windows: `Ctrl+Click`
  - macOS (Mac Catalyst): `Command+Click`
  - Implemented via reflection-based modifier detection (`IsSelectionModifierPressed`) to avoid Windows regressions.
- Theme switching buttons are intentionally removed from the UI.
- Theme mode is persisted via repository settings and restored on startup.
- Global shortcuts in `MainPage.xaml.cs`:
  - `Ctrl+Shift+L` => Light
  - `Ctrl+Shift+D` => Dark
  - `Ctrl+Shift+H` => System
  - Windows keeps shortcuts active in non-modal states by combining page-level key handlers, text-input key hooks, and root window key hook
- Global shortcuts on macOS are wired in `Platforms/MacCatalyst/AppDelegate.cs`:
  - `Command+Shift+L` => Light
  - `Command+Shift+D` => Dark
  - `Command+Shift+H` => System
- Status bar is a rounded `Border` (`StatusBarBorder`) and flashes color briefly on `StatusText` change:
  - normal: green
  - error (`Failed`/`error`/`exception`): red
  - after flash/theme switch, local background override is cleared so AppThemeBinding is restored
- Command input suggestion UX:
  - `MainViewModel` builds `CommandSuggestions` from partial match on `LauncherButtonItemViewModel.Command`
  - Suggestion refresh is debounced (`~120ms`) to reduce rapid recomputation during typing
  - Candidate row displays `Command`, `ButtonText`, `Tool Arguments` in `1:1:4` width ratio
  - `Up/Down` wraps at list edges, and `Enter` executes selected suggestion
  - Suggestion click fills `CommandInput` and executes immediately.
  - Plain Enter execution from command box runs all exact command matches (trim-aware, case-insensitive)
  - Windows arrow key handling is attached in `MainPage.xaml.cs` (`MainCommandEntry_HandlerChanged` / native `KeyDown`)
  - macOS arrow key handling is attached in `Controls/CommandEntry` + `Platforms/MacCatalyst/Handlers/CommandEntryHandler.cs` (`PressesBegan`)
  - macOS `Entry` visual/focus behavior is handled by `Platforms/MacCatalyst/Handlers/MacEntryHandler.cs`:
    - suppresses default blue focus ring
    - uses bottom-edge emphasis that respects corner radius
    - sets caret color by theme (Light=black, Dark=white)
- macOS editor modal keyboard behavior:
  - `Tab` / `Shift+Tab` traversal is confined to modal controls and wraps at edges.
  - `GUID` is selectable but not editable.
  - When pseudo-focus is on `Cancel` / `Save`, `Enter` executes the focused action.
- Placement-area rendering/performance:
  - `MainPage.xaml.cs` forwards viewport scroll/size to `MainViewModel.UpdateViewport(...)`
  - `MainViewModel` keeps filtered list and updates `VisibleButtons` via diff (insert/move/remove), not full clear+rebind
  - Visible target is viewport-based with a safety margin for smooth scrolling
  - Drag updates throttle `UpdateCanvasSize()` during move and force final update on completion
- Create flows:
  - Top-bar create icon button uses `CreateNewCommand` and does not consume clipboard.
  - Right-click on empty placement area is handled in `Selection_PointerPressed` and opens create editor at clicked canvas coordinates.
  - Right-click create flow seeds editor `Arguments` from clipboard.
  - Starting create flow clears `SearchText` (top-bar create and empty-area right-click).
- Editor modal field behavior:
  - `Clip Word` uses multiline `Editor` (same behavior class as `Note`).
- Conflict resolution dialog:
  - Replaces native action sheet with in-app overlay dialog (`ConflictOverlay`) for visual consistency.
  - Supports both Light and Dark themes.

## Test Coverage Notes
- `Praxis.Tests/UnitTest1.cs` (`CoreLogicTests` class) covers baseline behavior.
- `Praxis.Tests/CommandRecordMatcherTests.cs` covers:
  - exact command match selection for command-box Enter execution
  - trim/case-insensitive match behavior
  - blank/no-match handling
- `Praxis.Tests/CoreLogicEdgeCaseTests.cs` covers edge cases for:
  - command-line normalization
  - grid snapping/clamping boundaries
  - search matching defaults/case-insensitivity
  - retention threshold boundaries and minimum retention-day handling
- `Praxis.Tests/LaunchTargetResolverTests.cs` covers fallback-target parsing:
  - HTTP(S) detection
  - path-like argument detection
  - environment-variable expansion and quoted path handling
  - unsupported scheme/blank handling
- `Praxis.Tests/CoreLogicPerformanceSafetyTests.cs` covers regression/safety checks for:
  - button layout defaults (`120x40`) and 10px-grid alignment
  - parser/builder/snapper safety edge cases
  - retention lower-bound handling
  - expected button default constant values
  - record version conflict detection (`RecordVersionComparer`)

## Release/License
- Project license is MIT (`../LICENSE`)
- Keep copyright header/year aligned when needed

---

# 開発者ガイド（日本語）

## 対象範囲
このドキュメントは Praxis の開発者向けです。
README はユーザー向け要約、このガイドは実装仕様の正本です。

## 技術スタック
- UI / アプリ: .NET MAUI（`Praxis`）
- コアロジック: .NET クラスライブラリ（`Praxis.Core`）
- テスト: xUnit（`Praxis.Tests`）
- 永続化: SQLite（`sqlite-net-pcl`）
- MVVM ツール: `CommunityToolkit.Mvvm`

## 対応ターゲット
- Windows: `net10.0-windows10.0.19041.0`
- macOS（Mac Catalyst）: `net10.0-maccatalyst`

## アーキテクチャ方針
- アプリ層は厳格に MVVM を適用
- 機能ロジックをコードビハインドに書かない
- ビジネスロジックは可能な限り `Praxis.Core` に置く
- クリップボード / テーマ / プロセスなどの UI 依存処理は `Praxis/Services` に置く

## 主要コンポーネント
- `ViewModels/MainViewModel.cs`
  - コマンド実行、検索、編集モーダル、ドラッグ保存、Dock、テーマ適用を統括
  - Dock 更新時にリポジトリ経由で順序を永続化
  - 外部通知時にボタン/DOCK変更を差分再読込してウィンドウ間同期する
  - 外部通知受信時に保存済みテーマを再読込して、ウィンドウ間でテーマ同期する
  - command 入力中は外部同期時に候補一覧を再計算する
  - 編集モーダル表示中は同期反映を保留し、閉じた後に反映する
  - 編集保存時に `UpdatedAtUtc` の楽観的競合チェックを実施し、`Reload latest` / `Overwrite mine` / `Cancel` で解決する
- `Services/SqliteAppRepository.cs`
  - テーブル: ボタン定義、実行ログ、アプリ設定
  - ボタン読み取り向けのシンプルなメモリキャッシュを含む
  - ウィンドウ間同期経路では `ReloadButtonsAsync` で SQLite から強制再読込し、キャッシュを更新する
  - 保存時競合チェック向けに `GetByIdAsync(id, forceReload: true)` で最新行を取得できる
  - `GetByCommandAsync` は大文字小文字非依存の command キャッシュで高速化
  - 古いログ削除は `TimestampUtc` 閾値で SQL 一括 `DELETE` を実行
  - `AppSettingEntity` の `dock_order` キーに Dock 順序（GUID CSV）を保存
- `Services/CommandExecutor.cs`
  - ツール + 引数をシェル実行で起動
  - `tool` が空の場合は `Arguments` を解決してフォールバック起動:
    - `http/https` => 既定ブラウザ
    - ファイルパス => 既定関連付けアプリ（例: `.pdf`）
    - フォルダパス => ファイルマネージャ（`Explorer` / `Finder`）
- `Services/FileStateSyncNotifier.cs`
  - ローカル通知ファイル（`buttons.sync`）と `FileSystemWatcher` で複数ウィンドウ通知を実現
  - ペイロードのインスタンスID/時刻で自己通知を除外
- `Services/AppStoragePaths.cs`
  - ローカル保存先の共通定数/パス（DB、同期シグナル）を集約
- `Controls/CommandEntry.cs` / `Platforms/MacCatalyst/Handlers/CommandEntryHandler.cs`
  - macOS の command 入力は専用コントロール/ハンドラを使い、候補 `↑/↓` をネイティブ `UITextField` レベルで安定処理する
- `Praxis.Core/Logic/*.cs`
  - 検索マッチャー、コマンドライン構築、グリッドスナップ、ログ保持期間処理、起動ターゲット解決、ボタンレイアウト既定値、レコード版比較

## 開発ワークフロー
1. まず `Praxis.Core` に純粋ロジックを実装 / 修正する
2. `Praxis.Tests` に単体テストを追加 / 調整する
3. `Praxis` の ViewModel / Services にアプリ動作を接続する
4. 次のコマンドで確認する
   - `dotnet test Praxis.slnx`

## CI/CD（GitHub Actions）
- `ci.yml`
  - `main` / `master` への push・PR で実行
  - 実行内容:
    - `dotnet test Praxis.Tests/Praxis.Tests.csproj`
    - Windows アプリビルド（`net10.0-windows10.0.19041.0`）
    - Mac Catalyst アプリビルド（`net10.0-maccatalyst`）
- `delivery.yml`
  - 手動実行または `v*` タグ push で実行
  - Windows / Mac Catalyst 向け publish 結果を Actions アーティファクトとして保存

## コーディング規約
- nullable 安全なコードと明示的なガードを優先する
- メソッドは小さく保ち、副作用を分離する
- 既存ファイルが必要としない限り ASCII を維持する
- コマンドやサービスには意図が分かる名前を付ける

## 新機能の追加
- MAUI 非依存でテストできるものは `Praxis.Core` に追加する
- プラットフォーム依存の関心事は `Praxis/Services` にインターフェースを追加する
- `MauiProgram.cs` の DI 経由で依存関係を注入する
- UI 非依存ロジックの新規追加時は必ずテストを追加する

## 現在の UI 実装メモ
- モーダルのコピーアイコン押下時は `MainPage.xaml(.cs)` で中央通知オーバーレイをアニメーション表示する。
- アイコングリフは `OnPlatform` で出し分ける（Windows は `Segoe MDL2 Assets`、macOS は互換シンボル）。
- モーダル下部のアクションボタン（`Cancel` / `Save`）は中央寄せ・同一幅で揃えている。
- Dock ボタンの見た目は、配置領域のボタンと意図的に揃えている。
- ホイールクリック編集は `Behaviors/MiddleClickBehavior.cs` に加え、macOS 向けフォールバックを `MainPage.xaml.cs` / `Platforms/MacCatalyst/AppDelegate.cs` で実装している。
- Tab フォーカス制御は `MainPage.xaml.cs` の `ApplyTabPolicy` でネイティブ `IsTabStop` を切り替えて実現している。
- 矩形選択は `MainPage.xaml` の `SelectionRect`（グレーストローク/グレー透過塗り）で描画している。
- 選択トグル修飾キー判定は `MainPage.xaml.cs` に集約している。
  - Windows: `Ctrl+クリック`
  - macOS（Mac Catalyst）: `Command+クリック`
  - Windows 既存挙動を壊さないため、反射ベースで修飾キーを取得して分岐する。
- テーマ切替ボタンは UI から削除している。
- テーマモードはリポジトリ設定として保存され、次回起動時に復元される。
- `MainPage.xaml.cs` でグローバルショートカットを処理している。
  - `Ctrl+Shift+L` => ライト
  - `Ctrl+Shift+D` => ダーク
  - `Ctrl+Shift+H` => システム
  - Windows ではページレベルのキー処理、入力欄キーフック、ルートウィンドウキーフックを併用して、モーダル非表示時も有効化
- macOS のグローバルショートカットは `Platforms/MacCatalyst/AppDelegate.cs` で処理している。
  - `Command+Shift+L` => ライト
  - `Command+Shift+D` => ダーク
  - `Command+Shift+H` => システム
- ステータスバーは角丸 `Border`（`StatusBarBorder`）で構成し、`StatusText` 変更時に短時間の色フラッシュを行う。
  - 通常: 緑
  - エラー（`Failed` / `error` / `exception` 判定）: 赤
  - フェード後やテーマ切替後はローカル背景上書きを解除し、AppThemeBinding を再適用する
- command 入力補助:
  - `MainViewModel` で `LauncherButtonItemViewModel.Command` の部分一致候補 (`CommandSuggestions`) を構築
  - 候補更新はデバウンス（約 `120ms`）して、連続入力時の再計算を抑える
  - 候補行は `Command`、`ButtonText`、`Tool Arguments` を `1:1:4` 比率で表示
  - 候補ポップアップの行描画は Windows / macOS で同一の構築処理（`MainPage.xaml.cs` の `RebuildCommandSuggestionStack`）に統一
  - `↑/↓` は候補端で循環し、`Enter` で選択候補を実行する
  - 候補クリック時は `CommandInput` を埋めて即時実行する
  - コマンド欄で候補未選択の `Enter` 実行時は、`command` 完全一致（前後空白除去・大文字小文字非依存）の対象を全件実行する
  - Windows の方向キー上下は `MainPage.xaml.cs` の `MainCommandEntry_HandlerChanged` / ネイティブ `KeyDown` で処理
  - macOS の方向キー上下は `Controls/CommandEntry` + `Platforms/MacCatalyst/Handlers/CommandEntryHandler.cs` の `PressesBegan` で処理
  - macOS の `Entry` 見た目/フォーカス挙動は `Platforms/MacCatalyst/Handlers/MacEntryHandler.cs` で制御する。
    - 標準の青いフォーカスリングを抑制
    - 角丸に沿った下辺強調を適用
    - キャレット色をテーマ連動（Light=黒、Dark=白）
- macOS の編集モーダルのキーボード挙動:
  - `Tab` / `Shift+Tab` の遷移はモーダル内に閉じ、端で循環する。
  - `GUID` 欄は選択可能だが編集不可。
  - `Cancel` / `Save` の擬似フォーカス中は `Enter` で該当アクションを実行する。
- 配置領域の描画/性能最適化:
  - `MainPage.xaml.cs` からスクロール位置と表示サイズを `MainViewModel.UpdateViewport(...)` に連携
  - `MainViewModel` はフィルタ済み一覧を保持し、`VisibleButtons` を差分更新（insert/move/remove）する
  - 描画対象はビューポート＋余白で判定して、スクロール時の負荷を低減
  - ドラッグ中の `UpdateCanvasSize()` は間引き、完了時に最終更新を保証
- Create 作成フロー:
  - 上部の Create アイコンボタンは `CreateNewCommand` を実行し、クリップボードは参照しない。
  - 配置領域の空白右クリックは `Selection_PointerPressed` で処理し、クリックしたキャンバス座標で新規作成モーダルを開く。
  - 空白右クリック作成時のみ、エディタの `Arguments` にクリップボード値を初期設定する。
  - 新規作成開始時（上部 Create / 空白右クリック）の両経路で `SearchText` をクリアする。
- 編集モーダルの欄仕様:
  - `Clip Word` は `Note` と同様に複数行 `Editor` を使い、行数に応じて高さを調整する。
- 競合解決ダイアログ:
  - OS 既定のアクションシートではなく、アプリ内オーバーレイ（`ConflictOverlay`）で表示してデザインを統一。
  - ライト/ダーク両テーマに対応。

## テストカバレッジメモ
- `Praxis.Tests/UnitTest1.cs`（`CoreLogicTests` クラス）は基本動作を検証する。
- `Praxis.Tests/CommandRecordMatcherTests.cs` は次を検証する。
  - コマンド欄 Enter 実行で使う command 完全一致選択
  - 前後空白除去 / 大文字小文字非依存の一致挙動
  - 空入力 / 非一致時の扱い
- `Praxis.Tests/CoreLogicEdgeCaseTests.cs` は次の境界系を検証する。
  - コマンドライン文字列正規化
  - グリッドスナップ / 領域クランプ境界
  - 検索マッチャーの既定動作 / 大文字小文字非依存
  - ログ保持期間の閾値境界 / 最小保持日数補正
- `Praxis.Tests/LaunchTargetResolverTests.cs` はフォールバック起動ターゲット解析を検証する。
  - HTTP(S) 判定
  - パス形式引数の判定
  - 環境変数展開と引用符付きパスの扱い
  - 非対応スキーム / 空文字の扱い
- `Praxis.Tests/CoreLogicPerformanceSafetyTests.cs` は回帰/安全性の検証を行う。
  - ボタン既定サイズ（`120x40`）と 10px グリッド整合
  - 各種パーサ/ビルダ/スナッパの境界系
  - 保持日数の下限補正
  - ボタン既定定数の期待値
  - レコード版競合検知（`RecordVersionComparer`）

## リリース / ライセンス
- ライセンスは MIT（`../LICENSE`）
- 必要に応じて著作権ヘッダと年表記を整合させる
