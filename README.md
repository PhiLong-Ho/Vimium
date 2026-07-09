# Vimium

> Vimium/vimperator style keyboard navigation for Windows — now on .NET 10.

A lightweight keyboard-driven UI overlay for Windows. Press a hotkey, type a hint, and interact with any control — buttons, links, menus, tabs — without touching the mouse. Built on the Windows UI Automation framework (like screen readers), so it works with almost any Windows desktop application.

## Features

| Feature | Description |
| --- | --- |
| **Instant overlay** | Overlay appears immediately; hints populate asynchronously in the background. |
| **Multiple interaction modes** | Invoke (default), Left Click (Shift), Right Click (Shift) — hold modifier while typing. |
| **Line navigation mode** | `Ctrl+.` to label every visible text line in the active window. Jump, search, select, and copy text with keyboard alone. |
| **Sub-line selection & copy** | Hold `Ctrl` + hint label → search → Tab/arrows → Enter to copy exact text portions to clipboard. |
| **Themes** | Light, Dark, and Skadi themes with runtime switching via the options window. |
| **Modern options window** | Sidebar-navigated settings: font size, hint colors, shortcuts — all auto-save. |
| **Configurable hotkeys** | Change overlay/taskbar/line-nav activation shortcuts in Options → Keyboard. |
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

Hold one of these modifiers while typing the hint:

| Modifier | Action |
| --- | --- |
| _(none)_ | **UI Automation invoke** — the element's primary action (press a button, follow a link). |
| **Left Shift** | Move the mouse and perform a real **left click**. |
| **Right Shift** | Move the mouse and perform a real **right click** (e.g. open a context menu). |

Why the click modes? Some apps (notably Electron / web-based apps like Microsoft Teams) expose hints through UI Automation but don't implement the `InvokePattern`. A synthesized mouse click goes through the normal OS input path and works on those controls.

### Line navigation mode

Press **`Ctrl + .`** (configurable) to enter line navigation mode. The overlay shows hint labels on every visible text line in the active window.

| Action | How |
| --- | --- |
| **Jump to line** | Type the hint label (without modifier) — cursor moves to the line center. |
| **Copy whole line** | Hold `Ctrl` (or configured copy modifier) + type hint label, then press `Enter`. |
| **Copy text portion** | Hold copy modifier + hint label → type search text → `Tab`/`Shift+Tab` to cycle matches → arrow keys to refine → `Shift+Arrow` to select → `Enter` to copy. |
| **Dismiss** | Press `Escape` at any time. |

Line mode is independent of element mode (`Ctrl+;`). Both modes have hotkeys and operate on the active foreground window. Hotkeys and the copy modifier can be changed in **Options → Keyboard**.

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
- **Keyboard** — customizable overlay/taskbar activation shortcuts

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
