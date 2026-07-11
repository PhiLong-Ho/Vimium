# Installing Vimium

Vimium is a portable, self-contained single-file app — `Vimium.exe` needs no
installer and no .NET runtime. This guide covers putting it in a permanent
location, auto-starting it at login, and command-line/headless use.

## Get Vimium

- **Microsoft Store (recommended)** — signed, trusted, and auto-updating; no
  SmartScreen warning. <!-- TODO: add Microsoft Store link -->
- **Direct download** — grab `Vimium.exe` from the
  [GitHub Releases](https://github.com/PhiLong-Ho/Vimium/releases) page.

> **SmartScreen on direct downloads:** because the GitHub build isn't signed with
> a paid publisher certificate, Microsoft Defender SmartScreen may show
> *"Windows protected your PC."* Click **More info → Run anyway**. The Microsoft
> Store build is signed by Microsoft and shows no warning — prefer it if you want
> to skip this. Background: [docs/SIGNING.md](SIGNING.md).

## Install to a permanent location

```powershell
mkdir $env:LOCALAPPDATA\Programs\Vimium -Force
Copy-Item .\Vimium.exe $env:LOCALAPPDATA\Programs\Vimium\ -Force
```

Then just run `Vimium.exe` — it lives in the system tray.

## Auto-start at login (+ Start menu shortcut)

Run `register-startup-task.ps1` (in the repo root) from an **elevated**
PowerShell. It:

- Registers a scheduled task so Vimium auto-starts at logon (no UAC prompt), and
- Creates a Start menu shortcut (`Vimium.lnk`) you can pin to Start.

```powershell
.\register-startup-task.ps1
```

To pin to Start: press **Win**, type `Vimium`, right-click → **Pin to Start**.

> The scheduled task points at `%LOCALAPPDATA%\Programs\Vimium\Vimium.exe`, so
> install to that path first (above) or pass `-ExePath` to the script.

## Command-line / headless use

Launch Vimium directly with `/hint` or `/tray` for headless (no tray icon) use —
handy with AutoHotkey or custom key bindings:

```
Vimium.exe /hint    # show hints for the active window
Vimium.exe /tray    # show hints for the Windows taskbar
```

## Administrator mode

Vimium runs **non-elevated by default** (no UAC prompt) — suitable for
managed/enterprise environments. To interact with elevated apps, enable
**Options → General → "Run as Administrator"** (takes effect on next restart).
