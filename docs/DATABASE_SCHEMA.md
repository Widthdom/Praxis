# Database Schema

## English

Praxis v2 uses `Praxis.Data` for SQLite persistence. The Avalonia runtime currently loads and saves launcher buttons, persists recent Dock order, and writes launch logs through `SqliteLauncherButtonRepository`. Cross-window launcher-button sync uses a sidecar `buttons.sync` signal file through `FileStateSyncNotifier`; that file is not part of the SQLite schema. Persisted theme settings and runtime error-log writes are still migration follow-up work.

### Storage Path

- Windows: `%LOCALAPPDATA%\Praxis`
- macOS: `~/Library/Application Support/Praxis`
- Linux: `$XDG_DATA_HOME/Praxis`, falling back to `~/.local/share/Praxis`

The preferred database file name remains `praxis.db3` for v1 compatibility. If `praxis.db3` is not present but `praxis.db` exists in the same app data directory, v2 opens `praxis.db` in place and applies the same migrations.

For development and smoke tests, `PRAXIS_APP_DATA_DIR` can point the app at an alternate absolute app data directory.

`buttons.sync` is also stored in the app data directory. It contains the source instance id and UTC ticks for the latest launcher-button change notification, allowing other running Praxis windows to reload or defer reload while an editor is open.

### Version

`PRAGMA user_version` is `5`.

Migration history:

1. Create `LauncherButtonEntity`, `LaunchLogEntity`, and `AppSettingEntity`.
2. Add `LauncherButtonEntity.UseInvertedThemeColors`.
3. Create `ErrorLogEntity`.
4. Add `ErrorLogEntity.Level`.
5. Add launcher v2 fields: `ColorKey`, `ToolTip`, `LastExecutedAtUtc`, and `SortOrder`.

### Tables

`LauncherButtonEntity`

- `Id` text primary key
- `Command`, `ButtonText`, `Tool`, `Arguments`, `ClipText`, `Note` text
- `X`, `Y`, `Width`, `Height` numeric placement values
- `UseInvertedThemeColors` boolean/integer
- `ColorKey` text, default `Default`
- `ToolTip` text, default empty
- `LastExecutedAtUtc` nullable datetime
- `SortOrder` integer, default `0`
- `CreatedAtUtc`, `UpdatedAtUtc` datetime

`LaunchLogEntity` is used for command/button execution records. `AppSettingEntity` currently stores Dock order under `dock_order`. `ErrorLogEntity` keeps the v1 table and column names for compatibility and will be wired back into runtime error logging in a later migration slice.

## 日本語

Praxis v2 は SQLite 永続化に `Praxis.Data` を使います。Avalonia runtime は現在 `SqliteLauncherButtonRepository` 経由で launcher button を読み書きし、最近使った Dock 順を保存し、launch log を書き込みます。複数ウィンドウ間の launcher-button 同期は、`FileStateSyncNotifier` が sidecar の `buttons.sync` signal file を使います。このファイルは SQLite schema には含まれません。テーマ設定の永続化と runtime error-log 書き込みは今後の移植対象です。

### 保存先

- Windows: `%LOCALAPPDATA%\Praxis`
- macOS: `~/Library/Application Support/Praxis`
- Linux: `$XDG_DATA_HOME/Praxis`、未設定時は `~/.local/share/Praxis`

v1 互換のため、優先する DB ファイル名は引き続き `praxis.db3` です。同じ app data directory に `praxis.db3` がなく、既存の `praxis.db` がある場合は、v2 は `praxis.db` をそのまま開いて同じ migration を適用します。

開発や smoke test では、`PRAXIS_APP_DATA_DIR` に絶対パスを指定すると別の app data directory を使えます。

`buttons.sync` も app data directory に保存されます。最新の launcher-button 変更通知について、source instance id と UTC ticks を書き込み、他の起動中 Praxis window が即時 reload するか、editor が開いている間は reload を遅延できるようにします。

### バージョン

`PRAGMA user_version` は `5` です。

Migration 履歴:

1. `LauncherButtonEntity`、`LaunchLogEntity`、`AppSettingEntity` を作成。
2. `LauncherButtonEntity.UseInvertedThemeColors` を追加。
3. `ErrorLogEntity` を作成。
4. `ErrorLogEntity.Level` を追加。
5. launcher v2 field として `ColorKey`、`ToolTip`、`LastExecutedAtUtc`、`SortOrder` を追加。

### テーブル

`LauncherButtonEntity`

- `Id` text primary key
- `Command`、`ButtonText`、`Tool`、`Arguments`、`ClipText`、`Note` text
- `X`、`Y`、`Width`、`Height` numeric placement value
- `UseInvertedThemeColors` boolean/integer
- `ColorKey` text、default `Default`
- `ToolTip` text、default empty
- `LastExecutedAtUtc` nullable datetime
- `SortOrder` integer、default `0`
- `CreatedAtUtc`、`UpdatedAtUtc` datetime

`LaunchLogEntity` は command/button 実行履歴に使います。`AppSettingEntity` は現在 `dock_order` として Dock 順を保存します。`ErrorLogEntity` は v1 の table 名と column 名を維持しており、runtime error logging への再接続は後続の migration slice で行います。
