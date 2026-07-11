# Vimium

> Vimium/vimperator style keyboard navigation for Windows — now on .NET 10.

A lightweight keyboard-driven UI overlay for Windows. Press a hotkey, type a hint, and interact with any control — buttons, links, menus, tabs — without touching the mouse. Built on the Windows UI Automation framework (like screen readers), so it works with almost any Windows desktop application.

## Download & install

- **[Microsoft Store](#)** _(recommended)_ — signed by Microsoft, auto-updating, and no SmartScreen warning. <!-- TODO: add Microsoft Store link -->
- **Direct download** — grab `Vimium.exe` from the [Releases](https://github.com/PhiLong-Ho/Vimium/releases) page. It's a portable single-file app — just run it.

  > **SmartScreen note:** the direct-download build isn't signed with a paid certificate, so Microsoft Defender SmartScreen may warn *"Windows protected your PC."* Click **More info → Run anyway**. The Microsoft Store build is signed by Microsoft and shows no warning.

For a permanent install location, **auto-start at login**, or **command-line / headless** use, see **[docs/INSTALL.md](docs/INSTALL.md)**.

## How to use

1. Launch **Vimium.exe** (runs in the system tray).
2. With any window focused, press **`Ctrl + ;`** to show hints for the active window.
   - Press **`Ctrl + '`** to show hints for the taskbar.
   - Press **`Ctrl + .`** (configurable) to find and select visible text so you can copy or edit it without the mouse (see [Find & select text](#find--select-text) below).
   - Hotkeys are configurable in Options → Keyboard.
3. Type the hint letters shown on the control you want, then release the modifier key.

### Interaction modes

Hold one of these modifiers while typing the hint (defaults shown — all three slots are
configurable in **Options → Actions** with key-capture controls):

| Slot | Modifier (default) | Action | Description |
| --- | --- | --- | --- |
| **0** | _(none)_ | **Invoke** | UI Automation invoke — the element's primary action. |
| **1** | **Shift** | **Left Click** | Real left mouse click at the element's center. |
| **2** | **Ctrl** | **Right Click** | Real right mouse click at the element's center. |
| **3** | **Alt** | **Hover** | Move cursor to element center — triggers CSS `:hover`. |

**Available actions**: Invoke (UIA), Left Click, Right Click, Hover (no click, persist cursor — triggers CSS `:hover`).

**Supported modifiers**: `Shift`, `Ctrl`, `Alt`, `Win`, and two-key combos like `Ctrl+Shift`.

Why the click modes? Some apps (notably Electron / web-based apps like Microsoft Teams) expose hints through UI Automation but don't implement the `InvokePattern`. A synthesized mouse click goes through the normal OS input path and works on those controls. Hover mode is useful for revealing hidden UI (tooltips, hover cards, drop-down menus) before re-activating hints.

### Find & select text

Select on-screen text with the keyboard so you can **copy or edit it without the
mouse** — for example, grabbing output from a terminal. Press **`Ctrl + .`**
(configurable) to open a find bar over the active window; it searches the window's
**visible text** rather than interactive controls.

| Action | How |
| --- | --- |
| **Search** | Type **≥ 5 characters**. Matches highlight live — **yellow** = all, **orange** = active. |
| **Cycle matches** | `Tab` → next, `Shift+Tab` → previous (wraps); the count ("2 of 5") updates live. |
| **Select** | `Enter` scrolls the active match into view and **selects** it in the app, then closes the overlay. Press `Ctrl+C` to copy, or start editing. |
| **Dismiss** | `Escape`, or switch away (`Alt+Tab`). |

> **Works best** with classic Win32 text, the console host (**cmd** / **PowerShell** in
> `conhost`), File Explorer, and system dialogs. Browsers and very large pages may time
> out — use the app's own `Ctrl+F`. GPU-canvas editors (VS Code's Monaco, Windows
> Terminal) expose no accessibility text and aren't supported.

Find mode is independent of element mode (`Ctrl+;`); only one overlay is active at a time.

## Supported controls

Vimium shows hints for UI Automation elements that support any of these patterns:

- **Invoke** — buttons, links, menu items
- **Toggle** — checkboxes, radio buttons
- **Focus** — text fields, input areas
- **Expand / Collapse** — tree items, expandable panels
- **Selection** — list items, tabs

## How to configure

Right-click the Vimium tray icon, select **Options** to open the settings window:

- **General** — font size (with live preview), theme (Light / Dark / Arknights)
- **Overlay** — hint background colors (hex input + preset swatches), animation toggle
- **Actions** — configurable modifier→action slots; assign any modifier combo to Invoke, Left Click, Right Click, or Hover
- **Keyboard** — customizable overlay/taskbar/find activation shortcuts

All settings auto-save. Press `↑`/`↓` to navigate sidebar, `Alt+C` to close.

## Building from source

See **[docs/BUILD.md](docs/BUILD.md)** to build and produce a portable single-file
exe, and **[docs/SIGNING.md](docs/SIGNING.md)** for code signing and publishing.
