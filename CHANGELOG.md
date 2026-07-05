# Changelog

This is a fork of [zsims/hunt-and-peck](https://github.com/zsims/hunt-and-peck).

## v1.3 — Modern options window, themes, live settings

### Added

- **Modern options window** with sidebar navigation (General / Overlay / Keyboard), card-based sections, and full keyboard support (arrow keys, Tab, Alt+C/R).
- **Theme system** — Light, Dark, and Skadi (Arknights) themes with runtime switching. Themes affect the options window, tray context menu, and hint overlay colors.
- **JSON-backed settings** (`%APPDATA%\Vimium\config.json`) with auto-save — no manual Save button needed.
- **Configurable hotkeys** via Options → Keyboard tab. Supports Ctrl/Alt/Shift/Win + any letter, digit, or symbol.
- **Hint color customization** — hex color inputs with 12 preset swatches for active/inactive hint backgrounds and text color.
- **Live font size preview** in the options window; hint colors update overlay in real time.
- **Theme-matching hint defaults** — switching themes auto-applies appropriate hint colors; manual override still works.
- **Skadi loading icon** — appears during hint generation when Skadi theme is active.

### Changed

- **Project renamed** from `HuntAndPeck` to `Vimium` (namespaces, folders, assemblies).
- Overlay loading indicator and match string now use hint background colors for visibility.
- Invoke actions run on a background thread to prevent nested-message-loop deadlocks when navigating Vimium's own UI.
- `ShowDialog()` for tray menu options — menu now blocks until closed.

### Fixed

- **Overlay stuck** — click anywhere on the overlay to dismiss; 5-second safety auto-close timer; Escape always works.
- **ComboBox theming** — custom template with properly themed toggle button, popup, and system color overrides.
- **Context menu theming** — tray menu now matches theme with custom templates for MenuItem, Separator.
- **Digit key parsing** (`Ctrl+1` → `D1`) in `HotKey.Parse`.

### Dev

- Unit tests for `VimiumConfig`, `HotKey.Parse`, `HexToColorConverter`, `HintLabelService` (26 tests, all green).
- `spec-driven-dev` Claude Code skill for iterative spec → build → verify workflow.

## v1.2 — Instant overlay, async hints

### Changed

- **Overlay appears instantly.** Hints are now enumerated on a background thread while the overlay shows a "Generating hints…" loading indicator, instead of blocking the UI for up to 1 second before anything appeared.
- **Async hint enumeration.** `FindAllBuildCache` and pattern resolution run off the UI thread so the app stays responsive even on complex windows with many elements.

### Added

- **Loading indicator** in the overlay: a pulsing "Generating hints…" label visible until hints are ready.

### Removed

- Dropped stale `CheckInterop` scratch project that was never part of the solution.

## v1.1 — Elevated, popup-friendly, faster

### Added

- **Multiple interaction modes** when selecting a hint:
  - Default: UI Automation invoke (the element's primary action).
  - **Left Shift**: real left click (works with Electron/web apps like Microsoft Teams where invoke does nothing).
  - **Right Shift**: real right click (e.g. open a context menu).
- **Runs elevated** (`requireAdministrator`) so hints work on other "Run as administrator" apps.
- **Auto-start support** via a scheduled task script ([src/register-startup-task.ps1](src/register-startup-task.ps1)) that starts the elevated app at logon without a UAC prompt.

### Changed

- **Overlay no longer steals focus.** It uses a no-activate, topmost window plus a global low-level keyboard hook to read hint keys, so popups, drop downs, and menus stay open while hints are shown.
- **Default hotkeys**: `Ctrl + ;` for the focused window, `Ctrl + '` for the taskbar.
- **Faster hint enumeration.** UI Automation data (bounding rectangles + patterns) is now batched into a single cached request (`FindAllBuildCache`) instead of hundreds of per-element cross-process calls, greatly reducing the time to show hints.
- Repeated hotkey presses while an overlay is open are now **ignored** instead of stacking multiple overlays. `Esc` still closes the overlay.

### Fixed

- **Crash** (`COMException` from `ExpandCollapse`/invoke on stale UI Automation elements) that could terminate the app. Hint actions are now guarded, with a global dispatcher safety net.

## v1.0 — Mouse interaction

### Added

- Move the mouse pointer to a hint and click as alternatives to UI Automation invoke.
