# Implementation Plan: Version Display & Administrator Mode Toggle

**Branch**: `005-version-and-admin-mode` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-version-and-admin-mode/spec.md`

## Summary

Add version display to the settings window (read from assembly metadata at runtime) and an administrator mode toggle to `GeneralSettings` that allows users to disable elevation. When admin mode is disabled, the application launches as `asInvoker` without a UAC prompt. When enabled (default), the app re-launches itself with elevation via `Process.Start` with `runas` verb. The preference is persisted in the existing `config.json` alongside all other settings.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (net10.0-windows10.0.19041.0), WPF for UI

**Primary Dependencies**:
- WPF (built-in): UI framework for Options window and overlays
- `Hardcodet.NotifyIcon.Wpf` v1.1.0: System tray icon
- `System.Windows.Automation`: UI Automation (unaffected by this feature)
- `System.Text.Json`: Configuration serialization/deserialization
- `System.Reflection`: Reading assembly version metadata at runtime
- `System.Diagnostics.Process`: Runtime elevation relaunch

**Storage**: JSON file at `%APPDATA%\Vimium\config.json`. Uses existing `VimiumConfig` model class + `ConfigService` singleton pattern. One new boolean field (`RunAsAdministrator`) added to `VimiumConfig`; one new read-only string field (`AppVersion`) exposed on a ViewModel.

**Testing**: xUnit via `Vimium.Tests.csproj`, NSubstitute for mocking. Unit tests for `VimiumConfig` serialization/deserialization (including missing-key migration), `ConfigService.RunAsAdministrator` property, ViewModel version display binding.

**Target Platform**: Windows 10+ / Windows 11, .NET 10 WPF desktop application

**Project Type**: Desktop application (WPF) — system tray resident with overlay windows

**Performance Goals**:
- Version display: zero runtime overhead (reads static const at bind time)
- Admin mode check: <1ms boolean read from config on startup
- Non-elevated launch path: <2s cold start (faster than elevated due to no UAC)
- Elevated launch path: unchanged from current behavior (~2s + UAC prompt)

**Constraints**:
- <100MB steady-state memory (no change — no new retained objects)
- Elevated-process security implications (UIPI, cross-privilege COM) — read-only version display has no elevation impact; admin toggle follows established Windows patterns
- Config migration: missing `RunAsAdministrator` key in existing configs must default to `false` (non-elevated) silently

**Scale/Scope**: Single-user desktop app. 3 existing settings pages. Feature adds one checkbox to General page and one version label to the sidebar/footer. Minimal surface area.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. MVVM Separation & Code Quality

**Status**: ✅ PASS

- Version display: Version string is exposed via a ViewModel property (on `GeneralSettingsViewModel` or `OptionsViewModel`), bound in XAML. No code-behind logic.
- Admin toggle: `RunAsAdministrator` property on `GeneralSettingsViewModel` delegates to `ConfigService.RunAsAdministrator`. The checkbox is bound via `{Binding}`. The "restart required" message is a ViewModel-driven visibility binding.
- No business logic in `.xaml.cs` files. `App.xaml.cs` handles the startup-time elevation check (window lifecycle hook — allowed per constitution).
- All bound properties raise `PropertyChanged` via `NotifyPropertyChanged` base or `ConfigService.INotifyPropertyChanged`.

### II. Interface-Driven Services

**Status**: ✅ PASS

- `ConfigService` already exists and follows the project pattern (singleton service, `INotifyPropertyChanged`). No new service class required — this feature extends the existing config surface.
- If a separate `IElevationService` is needed for the relaunch logic (to keep `App.xaml.cs` testable), it will follow the `I{Feature}Service` naming convention with an interface in `Services/Interfaces/`.
- No violation: existing interfaces cover all new behavior.

### III. Testing Standards

**Status**: ✅ PASS

- **Unit tests planned for**:
  - `VimiumConfig`: serialization round-trip with `RunAsAdministrator`; deserialization of config missing the key (defaults to `false`)
  - `ConfigService.RunAsAdministrator`: get/set/property-changed notification
  - `GeneralSettingsViewModel`: version display binding, admin toggle binding, restart-message visibility
  - `App.xaml.cs` elevation logic (if extracted into `IElevationService`)
- **Coverage target**: ≥80% on new code (all non-view, non-interop)
- **Test framework**: xUnit, AAA pattern, scenario-named tests
- View/XAML changes covered by manual test scenarios in `quickstart.md`

### IV. User Experience Consistency

**Status**: ✅ PASS

- **Keyboard-first**: Admin toggle is a standard `CheckBox` — fully keyboard accessible (Tab, Space). Version label is read-only text. No new keyboard interactions needed.
- **Theme consistency**: Version label and admin toggle text use `{DynamicResource}` brushes from current theme. No hardcoded colors.
- **Interaction mode isolation**: No impact on element mode (`Ctrl+;`) or text selection mode (`Ctrl+.`). Settings window remains modal dialog.
- **Feedback SLA**: Admin toggle change shows immediate "restart required" message via bound visibility. Version displayed on settings open with no delay.

### V. Performance & Non-Blocking UI

**Status**: ✅ PASS

- Version display: static string read, zero overhead. No cross-process calls, no background thread needed.
- Admin toggle: boolean read from in-memory config object. No I/O on the UI thread.
- Startup elevation check: single `Process.Start` with `runas` verb + `Current.Shutdown()` — no polling, no blocking.
- Overlay latency: no impact (feature only touches settings window).

### Constitution Check Summary

| Principle | Status | Notes |
|-----------|--------|-------|
| I. MVVM Separation | ✅ PASS | All logic in ViewModels, XAML bindings only |
| II. Interface-Driven Services | ✅ PASS | Extends existing ConfigService; new service gets interface if needed |
| III. Testing Standards | ✅ PASS | Unit tests for config, ViewModel, elevation logic |
| IV. UX Consistency | ✅ PASS | Keyboard-accessible, theme-consistent, no impact on interaction modes |
| V. Performance | ✅ PASS | No UI-thread blocking, no cross-process calls, no memory impact |

**Gate result**: All principles pass. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/005-version-and-admin-mode/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── config-schema.md
└── tasks.md             # Phase 2 output (/speckit-tasks command)
```

### Source Code (repository root)

```text
src/
├── SolutionInfo.cs                          # UPDATE: bump version to 1.4.0.0
├── Vimium.sln
├── Vimium/
│   ├── app.manifest                         # MODIFY: requireAdministrator → asInvoker
│   ├── App.xaml.cs                          # MODIFY: startup elevation check + relaunch
│   ├── Vimium.csproj                        # UPDATE: bump ApplicationVersion
│   ├── Models/
│   │   └── VimiumConfig.cs                  # MODIFY: add RunAsAdministrator property
│   ├── Services/
│   │   └── ConfigService.cs                 # MODIFY: add RunAsAdministrator convenience property
│   ├── ViewModels/
│   │   ├── GeneralSettingsViewModel.cs      # MODIFY: add RunAsAdministrator + AppVersion
│   │   └── OptionsViewModel.cs             # MODIFY: add AppVersion property
│   └── Views/
│       └── OptionsView.xaml                 # MODIFY: version label + admin toggle section
└── Vimium.Tests/
    ├── Models/
    │   └── VimiumConfigTests.cs             # NEW: config serialization tests
    ├── Services/
    │   └── ConfigServiceTests.cs            # NEW/MODIFY: admin mode tests
    └── ViewModels/
        └── GeneralSettingsViewModelTests.cs # NEW/MODIFY: version + admin binding tests
```

**Structure Decision**: Single desktop application project. This feature extends existing files rather than creating new projects. The only new test files are for the new config property and ViewModel bindings.

## Post-Design Constitution Re-Evaluation

*Re-checked after Phase 1 design (data-model.md, contracts/, quickstart.md).*

### I. MVVM Separation & Code Quality

**Status**: ✅ PASS (re-confirmed)

- Design places version display on `OptionsViewModel`/`GeneralSettingsViewModel` via XAML `{Binding}`.
- Admin toggle is a `CheckBox` bound to `GeneralSettingsViewModel.RunAsAdministrator` → delegates to `ConfigService`.
- Restart message visibility uses `BooleanToVisibilityConverter` binding on `ShowRestartMessage` ViewModel property.
- Elevation logic in `App.xaml.cs` startup (window lifecycle — explicitly permitted per constitution).
- No code-behind logic in `.xaml.cs` files for settings window.

### II. Interface-Driven Services

**Status**: ✅ PASS (re-confirmed)

- No new service required. Configuration is extended via `VimiumConfig` + `ConfigService` convenience property — both already exist.
- If elevation relaunch logic is extracted to a service for testability, the design prescribes `IElevationService` following the `I{Feature}Service` convention.
- Existing `ConfigService` is already a singleton consumed by ViewModels.

### III. Testing Standards

**Status**: ✅ PASS (re-confirmed)

- Data model defines clear test scenarios for config serialization (missing key → default), ViewModel bindings, and elevation guards.
- Quickstart includes manual test checklist covering all acceptance scenarios.
- Model/config tests are unit-testable without UI infrastructure.

### IV. User Experience Consistency

**Status**: ✅ PASS (re-confirmed)

- UI contract specifies keyboard accessibility (`Alt+R` access key, `Tab`/`Space`).
- All colors use `{DynamicResource}` theme brushes — no hardcoded values.
- Version display is non-intrusive (sidebar/footer), always visible regardless of selected settings page.
- Admin toggle provides immediate visual feedback (restart message).

### V. Performance & Non-Blocking UI

**Status**: ✅ PASS (re-confirmed)

- Version is a static const — zero runtime cost.
- Admin toggle is a boolean read from in-memory config.
- Elevation decision on startup is a single `WindowsPrincipal.IsInRole` call + conditional `Process.Start` — no polling, no blocking.
- Overlay performance unaffected (feature only touches settings window).

### Re-Evaluation Summary

All five principles pass with no violations. The design is consistent with the constitution. No Complexity Tracking entries required.

## Complexity Tracking

> No violations to justify — all constitution principles pass (initial + post-design re-evaluation).
