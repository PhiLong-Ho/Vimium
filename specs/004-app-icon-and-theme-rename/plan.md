# Implementation Plan: App Icon Theming & Theme Rename

**Branch**: `004-app-icon-and-theme-rename` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-app-icon-and-theme-rename/spec.md`

## Summary

Add a default keyboard icon for Light/Dark themes, switch to Arknights-themed icons when the Arknights theme is selected, and rename the "Skadi" theme to "Arknights". Legacy or unrecognized theme values in an existing config are **reset to the default (Light)** on load — only the `Theme` field is reset; all other settings are preserved and the old value is **not** migrated to "Arknights" (per 2026-07-10 clarification). The feature extends the existing WPF `ResourceDictionary`-based theme system with dynamic icon switching triggered on theme change.

## Technical Context

**Language/Version**: C# 13 / .NET 10

**Primary Dependencies**: WPF (built-in), Hardcodet.NotifyIcon.Wpf (TaskbarIcon for system tray), `System.Windows.Automation`, `System.Text.Json`

**Storage**: JSON config file at `%APPDATA%\Vimium\config.json` via `ConfigService` singleton

**Testing**: xUnit (`Vimium.Tests.csproj`)

**Target Platform**: Windows 10+, Windows 11 — WPF desktop application running as `requireAdministrator`

**Project Type**: Desktop application (WPF with system tray)

**Performance Goals**: Icon switching within 500ms of theme change (imperceptible to user); cold-start to tray icon <2 seconds (existing target)

**Constraints**: Elevated process (UIPI implications); zero telemetry; no third-party UI libraries; pure WPF theme system via `ResourceDictionary` swapping

**Scale/Scope**: Single desktop app; 3 themes (Light, Dark, Arknights); 2 icon sets (keyboard default, Arknights-themed); ~10 files touched

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. MVVM Separation & Code Quality

✅ **PASS** — Icon switching logic will be implemented in `App.xaml.cs` (application lifecycle — valid code-behind concern per the focus-management exemption) and exposed via a bindable property on `ConfigService`. No business logic in view code-behind files. Theme rename updates ViewModel properties (`GeneralSettingsViewModel.Themes`) which already follow MVVM.

### II. Interface-Driven Services

✅ **PASS** — No new services required. Icon switching is a UI concern handled by the existing `ConfigService` (which already exposes `Theme` via `INotifyPropertyChanged`) and WPF resource management in `App.xaml.cs`. The existing `ConfigService` singleton-pattern is an accepted architectural choice (pre-existing).

### III. Testing Standards

✅ **PASS** — New logic is testable:
- `VimiumConfig.FromJson` theme validation (an unrecognized/legacy value such as "Skadi" is reset to "Light" while every other field is left untouched) is pure data transformation — unit-testable
- `ConfigService` load path preserving all non-theme settings while resetting only the theme — unit-testable with in-memory config
- Icon resource resolution from theme — WPF resource dictionary behavior (manual test)
- XAML bindings for dynamic icon — manual test per spec acceptance scenarios

### IV. User Experience Consistency

✅ **PASS** — All changes are keyboard-first and theme-consistent:
- Theme dropdown in settings already keyboard-navigable — rename is a label change
- Icon switching is automatic, transparent to user interaction flows
- No new UI elements — existing theme selector unchanged except for label text
- Icon renders at all standard Windows sizes (16–256px) ensuring clarity

### V. Performance & Non-Blocking UI

✅ **PASS** — Icon switching is synchronous on UI thread but trivial (swap `BitmapImage` resource reference). No cross-process COM, no UIA walks. The `ApplyTheme` method already runs synchronously on the UI thread with sub-millisecond `ResourceDictionary` operations. Icon resource swap adds microseconds, well within the 500ms spec limit.

### Gate Result: ALL GATES PASS — Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/004-app-icon-and-theme-rename/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit-tasks command)
```

### Source Code (repository root)

```text
src/Vimium/
├── Vimium.csproj             # ApplicationIcon: skadi.ico → keyboard.ico (default exe icon)
├── App.xaml                  # Global icon resource definition (static → dynamic)
├── App.xaml.cs               # ApplyTheme() — "Skadi"→"Arknights" switch case + icon switching
├── Models/
│   └── VimiumConfig.cs       # FromJson: validate Theme — reset unrecognized/legacy value to default (Light), other fields untouched
├── Services/
│   └── ConfigService.cs      # ApplyThemeHintDefaults — rename "Skadi" case to "Arknights" (no alias)
├── ViewModels/
│   └── GeneralSettingsViewModel.cs  # Themes list — "Skadi" → "Arknights"
├── Views/
│   ├── ShellView.xaml        # System tray icon binding (static → dynamic)
│   ├── OptionsView.xaml      # Sidebar icon (static → dynamic)
│   ├── OptionsView.xaml.cs   # (no changes needed)
│   └── OverlayView.xaml.cs   # Loading-icon theme check "Skadi" → "Arknights"
├── Themes/
│   ├── SkadiTheme.xaml       # → Rename to ArknightsTheme.xaml (also "SkadiLoadingIcon" key → "ArknightsLoadingIcon")
│   ├── LightTheme.xaml       # (unchanged)
│   └── DarkTheme.xaml        # (unchanged)
└── Resources/
    ├── keyboard.ico           # NEW: default keyboard icon
    └── skadi.ico              # → Becomes Arknights-themed icon (keep filename for compat, or rename)

tests/Vimium.Tests/
└── (new tests for theme validation / reset-to-default and preservation of other settings)
```

**Structure Decision**: Single WPF project — no new projects needed. Changes are localized to the existing theme system and configuration layer. The existing architecture of `ResourceDictionary` swapping in `App.xaml.cs` is extended to also swap icon resources.

## Complexity Tracking

> No violations to justify — all constitution gates passed.
