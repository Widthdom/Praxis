# AGENT_GUIDE.md

This file is the shared source of truth for AI-agent instructions in this repository.
`AGENTS.md` and `CLAUDE.md` are thin entry points that defer here.

## English

### Project Overview

Praxis v2 is an Avalonia desktop launcher migration.
The former .NET MAUI app project has been removed. The current app uses strict-MVVM Core models, a pseudo-acrylic frameless Avalonia UI, command-input execution with suggestions, SQLite-backed launcher-button/Dock persistence through `Praxis.Data`, launch logs, and an app-local desktop command/default-app execution service.

Repository structure:

- `Praxis.Avalonia/` - Avalonia app, views, behaviors, converters, theme, and app-local services
- `Praxis.Core/` - UI-independent models, records, policies, and service contracts
- `Praxis.Data/` - SQLite entities, storage path resolution, and repository implementations
- `Praxis.Tests/` - xUnit tests for Core logic, v2 models, storage/repository compatibility, and workflow policy guards
- `docs/` - bilingual developer/testing/database/migration documentation
- `.github/workflows/` - CI and delivery workflows for Avalonia builds

### Architecture Rules

1. Keep app state in Core models. `MainModel` owns command/search text, launcher buttons, recent buttons, and status state.
2. Keep ViewModels thin. Avalonia ViewModels should expose Models and commands that delegate to Model operations.
3. Keep View code-behind free of app-specific logic. `InitializeComponent()` is acceptable; app logic belongs in Model, service, behavior, converter, or command layers.
4. Keep Core UI-independent. Core models must not expose Avalonia, MAUI, window, control, brush, thickness, process, or SQLite types.
5. Put OS-specific behavior behind services. Command execution, default-app opening, paths, dialogs, clipboard, window operations, and future Linux support must remain separable.
6. Keep documentation and tests aligned with behavior changes.

### Build & Test

```bash
dotnet restore Praxis.slnx
dotnet build Praxis.slnx -c Debug --nologo
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --nologo
dotnet build Praxis.Avalonia/Praxis.Avalonia.csproj -c Release --nologo
```

Run the app:

```bash
dotnet run --project Praxis.Avalonia/Praxis.Avalonia.csproj
```

No MAUI workload is required.

### Code Search Rules

This repository uses `cdidx` for fast code search via `.cdidx/codeindex.db`.
Use `cdidx` before falling back to shell search.

Check freshness before searches where correctness matters:

```bash
cdidx status --check --json
```

If the index is stale, update it:

```bash
cdidx .
```

After editing source files, run `cdidx status --check --json` before the next search. If it reports a mismatch, run `cdidx .` or a scoped update.

Useful commands:

```bash
cdidx map --json
cdidx files --path Praxis.Avalonia
cdidx files --path Praxis.Core
cdidx search "MainModel" --path Praxis.Core --snippet-lines 6 --max-line-width 160
cdidx symbols --lang csharp --name MainModel --exact
cdidx outline Praxis.Avalonia/Views/MainWindow.axaml --json
cdidx validate
```

### Cross-Cutting Consistency

- User-visible behavior, platform support, setup steps, or startup changes: update `README.md`, `CHANGELOG.md`, and relevant docs/tests.
- Core model/service contract changes: update focused tests in `Praxis.Tests`.
- Workflow changes: update `.github/workflows/*.yml` and `Praxis.Tests/CiCoverageWorkflowPolicyTests.cs`.
- Database/storage changes: update `docs/DATABASE_SCHEMA.md` and migration/storage tests.
- Agent workflow changes: update `AGENT_GUIDE.md`.

### Git & Release Rules

- Do not create tags or publish packages.
- Do not commit unless the user asks or explicitly allows it for the current task.
- Do not use `git add .` or `git add -A`; add explicit files only.
- Do not revert user changes unless explicitly requested.

## 日本語

### プロジェクト概要

Praxis v2 は Avalonia デスクトップランチャーへの移行版です。
旧 .NET MAUI アプリプロジェクトは削除済みです。現在のアプリは、strict MVVM の Core model、擬似アクリル風フレームレス Avalonia UI、候補付き command input 実行、`Praxis.Data` 経由の SQLite launcher-button / Dock 永続化、launch log、app-local な desktop command / 既定アプリ起動 service を持ちます。

リポジトリ構成:

- `Praxis.Avalonia/` - Avalonia アプリ、View、Behavior、Converter、Theme、アプリ側 service
- `Praxis.Core/` - UI 非依存の Model、record、policy、service contract
- `Praxis.Data/` - SQLite entity、保存先解決、repository 実装
- `Praxis.Tests/` - Core logic、v2 model、storage/repository 互換、workflow policy guard の xUnit テスト
- `docs/` - 英日併記の開発者向け/テスト/DB/移行ドキュメント
- `.github/workflows/` - Avalonia build 用 CI / delivery workflow

### アーキテクチャルール

1. アプリ状態は Core model に置きます。`MainModel` が command/search text、launcher button、recent button、status state を所有します。
2. ViewModel は薄く保ちます。Avalonia ViewModel は Model と、Model 操作へ委譲する command を公開します。
3. View code-behind にアプリ固有ロジックを書きません。`InitializeComponent()` は許容し、アプリロジックは Model / service / behavior / converter / command 層へ置きます。
4. Core は UI 非依存に保ちます。Core model は Avalonia、MAUI、window、control、brush、thickness、process、SQLite 型を公開しません。
5. OS 依存処理は service の背後に置きます。コマンド実行、既定アプリ起動、path、dialog、clipboard、window 操作、将来 Linux 対応を分離可能にします。
6. 挙動変更に合わせて docs と tests を更新します。

### ビルドとテスト

```bash
dotnet restore Praxis.slnx
dotnet build Praxis.slnx -c Debug --nologo
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --nologo
dotnet build Praxis.Avalonia/Praxis.Avalonia.csproj -c Release --nologo
```

アプリ起動:

```bash
dotnet run --project Praxis.Avalonia/Praxis.Avalonia.csproj
```

MAUI workload は不要です。

### コード検索ルール

このリポジトリでは `.cdidx/codeindex.db` を使う `cdidx` を優先します。
shell search に戻る前に `cdidx` を使ってください。

検索前に freshness を確認します。

```bash
cdidx status --check --json
```

古い場合は更新します。

```bash
cdidx .
```

source file 変更後、次の検索前に `cdidx status --check --json` を実行し、mismatch なら `cdidx .` または scoped update を実行してください。

### 横断的な整合

- ユーザー向け挙動、platform support、setup、startup 変更: `README.md`、`CHANGELOG.md`、関連 docs/tests を更新します。
- Core model / service contract 変更: `Praxis.Tests` に focused test を追加/更新します。
- workflow 変更: `.github/workflows/*.yml` と `Praxis.Tests/CiCoverageWorkflowPolicyTests.cs` を更新します。
- DB/storage 変更: `docs/DATABASE_SCHEMA.md` と migration/storage tests を更新します。
- agent workflow 変更: `AGENT_GUIDE.md` を更新します。

### Git / Release ルール

- tag 作成や package publish はしません。
- ユーザーが求めるか、その作業で明示的に許可した場合を除き、commit しません。
- `git add .` / `git add -A` は使わず、明示ファイルだけを追加します。
- ユーザー変更を明示指示なしに revert しません。
