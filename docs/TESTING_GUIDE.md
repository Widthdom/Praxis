# Testing Guide

## English

### Scope
This guide covers the current Praxis v2 test suite after the Avalonia app replaced the old MAUI app project.

### Test Stack
- Framework: xUnit
- Runner: `Microsoft.NET.Test.Sdk`
- Coverage collector: `coverlet.collector`
- Test project: [`Praxis.Tests/Praxis.Tests.csproj`](../Praxis.Tests/Praxis.Tests.csproj)
- Primary target: `net10.0`

### How To Run
```bash
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --nologo
dotnet build Praxis.Avalonia/Praxis.Avalonia.csproj -c Release --nologo
dotnet build Praxis.slnx -c Debug --nologo
```

### Current Coverage Focus
- `Praxis.Core/Logic`: UI-independent policies and resolvers.
- `Praxis.Core/Models`: v2 Model-owned state and service contracts.
- `Praxis.Data`: storage path selection, `praxis.db3` / `praxis.db` compatibility, SQLite schema migration, v2 launcher field persistence, Dock order, and launch logs.
- `.github/workflows`: CI/delivery guards for Avalonia builds and coverage artifacts.

The former MAUI app-layer linked-source tests were removed with the MAUI project. New Avalonia behavior should be tested by moving reusable state and policy into `Praxis.Core` or `Praxis.Data`, then adding focused xUnit coverage there. View-only behavior should be covered by narrow source guards or UI-level checks when the Avalonia surface becomes stable enough for that.

## 日本語

### 目的
このガイドは、旧 MAUI アプリプロジェクトを Avalonia app に置き換えた後の Praxis v2 テスト構成を説明します。

### テストスタック
- Framework: xUnit
- Runner: `Microsoft.NET.Test.Sdk`
- Coverage collector: `coverlet.collector`
- Test project: [`Praxis.Tests/Praxis.Tests.csproj`](../Praxis.Tests/Praxis.Tests.csproj)
- Primary target: `net10.0`

### 実行方法
```bash
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --nologo
dotnet build Praxis.Avalonia/Praxis.Avalonia.csproj -c Release --nologo
dotnet build Praxis.slnx -c Debug --nologo
```

### 現在のカバレッジ方針
- `Praxis.Core/Logic`: UI 非依存の policy / resolver。
- `Praxis.Core/Models`: v2 の Model 所有状態と service contract。
- `Praxis.Data`: 保存先選択、`praxis.db3` / `praxis.db` 互換、SQLite schema migration、v2 launcher field 永続化、Dock 順、launch log。
- `.github/workflows`: Avalonia build と coverage artifact の workflow guard。

旧 MAUI app-layer の linked-source tests は MAUI プロジェクト削除に合わせて削除しました。新しい Avalonia 挙動は、再利用可能な状態や policy を `Praxis.Core` または `Praxis.Data` へ寄せ、そこで focused xUnit coverage を追加してください。View-only の挙動は、Avalonia surface が安定してから narrow source guard または UI-level check で保護します。
