# Quickstart: App Icon Theming & Theme Rename

**Feature**: 004-app-icon-and-theme-rename
**Date**: 2026-07-10

## Prerequisites

- .NET 10 SDK installed
- Built the solution at least once: `dotnet build src\Vimium.sln`
- A Windows 10+ machine (system tray icon behavior requires Windows shell)

## Validation Scenarios

These scenarios verify the feature end-to-end. Each maps to one or more acceptance scenarios in the [spec](./spec.md).

---

### Scenario 1: Default Keyboard Icon on Fresh Install

**Maps to**: User Story 1

1. Delete existing config: `Remove-Item "$env:APPDATA\Vimium\config.json" -ErrorAction SilentlyContinue`
2. Build and launch Vimium: `dotnet run --project src\Vimium\Vimium.csproj`
3. **Verify**: System tray icon is a **keyboard** icon (not the Arknights character)
4. Right-click tray icon → Options → verify sidebar header shows **keyboard** icon
5. **Verify**: Theme dropdown shows "Light" selected

---

### Scenario 2: Keyboard Icon Persists on Light ↔ Dark Switch

**Maps to**: User Story 1, Acceptance Scenario 4

1. From default state (Light theme), verify tray icon is keyboard
2. Open Options → General → switch Theme to "Dark"
3. **Verify**: System tray icon remains **keyboard** icon (no change)
4. Switch Theme back to "Light"
5. **Verify**: System tray icon remains **keyboard** icon

---

### Scenario 3: Arknights Theme Switches Icons

**Maps to**: User Story 2

1. From any theme, open Options → General → switch Theme to "Arknights"
2. **Verify**: System tray icon changes to **Arknights-themed** icon (within 500ms)
3. **Verify**: Options window sidebar header icon also changes to Arknights icon
4. Close and reopen Options window (or restart app) → **Verify**: Arknights icons persist

---

### Scenario 4: Arknights → Default Reverts Icons

**Maps to**: User Story 2, Acceptance Scenario 3

1. With Arknights theme active (icons are Arknights-themed)
2. Switch Theme to "Light"
3. **Verify**: System tray icon reverts to **keyboard** icon
4. Switch Theme to "Dark" → icon stays **keyboard**

---

### Scenario 5: Legacy "Skadi" Config Resets Theme to Default (No Migration)

**Maps to**: User Story 3

1. Create a legacy config file with a non-default, non-theme setting to prove preservation:
   ```powershell
   $configDir = "$env:APPDATA\Vimium"
   New-Item -ItemType Directory -Path $configDir -Force | Out-Null
   Set-Content -Path "$configDir\config.json" -Value '{"theme": "Skadi", "fontSize": "18", "language": "en"}'
   ```
2. Launch Vimium
3. **Verify**: App starts with the **default Light** theme active (keyboard icons visible), NOT Arknights
4. Open Options → General → **Verify**: Theme dropdown shows **"Light"** selected (the legacy "Skadi" was reset, not migrated to "Arknights")
5. **Verify**: The dropdown options are ["Light", "Dark", "Arknights"] — no "Skadi" option
6. **Verify preservation**: Font Size still shows **18** (the non-theme setting was NOT reset)
7. Select "Arknights" from the dropdown → **Verify**: Arknights icons appear and config saves automatically
8. Check config file: `Get-Content "$env:APPDATA\Vimium\config.json"`
9. **Verify**: Config contains `"theme": "Arknights"` and still contains `"fontSize": "18"`

---

### Scenario 6: Icon Clarity at All Sizes

**Maps to**: Edge Case: Multiple icon sizes

1. Launch Vimium with default keyboard icon
2. **Verify**: System tray icon is sharp at default size (no visible blur/pixelation)
3. Change Windows display scaling to 125%, 150%, 200% → **Verify**: Icon scales clearly
4. Open Options window → **Verify**: 20×20 sidebar icon is crisp

---

### Scenario 7: Missing Icon Graceful Fallback

**Maps to**: Edge Case: Icon file missing

1. Temporarily rename/delete `src/Vimium/Resources/skadi.ico`
2. Launch Vimium
3. Switch theme to Arknights
4. **Verify**: App does NOT crash — falls back to keyboard icon
5. **Verify**: Log output contains warning about missing icon (check Debug output)
6. Restore the icon file

---

## Running Tests

```powershell
# Run all tests
dotnet test src\Vimium.sln

# Run only config-related tests (if categorized)
dotnet test src\Vimium.sln --filter "Category=Config"
```

Expected test coverage for new logic:
- `VimiumConfig.FromJson` with `"theme": "Skadi"` → `Theme` reset to `"Light"`; all other fields unchanged
- `VimiumConfig.FromJson` with an unknown value (e.g. `"Neon"`) → `Theme` reset to `"Light"`
- `VimiumConfig.FromJson` with `"Arknights"` / `"Dark"` / `"Light"` → `Theme` unchanged
- `VimiumConfig.FromJson` with legacy theme + non-default other fields → only `Theme` changes, other fields preserved
