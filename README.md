# Vimium

> Vimium/vimperator style keyboard navigation for Windows — now on .NET 10.

A lightweight keyboard-driven UI overlay for Windows. Press a hotkey, type a hint, and interact with any control — buttons, links, menus, tabs — without touching the mouse. Built on the Windows UI Automation framework (like screen readers), so it works with almost any Windows desktop application.

## Features

| Feature | Description |
| --- | --- |
| **Instant overlay** | Overlay appears immediately; hints populate asynchronously in the background. |
| **Hint Overlay Improvements** | Pattern-availability pre-filtering at the UIA provider level, result caching by window handle, tree trimming, and cancellation support — cold-start enumeration ≤750ms for 200+ element apps. |
| **Multiple interaction modes** | Invoke (default), Left Click, Right Click, Move Mouse, Hover — all configurable per modifier slot via key-capture UI in Options → Actions. |
| **Customizable hint actions** | Three action slots: Slot 0 (default, no modifier), Slots 1–2 (configurable modifier + action). Assign any combination of Ctrl/Shift/Alt/Win plus an action type (Invoke, Left Click, Right Click, Move Mouse, Hover). |
| **Non-overlapping hint labels** | Spiral-offsetting algorithm prevents overlapping labels on dense UIs (Discord, Slack). Labels try positions in priority order (default → above → below → right → left → stacked). |
| **Multi-line wrapped link centering** | For wrapped text links (e.g., Wikipedia paragraphs), hint labels are vertically centered within the element to avoid appearing on the wrong line. |
| **Benchmark logging** | Structured JSONL benchmark logs at `%APPDATA%\Vimium\logs\benchmark.jsonl` with cold-start latency metrics. Parse with `scripts/parse-benchmark-log.ps1`. |
| **Input buffering** | Keystrokes typed during hint enumeration are buffered and applied once hints appear — no lost input. |
| **Find & navigate mode** | `Ctrl+.` opens a Chrome-style find bar. Type ≥5 characters, matches highlight live (yellow = all, orange = active), `Tab`/`Shift+Tab` cycle, `Enter` scrolls to and focuses the match. |
| **Themes** | Light, Dark, and Skadi themes with runtime switching via the options window. |
| **Modern options window** | Sidebar-navigated settings: font size, hint colors, actions, shortcuts — all auto-save. |
| **Configurable hotkeys** | Change overlay/taskbar/find activation shortcuts in Options → Keyboard. |
| **Elevated by default** | Runs as administrator so it can interact with elevated apps. |
| **Popup-friendly** | Overlay never steals focus — menus, dropdowns, and popups stay open. |
| **Auto-start** | Optional scheduled-task script for login launch without a UAC prompt. |
| **Taskbar mode** | `Ctrl + '` to highlight the Windows taskbar. |

## How to use

1. Launch **Vimium.exe** (runs in the system tray).
2. With any window focused, press **`Ctrl + ;`** to show hints for the active window.
   - Press **`Ctrl + '`** to show hints for the taskbar.
   - Hotkeys are configurable in Options → Keyboard.
3. Type the hint letters shown on the control you want, then release the modifier key.

### Interaction modes

Hold one of these modifiers while typing the hint (defaults shown — all three slots are
configurable in **Options → Actions** with key-capture controls):

| Slot | Modifier (default) | Action | Description |
| --- | --- | --- | --- |
| **0** | _(none)_ | **Invoke** | UI Automation invoke — the element's primary action. |
| **1** | **Shift** | **Left Click** | Real left mouse click at the element's center. |
| **2** | _(unassigned)_ | **Invoke** | Falls back to Invoke by default; assign your own modifier+action. |

**Available actions**: Invoke (UIA), Left Click, Right Click, Move Mouse Only (no click), Hover (persist cursor — triggers CSS `:hover`).

**Supported modifiers**: `Shift`, `Ctrl`, `Alt`, `Win`, and two-key combos like `Ctrl+Shift`.

Why the click modes? Some apps (notably Electron / web-based apps like Microsoft Teams) expose hints through UI Automation but don't implement the `InvokePattern`. A synthesized mouse click goes through the normal OS input path and works on those controls. Move Mouse and Hover modes are useful for revealing hidden UI (tooltips, hover cards, drop-down menus) before re-activating hints.

### Find & navigate mode

> **⚠ Best effort — your mileage may vary.**  
> Find mode depends on the target application exposing text through the Windows
> UI Automation accessibility layer.
>
> **What works best:** Classic Win32 text controls, the Windows console host
> (**cmd**, **PowerShell** in `conhost`), File Explorer file/folder names, and
> system dialogs. These are fast, reliable, and where the feature shines.
>
> **For browsers (Chrome, Firefox, Edge):** Modern web pages — especially
> massive ones like Wikipedia articles — may exceed the 3-second search timeout
> because UIA must marshal the DOM tree across processes. The overlay will show
> partial results (or "0 matches") plus a tip: *"Search timed out. Try the
> app's built-in Ctrl+F for better results."*  Press `Ctrl+F` in the browser
> instead — it searches the DOM directly and is always faster and more accurate.
>
> **What doesn't work:** GPU-canvas editors (VS Code Monaco editor, Windows
> Terminal, GPU-accelerated terminals) that render text to a raw surface with no
> accessibility layer. Use the editor's own `Ctrl+F`.

Press **`Ctrl + .`** (configurable) to open a Chrome `Ctrl+F`–style find bar over the
active window. Unlike element mode, this searches the window's **visible text** rather than
interactive controls.

| Action | How |
| --- | --- |
| **Search** | Start typing. The search fires once the query reaches **5 characters** (debounced 150 ms) so large pages aren't re-scanned on every keystroke. |
| **Highlights** | Every visible occurrence is boxed in **yellow**; the active match is **orange**. |
| **Cycle matches** | `Tab` → next match, `Shift+Tab` → previous (wraps around). The match count ("2 of 5") updates live. |
| **Navigate** | `Enter` scrolls the active match into view and focuses/selects it, then closes the overlay. No clipboard copy. |
| **Edit query** | `Backspace` to correct; symbols and `Shift`-ed characters (`@`, `!`, `:` …) are supported. |
| **Dismiss** | `Escape`, or switch away from the window (`Alt+Tab`) — the overlay auto-closes. |

Find mode is independent of element mode (`Ctrl+;`); only one overlay is active at a time.
The activation hotkey can be changed in **Options → Keyboard**.

#### How it finds text (and where it can't)

Vimium reads text through Windows **UI Automation** (the same accessibility layer screen
readers use). It searches, in order:

1. **TextPattern** — the rich, range-based text interface (browsers, most document/editor
   controls). Gives per-match rectangles and precise scroll-into-view.
2. **ValuePattern** — whole-text edit controls that don't expose TextPattern (e.g. the
   Windows 11 **Notepad** editor). Matches share the control's bounding box.
3. **Element names** — as a last resort, the accessible names of controls (file names in
   Explorer, link labels, etc.).

Search is scoped to the **visible viewport**, capped at 200 matches, and bounded by a
3-second timeout.

**Known limitations** — some surfaces render text to a raw canvas and expose **no** UI
Automation text, so find mode can't see them:

- **VS Code** — the Monaco editor pane and the integrated terminal (both canvas-rendered).
  VS Code's menus, sidebar, and tabs still work.
- Other Electron/GPU-canvas editors and terminals with no accessibility text layer.

The classic Windows console host (**cmd** and **PowerShell** in `conhost`) *does* expose
text and works normally.

### Command-line

Vimium can be launched directly with `/hint` or `/tray` for headless (no tray icon) use:

```
Vimium.exe /hint
Vimium.exe /tray
```

Useful with AutoHotKey or custom key bindings.

### Install (auto-start + Start menu shortcut)

1. Copy the published files to a permanent location:
   ```powershell
   mkdir $env:LOCALAPPDATA\Programs\Vimium -Force
   Copy-Item publish\win-x64\* $env:LOCALAPPDATA\Programs\Vimium\ -Recurse
   ```
2. Run `register-startup-task.ps1` in an **elevated** PowerShell. It does two things:
   - Registers a scheduled task so Vimium auto-starts (elevated, no UAC prompt) at logon.
   - Creates a Start menu shortcut (`Vimium.lnk`) so you can pin it to Start.

   ```powershell
   .\register-startup-task.ps1
   ```
3. To pin to Start: press **Win**, type `Vimium`, right-click → **Pin to Start**.

## Supported controls

Vimium shows hints for UI Automation elements that support any of these patterns:

- **Invoke** — buttons, links, menu items
- **Toggle** — checkboxes, radio buttons
- **Focus** — text fields, input areas
- **Expand / Collapse** — tree items, expandable panels
- **Selection** — list items, tabs

## How to configure

Right-click the Vimium tray icon, select **Options** to open the settings window:

- **General** — font size (with live preview), theme (Light / Dark / Skadi)
- **Overlay** — hint background colors (hex input + preset swatches), animation toggle
- **Actions** — configurable modifier→action slots with key-capture controls; assign any modifier combo to Invoke, Left Click, Right Click, Move Mouse, or Hover
- **Keyboard** — customizable overlay/taskbar/find activation shortcuts

All settings auto-save. Press `↑`/`↓` to navigate sidebar, `Alt+C` to close.

## Screenshots

![ScreenShot](screenshots/explorer.png)
![ScreenShot](screenshots/visual-studio.png)

## Building

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Windows.

```bash
dotnet build src\Vimium.sln
```

## License & Credits

This is a fork of [zsims/hunt-and-peck](https://github.com/zsims/hunt-and-peck), significantly reworked.
Original copyright © Zachary Sims. Licensed under the original project's license.
