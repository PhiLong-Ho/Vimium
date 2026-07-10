# Implementation Plan: Mouse Control Mode

**Branch**: `003-mouse-control-mode` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-mouse-control-mode/spec.md`

## Summary

Add **Mouse control mode** — a fully isolated "last resort" keyboard-driven mouse replacement activated via `Ctrl+/`, with WASD cursor movement, IJKL scrolling, Shift-based click-and-drag, `Space` speed cycling, dual visual indicators (cursor-attached + bottom-screen banner), and 30-second inactivity auto-exit. All keys are user-configurable via the settings window.

Technical approach: Extend VimiumConfig with a new `MouseControlConfiguration` section; add `IMouseControlService` that consumes all keyboard input via the existing low-level keyboard hook; use existing Win32 `SetCursorPos` / `SendInput` (clicks) / `SendMessage` (scroll) APIs from `NativeMethods`; create a transparent WPF indicator overlay (cursor-attached + bottom-screen banner).

## Technical Context

**Language/Version**: C# 13 / .NET 10 (Windows only)

**Primary Dependencies**: WPF (built-in, no third-party UI libraries), `System.Windows.Automation` (UIA managed namespace), Win32 interop via `NativeMethods/` project (`user32.dll`, `kernel32.dll`)

**Storage**: `%APPDATA%\Vimium\config.json` via `System.Text.Json` (existing `VimiumConfig` model + `ConfigService` singleton)

**Testing**: xUnit (`Vimium.Tests.csproj`), AAA pattern with mock implementations of service interfaces. Manual test scenarios for visual indicators (cursor overlay, bottom banner). Coverage target ≥80% on non-interop code.

**Target Platform**: Windows 10+ (10.0.19041.0), Windows 11. WPF desktop application.

**Project Type**: Desktop application — system tray resident with overlay windows

**Performance Goals**:
- Mouse cursor movement: <50ms from key press to cursor reposition (SC-002)
- Scroll response: <100ms from key press to scroll action (SC-004)
- Mode activation: <1 second from `Ctrl+/` to visual indicators visible (SC-001)
- Overlay: <100ms to appear (per constitution Principle V)

**Constraints**:
- UI thread must never block (constitution Principle V)
- Steady-state memory <100MB (constitution Principle V)
- No telemetry, no analytics, no phoning home (constitution Technical Standards)
- All Win32 interop isolated in `NativeMethods/` project
- Config auto-saves on every change (no explicit Save button)

**Scale/Scope**: Single-user desktop application. 3 interaction modes (element, text selection, mouse control). ~10 new config keys. 2 new ViewModels. 1 new service interface + implementation. 1 new WPF overlay view.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. MVVM Separation & Code Quality

✅ **PASS** — All new UI logic will live in ViewModels (`MouseControlViewModel`, extensions to `KeyboardSettingsViewModel`, `GeneralSettingsViewModel`). View code-behind limited to window lifecycle. No business logic in `.xaml.cs`. `DelegateCommand` for all user actions. `NotifyPropertyChanged` base class for bindings.

### II. Interface-Driven Services

✅ **PASS** — New `IMouseControlService` interface in `Services/Interfaces/`. ViewModels depend on the interface, not the concrete implementation. Win32 interop (cursor movement, click simulation, scroll) isolated behind the interface for testability with mocks.

### III. Testing Standards

✅ **PASS** — Unit tests for `MouseControlService` (mockable interface), `MouseControlViewModel`, `KeyboardSettingsViewModel` extensions, `GeneralSettingsViewModel` extensions, config serialization. Manual test scenarios for visual indicators (cursor overlay, bottom banner). Coverage target ≥80% on non-interop code. Test names follow `MethodName_Scenario_ExpectedBehavior`.

### IV. User Experience Consistency

✅ **PASS** — Mouse control mode is keyboard-first by definition. Visual indicators derive colors from active theme's `ResourceDictionary`. Mode isolation follows constitution: only one interaction mode active at a time. `Escape` dismisses. Feedback SLA met (<100ms overlay, immediate config apply). Activation hotkey is user-configurable (`Ctrl+/` default). Copy feedback not applicable (mouse mode doesn't copy text).

### V. Performance & Non-Blocking UI

✅ **PASS** — Mouse movement and click simulation run synchronously on the keyboard hook thread (sub-millisecond Win32 calls — no async needed). Keyboard hook already exists (`KeyboardHookService` with `WH_KEYBOARD_LL`). Visual indicator overlay created on UI thread via `Dispatcher.Invoke`. No cross-process COM calls on UI thread for mouse operations. Memory: overlay window is lightweight (no data enumeration).

### Gate Result: ✅ ALL GATES PASS — No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/003-mouse-control-mode/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0: technical research & decisions
├── data-model.md        # Phase 1: entity definitions & state transitions
├── quickstart.md        # Phase 1: validation & test scenarios
├── contracts/           # Phase 1: interface contracts
│   └── IMouseControlService.md
└── tasks.md             # Phase 2: /speckit-tasks output (NOT created here)
```

### Source Code (repository root)

```text
src/
├── NativeMethods/
│   └── User32.cs                    # ADD: GetCursorPos, WM_MOUSEWHEEL constants
├── Vimium/
│   ├── Models/
│   │   └── VimiumConfig.cs          # MODIFY: add MouseControlConfig
│   ├── Services/
│   │   ├── ConfigService.cs         # MODIFY: add convenience properties
│   │   ├── KeyboardHookService.cs   # REUSE: existing low-level hook
│   │   ├── KeyListenerService.cs    # MODIFY: add MouseControlHotKey registration
│   │   ├── Interfaces/
│   │   │   └── IMouseControlService.cs  # NEW
│   │   └── MouseControlService.cs       # NEW: concrete implementation
│   ├── ViewModels/
│   │   ├── ShellViewModel.cs        # MODIFY: add mouse control mode activation
│   │   ├── KeyboardSettingsViewModel.cs # MODIFY: add mouse control key bindings
│   │   ├── GeneralSettingsViewModel.cs  # (unchanged for this feature)
│   │   ├── MouseControlViewModel.cs     # NEW: mouse mode state & logic
│   │   └── MouseIndicatorViewModel.cs   # NEW: visual indicator state
│   ├── Views/
│   │   ├── OptionsView.xaml         # MODIFY: add mouse control key bindings section
│   │   ├── MouseIndicatorView.xaml  # NEW: cursor indicator + bottom banner overlay
│   │   └── Styles.xaml              # MODIFY: add mouse indicator theme resources (if needed)
│   └── Properties/
│       └── (no changes)
└── Vimium.Tests/
    ├── Services/
    │   └── MouseControlServiceTests.cs  # NEW
    ├── ViewModels/
    │   ├── MouseControlViewModelTests.cs # NEW
    │   └── KeyboardSettingsViewModelTests.cs # MODIFY
    └── Models/
        └── VimiumConfigTests.cs      # MODIFY: test new config serialization
```

**Structure Decision**: Single project (Option 1). Vimium is a monolithic WPF desktop application. The `NativeMethods/` project already isolates Win32 interop. New services, ViewModels, and views follow the existing folder convention. No new projects are needed.

## Complexity Tracking

> No constitution violations — this section intentionally left empty.
