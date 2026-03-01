# Praxis

License: MIT (see `LICENSE`).

## Overview
Praxis is a desktop launcher app built with .NET MAUI and an MVVM-centered architecture.
It stores launcher buttons in SQLite and can execute tools with arguments.

## Features (User)
- Enter-to-run from the command box
- Search and filter launcher buttons quickly
- Drag, multi-select, edit, and delete buttons in the placement area
- Dock history for recently clicked buttons (restored on next launch)
- Create new buttons from top-bar create icon or right-click in an empty region of the button placement area
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
- Clicking a suggestion fills the command box and executes the suggestion immediately.
- Right-clicking a launcher button to open the context menu closes the command suggestion popup, removes focus from the command input, and moves target focus to `Edit`.
- Enter from command box executes all buttons whose `command` exactly matches input (case-insensitive, trim-aware).
- When the app window becomes active, if editor modal and conflict dialog are both closed, focus moves to command box and its text is selected (Windows/macOS).
- On macOS, Search input uses a focus guard: non-user-initiated focus attempts are rejected so activation-time focus stays on command input.
- Editor modal keyboard behavior:
  - `Tab`/`Shift+Tab` stays inside modal controls and wraps at edges.
  - On macOS, `Shift+Tab` from `GUID` stays in the modal focus ring (does not escape to main page).
  - On macOS, pressing `Tab`/`Shift+Tab` in `Clip Word`/`Note` moves focus to next/previous control (no literal tab insertion).
  - On macOS, pressing `Esc` while focused in any modal field (including `Clip Word`/`Note`) closes the modal in a single press.
  - On macOS, modal editor key command registration is nullable-safe (`KeyCommands` override returns non-null).
  - On Windows, `Tab`/`Shift+Tab` navigation into modal text inputs automatically selects all text (pointer-focus does not trigger this).
  - On macOS, `GUID` remains read-only/selectable (not editable).
  - On macOS, when the editor modal opens, `Command` keeps caret at the end (no select-all on open).
  - On macOS, when pseudo-focus is on `Cancel`/`Save`, `Enter` triggers that action.
  - Copy buttons are vertically centered with each field; for multiline `Clip Word`/`Note`, copy buttons expand to the same height as the field and shrink back when content is cleared.
  - The editor modal field area is auto-sized (not `*`-filled), so once expanded it also shrinks back without leaving stale blank space.
- Context menu keyboard behavior:
  - `Up`/`Down` moves focus between `Edit` and `Delete` and wraps.
  - `Tab`/`Shift+Tab` moves focus between `Edit` and `Delete` and wraps.
  - `Enter` executes the currently focused action (`Edit` or `Delete`).
  - Focus visual is rendered as a single custom border.
- Conflict dialog keyboard behavior (`Reload latest` / `Overwrite mine` / `Cancel`):
  - On open, initial focus is moved to `Cancel`.
  - `Cancel` focus is visually emphasized with a single custom border.
  - `Left` moves to the previous action and `Right` moves to the next action (with wrap).
  - `Tab` moves actions left-to-right, `Shift+Tab` moves right-to-left (both with wrap), and focus stays inside the dialog.
  - `Enter` executes the currently focused dialog action.
  - On close (for example `Cancel` / `Reload latest`), focus returns to editor `Command` so `Esc` / `Ctrl+S` remain active on Windows.
  - While conflict dialog is open, focus does not move to the underlying button-editor modal.
- In editor modal, `Clip Word` is multiline like `Note`.
- In Windows, pressing `Enter` in `Clip Word` / `Note` expands each field height line-by-line (including `CRLF` line endings).
- In Windows Dark theme, `Clip Word` / `Note` text color follows theme-aware modal text color (same readable contrast policy as other editor inputs).
- On Windows, `Clip Word` / `Note` also enables vertical scrolling with `Auto` scrollbars at multiline overflow.
- Right-clicking an empty region in the button placement area opens the create modal at cursor position.
- Starting a new button (top create button or right-click in empty button placement area) clears search box.
- Dragging uses 10px snap; multi-select is supported with rectangle and modifier click.
- If `tool` is empty, `Arguments` falls back to URL/path launch behavior.
- Theme shortcuts:
  - Windows: `Ctrl+Shift+L` / `Ctrl+Shift+D` / `Ctrl+Shift+H`
  - macOS: `Command+Shift+L` / `Command+Shift+D` / `Command+Shift+H`
  - On macOS, theme shortcuts are handled at app level and remain available regardless of modal/context-menu state.
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
In some environments, direct app-binary launch can fail initial scene creation; the app relays direct launch to `open` internally.

## Notes
Current app targets in this workspace:
- Windows (`net10.0-windows10.0.19041.0`)
- macOS via Mac Catalyst (`net10.0-maccatalyst`)

Storage paths:
- Windows DB: `%USERPROFILE%/AppData/Local/Praxis/praxis.db3`
- macOS (Mac Catalyst) DB: `~/Library/Application Support/Praxis/praxis.db3`
- Sync signal: `~/Library/Application Support/Praxis/buttons.sync` (Windows: `%USERPROFILE%/AppData/Local/Praxis/buttons.sync`)

---

# Praxis（日本語）

ライセンス: MIT（`LICENSE` を参照）。

## 概要
Praxis は .NET MAUI と MVVM ベースの構成で実装したデスクトップ向けランチャーアプリです。
SQLite にボタン情報を保存し、ツールと引数を実行できます。

## 主な機能（ユーザー向け）
- コマンド欄で Enter 実行
- 検索でボタンを素早く絞り込み
- ボタン配置領域でドラッグ・複数選択・編集・削除
- クリック履歴を Dock に表示し、次回起動時に復元
- 上部 Create アイコンとボタン配置領域の空きスペース右クリックから新規ボタン作成
- 候補一覧の上下キー選択やモーダル保存ショートカットなどのキーボード操作
- テーマ切替（ライト / ダーク / システム）と起動時復元
- ステータス表示とコピー通知
- アイコン表示はプラットフォーム別フォールバック対応（Windows は Segoe MDL2、macOS は互換シンボル）
- `tool` が空でも、`Arguments` が HTTP(S) URL ならブラウザ起動、ファイルパスなら既定関連付けアプリ（例: `.pdf`）、フォルダパスなら Finder/Explorer 起動
- 複数ウィンドウ同期: ボタンの追加/削除/更新（座標変更を含む）と Dock 並び変更が、開いている他ウィンドウにも反映される
- 複数ウィンドウ同期で、テーマ変更（ライト / ダーク / システム）も他ウィンドウへ反映される

## 動作メモ
- command 部分一致で候補を表示し、キーボードで選択実行できます。
- 候補一覧のキーボード操作は `↑/↓` で移動、端で循環（先頭で↑→末尾、末尾で↓→先頭）、`Enter` で実行です。
- 候補一覧をクリックすると、Command欄に自動入力した上でその候補を即時実行します。
- ボタンを右クリックしてコンテキストメニューを開いたときは、Command 候補一覧を閉じ、Command 欄からフォーカスを外して `Edit` にフォーカスを移します。
- 候補一覧の描画は Windows / macOS で同一の行レイアウト（3列: `Command` / `ButtonText` / `Tool Arguments`）を使用します。
- コマンド欄で `Enter` 実行したとき、`command` 完全一致（前後空白除去・大文字小文字非依存）のボタンが複数あれば全件実行します。
- アプリウィンドウがアクティブになったとき、編集モーダルと競合ダイアログが閉じていれば Command 欄へフォーカスし、既存文字列を全選択します（Windows/macOS）。
- macOS の Search 欄にはフォーカスガードがあり、ユーザー起点でないフォーカス遷移は拒否して、アクティブ化直後の Command フォーカス維持を優先します。
- 編集モーダルのキーボード操作:
  - `Tab` / `Shift+Tab` はモーダル内のみで循環し、端でラップします。
  - macOS では `GUID` 欄で `Shift+Tab` を押しても、メイン画面へは抜けずモーダル内フォーカス循環を維持します。
  - macOS では `Clip Word` / `Note` 欄で `Tab` / `Shift+Tab` を押すと、タブ文字は入力せず前後フォーカス遷移します。
  - macOS ではモーダル内の任意入力欄（`Clip Word` / `Note` を含む）にフォーカス中でも、`Esc` 1 回でモーダルを閉じます。
  - macOS のモーダル編集キーコマンド登録は、`KeyCommands` オーバーライドを non-null 戻り値で実装して nullable 警告を回避しています。
  - Windows では `Tab`/`Shift+Tab` で遷移した入力欄のテキストを自動で全選択します（マウスフォーカス時は対象外）。
  - macOS では `GUID` 欄は選択可能ですが編集不可です。
  - macOS では編集モーダル表示時、`Command` 欄は全選択せずキャレットを末尾に配置します。
  - macOS では `Cancel` / `Save` の擬似フォーカス時に `Enter` で該当アクションを実行します。
  - コピーアイコンボタンは各入力欄に対して縦中央揃えにし、`Clip Word` / `Note` の複数行拡張時は入力欄と同じ高さに追従し、空欄化すると高さも元に戻ります。
  - 編集モーダルの項目領域は `*` 伸長ではなく自動高さでレイアウトし、一度最大まで広がった後でも不要な空白を残さず縮みます。
- コンテキストメニューのキーボード操作:
  - `↑` / `↓` で `Edit` と `Delete` 間を循環します。
  - `Tab` / `Shift+Tab` で `Edit` と `Delete` 間を循環します。
  - `Enter` で現在フォーカス中のアクション（`Edit` または `Delete`）を実行します。
  - フォーカス表示は単一のカスタム枠線で表示します。
- 競合ダイアログ（`Reload latest` / `Overwrite mine` / `Cancel`）のキーボード操作:
  - ダイアログ表示時は初期フォーカスを `Cancel` に移します。
  - `Cancel` のフォーカスは単一のカスタム枠線で強調表示します。
  - `←` は前のアクション、`→` は次のアクションへ移動（どちらも端でラップ）します。
  - `Tab` は左から右、`Shift+Tab` は右から左に循環（どちらも端でラップ）し、フォーカスはダイアログ内に留まります。
  - `Enter` で現在フォーカス中のダイアログアクションを実行します。
  - `Cancel` / `Reload latest` などで閉じた直後は、編集モーダルの `Command` にフォーカスを戻し、Windows でも `Esc` / `Ctrl+S` を継続して有効にします。
  - 競合ダイアログ表示中は、背面のボタン編集モーダルへフォーカスが移りません。
- 編集モーダルの `Clip Word` は `Note` と同様に複数行入力に対応しています。
- Windows では `Clip Word` / `Note` で `Enter` 改行すると、行数に応じて入力欄高さが順次拡張されます（`CRLF` 改行を含む）。
- Windows のダークテーマでは、`Clip Word` / `Note` の文字色も他の編集入力欄と同じ可読性ポリシーでテーマ連動します。
- Windows では `Clip Word` / `Note` の複数行あふれ時に、縦スクロール（`Auto` スクロールバー）を有効化しています。
- ボタン配置領域の空きスペース右クリックで、その座標に新規作成モーダルを開きます。
- 新規作成開始時（上部 Create ボタン / ボタン配置領域の空きスペース右クリック）に検索欄はクリアされます。
- ドラッグは 10px スナップ、矩形選択と修飾キー選択に対応しています。
- `tool` が空の場合は `Arguments` を URL/パスとしてフォールバック起動します。
- テーマ切替ショートカット:
  - Windows: `Ctrl+Shift+L` / `Ctrl+Shift+D` / `Ctrl+Shift+H`
  - macOS: `Command+Shift+L` / `Command+Shift+D` / `Command+Shift+H`
  - macOS ではアプリレベルで処理するため、モーダル/コンテキストメニュー表示状態に関係なく有効です。
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
環境によってはアプリ本体の直実行で初期シーン生成に失敗するため、アプリ側で直実行を検出した場合は内部的に `open` 経由へリレーします。

## 補足
このワークスペースでは現在、以下のターゲットで構成しています。
- Windows（`net10.0-windows10.0.19041.0`）
- macOS（Mac Catalyst: `net10.0-maccatalyst`）

ストレージパス:
- Windows の DB: `%USERPROFILE%/AppData/Local/Praxis/praxis.db3`
- macOS（Mac Catalyst）の DB: `~/Library/Application Support/Praxis/praxis.db3`
- 同期シグナル: `~/Library/Application Support/Praxis/buttons.sync`（Windows: `%USERPROFILE%/AppData/Local/Praxis/buttons.sync`）
