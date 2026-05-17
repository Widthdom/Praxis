# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [Unreleased]

## [2.0.4] - 2026-05-18

### Fixed
- Launcher and Dock button tooltip `ButtonText` rows now bind to the launcher label value so the row no longer appears blank.

## [2.0.3] - 2026-05-17

### Changed
- Updated the AI-agent `cdidx` code-search rules to match CodeIndex v1.22.3, including `status --check --json` as the freshness gate, scoped index refreshes, exact search/name options, and `inspect` / `find` / `excerpt` / `outline` usage guidance.

### Fixed
- Launcher and Dock button tooltips now show `ButtonText` above `Command`, making the display label visible alongside the execution details.

## [2.0.2] - 2026-05-17

### Fixed
- macOS title-bar double-click normal maximize now restores the previous window bounds when double-clicked again, including the path where Avalonia reports the maximized geometry as `WindowState.Normal`.
- macOS now keeps a dedicated title-bar drag surface active after normal maximize, so double-click restore works across the title-bar center instead of only a narrow top strip.
- Title-bar double-click handling now also honors Avalonia's click count, making macOS maximize/restore less sensitive to small pointer movement between clicks.

## [2.0.1] - 2026-05-16

### Fixed
- Windows top-edge hit testing now prioritizes native vertical resize over the custom caption drag region, so grabbing the top window edge can resize the Praxis window vertically
- Avalonia Windows and macOS windows now restore focus to the Command field, with the text selected, when the Praxis window becomes active and no editor/conflict dialog is open
- The Avalonia placement surface now grows from the visible launcher-button bounds instead of keeping a fixed 1600x880 extent, so Windows and macOS no longer show placement scrollbars while all visible buttons fit in the current viewport

## [2.0.0] - 2026-05-14

### Added
- Started the v2.0.0 Avalonia migration branch with strict-MVVM Core model structure, an Avalonia desktop shell, and a migration plan covering platform abstractions, pseudo-acrylic UI direction, DB compatibility risks, and future Linux readiness
- Added `Praxis.Data` with SQLite launcher-button persistence, platform-aware app data path resolution, v1 table-name compatibility, `praxis.db3` / existing `praxis.db` file support, and schema migration to version 5 for `ColorKey`, `ToolTip`, `LastExecutedAtUtc`, and `SortOrder`
- Added a desktop launcher execution service for direct command execution and default-app opening on Windows, macOS, and Linux-ready `xdg-open` paths
- Reintroduced command-input execution, command suggestions, persisted recent Dock order, button delete/move repository operations, and launch-log writes through Core/Data services
- Added app-local file-backed launcher-button state sync through `IStateSyncNotifier` / `FileStateSyncNotifier`, including external reload deferral while the editor is open
- Added Avalonia window icon resources, a dedicated draggable chrome row, double-click maximize on the drag area, and custom minimize/maximize/close caption buttons

### Changed
- Switched the solution, CI, delivery, README, and developer/test docs to the Avalonia desktop app as the active runtime target; MAUI workloads are no longer required
- Changed Avalonia startup to load launcher buttons from SQLite through `MainModel` and `ILauncherButtonRepository` instead of preview-only launcher data
- Changed `MainWindow` code-behind to load XAML directly instead of keeping a generated-style `InitializeComponent` wrapper

### Removed
- Removed the former .NET MAUI app project and its MAUI app-layer linked-source tests so v2 development starts from the Avalonia shell and Core model/service contracts

### Tests
- Added focused xUnit coverage for `praxis.db3` / `praxis.db` storage selection, SQLite v4-to-v5 launcher schema migration, and v2 launcher-field persistence
- Added focused xUnit coverage for command suggestions/execution, persisted Dock order, launch logs, button deletion, and snapped move persistence
- Added focused xUnit coverage for state-sync payload parsing, successful save notifications, external reload, and editor conflict detection when another window updates or deletes a button
- Added source guards for direct XAML loading, embedded icon assets, draggable chrome, and caption button wiring

### Fixed
- Context-menu Delete now removes the full selected launcher-button group when invoked from a selected button, while unselected button Delete still removes only the clicked button
- Windows Avalonia shell now uses a transparent-background app icon for taskbar and jump-list surfaces, tightens the custom caption buttons to the window top edge, and strengthens the rounded pseudo-acrylic shell corners
- Windows caption buttons use the OS chrome path for native minimize/maximize/restore animations, caption tooltips avoid clipped text, the pseudo-acrylic shell is more translucent, and custom title-bar dragging supports edge snap
- The Avalonia editor modal now exposes the button `Command` field, placement and Dock tooltips omit duplicate `ButtonText`, Windows caption hit testing uses the native title-bar path for Aero Snap, and small window icons are emitted as alpha DIB ICO frames
- Windows placement-area and Dock button tooltips now use fixed button-edge placement instead of pointer placement, so the tooltip does not appear under the cursor and intercept the first click
- Light-mode Windows caption buttons now use darker glyphs with a stronger but still neutral hover background
- Editor focus now places the caret at the end of `ButtonText` for normal edits, including context-menu Edit, and selects all `ButtonText` only for new buttons
- New buttons now default to fixed `New` button text with an empty command instead of numbered placeholder text and commands
- Windows jump-list relaunch icon metadata now points at the transparent small icon resource so the taskbar right-click menu can avoid the stale white-background executable icon
- Windows rounded corners now rely on DWM-managed clipping instead of a GDI region, smoothing the light-mode window corners
- Removed the Windows-only editor `ButtonText` focus stabilization experiments after they proved unreliable and could steal focus back from other modal controls; the remaining Windows caret-at-start race is now documented in the developer guide
- Windows shell corners now avoid drawing an inner rounded shell on top of the DWM-managed window corner, preventing doubled corner arcs and malformed snapped-window corners
- Windows now hides the custom Avalonia caption-button stack and clears the runtime title when using the OS chrome path for animated minimize/maximize/restore, avoiding duplicate caption buttons and the extra title text
- Windows hides Avalonia's drawn full-screen caption glyph while keeping the OS chrome path, so minimize/maximize/close remain visible without the extra button beside minimize
- `Ctrl+Shift+L`, `Ctrl+Shift+D`, and `Ctrl+Shift+H` now explicitly switch Avalonia between Light, Dark, and System theme modes
- `Ctrl+Shift+H` now returns Avalonia to the system theme instead of leaving an in-between fixed palette; window light/dark classes and theme-dependent bindings are refreshed from the actual OS-selected theme
- Windows editor `ButtonText` now uses the shared initial-focus path instead of pointer hit-test suppression or repeated caret/selection timers
- Windows launcher and Dock button labels are optically lowered to sit at the vertical center, and the pseudo-acrylic background uses a softer low-contrast blur-style sheen for environments where true window transparency is unavailable
- Single-line Avalonia text boxes now keep the caret visible at the right edge by allowing hidden horizontal scrolling instead of disabling horizontal scroll behavior, with modest extra right padding for Command/Search clear buttons
- README and developer/database/testing docs now describe the current Avalonia editing, drag, theme-switching, and file-backed button sync behavior instead of pointing at removed migration-plan notes or calling those flows unimplemented

## [1.2.0] - 2026-05-11

### Added
- The `Command` and `Search` text inputs render their placeholder as small SVG-style icons inside the field at the left edge — a `>_` chevron + underscore for `Command`, a magnifying glass for `Search`. The icons fade out once the field has any text, mirroring how the literal placeholder strings used to disappear, and use a muted gray (`Light=#A0A0A0, Dark=#7C7C7C`) so they read as a hint rather than a visible control
- Windows: a custom 30 px title bar with three subtle caption buttons (minimize / maximize-restore / close) replaces the OS-managed title bar chrome. `Microsoft.UI.Xaml.Window.ExtendsContentIntoTitleBar = true` (the canonical WinUI 3 XAML Window property — the `AppWindow.TitleBar.ExtendsContentIntoTitleBar` property left the OS title bar in place under MAUI's WinUI host) combined with `OverlappedPresenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false)` hides both the OS title bar and the system caption buttons while keeping the resize border and Windows 11 rounded corners. `Microsoft.UI.Xaml.Window.SystemBackdrop = null` disables MAUI's default `MicaBackdrop` so the page background (`Light=#F2F2F2 / Dark=#161616` matching `Resources/Styles/Colors.xaml`) fills the entire window with no Mica lavender tint. The drag region is declared via `Microsoft.UI.Input.InputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, ...)` covering the title bar area to the left of the caption buttons, with the rect re-declared on `WindowTitleBar.SizeChanged`, `WindowTitleBarDragRegion.SizeChanged`, `WindowCaptionButtonsStack.SizeChanged`, and `AppWindow.Changed` (`DidPresenterChange` / `DidSizeChange`) so dragging, double-click-to-maximize, snap-to-edge, and Aero Snap keep working at every window size; the buttons themselves call `OverlappedPresenter.Minimize / Maximize / Restore` and `Window.Close` so OS-native min/restore animations stay enabled even with the user's "Animate windows when minimizing and maximizing" Performance Options setting in effect. The maximize glyph swaps between `ChromeMaximize` and `ChromeRestore` (Segoe Fluent Icons) on `AppWindow.Changed`. All three buttons share the same subtle theme-aware tint (`Light=#E0E0E0 / Dark=#3A3A3A` hover, `Light=#D0D0D0 / Dark=#4A4A4A` pressed) so close does not draw the conventional destructive red. Mac Catalyst is unaffected: the title bar row is collapsed to height 0 so the OS title bar continues to own the chrome there
- Windows: backstop measures to soften the OS-painted background during native-side resize lag. `App.CreateWindow` Win32-installs a window class brush via `SetClassLongPtrW(GCLP_HBRBACKGROUND, CreateSolidBrush(...))` matching the page idle color (`Light=#F2F2F2 / Dark=#161616`), so when the OS expands the HWND before WinUI has rendered the new client area, the stretch gap is painted with the page color instead of pure white. `DwmSetWindowAttribute(DWMWA_BORDER_COLOR, DWMWA_CAPTION_COLOR)` are also set to the same color so the DWM-painted resize border / caption strip blend in. The native WinUI root content panel (`nativeWindow.Content`) gets its `Background` brush set to the same color as well so the WinUI render path itself never exposes a white fallback. Together these three measures reduce — but do not entirely eliminate — the white stretching strip on right / bottom edge resize-out (see Known issues below)
- Windows: best-effort reflection-based null-out of MAUI's internal `WindowRootView` title-bar slot. `App.TryNullAppTitleBarRecursive` walks the WinUI visual tree (`Microsoft.Maui.Platform.WindowRootViewContainer` → `Microsoft.Maui.Platform.WindowRootView`) and reflection-clears `_appTitleBar`, `_appTitleBarHeight=0`, `_useCustomAppTitleBar=false`, `_titleBar`, `WindowTitleBarContent=null`, `AppTitleBarTemplate=null`, `AppTitleBarContainer=null`, `AppTitleBarContentControl=null`, plus `WindowTitleBarContentControlVisibility=Collapsed` / `WindowTitleBarContentControlMinHeight=0`. The pass runs on `HandlerChanged`, on `Window.SizeChanged`, and once more from a deferred `DispatcherQueue.TryEnqueue(Low)` callback that also issues a 1-pixel `AppWindow.Resize` + restore so the drag-region transform updates before the user touches the window. This is what makes drag and double-click-to-maximize work on the visible caption-buttons row immediately at startup instead of only after the first manual resize

- Windows: eliminated the ~32 px phantom row above the visible caption-buttons row. Root cause: WinUI 3 `NavigationView`'s default template stamps `Margin="32,0,0,0"` onto its internal `ContentGrid` part whenever `ExtendsContentIntoTitleBar = true`, regardless of `IsTitleBarAutoPaddingEnabled`. `App.ForceNavigationViewContentGridMarginZero` walks the `WindowRootView` visual subtree with `VisualTreeHelper`, finds the named `ContentGrid`, and forces `Margin = Thickness(0)`. Since the template re-stamps the 32 px back during every measure pass (especially after a window resize), `AttachContentGridMarginGuard` also subscribes to the `ContentGrid`'s `LayoutUpdated` event — keyed by a `ConditionalWeakTable` so each element gets at most one handler — and re-zeroes the margin whenever the template restores it. Additionally `App.CreateWindow` now issues a Win32 `SetWindowPos(SWP_FRAMECHANGED)` from a deferred `DispatcherQueue.TryEnqueue(Low)` callback so the OS re-runs non-client hit-testing and the `InputNonClientPointerSource.SetRegionRects` Caption declaration becomes effective immediately at startup (without this, drag and double-click-to-maximize only worked after the user manually invoked a caption button, because the presenter state change was what forced NC re-evaluation). The `[DragRegion]` per-LayoutUpdated log line, the `WRV.Set` success-path logs, and the `WindowRootViewContainer.Properties/Fields` + `WindowRootView.TitleProps/TitleFields` diagnostic dumps that were used to discover the ContentGrid culprit are now removed since the resolution is locked in
- Windows: the dark-themed window no longer shows a near-white DWM-painted outer 1 px border. `App.ApplyWindowsImmersiveDarkMode` calls `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE = 20, BOOL)` so the OS draws the window outline in its Dark-mode chrome variant instead of the Light-mode default. The four theme-dependent chrome attributes (immersive dark mode, WinUI root content background, Win32 class brush, DWM border/caption colors) are now grouped under a single `App.ApplyWindowsThemeChrome(Window)` entry point that resolves the effective theme via `App.ResolveWindowsAppThemeIsDark` — MAUI's `Application.Current.UserAppTheme` wins over the OS `RequestedTheme` so an in-app `Ctrl+Shift+L/D/H` toggle drives the Windows chrome too, which the previous direct read of `Microsoft.UI.Xaml.Application.Current.RequestedTheme` couldn't do (that property mirrors the OS, not the user's in-app choice). `MainPage`'s existing `Application.Current.RequestedThemeChanged` handler now also calls `App.ApplyWindowsThemeChrome(windowsNativeWindow)`, so toggling the theme repaints all four chrome attributes immediately — Mac Catalyst path is unaffected
- Windows: dragging the right or bottom resize handle outward no longer exposes a white strip in the freshly-exposed area. `App.InstallEraseBkgndSubclass` installs a `comctl32!SetWindowSubclass` handler on the HWND that intercepts `WM_ERASEBKGND`, `FillRect`s the entire client rectangle with a GDI `CreateSolidBrush` brush matching the page color (`Light=#F2F2F2 / Dark=#161616`), and returns `(LRESULT)1` so the OS does not re-fill with the default class brush (which on WinUI 3 windows tends to be white regardless of `GCLP_HBRBACKGROUND` overrides — that was why the previous class-brush + DWM-color combo only narrowed the gap rather than closing it). All other window messages pass through to `DefSubclassProc`, so rounded corners, resize borders, NC hit-testing, and WinUI's own input handling stay untouched. `App.UpdateEraseBkgndBrush(isDark)` is wired into `ApplyWindowsThemeChrome` so the brush is recreated with the new color on every theme toggle; the old brush is released via `gdi32!DeleteObject` to avoid a GDI leak. The static `eraseBkgndSubclassDelegate` field keeps the marshalled subclass thunk alive for the lifetime of the HWND

### Changed
- The Edit/Delete context menu, modal editor, conflict dialog, and command-suggestion popup now fade in and out instead of toggling instantly, with cancellation tokens so rapid open/close cycles cannot leave an overlay stuck mid-fade
- The Dock surface uses a shorter bottom-biased footprint so the borderless placement area gains more vertical room while the horizontal scrollbar still has space when visible. The Dock horizontal scrollbar gets a small bottom margin so launcher buttons no longer brush against it, and the Dock scroll content clips at the left/right edges so partially scrolled buttons end in straight edges
- The placement-area and Dock surrounding `Border`s no longer paint their own backgrounds (`BackgroundColor="Transparent"`) and have no visible stroke, so the page background reads through; the launcher buttons themselves carry the only visible fill in those regions
- The status bar at the bottom of the page is borderless (`Stroke="Transparent" StrokeThickness="0"`), keeps its idle background fully transparent so it disappears against the page when not flashing, and centers the status text horizontally
- The placement-area drag-selection rectangle now uses a 1-pixel stroke (down from 2 px) so multi-select feedback is less visually heavy
- Placement-area inverted-theme buttons (`UseInvertedThemeColors=True`) now use `#787878` (light) / `#A0A0A0` (dark) for the selected fill — visibly distinct from the inverted idle `#363636` / `#FFFFFF` while staying inside the monochrome palette
- Edit/Delete context-menu, modal Cancel/Save, and conflict-dialog Reload/Overwrite/Cancel buttons indicate focus via a background tint (`#E6E6E6` light, `#3D3D3D` dark) instead of a focus border ring, keeping the labels stable across focus changes; the unfocused fill falls back to the platform default
- The OS titlebar no longer shows the literal "Praxis" string on either Windows or Mac Catalyst. On Mac Catalyst, the bundle / display-name fallback is overridden through a new `ClearMacWindowTitles` helper in `AppDelegate.OnActivated` that walks every connected `UIWindowScene` and sets `Title=""` plus `Titlebar.TitleVisibility=Hidden`

### Fixed
- Modal-editor Tab traversal now reaches `Cancel` from the `Note` (and `Clip Word`) editor on both Windows and Mac Catalyst, completing the `Note → Cancel → Save → GUID` cycle, and the focused button is now actually visible. The Windows multi-line `TextBox` (`AcceptsReturn=true`) eats `Tab` as a literal character with no `AcceptsTab` override available, so `WindowsTextBox_PreviewKeyDown` intercepts `Tab` / `Shift+Tab` on the modal multi-line editors before the TextBox's built-in `OnKeyDown` runs and dispatches focus to the next/previous modal field via MAUI `VisualElement.Focus()` plus a platform `Control.Focus(FocusState.Keyboard)` assertion. `FocusModalTabTarget` clears the pending `WindowsModalInput_LostFocus` restore (`windowsEditorFocusRestorePending = false`) and stamps a `WindowsModalActionFocusTarget` pseudo-focus field that `HasWindowsEditorModalFocus` honors, so the queued restore cannot steal focus back to `ButtonText` while the platform Button is settling; `ApplyModalActionButtonFocusVisuals` paints the focus tint from the pseudo-focus field on Windows (mirroring the existing Mac pseudo-focus path) so the highlighted button stays visible without waiting for the Button's own `Focused` event, and `WindowsTextBox_GotFocus`, `WindowsTextBox_PointerPressed`, and the action button's own `Focused` event clear the pseudo-focus once native focus settles inside the modal again. Separately, the invisible `ModalInvertThemeCheckBox` (`Opacity=0`, toggled via the sibling Grid+TapGesture) is now `IsTabStop=false`, applied both in `ApplyTabPolicy` and in a new `HandlerChanged` event so the property is set as soon as the platform handler exists. On Mac Catalyst, `ModalFocusTarget.InvertThemeColors` is removed from `ModalFocusOrder` so the cycle skips the CheckBox by design instead of relying on its UIResponder failing to become first responder
- `ButtonFocusVisualPolicy.ResolveBackgroundColorHex` now returns `#C8C8C8` (light) / `#5A5A5A` (dark) instead of `#E6E6E6` / `#3D3D3D` so the focus tint is visibly distinct from the default `Button` idle fill in `Resources/Styles/Styles.xaml` (`Light=#E6E6E6, Dark=#3A3A3A`). The previous values matched the idle fill on both platforms, so the modal `Cancel` / `Save` and conflict-dialog `Reload` / `Overwrite` / `Cancel` buttons looked the same focused as unfocused even though focus was actually moving; the new values give the slightly darker (light) / slightly lighter (dark) contrast the original design intended
- `MainPage.ViewModelOnPropertyChanged` (for `SelectedTheme`) and `Application.Current.RequestedThemeChanged` now also call `ApplyModalActionButtonFocusVisuals` and `ApplyConflictActionButtonFocusVisuals` after `ApplyContextActionButtonFocusVisuals`, so toggling theme via `Ctrl+Shift+L/D/H` while one of the modal `Cancel` / `Save` (or conflict-dialog) buttons is focused repaints the focus tint with the new theme's gray instead of leaving the previous theme's tint stuck on the button
- Edit/Delete overlays now close when clicking outside the menu on both Windows and Mac Catalyst, and Edit/Delete, editor, and conflict overlays share a full-window hit target rendered as a `Border` (more reliable than a `Grid` for MAUI iOS gesture pickup) using a near-black `#01000000` tint that stays visually neutral. Editor/conflict hit targets block lower-layer clicks without dismissing their dialogs, with the conflict layer sitting between the conflict panel and any editor modal underneath
- Mac Catalyst context-menu Edit/Delete actions dispatch the selected item from a single Return/Enter key press after arrow-key navigation, including menus opened from the placement area, Dock, and command suggestions. The Mac path uses a private nested `MacContextMenuKeyCaptureView` UIView attached to the host as first responder, dispatching Return / Arrow / Tab / Escape into `App.RaiseEditorShortcut`
- Placement rectangle feedback now fades out on mouse release on both Windows and Mac Catalyst instead of disappearing abruptly
- Two phantom horizontal bars previously visible below the Dock scrollbar are gone — `DockScrollBarMask` now paints a transparent fill, and the obsolete drop shadows on the now-transparent placement / Dock `Border`s are removed
- The modal editor's Invert Theme checkbox indicator and its accompanying label now use the pointing-hand hover cursor, matching the modal Cancel / Save buttons rather than leaving the cursor on the default arrow
- The main window now enforces `MinimumWidth=860` and `MinimumHeight=600` so the editor modal (`WidthRequest=760` plus padding) always fits, and individual UI elements stop overflowing their containers when the window is shrunk to extreme small sizes

## [1.1.13] - 2026-04-30

### Changed
- Placement-area launcher buttons now keep the default arrow cursor on hover (instead of the pointing-hand cursor) and switch to a closed-hand "grab" cursor only while the primary pointer is pressed, so drag-to-reposition reads as a grabbed object while idle hover stops implying a click target. Dock launcher buttons intentionally keep the existing hover-hand cursor so the Dock still signals "click to launch"
- macOS placement-area grab cursors now also recover back to arrow when a pointer-release event is missed during pointer movement or exit, so a drag no longer leaves the closed-hand cursor stuck in the button area. The Mac drag path now also clears the cursor from `MainPage.Draggable_PointerMoved` and `Draggable_PointerReleased` so single-button and multi-button drags both recover cleanly after a missed release
- macOS placement-area and Dock launcher labels now use a 14pt font instead of 12pt, so the button text reads a little larger without changing the overall layout

### Added
- `Behaviors/GrabHandCursorBehavior.cs` encapsulates the placement-area press/release/enter/exit cursor swap (macOS `NSCursor.closedHandCursor`, Windows `InputSystemCursorShape.SizeAll` substitute via reflective `ProtectedCursor` assignment), mirroring the platform wiring of `HoverHandCursorBehavior` but keyed off pointer-pressed state instead of hover. The behavior only reacts to a primary-only press (so right-click context-menu and middle-click editor-open do not hijack the cursor) and also clears the grab cursor from `PointerMoved` / `PointerExited` when the primary button is no longer down, mirroring the Windows `PointerReleased`-miss fallback used in `MainPage.Draggable_PointerMoved`. `MainPage.Draggable_PointerMoved` and `Draggable_PointerReleased` now also perform the same Mac fallback so a single-button drag and a multi-button drag both recover after a missed release. `OnDetachingFrom` also restores the default cursor before gesture recognizers are removed so template teardown / filter / sync deletes cannot leave the Mac `NSCursor.closedHandCursor` or Windows `ProtectedCursor` wired to a detached platform view
- `Praxis.Core/Logic/PointerButtonClassifier.cs` centralizes the reflection-based secondary / middle pointer-button detection rules that `MainPage.PointerAndSelection.cs` already uses (inspecting `Type`, `PressedButton` / `Button` / `Buttons`, `ButtonMask`, `ButtonNumber`, `CurrentEvent`, plus `GestureRecognizer` / `Event` chains), so `GrabHandCursorBehavior` uses the same rules on Mac Catalyst rather than a substring-only ToString() inspection

### Tests
- Expanded `AppLayerSourceGuardTests` to lock the new `GrabHandCursorBehavior` platform wiring and to guard that placement-area launcher buttons in `MainPage.xaml` attach the grab-cursor behavior while the hover-hand count drops to 16 (Dock, top-bar Create, modal copy/action, context, and conflict buttons still share `HoverHandCursorBehavior`). Also guards that the behavior subscribes to `PointerMoved`, delegates Mac primary-only detection to `PointerButtonClassifier.IsPrimaryOnly(...)`, clears the grab cursor through the `PointerMoved`/`PointerExited` primary-release fallback, verifies the Mac `Draggable_PointerMoved` / `Draggable_PointerReleased` fallbacks, and restores the default cursor during `OnDetachingFrom` before gesture recognizers are removed
- Added `PointerButtonClassifierTests` to cover Mac-style platform-args detection with fake objects exercising `Type = "OtherMouseDown"`, `ButtonNumber` variations, `ButtonMask` bits for middle (`0x4` / `0x8` / `0x10`) and secondary (`0x2`), `IsMiddleButtonPressed` / `IsRightButtonPressed`, textual `PressedButton` / `Button` / `Buttons` markers, and `CurrentEvent` / `GestureRecognizer` / `Event` chain traversal

## [1.1.12] - 2026-04-21

### Fixed
- Windows and Mac Catalyst now switch the top-bar create action, modal copy/action buttons, and placement/Dock launcher buttons to a pointing-hand cursor on hover instead of leaving those clickable targets on the arrow cursor
- `DbErrorLogger` now includes the exception type in unexpected flush/drain/purge warning breadcrumbs, so shutdown-time log persistence failures can be grouped by failure class without expanding the safe exception payload
- `FileStateSyncNotifier` now includes the exception type in retry-exhaustion and unexpected-publish warning breadcrumbs, so watcher failures can be grouped by failure class without expanding the safe exception payload
- `AppStoragePaths` now includes the exception type in legacy migration warning breadcrumbs, so I/O and permission failures can be distinguished without expanding the safe exception payload
- `CommandExecutor` now includes `UseShellExecute` in the null-process-handle breadcrumb, so shell launches and direct launches can be distinguished when `Process.Start(...)` returns no handle
- `CommandExecutor` now includes whether a missing resolved path was rooted in the warning breadcrumb, so relative-path misses can be separated from broken absolute targets without reproducing the exact launch context
- `FileAppConfigService` now includes the exception type in skipped-config warning breadcrumbs, so invalid JSON, permission failures, and transient I/O can be distinguished without expanding the safe exception message
- `Win.AppDomain.UnhandledException` warnings now include the runtime type name for non-`Exception` payloads before mirroring to `startup.log`, so COM or platform-originated throw objects leave a stronger startup breadcrumb
- `Mac.AppDomain.UnhandledException` warnings now include the runtime type name for non-`Exception` payloads, so bridge-originated throw objects leave a more actionable breadcrumb than the safe payload text alone
- Mac key-input reflection warnings now render fallback keys as symbolic names like `Tab`, `Escape`, `Return`, and arrow names, so cross-platform/plain-text crash-log readers do not lose that breadcrumb to control or private-use characters
- `Program.TryRelaunchViaOpen` now includes both the normalized relay executable path and relay argument in LaunchServices warning breadcrumbs, so Mac startup relay failures distinguish bundle-path problems, `open` relay failures, and relay-contract mismatches
- `MainPage.DisableWindowsSystemFocusVisual` now includes the native control type in `UseSystemFocusVisuals` warning breadcrumbs, so Windows reflection failures identify which control rejected the write
- Mac key-input reflection warnings now include the fallback literal for `CommandEntryHandler`, `MacEntryHandler`, and `MacEditorHandler`, making `UIKeyCommand` resolution failures easier to correlate with the control key that would have been used
- `SecondaryFailureLogger` now normalizes secondary fallback sink roots to absolute directories before combining the `Praxis/secondary-failures.log` path, so quoted absolute overrides still work while blank or relative roots are ignored instead of creating accidental relative diagnostics paths
- `FileStateSyncNotifier` now includes the normalized sync-file path in the read-after-retries warning breadcrumb, so exhausted watcher read retries identify which `buttons.sync` target failed instead of logging only the exception summary
- `MauiThemeService` now includes both the target `AppTheme` and current `UserAppTheme` in the Mac dispatch-failure warning breadcrumb, so a failed Light/Dark/System transition leaves clearer pre-dispatch state than a generic window-style warning
- `FileAppConfigService` now includes the normalized raw `theme` value in invalid-theme warning breadcrumbs, so broken `praxis.config.json` files leave evidence of the actual bad value instead of only saying the theme was invalid
- `FileAppConfigService` now canonicalizes absolute config candidate roots before deduplicating them, so equivalent paths with `.` or `..` segments do not probe the same `praxis.config.json` twice
- `App.xaml.cs` now warning-logs `Window.HandlerChanged` failures with both the root page type and current `PlatformView` type, so Windows activation-hook failures show whether the shell had already reached a native window before failing
- `MainPage` now includes the hovered button `item.Id` in Quick Look delayed-show warning breadcrumbs, so failed preview popups identify which button context faulted instead of logging only the exception summary
- `CommandExecutor` now warning-logs normalized missing filesystem targets before returning `Path not found: ...`, so empty-tool fallback misses still leave a crash-log breadcrumb
- `MainViewModel.CommandSuggestions` now includes the current command-input length in debounce/close-dispatch warning breadcrumbs, so degraded suggestion refreshes leave context without persisting the full input text
- `MainPage` now includes the pending button `item.Id` in Quick Look delayed-hide warning breadcrumbs, so popup teardown failures keep the same button-level context as delayed-show failures
- `MainPage` now includes the current Dock hover flag in hover-exit hide warning breadcrumbs, so delayed scrollbar teardown failures show whether the pointer had already re-entered
- `MainPage.CopyIconButton_Clicked` now includes the copy-notice overlay visibility in animation warning breadcrumbs, so failed notification teardown leaves a quick state hint instead of only the exception summary
- `MainPage.TriggerStatusFlash` now includes the status-text length and error classification in animation warning breadcrumbs, so degraded flash paths keep lightweight context without logging the full message
- `MainViewModel` now includes the target theme in external theme-sync warning breadcrumbs, so dispatch or apply failures keep the intended `ThemeMode`
- `MainViewModel.CommandSuggestions` now includes input length in refresh-dispatch warning breadcrumbs too, so all dispatch-side suggestion warnings use the same lightweight context pattern
- `MainViewModel.CommandSuggestions` now includes input length in command-lookup fallback warnings, so repository lookup failures keep the same lightweight context pattern as other suggestion warnings
- `MainViewModel.CommandSuggestions` now includes input length in in-thread refresh warnings too, so the whole suggestion-refresh path uses one lightweight warning pattern
- `CommandExecutor` now includes the normalized target filename in the `Process.Start(...) == null` warning/result breadcrumb, so null-handle launch failures identify which tool or fallback target produced no process handle
- `FileStateSyncNotifier` now includes the normalized sync-file path in malformed-payload warning breadcrumbs too, so broken `buttons.sync` contents still identify which watched file produced the bad payload
- `Windows CommandEntryHandler` now includes both the current `EnforceAsciiInput` flag and native `TextBox` type in `InputScope` assignment warning breadcrumbs, so WinUI compatibility failures show both whether ASCII enforcement was active and which control rejected the write
- `MiddleClickBehavior` now includes current `contextMenuOpen` / `hasCommand` state plus the attached view type in deferred middle-click warning breadcrumbs, so delayed command-path failures show whether the fallback ran against an open menu, detached command binding, or unexpected host view
- `MainPage.FocusModalPrimaryEditorField` now includes current `shouldSelectAll` state in modal `ButtonText` focus warning breadcrumbs, so editor-open focus failures distinguish create-flow select-all from normal focus retry paths
- `MainPage.SetTabStop` now includes the native target control type in `IsTabStop` warning breadcrumbs, so Windows reflection failures identify which view rejected the write
- `MainPage.IsMacMiddleButtonCurrentlyDown` now includes current `isActive` / `activationSuppressed` state plus whether a root-pointer position was known in CoreGraphics warning breadcrumbs, so degraded middle-click polling shows both app eligibility and stale-pointer risk
- `MainPage.CopyIconButton_Clicked` now includes current token-cancellation state in copy-notice animation warning breadcrumbs, so expected teardown cancellation is easier to distinguish from unexpected animation faults
- `MainPage.ShowQuickLookAfterDelayAsync` now includes current popup visibility in Quick Look show warning breadcrumbs, so delayed preview failures show whether the popup had already become visible
- `MainPage.Draggable_Tapped` now includes `item.Id` in button-tap execution warning breadcrumbs, so failed launch commands can be mapped back to a concrete launcher record even when labels are duplicated
- `MainPage.PlacementCanvas_SecondaryTapped` now includes the canvas point in secondary-tap create warning breadcrumbs, so failed create-editor flows can be correlated with placement hit-testing and coordinate conversion issues
- `MainPage.FocusModalPrimaryEditorField` now includes current modal visibility in `ButtonText` focus warning breadcrumbs, so modal-open races can be separated from hidden-modal retries

### Tests
- Expanded `AppLayerSourceGuardTests` to lock hover-hand cursor behavior wiring for the top-bar create action, modal copy/action buttons, and placement/Dock launcher buttons
- Expanded `DbErrorLoggerTests` and `AppLayerSourceGuardTests` to lock unexpected flush/drain/purge warning breadcrumbs to the exception type
- Expanded `FileStateSyncNotifierTests` and `AppLayerSourceGuardTests` to lock sync warning breadcrumbs to the exception type
- Expanded `AppStoragePathsTests` and `AppLayerSourceGuardTests` to lock legacy migration warning breadcrumbs to the exception type
- Expanded `CommandExecutorTests` and `AppLayerSourceGuardTests` to lock null-process-handle breadcrumbs to the `UseShellExecute` mode
- Expanded `CommandExecutorTests` and `AppLayerSourceGuardTests` to lock missing-path warning breadcrumbs to the rooted flag
- Expanded `FileAppConfigServiceTests` and `AppLayerSourceGuardTests` to lock skipped-config warning breadcrumbs to the exception type
- Expanded `AppLayerSourceGuardTests` to lock Windows AppDomain non-`Exception` warning breadcrumbs to the runtime payload type
- Expanded `AppLayerSourceGuardTests` to lock Mac AppDomain non-`Exception` warning breadcrumbs to the runtime payload type
- Expanded `AppLayerSourceGuardTests` to lock Mac key-input reflection breadcrumbs to symbolic fallback key names instead of raw control/private-use literals
- Expanded `AppLayerSourceGuardTests` to lock LaunchServices relay warning breadcrumbs to the normalized relay executable path and relay argument
- Expanded `AppLayerSourceGuardTests` to lock `DisableWindowsSystemFocusVisual` warning breadcrumbs to the native control type
- Expanded `AppLayerSourceGuardTests` to lock Mac key-input reflection warnings to the fallback literal
- Expanded `SecondaryFailureLoggerTests` to cover quoted absolute fallback roots and rejection of relative fallback roots before the startup-diagnostics file path is built
- Expanded `FileStateSyncNotifierTests` and `AppLayerSourceGuardTests` to cover the normalized sync-file path prefix in read-retry exhaustion warnings
- Expanded `AppLayerSourceGuardTests` to lock the `MauiThemeService` Mac dispatch-failure breadcrumb to both the requested `AppTheme` and current `UserAppTheme`
- Expanded `FileAppConfigServiceTests` and `AppLayerSourceGuardTests` to cover normalized invalid-theme values in skipped-config warnings
- Expanded `FileAppConfigServiceTests` to cover canonical deduplication of equivalent absolute config roots and dot-segment normalization in `NormalizeAbsoluteDirectory(...)`
- Expanded `AppLayerSourceGuardTests` to lock `App.xaml.cs` `Window.HandlerChanged` warnings to both the root page type and current `PlatformView` type
- Expanded `AppLayerSourceGuardTests` to lock Quick Look delayed-show warnings to the hovered `item.Id`
- Expanded `CommandExecutorTests` and `AppLayerSourceGuardTests` to cover missing-path warning breadcrumbs in the empty-tool fallback path
- Expanded `CommandExecutorTests` and `AppLayerSourceGuardTests` to cover the normalized filename breadcrumb used when `Process.Start(...)` returns `null`
- Expanded `FileStateSyncNotifierTests` and `AppLayerSourceGuardTests` to cover malformed-payload warning breadcrumbs that include the normalized sync-file path
- Expanded `AppLayerSourceGuardTests` to lock `CommandEntryHandler` `InputScope` warning breadcrumbs to the current `EnforceAsciiInput` flag and native `TextBox` type
- Expanded `AppLayerSourceGuardTests` to lock deferred `MiddleClickBehavior` warning breadcrumbs to current `contextMenuOpen` / `hasCommand` state plus the attached view type
- Expanded `AppLayerSourceGuardTests` to lock modal `ButtonText` focus warning breadcrumbs to current `shouldSelectAll` state
- Expanded `AppLayerSourceGuardTests` to lock `SetTabStop` warning breadcrumbs to the native target control type
- Expanded `AppLayerSourceGuardTests` to lock CoreGraphics middle-button warning breadcrumbs to current `isActive` / `activationSuppressed` state plus `pointerKnown`
- Expanded `AppLayerSourceGuardTests` to lock copy-notice animation warning breadcrumbs to current token-cancellation state
- Expanded `AppLayerSourceGuardTests` to lock Quick Look show warning breadcrumbs to current popup visibility
- Expanded `AppLayerSourceGuardTests` to lock button-tap execution warning breadcrumbs to `item.Id`
- Expanded `AppLayerSourceGuardTests` to lock secondary-tap create warning breadcrumbs to the canvas point
- Expanded `AppLayerSourceGuardTests` to lock modal `ButtonText` focus warning breadcrumbs to current modal visibility
- Expanded `AppLayerSourceGuardTests` to lock command-suggestion debounce/close-dispatch warnings to the current input length
- Expanded `AppLayerSourceGuardTests` to lock Quick Look delayed-hide warnings to the pending `item.Id`
- Expanded `AppLayerSourceGuardTests` to lock Dock hover-exit warnings to the current Dock hover flag
- Expanded `AppLayerSourceGuardTests` to lock copy-notice animation warnings to the overlay visibility state
- Expanded `AppLayerSourceGuardTests` to lock status-flash animation warnings to message-length and error-classification context
- Expanded `AppLayerSourceGuardTests` to lock external theme-sync warnings to the target theme
- Expanded `AppLayerSourceGuardTests` to lock command-suggestion refresh-dispatch warnings to the input length
- Expanded `AppLayerSourceGuardTests` to lock command-lookup fallback warnings to the input length
- Expanded `AppLayerSourceGuardTests` to lock in-thread command-suggestion refresh warnings to the input length

## [1.1.11] - 2026-04-17

### Changed
- `MainViewModel` now records an explicit `ClearCommandInput` breadcrumb (cleared-length or no-op) and an `ExecuteCommandInputAsync` breadcrumb (command length plus whether a suggestion was selected), so diagnosing a crash that follows the command-input path no longer requires reconstructing intent from the surrounding handler-side tap log
- `MainPage.FocusEntryAfterClearButtonTap` / `ApplyEntryFocusAfterClearButtonTap` now emit entry/retry and target/outcome markers to `crash.log`, and `Entry.Focus()` itself is wrapped in try/catch so a failure inside the MAUI handler surfaces as a crash-file exception instead of a silent process termination
- `MainViewModel.ClearSearchText` now mirrors the `ClearCommandInput` breadcrumb (cleared-length vs. no-op), so the search-side X-button path has the same ViewModel-level evidence as the command-side path when a crash follows the tap

## [1.1.10] - 2026-04-15

### Fixed
- `CommandWorkingDirectoryPolicy` now treats Windows shell executable names case-insensitively, so uppercase or mixed-case `cmd.exe` / `powershell.exe` / `pwsh.exe` / `wt.exe` paths still switch `WorkingDirectory` to the user profile instead of inheriting the Praxis process directory
- `LaunchTargetResolver` now preserves valid path targets whose first or last character is a quote, normalizes env-expanded quoted rooted/home/relative and `file://` path prefixes, and explicitly excludes quoted non-file URI-scheme prefixes so malformed quoted URLs still fail closed
- `CrashFileLogger` now keeps crash-log records alive even when custom exception `Message` / `StackTrace` getters or `Exception.Data` key/value `ToString()` implementations throw, and exception messages are flattened to single-line output so malformed payloads do not corrupt inline log formatting
- `CrashFileLogger.SafeExceptionMessage(...)` now normalizes whitespace-only exception messages to `(empty)`, so warning breadcrumbs no longer degrade into blank trailing separators when the underlying exception body is present but empty
- `DbErrorLogger` now persists the same single-line exception-message normalization and getter-failure fallback markers into `ErrorLogEntity`, and app/process-exit flush failures plus Windows startup-log write failures now record the full exception body before their warning breadcrumb, falling back to an independent temp/current-directory diagnostics file when the normal `%LOCALAPPDATA%\\Praxis` crash sink is unavailable
- `SecondaryFailureLogger` now also normalizes logged startup target-path and operation fragments before interpolating them into fallback warning/body lines, so malformed startup-log metadata cannot break the secondary diagnostics file
- `MainViewModel` warning paths now use the same safe exception-message helper when external theme sync, command-suggestion refresh/lookup, conflict callbacks, clipboard helpers, sync notifications, or local persistence follow-up logging encounter hostile exception `Message` getters, so degraded warning logging no longer rethrows out of those recovery paths
- `AppStoragePaths` now uses the same safe exception-message helper for legacy database migration and invalid-path-comparison warnings, so startup migration keeps skipping bad candidates even when an exception's `Message` getter is hostile
- `AppStoragePaths` now also normalizes logged migration source/comparison path fragments before interpolating them into warning prefixes, so malformed path text cannot break crash-log line structure during legacy migration
- `FileAppConfigService` now uses the same safe exception-message helper when skipped config reads throw `IOException` / `UnauthorizedAccessException` / `JsonException`, so warning logging still persists a breadcrumb even if the exception's `Message` getter is hostile
- `FileAppConfigService` now also normalizes logged config path fragments before interpolating them into invalid-theme / skipped-config breadcrumbs, so newline-bearing candidate paths cannot break crash-log line structure
- `CommandExecutor` now uses the same safe exception-message helper for launch-target resolution and native process-start failure messages, so fallback warning/result construction no longer rethrows on hostile exception `Message` getters
- `CommandExecutor` now also normalizes logged tool / URL / path / argument fragments before interpolating them into failure prefixes, so newline-bearing launch targets cannot break crash-log breadcrumb formatting
- `MauiThemeService` now uses the same safe exception-message helper for Mac dispatch-failure breadcrumbs, so theme-apply warning logging no longer rethrows on hostile exception `Message` getters
- `FileStateSyncNotifier` now routes write/read/unexpected publish warning construction through the same safe exception-message helper, so sync breadcrumbs survive hostile exception `Message` getters
- `FileStateSyncNotifier` now also normalizes malformed payload and observed-source fragments before interpolating them into crash-log warning/info lines, so sync breadcrumbs stay single-line even if the sync file contains embedded newlines or whitespace-only payload markers
- `FileStateSyncNotifier` now also normalizes sync-file path fragments before interpolating them into write-success/write-failure breadcrumbs, so malformed storage paths cannot break crash-log line structure
- `Windows CommandEntryHandler` now uses the same safe exception-message helper for compatibility-triggered and unexpected `InputScope` assignment warnings, so WinUI fallback logging no longer rethrows on hostile exception `Message` getters
- Mac `AppDelegate` and the Mac entry/editor/command handlers now use the same safe exception-message helper for `MarshalManagedException` hook, key-command-priority, and `UIKeyCommand` input-resolution warning breadcrumbs, so those fallback paths no longer rethrow on hostile exception `Message` getters
- Mac entry/editor/command handlers now also normalize reflected `UIKeyCommand` input names before interpolating them into warning breadcrumbs, so malformed reflection metadata cannot break crash-log line structure
- Mac `Program` now uses the same safe exception-message helper for LaunchServices relay failure breadcrumbs, so open-relay warning logging no longer rethrows on hostile exception `Message` getters
- Mac `Program` now also normalizes logged LaunchServices bundle-path fragments before interpolating them into relay breadcrumbs, so malformed bundle paths cannot break crash-log line structure
- `MiddleClickBehavior` and `MainPage.MacCatalystBehavior` now use the same safe exception-message helper for `buttonMaskRequired`, deferred middle-click execution, Mac editor key-command creation, and CoreGraphics fallback warning breadcrumbs, so those degraded Mac input paths no longer rethrow on hostile exception `Message` getters
- `MainPage` now uses the same safe exception-message helper for copy-notice, status-flash, Dock hover-exit, and Quick Look animation warning breadcrumbs, so those non-fatal UI recovery paths no longer rethrow on hostile exception `Message` getters
- `MainPage` now also routes button-tap execution, secondary-tap create flow, modal primary-focus fallback, `UseSystemFocusVisuals`, and `IsTabStop` warning breadcrumbs through the same safe exception-message helper, so more Windows/UI fallback paths no longer rethrow on hostile exception `Message` getters
- `MainPage` and `App` now route fallback initialization UI text through `CrashFileLogger.SafeExceptionMessage(...)`, so hostile exception `Message` getters cannot break the last-resort error page / alert surface itself
- `CrashFileLogger.SafeObjectDescription(...)` now hardens non-`Exception` `AppDomain.UnhandledException` payloads, so hostile object `ToString()` implementations cannot break the last-resort global exception path in base `App`, Windows startup logging, or Mac `AppDelegate`

### Tests
- Expanded `CommandWorkingDirectoryPolicyTests` to cover mixed-case shell executable names and uppercase env-expanded shell paths
- Expanded `LaunchTargetResolverTests` to cover quoted relative/`file://` path prefixes, quoted-boundary path names, and malformed quoted URL handling both before and after env expansion
- Expanded `CrashFileLoggerTests`, `DbErrorLoggerTests`, `SecondaryFailureLoggerTests`, and `AppLayerSourceGuardTests` to cover multiline exception-message normalization, throwing custom exception getters / data formatters, and startup-log failure diagnostics that fall back to an independent file when the primary crash sink cannot be written
- Expanded `CrashFileLoggerTests` to cover direct source/context normalization helpers alongside persisted crash-breadcrumb behavior
- Expanded `CrashFileLoggerTests` to cover direct null `NormalizeSource(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover direct null `NormalizeContext(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover direct `NormalizeMessagePayload(...)` helper behavior alongside persisted crash-breadcrumb behavior
- Expanded `CrashFileLoggerTests` to cover multiline/whitespace-only getter-failure messages inside `SafeExceptionMessage(...)` fallback markers
- Expanded `CrashFileLoggerTests` to cover direct multiline `NormalizeExceptionMessage(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover whitespace-only `NormalizeExceptionMessage(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover null `NormalizeExceptionMessage(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover direct null `SafeObjectDescription(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover whitespace-only object-formatter failure markers inside `SafeObjectDescription(...)`
- Expanded `CrashFileLoggerTests` to cover multiline object-formatter failure markers inside `SafeObjectDescription(...)`
- Expanded `CrashFileLoggerTests` to cover direct empty-stack `SafeExceptionStackTrace(...)` helper behavior
- Expanded `CrashFileLoggerTests` to cover multiline stack-trace getter-failure markers inside `SafeExceptionStackTrace(...)`
- Expanded `CrashFileLoggerTests` to cover whitespace-only stack-trace getter-failure markers inside `SafeExceptionStackTrace(...)`
- Expanded `SecondaryFailureLoggerTests` to cover direct null target-path/operation normalization helper behavior
- Expanded `SecondaryFailureLoggerTests` to cover the `false` / null-path result when both fallback sink roots are blocked
- Expanded `DbErrorLoggerTests` to cover normalized multiline contexts inside persist-failure breadcrumbs
- Expanded `DbErrorLoggerTests` to cover normalized multiline contexts inside Warning persist-failure breadcrumbs
- Expanded `DbErrorLoggerTests` to cover normalized multiline contexts inside Info persist-failure breadcrumbs
- Expanded `DbErrorLoggerTests` to cover normalized multiline contexts inside purge-failure breadcrumbs
- Expanded `SecondaryFailureLoggerTests` and `AppLayerSourceGuardTests` to cover startup target-path/operation normalization before those fragments reach fallback diagnostics
- Expanded `SecondaryFailureLoggerTests` to cover direct target-path normalization helper behavior
- Expanded `SecondaryFailureLoggerTests` to cover whitespace-only target-path normalization helper behavior
- Expanded `SecondaryFailureLoggerTests` to cover multiline operation normalization helper behavior
- Expanded `MainViewModelWorkflowIntegrationTests` and `AppLayerSourceGuardTests` to cover hostile exception-message getters on `MainViewModel` warning paths, including external theme sync, command lookup fallback, conflict callbacks, clipboard follow-up logging, sync notifications, and theme persistence
- Expanded `AppLayerSourceGuardTests` to cover normalized reflected `UIKeyCommand` input names before Mac key-input warning breadcrumbs are assembled
- Expanded `AppStoragePathsTests` and `AppLayerSourceGuardTests` to cover hostile exception-message getters on legacy migration warning construction
- Expanded `AppStoragePathsTests` and `AppLayerSourceGuardTests` to cover migration source/comparison path normalization before legacy-migration breadcrumbs are written
- Expanded `AppStoragePathsTests` to cover direct null logged-path normalization inside `NormalizePathForLog(...)`
- Expanded `FileAppConfigServiceTests` and `AppLayerSourceGuardTests` to cover hostile exception-message getters on skipped-config warning construction
- Expanded `FileAppConfigServiceTests` and `AppLayerSourceGuardTests` to cover config-path normalization before invalid-theme / skipped-config breadcrumbs are written
- Expanded `FileAppConfigServiceTests` to cover direct null logged-config-path normalization inside `NormalizePathForLog(...)`
- Expanded `FileAppConfigServiceTests` to cover candidate enumeration rejecting blank/relative config roots
- Expanded `FileAppConfigServiceTests` to cover direct null `NormalizeAbsoluteDirectory(...)` helper behavior
- Expanded `FileAppConfigServiceTests` to cover quoted-relative rejection inside `NormalizeAbsoluteDirectory(...)`
- Expanded `CommandExecutorTests` and `AppLayerSourceGuardTests` to cover hostile exception-message getters on launch failure message construction
- Expanded `CommandExecutorTests` and `AppLayerSourceGuardTests` to cover tool / URL / path / argument normalization before launch-failure breadcrumbs are assembled
- Expanded `CommandExecutorTests` to cover direct null target-fragment normalization inside `NormalizeTargetForLog(...)`
- Expanded `CommandExecutorTests` to cover direct null `NormalizeToolPath(...)` helper behavior
- Expanded `CommandExecutorTests` to cover direct null `HasUsableTool(...)` helper behavior
- Expanded `AppLayerSourceGuardTests` to cover LaunchServices bundle-path normalization before Mac relay breadcrumbs are written
- Added `FileStateSyncNotifierTests` and expanded `AppLayerSourceGuardTests` to cover hostile exception-message getters on sync warning construction
- Expanded `FileStateSyncNotifierTests` and `AppLayerSourceGuardTests` to cover malformed/observed sync payload normalization before those fragments reach warning/info crash-log lines
- Expanded `FileStateSyncNotifierTests` to cover direct null sync-payload normalization inside `NormalizePayloadForLog(...)`
- Expanded `AppLayerSourceGuardTests` to cover sync-file path normalization before write-success/write-failure breadcrumbs are written
## [1.1.9] - 2026-04-14

### Fixed
- `CrashFileLogger` and `DbErrorLogger` now normalize null Warning/Info message payloads to `(no message payload)` before writing to `crash.log` or persisting `ErrorLogEntity` rows, so degraded logging paths keep explicit evidence instead of blank message fields

### Changed
- Repository guidance now tracks the current cdidx workflow more explicitly: `CLAUDE.md` was refreshed for cdidx 1.9.0, and tracked `.claude/settings.json` now denies `rg` / `grep` / `find`-style shell search commands so repo sessions stay on the cdidx-first path by default

### Tests
- Expanded logging regression coverage for persistence-failure breadcrumbs and null Warning/Info payload normalization in `CrashFileLoggerTests` and `DbErrorLoggerTests`
- Updated `AppLayerSourceGuardTests` so the file-first logger ordering guard matches the normalized Warning/Info logging path

## [1.1.8] - 2026-04-13

### Fixed
- `CrashFileLogger.AppendExceptionChain` and `DbErrorLogger`'s exception-type / message builders now cap recursion at depth 32 and emit an explicit truncation marker, protecting the last-resort crash logger from StackOverflow on pathological inner-exception chains
- `DbErrorLogger.BuildFullStackTrace` now traverses the inner-exception graph iteratively with the same depth cap and reference-equality cycle detection instead of delegating to `Exception.ToString()`, so the DB logger's stack-trace field cannot stack-overflow or balloon on pathological or cyclic exception graphs
- `CrashFileLogger.AppendExceptionChain` and `DbErrorLogger`'s exception-type / message builders now also track visited exceptions by reference equality, so a shared `AggregateException` subtree referenced from multiple parent slots is serialized once with a `shared/cyclic reference` marker instead of fanning out exponentially before the depth cap engages
- `CrashFileLogger` and `DbErrorLogger` now cap total exception-node serialization at 256 per call (in addition to depth 32) so a wide `AggregateException` fan-out — thousands of task failures under one aggregate — cannot synchronously stall the UI, balloon `crash.log`, or bloat the DB error-log payload
- `DbErrorLogger.BuildFullStackTrace` now bounds traversal-stack *growth* by the remaining node budget before enqueueing `AggregateException` children, not just after popping them. Previously a 50k-child aggregate still allocated 50k traversal entries synchronously before the cap fired; now allocation stays proportional to the 256-node budget regardless of aggregate width
- `CrashFileLogger.AppendExceptionChain` and `DbErrorLogger`'s type/message builders now hard-cap `AggregateException` child-edge iterations at the remaining node budget so a repeated-reference aggregate (e.g. `Enumerable.Repeat(sharedEx, 100_000)`) cannot iterate O(N) shared-reference markers. Both loggers share a `TryGetAggregateTopLevelSummary` helper that uses a bounded BFS to confirm the full descendant tree is under `AggregateMessageExpansionCap` (32) before invoking `AggregateException.Message`. Small nested wrappers like `new AggregateException("sync user-42", new AggregateException(...))` therefore keep their caller-supplied summary in persisted logs; wide or deep aggregates fall back to a synthetic bounded summary. No private framework fields are inspected
- `AggregateException` traversal now scans every child position so a later distinct exception after duplicated leading children is still serialized while node budget remains. Duplicates cost one hashset lookup each and collapse into a single per-aggregate summary marker. `BuildFullStackTrace` adds to `visited` at enqueue-time (instead of pop-time) so sibling duplicates within the same aggregate are recognised before being pushed onto the traversal stack
- Added a per-aggregate child-edge scan cap (`MaxAggregateChildEdgeScan = 4096`) to both `CrashFileLogger` and `DbErrorLogger` so synchronous work stays O(budget), not O(child count), even for aggregates with millions of repeated references
- Added a suffix sample window (`MaxAggregateChildTailSample = 128`) to both loggers on top of the prefix scan. A far-tail distinct exception — the realistic "failure storm ends with the actionable root cause" shape — is still persisted in all three DB fields and in `crash.log` instead of being silently dropped after the prefix cap. The middle region (between prefix scan and tail sample) is clipped with an explicit `"middle child(ren) not scanned"` marker so operators know a suffix sample was used
- Reserved tail + interior budget (`AggregateChildTailReserve = 16`, `MaxAggregateChildMiddleSample = 8`) before the prefix loop in both loggers. Prefix processing is capped at `remainingNodes − reservedTail − reservedMiddle`, so an all-unique wide aggregate (the realistic `Task.WhenAll` storm) can no longer starve the tail sample; the actionable last-index child survives in both DB fields and `crash.log`. Interior positions are evenly-spaced sampled so a distinct middle-region root cause is not deterministically lost. Tail iteration goes from end inward so the final indices are always preserved
- `DbErrorLogger.BuildFullStackTrace` now accounts for `stack.Count` (ancestor-discovered siblings already pending) in its enqueue cap, so nested-wide aggregates cannot queue `remainingNodes` new frames on top of many already-pending siblings and blow past the 256-node bound
- `DbErrorLogger.BuildFullStackTrace` now emits `[i]` slot labels for every queued `AggregateException` child (and `[i] (tail sample)` for children surfaced through the suffix sample), so the DB stack-trace field correlates 1:1 with `CrashFileLogger`'s `AggregateException[i]` markers on the same failure
- Added a pre-enqueue depth-cap short-circuit in both loggers: when an `AggregateException` sits at `depth == MaxExceptionChainDepth - 1`, its sibling loop is skipped entirely and a single marker is emitted. Previously each depth-truncated child returned before consuming node budget, letting the sibling scan iterate up to the edge-scan cap and emit one truncation marker per child

### Tests
- Added deep inner-exception-chain safety tests for `CrashFileLogger.WriteException` and `DbErrorLogger.Log`
- Added cyclic inner-exception graph regression test for `DbErrorLogger.Log` that asserts stack-trace persistence records a cycle marker rather than recursing
- Added shared-`AggregateException`-subtree regression test for `DbErrorLogger.Log` that asserts a fan-out-of-10 graph (1024 paths untracked → 11 distinct nodes) serializes linearly and logs shared-reference markers across all three fields
- Added wide-`AggregateException`-fan-out regression test (5000 children) that asserts node-budget truncation markers appear on all three DB fields and in `crash.log` and that each persisted field's length stays O(budget), not O(children)
- Added very-wide-`AggregateException` stress regression (50 000 children) that asserts the persisted stack-trace field length stays O(budget), proving traversal-stack growth is bounded (deterministic output-shape check instead of flaky wall-clock assertions)
- Added repeated-reference-`AggregateException` stress regression (100 000 copies of the same leaf) that asserts truncation markers + O(budget) field lengths across all three DB fields and `crash.log`, proving edge-loop iteration is capped regardless of duplicate-reference visit-caching
- Added mixed-`AggregateException` regression (`[shared × 2999, uniqueTail]`, within the 4096 edge-scan cap) that asserts the tail's distinct `NullReferenceException` still appears in all three DB fields and in `crash.log`, guarding against duplicate-heavy aggregates hiding the failure that actually explains an incident
- Added middle-region scan-cap regression (100 000 children with a unique GUID-marked needle at position 50 000) that asserts the needle does NOT appear in any persisted field *and* the `"middle child(ren) not scanned"` truncation marker IS emitted — direct behavioural guard against a regression that removes the per-aggregate edge cap
- Added far-tail root-cause regression (100 000 children with a unique GUID-marked `NullReferenceException` at position 99 999) that asserts the tail sample window surfaces the root cause in all three DB fields and in `crash.log` — guard against a regression that drops the suffix sample and hides the actionable failure
- Added all-unique-wide regression (100 000 distinct children with actionable `NullReferenceException` at the final index) that asserts the reserved tail budget preserves the last-index root cause across all DB fields and `crash.log` — direct guard for the adversarial shape where the prefix alone would exhaust the node budget
- Added nested-wide regression (outer aggregate of 5 inner aggregates of 1 000 children each) that asserts persisted output length stays bounded — guard for the `stack.Count` accounting in `BuildFullStackTrace`
- Added small-aggregate top-level-message regression that asserts a 2-child aggregate's caller-supplied summary is preserved via public API (no reflection) in both persisted stores
- Added nested-wrapper regression (`AggregateException("sync user-42", AggregateException("child", leaf))`) that asserts the outer wrapper summary survives in all DB fields and in `crash.log`
- Added per-child-index-label regressions for `BuildFullStackTrace` that assert `[0]`, `[1]`, `[2]` slot prefixes and a `(tail sample)` annotation on children surfaced through the suffix window
- Added deep-wide regression (4000-child aggregate wrapped at depth 31) that asserts exactly one `"would exceed depth cap"` marker per builder and bounded field lengths — direct guard against a regression that removes the pre-enqueue depth short-circuit
- Expanded `CiCoverageWorkflowPolicyTests` to assert `fetch-depth: 0`, GA SDK pinning, MAUI workload install flags, Xcode first-launch / compatibility gate, platform frameworks, and delivery-workflow Windows RID guard so documented CI/release invariants no longer rely on reviewer memory
- Scoped `CiCoverageWorkflowPolicyTests` assertions to individual jobs (`core-tests`, `windows-build`, `mac-build`, delivery `package` + matrix entries) so a regression dropping an invariant from one job cannot pass green just because a sibling job still contains the same string
- `CiCoverageWorkflowPolicyTests` now parses workflow steps structurally (name/if/run/uses/id) for the critical Xcode-gate invariants. The delivery `Initialize Xcode` / `Check Xcode compatibility` / `Publish app` step guards are asserted against their actual `if:` expressions and `run:` bodies, so a comment or mis-scoped step condition cannot satisfy the test

## [1.1.7] - 2026-04-12

### Fixed
- UI event handlers, sync notifiers, dock/QuickLook timers, command-suggestion debounce, clipboard helpers, and conflict callbacks now persist unhandled exceptions to `crash.log` so crash evidence survives abrupt termination
- Narrowed `OperationCanceledException` handling in command-suggestion debounce and Windows IME reassert paths to avoid swallowing non-cancellation exceptions
- `DbErrorLogger` now preserves inner-exception details when logging persistence failures

### Changed
- Consolidated repeated dispatcher-invoke logging helpers in `MainViewModel` and `App` into shared static methods
- Refreshed cdidx guidance in `CLAUDE.md` for v1.6 features (inspect metadata, `suggest_improvement`, MCP-first SQL fallback policy)

### Tests
- Added undo/redo workflow integration tests for `MainViewModel`
- Expanded `AppLayerSourceGuardTests` to cover newly hardened partial classes
- Added `DbErrorLogger` detail-preservation tests

## [1.1.6] - 2026-04-12

### Fixed
- `ThemeModeParser.ParseOrDefault()` now sanitizes invalid caller-supplied fallback enum values back to `System` instead of returning an out-of-range theme when both the input string and default are invalid
- `CommandExecutor` now expands environment-variable-backed tool paths and trims quotes after expansion, so `%ComSpec%` / quoted `%...%` tools execute with the same normalized path logic as literal tool values
- Windows startup now warning-logs failures to create the `startup.log` directory instead of losing that breadcrumb before the later append path even runs
- `DockOrderValueCodec.Parse()` now trims wrapping quotes from both the whole CSV payload and individual GUID entries so quoted dock-order persistence still restores the intended order
- `WindowsPathPolicy.IsUncPath()` now accepts quoted UNC paths but rejects `\\\\?\\` and `\\\\.\\` local-device prefixes instead of misclassifying them as network shares
- Mac `AppDelegate` now warning-logs failures to prioritize key commands over system behavior instead of letting runtime reflection differences fail silently
- `QuickLookPreviewFormatter.BuildLine()` now keeps the entire labeled quick-look line within the requested `maxLength` instead of letting the `label + ": "` prefix push the final string past the caller's limit
- `CommandNotFoundRefocusPolicy` now ignores leading whitespace before checking the `Command not found:` prefix so refocus still triggers for padded status text without matching embedded phrases
- Mac `AppDelegate` now warning-logs `MarshalManagedException` hook failures instead of swallowing them silently during startup
- `MacEntryHandler`, `MacEditorHandler`, and Mac `CommandEntryHandler` now warning-log `UIKeyCommand` input-reflection failures and fall back to baked key literals instead of letting handler type initialization fail on runtime API differences
- `ThemeModeParser.NormalizeOrDefault` now sanitizes invalid fallback enum values back to `System` instead of returning an out-of-range theme when both the parsed value and caller-supplied default are invalid
- `DbErrorLogger` now warning-logs unexpected drain-loop failures to `crash.log` so background log persistence does not fail silently after enqueue succeeds
- `FileStateSyncNotifier.NotifyButtonsChangedAsync` now warning-logs sync-payload write failures before rethrowing so local save/delete success paths leave a breadcrumb when file signaling breaks
- `MainPage` now warning-logs Windows focus-visual reflection failures, modal primary-editor focus failures, and `IsTabStop` reflection failures instead of swallowing those fallback-path errors silently
- Mac middle-click/button-mask and editor-key-command/CoreGraphics fallback paths now warning-log failures so native pointer or shortcut bridging issues leave local diagnostics instead of quietly degrading
- Windows `CommandEntryHandler` now warning-logs both compatibility-triggered and unexpected `InputScope` assignment failures before disabling or rejecting the write
- `FileAppConfigService` now continues to later config candidates when an earlier config file is readable but omits `theme` or contains an invalid theme value, instead of prematurely defaulting to `System`
- `FileAppConfigService` now warning-logs skipped config candidates so malformed, unreadable, or invalid theme configs leave a crash-log breadcrumb before fallback continues
- `DbErrorLogger` now warning-logs DB persistence and retention-purge failures to `crash.log` so non-fatal repository errors still leave diagnostics during shutdown or degraded logging paths
- `DbErrorLogger.FlushAsync(timeout)` now warning-logs timeout and unexpected flush failures so graceful-shutdown logging gaps leave an explicit breadcrumb instead of failing silently
- `Praxis/Platforms/MacCatalyst/Program.cs` is now UTF-8 BOM-free, and a repository encoding guard test now keeps `cdidx validate` clean for that entrypoint
- `CommandExecutor` now warning-logs native process-start and launch-target-resolution failures to `crash.log` so returned user-facing launch errors also leave a local diagnostic breadcrumb
- `LaunchTargetResolver` now trims wrapping quotes after environment-variable expansion, so quoted env-backed HTTP URLs and filesystem paths still resolve correctly
- `AppStoragePaths` now ignores malformed legacy-path comparisons instead of letting `Path.GetFullPath` abort storage migration checks
- `CommandRecordMatcher` now ignores `null` collection entries instead of throwing while scanning command suggestions
- `StateSyncPayloadParser` now rejects double-separator payloads instead of collapsing empty segments and accepting malformed sync signals
- `QuickLookPreviewFormatter` now keeps ellipsis-truncated output within the requested `maxLength` instead of returning strings longer than the caller asked for
- `ButtonSearchMatcher`, `LogRetentionPolicy`, and `LauncherButtonOrderPolicy` now ignore `null` record entries instead of throwing inside search, retention, or placement normalization helpers
- `CommandWorkingDirectoryPolicy` now expands environment-variable tool paths before shell detection, so `%ComSpec%` / quoted `%...%` Windows shell tools still pick the user-profile working directory
- `CommandLineBuilder` now treats quoted-empty tool values as empty so preview/status command lines stay aligned with execution-time empty-tool handling
- `AppStoragePathLayoutResolver` now trims wrapping quotes from configured storage roots before composing app-data paths
- `GridSnapper` and `ModalEditorScrollHeightResolver` now sanitize non-finite numeric inputs instead of propagating `NaN` / infinity through layout calculations
- Windows startup-log append failures and `MainPage` copy-notice animation failures now leave `crash.log` breadcrumbs instead of being swallowed silently

## [1.1.5] - 2026-04-11

### Fixed
- `MainViewModel.CommandSuggestions` now warning-logs failures to dispatch popup close/refresh work onto the main thread instead of letting those scheduling errors vanish silently
- `MauiThemeService.Apply()` now skips no-op theme reapplication when the target theme is already active, and Mac window-style dispatch failures are crash-logged
- Mac Catalyst open-relay startup now treats `Process.Start(...) == null` as failure and crash-logs LaunchServices relay failures instead of silently falling back
- `FileStateSyncNotifier.Dispose()` now disables `EnableRaisingEvents` before unsubscribing/disposing the watcher to reduce late callback races during teardown
- Base `App` global unhandled-exception/process-exit hooks are now registered only once, preventing duplicate crash/log flush handlers from stacking if app initialization is re-entered
- `MainViewModel.SyncThemeFromExternalChangeAsync()` now warning-logs failures thrown from the dispatched main-thread apply path, and external reload uses `RunContinuationsAsynchronously` for its bridge `TaskCompletionSource`
- `FileStateSyncNotifier.NotifyButtonsChangedAsync()` now no-ops after disposal instead of trying to write stale sync files during teardown
- `AppStoragePaths.TryMigrateLegacyDatabase()` now warning-logs copy failures and continues scanning other legacy candidates instead of aborting migration on the first unreadable source
- `FileAppConfigService` now falls back to later config candidates when an earlier config file is inaccessible with `UnauthorizedAccessException`
- `MainPage.OnDisappearing()` now detaches window-activation hooks, and the detach path also releases Mac activation observers so disappearing pages do not keep stale activation callbacks alive
- `CommandExecutor` now expands home-prefixed tool paths (`~`, `~/...`, `~\\...`) before deciding whether a tool is executable, so direct tool launches can use the same home shorthand as empty-tool path launches
- Windows `startup.log` now uses the normalized shared app-storage root instead of the raw local-app-data special-folder string, avoiding malformed startup-log paths when the environment value is quoted or missing
- Windows and Mac platform startup classes now guard global exception-hook registration so repeated initialization cannot stack duplicate unhandled-exception handlers
- `App.CreateWindow()` no longer caches the fallback error page when `ResolveRootPage()` fails, so later window creation can recover instead of being pinned to the first startup failure page
- `App` now crash-logs log-flush failures during both `AppDomain.UnhandledException` termination handling and `ProcessExit`, instead of suppressing them silently
- `FileStateSyncNotifier` now warning-logs sync-file read retry exhaustion instead of silently dropping unreadable external-sync payloads
- `CommandExecutor` now treats normalized empty/quoted-empty tool values as “no tool” and falls back to URL/path launching instead of trying to execute an empty filename
- `MainPage` now crash-logs XAML load failures, resets its initialization gate when first-load startup fails, and separately crash-logs initialization-alert failures so a broken alert path does not erase the original startup exception
- `MauiClipboardService` now honors cancellation tokens for both clipboard reads and writes instead of ignoring canceled operations
- `SqliteAppRepository` now publishes its shared SQLite connection only after schema upgrade and initial cache load succeed, allowing clean retry after initialization failures
- `DbErrorLogger.FlushAsync(timeout)` now waits for both queued entries and already-dequeued in-flight DB writes up to the timeout instead of returning early once the queue becomes temporarily empty
- Error-log retention purge remains Error-only; added regression coverage so Info/Warning writes do not trigger `PurgeOldErrorLogsAsync`
- `CrashFileLoggerTests` no longer use blocking `Task.WaitAll`, removing the xUnit analyzer warning from Release test runs
- Shared and platform-specific unhandled-exception hooks now keep more diagnostics: non-`Exception` thrown objects are logged as warnings instead of degrading to empty payloads, and Windows/Mac `UnobservedTaskException` handlers now call `SetObserved()`
- `FileStateSyncNotifier` now subscribes before enabling the watcher, recreates the sync directory before writes, ignores malformed/out-of-range payload timestamps safely, and crash-logs event-subscriber failures instead of letting the background task fault silently
- `LaunchTargetResolver` now treats separator-based relative paths and bare `~` as filesystem fallback targets when `Tool` is empty, and `CommandExecutor` now expands bare `~` to the user-profile path before existence checks
- Windows shell launches (`cmd.exe`, `powershell`, `pwsh`, `wt`) now start from the user-profile directory instead of inheriting the Praxis process working directory
- Theme parsing now rejects numeric enum strings in config/repository/ViewModel inputs, `.` / `..` now resolve as filesystem targets, and quoted tool paths are normalized before process launch
- `SqliteAppRepository` now normalizes cached button order to placement order after load/reload/upsert paths, and dock-order persistence now discards duplicate or empty GUIDs while preserving first occurrence order
- Config/storage path handling is stricter: malformed base `praxis.config.json` now falls back to later valid candidates, quoted `%LOCALAPPDATA%` values are normalized, and blank/relative storage roots no longer degrade into working-directory-relative DB/crash-log paths
- `DbErrorLogger` now preserves nested inner exception type/message chains inside `AggregateException` entries instead of truncating at the direct children
- `SqliteAppRepository.SetThemeAsync` now normalizes out-of-range `ThemeMode` values to `System`, and external empty `dock_order` sync now clears stale Dock UI state instead of leaving old buttons visible
- `MainViewModel` now warning-logs external reload/theme sync failures, command-suggestion refresh failures, and conflict-dialog callback failures instead of swallowing them silently
- Windows clear-button native refocus failures now write directly to `crash.log`, improving diagnostics for freeze/abort paths where async DB logging may never complete
- Startup, external sync, execution-request, clipboard-copy, clear-button, and sync-signal boundaries now emit additional low-cost Info breadcrumbs so GUI hangs/aborts leave a clearer last-known-good stage
- Clipboard and sync-notifier failures are now isolated from successful local actions: create-with-clipboard falls back to empty args, copy failures become warning/status feedback, execution still logs after clipboard-copy failure, and save/delete/theme/dock/history operations no longer unwind after post-success sync notification errors
- Launch-log write/purge, dock persistence, undo/redo dock restore, and theme persistence failures are now treated as non-fatal after local success, with warning logs instead of surfacing as user-action exceptions
- Initialization and external reload now tolerate non-critical theme/dock read failures with warning logs and safe fallbacks, and command lookup fallback errors now degrade to `Command not found` instead of bubbling exceptions

## [1.1.4] - 2026-04-09

### Added
- add .codex & CLAUDE.md

### Fixed
- .NET 10 preview runtime package restore failure (`NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64 with version (= 10.0.5)`) in CI and Delivery workflows — replaced pinned `dotnet-version: 10.0.100` with `10.0.x` + `dotnet-quality: preview` to enable the preview NuGet feed

## [1.1.3] - 2026-04-05

### Added
- Automatic GitHub Release creation on `v*` tag push — delivery workflow now zips OS-specific artifacts (Windows / macOS) and publishes them with auto-generated release notes

## [1.1.2] - 2026-03-31

### Added
- Synchronous file-based crash logger (`CrashFileLogger`) that writes to `crash.log` immediately on every log call, surviving abrupt process termination where async DB writes would be lost
  - Cross-platform: Windows `%LOCALAPPDATA%\Praxis\crash.log`, macOS `~/Library/Application Support/Praxis/crash.log`
  - Automatic log rotation at 512 KB
  - Full exception chain output including inner exceptions, `AggregateException` flattening, and `Exception.Data` dictionary
- `IErrorLogger.LogWarning(message, context)` for warning-level log entries
- `IErrorLogger.FlushAsync(timeout)` to drain pending async DB writes during graceful shutdown
- `AppDomain.ProcessExit` handler that flushes logs before process exit
- `UnhandledException` handler now attempts synchronous flush when `IsTerminating=true`
- Mac Catalyst `AppDelegate` crash file logging hooks (`UnhandledException`, `UnobservedTaskException`, `MarshalManagedException`)
- Windows platform exception handlers now write to both `startup.log` and `crash.log`
- Non-Exception thrown objects are now captured in `UnhandledException` handler

### Changed
- `DbErrorLogger` rewritten: all `Log`/`LogInfo`/`LogWarning` calls write to crash file synchronously first, then enqueue for async DB write via `ConcurrentQueue` with single-writer drain loop (replaces fire-and-forget `_ = LogAsync()` pattern)
- Error log entries now capture full exception type chains (e.g. `InvalidOperationException -> NullReferenceException`), concatenated inner messages, and complete stack traces via `Exception.ToString()`
- `ErrorLogEntity.Level` column now accepts `Warning` in addition to `Error` and `Info`
- `ResolveRootPage` failure now logged via `IErrorLogger` (was silently swallowed)

## [1.1.1] - 2026-03-28

### Fixed
- GitHub Actions checkout now fetches full history so Nerdbank.GitVersioning can calculate version height in CI and release packaging jobs
- macOS GitHub Actions jobs initialize Xcode before Mac Catalyst build/publish to avoid `ibtoold` / Xcode plug-in initialization failures on fresh runners

### Changed
- Added README header badges for CI, CodeQL, Delivery, .NET 10, .NET MAUI, supported platforms, SQLite, and MIT license

## [1.1.0] - 2026-03-28

### Added
- Per-button inverted colors with auto DB schema migration (v1 → v2)
- DB-backed error logging (ERROR + INFO levels) with 30-day retention (`ErrorLog` table, schema v3 → v4)
- INFO-level contextual tracing for key user actions: button/command execution, editor open/save/cancel/delete, theme change, undo/redo, conflict resolution, window close
- Undo/Redo for button mutations (move/edit/delete): Ctrl+Z / Ctrl+Y on Windows, Command+Z / Command+Shift+Z on macOS
- Quick Look preview on button hover (Command / Tool / Arguments / Clip Word / Note)
- SQLite schema versioning via `PRAGMA user_version` with sequential auto-migration
- Cross-window sync via `FileSystemWatcher` signal file (`buttons.sync`) with instance-id self-filter
- Conflict detection dialog for concurrent multi-window edits (optimistic locking via `Version` column)
- Execute all matching commands on Enter (not just the first match)
- Clip Word field supports multiline input
- Search text auto-cleared on button create
- Arrow-key focus cycling for context menu and conflict dialog on Windows/macOS
- Middle-click and right-click interactions on command suggestion rows
- Command suggestion debounce increased to 400 ms to reduce noise during fast typing
- First `Down` key selects first candidate; popup no longer auto-selects on open
- CI coverage collection and Cobertura artifact upload (GitHub Actions)
- UNC path fallback via `explorer.exe` on Windows so auth prompt can appear before existence checks succeed
- Dock horizontal scrollbar shown only while hovering the Dock area and horizontal overflow exists
- Invert-theme label is tappable to toggle the checkbox (not just the checkbox itself)

### Fixed
- Command suggestions auto-close when context menu opens
- Command suggestion click runs the command and autofills the command input
- Editor modal default focus now lands on `ButtonText`, matching the field order after the Button Text / Command swap
- New-button create now selects all `ButtonText` text on initial modal focus on both Windows and macOS
- Windows: Tab focus navigation selects all text in input fields
- Clear button focus restore stability after tap (immediate attempt + short delayed retry)
- Windows: top-bar `Command` / `Search` clear-button refocus now skips stale native `TextBox` instances to avoid rare aborts
- macOS: clear-button refocus is deferred to the next frame to avoid responder re-entry during clear-button hide
- Clear-button X glyph vertical centering on Windows
- Command suggestion colors stay theme-synced during live theme switch
- `CommandEntry` / `SearchEntry`: lowercase letters no longer converted to uppercase
- Main command entry no longer attempts to switch IME/input-source mode on focus on Windows or macOS
- Modal `Command` IME / ASCII enforcement:
  - Windows: `InputScopeNameValue.AlphanumericHalfWidth` on focus + `imm32` nudge (immediate + one delayed retry)
  - macOS: `AsciiInputFilter` + `setMarkedText` / `insertText` blocking, detached on app deactivation
- Modal `Command` IME reasserted while focused on Windows (prevents manual IME-mode switching)
- macOS: modal ASCII input source enforced only while field is first responder in active key window
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
- Dock area height expanded
- New button icon changed from plain `+` to wireframe hex logo (outer hexagon · inscribed circle · inner hexagon · center `+`)
- App icon and splash screen refreshed to hexagon + polygon contrast design with micro-optimized variants
- `MainPage` concern split refined further: `EditorAndInput` was narrowed to shared input behavior, while modal editor, view-model event wiring, status/theme logic, dock/quick-look behavior, and Windows-native input hooks moved into dedicated partial classes
- `SqliteAppRepository` public operations protected with exclusive locking for thread safety
- UI delay values consolidated into `UiTimingPolicy`
- Platform preprocessor blocks consolidated across `MainPage` field files and `MauiProgram` handler registration
- Redundant `using` directives removed and `using` order normalized
- `MainViewModel` and its partial classes annotated with `LogInfo` calls for key lifecycle events

---

# 変更履歴（日本語）

このファイルにはプロジェクトの主な変更をすべて記録します。

形式は Keep a Changelog に準拠し、バージョン管理は Semantic Versioning に従います。

## [Unreleased]

## [2.0.4] - 2026-05-18

### 修正
- ランチャーと Dock のボタン ToolTip の `ButtonText` 行がランチャーの表示ラベル値に bind され、空欄で表示されないようになりました。

## [2.0.3] - 2026-05-17

### 変更
- AI agent 向けの `cdidx` code search rule を CodeIndex v1.22.3 に合わせて更新し、`status --check --json` を freshness gate とすること、scoped index refresh、exact search/name option、`inspect` / `find` / `excerpt` / `outline` の使い分けを明記しました。

### 修正
- ランチャーと Dock のボタン ToolTip で `Command` の上に `ButtonText` を表示し、実行情報とあわせて表示ラベルも確認できるようになりました。

## [2.0.2] - 2026-05-17

### 修正
- macOS の title-bar double-click normal maximize は、再度 double-click した時に以前の window bounds を復元するようになりました。Avalonia が最大化後の geometry を `WindowState.Normal` として報告する経路も含みます
- macOS は normal maximize 後も専用の title-bar drag surface を有効に保ち、title-bar 中央でも double-click restore が効くようになりました
- title-bar double-click 処理は Avalonia の click count も見るようになり、click 間の小さな pointer 移動で macOS の maximize / restore が不安定になりにくくなりました

## [2.0.1] - 2026-05-16

### 修正
- Windows の上辺 hit test で custom caption drag 領域より native の縦方向リサイズを優先し、Praxis ウィンドウ上辺をつかんで縦方向にリサイズできるようになりました
- Avalonia 版の Windows/macOS で、editor や conflict dialog が開いていない時に Praxis ウィンドウがアクティブになると Command 欄へフォーカスし、テキストを選択するようになりました
- Avalonia の配置面は固定 1600x880 ではなく表示中のランチャーボタンの範囲から広がるようになり、Windows/macOS で表示中ボタンが現在の viewport に収まっている時は配置面のスクロールバーが出なくなりました

## [2.0.0] - 2026-05-14

### 追加
- strict MVVM の Core model 構成、Avalonia デスクトップシェル、プラットフォーム抽象化・擬似アクリル UI 方針・DB 互換リスク・将来の Linux 対応を含む移行計画を持つ v2.0.0 Avalonia 移行ブランチを開始
- SQLite launcher-button 永続化、プラットフォーム対応 app data path 解決、v1 テーブル名互換、`praxis.db3` / 既存 `praxis.db` ファイル対応、`ColorKey` / `ToolTip` / `LastExecutedAtUtc` / `SortOrder` 用の schema version 5 migration を備えた `Praxis.Data` を追加
- Windows、macOS、および Linux-ready な `xdg-open` 経路で、直接 command 実行と既定アプリ起動を行う desktop launcher execution service を追加
- command input 実行、command suggestion、永続化された recent Dock order、button delete/move repository 操作、Core/Data service 経由の launch-log 書き込みを再導入
- `IStateSyncNotifier` / `FileStateSyncNotifier` による app-local なファイルベース launcher-button state sync を追加し、editor が開いている間は外部変更 reload を遅延するようにした
- Avalonia window icon resource、専用 draggable chrome row、drag area の double-click maximize、カスタム minimize/maximize/close caption button を追加

### 変更
- solution、CI、delivery、README、developer/test docs の active runtime target を Avalonia desktop app へ切り替え、MAUI workload を不要化
- Avalonia startup は preview-only launcher data ではなく、`MainModel` と `ILauncherButtonRepository` 経由で SQLite から launcher button を読み込むよう変更
- `MainWindow` code-behind は generated-style の `InitializeComponent` wrapper を持たず、XAML を直接読み込むよう変更

### 削除
- 旧 .NET MAUI app project と MAUI app-layer linked-source tests を削除し、v2 開発を Avalonia shell と Core model/service contract から開始する構成に変更

### テスト
- `praxis.db3` / `praxis.db` storage selection、SQLite v4-to-v5 launcher schema migration、v2 launcher-field persistence の focused xUnit coverage を追加
- command suggestion/execution、永続化された Dock order、launch log、button deletion、snapped move persistence の focused xUnit coverage を追加
- state-sync payload parsing、保存成功時の通知、外部変更 reload、他 window が button を更新または削除した場合の editor conflict 検出の focused xUnit coverage を追加
- direct XAML loading、embedded icon assets、draggable chrome、caption button wiring の source guard を追加

### 修正
- Windows Avalonia shell は taskbar / jump-list surface 向けに透明背景の app icon を使い、custom caption button を window 上端へ詰め、擬似アクリル shell の角丸を強化
- Windows caption button は native の minimize / maximize / restore animation を維持するため OS chrome 経路を使い、caption tooltip の文字切れを避け、擬似アクリル shell の透明度を上げ、custom title-bar drag の edge snap に対応
- Avalonia editor modal に button `Command` 欄を追加し、配置領域 / Dock の tooltip から重複する `ButtonText` を削除し、Windows caption hit test を native title-bar 経路に寄せて Aero Snap に対応し、小さい window icon を alpha DIB ICO frame として出力
- Windows の配置領域 / Dock button tooltip は pointer 追従ではなく button edge 基準の固定配置にし、tooltip が cursor 下に出て初回 click を奪う状況を避けるよう修正
- ライトモードの Windows caption button は glyph を濃くし、hover background を中立色のまま少し強く見えるよう調整
- 編集モーダルは context menu の Edit を含む通常編集時に `ButtonText` 末尾へ caret を置き、新規ボタン時だけ `ButtonText` を全選択
- 新規ボタンの初期値は番号付きの placeholder text / command ではなく、固定の `New` と空の command に変更
- context menu の Delete は、選択中 button から実行した場合に選択中 launcher-button 全体を削除し、未選択 button から実行した場合は従来どおりクリックした button だけを削除するよう修正
- Windows jump-list の relaunch icon metadata は透明背景の小アイコン resource を指すようにし、taskbar 右クリックメニューで古い白背景の実行ファイル icon が使われにくいよう調整
- Windows の角丸は GDI region ではなく DWM 管理の clipping に寄せ、ライトモードで目立っていた角の粗さを滑らかに調整
- Windows 専用の編集モーダル `ButtonText` focus 安定化策は、不安定で他の modal control から focus を奪い返す副作用があったため削除し、残る Windows の caret 先頭戻り race は開発者ガイドの未解決課題として記録
- Windows shell の角は DWM 管理の window corner に重ねて内側の角丸を描かないようにし、二重の弧や snap 時の不自然な角描画を避けるよう調整
- Windows は animated minimize / maximize / restore のために OS chrome 経路を使う場合、自前の Avalonia caption-button stack を隠し runtime title を空にして、caption button の二重表示と余計な title text を避けるよう調整
- Windows は OS chrome 経路を維持しつつ Avalonia drawn full-screen caption glyph だけを隠し、minimize / maximize / close を残したまま minimize button 左側の余計なボタンを消すよう調整
- `Ctrl+Shift+L` / `Ctrl+Shift+D` / `Ctrl+Shift+H` で Avalonia を Light / Dark / System theme mode へ明示的に切り替えられるよう修正
- `Ctrl+Shift+H` は固定の中間 palette ではなく system theme へ戻し、OS が選んだ実際の light / dark から window class と theme 依存 binding を再評価するよう修正
- Windows の編集モーダル `ButtonText` は pointer hit-test 抑止や caret / selection の反復 timer ではなく、Windows / macOS 共通の初期 focus 経路を使うよう変更
- Windows の launcher / Dock button label は縦方向の見た目中央へ下げ、真の window 透過が効かない環境でも blur 風に見えるよう擬似アクリル背景を低コントラストで柔らかい sheen に調整
- 単一行の Avalonia text box は水平スクロール動作を無効化せず非表示スクロールにし、Command / Search の clear button 用に控えめな右余白を確保したうえで、入力が表示幅を超えても caret が右端に残って文字列が左へ流れるよう修正
- README と developer/database/testing docs は、削除済みの移行計画メモへのリンクや未実装扱いの記述を外し、現在の Avalonia editing、drag、theme switching、file-backed button sync の挙動に合わせて更新

## [1.2.0] - 2026-05-11

### 追加
- `Command` と `Search` 入力欄の placeholder を、欄内左端に配置する SVG 風アイコンに置き換え。`Command` は `>_`（chevron + underscore のターミナルプロンプト形）、`Search` はよくある虫眼鏡。文字列が入ると placeholder と同じくフェードアウトし、色は `Light=#A0A0A0, Dark=#7C7C7C` の控えめなグレーで「主張しない hint」として読ませる
- Windows: OS タイトルバーを置き換える 30px のカスタムタイトルバー（控えめな minimize / maximize-restore / close キャプションボタン3つ）を追加。`Microsoft.UI.Xaml.Window.ExtendsContentIntoTitleBar = true`（WinUI 3 XAML 公式の API。`AppWindow.TitleBar.ExtendsContentIntoTitleBar` だと MAUI の WinUI ホスト下で OS タイトルバーが残る）と `OverlappedPresenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false)` の組み合わせで OS タイトルバーとシステムキャプションボタンの両方を消しつつ、リサイズ境界と Windows 11 の角丸を維持する。`Microsoft.UI.Xaml.Window.SystemBackdrop = null` で MAUI 既定の `MicaBackdrop` を無効化し、ページ背景（`Resources/Styles/Colors.xaml` の `Light=#F2F2F2 / Dark=#161616`）が Mica ラベンダー tint なしでウィンドウ全面を塗る。ドラッグ領域は `Microsoft.UI.Input.InputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, ...)` でキャプションボタン群より左側を OS に通知し、`WindowTitleBar.SizeChanged` / `WindowTitleBarDragRegion.SizeChanged` / `WindowCaptionButtonsStack.SizeChanged` / `AppWindow.Changed`（`DidPresenterChange` / `DidSizeChange`）で再計算する。各ボタンは `OverlappedPresenter.Minimize / Maximize / Restore` と `Window.Close` を呼ぶため、ユーザーの「パフォーマンスオプション」の「ウィンドウの最小化と最大化のアニメーション」設定が無効化されない。最大化グリフは `AppWindow.Changed` で `ChromeMaximize` ⇔ `ChromeRestore`（Segoe Fluent Icons）を切り替える。3 ボタンは同じテーマ連動 tint（`Light=#E0E0E0 / Dark=#3A3A3A` ホバー、`Light=#D0D0D0 / Dark=#4A4A4A` 押下）を共有し、close も赤系を使わない。Mac Catalyst は影響を受けない（タイトルバー行は高さ 0 に潰して OS のタイトルバーがクロムを担当する）
- Windows: ネイティブ側リサイズ遅延中の OS 塗りを目立たなくするためのバックストップ。`App.CreateWindow` は Win32 `SetClassLongPtrW(GCLP_HBRBACKGROUND, CreateSolidBrush(...))` でウィンドウクラスブラシをページ idle 色（`Light=#F2F2F2 / Dark=#161616`）に置き換えるため、OS が HWND を拡張して WinUI が新クライアント領域を描く前の隙間も、純白ではなくページ色で塗られる。`DwmSetWindowAttribute(DWMWA_BORDER_COLOR, DWMWA_CAPTION_COLOR)` も同色に設定し、DWM が描くリサイズ境界／キャプション帯も同じ色で馴染ませる。WinUI ルート content パネル（`nativeWindow.Content`）の `Background` も同色にし、WinUI 自身の描画経路でも白フォールバックが見えないようにする。これら 3 つの組み合わせで右端／下端リサイズアウト時の白い「のびしろ」帯はかなり緩和されたが完全には消えなかった（後段の `WM_ERASEBKGND` subclass で最終的に解消）
- Windows: MAUI 内部の `WindowRootView` のタイトルバー枠を保険としてリフレクションで潰す。`App.TryNullAppTitleBarRecursive` は WinUI のビジュアルツリー（`Microsoft.Maui.Platform.WindowRootViewContainer` → `Microsoft.Maui.Platform.WindowRootView`）を歩いて `_appTitleBar`、`_appTitleBarHeight=0`、`_useCustomAppTitleBar=false`、`_titleBar`、`WindowTitleBarContent=null`、`AppTitleBarTemplate=null`、`AppTitleBarContainer=null`、`AppTitleBarContentControl=null`、`WindowTitleBarContentControlVisibility=Collapsed`、`WindowTitleBarContentControlMinHeight=0` をリフレクションで設定する。`HandlerChanged`、`Window.SizeChanged`、および遅延 `DispatcherQueue.TryEnqueue(Low)` コールバックで走る。遅延パスでは 1 ピクセルの `AppWindow.Resize` + 復元も行い、ユーザー操作前にドラッグ領域の変換が確定するようにしている — これにより視覚的なキャプションボタン行で起動直後（手動リサイズ前）からドラッグ／ダブルクリック最大化が効くようになった
- Windows: キャプションボタン行のさらに上に出ていた約 32 px の余白行を解消した。原因は WinUI 3 `NavigationView` の既定テンプレートが、`ExtendsContentIntoTitleBar = true` 環境下で（`IsTitleBarAutoPaddingEnabled` の値にかかわらず）内部 `ContentGrid` パートに `Margin="32,0,0,0"` を貼ること。`App.ForceNavigationViewContentGridMarginZero` が `VisualTreeHelper` で `WindowRootView` 配下を walk し、`x:Name="ContentGrid"` を見つけて `Margin = Thickness(0)` を強制する。テンプレートは `MeasureOverride` 中（特にウィンドウリサイズ後）に 32 px を再スタンプし直すので、`AttachContentGridMarginGuard` が `ConditionalWeakTable` で要素ごとにハンドラ重複なくつけた `LayoutUpdated` 経由で都度 0 に戻し続ける。加えて `App.CreateWindow` は遅延 `DispatcherQueue.TryEnqueue(Low)` から Win32 `SetWindowPos(SWP_FRAMECHANGED)` を発行し、OS に非クライアント領域の再評価を強制する — これで `InputNonClientPointerSource.SetRegionRects` の Caption 宣言が起動直後から有効になる（これがないと、ユーザーがキャプションボタンを押して presenter 状態が変化するまでドラッグ／ダブルクリック最大化が効かなかった）。原因特定に使った per-LayoutUpdated 単位の `[DragRegion]` ログ、`WRV.Set` 成功ログ、`WindowRootViewContainer.Properties/Fields` + `WindowRootView.TitleProps/TitleFields` の diagnostic dump は、解決法が固まったので削除済み
- Windows: ダークテーマ時にウィンドウ外周にうっすら白い 1px ボーダーが見えていた問題を解消。`App.ApplyWindowsImmersiveDarkMode` が `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE = 20, BOOL)` を呼び、OS が描く外周枠を Light モード既定ではなく Dark モード版で描かせる。テーマ依存の chrome 属性 4 つ（immersive dark mode、WinUI ルート content 背景、Win32 クラスブラシ、DWM ボーダー／キャプション色）は `App.ApplyWindowsThemeChrome(Window)` 単一エントリポイントに集約し、実効テーマは `App.ResolveWindowsAppThemeIsDark` 経由で解決する — MAUI の `Application.Current.UserAppTheme` が OS の `RequestedTheme` より優先されるので、アプリ内 `Ctrl+Shift+L/D/H` でのテーマ切替が Windows chrome 側にも届くようになった（旧来の `Microsoft.UI.Xaml.Application.Current.RequestedTheme` を直接読む実装では OS テーマしか拾えず、ユーザーのアプリ内選択が反映されなかった）。`MainPage` 既存の `Application.Current.RequestedThemeChanged` ハンドラからも `App.ApplyWindowsThemeChrome(windowsNativeWindow)` を呼ぶようにしたので、テーマ切替で chrome 4 属性が即時再塗装される。Mac Catalyst 側は影響なし
- Windows: 右端／下端のリサイズハンドルを外側へ引いたときに新しく現れる領域に白い帯が見えていた問題を解消。`App.InstallEraseBkgndSubclass` が HWND に `comctl32!SetWindowSubclass` ハンドラを設置し、`WM_ERASEBKGND` を捕捉してクライアント矩形全体をページ色（`Light=#F2F2F2 / Dark=#161616`）の GDI `CreateSolidBrush` ブラシで `FillRect` し、`(LRESULT)1` を返して OS の既定クラスブラシ塗り（WinUI 3 ウィンドウでは `GCLP_HBRBACKGROUND` を差し替えても白いままになりがちで、クラスブラシ + DWM 色の組み合わせだけでは帯を狭くするのが精一杯だった）をスキップさせる。その他のメッセージは `DefSubclassProc` に流すので、角丸・リサイズ境界・NC ヒットテスト・WinUI 自身の入力処理には影響しない。`App.UpdateEraseBkgndBrush(isDark)` は `ApplyWindowsThemeChrome` に組み込んであり、テーマ切替時に新色でブラシを再生成、古いブラシは `gdi32!DeleteObject` で解放して GDI リークを避ける。静的フィールド `eraseBkgndSubclassDelegate` がマーシャル化された subclass thunk を HWND の寿命の間保持する

### 変更
- Edit/Delete のコンテキストメニュー、編集モーダル、conflict ダイアログ、command 候補ポップアップは即時切り替えではなくフェードイン/アウトで開閉する。連続で開閉しても途中で固まらないよう、各 overlay にキャンセルトークンを持たせている
- Dock は最小高と下寄せの余白をコンパクトにし、枠線なしの配置領域へ縦方向の余白を返す。Dock 横スクロールバーには下方向の小さなマージンを設けてランチャーボタンと干渉しないようにし、Dock のスクロール内容は左右端で clip して部分表示のボタン端を直線で切る
- 配置領域と Dock を囲む `Border` は背景塗り (`BackgroundColor="Transparent"`) を持たず、stroke も透明。ページ背景がそのまま見える状態にし、ランチャーボタンの塗りだけがその領域内で視認できる
- ページ下端のステータスバーは枠なし（`Stroke="Transparent" StrokeThickness="0"`）。フラッシュしていない待機状態の背景も完全透明とし、ページに溶け込んで見えなくなる。ステータステキストは水平中央揃え
- 配置領域のドラッグ選択矩形のストローク太さを 2px から 1px に半減し、複数選択フィードバックの見た目を軽くする
- 配置領域の inverted ボタン（`UseInvertedThemeColors=True`）の選択時の塗りを `Light=#787878 / Dark=#A0A0A0` に変更し、inverted idle の `#363636 / #FFFFFF` とのコントラストを広げる（モノトーンの範囲内で）
- Edit/Delete コンテキストメニュー、モーダルの Cancel/Save、conflict ダイアログの Reload/Overwrite/Cancel の各ボタンは、フォーカス表示を枠線ではなく背景色チント（`Light=#E6E6E6 / Dark=#3D3D3D`）で行うよう変更。フォーカス遷移でラベル位置がずれない。アンフォーカスの塗りはプラットフォーム既定にフォールバックする
- OS のタイトルバーから "Praxis" 文言を削除（Windows / Mac Catalyst 両方）。Mac Catalyst では bundle / display-name にフォールバックするため、`AppDelegate.OnActivated` で新規ヘルパー `ClearMacWindowTitles` を実装し、接続中の `UIWindowScene` ごとに `Title=""` と `Titlebar.TitleVisibility=Hidden` を適用する

### 修正
- 編集モーダルで `Note` / `Clip Word` 欄から `Tab` / `Shift+Tab` を押したときに `Cancel` までフォーカスが進むようになり、Windows / Mac Catalyst のどちらでも `Note → Cancel → Save → GUID` のサイクルになり、フォーカスされたボタンが視覚的にも見えるようになった。Windows の複数行 `TextBox`（`AcceptsReturn=true`）は `Tab` を文字として食う上に WinUI 3 では `AcceptsTab` の override がないため、`WindowsTextBox_PreviewKeyDown` で TextBox 内蔵の `OnKeyDown` が走る前に `Tab` / `Shift+Tab` を捕まえ、次／前のモーダルフィールドへ MAUI `VisualElement.Focus()` ＋ プラットフォーム `Control.Focus(FocusState.Keyboard)` でフォーカスを移す。`FocusModalTabTarget` は `windowsEditorFocusRestorePending = false` で `WindowsModalInput_LostFocus` 由来の遅延 restore をキャンセルし、新しい `WindowsModalActionFocusTarget` 擬似フォーカスフィールドを立てて `HasWindowsEditorModalFocus` が「モーダル内にフォーカスがある」と判定するようにする（Button 側の native focus が settle するまでの隙間で `ButtonText` に focus を奪い返されないため）。`ApplyModalActionButtonFocusVisuals` は Windows ではこの擬似フォーカスから直接 focus tint を塗る（既存の Mac の擬似フォーカス経路と対称な構造）。`WindowsTextBox_GotFocus`、`WindowsTextBox_PointerPressed`、Cancel/Save 自身の `Focused` イベントで擬似フォーカスをクリアし、モーダル内に native focus が戻ったら通常経路に戻る。不可視の `ModalInvertThemeCheckBox`（`Opacity=0`、隣の Grid+TapGesture でトグル）は `ApplyTabPolicy` と新しい `HandlerChanged` イベントの両方で `IsTabStop=false` に固定し、PlatformView 生成タイミングのズレに左右されないようにする。Mac Catalyst 側は `ModalFocusOrder` から `ModalFocusTarget.InvertThemeColors` を取り除き、CheckBox の UIResponder が first responder になれずスキップされる偶発挙動に依存しないようにした
- `ButtonFocusVisualPolicy.ResolveBackgroundColorHex` の focus tint 値を `#E6E6E6` / `#3D3D3D` から `#C8C8C8`（ライト）/ `#5A5A5A`（ダーク）に変更し、`Resources/Styles/Styles.xaml` の `Button` デフォルト fill（`Light=#E6E6E6, Dark=#3A3A3A`）と区別がつくようにした。従来値は idle fill とほぼ同色だったため、モーダルの `Cancel` / `Save` や conflict ダイアログの `Reload` / `Overwrite` / `Cancel` がフォーカスされても見た目が変わらず（フォーカス自体は移っていた）、本来意図していた「ライトは少し暗く、ダークは少し明るい」コントラストが見えていなかった
- `MainPage.ViewModelOnPropertyChanged`（`SelectedTheme` 経路）と `Application.Current.RequestedThemeChanged` から `ApplyContextActionButtonFocusVisuals` に加えて `ApplyModalActionButtonFocusVisuals` と `ApplyConflictActionButtonFocusVisuals` も呼ぶようにし、`Cancel` / `Save` やコンフリクトダイアログのボタンにフォーカスがある状態で `Ctrl+Shift+L/D/H` でテーマを切り替えても、新テーマのグレーで focus tint を塗り直すようにした（旧テーマの色が残らない）
- Edit/Delete オーバーレイは Windows / Mac Catalyst のどちらでも、メニュー外をクリックすると閉じるようになった。Edit/Delete・編集・conflict の各オーバーレイは全画面の hit target を共有し、`Grid` では MAUI iOS 経路で gesture が拾えなかったため `Border` をターゲットにしている。ほぼ黒の `#01000000` で塗ることで視覚的にニュートラル。編集と conflict の hit target は下層クリックを止めるだけでダイアログ自体は閉じない（conflict layer は conflict panel の背面、かつ下層に残り得る編集モーダルの上）
- Mac Catalyst のコンテキストメニュー（Edit/Delete）は、配置領域・Dock・command 候補のいずれから開いた場合でも、矢印キー選択後の Return / Enter 単発で対象アクションを発火するようになった。Mac 側は private nested `MacContextMenuKeyCaptureView` UIView をホストにアタッチして first responder にし、Return / Arrow / Tab / Escape を `App.RaiseEditorShortcut` へ中継して実装している
- 配置領域の選択矩形は、Windows / Mac Catalyst のどちらでもマウスリリース時に瞬時に消えるのではなくフェードアウトするようになった
- Dock スクロールバーの下に見えていた 2 本の余分な横バーを削除（`DockScrollBarMask` の塗りを透明化し、透明背景になった配置領域 / Dock の `Border` に残っていたドロップシャドウを除去）
- モーダル編集画面の Invert Theme チェックボックスとその横のラベルにも、Cancel / Save ボタンと同じ pointing-hand ホバーカーソルを適用
- メインウィンドウに `MinimumWidth=860` / `MinimumHeight=600` を設定し、編集モーダル（`WidthRequest=760` + padding）が常に収まるサイズを保証。ウィンドウを極端に小さくしても UI 要素がコンテナからはみ出さない

## [1.1.13] - 2026-04-30

### 変更
- 配置領域のランチャーボタンは hover 時に既定の矢印カーソルのままとなり（従来の pointing-hand ではなく）、主ポインタが押下されている間だけ「掴んだ手」の grab カーソルへ切り替えるよう変更。これによりボタンのドラッグ移動が「掴んで動かす」操作として読み取れるようになり、ただ hover しているだけのときはクリック可能に見えすぎない挙動になる。Dock ボタンは意図的に従来の hover-hand カーソルのまま維持する（「Dock クリックで起動」のサイン）
- macOS の配置領域 grab カーソルは、pointer release を取り逃した場合でも pointer move / exit のタイミングで arrow に復帰するようにし、閉じた手のまま残り続けるケースを防ぐ。複数の選択済みボタンの間をまたいで pointer が移動しても、共有された grab 状態が解除されるようにした
- macOS の配置領域・Dock のランチャーボタンのラベルは、`12pt` ではなく `14pt` を使うようになり、レイアウトを変えずに文字を一回り大きく読める

### 追加
- `Behaviors/GrabHandCursorBehavior.cs` を追加し、配置領域の pointer press/release/move/enter/exit に応じたカーソル切替（macOS は `NSCursor.closedHandCursor`、Windows は代替として `InputSystemCursorShape.SizeAll` を `ProtectedCursor` 経由で適用）を `HoverHandCursorBehavior` と同じプラットフォーム配線で実装。ただし hover ではなく pointer-pressed 状態を基準にし、かつ「主ポインタのみ押されている」場合だけ grab カーソルを出す（右クリック＝コンテキストメニュー、中クリック＝エディタ起動では grab カーソルに切り替えない）。加えて `MainPage.Draggable_PointerMoved` が Windows で使っている「`PointerReleased` 欠落時の primary 再判定」fallback と同じ考え方で、`PointerMoved` / `PointerExited` でも「もう primary が押されていない」と判定したら grab カーソルを解除する。複数の grab 対象ボタンの間を移動するケースでも、共有された active grab が解除されるようにしている。`OnDetachingFrom` では gesture recognizer を外す前に（grab 中なら）既定カーソルへ戻しておくことで、テンプレート再生成／絞り込み／外部同期削除など押下中の teardown でも Mac の `NSCursor.closedHandCursor` や Windows の `ProtectedCursor` が detach 済み platform view に残らないようにする
- `Praxis.Core/Logic/PointerButtonClassifier.cs` を追加し、secondary / middle ポインタボタン判定の reflection ルール（`Type`, `PressedButton` / `Button` / `Buttons`, `ButtonMask`, `ButtonNumber`, `CurrentEvent` に加えて `GestureRecognizer` / `Event` チェーンを辿る）を共通化。`MainPage.PointerAndSelection.cs` と同じ粒度で `GrabHandCursorBehavior` も Mac Catalyst の判定をこの classifier へ委譲し、プラットフォーム引数の `ToString()` 部分一致だけに頼らないようにする

### テスト
- `AppLayerSourceGuardTests` に `GrabHandCursorBehavior` のプラットフォーム配線ガードと、配置領域のランチャーボタンが grab-cursor behavior を貼っていることを固定するアサーションを追加し、hover-hand の XAML 出現数は 16 まで下がる（Dock・上部 Create・モーダルの copy/action・context・conflict の各ボタンは引き続き `HoverHandCursorBehavior` を共有）。加えて `PointerMoved` 購読、Mac の primary-only 判定を `PointerButtonClassifier.IsPrimaryOnly(...)` に委譲していること、`PointerMoved` / `PointerExited` 経由の primary-release fallback、`OnDetachingFrom` で gesture recognizer 解除より前に（grab 中なら）カーソルを戻すこともソースレベルで固定する
- `PointerButtonClassifierTests` を追加し、`Type = "OtherMouseDown"`・`ButtonNumber` の値域・middle (`0x4` / `0x8` / `0x10`) と secondary (`0x2`) の `ButtonMask` ビット・`IsMiddleButtonPressed` / `IsRightButtonPressed`・`PressedButton` / `Button` / `Buttons` のテキスト判定・`CurrentEvent` / `GestureRecognizer` / `Event` チェーン走査までを fake object を使った policy テストで固定する

## [1.1.12] - 2026-04-21

### 修正
- `SecondaryFailureLogger` は二次 fallback sink root を `Praxis/secondary-failures.log` の組み立て前に絶対パスへ正規化するようになり、quote 付き絶対パス override は引き続き使える一方、空や相対 root は誤って相対診断パスを作らないよう無視するよう修正
- `FileStateSyncNotifier` は読込リトライ枯渇 warning breadcrumb にも正規化済み sync-file path を含めるようになり、`buttons.sync` の読込失敗時に例外要約だけでなく対象ファイルも特定できるよう修正
- `MauiThemeService` は Mac の dispatch-failure warning breadcrumb に対象 `AppTheme` も含めるようになり、Light/Dark/System のどのテーマ遷移で失敗したかを generic な window-style warning より具体的に追えるよう修正
- `FileAppConfigService` は invalid theme の skipped-config warning breadcrumb に正規化済みの生 `theme` 値も含めるようになり、壊れた `praxis.config.json` が返した実値を「無効だった」という事実だけでなく具体的に残せるよう修正

### テスト
- `SecondaryFailureLoggerTests` を拡張し、quote 付き絶対パスの fallback root と、相対 fallback root を拒否する挙動を startup diagnostics file パス組み立て前に固定
- `FileStateSyncNotifierTests` と `AppLayerSourceGuardTests` を拡張し、読込リトライ枯渇 warning に入る正規化済み sync-file path prefix を固定
- `AppLayerSourceGuardTests` を拡張し、`MauiThemeService` の Mac dispatch-failure breadcrumb が要求された `AppTheme` を含むことを固定
- `FileAppConfigServiceTests` と `AppLayerSourceGuardTests` を拡張し、invalid theme の skipped-config warning に入る正規化済み `theme` 値を固定
## [1.1.11] - 2026-04-17

### 変更
- `MainViewModel` は `ClearCommandInput` でクリア文字数／no-op を、`ExecuteCommandInputAsync` で実行コマンド長と候補選択フラグを明示的に `LogInfo` するようになった。コマンド入力経路の直後にクラッシュした際に、ハンドラ側のタップログだけから意図を逆算する必要がなくなり、ViewModel 側でも breadcrumb が確実に残る
- `MainViewModel.ClearSearchText` も `ClearCommandInput` と同形の breadcrumb（クリア文字数／no-op）を記録するようになり、検索欄 X ボタンのタップ経路でクラッシュが続いた場合も、コマンド側と同等の ViewModel 側 evidence が残るようになった
- `MainPage.FocusEntryAfterClearButtonTap` / `ApplyEntryFocusAfterClearButtonTap` は対象 Entry と retry 件数、および完了マーカーを `crash.log` に出力し、`Entry.Focus()` 自体も try/catch で包んで MAUI ハンドラ内の失敗を crash-file 例外として顕在化するようになり、クリア直後のフォーカス復帰経路で落ちても無言のプロセス終了にならないようにした

## [1.1.10] - 2026-04-15

### 修正
- `CommandWorkingDirectoryPolicy` は Windows のシェル実行ファイル名を大文字小文字を区別せず判定するようになり、大文字・混在表記の `cmd.exe` / `powershell.exe` / `pwsh.exe` / `wt.exe` パスでも Praxis プロセスディレクトリを継承せず `WorkingDirectory` をユーザープロファイルに切り替えるよう修正
- `LaunchTargetResolver` は先頭/末尾が引用符の有効なパスターゲットを維持し、環境変数展開後の引用符付きルート/ホーム/相対パスおよび `file://` プレフィックスを正規化し、引用符付きの非 file URI スキームプレフィックスは明示的に除外することで、不正な引用符付き URL も確実に fail-closed するよう修正
- `CrashFileLogger` は、カスタム例外の `Message` / `StackTrace` ゲッターや `Exception.Data` のキー/値の `ToString()` が例外を投げた場合でも crash-log レコードを残すよう維持し、例外メッセージを単一行に整形することで、不正なペイロードでインラインログ書式が壊れないよう修正
- `CrashFileLogger.SafeExceptionMessage(...)` は空白のみの例外メッセージを `(empty)` に正規化するようになり、例外本体が存在するが空の場合でも、警告 breadcrumb が末尾区切り子だけの空行にならないよう修正
- `DbErrorLogger` は同じ単一行例外メッセージ正規化とゲッター失敗フォールバックマーカーを `ErrorLogEntity` に永続化するようになり、アプリ/プロセス終了時の flush 失敗や Windows 起動ログ書き込み失敗時にも警告 breadcrumb より先に例外本体を記録し、通常の `%LOCALAPPDATA%\\Praxis` crash sink が使えない場合は独立した temp/カレントディレクトリの診断ファイルへフォールバックするよう修正
- `SecondaryFailureLogger` は、起動時のターゲットパスや操作名の断片もフォールバック警告/本文行に埋め込む前に正規化するようになり、不正な起動ログメタデータが二次診断ファイルを壊さないよう修正
- `MainViewModel` の警告経路は、外部テーマ同期、コマンド候補の更新/参照、競合コールバック、クリップボードヘルパー、同期通知、ローカル永続化のフォローアップログで敵対的な例外 `Message` ゲッターに遭遇した場合に共通の安全な例外メッセージヘルパーを使うよう修正し、劣化時の警告ログがリカバリ経路から再スローしないよう修正
- `AppStoragePaths` はレガシー DB マイグレーションおよび無効パス比較の警告に同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターの場合でも起動時マイグレーションが不正な候補を確実にスキップし続けるよう修正
- `AppStoragePaths` はマイグレーションのソース/比較パス断片も警告プレフィックスに埋め込む前に正規化するようになり、不正なパステキストがレガシーマイグレーション中に crash-log の行構造を壊さないよう修正
- `FileAppConfigService` は、スキップされた設定読み込みが `IOException` / `UnauthorizedAccessException` / `JsonException` を投げた際にも同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターでも警告ログの breadcrumb が確実に残るよう修正
- `FileAppConfigService` は設定パス断片も invalid-theme / skipped-config の breadcrumb に埋め込む前に正規化するようになり、改行を含む候補パスが crash-log の行構造を壊さないよう修正
- `CommandExecutor` は、launch-target 解決およびネイティブプロセス起動失敗メッセージで同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターでフォールバック警告/結果構築が再スローしないよう修正
- `CommandExecutor` はツール / URL / パス / 引数の断片も失敗プレフィックスに埋め込む前に正規化するようになり、改行を含む launch target が crash-log の breadcrumb 書式を壊さないよう修正
- `MauiThemeService` は Mac ディスパッチ失敗の breadcrumb に同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターでテーマ適用警告ログが再スローしないよう修正
- `FileStateSyncNotifier` は書き込み/読み取り/予期しない publish の警告構築を共通の安全な例外メッセージヘルパーへ通すようになり、敵対的な例外 `Message` ゲッターでも同期 breadcrumb が残るよう修正
- `FileStateSyncNotifier` は不正ペイロードや observed-source の断片も crash-log の警告/情報行に埋め込む前に正規化するようになり、同期ファイルに改行や空白のみのペイロードマーカーが含まれていても同期 breadcrumb が単一行を保つよう修正
- `FileStateSyncNotifier` は同期ファイルのパス断片も write-success/write-failure の breadcrumb に埋め込む前に正規化するようになり、不正なストレージパスが crash-log の行構造を壊さないよう修正
- `Windows CommandEntryHandler` は互換性起因および予期しない `InputScope` 割り当て警告で同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターで WinUI フォールバックログが再スローしないよう修正
- Mac の `AppDelegate` および Mac entry/editor/command ハンドラは `MarshalManagedException` フック、key-command 優先度、`UIKeyCommand` 入力解決の警告 breadcrumb で同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターでそれらのフォールバック経路が再スローしないよう修正
- Mac entry/editor/command ハンドラはリフレクションで取得した `UIKeyCommand` 入力名も警告 breadcrumb に埋め込む前に正規化するようになり、不正なリフレクションメタデータが crash-log の行構造を壊さないよう修正
- Mac `Program` は LaunchServices リレー失敗の breadcrumb で同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターで open-relay 警告ログが再スローしないよう修正
- Mac `Program` は LaunchServices バンドルパス断片もリレー breadcrumb に埋め込む前に正規化するようになり、不正なバンドルパスが crash-log の行構造を壊さないよう修正
- `MiddleClickBehavior` と `MainPage.MacCatalystBehavior` は `buttonMaskRequired`、遅延 middle-click 実行、Mac エディタの key-command 作成、CoreGraphics フォールバックの警告 breadcrumb で同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターでそれらの劣化 Mac 入力経路が再スローしないよう修正
- `MainPage` はコピー通知、ステータスフラッシュ、Dock hover-exit、Quick Look アニメーションの警告 breadcrumb で同じ安全な例外メッセージヘルパーを使うようになり、敵対的な例外 `Message` ゲッターで非致命的な UI リカバリ経路が再スローしないよう修正
- `MainPage` はボタンタップ実行、セカンダリタップの新規作成フロー、モーダル primary フォーカスのフォールバック、`UseSystemFocusVisuals`、`IsTabStop` の警告 breadcrumb も同じ安全な例外メッセージヘルパー経由にするようになり、より多くの Windows/UI フォールバック経路が敵対的な例外 `Message` ゲッターで再スローしないよう修正
- `MainPage` と `App` はフォールバック初期化 UI テキストも `CrashFileLogger.SafeExceptionMessage(...)` 経由で取得するようになり、敵対的な例外 `Message` ゲッターが最終手段のエラーページ / アラート面自体を壊せないよう修正
- `CrashFileLogger.SafeObjectDescription(...)` は `Exception` 以外の `AppDomain.UnhandledException` ペイロードを強化し、敵対的なオブジェクトの `ToString()` 実装が base `App`、Windows 起動ログ、Mac `AppDelegate` の最終手段グローバル例外経路を壊せないよう修正

### テスト
- `CommandWorkingDirectoryPolicyTests` を拡張し、混在表記のシェル実行ファイル名や大文字の環境変数展開シェルパスをカバー
- `LaunchTargetResolverTests` を拡張し、引用符付き相対/`file://` パスプレフィックス、引用符境界のパス名、環境変数展開前後における不正な引用符付き URL の扱いをカバー
- `CrashFileLoggerTests`、`DbErrorLoggerTests`、`SecondaryFailureLoggerTests`、`AppLayerSourceGuardTests` を拡張し、複数行例外メッセージの正規化、カスタム例外のゲッター/data フォーマッタが投げるケース、主要 crash sink が書けないときに独立ファイルへフォールバックする起動ログ失敗診断をカバー
- `CrashFileLoggerTests` を拡張し、source/context 正規化ヘルパーの直接呼び出しと、永続化された crash breadcrumb 挙動を同時にカバー
- `CrashFileLoggerTests` を拡張し、null に対する `NormalizeSource(...)` ヘルパーの直接挙動をカバー
- `CrashFileLoggerTests` を拡張し、null に対する `NormalizeContext(...)` ヘルパーの直接挙動をカバー
- `CrashFileLoggerTests` を拡張し、`NormalizeMessagePayload(...)` ヘルパーの直接挙動と永続化された crash breadcrumb 挙動を同時にカバー
- `CrashFileLoggerTests` を拡張し、`SafeExceptionMessage(...)` フォールバックマーカー内の複数行/空白のみのゲッター失敗メッセージをカバー
- `CrashFileLoggerTests` を拡張し、複数行に対する `NormalizeExceptionMessage(...)` ヘルパーの直接挙動をカバー
- `CrashFileLoggerTests` を拡張し、空白のみに対する `NormalizeExceptionMessage(...)` ヘルパー挙動をカバー
- `CrashFileLoggerTests` を拡張し、null に対する `NormalizeExceptionMessage(...)` ヘルパー挙動をカバー
- `CrashFileLoggerTests` を拡張し、null に対する `SafeObjectDescription(...)` ヘルパーの直接挙動をカバー
- `CrashFileLoggerTests` を拡張し、`SafeObjectDescription(...)` 内の空白のみのオブジェクトフォーマッタ失敗マーカーをカバー
- `CrashFileLoggerTests` を拡張し、`SafeObjectDescription(...)` 内の複数行オブジェクトフォーマッタ失敗マーカーをカバー
- `CrashFileLoggerTests` を拡張し、空スタックに対する `SafeExceptionStackTrace(...)` ヘルパーの直接挙動をカバー
- `CrashFileLoggerTests` を拡張し、`SafeExceptionStackTrace(...)` 内の複数行スタックトレースゲッター失敗マーカーをカバー
- `CrashFileLoggerTests` を拡張し、`SafeExceptionStackTrace(...)` 内の空白のみのスタックトレースゲッター失敗マーカーをカバー
- `SecondaryFailureLoggerTests` を拡張し、null に対するターゲットパス/操作名正規化ヘルパーの直接挙動をカバー
- `SecondaryFailureLoggerTests` を拡張し、両方のフォールバック sink ルートが阻害された場合の `false` / null-path 結果をカバー
- `DbErrorLoggerTests` を拡張し、persist 失敗 breadcrumb 内で正規化された複数行 context をカバー
- `DbErrorLoggerTests` を拡張し、Warning persist 失敗 breadcrumb 内で正規化された複数行 context をカバー
- `DbErrorLoggerTests` を拡張し、Info persist 失敗 breadcrumb 内で正規化された複数行 context をカバー
- `DbErrorLoggerTests` を拡張し、purge 失敗 breadcrumb 内で正規化された複数行 context をカバー
- `SecondaryFailureLoggerTests` と `AppLayerSourceGuardTests` を拡張し、それらの断片がフォールバック診断に到達する前段での起動ターゲットパス/操作名正規化をカバー
- `SecondaryFailureLoggerTests` を拡張し、ターゲットパス正規化ヘルパーの直接挙動をカバー
- `SecondaryFailureLoggerTests` を拡張し、空白のみのターゲットパス正規化ヘルパー挙動をカバー
- `SecondaryFailureLoggerTests` を拡張し、複数行の操作名正規化ヘルパー挙動をカバー
- `MainViewModelWorkflowIntegrationTests` と `AppLayerSourceGuardTests` を拡張し、`MainViewModel` の警告経路（外部テーマ同期、コマンド参照フォールバック、競合コールバック、クリップボードフォローアップログ、同期通知、テーマ永続化）における敵対的な例外メッセージゲッターをカバー
- `AppLayerSourceGuardTests` を拡張し、Mac キー入力警告 breadcrumb 構築前のリフレクション `UIKeyCommand` 入力名正規化をカバー
- `AppStoragePathsTests` と `AppLayerSourceGuardTests` を拡張し、レガシーマイグレーション警告構築における敵対的な例外メッセージゲッターをカバー
- `AppStoragePathsTests` と `AppLayerSourceGuardTests` を拡張し、レガシーマイグレーション breadcrumb 書き込み前のマイグレーションソース/比較パス正規化をカバー
- `AppStoragePathsTests` を拡張し、`NormalizePathForLog(...)` 内の null ログ対象パス正規化の直接挙動をカバー
- `FileAppConfigServiceTests` と `AppLayerSourceGuardTests` を拡張し、skipped-config 警告構築における敵対的な例外メッセージゲッターをカバー
- `FileAppConfigServiceTests` と `AppLayerSourceGuardTests` を拡張し、invalid-theme / skipped-config の breadcrumb 書き込み前の設定パス正規化をカバー
- `FileAppConfigServiceTests` を拡張し、`NormalizePathForLog(...)` 内の null ログ対象設定パス正規化の直接挙動をカバー
- `FileAppConfigServiceTests` を拡張し、空白/相対の設定ルートを候補列挙で拒否する挙動をカバー
- `FileAppConfigServiceTests` を拡張し、null に対する `NormalizeAbsoluteDirectory(...)` ヘルパーの直接挙動をカバー
- `FileAppConfigServiceTests` を拡張し、`NormalizeAbsoluteDirectory(...)` 内の引用符付き相対パス拒否挙動をカバー
- `CommandExecutorTests` と `AppLayerSourceGuardTests` を拡張し、launch 失敗メッセージ構築における敵対的な例外メッセージゲッターをカバー
- `CommandExecutorTests` と `AppLayerSourceGuardTests` を拡張し、launch-failure breadcrumb 構築前のツール / URL / パス / 引数の正規化をカバー
- `CommandExecutorTests` を拡張し、`NormalizeTargetForLog(...)` 内の null ターゲット断片正規化の直接挙動をカバー
- `CommandExecutorTests` を拡張し、null に対する `NormalizeToolPath(...)` ヘルパーの直接挙動をカバー
- `CommandExecutorTests` を拡張し、null に対する `HasUsableTool(...)` ヘルパーの直接挙動をカバー
- `AppLayerSourceGuardTests` を拡張し、Mac リレー breadcrumb 書き込み前の LaunchServices バンドルパス正規化をカバー
- `FileStateSyncNotifierTests` を追加し、`AppLayerSourceGuardTests` を拡張して、sync 警告構築における敵対的な例外メッセージゲッターをカバー
- `FileStateSyncNotifierTests` と `AppLayerSourceGuardTests` を拡張し、それらの断片が警告/情報の crash-log 行に到達する前段での malformed / observed sync ペイロード正規化をカバー
- `FileStateSyncNotifierTests` を拡張し、`NormalizePayloadForLog(...)` 内の null 同期ペイロード正規化の直接挙動をカバー
- `AppLayerSourceGuardTests` を拡張し、write-success/write-failure breadcrumb 書き込み前の sync ファイルパス正規化をカバー

## [1.1.9] - 2026-04-14

### 修正
- `CrashFileLogger` と `DbErrorLogger` は、Warning / Info の message payload が `null` の場合でも `crash.log` と `ErrorLogEntity` へ書き込む前に `(no message payload)` へ正規化するようにし、劣化時のログ経路でも空欄ではなく明示的な診断証跡を残すよう修正

### 変更
- リポジトリ運用ガイダンスを現行の cdidx ワークフローへ追従。`CLAUDE.md` を cdidx 1.9.0 向けに更新し、追跡対象の `.claude/settings.json` では `rg` / `grep` / `find` 系シェル検索を拒否して、既定で cdidx-first の検索経路を維持するように変更

### テスト
- `CrashFileLoggerTests` / `DbErrorLoggerTests` の logging 回帰カバレッジを拡充し、永続化失敗 breadcrumb と Warning / Info の null payload 正規化を検証するよう追加
- `AppLayerSourceGuardTests` を更新し、Warning / Info の正規化後も file-first logging 順序ガードが現在の実装と一致するよう追従

## [1.1.8] - 2026-04-13

### 修正
- `CrashFileLogger.AppendExceptionChain` と `DbErrorLogger` の例外型/メッセージ構築を深さ 32 で上限化し、明示的な truncation マーカーを出力するよう修正。病的に深い inner exception チェーンで last-resort クラッシュロガーが StackOverflow に至らないよう保護

### テスト
- `CrashFileLogger.WriteException` と `DbErrorLogger.Log` に深い inner exception チェーン安全性テストを追加

## [1.1.7] - 2026-04-12

### 修正
- UI イベントハンドラ、同期通知、Dock/QuickLook タイマー、コマンド候補 debounce、クリップボードヘルパー、競合コールバックで未捕捉例外を `crash.log` に永続化するようにし、異常終了時のクラッシュ証跡を確保
- コマンド候補 debounce と Windows IME 再設定パスの `OperationCanceledException` ハンドリングを限定し、キャンセル以外の例外を握りつぶさないよう修正
- `DbErrorLogger` がログ永続化失敗時に inner exception の詳細を保持するよう修正

### 変更
- `MainViewModel` と `App` の重複 dispatcher ログヘルパーを共有 static メソッドに集約
- `CLAUDE.md` の cdidx ガイダンスを v1.6 向けに更新（inspect メタデータ、`suggest_improvement`、MCP 優先 SQL フォールバック方針）

### テスト
- `MainViewModel` の undo/redo ワークフロー統合テストを追加
- `AppLayerSourceGuardTests` を拡充し、新たに例外永続化対応した partial class を網羅
- `DbErrorLogger` の failure detail 保持テストを追加

## [1.1.6] - 2026-04-12

### 修正
- `DockOrderValueCodec.Parse()` は CSV 全体や各 GUID 要素を囲む quote も除去するようにし、quote 付きで保存された Dock 順序でも意図した並びを復元できるよう修正
- `WindowsPathPolicy.IsUncPath()` は quote 付き UNC を受理しつつ `\\\\?\\` と `\\\\.\\` のローカルデバイス接頭辞は共有パスとして誤判定しないよう修正
- Mac `AppDelegate` は key command の system 優先解除失敗も warning 記録するようにし、runtime の reflection 差異が無音にならないよう修正
- `QuickLookPreviewFormatter.BuildLine()` はラベル付き Quick Look 行全体でも要求 `maxLength` を超えないようにし、`label + ": "` 分で最終文字列が上限超過しないよう修正
- `CommandNotFoundRefocusPolicy` は `Command not found:` 判定前に先頭空白を無視するようにし、前置空白つき status でも再フォーカスを維持しつつ埋め込み語句とは誤一致しないよう修正
- Mac `AppDelegate` は `MarshalManagedException` hook 失敗も startup 中に無音で握りつぶさず warning 記録するよう修正
- `MacEntryHandler`、`MacEditorHandler`、Mac の `CommandEntryHandler` は `UIKeyCommand` 入力の reflection 解決失敗も warning 記録したうえで既定キー文字列へフォールバックするようにし、runtime API 差異で handler の型初期化ごと失敗しないよう修正
- `ThemeModeParser.NormalizeOrDefault` は、解析値と呼び出し元既定値の両方が不正 enum でも out-of-range 値を返さず `System` へ安全化するよう修正
- `DbErrorLogger` は background drain loop の予期しない失敗も `crash.log` に warning 記録するようにし、enqueue 済みでも以後のログ永続化失敗が無音にならないよう修正
- `FileStateSyncNotifier.NotifyButtonsChangedAsync` は sync payload の書込失敗も再送出前に warning 記録するようにし、ローカル save/delete 成功後に file signaling が壊れても breadcrumb を残すよう修正
- `MainPage` は Windows の focus-visual reflection 失敗、モーダル primary editor focus 失敗、`IsTabStop` reflection 失敗も warning 記録するようにし、フォールバック経路の silent failure をなくすよう修正
- Mac の middle-click/button-mask と editor key-command/CoreGraphics fallback は失敗時も warning 記録するようにし、native pointer / shortcut bridge 劣化時にローカル診断痕跡を残すよう修正
- Windows `CommandEntryHandler` は `InputScope` 代入の互換性由来失敗と予期しない失敗の両方を warning 記録したうえで無効化または拒否するよう修正
- `FileAppConfigService` は先頭設定ファイルが読めても `theme` 欠落または不正値だった場合にそこで `System` へ確定せず、後続候補へフォールバックするよう修正
- `FileAppConfigService` は壊れた設定・読めない設定・不正な theme 設定をスキップした理由を warning として残すようにし、後続候補へのフォールバック前に `crash.log` に診断 breadcrumb を残すよう修正
- `DbErrorLogger` は DB への永続化失敗や保持期間 purge 失敗も `crash.log` に warning 記録するようにし、非致命なリポジトリエラーでも shutdown / 劣化動作時の診断痕跡を残すよう修正
- `DbErrorLogger.FlushAsync(timeout)` は timeout や予期しない flush 失敗も warning 記録するようにし、graceful shutdown 中のログ欠落が無音にならないよう修正
- `Praxis/Platforms/MacCatalyst/Program.cs` の UTF-8 BOM を除去し、同 entrypoint を BOM-free に保つ repository encoding guard テストを追加して `cdidx validate` をクリーン化
- `CommandExecutor` は native process 起動失敗や launch-target-resolution 失敗も `crash.log` に warning 記録するようにし、ユーザー向け失敗メッセージの裏側にローカル診断 breadcrumb を残すよう修正
- `LaunchTargetResolver` は環境変数展開後の引用符も正規化するようにし、引用符付き env 由来の HTTP URL や filesystem path も正しく解決できるよう修正
- `AppStoragePaths` は壊れた legacy path 比較入力を無視するようにし、`Path.GetFullPath` 例外でストレージ移行チェック全体が止まらないよう修正
- `CommandRecordMatcher` は command 候補走査中に `null` コレクション要素を無視するようにし、候補生成で例外落ちしないよう修正
- `StateSyncPayloadParser` は二重区切り payload を空セグメント圧縮で受理しないようにし、壊れた同期シグナルを拒否するよう修正
- `QuickLookPreviewFormatter` は省略記号つき短縮後も要求 `maxLength` を超えないようにし、呼び出し元の長さ制約を破らないよう修正
- `ButtonSearchMatcher`、`LogRetentionPolicy`、`LauncherButtonOrderPolicy` は `null` 要素を無視するようにし、検索・保持期間計算・配置正規化 helper 内で例外落ちしないよう修正
- `CommandWorkingDirectoryPolicy` は shell 判定前に環境変数つき tool path を展開するようにし、`%ComSpec%` や引用符付き `%...%` Windows shell tool でもユーザープロファイル起点 working directory を選べるよう修正
- `CommandLineBuilder` は quoted-empty の tool 値を空として扱うようにし、実行時の empty-tool 判定と preview/status 表示を整合させるよう修正
- `AppStoragePathLayoutResolver` は app-data path 合成前に保存先 root の外側引用符を除去するよう修正
- `GridSnapper` と `ModalEditorScrollHeightResolver` は非有限数値入力を安全化するようにし、`NaN` / infinity がレイアウト計算へ伝播しないよう修正
- Windows startup-log 追記失敗と `MainPage` copy notice animation 失敗は、無言で握りつぶさず `crash.log` に breadcrumb を残すよう修正

## [1.1.5] - 2026-04-11

### 修正
- `MainViewModel.CommandSuggestions` は候補ポップアップ close / refresh の main-thread dispatch 失敗も warning ログに残すようにし、スケジューリング失敗を無言で消さないよう修正
- `MauiThemeService.Apply()` は適用済み theme への no-op 再適用を避けるようにし、Mac の window-style dispatch 失敗は `crash.log` に残すよう修正
- Mac Catalyst の open relay 起動は `Process.Start(...) == null` も失敗扱いにし、LaunchServices relay 失敗を無言で握り潰さず `crash.log` へ記録するよう修正
- `FileStateSyncNotifier.Dispose()` は watcher の unsubscribe / dispose 前に `EnableRaisingEvents` を false にし、teardown 中の遅延 callback race を減らすよう修正
- ベース `App` のグローバル unhandled-exception / process-exit hook は一度だけ登録するようにし、アプリ初期化が再入した場合でも crash / flush handler が多重化しないよう修正
- `MainViewModel.SyncThemeFromExternalChangeAsync()` は dispatch 先メインスレッド適用で発生した失敗も warning ログ化するようにし、外部 reload の `TaskCompletionSource` には `RunContinuationsAsynchronously` を付けて継続の再入を抑制
- `FileStateSyncNotifier.NotifyButtonsChangedAsync()` は dispose 後は no-op にし、teardown 中に stale な sync file 書き込みを試みないよう修正
- `AppStoragePaths.TryMigrateLegacyDatabase()` はコピー失敗を warning 記録しつつ次の legacy 候補探索を継続するようにし、最初の読めない DB で移行全体が止まらないよう修正
- `FileAppConfigService` は先頭設定ファイルが `UnauthorizedAccessException` で読めない場合でも後続候補へフォールバックするよう修正
- `MainPage.OnDisappearing()` は window activation hook を解除し、解除経路では Mac の activation observer も解放するようにして、非表示ページへ stale な activation callback が残らないよう修正
- `CommandExecutor` は `~` / `~/...` / `~\\...` のような home 省略つき `tool` も実行可否判定前に展開するようにし、empty-tool path launch と同じ shorthand を direct tool launch でも使えるよう修正
- Windows の `startup.log` は raw な local-app-data special folder 文字列ではなく正規化済み共有 app-storage root を使うようにし、quote 付きや欠落した環境値でパスが壊れにくいよう修正
- Windows / Mac の platform startup class はグローバル例外 hook の多重登録を防ぐガードを追加し、初期化の再実行で unhandled-exception handler が重複しないよう修正
- `App.CreateWindow()` は `ResolveRootPage()` 失敗時のエラー表示ページを恒久キャッシュしないようにし、後続ウィンドウ生成で初回失敗ページに固定されず回復できるよう修正
- `App` は `AppDomain.UnhandledException` の終端処理と `ProcessExit` の両方でログ flush 失敗を黙殺せず `crash.log` に残すよう修正
- `FileStateSyncNotifier` は sync ファイルの再読込リトライが尽きた場合に warning を残し、読めない外部同期 payload を無言で捨てないよう修正
- `CommandExecutor` は正規化後に空になる tool 値（空引用符や引用符内空白）を「tool 未指定」として扱い、空ファイル名を実行しようとせず URL / path フォールバックへ回すよう修正
- `MainPage` は XAML 読込失敗を `crash.log` に記録し、初回初期化失敗時は初期化済みフラグを戻して再試行可能にし、初期化エラー表示自体が失敗した場合も元の例外を消さず別途 `crash.log` へ残すよう修正
- `MauiClipboardService` は clipboard 読み書きの両方で `CancellationToken` を尊重し、キャンセル済み操作を無視して走らせないよう修正
- `SqliteAppRepository` はスキーマ更新と初回キャッシュ読込が成功するまで共有 SQLite 接続を公開しないようにし、初期化途中失敗後に安全に再試行できるよう修正
- `SqliteAppRepository.SetThemeAsync` は範囲外の `ThemeMode` 値を `System` へ正規化して保存し、外部同期で `dock_order` が空になった場合は古い Dock 表示を残さず明示的にクリアするよう修正
- `MainViewModel` は外部 reload/theme 同期失敗、command 候補再計算失敗、競合ダイアログ callback 失敗を無言で握り潰さず warning ログへ残すよう修正
- Windows のクリアボタン後 native refocus 失敗は `crash.log` へ直接同期記録するようにし、freeze/abort 系の診断痕跡を残しやすくした
- 起動、外部同期、実行リクエスト、クリップボード反映、クリアボタン、sync signal の境界に低コストな Info breadcrumb を追加し、GUI ハング/abort 時に最後に成功していた段階を追いやすくした
- clipboard と sync notifier の失敗を主処理から分離し、clipboard 引数読込は空文字へフォールバック、コピー失敗は warning/status 化、clipboard 反映失敗後も実行ログを保持し、save/delete/theme/dock/history は同期通知失敗で巻き戻らないようにした

## [1.1.4] - 2026-04-09

### 追加
- .codex および CLAUDE.md を追加

### 修正
- CI および Delivery ワークフローで .NET 10 プレビューランタイムパッケージの復元が失敗する問題を修正（`NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64 with version (= 10.0.5)`）— 固定指定の `dotnet-version: 10.0.100` を `10.0.x` + `dotnet-quality: preview` に変更しプレビュー NuGet フィードを有効化

## [1.1.3] - 2026-04-05

### 追加
- `v*` タグ push 時に GitHub Release を自動作成 — delivery ワークフローが OS 別アーティファクト（Windows / macOS）を zip 化し、自動生成リリースノート付きで公開

## [1.1.2] - 2026-03-31

### 追加
- 同期ファイルベースのクラッシュロガー（`CrashFileLogger`）: 全ログ呼び出しで `crash.log` に即座に同期書き込みし、非同期 DB 書き込みが完了しないまま異常終了してもログを保持
  - クロスプラットフォーム対応: Windows `%LOCALAPPDATA%\Praxis\crash.log`、macOS `~/Library/Application Support/Praxis/crash.log`
  - 512 KB での自動ログローテーション
  - InnerException、`AggregateException` 展開、`Exception.Data` 辞書を含む完全な例外チェーン出力
- `IErrorLogger.LogWarning(message, context)` — 警告レベルのログエントリ追加
- `IErrorLogger.FlushAsync(timeout)` — シャットダウン時に保留中の非同期 DB 書き込みをドレイン
- `AppDomain.ProcessExit` ハンドラでプロセス終了前にログをフラッシュ
- `UnhandledException` ハンドラで `IsTerminating=true` の場合に同期的フラッシュを試行
- Mac Catalyst `AppDelegate` にクラッシュファイルログフック追加（`UnhandledException`、`UnobservedTaskException`、`MarshalManagedException`）
- Windows プラットフォーム例外ハンドラが `startup.log` と `crash.log` の両方に出力
- `UnhandledException` ハンドラで Exception 以外のスローオブジェクトも記録

### 変更
- `DbErrorLogger` を書き換え: 全 `Log`/`LogInfo`/`LogWarning` 呼び出しでまずクラッシュファイルに同期書き込みし、次に `ConcurrentQueue` 経由で非同期 DB 書き込みをキューイング（従来の fire-and-forget `_ = LogAsync()` パターンを置換）
- エラーログエントリが完全な例外型チェーン（例: `InvalidOperationException -> NullReferenceException`）、連結された内部メッセージ、`Exception.ToString()` による完全スタックトレースを記録
- `ErrorLogEntity.Level` 列が `Error`・`Info` に加えて `Warning` を受容
- `ResolveRootPage` の失敗を `IErrorLogger` でログ出力（従来は無言で握り潰し）

## [1.1.1] - 2026-03-28

### 修正
- GitHub Actions の checkout で履歴をフル取得するようにし、CI と配布ジョブで Nerdbank.GitVersioning の version height 計算が shallow clone で失敗しないよう修正
- macOS の GitHub Actions ジョブで Mac Catalyst の build / publish 前に Xcode を初期化し、fresh runner 上の `ibtoold` / Xcode プラグイン初期化失敗を回避

### 変更
- README 冒頭に CI / CodeQL / Delivery / .NET 10 / .NET MAUI / 対応プラットフォーム / SQLite / MIT License のバッジを追加

## [1.1.0] - 2026-03-28

### 追加
- ボタン単位の色反転（インバート）機能、DB スキーマ自動マイグレーション付き（v1 → v2）
- DB バックの ERROR / INFO 2レベルエラーログ（`ErrorLog` テーブル、30日保持、schema v3 → v4）
- 主要ユーザー操作への INFO ログ: ボタン/コマンド実行、エディタ開閉/保存/キャンセル/削除、テーマ変更、Undo/Redo、競合解決、ウィンドウ閉鎖
- ボタン変更（移動/編集/削除）の Undo/Redo: Windows は Ctrl+Z / Ctrl+Y、macOS は Command+Z / Command+Shift+Z
- ボタンホバー時の Quick Look プレビュー（Command / Tool / Arguments / Clip Word / Note）
- `PRAGMA user_version` による SQLite スキーマバージョン管理と順次自動マイグレーション
- `FileSystemWatcher` シグナルファイル（`buttons.sync`）によるウィンドウ間同期（自インスタンス発信は除外）
- 複数ウィンドウ同時編集の競合検出ダイアログ（`Version` 列による楽観的ロック）
- Enter キーで一致するすべてのコマンドを実行（先頭一致だけでなく全一致）
- Clip Word フィールドの複数行入力対応
- ボタン新規作成時に検索テキストを自動クリア
- コンテキストメニュー・競合ダイアログの矢印キーフォーカス循環（Windows/macOS）
- コマンド候補行へのミドルクリック・右クリック操作
- コマンド候補のデバウンスを 400 ms に延長し高速入力時のノイズを軽減
- 候補ポップアップ表示直後は自動選択せず、最初の `↓` キーで先頭候補を選択
- CI カバレッジ収集と Cobertura アーティファクトアップロード（GitHub Actions）
- Windows UNC パスを `explorer.exe` 経由で開き、存在確認前に認証ダイアログを表示可能に
- Dock 横スクロールバーを「Dock 領域ホバー中かつ横オーバーフロー時のみ」表示
- 色反転ラベルをタップしてチェックボックスをトグル可能に（チェックボックス本体以外もタップ可）

### 修正
- コンテキストメニュー表示時にコマンド候補を自動クローズ
- コマンド候補クリックでコマンド実行・入力欄への自動補完
- エディタモーダルの既定フォーカスを、Button Text / Command 入れ替え後の欄順に合わせて `ButtonText` へ修正
- 新規ボタン作成時は、モーダル初回フォーカスの `ButtonText` を Windows / macOS で全選択
- Windows: Tab フォーカス移動時にテキスト入力欄の全選択
- クリアボタンタップ後のフォーカス復帰を安定化（即時試行 + 短遅延リトライ）
- Windows: 上部 `Command` / `Search` のクリア後再フォーカスで stale な native `TextBox` を避け、まれな abort を抑止
- macOS: クリア後再フォーカスを次フレームへ遅延し、クリアボタン非表示切替中の responder 再入を回避
- Windows のクリアボタン X グリフの垂直方向センタリング
- テーマのライブ切替中もコマンド候補の色をテーマに同期
- `CommandEntry` / `SearchEntry` で英小文字が大文字変換される問題
- メインの command 入力欄で、Windows / macOS ともフォーカス時の IME / 入力ソース切替を行わないよう修正
- モーダル `Command` 欄の IME / ASCII 強制:
  - Windows: フォーカス時に `InputScopeNameValue.AlphanumericHalfWidth` + `imm32` ナッジ（即時 + 短遅延リトライ）
  - macOS: `AsciiInputFilter` + `setMarkedText` / `insertText` ブロック（アプリ非アクティブ時は即時解除）
- Windows のモーダル `Command` 欄でフォーカス中も IME を英字に再強制（手動 IME 切替を抑止）
- macOS: モーダル command 欄の ASCII 入力ソース強制をアクティブキーウィンドウのファーストレスポンダ中のみに限定
- macOS: 「Command not found」後にコマンド入力欄へフォーカスを戻し即時リトライ可能に
- macOS: ILLink によるクリアボタンパスの入力欠落を中間アセンブリコピーで防止
- Windows: モーダル/競合フォーカス復帰を 2 段リトライ化し Esc・Ctrl+S の取りこぼしを防止
- Windows: `InputScope` の `ArgumentException`（E_RUNTIME_SETVALUE）を一方向フラグで吸収し IME フォールバックへ継続
- Windows: Ctrl+Z/Y のアンドゥ粒度を保持（コマンド入力の `TextChanging` 書き換えを無効化）
- 単一ウィンドウ編集での競合ダイアログ誤検知を解消（インスタンス ID 自己フィルタ）
- 「Command not found」をニュートラル表示ではなくエラーフラッシュ（赤）として扱う
- macOS で別アプリから戻った後の編集モーダル再フォーカス

### 変更
- エディタモーダルのフィールド順を変更（Command を Button Text より前に）
- UI ボタンのフォント サイズを全プラットフォームで 12 に統一
- 配置領域と Dock のボタンの padding を 0 に統一
- Dock 領域の縦幅を拡張
- 新規ボタンアイコンをプレーンな `+` から線画ヘックスロゴに変更（外六角形・内接円・内六角形・中央 `+`）
- アプリアイコン・スプラッシュを六角形＋ポリゴンコントラストデザインに刷新（マイクロサイズ最適化バリアント付き）
- `MainPage` の責務分割をさらに細分化し、`EditorAndInput` は共有入力処理へ絞り込み、編集モーダル、ViewModel イベント配線、ステータス/テーマ、Dock/Quick Look、Windows ネイティブ入力フックを専用 partial へ分離
- `SqliteAppRepository` の全公開操作を排他制御で保護しスレッドセーフに
- UI 遅延値を `UiTimingPolicy` へ集約
- `MainPage` フィールドファイルと `MauiProgram` ハンドラ登録のプラットフォームプリプロセッサブロックを整理・統合
- 重複 `using` ディレクティブの削除と `using` 順序の正規化
- `MainViewModel` と各 partial クラスに主要ライフサイクルイベントの `LogInfo` 呼び出しを追加

[Unreleased]: https://github.com/Widthdom/Praxis/compare/v2.0.4...HEAD
[2.0.4]: https://github.com/Widthdom/Praxis/compare/v2.0.3...v2.0.4
[2.0.3]: https://github.com/Widthdom/Praxis/compare/v2.0.2...v2.0.3
[2.0.2]: https://github.com/Widthdom/Praxis/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/Widthdom/Praxis/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/Widthdom/Praxis/compare/v1.2.0...v2.0.0
[1.2.0]: https://github.com/Widthdom/Praxis/compare/v1.1.13...v1.2.0
[1.1.13]: https://github.com/Widthdom/Praxis/compare/v1.1.12...v1.1.13
[1.1.12]: https://github.com/Widthdom/Praxis/compare/v1.1.11...v1.1.12
[1.1.11]: https://github.com/Widthdom/Praxis/compare/v1.1.10...v1.1.11
[1.1.10]: https://github.com/Widthdom/Praxis/compare/v1.1.9...v1.1.10
[1.1.9]: https://github.com/Widthdom/Praxis/compare/v1.1.8...v1.1.9
[1.1.8]: https://github.com/Widthdom/Praxis/compare/v1.1.7...v1.1.8
[1.1.7]: https://github.com/Widthdom/Praxis/compare/v1.1.6...v1.1.7
[1.1.6]: https://github.com/Widthdom/Praxis/compare/v1.1.5...v1.1.6
[1.1.5]: https://github.com/Widthdom/Praxis/compare/v1.1.4...v1.1.5
[1.1.4]: https://github.com/Widthdom/Praxis/compare/v1.1.3...v1.1.4
[1.1.3]: https://github.com/Widthdom/Praxis/compare/v1.1.2...v1.1.3
[1.1.2]: https://github.com/Widthdom/Praxis/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/Widthdom/Praxis/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/Widthdom/Praxis/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Widthdom/Praxis/tree/v1.0.0
