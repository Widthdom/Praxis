# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [Unreleased]

### Added
- Per-button inverted colors with auto DB schema migration (v1 → v2)
- DB-backed error logging (ERROR + INFO levels) with 30-day retention (`ErrorLog` table, schema v3 → v4)
- INFO-level contextual tracing for key user actions: button/command execution, editor open/save/cancel/delete, theme change, undo/redo, conflict resolution, window close
- Undo/Redo for button mutations (move/edit/delete): Ctrl+Z / Ctrl+Y on Windows, Command+Z / Command+Shift+Z on macOS
- Quick Look preview on button hover (Command / Tool / Arguments / Clip Word / Note)
- SQLite schema versioning via `PRAGMA user_version` with sequential auto-migration
- Cross-window sync via `FileSystemWatcher` signal file (`buttons.sync`) with instance-id self-filter
- Conflict detection dialog for concurrent multi-window edits (optimistic locking via `Version` column)
- Middle-click and right-click interactions on command suggestion rows
- Command suggestion debounce increased to 400 ms to reduce noise during fast typing
- First `Down` key selects first candidate; popup no longer auto-selects on open
- CI coverage collection and Cobertura artifact upload (GitHub Actions)
- UNC path fallback via `explorer.exe` on Windows so auth prompt can appear before existence checks succeed
- Dock horizontal scrollbar shown only while hovering the Dock area and horizontal overflow exists
- Invert-theme label is tappable to toggle the checkbox (not just the checkbox itself)

### Fixed
- Clear button focus restore stability after tap (immediate attempt + short delayed retry)
- Clear-button X glyph vertical centering on Windows
- Command suggestion colors stay theme-synced during live theme switch
- `CommandEntry` / `SearchEntry`: lowercase letters no longer converted to uppercase
- IME / ASCII enforcement in command entry:
  - Windows: `InputScopeNameValue.AlphanumericHalfWidth` on focus + `imm32` nudge (immediate + one delayed retry)
  - macOS: `AsciiInputFilter` + `setMarkedText` / `insertText` blocking, detached on app deactivation
- Modal `Command` IME reasserted while focused on Windows (prevents manual IME-mode switching)
- macOS: ASCII input source enforced only while field is first responder in active key window
- macOS: after "Command not found", focus is restored to command input for immediate retry
- macOS: ILLink input loss in clear-button path prevented via intermediate assembly copy
- Windows: modal/conflict focus fallback uses 2-stage retry so Esc and Ctrl+S remain responsive
- Windows: `InputScope` `ArgumentException` (E_RUNTIME_SETVALUE) handled gracefully via one-way unsupported flag
- Windows: Ctrl+Z/Y undo granularity preserved; `TextChanging` rewrite disabled for command input
- Single-window false-positive conflict dialog eliminated (instance-id self-filter in sync signal)
- "Command not found" shown as error flash (red) rather than neutral status
- Editor modal re-focus on macOS after returning from another app

### Changed
- Command and Button Text field order swapped in editor modal (Command first)
- UI button font size unified to 12 across all platforms
- UI button padding set to 0 in placement area and Dock
- New button icon changed from plain `+` to wireframe hex logo (outer hexagon · inscribed circle · inner hexagon · center `+`)
- `MainPage` refactored into 12 concern-based partial classes (`PointerAndSelection`, `FocusAndContext`, `EditorAndInput`, `ShortcutsAndConflict`, `MacCatalystBehavior`, `LayoutUtilities`, and field partials)
- Platform preprocessor blocks consolidated across `MainPage` field files and `MauiProgram` handler registration
- Redundant `using` directives removed and `using` order normalized
- `MainViewModel` and its partial classes annotated with `LogInfo` calls for key lifecycle events
