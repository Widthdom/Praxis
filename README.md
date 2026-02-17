# Praxis

License: MIT (see `LICENSE`).

## Overview
Praxis is a desktop launcher app built with .NET MAUI and strict MVVM.
It stores launcher buttons in SQLite and can execute tools with arguments.

## Features (User)
- Enter-to-run from the command box
- Search and filter launcher buttons quickly
- Drag, multi-select, edit, and delete buttons in the placement area
- Dock history for recently clicked buttons (restored on next launch)
- Create new buttons from top-bar create icon or right-click in empty placement area
- Keyboard-friendly operation (arrow/enter in suggestions, modal save/cancel shortcuts)
- Theme switching shortcuts (Light / Dark / System) with persisted setting
- Status bar feedback and modal copy notification
- Platform icon fallback: WinUI uses Segoe MDL2 glyphs, macOS uses native-safe symbols
- When `tool` is empty, `Arguments` can open HTTP(S) URLs in browser; file paths open with the default associated app (for example `.pdf`), and directory paths open in Finder/Explorer
- Multi-window sync: button add/delete/update (including position changes) and dock order changes are propagated to other open windows
- Multi-window sync also propagates theme changes (Light / Dark / System) to other open windows

## Behavior Notes
- Suggestions are shown from partial `command` matches and can be selected by keyboard.
- Suggestion keyboard behavior: `Up/Down` moves selection, wraps at edges, and `Enter` executes selected command.
- Editor modal keyboard behavior:
  - `Tab`/`Shift+Tab` stays inside modal controls and wraps at edges.
  - On macOS, `GUID` remains read-only/selectable (not editable).
  - On macOS, when pseudo-focus is on `Cancel`/`Save`, `Enter` triggers that action.
- Empty-space right-click on the placement area opens create modal at cursor position.
- Dragging uses 10px snap; multi-select is supported with rectangle and modifier click.
- If `tool` is empty, `Arguments` falls back to URL/path launch behavior.
- Theme shortcuts:
  - Windows: `Ctrl+Shift+L` / `Ctrl+Shift+D` / `Ctrl+Shift+H`
  - macOS: `Command+Shift+L` / `Command+Shift+D` / `Command+Shift+H`
- Cross-window sync includes button changes, dock order, command-suggestion refresh, and theme sync.
- Save-time editor conflicts are resolved via an in-app dialog (`Reload latest` / `Overwrite mine` / `Cancel`).
- Full implementation-level specification: `docs/DEVELOPER_GUIDE.md`.

## Performance Notes
- Command suggestions use debounce (`~120ms`) to reduce per-keystroke recomputation.
- Placement area rendering is viewport-based with margin, and `VisibleButtons` is updated by diff (insert/move/remove) instead of full rebuild.
- During drag, canvas-size recomputation is throttled (periodic while moving, always on drag end).
- Repository command lookup uses an in-memory command cache (case-insensitive).
- Log retention purge is executed as a single SQL delete by timestamp threshold.

## Project Structure
- `Praxis/`: MAUI app (UI, ViewModels, Services)
- `Praxis.Core/`: pure logic and domain models
- `Praxis.Tests/`: unit tests for core logic

## Developer Dependencies (NuGet)
- App:
  - `CommunityToolkit.Mvvm` (MVVM source generators/commands)
  - `sqlite-net-pcl` (SQLite persistence)
- Tests:
  - `xunit`
  - `Microsoft.NET.Test.Sdk`
  - `coverlet.collector`

## Build and Test
```bash
dotnet test Praxis.slnx
```

## GitHub Actions
- `CI` (`.github/workflows/ci.yml`)
  - Trigger: push / pull request (`main`, `master`)
  - Runs: core tests, Windows build, Mac Catalyst build
- `Delivery` (`.github/workflows/delivery.yml`)
  - Trigger: manual (`workflow_dispatch`) or tag push (`v*`)
  - Runs: publish for Windows and Mac Catalyst
  - Output: downloadable workflow artifacts (`praxis-windows`, `praxis-maccatalyst`)

Platform-specific run examples:
```bash
# Windows
dotnet run --project Praxis/Praxis.csproj -f net10.0-windows10.0.19041.0

# macOS (Mac Catalyst)
dotnet build Praxis/Praxis.csproj -t:Run -f net10.0-maccatalyst -r maccatalyst-arm64 -p:RunWithOpen=false
```

If `-t:Run` fails at launch on macOS, use this fallback:
```bash
dotnet build Praxis/Praxis.csproj -f net10.0-maccatalyst -r maccatalyst-arm64
open Praxis/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Praxis.app
```

## Notes
Current app targets in this workspace:
- Windows (`net10.0-windows10.0.19041.0`)
- macOS via Mac Catalyst (`net10.0-maccatalyst`)

---

# Praxis（日本語）

ライセンス: MIT（`LICENSE` を参照）。

## 概要
Praxis は .NET MAUI と厳格な MVVM で構築したデスクトップ向けランチャーアプリです。
SQLite にボタン情報を保存し、ツールと引数を実行できます。

## 主な機能（ユーザー向け）
- コマンド欄で Enter 実行
- 検索でボタンを素早く絞り込み
- ボタン配置領域でドラッグ・複数選択・編集・削除
- クリック履歴を Dock に表示し、次回起動時に復元
- 上部 Create アイコンと空白右クリックから新規ボタン作成
- 候補一覧の上下キー選択やモーダル保存ショートカットなどのキーボード操作
- テーマ切替（ライト / ダーク / システム）と起動時復元
- ステータス表示とコピー通知
- アイコン表示はプラットフォーム別フォールバック対応（Windows は Segoe MDL2、macOS は互換シンボル）
- `tool` が空でも、`Arguments` が HTTP(S) URL ならブラウザ起動、ファイルパスなら既定関連付けアプリ（例: `.pdf`）、フォルダパスなら Finder/Explorer 起動
- 複数ウィンドウ同期: ボタンの追加/削除/更新（座標変更を含む）と DOCK 並び変更が、開いている他ウィンドウにも反映される
- 複数ウィンドウ同期で、テーマ変更（ライト / ダーク / システム）も他ウィンドウへ反映される

## 動作メモ
- command 部分一致で候補を表示し、キーボードで選択実行できます。
- 候補一覧のキーボード操作は `↑/↓` で移動、端で循環（先頭で↑→末尾、末尾で↓→先頭）、`Enter` で実行です。
- 編集モーダルのキーボード操作:
  - `Tab` / `Shift+Tab` はモーダル内のみで循環し、端でラップします。
  - macOS では `GUID` 欄は選択可能ですが編集不可です。
  - macOS では `Cancel` / `Save` の擬似フォーカス時に `Enter` で該当アクションを実行します。
- 配置領域の空白右クリックで、その座標に新規作成モーダルを開きます。
- ドラッグは 10px スナップ、矩形選択と修飾キー選択に対応しています。
- `tool` が空の場合は `Arguments` を URL/パスとしてフォールバック起動します。
- テーマ切替ショートカット:
  - Windows: `Ctrl+Shift+L` / `Ctrl+Shift+D` / `Ctrl+Shift+H`
  - macOS: `Command+Shift+L` / `Command+Shift+D` / `Command+Shift+H`
- ウィンドウ間同期はボタン変更、Dock順序、候補再計算、テーマ同期に対応します。
- 編集保存時に競合があれば、アプリ内ダイアログで `Reload latest` / `Overwrite mine` / `Cancel` を選択します。
- 実装仕様の正本は `docs/DEVELOPER_GUIDE.md` を参照してください。

## パフォーマンス最適化メモ
- command 候補検索はデバウンス（約 `120ms`）を入れて、キー入力ごとの過剰再計算を抑制。
- 配置領域はビューポート＋余白ベースで描画対象を絞り、`VisibleButtons` は全差し替えではなく差分更新（insert/move/remove）を行う。
- ドラッグ中のキャンバスサイズ再計算は間引きし、ドラッグ終了時には必ず更新する。
- リポジトリの command 検索は大文字小文字非依存のインメモリキャッシュを利用。
- ログ保持期間の削除は、閾値日時による SQL 一括削除で実行。

## プロジェクト構成
- `Praxis/`: MAUI アプリ本体（UI、ViewModel、Service）
- `Praxis.Core/`: 純粋ロジックとドメインモデル
- `Praxis.Tests/`: Core ロジックの単体テスト

## 開発者向け依存関係（NuGet）
- アプリ本体:
  - `CommunityToolkit.Mvvm`（MVVM のソースジェネレータ/コマンド）
  - `sqlite-net-pcl`（SQLite 永続化）
- テスト:
  - `xunit`
  - `Microsoft.NET.Test.Sdk`
  - `coverlet.collector`

## ビルドとテスト
```bash
dotnet test Praxis.slnx
```

## GitHub Actions
- `CI`（`.github/workflows/ci.yml`）
  - 実行契機: push / pull request（`main`, `master`）
  - 実行内容: Core テスト、Windows ビルド、Mac Catalyst ビルド
- `Delivery`（`.github/workflows/delivery.yml`）
  - 実行契機: 手動実行（`workflow_dispatch`）またはタグ push（`v*`）
  - 実行内容: Windows / Mac Catalyst 向け publish
  - 成果物: Actions のアーティファクト（`praxis-windows`, `praxis-maccatalyst`）

プラットフォーム別の実行 / ビルド例:
```bash
# Windows
dotnet run --project Praxis/Praxis.csproj -f net10.0-windows10.0.19041.0

# macOS（Mac Catalyst）
dotnet build Praxis/Praxis.csproj -t:Run -f net10.0-maccatalyst -r maccatalyst-arm64 -p:RunWithOpen=false
```

`-t:Run` で起動時エラーになる場合は、以下の手順で起動してください。
```bash
dotnet build Praxis/Praxis.csproj -f net10.0-maccatalyst -r maccatalyst-arm64
open Praxis/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Praxis.app
```

## 補足
このワークスペースでは現在、以下のターゲットで構成しています。
- Windows（`net10.0-windows10.0.19041.0`）
- macOS（Mac Catalyst: `net10.0-maccatalyst`）
