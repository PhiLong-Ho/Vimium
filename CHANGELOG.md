# Changelog

This is a fork of [zsims/hunt-and-peck](https://github.com/zsims/hunt-and-peck).

## v1.4 — Line Navigation Mode, App Icon Theming & Theme Rename

### Added

- **App icon theming** — the default application icon is now a keyboard for Light and Dark themes. Selecting the Arknights theme switches all app icons (system tray, settings window) to the Arknights-themed icon. Icon switching is immediate (≤500ms) with no restart required. See `specs/004-app-icon-and-theme-rename/`.
- **Theme rename: Skadi → Arknights** — the theme previously named "Skadi" has been renamed to "Arknights" across all user-facing UI (settings dropdown, labels). A legacy `"theme": "Skadi"` (or any unrecognized value) in an existing config resets **only** the `Theme` field to the default (Light) on load — all other settings are preserved and the value is NOT migrated to "Arknights". Users can re-select "Arknights" from the dropdown. See `specs/004-app-icon-and-theme-rename/`.
- **Version display** — the current application version (e.g. `v1.4.0.0`) is shown in the settings window footer, read from assembly metadata at build time. See `specs/005-version-and-admin-mode/`.
- **Administrator mode toggle** — a "Run as Administrator" checkbox in General settings lets users (e.g. in enterprise environments) opt out of elevation. The manifest now requests `asInvoker`; when the toggle is enabled (the default), Vimium self-elevates at startup via the Windows `runas` verb. The preference persists in `config.json` and a restart-required notice appears after a change. See `specs/005-version-and-admin-mode/`.
- **Line navigation mode** (`Ctrl+.` by default) — discover and label every visible text line in the foreground window via UI Automation TextPattern, independent of the existing element navigation mode.
- **Jump to line** — Type a hint label (without modifier) to move the cursor to the center of that text line.
- **Sub-line selection & copy** — Hold the copy modifier (`Ctrl` by default) while typing a hint label to enter **selection mode**: incremental search across all visible lines, Tab/Shift+Tab to cycle search matches, standard Windows navigation keys (arrows, Ctrl+arrow, Shift+arrow for selection), Home/End, Enter to copy (whole line or selection), Escape to cancel.
- **Clipboard integration** — Copied text goes to the system clipboard with retry handling for clipboard contention.
- **Configurable hotkeys** — Line navigation activation hotkey and copy modifier are configurable in Options → Keyboard. Changes take effect immediately (auto-save). Duplicate hotkey prevention with element overlay.
- **Mode isolation** — Element mode (`Ctrl+;`) and line mode (`Ctrl+.`) operate independently. Only one overlay can be active at a time. Each has its own keyboard hook and state.
- **Copy confirmation** — Brief "Copied!" toast animation in selection mode on Enter.
- **Zero-text handling** — Windows with no text (e.g., Paint) show "No text lines found" and auto-dismiss after 1.5s.
- **Blazing-fast hint enumeration** — Cold-start hints appear within 750ms for 200+ element apps (was 1–3s). Achieved via pattern-availability pre-filtering at the UIA provider level (40–60% fewer cross-process elements), conservative tree trimming, and result caching by window handle.
- **4-slot configurable hint actions** — Replace hardcoded Shift→Click behavior with four configurable modifier-action slots: Slot 0 (default, no modifier), Slots 1–3 with text-based modifier input and action dropdowns. Actions: Invoke, Left Click, Right Click, Hover.
- **Hover action** — Move cursor to element center without clicking. Triggers CSS `:hover` effects; cursor persists so hover-revealed UI stays visible for the next hint activation. Useful for tooltips, hover cards, and drop-down menus.
- **Non-overlapping hint labels** — Spiral-offsetting algorithm prevents label overlap on dense UIs (Discord, Slack). Labels try positions in priority order (default → above → below → right → left), stacking vertically as a last resort.
- **Per-action hint filtering** — When the default action is a click (Invoke/LeftClick/RightClick), only interactive elements receive hints, reducing visual noise. When default is Hover, all visible elements get hints.
- **Input buffering** — Keystrokes typed during hint enumeration are buffered and applied once hints appear — no lost input.
- **Benchmark logging** — Structured JSONL log at `%APPDATA%\Vimium\logs\benchmark.jsonl` with cold-start latency metrics. Analyzable with `scripts/parse-benchmark-log.ps1` (mean, median, P95).

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
