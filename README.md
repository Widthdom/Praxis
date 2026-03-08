# Praxis

License: MIT (see `LICENSE`).

## Overview
Praxis is a desktop launcher app built with .NET MAUI.
It stores launcher buttons in SQLite and executes commands with arguments.

## User Features
- Execute from the command input with `Enter`
- Keep command inputs (top bar + edit modal `Command`) in alphanumeric mode on macOS/Windows
  - Windows compromise: IME is forced toward ASCII once on focus acquisition (not continuously), to keep native text Undo/Redo behavior stable.
- Search and filter launcher buttons quickly
- Drag, multi-select, edit, and delete buttons in the placement area
- Undo/Redo for recent button mutations (move/edit/delete):
  - macOS: `Command+Z` / `Command+Shift+Z`
  - Windows: `Ctrl+Z` / `Ctrl+Y`
- Show recent launches in Dock (restored on next startup)
- Show Dock horizontal scrollbar only while hovering the Dock area and horizontal overflow exists
- Create buttons from the top Create button or right-clicking empty placement area
- Keyboard-friendly suggestions and modal operations
- Quick Look preview on button hover (`Command` / `Tool` / `Arguments` / `Clip Word` / `Note`)
- Persisted theme mode (`Light` / `Dark` / `System`)
- Cross-window sync for button changes, Dock order, and theme mode
- Fallback launch behavior when `Tool` is empty:
  - HTTP(S) URL: open in default browser
  - File path: open with associated app
  - Directory path: open in file manager

## Supported Platforms
- Windows: `net10.0-windows10.0.19041.0`
- macOS (Mac Catalyst): `net10.0-maccatalyst`

## Quick Start (Developers)
```bash
dotnet test Praxis.slnx
```

Platform-specific run/build examples:
```bash
# Windows
dotnet run --project Praxis/Praxis.csproj -f net10.0-windows10.0.19041.0

# macOS (Mac Catalyst)
dotnet build Praxis/Praxis.csproj -t:Run -f net10.0-maccatalyst -r maccatalyst-arm64 -p:RunWithOpen=false
```

If `-t:Run` launch fails on macOS:
```bash
dotnet build Praxis/Praxis.csproj -f net10.0-maccatalyst -r maccatalyst-arm64
open Praxis/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Praxis.app
```

## Documentation Map
- Implementation details: `docs/DEVELOPER_GUIDE.md`
- Refactoring notes: `MainPage` / `MainViewModel` are split into feature-based partial classes, and UI delay constants are centralized in `UiTimingPolicy` (see Developer Guide).
- Testing guide (execution, coverage, test inventory): `docs/TESTING_GUIDE.md`
- SQLite schema: `docs/DATABASE_SCHEMA.md`
- Branding assets: `docs/branding/README.md`

---

# Praxis（日本語）

ライセンス: MIT（`LICENSE` を参照）。

## 概要
Praxis は .NET MAUI で実装したデスクトップ向けランチャーです。
ランチャーボタンを SQLite に保存し、コマンドと引数を実行します。

## 主な機能（ユーザー向け）
- コマンド入力欄で `Enter` 実行
- コマンド入力欄（上部 + 編集モーダル `Command`）を macOS/Windows で英字入力モードに維持
  - Windows の妥協点: IME の英字入力寄せはフォーカス取得時の 1 回のみ（常時再強制しない）。テキストの Undo/Redo 粒度を崩さないため。
- ボタンの高速検索・絞り込み
- 配置領域でのドラッグ、複数選択、編集、削除
- 直近のボタン変更（移動/編集/削除）の Undo/Redo:
  - macOS: `Command+Z` / `Command+Shift+Z`
  - Windows: `Ctrl+Z` / `Ctrl+Y`
- 実行履歴の Dock 表示（次回起動時に復元）
- Dock の横スクロールバーは Dock 領域ホバー中かつ横オーバーフロー時のみ表示
- 上部 Create ボタンと配置領域の空きスペース右クリックから新規作成
- 候補一覧とモーダル操作のキーボード対応
- ボタンホバー時の Quick Look プレビュー（`Command` / `Tool` / `Arguments` / `Clip Word` / `Note`）
- テーマモード（`Light` / `Dark` / `System`）の保存・復元
- ボタン変更、Dock 順序、テーマの複数ウィンドウ同期
- `Tool` が空の場合のフォールバック起動:
  - HTTP(S) URL: 既定ブラウザ
  - ファイルパス: 関連付けアプリ
  - ディレクトリパス: ファイルマネージャ

## 対応プラットフォーム
- Windows: `net10.0-windows10.0.19041.0`
- macOS（Mac Catalyst）: `net10.0-maccatalyst`

## 開発クイックスタート
```bash
dotnet test Praxis.slnx
```

プラットフォーム別の実行 / ビルド例:
```bash
# Windows
dotnet run --project Praxis/Praxis.csproj -f net10.0-windows10.0.19041.0

# macOS（Mac Catalyst）
dotnet build Praxis/Praxis.csproj -t:Run -f net10.0-maccatalyst -r maccatalyst-arm64 -p:RunWithOpen=false
```

macOS で `-t:Run` の起動が失敗する場合:
```bash
dotnet build Praxis/Praxis.csproj -f net10.0-maccatalyst -r maccatalyst-arm64
open Praxis/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Praxis.app
```

## ドキュメント一覧
- 実装仕様: `docs/DEVELOPER_GUIDE.md`
- リファクタ方針: `MainPage` / `MainViewModel` は機能別 partial class に分割し、UI 遅延定数は `UiTimingPolicy` に集約（詳細は開発者ガイド参照）
- テストガイド（実行手順・カバレッジ・テスト一覧）: `docs/TESTING_GUIDE.md`
- SQLite スキーマ: `docs/DATABASE_SCHEMA.md`
- ブランディング素材: `docs/branding/README.md`
