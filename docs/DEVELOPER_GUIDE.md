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

### macOS Frameless Window Notes
Praxis v2 uses a frameless Avalonia window with custom caption buttons on macOS. Most of the implementation lives in `Praxis.Avalonia/Behaviors/WindowDragBehavior.cs` and `Praxis.Avalonia/Behaviors/MainWindowInteractionBehavior.cs`, because code-behind must stay free of app-specific logic.

The current macOS window behavior is the result of several attempted approaches:

1. Manual working-area maximize by setting `Position`, `Width`, and `Height` directly.
   This made double-click maximize appear to work at first, but it created fragile edge cases. After maximizing, manual resize could collapse the window into a very narrow shape or corrupt the shell bounds. Returning from pseudo-maximized states was also unreliable when the window was later moved or resized.
2. Small-offset manual resize after pseudo-maximize.
   We tried nudging the window a few pixels inside the working area before starting a resize. This did not fix the core issue because the window was still treated as a hand-built maximized rectangle, and resize math remained sensitive to display scale, grip direction, and the exact edge being dragged.
3. Post-resize clamping to the current working area.
   Clamping `Position`, `Width`, and `Height` after resize looked conservative, but it did not prevent the broken intermediate states. The bad geometry was already visible during the resize interaction.
4. One-shot suppression of the next snap after resize.
   This addressed a symptom where a window could snap back to maximized after being moved, but it did not solve the maximize/resize geometry problem and risked interfering with otherwise-good double-click behavior.
5. Native `WindowState.Maximized` for normal maximize, plus explicit restore bounds.
   This is the adopted approach. Title-bar double-click and top-edge snap both enter `WindowState.Maximized` instead of synthesizing a maximized rectangle. Before maximizing, the behavior captures normal bounds. Restoring leaves `WindowState.Maximized`, then restores the captured normal bounds through `RestoreWindowBoundsWithinCurrentScreen`. This proved stable for repeated maximize/restore cycles, top-edge snap restore, and resizing after maximize.

Implementation rules that came out of this work:

- Do not emulate normal maximization on macOS by writing working-area `Position`, `Width`, and `Height` directly.
- Keep macOS FullScreen separate from normal maximize. The green caption button enters `WindowState.FullScreen`; title-bar double-click and top-edge snap use normal maximize.
- Before normal maximize, capture restore bounds while the window is still normal.
- When leaving normal maximize, restore captured bounds explicitly and clear stale restore state.
- Snap-to-top should only maximize when the pointer is at the top edge. A window that merely still touches the top edge after a resize or drag must not auto-maximize on release.
- Manual resize should clear normal maximize restore state, because resizing means the user is defining a new normal window geometry.

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

### macOS フレームレスウィンドウの知見
Praxis v2 は macOS でフレームレス Avalonia window と独自 caption button を使います。code-behind にアプリ固有ロジックを書かない方針のため、実装の大半は `Praxis.Avalonia/Behaviors/WindowDragBehavior.cs` と `Praxis.Avalonia/Behaviors/MainWindowInteractionBehavior.cs` に置きます。

現在の macOS window 挙動は、次の試行錯誤を経て決めたものです。

1. `Position`、`Width`、`Height` を直接設定する working area 擬似最大化。
   最初はダブルクリック最大化が動いているように見えましたが、境界条件が脆くなりました。最大化後に手動リサイズすると、ウィンドウが極端に細くなったり、shell bounds が壊れたりしました。擬似最大化後に移動やリサイズを挟むと、元のサイズへの復帰も不安定でした。
2. 擬似最大化後の手動リサイズ開始時に数 px だけ内側へずらす案。
   working area 境界から少し外してからリサイズすれば安定するか試しました。しかし、根本的には手作業で作った最大化矩形のままであり、display scale、つかんだ辺、角の種類にリサイズ計算が影響され続けるため、解決にはなりませんでした。
3. リサイズ後に current working area へ clamp する案。
   `Position`、`Width`、`Height` をリサイズ後に制限するのは保守的に見えましたが、壊れた中間状態がリサイズ操作中にすでに見えてしまうため、不十分でした。
4. リサイズ後の次回 snap だけを抑止する案。
   リサイズ後にウィンドウを少し動かして放すと再最大化する症状には関係しましたが、最大化/リサイズ時の geometry 崩れは直せませんでした。また、正常に動いていたダブルクリック挙動へ干渉するリスクがありました。
5. 通常最大化は native の `WindowState.Maximized` を使い、復帰矩形だけ明示管理する案。
   これを採用しています。タイトルバーダブルクリックと上辺 snap は、working area へ `Position`/`Width`/`Height` を直接貼り付けるのではなく `WindowState.Maximized` に入ります。最大化前に通常時の bounds を保存し、復帰時は `WindowState.Maximized` を解除してから `RestoreWindowBoundsWithinCurrentScreen` で保存済み bounds へ戻します。この方式で、最大化/復帰の反復、上辺 snap からの復帰、最大化後のリサイズが安定しました。

この作業から得た実装ルール:

- macOS の通常最大化を、working area の `Position`、`Width`、`Height` 直書きで再現しない。
- macOS FullScreen と通常最大化は分ける。緑 caption button は `WindowState.FullScreen`、タイトルバーダブルクリックと上辺 snap は通常最大化を使う。
- 通常最大化に入る前に、window が通常状態のうちに復帰用 bounds を保存する。
- 通常最大化から戻るときは、最大化状態を解除したうえで保存済み bounds を明示復元し、古い復帰状態を消す。
- 上辺 snap は pointer が上端にある場合だけ通常最大化する。リサイズやドラッグ後に window の上辺がたまたま画面上端に残っているだけでは、放した瞬間に再最大化してはいけない。
- 手動リサイズはユーザーが新しい通常サイズを定義する操作なので、通常最大化の復帰状態をクリアする。

### ビルド
```bash
dotnet restore Praxis.slnx
dotnet build Praxis.slnx -c Debug
dotnet build Praxis.Avalonia/Praxis.Avalonia.csproj -c Release --nologo
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release --nologo
```
