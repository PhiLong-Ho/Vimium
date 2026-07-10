# Research: Version Display & Administrator Mode Toggle

**Feature**: 005-version-and-admin-mode
**Date**: 2026-07-10

## 1. Version Reading Mechanism

### Decision
Read the version from `SolutionInfo.cs`'s `AssemblyVersionInformation.Version` const string at compile time. Expose via a ViewModel property bound to a `TextBlock` in the settings window.

### Rationale
- The project already has `SolutionInfo.cs` with `AssemblyVersionInformation.Version` (currently `"1.3.0.0"`).
- `GenerateAssemblyInfo=false` in the `.csproj` means standard `Assembly.GetEntryAssembly().GetName().Version` would NOT reflect the real version — it would return the fallback `1.0.0.0` from the project file.
- Using the existing `Version` const avoids adding new build infrastructure (no MSBuild targets, no GitVersion, no source generators).
- The version update process remains: bump `SolutionInfo.cs` and `Vimium.csproj`/`ApplicationVersion` on each release.

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| `Assembly.GetEntryAssembly().GetName().Version` | Returns `1.0.0.0` due to `GenerateAssemblyInfo=false` |
| `FileVersionInfo.GetVersionInfo()` | Reads from file system, requires path resolution, overkill for a const |
| MSBuild property `$(Version)` in `.csproj` | Would require build-time source generation or assembly-level attribute; adds complexity vs. updating one const |
| Git tag/commit hash | Adds build dependency on git; doesn't match user-facing version scheme |

### Implementation
```csharp
// In GeneralSettingsViewModel or OptionsViewModel:
public string AppVersion => AssemblyVersionInformation.Version;
```

## 2. Administrator Mode Elevation Strategy

### Decision
Change `app.manifest` from `requireAdministrator` to `asInvoker`. On startup, check `ConfigService.RunAsAdministrator`: if `true` AND the current process is NOT elevated, relaunch via `Process.Start` with `Verb = "runas"` and exit. If `false` OR already elevated, proceed normally.

### Rationale
- Single executable — no separate launcher/shim process needed.
- `asInvoker` in the manifest means Windows does NOT force elevation. The app starts non-elevated by default, then self-elevates conditionally.
- `Process.Start` with `useShellExecute = true` and `Verb = "runas"` triggers the standard Windows UAC consent prompt. This is the documented, supported way to request elevation at runtime.
- The existing `SingleLaunchMutex` check runs BEFORE the elevation check, so we won't spawn duplicate elevated processes when a non-elevated instance is already running.
- This pattern is used by many established Windows tools (Task Manager, Process Explorer, various installers).

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| Two separate executables (one elevated, one not) | Doubles deployment complexity; confuses users |
| Keep `requireAdministrator` manifest, use Task Scheduler for non-elevated | Task Scheduler is too complex for a simple toggle; fragile |
| COM elevation moniker | Over-engineered for a simple process relaunch; adds COM registration complexity |
| Windows service for elevated operations | Massive overkill for a user-facing overlay app |

### Implementation Sketch
```csharp
// In App.OnStartup or ShellViewModel constructor:
if (ConfigService.Instance.RunAsAdministrator && !IsUserAdmin())
{
    var exePath = Environment.ProcessPath;
    var startInfo = new ProcessStartInfo(exePath)
    {
        UseShellExecute = true,
        Verb = "runas"
    };
    Process.Start(startInfo);
    Current.Shutdown();
    return;
}
```

### Elevation Check
```csharp
private static bool IsUserAdmin()
{
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}
```

## 3. Restart Notification Pattern

### Decision
When `RunAsAdministrator` changes in the settings UI, show a `TextBlock` message: "A restart is required for this change to take effect." The message is bound to a `ShowRestartMessage` boolean on the ViewModel, which is set to `true` when the current UI value differs from the saved config value.

### Rationale
- Follows the standard MVVM pattern used throughout the project: ViewModel exposes a boolean, XAML binds `Visibility` via `BooleanToVisibilityConverter`.
- The message appears immediately on toggle (per `Feedback SLA` in constitution), no save button needed (settings auto-save).
- On close and reopen, if config was saved with the new value, the message is absent (the change already took effect on the last restart).

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| `MessageBox` on close | Modal dialog is intrusive; user may want to change other settings first |
| Separate "Apply" button | Violates constitution: "Settings changes MUST apply immediately (auto-save)" |
| Toast notification | Toast may be missed; persistent inline message is more visible |

### Implementation
```xaml
<TextBlock Text="A restart is required for this change to take effect."
           Foreground="{DynamicResource WarningBrush}"
           Visibility="{Binding ShowRestartMessage, Converter={StaticResource BooleanToVisibilityConverter}}"
           FontStyle="Italic" />
```

## 4. Config Migration (Missing Key)

### Decision
Rely on `System.Text.Json` default value handling. `VimiumConfig.RunAsAdministrator` defaults to `true`. When deserializing a config JSON that lacks the `runAsAdministrator` key, the serializer uses the CLR default (`true`). No explicit migration code needed.

### Rationale
- `System.Text.Json` by default populates missing properties with their CLR default values. Since the C# property is `public bool RunAsAdministrator { get; set; } = true;`, the default matches the previous always-elevated behavior.
- The existing `ConfigService.MigrateFromLegacy()` method only handles the old `Settings.settings` → `config.json` migration for `FontSize`. No additional migration needed for this property.
- Test coverage will verify: deserialize `{"fontSize": "14"}` → `RunAsAdministrator` equals `true`.

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| Custom `JsonConverter` to detect missing key | Over-engineered; CLR default achieves the same result |
| Explicit migration code in `ConfigService.Load()` | Redundant when JSON deserialization handles it natively |
| Store as `bool?` and coalesce null → true | Adds null-handling complexity for no benefit |

### Test Verification
```csharp
[Fact]
public void RunAsAdministrator_MissingFromJson_DefaultsToTrue()
{
    var json = "{\"fontSize\": \"14\"}";
    var config = VimiumConfig.FromJson(json);
    Assert.True(config.RunAsAdministrator);
}
```

## 5. Elevation State on UAC-Disabled Systems

### Decision
On systems where UAC is disabled, `Process.Start` with `Verb = "runas"` will still launch the process (it succeeds silently without a consent prompt). The `IsUserAdmin()` check handles the edge case: if UAC is off and the user is admin, the process may already be elevated. The behavior is consistent and requires no special-case code.

### Rationale
- Windows behavior when UAC is disabled: all processes run elevated if the user is an administrator. `IsUserAdmin()` returns `true`, so the relaunch is skipped.
- The admin toggle still persists correctly in config — when re-enabled, on a UAC-enabled system, the consent prompt appears as expected.

## Summary of Decisions

| Topic | Decision | Key Implementation |
|-------|----------|--------------------|
| Version source | `AssemblyVersionInformation.Version` const | Bind `AppVersion` property in XAML |
| Elevation manifest | Change to `asInvoker` | Runtime `runas` verb via `Process.Start` |
| Elevation check | `WindowsPrincipal.IsInRole(Administrator)` | Guard in `App.OnStartup` |
| Restart message | Inline `TextBlock` with visibility binding | MVVM `ShowRestartMessage` property |
| Config migration | CLR default (`true`) via System.Text.Json | No explicit migration needed |
| UAC-disabled systems | No special handling; `IsUserAdmin()` gate | Consistent behavior |
