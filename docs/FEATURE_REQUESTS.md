# Praxis 機能追加指示書

## プロジェクト概要

Praxis は .NET MAUI 製のクロスプラットフォーム（Windows / macOS）デスクトップランチャーアプリ。
コマンド入力からツール起動、ボタンのドラッグ配置、テーマ切替、マルチウィンドウ同期を備える。

### 技術スタック
- UI: .NET MAUI 10.0 (`Praxis` プロジェクト)
- ロジック: .NET 10.0 クラスライブラリ (`Praxis.Core`)
- テスト: xUnit (`Praxis.Tests`)
- DB: SQLite (`sqlite-net-pcl`)
- MVVM: `CommunityToolkit.Mvvm`

### アーキテクチャルール
- MAUI に依存しないロジックは `Praxis.Core/Logic/` に置く
- UI サービスのインターフェースは `Praxis/Services/I*.cs` に定義し、DI で注入する
- テスト対象は `Praxis.Core` のロジッククラスに集中する
- 開発ワークフロー: Core にロジック → Tests にテスト → ViewModel/Services/UI に統合

### 主要ファイル
- `Praxis/ViewModels/MainViewModel.cs` — 状態管理・コマンド実行・ドック・テーマ
- `Praxis/Services/SqliteAppRepository.cs` — SQLite 永続化（buttons / logs / settings テーブル）
- `Praxis/Services/IAppRepository.cs` — リポジトリインターフェース
- `Praxis/Services/CommandExecutor.cs` — プロセス起動
- `Praxis/MainPage.xaml` / `MainPage.xaml.cs` — UI レイアウトとオーケストレーション
- `Praxis.Core/Models/LauncherButtonRecord.cs` — ボタンデータモデル
- `Praxis.Core/Models/LaunchLogEntry.cs` — 起動ログモデル
- `docs/DEVELOPER_GUIDE.md` — 実装リファレンス
- `docs/DATABASE_SCHEMA.md` — DB スキーマ定義

---

## 機能 1: インポート / エクスポート

### 目的
ボタン構成のバックアップ・復元・別マシンへの移行を可能にする。

### 仕様

#### エクスポート
- 全ボタンを JSON ファイルとして書き出す
- ファイル形式: `praxis-export-{yyyy-MM-dd}.json`
- 内容: `LauncherButtonRecord` の全フィールドを配列で格納
- ドック順序（`dock_order`）とテーマ設定も含める
- 保存先はOS標準のファイル保存ダイアログで選択させる

```json
{
  "version": 1,
  "exportedAtUtc": "2026-03-08T12:00:00Z",
  "theme": "Dark",
  "dockOrder": ["guid1", "guid2"],
  "buttons": [
    {
      "id": "...",
      "command": "...",
      "buttonText": "...",
      "tool": "...",
      "arguments": "...",
      "clipText": "...",
      "note": "...",
      "x": 0,
      "y": 0,
      "width": 80,
      "height": 60
    }
  ]
}
```

#### インポート
- JSON ファイルを読み込み、既存ボタンとマージまたは置換する
- ファイル選択はOS標準のファイル選択ダイアログ
- インポートモード:
  - **マージ**: 同一 ID があれば上書き、なければ追加
  - **置換**: 全ボタンを削除してからインポート
- インポート後、ドック順序とテーマも反映する
- インポート完了後 `FileStateSyncNotifier` で他ウィンドウに通知する

### 実装方針
1. `Praxis.Core/Logic/` に `ExportDataBuilder` と `ImportDataParser` を作る（JSON のシリアライズ/デシリアライズ、バリデーション）
2. `IAppRepository` に `GetAllSettingsForExportAsync()` / `ImportButtonsAsync(records, mode)` を追加
3. `MainViewModel` にエクスポート/インポートコマンドを追加
4. UI: キーボードショートカット `Ctrl+Shift+E`（エクスポート）/ `Ctrl+Shift+I`（インポート）
5. テスト: `ExportDataBuilderTests`, `ImportDataParserTests` を追加

---

## 機能 2: グローバルホットキー（アプリ外からの起動）

### 目的
他アプリ使用中でも即座に Praxis をアクティブ化してコマンド入力できるようにする。

### 仕様
- デフォルトホットキー: `Ctrl+Space`（Windows）/ `Cmd+Space`（macOS、Spotlight と競合する場合は `Ctrl+Space`）
- ホットキー押下時の動作:
  1. アプリウィンドウを最前面に表示
  2. `MainCommandEntry` にフォーカスを移動
  3. 既存テキストを全選択（即上書き可能）
- アプリが既に最前面の場合はウィンドウを隠す（トグル動作）

### 実装方針

#### Windows
1. `Praxis/Platforms/Windows/` に `GlobalHotkeyService.cs` を作成
2. `RegisterHotKey` / `UnregisterHotKey` Win32 API を P/Invoke で呼び出す
3. メッセージループで `WM_HOTKEY` を捕捉し、ウィンドウアクティベーションを実行

#### macOS
1. `Praxis/Platforms/MacCatalyst/` に `GlobalHotkeyService.cs` を作成
2. `CGEvent.TapCreate` でグローバルキーイベントを監視
3. またはアクセシビリティ権限が不要な `MASShortcut` 相当のアプローチを検討

#### 共通
1. `Praxis/Services/IGlobalHotkeyService.cs` インターフェースを定義
2. `MauiProgram.cs` で DI 登録
3. `MainViewModel` または `MainPage.xaml.cs` からホットキー発火時のアクションをハンドル
4. ホットキーの設定は `AppSettingEntity` に `global_hotkey` キーで保存

---

## 機能 3: コマンドエイリアス / チェーン実行

### 目的
1つのコマンドで複数の処理を順次実行できるようにする。

### 仕様
- ボタンの `Arguments` フィールドにセミコロン区切りで複数コマンドを記述可能にする
- 例: `git pull; dotnet build; dotnet run`
- 各コマンドは順次実行し、途中で失敗したら残りをスキップ（ステータスにエラー表示）
- 既存の単一コマンド動作と完全に後方互換

### 実装方針
1. `Praxis.Core/Logic/` に `CommandChainParser` を作成
   - 入力文字列をセミコロンで分割し、`(tool, arguments)` のリストを返す
   - クォート内のセミコロンはエスケープ: `"path;with;semicolons"` は分割しない
2. `CommandExecutor` の `ExecuteAsync` を拡張（または新メソッド `ExecuteChainAsync`）
   - パース結果を順次実行
   - 結果を集約して返す
3. `LaunchLogEntry` は各ステップごとに記録
4. テスト: `CommandChainParserTests` を追加（分割ロジック、エスケープ、空入力）

---

## 機能 4: 起動統計ダッシュボード

### 目的
既存の `LaunchLogEntry` データを活用し、使用状況を可視化する。

### 仕様
- 新しいオーバーレイパネル（モーダル）として表示
- 表示内容:
  1. **よく使うコマンド Top 10**: コマンド名と実行回数
  2. **最近7日間の起動回数**: 日別の棒グラフ的な表示（テキストベースでも可）
  3. **未使用ボタン一覧**: 30日以上起動されていないボタン
  4. **最終起動日時**: 各ボタンの最終使用日
- 開閉ショートカット: `Ctrl+Shift+S`（Statistics）

### 実装方針
1. `IAppRepository` にクエリメソッドを追加:
   - `GetLaunchStatsAsync()` — ボタン別の起動回数と最終起動日時
   - `GetDailyLaunchCountsAsync(int days)` — 直近N日間の日別起動数
2. `Praxis.Core/Logic/` に `LaunchStatsAggregator` を作成（集計ロジック）
3. `ViewModels/StatsViewModel.cs` を新規作成
4. `MainPage.xaml` にオーバーレイパネルを追加（既存のエディタモーダルと同じパターン）
5. テスト: `LaunchStatsAggregatorTests` を追加

### データ取得 SQL イメージ
```sql
-- ボタン別起動回数
SELECT ButtonId, COUNT(*) as Count, MAX(TimestampUtc) as LastUsed
FROM LaunchLogEntity
WHERE Succeeded = 1
GROUP BY ButtonId
ORDER BY Count DESC

-- 日別起動数
SELECT DATE(TimestampUtc) as Day, COUNT(*) as Count
FROM LaunchLogEntity
WHERE TimestampUtc >= ?
GROUP BY DATE(TimestampUtc)
```

---

## 機能 5: ボタンのアイコン / カラーカスタマイズ

### 目的
ボタンの視覚的識別性を向上させる。

### 仕様

#### カラー
- ボタンごとに背景色を設定可能
- プリセット8色: Red, Orange, Yellow, Green, Blue, Purple, Pink, Gray
- 未設定時はテーマデフォルト色を使用

#### アイコン（任意、カラーのみでも十分価値がある）
- プリセットアイコン（テキスト絵文字ベース）: 🌐 📁 💻 🔧 📝 🎮 📊 🔗
- ボタン左上に小さく表示

### 実装方針

#### データモデル変更
1. `LauncherButtonRecord` に `Color` (string, nullable) と `Icon` (string, nullable) を追加
2. `LauncherButtonEntity` にも同じカラムを追加
3. `SqliteAppRepository` の `Map` / `Clone` メソッドを更新
4. `ButtonEditorViewModel` にカラー/アイコン選択フィールドを追加

#### UI
1. エディタモーダルにカラーパレットとアイコン選択を追加
2. `MainPage.xaml` のボタンテンプレートで `BackgroundColor` をバインド
3. `Praxis.Core/Logic/ButtonColorResolver.cs` でカラー文字列→色変換とデフォルトフォールバック

#### テスト
- `ButtonColorResolverTests`: null/空/不正値でデフォルト返却、有効値で正しい色返却

---

## 機能 6: Undo / Redo

### 目的
ボタンの移動・削除・編集の誤操作を取り消し可能にする。

### 仕様
- 対象操作: ボタン移動、ボタン削除、ボタン編集（プロパティ変更）、ボタン作成
- ショートカット: `Ctrl+Z`（Undo）/ `Ctrl+Shift+Z` or `Ctrl+Y`（Redo）
- 履歴上限: 直近50操作
- アプリ再起動で履歴クリア（メモリのみ保持）

### 実装方針
1. `Praxis.Core/Logic/UndoRedoManager.cs` を作成
   - コマンドパターン: `IUndoableAction` インターフェース
   - `Execute()`, `Undo()`, `Redo()` メソッド
   - スタックベースの履歴管理
2. 具体的なアクションクラス:
   - `MoveButtonAction` — 移動前後の座標を保持
   - `DeleteButtonAction` — 削除されたレコードのスナップショットを保持
   - `EditButtonAction` — 編集前後のレコードを保持
   - `CreateButtonAction` — 作成されたレコードの ID を保持
3. `MainViewModel` の各操作メソッドで `UndoRedoManager.Push(action)` を呼ぶ
4. Undo/Redo 実行時は `IAppRepository` を通じて DB を更新し、UI を再描画
5. テスト: `UndoRedoManagerTests` — Push/Undo/Redo/上限超過/Redo 無効化

---

## 機能 7: クイックルックプレビュー

### 目的
エディタを開かずにボタンの詳細情報を確認可能にする。

### 仕様
- ボタン上にマウスホバー 500ms 後にポップアップ表示
- 表示内容:
  - Command
  - Tool → Arguments（1行にまとめる）
  - Note（あれば）
  - 最終起動日時（LaunchLog から取得、あれば）
- ポップアップ位置: ボタンの上側に表示、画面端では自動調整
- ドラッグ開始やクリック時は即座に非表示

### 実装方針
1. `MainViewModel` に `HoveredButton` プロパティと `ShowPreviewCommand` / `HidePreviewCommand` を追加
2. `MainPage.xaml` にプレビュー用オーバーレイ（`Border` + `StackLayout`）を追加
   - `IsVisible` を `HoveredButton != null` にバインド
3. ホバー検出は既存の `PointerGestureRecognizer` (`PointerEntered` / `PointerExited`) を利用
4. 500ms タイマーで遅延表示（`CancellationTokenSource` で制御）
5. `Praxis.Core/Logic/PreviewContentBuilder.cs` で表示テキストを組み立て
6. テスト: `PreviewContentBuilderTests` — 各フィールドの有無に応じた出力パターン

---

## 共通の注意事項

### 実装順序（推奨）
依存関係が少なく既存コードへの影響が小さい順:
1. クイックルックプレビュー（機能7）— 既存データだけで動く、UI 追加のみ
2. ボタンカラー（機能5）— DB カラム追加 + UI バインディング
3. 起動統計（機能4）— 既存ログデータ活用、新規オーバーレイ
4. インポート/エクスポート（機能1）— Repository 拡張 + ファイル I/O
5. コマンドチェーン（機能3）— CommandExecutor 拡張
6. Undo/Redo（機能6）— 既存操作への横断的な変更
7. グローバルホットキー（機能2）— プラットフォーム固有の実装が重い

### コーディングルール
- 既存の命名規則・コードスタイルに従う
- `Praxis.Core` に置くロジックは MAUI 依存禁止
- 新規ロジッククラスには必ずテストを追加
- `MainPage.xaml.cs` をこれ以上肥大化させない（可能なら ViewModel やサービスに寄せる）
- `SqliteAppRepository` のカラム追加時は `Map` / `Clone` の両方を忘れず更新
- マルチウィンドウ同期が必要な変更は `FileStateSyncNotifier` 経由で通知

### テスト
- `dotnet test Praxis.Tests/Praxis.Tests.csproj` で全テストが通ることを確認
- 新規テストクラスは `Praxis.Tests/` 直下に `{クラス名}Tests.cs` として配置
