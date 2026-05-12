# Praxis v2.0.0 Avalonia Migration Plan

## English

### 1. Current State Summary

Praxis v2 now starts from an Avalonia desktop app in `Praxis.Avalonia/Praxis.Avalonia.csproj`. The former .NET MAUI project has been removed, and the current runtime surface is a strict-MVVM Avalonia app with model-owned launcher button state, a pseudo-acrylic frameless shell, command input with suggestions, SQLite-backed launcher-button/Dock persistence, launch logs, and basic desktop command/default-app execution.

Important current responsibilities:

- `Praxis.Avalonia/Views/MainWindow.axaml`: frameless Avalonia shell, top command/search/create row, Canvas-backed button area, Dock, and status bar.
- `Praxis.Avalonia/ViewModels/MainWindowViewModel.cs`: thin command adapter over `MainModel`.
- `Praxis.Core/Models/MainModel.cs`: command/search text, launcher buttons, recent buttons, and status state.
- `Praxis.Core/Services/ILauncherExecutionService.cs`: first platform-service seam for command execution.
- `Praxis.Core/Services/ILauncherButtonRepository.cs`: storage-neutral launcher-button persistence seam.
- `Praxis.Data/Repositories/SqliteLauncherButtonRepository.cs`: SQLite schema migration and launcher-button persistence.
- `Praxis.Core/Logic/*`: reusable policies and pure helpers retained from v1 where still UI-independent.

SQLite launcher-button persistence, command input execution, recent Dock persistence, launch logs, and basic command execution are wired into the Avalonia runtime. Button editing UI, drag UI wiring, error logging, theme settings, and cross-window sync should continue coming back through shared service/data projects rather than a new UI-specific monolith.

### 2. Migration Plan

1. Keep the Avalonia app as the only desktop app project.
2. Continue moving state ownership into `Praxis.Core` models.
3. Reintroduce persistence through a shared data/infrastructure layer usable by Avalonia.
4. Keep `MainWindowViewModel` thin and delegate operations to `MainModel`.
5. Port launch execution, default-app opening, path resolution, clipboard, dialogs, and window operations behind platform services.
6. Port missing workflows in slices: persistence load/save, button editing, drag movement, real command execution, Dock persistence, conflict flow, and sync flow.
7. Add focused tests for Core models, repository migrations, platform service policies, and view-model command behavior.
8. Keep Linux-ready seams in every platform-specific addition.

### 3. Proposed Architecture

Target structure:

```text
Praxis.Core/
  Models/
    MainModel.cs
    LauncherButtonModel.cs
    StatusModel.cs
    LauncherButtonColorKey.cs
    LauncherStatusKind.cs
  Services/
    ILauncherExecutionService.cs
    ILauncherSearchService.cs
    IRecentButtonService.cs

Praxis.Data/
  Entities/
  Repositories/
  Storage/

Praxis.Avalonia/
  Views/
  ViewModels/
  Behaviors/
  Converters/
  Services/
  Platform/
    Windows/
    Mac/
    Linux/
  Themes/
```

SQLite entities and repositories live in `Praxis.Data`, keeping `Praxis.Core` UI- and storage-provider-neutral. The current repository preserves v1 table names, opens existing `praxis.db3` files, also accepts an existing `praxis.db`, and migrates launcher databases to schema version 5.

### 4. MVVM Compliance Policy

Model owns application state:

- Button collections, recent-button collections, command/search text, status kind/message, and per-button execution/selection state belong to `MainModel` and `LauncherButtonModel`.
- Model types may implement `INotifyPropertyChanged` through `ObservableObject`.
- Model types must not expose Avalonia, MAUI, process, SQLite, window, brush, thickness, or control types.

ViewModel is a shadow of Model:

- Expose `MainModel`.
- Expose commands that delegate to `MainModel`.
- Do not duplicate `Buttons`, `RecentButtons`, `StatusMessage`, `IsError`, or button state in parallel view-model collections unless a small adapter is required by Avalonia and documented.

View and UI infrastructure:

- View code-behind should stay at `InitializeComponent()`.
- Behaviors, attached properties, converters, controls, and platform services may contain UI infrastructure logic.
- SQLite, process launching, file IO, and platform-specific command details must not live in views.

### 5. UI Implementation Plan

The v2 UI should be one dark, pseudo-acrylic surface rather than a stack of bordered panels. The current UI uses a dark gradient shell, translucent surface brushes, no panel strokes, a frameless `MainWindow`, a dedicated draggable chrome row with app icon and custom caption buttons, a command/create/search row, a `Canvas`-backed button area, a Dock items control, and a status bar whose brush is driven by `LauncherStatusKind`.

Avalonia 12 changes the custom chrome API, so v2 should use `WindowDecorations="None"` with `ExtendClientAreaToDecorationsHint="True"` and a title-bar role/behavior on the top region instead of depending on older `SystemDecorations`/`ExtendClientAreaChromeHints` patterns. Native Acrylic/Mica should remain optional and platform-specific; the default theme should stay pseudo-acrylic for Linux readiness.

### 6. Platform Abstraction Plan

Required service seams:

- Command execution: `ILauncherExecutionService`, currently backed by a desktop implementation that handles direct commands and default-app opening. Dedicated Windows/macOS/Linux policy classes can still be split out as execution rules become stricter.
- Default-app opening: separate policy/service so Windows Explorer, macOS `open`, and Linux `xdg-open` are isolated.
- Paths: service around DB/config/cache/log roots, with Windows `%LOCALAPPDATA%`, macOS Application Support, and Linux XDG paths.
- DB/settings: repository abstractions in Core, with SQLite implementations in `Praxis.Data`.
- Window operations: Avalonia window service for close/minimize/maximize, dialogs, and frameless-window quirks.
- Clipboard and file dialogs: Avalonia services, not model or view-model direct calls.

### 7. Linux Future Support Notes

Linux is not a v2.0.0 release target, but the architecture should preserve these future work items:

- Verify frameless windows and drag regions under X11 and Wayland.
- Test GNOME, KDE, and Xfce visual differences.
- Add XDG base-directory path resolution for DB/config/log files.
- Implement default-app opening through `xdg-open` or a safer desktop portal path.
- Define shell-command execution rules, quoting behavior, executable-bit handling, and environment inheritance.
- Decide packaging order: tarball first, then AppImage/deb if needed.
- Add `.desktop` file generation and icon installation.

### 8. Risks

- Windows and macOS custom window behavior will not match exactly, especially for frameless resizing, native drag, and title-button expectations.
- Future Linux support has X11/Wayland and desktop-environment variability.
- Avalonia `Canvas` plus `ItemsControl` requires careful container binding for `Canvas.Left`/`Canvas.Top`.
- Button drag should be implemented as behavior-to-command-to-model, not direct view mutation.
- DB compatibility is manageable because current records already have `X`, `Y`, `Width`, and `Height`, but v2 still needs decisions for `ColorKey`, tooltip, last execution time, sort order, and Dock metadata.
- Existing MAUI features are broad: conflict resolution, sync, focus policies, modal behavior, crash logging, and theme sync can be missed if ported as visual work only.
- Strict MVVM may require extra adapters, especially for drag and dialogs.
- Platform abstraction must happen before Linux work; otherwise OS-specific behavior will leak back into common code.

### 9. Suggested Next Scope

The next practical slice should be:

- Port button editing and save/update flows onto `MainModel` and `ILauncherButtonRepository`.
- Persist drag/move placement changes through the same repository path.
- Reintroduce theme settings, error logs, and cross-window sync through repository/service abstractions.
- Split command/default-app execution into stricter per-platform policies before adding more launch modes.
- Add a dialog/window service boundary before implementing edit and conflict flows.

## 日本語

### 1. 現状整理

Praxis v2 は現在、`Praxis.Avalonia/Praxis.Avalonia.csproj` の Avalonia デスクトップアプリから起動します。旧 .NET MAUI プロジェクトは削除済みです。現在の runtime surface は、Model がランチャーボタン状態を所有する strict MVVM Avalonia app、擬似アクリル風 frameless shell、候補付き command input、SQLite launcher-button / Dock 永続化、launch log、基本的な desktop command / 既定アプリ起動 service です。

主な責務は次の通りです。

- `Praxis.Avalonia/Views/MainWindow.axaml`: frameless Avalonia shell、上部 command/search/create row、Canvas ベースのボタン領域、Dock、status bar。
- `Praxis.Avalonia/ViewModels/MainWindowViewModel.cs`: `MainModel` の薄い command adapter。
- `Praxis.Core/Models/MainModel.cs`: command/search text、launcher buttons、recent buttons、status state。
- `Praxis.Core/Services/ILauncherExecutionService.cs`: コマンド実行の最初の platform-service seam。
- `Praxis.Core/Services/ILauncherButtonRepository.cs`: storage-neutral な launcher-button 永続化 seam。
- `Praxis.Data/Repositories/SqliteLauncherButtonRepository.cs`: SQLite schema migration と launcher-button 永続化。
- `Praxis.Core/Logic/*`: v1 から残した UI 非依存の再利用可能な policy / helper。

SQLite launcher-button 永続化、command input 実行、最近使った Dock 永続化、launch log、基本的なコマンド実行は Avalonia runtime に接続済みです。ボタン編集 UI、drag UI wiring、error logging、テーマ設定、複数ウィンドウ同期は、UI 専用の巨大実装へ戻さず共有 service/data project 経由で戻します。

### 2. 移行手順

1. Avalonia アプリを唯一の desktop app project として維持します。
2. v2 の状態所有を引き続き `Praxis.Core` の Model へ移します。
3. 永続化は Avalonia から使う共有 data/infrastructure layer として戻します。
4. `MainWindowViewModel` は薄く保ち、操作は `MainModel` に委譲します。
5. コマンド実行、既定アプリ起動、パス解決、クリップボード、ダイアログ、ウィンドウ操作を platform service 化します。
6. persistence load/save、button editing、drag movement、real command execution、Dock persistence、conflict flow、sync flow の順に移植します。
7. Core model、repository migration、platform service policy、view-model command をテストで固めます。
8. platform-specific 追加では Linux-ready seam を残します。

### 3. 提案構成

目標構成は次の通りです。

```text
Praxis.Core/
  Models/
  Services/

Praxis.Data/
  Entities/
  Repositories/
  Storage/

Praxis.Avalonia/
  Views/
  ViewModels/
  Behaviors/
  Converters/
  Services/
  Platform/
    Windows/
    Mac/
    Linux/
  Themes/
```

SQLite entity と repository は `Praxis.Data` に置き、`Praxis.Core` は UI と storage provider から独立させています。現在の repository は v1 table 名を維持し、既存の `praxis.db3` を開き、既存の `praxis.db` も受け入れ、launcher DB を schema version 5 へ移行します。

### 4. MVVM 遵守方針

Model がアプリ状態を持ちます。

- ボタン collection、recent collection、command/search text、status kind/message、ボタンごとの実行/選択状態は `MainModel` と `LauncherButtonModel` に置きます。
- Model は `ObservableObject` による `INotifyPropertyChanged` を実装してよいです。
- Model は Avalonia、MAUI、Process、SQLite、Window、Brush、Thickness、Control を公開しません。

ViewModel は Model の影にします。

- `MainModel` を公開します。
- command は `MainModel` の操作へ中継します。
- `Buttons`、`RecentButtons`、`StatusMessage`、`IsError`、ボタン状態を ViewModel 側で重複保持しません。Avalonia の都合で adapter が必要な場合だけ、理由を明記して最小化します。

View と UI infrastructure の責務は次の通りです。

- View code-behind は `InitializeComponent()` に留めます。
- Behavior、AttachedProperty、Converter、Control、PlatformService は UI infrastructure logic を持ってよいです。
- SQLite、Process 起動、ファイル IO、OS 別コマンド詳細を View に置きません。

### 5. UI 実装方針

v2 UI は bordered panel の集合ではなく、暗い一枚板の擬似アクリル風 surface とします。現在の UI は、暗い gradient shell、半透明風 surface brush、panel stroke なし、frameless `MainWindow`、app icon と custom caption button を持つ drag 専用 chrome row、上部の command/create/search row、`Canvas` ベースのボタン領域、Dock、`LauncherStatusKind` で色が変わる status bar を置いています。

Avalonia 12 では custom chrome API が変わっているため、古い `SystemDecorations` / `ExtendClientAreaChromeHints` 前提ではなく、`WindowDecorations="None"` と `ExtendClientAreaToDecorationsHint="True"`、title-bar role / behavior を使う方針にします。本物の Acrylic / Mica は platform service 側の任意拡張に留め、標準テーマは Linux-ready な擬似アクリルにします。

### 6. Platform 抽象化方針

必要な service seam は次の通りです。

- コマンド実行: `ILauncherExecutionService`。現在は直接コマンドと既定アプリ起動を扱う desktop 実装で接続済みです。実行ルールが増えたら Windows/macOS/Linux policy class へ分けます。
- 既定アプリ起動: Windows Explorer、macOS `open`、Linux `xdg-open` を共通層から分離します。
- パス: DB/config/cache/log の保存先 resolver。Windows `%LOCALAPPDATA%`、macOS Application Support、Linux XDG を分けます。
- DB/設定: Core の repository abstraction と、`Praxis.Data` の SQLite 実装。
- ウィンドウ操作: close/minimize/maximize、dialog、frameless window 差異を扱う Avalonia window service。
- クリップボード/ファイルダイアログ: Model や ViewModel から直接触らず Avalonia service にします。

### 7. Linux 将来対応メモ

Linux は v2.0.0 の正式対象外ですが、次の作業余地を残します。

- X11 / Wayland で frameless window と drag region を検証します。
- GNOME、KDE、Xfce の見た目差を確認します。
- DB/config/log には XDG base-directory 対応を追加します。
- 既定アプリ起動は `xdg-open` または desktop portal を検討します。
- shell command 実行、quote、実行権限、環境変数継承のルールを定義します。
- 配布は tarball から始め、必要に応じて AppImage / deb を検討します。
- `.desktop` file と icon install を追加します。

### 8. リスク

- Windows と macOS の custom window 挙動は完全には一致しません。特に frameless resize、native drag、caption button の期待値が違います。
- Linux は X11 / Wayland / desktop environment 差が大きいです。
- Avalonia の `Canvas` + `ItemsControl` は `Canvas.Left` / `Canvas.Top` の container binding を慎重に扱う必要があります。
- ボタンドラッグは view 直接変更ではなく、behavior -> command -> model の流れにします。
- 既存 DB はすでに `X` / `Y` / `Width` / `Height` を持つため互換性は取りやすい一方、`ColorKey`、tooltip、last executed、sort order、Dock metadata の追加方針が必要です。
- 現行 MAUI 機能は広く、競合解決、同期、フォーカス policy、modal、crash logging、theme sync を見落としやすいです。
- 厳格 MVVM は drag/dialog で adapter が増える可能性があります。
- platform abstraction を先に切らないと、将来 Linux 対応時に OS 依存処理が共通層へ漏れます。

### 9. 次の作業範囲

次の現実的な slice は次の通りです。

- button editing と save/update flow を `MainModel` と `ILauncherButtonRepository` へ載せます。
- drag/move による配置変更を同じ repository path で保存します。
- テーマ設定、error log、複数ウィンドウ同期を repository/service abstraction 経由で戻します。
- launch mode を増やす前に、コマンド/既定アプリ起動を platform 別 policy へ分けます。
- edit / conflict flow の前に dialog/window service boundary を追加します。

## References

- NuGet Gallery: Avalonia package versions: <https://www.nuget.org/packages/Avalonia/>
- Avalonia Docs: Avalonia 12 breaking changes: <https://docs.avaloniaui.net/docs/avalonia12-breaking-changes>
- Avalonia Docs: Windows custom title bar example: <https://docs.avaloniaui.net/docs/platform-specific-guides/windows/>
- Avalonia Docs: Window management and platform differences: <https://docs.avaloniaui.net/docs/app-development/window-management>
