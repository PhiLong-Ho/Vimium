# Implementation Plan: Hint Overlay Improvements

**Branch**: `002-hint-overlay-improvements` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-hint-overlay-improvements/spec.md`

## Summary

Improve the Vimium hint overlay across four axes: **performance** (750ms enumeration via provider-side pattern-availability condition filtering + result caching), **customization** (3-slot configurable modifier→action mapping with key-capture UI), **overlap avoidance** (spiral offsetting for hint labels), and **observability** (structured JSON benchmark logging). The primary performance strategy shifted from parallel subtree retrieval (infeasible — UIA COM is STA-threaded) to provider-side filtering at the `FindAllBuildCache` condition level, which is reliable for element-mode patterns (TextPattern's known issues with availability properties do not apply to Invoke, Toggle, SelectionItem, ExpandCollapse, Value, or RangeValue patterns).

## Technical Context

**Language/Version**: C# 13 / .NET 10, WPF for UI

**Primary Dependencies**: Interop.UIAutomationClient (COM), System.Text.Json (config/logging)

**Storage**: JSON config file (`%APPDATA%\Vimium\config.json`) via `ConfigService`; rolling JSON log file (`%APPDATA%\Vimium\logs\benchmark.jsonl`) for benchmark entries

**Testing**: xUnit (`Vimium.Tests`), AAA pattern, ≥80% line coverage on services/models/ViewModels per constitution Principle III. Benchmark validation via manual procedure + PowerShell parse script.

**Target Platform**: Windows 10+ / Windows 11, WPF desktop app, runs elevated (`requireAdministrator`)

**Project Type**: Desktop application (WPF, MVVM, UI Automation interop)

**Performance Goals**: 750ms end-to-end enumeration for 200+ element apps (SC-001), 95% of sessions within 750ms for up to 500 elements (SC-002), 400ms for <50 elements (FR-004), <100ms overlay appearance (constitution Principle V)

**Constraints**: UIA COM STA-threaded (no parallel enumeration), accuracy-first (never drop interactive elements), no telemetry (logs local-only), no third-party UI libraries (pure WPF), maintain <100MB steady-state memory

**Scale/Scope**: Single-feature improvement touching ~8 existing files, ~7 new files. 20 functional requirements, 8 success criteria.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Status |
|-----------|-------------|--------|
| **I. MVVM Separation** | All UI logic in ViewModels, not code-behind. Key-capture control must follow this pattern. | ✅ PASS — key-capture logic in ViewModel, view handles only `KeyDown`/`KeyUp` forwarding |
| **II. Interface-Driven Services** | New services (benchmark logging, overlap resolver) must expose interfaces. | ✅ PASS — `IOverlapResolver`, `IBenchmarkService` interfaces defined |
| **III. Testing Standards** | ≥80% coverage on core logic. FR-019 mandates unit-testable filtering logic. | ✅ PASS — mock UIA elements for overlap-resolver, action-resolution, and benchmark-log tests |
| **IV. UX Consistency** | Element interaction contract unchanged. Overlay appearance <100ms. Settings apply immediately. | ✅ PASS — default behavior (no modifier → Invoke) preserved. Auto-save for action config via ConfigService subscription. |
| **V. Performance & Non-Blocking UI** | Enumeration on background thread. Cached requests. No synchronous cross-process calls on UI thread. | ✅ PASS — enumeration continues on `Task.Run`. CacheRequest preserved. Overlay shows loading indicator immediately. |

**Gate Result**: ALL PASS — no violations requiring complexity tracking.

## Project Structure

### Documentation (this feature)

```text
specs/002-hint-overlay-improvements/
├── plan.md              # This file
├── research.md          # Phase 0 output — feasibility and approach decisions
├── data-model.md        # Phase 1 output — entity definitions and relationships
├── quickstart.md        # Phase 1 output — validation scenarios
├── contracts/           # Phase 1 output — interface contracts
│   └── interface-contracts.md
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
src/Vimium/
├── Models/
│   ├── Hint.cs                          # MODIFY: expose cached properties for fast-path rejection
│   ├── VimiumConfig.cs                 # MODIFY: add ActionSlot[] and benchmark settings
│   ├── HintAction.cs                   # NEW: action type enum + ActionSlot model
│   ├── BenchmarkLogEntry.cs            # NEW: structured log entry model
│   └── HintLabelPosition.cs            # NEW: adjusted label position model
├── Services/
│   ├── Interfaces/
│   │   ├── IHintProviderService.cs     # MODIFY: add InvalidateCache method
│   │   ├── IOverlapResolver.cs         # NEW: spiral-offsetting interface
│   │   └── IBenchmarkService.cs        # NEW: benchmark logging interface
│   ├── UiAutomationHintProviderService.cs # MODIFY: client-side fast-path rejection, result caching
│   ├── HintLabelService.cs             # (unchanged)
│   ├── OverlapResolver.cs              # NEW: spiral-offsetting implementation
│   ├── BenchmarkService.cs             # NEW: JSON rolling-log implementation
│   └── ConfigService.cs               # MODIFY: add action slot defaults, benchmark settings
├── ViewModels/
│   ├── OverlayViewModel.cs             # MODIFY: multi-slot action resolution, position adjustment
│   ├── HintViewModel.cs                # MODIFY: adjusted position bindings
│   ├── ShellViewModel.cs               # MODIFY: pass action config to OverlayViewModel
│   ├── OptionsViewModel.cs             # MODIFY: add action-config sub-VM
│   ├── ActionSettingsViewModel.cs      # NEW: key-capture + action slot configuration
│   └── GeneralSettingsViewModel.cs     # (unchanged)
├── Views/
│   ├── OverlayView.xaml                # MODIFY: label position binding for spiral offsets
│   ├── OptionsView.xaml                # MODIFY: add action-configuration section with key-capture controls
│   └── OptionsView.xaml.cs             # MODIFY: key-capture event forwarding
└── Themes/                             # (unchanged)

src/Vimium.Tests/
├── Services/
│   ├── UiAutomationHintProviderServiceTest.cs  # NEW: client-side rejection logic tests
│   └── OverlapResolverTest.cs                  # NEW: spiral offsetting tests
├── ViewModels/
│   ├── OverlayViewModelTest.cs                 # NEW: multi-slot action resolution tests
│   └── ActionSettingsViewModelTest.cs          # NEW: key-capture VM tests
└── Models/
    ├── HintActionTest.cs                       # NEW: serialization/deserialization
    └── BenchmarkLogEntryTest.cs                # NEW: JSON roundtrip

scripts/
└── parse-benchmark-log.ps1                     # NEW: benchmark log analysis script
```

**Structure Decision**: Single WPF project (`src/Vimium/`) with MVVM structure. Test project at `src/Vimium.Tests/`. All new classes follow existing namespace conventions. No new projects needed — this is a refinement of existing hint overlay infrastructure.

## Complexity Tracking

> No constitution violations. This section intentionally empty.
