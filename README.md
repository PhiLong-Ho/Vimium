# Vimium

> Vimium/vimperator style keyboard navigation for Windows — now on .NET 10.

A lightweight keyboard-driven UI overlay for Windows. Press a hotkey, type a hint, and interact with any control — buttons, links, menus, tabs — without touching the mouse. Built on the Windows UI Automation framework (like screen readers), so it works with almost any Windows desktop application.

## Features

| Feature | Description |
| --- | --- |
| **Instant overlay** | Overlay appears immediately; hints populate asynchronously in the background. |
| **Multiple interaction modes** | Invoke (default), Left Click, Right Click — hold the modifier while typing. |
| **Elevated by default** | Runs as administrator so it can interact with elevated apps. |
| **Popup-friendly** | Overlay never steals focus — menus, dropdowns, and popups stay open. |
| **Auto-start** | Optional scheduled-task script for login launch without a UAC prompt. |
| **Configurable font size** | Tray icon → Options → FontSize. |
| **Taskbar mode** | `Ctrl + '` to highlight the Windows taskbar. |

## Download

Releases are published at: <https://github.com/PhiLong-Ho/Vim_with_mouse/releases>

## How to use

1. Launch **Vimium.exe** (runs in the system tray).
2. With any window focused, press **`Ctrl + ;`** to show hints for the active window.
   - Press **`Ctrl + '`** to show hints for the taskbar.
3. Type the hint letters shown on the control you want, then release the modifier key.

### Interaction modes

Hold one of these modifiers while typing the hint:

| Modifier | Action |
| --- | --- |
| _(none)_ | **UI Automation invoke** — the element's primary action (press a button, follow a link). |
| **Left Shift** | Move the mouse and perform a real **left click**. |
| **Right Shift** | Move the mouse and perform a real **right click** (e.g. open a context menu). |

Why the click modes? Some apps (notably Electron / web-based apps like Microsoft Teams) expose hints through UI Automation but don't implement the `InvokePattern`. A synthesized mouse click goes through the normal OS input path and works on those controls.

### Command-line

Vimium can be launched directly with `/hint` or `/tray` for headless (no tray icon) use:

```
Vimium.exe /hint
Vimium.exe /tray
```

Useful with AutoHotKey or custom key bindings.

### Auto-start with elevated privileges

Run `src/register-startup-task.ps1` in an elevated PowerShell to register a scheduled task that starts Vimium at logon without a UAC prompt:

```powershell
.\register-startup-task.ps1
```

## Supported controls

Vimium shows hints for UI Automation elements that support any of these patterns:

- **Invoke** — buttons, links, menu items
- **Toggle** — checkboxes, radio buttons
- **Focus** — text fields, input areas
- **Expand / Collapse** — tree items, expandable panels
- **Selection** — list items, tabs

## How to change font size

Right-click the Vimium tray icon, select **Options**, then use the **FontSize** menu.

## Screenshots

![ScreenShot](screenshots/explorer.png)
![ScreenShot](screenshots/visual-studio.png)

## Building

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Windows.

```bash
dotnet build src\HuntAndPeck.sln
```

## License & Credits

This is a fork of [zsims/hunt-and-peck](https://github.com/zsims/hunt-and-peck), significantly reworked.
Original copyright © Zachary Sims. Licensed under the original project's license.
