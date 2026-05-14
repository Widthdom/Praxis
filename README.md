# Praxis

> **[日本語はこちら / Japanese](#praxis日本語)**

[![CI](https://github.com/Widthdom/Praxis/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Widthdom/Praxis/actions/workflows/ci.yml)
[![CodeQL](https://img.shields.io/badge/CodeQL-GitHub%20default-2EA44F)](https://github.com/Widthdom/Praxis/security/code-scanning)
[![Delivery](https://img.shields.io/badge/Delivery-tags%20%7C%20manual-2EA44F)](https://github.com/Widthdom/Praxis/actions/workflows/delivery.yml)

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Avalonia](https://img.shields.io/badge/Avalonia-Desktop-8B44AC)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)
![License](https://img.shields.io/badge/License-MIT-blue)

License: MIT (see [`LICENSE`](LICENSE)).

## Overview
Praxis v2 is an Avalonia desktop launcher migration. The app uses strict-MVVM Core models with model-owned launcher button state, a pseudo-acrylic frameless shell, command execution with suggestions, search, free-positioned launcher buttons, a persisted recent Dock, a model-driven status bar, SQLite-backed launcher persistence, launch logging, button editing, drag/multi-select operations, file-backed launcher-button sync, and basic desktop command/default-app execution.

The former .NET MAUI app project has been removed. Existing v1 launcher databases remain readable through the shared data layer: v2 uses the existing `praxis.db3` file when present, also accepts an existing `praxis.db`, and migrates launcher-button schema to version 5. Theme mode switching is available in the Avalonia shell; persisted theme settings and runtime error-log writes remain migration follow-up work.

## Supported Platforms
- Windows: Avalonia desktop on .NET 10
- macOS: Avalonia desktop on .NET 10
- Linux: buildable experimental target for v2.x readiness

Linux support is intentionally kept architecture-ready, but Windows and macOS remain the first v2.0.0 product targets.

## Environment Setup
- Install .NET 10 SDK (`10.x`).
- No MAUI workload is required.
- Restore packages before the first build:

```bash
dotnet restore Praxis.slnx
```

## Quick Start

Run the Avalonia app:

```bash
dotnet run --project Praxis.Avalonia/Praxis.Avalonia.csproj
```

Build and test:

```bash
dotnet build Praxis.slnx -c Debug
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --nologo
dotnet build Praxis.Avalonia/Praxis.Avalonia.csproj -c Release --nologo
```

## Repository Map
- `Praxis.Avalonia/` - Avalonia desktop app, views, converters, behaviors, and app-local services
- `Praxis.Core/` - UI-independent models, records, policies, and service contracts
- `Praxis.Data/` - SQLite entities, storage path resolution, and repository implementations
- `Praxis.Tests/` - xUnit tests for Core policies, v2 model behavior, storage paths, and repository migrations
- `docs/` - developer, testing, database, and branding notes
- `.github/workflows/` - CI and delivery workflows for Avalonia builds

## Documentation
- Developer guide: [`docs/DEVELOPER_GUIDE.md`](docs/DEVELOPER_GUIDE.md)
- Testing guide: [`docs/TESTING_GUIDE.md`](docs/TESTING_GUIDE.md)
- Database status: [`docs/DATABASE_SCHEMA.md`](docs/DATABASE_SCHEMA.md)
- Branding assets: [`docs/branding/README.md`](docs/branding/README.md)

---

# Praxis（日本語）

> **[English / 英語はこちら](#praxis)**

ライセンス: MIT（[`LICENSE`](LICENSE) を参照）。

## 概要
Praxis v2 は Avalonia へのデスクトップランチャー移行版です。strict MVVM の Core model がランチャーボタン状態を所有し、擬似アクリル風のフレームレス shell、候補付き Command 実行、Search、自由配置ボタン、永続化される最近使った Dock、Model 駆動のステータスバー、SQLite 永続化、launch log、ボタン編集、ドラッグ/複数選択操作、ファイルベースの launcher-button 同期、基本的なデスクトップコマンド/既定アプリ起動を持ちます。

旧 .NET MAUI アプリプロジェクトは削除済みです。既存 v1 の launcher DB は共有 data layer から読み込めます。v2 は既存の `praxis.db3` を優先し、既存の `praxis.db` も受け入れ、launcher button schema を version 5 へ移行します。Avalonia shell ではテーマモード切り替えを利用できます。テーマ設定の永続化と runtime error-log 書き込みは今後の移行対象です。

## 対応プラットフォーム
- Windows: .NET 10 上の Avalonia desktop
- macOS: .NET 10 上の Avalonia desktop
- Linux: v2.x 対応準備として build 可能な実験対象

Linux は構成上の余地を残しますが、v2.0.0 の製品対象は Windows / macOS を優先します。

## 開発環境
- .NET 10 SDK（`10.x`）をインストールしてください。
- MAUI workload は不要です。
- 初回ビルド前にパッケージを復元します。

```bash
dotnet restore Praxis.slnx
```

## クイックスタート

Avalonia アプリを起動:

```bash
dotnet run --project Praxis.Avalonia/Praxis.Avalonia.csproj
```

ビルドとテスト:

```bash
dotnet build Praxis.slnx -c Debug
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --nologo
dotnet build Praxis.Avalonia/Praxis.Avalonia.csproj -c Release --nologo
```

## リポジトリ構成
- `Praxis.Avalonia/` - Avalonia デスクトップアプリ、View、Converter、Behavior、アプリ側 service
- `Praxis.Core/` - UI 非依存の Model、record、policy、service contract
- `Praxis.Data/` - SQLite entity、保存先解決、repository 実装
- `Praxis.Tests/` - Core policy、v2 Model、保存先、repository migration の xUnit テスト
- `docs/` - 開発者向け、テスト、DB、ブランディングのメモ
- `.github/workflows/` - Avalonia build 用 CI / delivery workflow

## ドキュメント
- 開発者ガイド: [`docs/DEVELOPER_GUIDE.md`](docs/DEVELOPER_GUIDE.md)
- テストガイド: [`docs/TESTING_GUIDE.md`](docs/TESTING_GUIDE.md)
- DB 状態: [`docs/DATABASE_SCHEMA.md`](docs/DATABASE_SCHEMA.md)
- ブランディング素材: [`docs/branding/README.md`](docs/branding/README.md)
