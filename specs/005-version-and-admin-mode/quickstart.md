# Quickstart: Version Display & Administrator Mode Toggle

**Feature**: 005-version-and-admin-mode
**Date**: 2026-07-10

## Prerequisites

- Windows 10 or Windows 11
- .NET 10 SDK installed
- Repository cloned and on branch `005-version-and-admin-mode`
- Built the solution: `dotnet build src\Vimium.sln`

## Validation Scenarios

### Scenario 1: Version Display

**Objective**: Verify the application version is visible in the settings window.

**Steps**:

1. Build and run the application:
   ```powershell
   dotnet run --project src\Vimium
   ```

2. Open the settings window:
   - Right-click the Vimium tray icon → "Settings"
   - Or use the hotkey (if configured)

3. **Verify**: The version number (e.g., `v1.4.0`) is visible in the settings window without any additional clicks.

4. **Verify**: The version matches the value in `src/SolutionInfo.cs` (`AssemblyVersionInformation.Version`).

**Expected Outcome**: Version is displayed in the settings window, matches the assembly metadata.

---

### Scenario 2: Default Administrator Mode (Non-Elevated)

**Objective**: Verify admin mode is DISABLED by default for new/fresh installs — no UAC prompt.

**Steps**:

1. Delete or rename the existing config file:
   ```powershell
   Remove-Item "$env:APPDATA\Vimium\config.json" -ErrorAction SilentlyContinue
   ```

2. Launch Vimium:
   ```powershell
   dotnet run --project src\Vimium
   ```

3. **Verify**: NO UAC prompt appears (admin mode is disabled by default).

4. Open settings → General tab.

5. **Verify**: "Run as Administrator" checkbox is unchecked.

6. **Verify**: No restart message is shown (default state, no change made).

7. **Verify**: Task Manager → Details → "Elevated" column shows `Vimium.exe` = "No".

**Expected Outcome**: Fresh install defaults to non-elevated mode, no UAC prompt on launch.

---

### Scenario 3: Disable Administrator Mode

**Objective**: Verify the user can disable admin mode and the setting persists.

**Steps**:

1. Open settings → General tab.

2. Uncheck "Run as Administrator".

3. **Verify**: A restart message appears: "A restart is required for this change to take effect."

4. Close settings.

5. Reopen settings.

6. **Verify**: "Run as Administrator" is still unchecked (setting persisted).

7. Close Vimium completely (right-click tray icon → Exit).

8. Relaunch Vimium.

9. **Verify**: No UAC prompt appears. The application launches with normal user privileges.

10. Open Task Manager → Details tab → add "Elevated" column → **Verify**: `Vimium.exe` shows "No" in the Elevated column.

**Expected Outcome**: Admin mode disabled, no UAC on relaunch, setting persists across restarts.

---

### Scenario 4: Re-enable Administrator Mode

**Objective**: Verify the user can re-enable elevated mode after disabling it.

**Steps**:

1. With Vimium running in non-elevated mode (from Scenario 3), open settings → General tab.

2. Check "Run as Administrator".

3. **Verify**: Restart message appears.

4. Close Vimium completely.

5. Relaunch Vimium.

6. **Verify**: UAC prompt appears.

7. Approve the UAC prompt.

8. **Verify**: Vimium launches and shows "Yes" in Task Manager's Elevated column.

**Expected Outcome**: Admin mode re-enabled, UAC prompt returns, app runs elevated.

---

### Scenario 5: Config Migration (Upgrade from Previous Version)

**Objective**: Verify existing users upgrading from a version without the `runAsAdministrator` key adopt the non-elevated default.

**Steps**:

1. Create a config file WITHOUT the `runAsAdministrator` key:
   ```powershell
   $configDir = "$env:APPDATA\Vimium"
   New-Item -ItemType Directory -Path $configDir -Force | Out-Null
   @"
   {
     "fontSize": "16",
     "theme": "Dark"
   }
   "@ | Set-Content "$configDir\config.json"
   ```

2. Launch Vimium.

3. **Verify**: NO UAC prompt appears (missing key resolves to the default `false`).

4. Open settings → General tab.

5. **Verify**: "Run as Administrator" is unchecked.

6. Change any setting (e.g. font size) so the config auto-saves, then close Vimium.

7. Check the config file:
   ```powershell
   Get-Content "$env:APPDATA\Vimium\config.json"
   ```

8. **Verify**: The file retains the original keys (`fontSize`, `theme`) and now also contains `"runAsAdministrator": false` — the key is always written on save (`JsonIgnoreCondition.Never`). (If no setting is changed, the file is not rewritten and the key is simply added on the next save.)

**Expected Outcome**: Upgrading users keep existing settings; admin mode defaults to disabled (non-elevated).

---

### Scenario 6: Non-Admin User Handling

**Objective**: Verify graceful handling when a non-admin user enables admin mode but cannot provide admin credentials.

**Steps**:

1. Ensure Vimium is running in non-elevated mode (admin mode disabled).

2. Enable admin mode in settings, then restart.

3. At the UAC prompt, click "No" (or provide incorrect credentials on a limited account).

4. **Verify**: Vimium does NOT crash. The elevated process fails to start (UAC denied), and the original process has already exited via `Current.Shutdown()`. The user can relaunch manually.

> **Note**: This is the expected behavior with the self-elevation pattern. The original process exits after requesting elevation, so there's nothing to crash. The user relaunches the app manually; because the config still has `runAsAdministrator: true` (they opted in), it will request elevation again — the user should disable admin mode via the settings toggle or by editing config if they cannot elevate.

---

### Scenario 7: Unit Tests

**Objective**: Verify all automated tests pass.

**Steps**:

```powershell
dotnet test src\Vimium.sln
```

**Expected Outcome**: All tests pass with zero failures. New tests cover:
- `VimiumConfig` serialization round-trip with `RunAsAdministrator`
- `VimiumConfig` deserialization from JSON missing the key → defaults to `false`
- `ConfigService.RunAsAdministrator` get/set/property-changed
- `GeneralSettingsViewModel` version display and admin toggle bindings

---

## Manual Test Checklist

| # | Scenario | Pass/Fail |
|---|----------|-----------|
| 1 | Version visible in settings on open | ☐ |
| 2 | Version matches `SolutionInfo.cs` const | ☐ |
| 3 | Fresh install defaults to admin mode OFF (non-elevated) | ☐ |
| 4 | No UAC prompt on fresh launch; UAC appears only after enabling admin mode | ☐ |
| 5 | Admin toggle checked → restart message shown | ☐ |
| 6 | Admin toggle persists after settings close/reopen | ☐ |
| 7 | No UAC when admin mode OFF | ☐ |
| 8 | Task Manager shows "Not elevated" when OFF | ☐ |
| 9 | Re-enable → UAC returns | ☐ |
| 10 | Config migration (missing key → defaults false / non-elevated) | ☐ |
| 11 | Unit tests pass (`dotnet test`) | ☐ |
