# Developer Guide

## English

### Scope
This guide describes the current Praxis v2 Avalonia architecture. Test-specific operation is documented in [`TESTING_GUIDE.md`](TESTING_GUIDE.md).

### Tech Stack
- UI/App: Avalonia desktop (`Praxis.Avalonia`)
- Core logic and model state: .NET class library (`Praxis.Core`)
- Persistence: SQLite data layer (`Praxis.Data`)
- Tests: xUnit (`Praxis.Tests`)
- MVVM tooling: `CommunityToolkit.Mvvm`
- Versioning: `version.json` + Nerdbank.GitVersioning

### Architecture
- `Praxis.Core.Models.MainModel` owns command/search text, launcher buttons, recent buttons, and status state.
- `Praxis.Core.Models.LauncherButtonModel` owns per-button state and exposes only UI-independent values.
- `Praxis.Core.Services.ILauncherButtonRepository` keeps Core storage-neutral while allowing the Avalonia app to use SQLite for launcher buttons, Dock order, and launch logs.
- `Praxis.Data.Repositories.SqliteLauncherButtonRepository` owns database schema migration and launcher-button persistence.
- `Praxis.Avalonia.ViewModels.MainWindowViewModel` is intentionally thin. It exposes `MainModel` and commands that delegate to Model operations.
- `Praxis.Avalonia.Views.MainWindow` has no app-specific code-behind logic beyond `InitializeComponent()`.
- Behaviors, converters, themes, and platform services may contain UI infrastructure logic.
- OS-specific command execution belongs behind service contracts such as `ILauncherExecutionService`.

### Project Layout
- `Praxis.Avalonia/Views/` - Avalonia windows and user-facing visual surfaces
- `Praxis.Avalonia/Behaviors/` - UI infrastructure behaviors such as frameless-window drag support
- `Praxis.Avalonia/Converters/` - UI-only value conversion from Core enum values to Avalonia types
- `Praxis.Avalonia/Themes/` - pseudo-acrylic dark theme resources
- `Praxis.Avalonia/Services/` - app-local desktop services such as command/default-app execution
- `Praxis.Core/Models/` - UI-independent state models and records
- `Praxis.Core/Logic/` - pure policy and resolver logic
- `Praxis.Core/Services/` - platform/service contracts
- `Praxis.Data/Entities/` - SQLite table entities compatible with v1 table names
- `Praxis.Data/Repositories/` - SQLite repository implementations
- `Praxis.Data/Storage/` - platform-aware app data path resolution

### Current Migration State
The Avalonia app loads launcher buttons from SQLite at startup, executes registered commands from the top command field, maintains command suggestions from loaded launcher records, writes newly created/executed/moved/deleted launcher records back to SQLite, persists recent Dock order, writes launch logs, and uses a desktop execution service for direct commands or default-app opening. The former MAUI app project has been removed. Button editing UI, drag UI wiring, theme settings, error logging, and cross-window sync remain migration follow-up work and should be reintroduced behind shared service abstractions rather than view code.

The data layer keeps compatibility with existing `praxis.db3` files and also opens an existing `praxis.db` file in the same app data directory. Schema migration currently advances launcher databases to `PRAGMA user_version = 5`.

### Build
```bash
dotnet restore Praxis.slnx
dotnet build Praxis.slnx -c Debug
dotnet build Praxis.Avalonia/Praxis.Avalonia.csproj -c Release --nologo
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --nologo
```

## 日本語

### 目的
このガイドは現在の Praxis v2 Avalonia 構成を説明します。テスト固有の手順は [`TESTING_GUIDE.md`](TESTING_GUIDE.md) を参照してください。

### 技術スタック
- UI/App: Avalonia desktop (`Praxis.Avalonia`)
- Core logic / model state: .NET class library (`Praxis.Core`)
- Persistence: SQLite data layer (`Praxis.Data`)
- Tests: xUnit (`Praxis.Tests`)
- MVVM tooling: `CommunityToolkit.Mvvm`
- Versioning: `version.json` + Nerdbank.GitVersioning

### アーキテクチャ
- `Praxis.Core.Models.MainModel` が command/search text、launcher button、recent button、status state を所有します。
- `Praxis.Core.Models.LauncherButtonModel` はボタンごとの状態を持ち、UI 非依存の値だけを公開します。
- `Praxis.Core.Services.ILauncherButtonRepository` により、Core は storage-neutral のまま Avalonia app から launcher button、Dock 順、launch log 用の SQLite を使えます。
- `Praxis.Data.Repositories.SqliteLauncherButtonRepository` が DB schema migration と launcher button 永続化を担当します。
- `Praxis.Avalonia.ViewModels.MainWindowViewModel` は薄く保ち、`MainModel` と Model 操作へ委譲する command だけを公開します。
- `Praxis.Avalonia.Views.MainWindow` の code-behind は `InitializeComponent()` に留めます。
- Behavior、Converter、Theme、Platform service は UI infrastructure logic を持ってよいです。
- OS 別のコマンド実行は `ILauncherExecutionService` などの service contract の背後に置きます。

### プロジェクト構成
- `Praxis.Avalonia/Views/` - Avalonia window と画面
- `Praxis.Avalonia/Behaviors/` - フレームレスウィンドウのドラッグなどの UI infrastructure behavior
- `Praxis.Avalonia/Converters/` - Core enum から Avalonia 型への UI 専用変換
- `Praxis.Avalonia/Themes/` - 擬似アクリル風ダークテーマ
- `Praxis.Avalonia/Services/` - コマンド/既定アプリ起動などの app-local desktop service
- `Praxis.Core/Models/` - UI 非依存の状態 Model / record
- `Praxis.Core/Logic/` - 純粋な policy / resolver
- `Praxis.Core/Services/` - platform/service contract
- `Praxis.Data/Entities/` - v1 table 名と互換の SQLite entity
- `Praxis.Data/Repositories/` - SQLite repository 実装
- `Praxis.Data/Storage/` - platform-aware な app data path 解決

### 現在の移行状態
Avalonia アプリは起動時に SQLite から launcher button を読み込み、上部 command field から登録済み command を実行し、読み込んだ launcher record から command suggestion を作り、作成/実行/移動/削除した launcher record を SQLite に書き戻し、最近使った Dock 順と launch log を保存し、desktop execution service で直接コマンドまたは既定アプリ起動を行います。旧 MAUI アプリプロジェクトは削除済みです。ボタン編集 UI、ドラッグ UI wiring、テーマ設定、error logging、複数ウィンドウ同期は今後の移植対象であり、View ではなく共有 service abstraction の背後に戻します。

data layer は既存の `praxis.db3` と互換で、同じ app data directory に既存の `praxis.db` がある場合も開きます。現在の schema migration は launcher DB を `PRAGMA user_version = 5` へ進めます。

### ビルド
```bash
dotnet restore Praxis.slnx
dotnet build Praxis.slnx -c Debug
dotnet build Praxis.Avalonia/Praxis.Avalonia.csproj -c Release --nologo
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --nologo
```
