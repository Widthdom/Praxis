# AGENT_GUIDE.md

This file is the shared source of truth for AI-agent instructions in this repository.
`AGENTS.md` (Codex) and `CLAUDE.md` (Claude Code) are thin entry points that defer here.
It captures the coding standards, workflow rules, and review checklist for this repository.

---

## English

### Project Overview

**What this app does:**

Praxis is a desktop launcher built with .NET MAUI for Windows and macOS (Mac Catalyst).
It stores launcher buttons in SQLite, supports drag/edit/search workflows, executes commands with arguments, keeps recent launches in a Dock, and synchronizes button/theme changes across open windows.

This repository is optimized for **desktop workflow reliability**, not just happy-path UI behavior.
Many implementation details exist specifically to keep command input, focus, IME/input-source behavior, cross-window sync, crash logging, and platform-specific build behavior stable.

**Repository structure:**

- `Praxis/` - MAUI app, XAML, platform handlers, services, ViewModels
- `Praxis.Core/` - reusable logic, policies, models, path/layout/IME/focus helpers
- `Praxis.Tests/` - xUnit tests, including linked-source workflow tests for app-layer files
- `docs/` - bilingual developer, testing, schema, and branding documentation
- `.github/workflows/` - CI, delivery, and CodeQL placeholder workflows
- `global.json` / `nuget.config` - pinned .NET 10 SDK and additive restore-feed configuration used by local/CI builds

**Key technical facts:**

- .NET 10 MAUI desktop app
- Supported targets: `net10.0-windows10.0.19041.0`, `net10.0-maccatalyst`
- Persistence: SQLite (`sqlite-net-pcl`)
- MVVM: `CommunityToolkit.Mvvm`
- Versioning: `version.json` + Nerdbank.GitVersioning
- Error persistence: synchronous crash file + asynchronous DB logging
- Cross-window sync: file-based notifier plus repository reload/diff-apply
- SDK/feed stability: `global.json` pins .NET `10.0.100` with patch roll-forward, and `nuget.config` adds `dotnet10` / `dotnet10-transport` feeds without replacing the default sources

### Design Philosophy

1. **User-visible desktop behavior is a contract**: focus routing, keyboard shortcuts, command suggestions, clear-button behavior, IME/input-source rules, Dock behavior, and modal flows are heavily regression-tested because small UI changes can easily break real usage.
2. **Push reusable logic down into `Praxis.Core`**: if behavior can be tested without MAUI, keep it in `Praxis.Core/Logic` and cover it with unit tests. Keep MAUI code for orchestration, handlers, view wiring, and platform integration.
3. **Respect the current partial-class boundaries**: `MainPage` and `MainViewModel` are intentionally split by concern. Do not collapse behavior back into giant files.
4. **Crash evidence must survive termination**: the crash file logger exists because async DB logging alone is not enough during abrupt shutdown. When touching logging, preserve the “write to file first, DB second” rule.
5. **Cross-window and optimistic-concurrency behavior is deliberate**: button reloads, conflict dialogs, dock persistence, and theme sync are core product behavior, not incidental details.
6. **Docs and tests are part of the feature**: README, developer/testing/schema docs, workflow guards, and changelog entries are expected to move in the same commit as behavioral changes.

### Build & Test

```bash
dotnet restore Praxis.slnx
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release -v minimal
dotnet test Praxis.slnx -c Release -v minimal
```

Platform-specific builds:

```bash
# Windows
dotnet build Praxis/Praxis.csproj -c Release -f net10.0-windows10.0.19041.0 -v minimal

# macOS (Mac Catalyst)
dotnet build Praxis/Praxis.csproj -c Release -f net10.0-maccatalyst -v minimal -p:EnableCodeSigning=false -p:CodesignRequireProvisioningProfile=false
```

- Prefer **Release** configuration before committing because CI runs Release.
- If you change workflows, verify the exact commands in `.github/workflows/ci.yml`.
- Keep `global.json`, `nuget.config`, and the checked-in workflow commands aligned when changing SDK/feed behavior.
- If `dotnet` or MAUI workloads are unavailable, state that explicitly.

### Code Search Rules

This project uses **`cdidx`** for fast code search via a pre-built SQLite index at `.cdidx/codeindex.db`.
**Query this database** instead of using `find`, `grep`, `rg`, or `ls -R`.
The tracked `.claude/settings.json` denies the common shell search commands so the tool follows the same cdidx-first workflow.

These rules track the cdidx v1.21.0 "Setup: Add AI agent search rules" template upstream. Treat the upstream `cdidx` semantics as the source of truth.

#### Setup

First check if `cdidx` is available:

```bash
cdidx --version
```

If the current client can load local MCP servers, register `cdidx` as a stdio server so structured search is available without shelling out:

Claude Code (`.claude/settings.json` or `.mcp.json`) / Codex CLI (`codex.json` or `~/.codex/config.json`):

```json
{
  "mcpServers": {
    "cdidx": {
      "command": "cdidx",
      "args": ["mcp", "--db", ".cdidx/codeindex.db"]
    }
  }
}
```

Prefer MCP for read-only investigation when it is available. Keep the CLI as the fallback or for explicit local verification.

**If not found**, install it:

```bash
# No .NET required — downloads a self-contained binary
curl -fsSL https://raw.githubusercontent.com/Widthdom/CodeIndex/main/install.sh | bash
```

Or, if .NET 8+ SDK is available:

```bash
dotnet tool install -g cdidx
```

**If already installed**, reinstall or switch to a specific version explicitly:

```bash
curl -fsSL https://raw.githubusercontent.com/Widthdom/CodeIndex/vX.Y.Z/install.sh | bash -s -- vX.Y.Z
```

Re-running the no-argument one-liner still targets the latest release: the installer resolves the latest tag first and skips the download only when the current healthy install already matches that version. Broken `v0.0.0` installs or same-version installs missing required adjacent assets are treated as reinstall targets.

If install fails (no network, unsupported platform), skip to the **Direct SQL fallback** section — you can query `.cdidx/codeindex.db` directly with `sqlite3` if the database is already built. If neither `cdidx` nor `sqlite3` is available, use the Claude Code built-in `Grep` / `Glob` tools (or your harness's equivalent). Do not fall back to shell `rg` / `grep` / `find` or a global `cdidx` in a Claude Code session — those may be blocked by the repo deny list, and bypassing them can hide stale-binary bugs.

Before searching, check whether the index already matches the workspace:

```bash
cdidx status --check --json
```

If it exits `0` with `index_matches_workspace: true`, skip reindexing. Otherwise update the index so results are accurate:

```bash
cdidx .   # incremental update (skips unchanged files)
```

#### Keeping the index up to date

After editing files, update the database so search results stay accurate:

```bash
cdidx . --files path/to/changed_file.cs   # only files you modified
cdidx . --commits HEAD                     # all files changed in the last commit
cdidx . --commits abc123                   # a specific commit hash
cdidx .                                    # full incremental update (skips unchanged)
```

**Rule: whenever you modify source files, run `cdidx status --check --json` before the next search; if it reports a mismatch, run one of the update commands above.**
After `git reset`, `git rebase`, `git commit --amend`, `git switch`, or `git merge`, prefer `cdidx .` so stale paths are purged against the current worktree instead of only refreshing commit-local paths.

If `cdidx status --json` reports `DEGRADED` after a scoped refresh, rerun `cdidx .` before trusting graph or validation commands.

#### Query strategy

- Start by checking freshness with `status --check --json` when search correctness matters. If the index does not match the workspace, run `cdidx .` before trusting symbol or graph results. Use `map` / `map --json` for a quick overview of languages, modules, likely entrypoints, and high-activity areas.
- Use `languages` as the source of truth for canonical `--lang` values and current symbol / graph support. Don't rely on memorized per-language details — support changes over time, and the CLI reports `graph_supported`, `graph_support_reason`, and related trust metadata.
- When you have a likely symbol name, run `symbols` first to resolve candidates. Add `--exact` once the intended symbol is known. Use `outline` for a single file's structure and `inspect` when you want bundled definition, reference, caller, and callee context in one request.
- Use `definition --body` for implementation text, then `references`, `callers`, `callees`, or `impact` for graph questions in supported languages. Prefer `--exact` after a candidate has been resolved so names like `Run` do not expand to `RunAsync` or `RunImpact`. Treat graph fallback and degraded metadata as confidence signals, not decoration.
- Use `search` for raw text, comments, strings, option names, generated code, or languages where structured graph support is unavailable. Use `--exact-substring` for punctuation-heavy literals and `--fts` only when you intentionally want raw FTS5 syntax such as `NEAR` or `OR`.
- Scope broad searches early with `--path <text>`, repeatable `--exclude-path <text>`, and `--exclude-tests` unless tests are the target. For noisy generated, minified, or transpiled files, reduce payload size with `--snippet-lines <n>` and `--max-line-width <n>`.
- Use `files` to discover candidate paths, `find` to re-locate exact text within known files, and `excerpt` to fetch only the needed lines instead of opening entire files.
- Use `deps --reverse` for file-level impact, `impact` for callable-symbol ripple checks, `unused` for potentially dead definitions, and `hotspots` for central symbols. These commands are only as strong as the current graph support and freshness metadata, so keep `languages` and `status --check --json` in the loop.
- Use `files --since <datetime>` or `search --since <datetime>` to focus on recent changes, `index --dry-run` to preview index scope, and `--count` to size result sets before fetching full payloads.
- Use `validate` for encoding issues (BOMs, null bytes, U+FFFD, mixed line endings).
- Report `cdidx` bugs or improvement ideas at https://github.com/Widthdom/CodeIndex/issues with the observed behavior, expected behavior, and the command you ran.

#### CLI examples

```bash
cdidx status --check --json
cdidx map --path Praxis --exclude-tests --json
cdidx inspect "MainViewModel" --lang csharp --exact --exclude-tests
cdidx symbols --lang csharp --name MainViewModel --exact
cdidx definition "MainViewModel" --lang csharp --exact --body
cdidx search "keyword" --path Praxis --exclude-tests --snippet-lines 6 --max-line-width 160
cdidx search "Run();" --exact-substring --path Praxis
cdidx callers "ApplyButtonDiffAsync" --lang csharp --exact --exclude-tests
cdidx callees "ReloadButtonsAsync" --lang csharp --exact --exclude-tests
cdidx impact "ReloadButtonsAsync" --lang csharp --exact --exclude-tests --json
cdidx deps --path Praxis/Services/SqliteAppRepository.cs --reverse --json
cdidx hotspots --lang csharp --limit 20 --json
cdidx unused --lang csharp --exclude-tests --json
cdidx find "SemaphoreSlim" --path Praxis/Services/SqliteAppRepository.cs --after 2
cdidx excerpt Praxis/Services/SqliteAppRepository.cs --start 1 --end 80
cdidx outline Praxis/MainPage.EditorAndInput.cs --json
cdidx files --lang csharp --path Praxis.Core
cdidx files --since 2026-01-01T00:00:00Z --path Praxis
cdidx languages --json
cdidx validate
cdidx index --dry-run
cdidx backfill-fold
git log --oneline -- path/to/file
```

#### MCP workflow

If the client exposes `cdidx` as MCP tools, prefer them before shelling out:

- `map` for repo overview
- `analyze_symbol` (or `inspect`) for a single-symbol round-trip with bundled definition / references / callers / callees / file metadata / graph metadata
- `search`, `definition`, `references`, `callers`, `callees`, `symbols`, `files`, `find_in_file`, `excerpt`, `outline`, `deps`, `impact_analysis`, `unused_symbols`, `symbol_hotspots`, `status`, `languages`, `validate`, `ping`, `index`, `backfill_fold`
- `batch_query` for multiple read-only queries in one round-trip
- `suggest_improvement` when `cdidx` itself appears to miss symbols, misreport graph support, or expose an indexing or MCP gap

All MCP tools include `annotations` (`readOnlyHint`, `destructiveHint`, `idempotentHint`, `openWorldHint`) so AI clients can auto-approve safe read-only queries without prompting.

Tool results include structured JSON in `structuredContent` plus a short text summary in `content`. Graph-oriented MCP tools (`references`, `callers`, `callees`) return `graph_language`, `graph_supported`, and `graph_support_reason` when a language filter is provided, so clients can distinguish unsupported languages from genuine zero-hit queries.

Before using `suggest_improvement`, check existing GitHub issues to avoid duplicates. With `CDIDX_GITHUB_TOKEN` configured it may open an upstream issue; otherwise it stays local in `.cdidx/suggestions.json`.

Keep the SQL fallback only for sessions where MCP and CLI are both unavailable or broken.

#### Search rules by change type

- UI or page behavior: search `Praxis/MainPage.xaml`, `Praxis/MainPage*.cs`, and platform handlers under `Praxis/Platforms/**/Handlers/`.
- MainPage behavior is split across `MainPage.PointerAndSelection.cs`, `MainPage.FocusAndContext.cs`, `MainPage.EditorAndInput.cs`, `MainPage.ShortcutsAndConflict.cs`, `MainPage.WindowsInput.cs`, `MainPage.MacCatalystBehavior.cs`, `MainPage.ViewModelEvents.cs`, and the field partials.
- ViewModel workflow: search `Praxis/ViewModels/MainViewModel*.cs`, related services, and `Praxis.Tests/MainViewModelWorkflowIntegrationTests.cs`.
- Core logic or policies: search `Praxis.Core/Logic/` first, then the caller sites in `Praxis/`, then matching tests in `Praxis.Tests/`.
- SQLite, schema, storage, or logging: search `Praxis/Services/SqliteAppRepository.cs`, `Praxis/Services/DbErrorLogger.cs`, `Praxis/Services/CrashFileLogger.cs`, `docs/DATABASE_SCHEMA.md`, `Praxis.Tests/DatabaseSchemaVersionPolicyTests.cs`, `Praxis.Tests/AppStoragePathLayoutResolverTests.cs`, `Praxis.Tests/DbErrorLoggerTests.cs`, and `Praxis.Tests/CrashFileLoggerTests.cs`.
- CI, release, or coverage: search `.github/workflows/*.yml`, `Praxis.Tests/CiCoverageWorkflowPolicyTests.cs`, `README.md`, `docs/DEVELOPER_GUIDE.md`, `docs/TESTING_GUIDE.md`, and `version.json`.
- Partial-class structure: search both the implementation files and `Praxis.Tests/MainPageStructureTests.cs` before moving fields, renaming XAML elements, or reordering modal fields.
- Linked-source tests: if you touch app-layer files compiled into tests, check `Praxis.Tests/Praxis.Tests.csproj` to make sure the linked-source list still matches.

#### Direct SQL fallback

Use only when `cdidx` is unavailable but `.cdidx/codeindex.db` already exists. The queries below require `sqlite3`. Treat this as a basic fallback for raw text / symbol inspection only; prefer `cdidx` for call graph queries, freshness metadata, exact-name semantics, scoped snippets, `impact`, `unused`, and `hotspots`.

```sql
-- Full-text search
SELECT f.path, c.start_line, c.content
FROM fts_chunks fc
JOIN chunks c ON c.id = fc.rowid
JOIN files f ON f.id = c.file_id
WHERE fts_chunks MATCH 'keyword'
LIMIT 20;
```

```sql
-- Search by symbol name
SELECT f.path, s.name, s.line
FROM symbols s
JOIN files f ON f.id = s.file_id
WHERE s.name LIKE '%MainViewModel%'
ORDER BY f.path, s.line
LIMIT 20;
```

If `sqlite3` is also unavailable, use the Claude Code built-in `Grep` / `Glob` tools (or your harness's equivalent). Do not fall back to shell `rg` / `grep` / `find` — they are blocked by the repo deny list.

#### Search expectations before editing

Before changing behavior, search for:

1. The implementation site
2. The guard tests
3. The relevant docs
4. The workflow or versioning file if CI/release behavior is involved
5. The recent commit history for that file or area

Do not assume a change is "local" until you search those five surfaces.

### Cross-Cutting Consistency Rule

**Any behavior change must update all related artifacts in the same commit.**

| When you change... | Also update... |
|---|---|
| User-visible behavior, shortcuts, defaults, supported-platform wording | `README.md`, `CHANGELOG.md`, related tests |
| `MainPage.xaml` or `MainPage*.cs` behavior/structure | `Praxis.Tests/MainPageStructureTests.cs`, `docs/DEVELOPER_GUIDE.md`, `README.md` if user-visible |
| `MainViewModel*.cs` workflow, command suggestions, undo/redo, sync behavior | `Praxis.Tests/MainViewModelWorkflowIntegrationTests.cs`, related policy tests, `CHANGELOG.md`, `README.md` if user-visible |
| `Praxis.Core/Logic/*` policies/resolvers | Matching `Praxis.Tests/*Tests.cs`, caller sites in `Praxis/`, docs if the behavior is externally visible |
| SQLite schema, migrations, storage paths, log retention, error log behavior | `docs/DATABASE_SCHEMA.md`, `Praxis.Tests/DatabaseSchemaVersionPolicyTests.cs`, `Praxis.Tests/AppStoragePathLayoutResolverTests.cs`, logging tests, `CHANGELOG.md` |
| DI services, interfaces, ViewModel/service wiring | `Praxis/MauiProgram.cs`, linked-source includes in `Praxis.Tests/Praxis.Tests.csproj` when needed, `docs/DEVELOPER_GUIDE.md` |
| CI workflow commands, coverage artifact names/paths, workflow names | `.github/workflows/*.yml`, `Praxis.Tests/CiCoverageWorkflowPolicyTests.cs`, README badges/links, relevant docs |
| Versioning or release mechanics | `version.json`, `CHANGELOG.md`, README/docs if release instructions changed |
| Branding/icon resource filenames or packaging assets | `Praxis/Praxis.csproj`, `docs/branding/README.md`, related docs/tests if applicable |
| Architecture or repo workflow rules | `AGENT_GUIDE.md` |

### Architecture

#### App/Core/Test split

- `Praxis/` contains MAUI UI, handlers, services, and ViewModels.
- `Praxis.Core/` contains pure logic, policies, formatters, resolvers, and models.
- `Praxis.Tests/` covers core logic directly and also compiles selected app-layer source files as linked files for workflow/integration-style tests.

#### MainPage partial structure is intentional

`MainPage` is deliberately split into:

- field-only files: `MainPage.Fields.*.cs`
- state file: `MainPage.InteractionState.cs`
- behavior files such as `MainPage.PointerAndSelection.cs`, `MainPage.FocusAndContext.cs`, `MainPage.EditorAndInput.cs`, `MainPage.ModalEditor.cs`, `MainPage.ViewModelEvents.cs`, `MainPage.StatusAndTheme.cs`, `MainPage.DockAndQuickLook.cs`, `MainPage.WindowsInput.cs`, `MainPage.ShortcutsAndConflict.cs`, `MainPage.MacCatalystBehavior.cs`, `MainPage.LayoutUtilities.cs`

`Praxis.Tests/MainPageStructureTests.cs` enforces several of these boundaries.
If you rename XAML elements, move fields, or reorder modal UI, update that test in the same commit.

#### MainViewModel partial structure

`MainViewModel` is split across:

- `MainViewModel.cs`
- `MainViewModel.Actions.cs`
- `MainViewModel.CommandSuggestions.cs`

Behavioral changes often require matching updates in workflow integration tests and command/focus policy tests.

#### DI registrations

`Praxis/MauiProgram.cs` is the DI composition root.
Repository, loggers, services, `MainViewModel`, and `MainPage` are registered there as singletons.
If you add a new service or change wiring, update `MauiProgram.cs` and relevant docs/tests.

#### SQLite repository and schema migration

`Praxis/Services/SqliteAppRepository.cs` is the persistence core:

- guarded by `SemaphoreSlim`
- keeps an in-memory cache and command cache
- uses `PRAGMA user_version`
- performs sequential migrations
- supports explicit reloads for cross-window sync and conflict detection

Schema or storage-path changes are not “just implementation details”; they must stay aligned with `docs/DATABASE_SCHEMA.md` and schema/path tests.

#### Linked-source test model

`Praxis.Tests/Praxis.Tests.csproj` links selected app-layer source files directly into the test project.
If you split or rename linked files, or add new app-layer files that tests should compile against, update that project file in the same commit.

### Test Infrastructure

- Test framework: xUnit
- Coverage collector: `coverlet.collector`
- Workflow guard: `Praxis.Tests/CiCoverageWorkflowPolicyTests.cs`
- Prefer built-in xUnit assertions and simple local test doubles/stubs
- Keep UI-agnostic logic in `Praxis.Core` where possible
- Test platform policies with explicit inputs rather than runtime platform detection
- When changing behavior already described in `docs/TESTING_GUIDE.md`, update that guide too

### CI / GitHub Actions

- Workflows:
  - `.github/workflows/ci.yml`
  - `.github/workflows/delivery.yml`
  - `.github/workflows/codeql.yml` (intentionally disabled placeholder; GitHub default CodeQL is used instead)
- `ci.yml` runs tests with coverage, Windows build, and Mac Catalyst build
- `delivery.yml` runs on `workflow_dispatch` and `v*` tags

#### Important CI rules learned from past failures

1. **Do not switch checkout back to shallow clone**. `fetch-depth: 0` is required because Nerdbank.GitVersioning needs history for version height.
2. **Do not casually override `TargetFrameworks` in workflows**. Earlier workflow fixes had to back out fragile `-p:TargetFrameworks=...` patterns. Prefer normal `--framework` / `-f` usage unless there is a proven build reason.
3. **Keep workload install commands aligned with the checked-in workflows**. The current CI/delivery flows use `dotnet workload install maui` without `--skip-manifest-update`; do not reintroduce older flags casually.
4. **Keep .NET 10 restore settings aligned across `global.json`, workflows, and `nuget.config`**. Current automation uses the GA SDK (`10.0.x` / `dotnet-quality: ga`) and additive `dotnet10` / `dotnet10-transport` feeds while preserving default NuGet sources.
5. **Mac runners need `sudo xcodebuild -runFirstLaunch`** before Mac Catalyst build/publish.
6. **Keep the Xcode compatibility gate**. The workflow intentionally skips Mac Catalyst work when the runner Xcode version is below the required floor.
7. **Windows delivery publish intentionally leaves the RID empty**. Reintroducing a Windows RID can bring back Mono-runtime packaging failures.
8. **If you change coverage artifact naming or path**, update `Praxis.Tests/CiCoverageWorkflowPolicyTests.cs` in the same commit.
9. **`codeql.yml` is intentionally a placeholder**. Do not re-enable a duplicate custom CodeQL analysis without explicit intent.

### Git & Release Rules

- Do not create `git tag` without explicit user approval. Tag pushes trigger `delivery.yml`.
- Version numbers come from `version.json` and GitVersion; `Praxis/Praxis.csproj` should keep using `$(Version)` and `$(GitVersionHeight)`.
- Prefer concise English commit messages unless the user asks otherwise.

#### Version bump steps

When intentionally releasing a new version:

1. Update `version.json`
2. Move `CHANGELOG.md` entries from `[Unreleased]` into the new version heading
3. Update compare links at the bottom of `CHANGELOG.md`
4. Verify workflows/docs if release behavior changed

### Common Pitfalls

1. **GitVersion + shallow fetch**: removing `fetch-depth: 0` breaks version-height calculation in CI and packaging.
2. **Mac Catalyst runner setup**: skipping `xcodebuild -runFirstLaunch` can fail before the actual app build even starts.
3. **Xcode floor drift**: the compatibility guard exists for a reason; do not delete it just to make the workflow look greener.
4. **Workflow argument churn**: CI had repeated failures around `dotnet restore/build` framework selection. Be conservative with MSBuild property overrides.
5. **.NET 10 restore/feed drift**: do not switch back to preview SDK quality, clear default NuGet sources, or remove the additive `dotnet10` feeds without a concrete restore failure analysis.
6. **Windows publish RID drift**: Windows delivery currently avoids a RID on purpose; adding one back can reintroduce Mono runtime dependency failures.
7. **`MainPage` file drift**: fields belong in `MainPage.Fields.*.cs`; behavior belongs in concern-specific partials such as `MainPage.PointerAndSelection.cs` and `MainPage.FocusAndContext.cs`, not back in `MainPage.xaml.cs`.
8. **Modal-only ASCII enforcement**: `ModalCommandEntry` opts into ASCII enforcement; the main command entry intentionally does not.
9. **Linked-source test drift**: app-layer source renames/splits can break tests unless `Praxis.Tests/Praxis.Tests.csproj` is kept in sync.
10. **Crash logging order**: write to `CrashFileLogger` first, then enqueue DB logging. Reversing that weakens crash survivability.
11. **Schema changes require doc/test updates**: `docs/DATABASE_SCHEMA.md`, migration tests, and path/layout tests must move together.
12. **Workflow guard drift**: coverage workflow changes require `CiCoverageWorkflowPolicyTests` updates.
13. **Windows icon filename**: keep `Resources/AppIcon/appiconfg_windows.svg` with the underscore name; a previous hyphenated name caused build/package trouble.
14. **Mac Catalyst custom targets are there for stability**: do not remove signing/copy/intermediate-assembly targets casually; they were added to address real build/runtime issues.

### Commit-by-Commit Checklist

Evaluate every item below for **each commit**, not only before opening a PR:

1. **Tests** - Did this change break existing tests? Does it require new tests? Run Release tests.
2. **README.md** - Did user-visible behavior, defaults, shortcuts, supported platforms, or setup steps change?
3. **CHANGELOG.md** - Does this behavior/fix/docs/workflow change deserve an entry?
4. **docs/DEVELOPER_GUIDE.md** - Did architecture, wiring, handlers, partial structure, or platform behavior change?
5. **docs/TESTING_GUIDE.md** - Did you add/rename/remove tests or materially change test coverage intent?
6. **docs/DATABASE_SCHEMA.md** - Did schema, storage paths, log retention, or error-log behavior change?
7. **AGENT_GUIDE.md** - Did you change repository workflow rules, release rules, architecture rules, or recurring gotchas?
8. **MainPage structure guards** - If you touched `MainPage.xaml` / `MainPage*.cs`, do `Praxis.Tests/MainPageStructureTests.cs` assertions still match?
9. **Linked-source tests** - If you moved/split app-layer source files used by tests, did you update `Praxis.Tests/Praxis.Tests.csproj`?
10. **DI registration** - If you added a service/interface/ViewModel dependency, did you update `Praxis/MauiProgram.cs`?
11. **Logging/crash safety** - If you touched logging, do `CrashFileLoggerTests` / `DbErrorLoggerTests` still cover the behavior and preserve file-first logging?
12. **Schema/path guards** - If you touched persistence or storage paths, did you update `DatabaseSchemaVersionPolicyTests` / `AppStoragePathLayoutResolverTests`?
13. **CI/workflows** - If you touched `.github/workflows/*.yml`, did you update `CiCoverageWorkflowPolicyTests` and any related docs/badges?
14. **Version/release** - If this commit affects versioning or delivery, did you update `version.json`, `CHANGELOG.md`, and release notes/links as needed?

---

## 日本語

### プロジェクト概要

**このアプリがすること:**

Praxis は Windows / macOS（Mac Catalyst）向けの .NET MAUI 製デスクトップランチャーです。
ランチャーボタンを SQLite に保存し、ドラッグ編集・検索・コマンド実行・Dock 表示・複数ウィンドウ同期を提供します。

このリポジトリは、単に UI が動けばよいのではなく、**デスクトップ運用で壊れやすい箇所を安定させること**を重視しています。
コマンド入力、フォーカス、IME/入力ソース、複数ウィンドウ同期、クラッシュ記録、プラットフォーム別ビルドの安定化に関する実装が多く入っています。

**リポジトリ構成:**

- `Praxis/` - MAUI アプリ本体、XAML、プラットフォームハンドラ、サービス、ViewModel
- `Praxis.Core/` - 再利用可能なロジック、ポリシー、モデル、各種 Resolver
- `Praxis.Tests/` - xUnit テスト。アプリ層ソースを linked file として取り込む統合寄りテストも含む
- `docs/` - 英日併記の開発者向け・テスト・DB スキーマ・ブランディング文書
- `.github/workflows/` - CI、配布、CodeQL プレースホルダ
- `global.json` / `nuget.config` - ローカル/CI ビルドで使う .NET 10 SDK 固定と追加 restore feed 設定

**技術的な要点:**

- .NET 10 MAUI デスクトップアプリ
- 対応ターゲット: `net10.0-windows10.0.19041.0`, `net10.0-maccatalyst`
- 永続化: SQLite（`sqlite-net-pcl`）
- MVVM: `CommunityToolkit.Mvvm`
- バージョニング: `version.json` + Nerdbank.GitVersioning
- エラーログ: 同期クラッシュファイル + 非同期 DB 書き込み
- 複数ウィンドウ同期: ファイルシグナル + リポジトリ再読込/差分反映
- SDK / feed 安定化: `global.json` は .NET `10.0.100` を patch roll-forward 付きで固定し、`nuget.config` は既定ソースを消さずに `dotnet10` / `dotnet10-transport` feed を追加している

### 設計方針

1. **ユーザーが触るデスクトップ挙動は契約である**: フォーカス遷移、キーボードショートカット、候補一覧、クリアボタン、IME/入力ソース、Dock、モーダル挙動は小さな変更で壊れやすいため、強く回帰保護されている。
2. **再利用可能な挙動は `Praxis.Core` へ寄せる**: MAUI を必要としないロジックは `Praxis.Core/Logic` に置き、ユニットテストで固める。MAUI 側はオーケストレーション、ハンドラ、画面配線、プラットフォーム統合に留める。
3. **いまの partial class 分割を尊重する**: `MainPage` と `MainViewModel` は責務別に意図して分割されている。巨大ファイルへ戻さない。
4. **クラッシュ時の証跡は異常終了後も残す**: async DB ログだけでは不十分なので crash file logger がある。ログ周りを触るときは「まずファイル、次に DB」の順序を守る。
5. **複数ウィンドウ同期と競合検出は本質機能**: ボタン再読込、競合ダイアログ、Dock 永続化、テーマ同期は incidental な処理ではない。
6. **ドキュメントとテストも機能の一部**: README、開発者/テスト/スキーマ文書、ワークフローガード、CHANGELOG は、挙動変更と同一コミットで更新する前提。

### ビルドとテスト

```bash
dotnet restore Praxis.slnx
dotnet test Praxis.Tests/Praxis.Tests.csproj -c Release -v minimal
dotnet test Praxis.slnx -c Release -v minimal
```

プラットフォーム別ビルド:

```bash
# Windows
dotnet build Praxis/Praxis.csproj -c Release -f net10.0-windows10.0.19041.0 -v minimal

# macOS (Mac Catalyst)
dotnet build Praxis/Praxis.csproj -c Release -f net10.0-maccatalyst -v minimal -p:EnableCodeSigning=false -p:CodesignRequireProvisioningProfile=false
```

- コミット前は **Release** 構成を優先すること。CI も Release で走る。
- ワークフローを変えるなら、実際に `.github/workflows/ci.yml` のコマンドも確認すること。
- SDK / feed 周りを変えるときは `global.json`、`nuget.config`、チェックイン済み workflow コマンドの整合を保つこと。
- `dotnet` や MAUI workload が使えない場合は、その旨を明示すること。

### コードベース検索ルール

このリポジトリでは **`cdidx`** を使い、事前構築済み SQLite インデックス（`.cdidx/codeindex.db`）で高速コード検索を行います。
コードを検索する際は `find`、`grep`、`rg`、`ls -R` ではなく **このデータベースを検索** してください。
追跡対象の `.claude/settings.json` では `rg` / `grep` / `find` 系のシェル検索を拒否し、Claude セッションが既定で `cdidx` を使うように固定しています。

これらのルールは cdidx v1.21.0 の "Setup: Add AI agent search rules" テンプレートに追従しています。`cdidx` の挙動は upstream を正本としてください。

#### セットアップ

まず `cdidx` が利用可能か確認してください:

```bash
cdidx --version
```

AI クライアント側でローカル MCP 設定を持てるなら、まず `cdidx` を stdio MCP サーバーとして登録し、通常の調査では shell 実行より構造化ツールを優先します:

Claude Code（`.claude/settings.json` または `.mcp.json`） / Codex CLI（`codex.json` または `~/.codex/config.json`）:

```json
{
  "mcpServers": {
    "cdidx": {
      "command": "cdidx",
      "args": ["mcp", "--db", ".cdidx/codeindex.db"]
    }
  }
}
```

このリポジトリでは、追跡対象の Claude Code プロジェクト設定もこの方針に合わせ、CLI フォールバックだけでなく `cdidx` MCP のフル機能を使える状態を維持してください。

**見つからない場合**、インストールしてください:

```bash
# .NET 不要 — self-contained バイナリをダウンロード
curl -fsSL https://raw.githubusercontent.com/Widthdom/CodeIndex/main/install.sh | bash
```

または .NET 8+ SDK がある場合:

```bash
dotnet tool install -g cdidx
```

**すでにインストール済みの場合**、再インストールやバージョン切り替えは明示バージョン指定で行ってください:

```bash
curl -fsSL https://raw.githubusercontent.com/Widthdom/CodeIndex/vX.Y.Z/install.sh | bash -s -- vX.Y.Z
```

引数なしワンライナーの再実行も常に latest release を対象にします。installer は最新 tag を解決し、現在の健全 install がその最新版と一致している場合だけ download を skip します。壊れた `v0.0.0` install や、同版でも必須隣接資産が欠けている場合は再インストール対象です。

インストールに失敗した場合（ネットワーク不通、未対応プラットフォーム等）は、データベースが構築済みであれば下の **直接 SQL フォールバック** で `sqlite3` から `.cdidx/codeindex.db` を検索できます。`cdidx` も `sqlite3` も利用できない場合は、Claude Code の組み込み `Grep` / `Glob` ツール（または使用ハーネスの同等機能）を使ってください。Claude Code セッション内では shell の `rg` / `grep` / `find` やグローバル `cdidx` にフォールバックしないでください。リポジトリ追跡の deny リストで塞がれている可能性があり、迂回すると古いバイナリ由来のバグを隠すおそれがあります。

検索を始める前に、インデックスが現在の workspace と一致しているか確認してください:

```bash
cdidx status --check --json
```

終了コード `0` かつ `index_matches_workspace: true` なら再インデックス不要です。それ以外ならインデックスを最新化してください:

```bash
cdidx .   # インクリメンタル更新（未変更ファイルはスキップ）
```

#### インデックスの最新化

ファイルを編集したら、検索結果を正確に保つためにデータベースを更新してください:

```bash
cdidx . --files path/to/changed_file.cs   # 触ったファイルだけ更新
cdidx . --commits HEAD                     # 直前コミットの変更ファイルを更新
cdidx . --commits abc123                   # 特定のコミットハッシュも指定可能
cdidx .                                    # フルインクリメンタル更新（未変更はスキップ）
```

**ルール: ソースを修正したら、次の検索の前に `cdidx status --check --json` を実行し、差分が報告された場合は上記のいずれかを実行する。**
`git reset`、`git rebase`、`git commit --amend`、`git switch`、`git merge` のように checkout 自体が変わった後は、stale path も掃除するため `cdidx .` を優先してください。

スコープ更新後に `cdidx status --json` が `DEGRADED` を報告したら、graph や validation 系を信用する前にもう一度 `cdidx .` を走らせてください。

#### クエリ戦略

- 検索結果の正しさが重要なときは、まず `status --check --json` で鮮度を確認する。インデックスが workspace と一致していなければ、symbol / graph 結果を信頼する前に `cdidx .` を実行する。全体像の把握には `map` / `map --json` を使い、言語、モジュール、entrypoint 候補、活動量の高い領域を先に見る。
- `--lang` に渡す正式名と現在の symbol / graph 対応状況は `languages` を正とする。プロンプトやエージェント向け手順に言語別抽出仕様を細かく固定しない。対応範囲は更新されるため、必要な場所では CLI が返す `graph_supported`、`graph_support_reason`、関連 trust metadata を読む。
- 候補シンボル名がある場合は、まず `symbols` で候補を固める。対象が決まったら `--exact` を付ける。1 ファイルの構造だけ見たいときは `outline`、定義・参照・caller・callee のまとまった文脈が欲しいときは `inspect` を使う。
- 実装本文が必要なら `definition --body` を使い、その後に graph 対応言語では `references`、`callers`、`callees`、`impact` を使う。候補が解決済みなら `--exact` を付け、`Run` が `RunAsync` や `RunImpact` に広がらないようにする。graph fallback や degraded metadata は信頼度の情報として扱う。
- 生テキスト、コメント、文字列、オプション名、生成コード、または現在の `languages` 出力で構造化 graph 対応がない言語には `search` を使う。記号を多く含む literal には `--exact-substring` を、`NEAR` や `OR` などの FTS5 構文を意図して使う場合だけ `--fts` を使う。
- 広い検索は早い段階で `--path <text>`、繰り返し指定できる `--exclude-path <text>`、テストが目的でない場合の `--exclude-tests` で絞る。生成・minified・transpiled などノイズの大きいファイルでは `--snippet-lines <n>` と `--max-line-width <n>` で payload を小さくする。
- 候補パスの把握には `files`、既知ファイル内の再探索には `find`、必要行だけ読むときは `excerpt` を使い、ファイル全体を開かない。
- ファイル単位の影響確認には `deps --reverse`、callable symbol の波及確認には `impact`、潜在的な未使用定義には `unused`、中心的なシンボルの把握には `hotspots` を使う。これらは現在の graph 対応とインデックス鮮度に依存するため、`languages` と `status --check --json` を併用する。
- 最近の変更に絞るときは `files --since <datetime>` または `search --since <datetime>`、インデックス対象の事前確認には `index --dry-run`、大きな結果を取る前の見積もりには `--count` を使う。
- BOM、null byte、U+FFFD、改行混在などエンコーディング系の疑いがあるときは `validate` を使う。
- `cdidx` のバグや改善アイデアを見つけたら、https://github.com/Widthdom/CodeIndex/issues に、実際の挙動・期待する挙動・実行コマンドを書いて issue を作成してください。

#### CLI 例

```bash
cdidx status --check --json
cdidx map --path Praxis --exclude-tests --json
cdidx inspect "MainViewModel" --lang csharp --exact --exclude-tests
cdidx symbols --lang csharp --name MainViewModel --exact
cdidx definition "MainViewModel" --lang csharp --exact --body
cdidx search "keyword" --path Praxis --exclude-tests --snippet-lines 6 --max-line-width 160
cdidx search "Run();" --exact-substring --path Praxis
cdidx callers "ApplyButtonDiffAsync" --lang csharp --exact --exclude-tests
cdidx callees "ReloadButtonsAsync" --lang csharp --exact --exclude-tests
cdidx impact "ReloadButtonsAsync" --lang csharp --exact --exclude-tests --json
cdidx deps --path Praxis/Services/SqliteAppRepository.cs --reverse --json
cdidx hotspots --lang csharp --limit 20 --json
cdidx unused --lang csharp --exclude-tests --json
cdidx find "SemaphoreSlim" --path Praxis/Services/SqliteAppRepository.cs --after 2
cdidx excerpt Praxis/Services/SqliteAppRepository.cs --start 1 --end 80
cdidx outline Praxis/MainPage.EditorAndInput.cs --json
cdidx files --lang csharp --path Praxis.Core
cdidx files --since 2026-01-01T00:00:00Z --path Praxis
cdidx languages --json
cdidx validate
cdidx index --dry-run
cdidx backfill-fold
git log --oneline -- path/to/file
```

#### MCP ワークフロー

現在の AI クライアントで `cdidx` MCP が使えるなら、shell で `cdidx` を叩く前に構造化ツールを優先する:

- `map` でリポジトリ全体を把握
- 1 シンボルの詳細調査は `analyze_symbol`（または `inspect`）で、定義・参照・caller・callee・ファイル情報・graph metadata をまとめて取得
- `search`、`definition`、`references`、`callers`、`callees`、`symbols`、`files`、`find_in_file`、`excerpt`、`outline`、`deps`、`impact_analysis`、`unused_symbols`、`symbol_hotspots`、`status`、`languages`、`validate`、`ping`、`index`、`backfill_fold`
- 小さな read-only クエリをまとめたいときは `batch_query`
- `cdidx` 自体のシンボル抽出漏れ、graph 未対応判定の誤り、CLI/MCP の欠落に気づいたときは `suggest_improvement` で上流にフィードバックする

すべての MCP ツールは `annotations`（`readOnlyHint`、`destructiveHint`、`idempotentHint`、`openWorldHint`）を持つため、AI クライアントは安全な read-only クエリをユーザー確認なしで自動承認できます。

ツール結果は `structuredContent` に構造化 JSON、`content` に短い要約テキストを含みます。graph 系 MCP ツール（`references`、`callers`、`callees`）は言語フィルタが指定されたとき `graph_language`、`graph_supported`、`graph_support_reason` を返すので、未対応言語と純粋な 0 件結果を区別できます。

`suggest_improvement` を使う前に既存 Issue を検索して重複を避けてください。`CDIDX_GITHUB_TOKEN` が設定されていれば upstream Issue 化される可能性があり、未設定なら `.cdidx/suggestions.json` にローカル保存されます。

SQL フォールバックは MCP と CLI のどちらも使えない / 壊れているセッションだけに限定すること。

#### 変更内容ごとの検索ルール

- **UI / 画面挙動**
  - `Praxis/MainPage.xaml`、`Praxis/MainPage*.cs`、`Praxis/Platforms/**/Handlers/` をセットで検索する。
  - 多くの挙動は `MainPage.PointerAndSelection.cs`、`MainPage.FocusAndContext.cs`、`MainPage.EditorAndInput.cs`、`MainPage.ShortcutsAndConflict.cs`、`MainPage.WindowsInput.cs`、`MainPage.MacCatalystBehavior.cs`、`MainPage.ViewModelEvents.cs`、各 field partial に分散している。
- **ViewModel ワークフロー**
  - `Praxis/ViewModels/MainViewModel*.cs`、関連サービス、`Praxis.Tests/MainViewModelWorkflowIntegrationTests.cs` を見る。
- **Core ロジック / ポリシー**
  - まず `Praxis.Core/Logic/` を検索し、その呼び出し元 UI/handler と対応テストへ広げる。
- **SQLite / スキーマ / ストレージ / ログ**
  - `Praxis/Services/SqliteAppRepository.cs`、`Praxis/Services/DbErrorLogger.cs`、`Praxis/Services/CrashFileLogger.cs`、`docs/DATABASE_SCHEMA.md`、`Praxis.Tests/DatabaseSchemaVersionPolicyTests.cs`、`Praxis.Tests/AppStoragePathLayoutResolverTests.cs`、`Praxis.Tests/DbErrorLoggerTests.cs`、`Praxis.Tests/CrashFileLoggerTests.cs` を見る。
- **CI / リリース / カバレッジ**
  - `.github/workflows/*.yml`、`Praxis.Tests/CiCoverageWorkflowPolicyTests.cs`、`README.md`、`docs/DEVELOPER_GUIDE.md`、`docs/TESTING_GUIDE.md`、`version.json` を検索する。
- **partial class 構造**
  - field 移動、XAML 名変更、モーダル欄順変更の前に、実装ファイルだけでなく `Praxis.Tests/MainPageStructureTests.cs` も見る。
- **linked-source テスト**
  - テストがアプリ層ソースを直接コンパイルしている場合があるので、`Praxis.Tests/Praxis.Tests.csproj` を見て linked file を確認する。

#### 直接 SQL フォールバック

`cdidx` が使えず、`.cdidx/codeindex.db` だけ存在する場合に限る。クエリには `sqlite3` が必要。これは raw text / symbol を最低限覗くためのフォールバックであり、call graph、freshness metadata、exact-name semantics、scoped snippet、`impact`、`unused`、`hotspots` には `cdidx` を優先する:

```sql
-- 全文検索
SELECT f.path, c.start_line, c.content
FROM fts_chunks fc
JOIN chunks c ON c.id = fc.rowid
JOIN files f ON f.id = c.file_id
WHERE fts_chunks MATCH 'keyword'
LIMIT 20;
```

```sql
-- シンボル名で検索
SELECT f.path, s.name, s.line
FROM symbols s
JOIN files f ON f.id = s.file_id
WHERE s.name LIKE '%MainViewModel%'
ORDER BY f.path, s.line
LIMIT 20;
```

`sqlite3` も使えない場合は、Claude Code の組み込み `Grep` / `Glob` ツール（または使用ハーネスの同等機能）を使うこと。shell の `rg` / `grep` / `find` にはフォールバックしない（deny リストで塞がれているため）。

#### 編集前に最低限見るべき面

挙動を変える前に、少なくとも次を検索すること:

1. 実装箇所
2. ガードテスト
3. 関連ドキュメント
4. CI / リリース挙動が絡むなら workflow / versioning ファイル
5. そのファイルや周辺の最近の commit 履歴

これら 5 面を見ずに「ローカルな変更」と判断しないこと。

### 横断的一貫性ルール

**動作を変えたら、関連成果物を同じコミットで全部更新すること。**

| 変更したもの | 同時に更新すべきもの |
|---|---|
| ユーザー向け挙動、ショートカット、既定値、対応プラットフォーム表記 | `README.md`、`CHANGELOG.md`、関連テスト |
| `MainPage.xaml` または `MainPage*.cs` の挙動/構造 | `Praxis.Tests/MainPageStructureTests.cs`、`docs/DEVELOPER_GUIDE.md`、必要なら `README.md` |
| `MainViewModel*.cs` のワークフロー、候補表示、Undo/Redo、同期挙動 | `Praxis.Tests/MainViewModelWorkflowIntegrationTests.cs`、関連ポリシーテスト、`CHANGELOG.md`、必要なら `README.md` |
| `Praxis.Core/Logic/*` のポリシー/Resolver | 対応する `Praxis.Tests/*Tests.cs`、呼び出し元、外部仕様に影響するなら docs |
| SQLite スキーマ、マイグレーション、保存先、保持期間、エラーログ挙動 | `docs/DATABASE_SCHEMA.md`、`Praxis.Tests/DatabaseSchemaVersionPolicyTests.cs`、`Praxis.Tests/AppStoragePathLayoutResolverTests.cs`、ログ関連テスト、`CHANGELOG.md` |
| DI サービス、インターフェース、ViewModel/Service 配線 | `Praxis/MauiProgram.cs`、必要なら `Praxis.Tests/Praxis.Tests.csproj` の linked-source 設定、`docs/DEVELOPER_GUIDE.md` |
| CI コマンド、カバレッジ成果物名/パス、workflow 名 | `.github/workflows/*.yml`、`Praxis.Tests/CiCoverageWorkflowPolicyTests.cs`、README のバッジ/リンク、関連 docs |
| バージョニングやリリースの仕組み | `version.json`、`CHANGELOG.md`、必要なら README / docs |
| ブランディングやアイコン資産名、パッケージング資産 | `Praxis/Praxis.csproj`、`docs/branding/README.md`、必要なら関連 docs/tests |
| リポジトリ運用ルールそのもの | `AGENT_GUIDE.md` |

### アーキテクチャ

#### App / Core / Tests の分離

- `Praxis/` は MAUI UI、ハンドラ、サービス、ViewModel
- `Praxis.Core/` は純粋ロジック、ポリシー、Formatter、Resolver、モデル
- `Praxis.Tests/` は core を直接テストしつつ、選択された app-layer ソースを linked file としてコンパイルして統合寄りに検証する

#### MainPage の partial 構造は意図的

`MainPage` は次のように分割されている:

- field 専用: `MainPage.Fields.*.cs`
- state 専用: `MainPage.InteractionState.cs`
- 挙動別: `MainPage.PointerAndSelection.cs`、`MainPage.FocusAndContext.cs`、`MainPage.EditorAndInput.cs`、`MainPage.ModalEditor.cs`、`MainPage.ViewModelEvents.cs`、`MainPage.StatusAndTheme.cs`、`MainPage.DockAndQuickLook.cs`、`MainPage.WindowsInput.cs`、`MainPage.ShortcutsAndConflict.cs`、`MainPage.MacCatalystBehavior.cs`、`MainPage.LayoutUtilities.cs`

この境界の一部は `Praxis.Tests/MainPageStructureTests.cs` が検証している。
XAML 名変更、field 移動、モーダル欄順変更をしたら、同じコミットで必ずそのテストを更新すること。

#### MainViewModel の partial 構造

`MainViewModel` は以下に分割されている:

- `MainViewModel.cs`
- `MainViewModel.Actions.cs`
- `MainViewModel.CommandSuggestions.cs`

この層の変更は、workflow integration test や command/focus 系 policy test への波及が多い。

#### DI 登録

`Praxis/MauiProgram.cs` が DI の composition root。
repository、logger、各種 service、`MainViewModel`、`MainPage` はここで singleton 登録される。
サービス追加や配線変更をしたら `MauiProgram.cs` と関連 docs/tests を更新すること。

#### SQLite リポジトリとスキーマ移行

`Praxis/Services/SqliteAppRepository.cs` が永続化の中心であり、

- `SemaphoreSlim` による保護
- メモリキャッシュと command cache
- `PRAGMA user_version`
- 段階的 migration
- 複数ウィンドウ同期や競合検出のための明示的 reload

を担う。

スキーマや保存先変更は単なる内部実装ではなく、`docs/DATABASE_SCHEMA.md` や schema/path テストと必ず同期させること。

#### linked-source テストモデル

`Praxis.Tests/Praxis.Tests.csproj` は、アプリ層の一部ソースを linked file として直接取り込んでいる。
linked されているファイルを分割・改名したり、テスト対象にしたい app-layer ファイルを増やした場合は、同一コミットでこの csproj を更新すること。

### テスト基盤

- テストフレームワーク: xUnit
- カバレッジ収集: `coverlet.collector`
- workflow ガード: `Praxis.Tests/CiCoverageWorkflowPolicyTests.cs`
- アサーションは xUnit 組み込みを優先し、簡単なテストダブル/スタブで十分なケースではそれを使う
- UI 非依存ロジックは可能な限り `Praxis.Core` に置く
- プラットフォームポリシーは runtime 判定ではなく明示的入力で検証する
- `docs/TESTING_GUIDE.md` に書かれているテスト意図を変えたら、その文書も更新する

### CI / GitHub Actions

- workflow:
  - `.github/workflows/ci.yml`
  - `.github/workflows/delivery.yml`
  - `.github/workflows/codeql.yml`（意図的に無効化したプレースホルダ。実際の CodeQL は GitHub default setup を使う）
- `ci.yml` は test + coverage、Windows build、Mac Catalyst build を実行する
- `delivery.yml` は `workflow_dispatch` と `v*` タグ push で動く

#### 過去の失敗から得た重要ルール

1. **checkout を shallow clone に戻さないこと**。Nerdbank.GitVersioning が version height を計算するため `fetch-depth: 0` が必要。
2. **workflow で `TargetFrameworks` を安易に上書きしないこと**。過去に脆い `-p:TargetFrameworks=...` を入れては戻す修復が続いた。特別な理由がない限り、普通の `--framework` / `-f` を優先する。
3. **workload install コマンドはチェックイン済み workflow に揃えること**。現在の CI / delivery は `dotnet workload install maui` を使っており、`--skip-manifest-update` などの旧フラグを安易に戻さない。
4. **.NET 10 restore 設定は `global.json`・workflow・`nuget.config` で揃えること**。現在は GA SDK（`10.0.x` / `dotnet-quality: ga`）と、既定 NuGet source を残したまま `dotnet10` / `dotnet10-transport` feed を追加する構成。
5. **Mac runner では `sudo xcodebuild -runFirstLaunch` が必要**。Mac Catalyst build/publish 前に入れること。
6. **Xcode 互換判定は消さないこと**。必要バージョン未満では Mac Catalyst 処理を skip する設計になっている。
7. **Windows 配布 publish は意図的に RID を空にしている**。Windows 側の RID を戻すと Mono runtime 依存の packaging failure が再発しうる。
8. **coverage artifact 名や path を変えるなら**、同じコミットで `Praxis.Tests/CiCoverageWorkflowPolicyTests.cs` を更新すること。
9. **`codeql.yml` は意図的なプレースホルダ**。重複する custom CodeQL を勝手に復活させないこと。

### Git / リリースルール

- ユーザーの明示的な許可なく `git tag` を作らないこと。タグ push は `delivery.yml` を起動する。
- バージョン番号は `version.json` と GitVersion から来る。`Praxis/Praxis.csproj` は `$(Version)` / `$(GitVersionHeight)` を使い続けること。
- コミットメッセージは、特段の指示がなければ簡潔な英語を優先する。

#### バージョンアップ手順

意図して新バージョンを出すときは:

1. `version.json` を更新
2. `CHANGELOG.md` の `[Unreleased]` から新バージョン見出しへ項目を移動
3. `CHANGELOG.md` 末尾の compare link を更新
4. リリース挙動が変わるなら workflow / docs も確認

### よくある落とし穴

1. **GitVersion と shallow fetch**: `fetch-depth: 0` を外すと CI / packaging で version height 計算が壊れる。
2. **Mac Catalyst runner 初期化**: `xcodebuild -runFirstLaunch` を抜くと、アプリ build 本体より前で失敗することがある。
3. **Xcode 下限の変動**: 互換判定は理由があって入っている。workflow を緑に見せるためだけに消さない。
4. **workflow 引数の揺れ**: `dotnet restore/build` の framework 選択は過去に何度も失敗している。MSBuild property override は保守的に扱う。
5. **.NET 10 restore / feed のズレ**: preview SDK quality に戻したり、既定 NuGet source を消したり、追加 `dotnet10` feed を外したりする変更は、具体的な restore 障害の分析なしに行わない。
6. **Windows publish RID のズレ**: Windows 配布は現状 RID なしが前提で、戻すと Mono runtime 依存失敗が再発しうる。
7. **`MainPage` のファイル漂流**: field は `MainPage.Fields.*.cs`、挙動は `MainPage.PointerAndSelection.cs` や `MainPage.FocusAndContext.cs` を含む責務別 partial に置き、`MainPage.xaml.cs` に戻さない。
8. **ASCII 強制は modal 限定**: `ModalCommandEntry` は ASCII 強制 opt-in だが、メイン command 欄は意図的にそうしていない。
9. **linked-source テストのズレ**: app-layer ソースの改名/分割時に `Praxis.Tests/Praxis.Tests.csproj` を同期しないとテストが壊れる。
10. **クラッシュログの順序**: 先に `CrashFileLogger`、後で DB キュー投入。順序を逆転させるとクラッシュ耐性が下がる。
11. **スキーマ変更の docs/test 更新漏れ**: `docs/DATABASE_SCHEMA.md`、migration テスト、path/layout テストは一緒に更新すること。
12. **workflow ガードの更新漏れ**: coverage workflow を変えたら `CiCoverageWorkflowPolicyTests` を更新すること。
13. **Windows アイコンのファイル名**: `Resources/AppIcon/appiconfg_windows.svg` のアンダースコア名を維持する。過去にハイフン名でビルド/パッケージング問題が出た。
14. **Mac Catalyst 用 custom target は安定化のためにある**: signing/copy/intermediate assembly 関連 target は気軽に消さない。実問題に対する対策として入っている。

### コミットごとのチェックリスト

以下は **各コミットごと** に評価すること。PR 前にまとめてではなく、コミット単位で確認する:

1. **テスト** - 既存テストは壊れていないか。新規テストは必要か。Release で実行したか。
2. **README.md** - ユーザー向け挙動、既定値、ショートカット、対応プラットフォーム、セットアップ手順は変わったか。
3. **CHANGELOG.md** - この挙動変更/修正/文書変更/workflow 変更は記録すべきか。
4. **docs/DEVELOPER_GUIDE.md** - アーキテクチャ、配線、ハンドラ、partial 構造、プラットフォーム挙動は変わったか。
5. **docs/TESTING_GUIDE.md** - テスト追加/改名/削除、あるいはテスト意図の実質変更はあったか。
6. **docs/DATABASE_SCHEMA.md** - スキーマ、保存先、保持期間、エラーログ挙動は変わったか。
7. **AGENT_GUIDE.md** - リポジトリ運用ルール、リリースルール、アーキテクチャルール、 recurring gotcha は変わったか。
8. **MainPage 構造ガード** - `MainPage.xaml` / `MainPage*.cs` を触ったなら、`Praxis.Tests/MainPageStructureTests.cs` の前提はまだ正しいか。
9. **linked-source テスト** - テストで使う app-layer ソースの分割/改名/追加をしたなら `Praxis.Tests/Praxis.Tests.csproj` を更新したか。
10. **DI 登録** - service / interface / ViewModel 依存を増やしたなら `Praxis/MauiProgram.cs` を更新したか。
11. **ログ / クラッシュ安全性** - ログ周りを触ったなら `CrashFileLoggerTests` / `DbErrorLoggerTests` が挙動を守れているか。file-first logging を維持しているか。
12. **スキーマ / パス系ガード** - 永続化や保存先を触ったなら `DatabaseSchemaVersionPolicyTests` / `AppStoragePathLayoutResolverTests` を更新したか。
13. **CI / workflow** - `.github/workflows/*.yml` を触ったなら `CiCoverageWorkflowPolicyTests` と関連 docs / バッジを更新したか。
14. **バージョン / リリース** - versioning や配布に影響するなら `version.json`、`CHANGELOG.md`、必要な release note / compare link を更新したか。
