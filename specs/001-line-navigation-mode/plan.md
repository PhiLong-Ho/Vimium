# Implementation Plan: Text Selection Mode (Redesigned)

**Branch**: `001-line-navigation-mode` | **Date**: 2026-07-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-line-navigation-mode/spec.md`

## Summary

Replace the per-line hint labeling approach with a **search-first text selection mode** that mimics mouse-driven text selection: activate via `Ctrl+.`, type a visible phrase to find and highlight it, Tab/Shift+Tab to cycle matches, Shift+Arrow to refine the selection, and Enter to copy.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (net10.0-windows)
**Primary Dependencies**: WPF, `Interop.UIAutomationClient` (COM interop). No third-party libraries.
**Storage**: `%APPDATA%\Vimium\config.json` (existing `VimiumConfig`, extended)
**Testing**: xUnit (`Vimium.Tests`), `dotnet-coverage`
**Target Platform**: Windows 10+ / Windows 11, x64. Elevated process.

## Constitution Check

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. MVVM Separation | ✅ PASS | `SelectionModeViewModel` holds all state; code-behind limited to window lifecycle and keyboard hook dispatch. |
| II. Interface-Driven Services | ✅ PASS | New `ITextSourceProviderService` interface. Existing interfaces reused. |
| III. Testing Standards | ✅ PASS | All new services, models, and ViewModels will have xUnit tests. Coverage target ≥80%. |
| IV. UX Consistency | ✅ PASS | Search-bar overlay uses same transparency/focus model. Theme-consistent. Distinct hotkey (`Ctrl+.`) with zero overlap with element mode. |
| V. Performance & Non-Blocking | ✅ PASS | Text extraction on background thread. No synchronous UIA calls on UI thread. Overlay visible <100ms. |

## Project Structure

### Source Code (repository root)

```text
src/
├── Vimium/
│   ├── Models/
│   │   ├── SelectionState.cs                 # KEEP — cursor/search/selection state
│   │   ├── SearchMatch.cs                    # KEEP — search match entity
│   │   ├── TextSource.cs                     # NEW — text content + line rects
│   │   ├── LineNavigationSession.cs          # DELETE — replaced by TextSource
│   │   └── TextLineHint.cs                   # DELETE — no more per-line hints
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── ITextSourceProviderService.cs # RENAMED from ILineHintProviderService
│   │   │   └── IHintLabelService.cs          # Existing (unchanged)
│   │   ├── TextSourceProviderService.cs      # RENAMED from UiAutomationLineHintProviderService
│   │   ├── ClipboardService.cs               # KEEP — unchanged
│   │   └── ...                               # All existing services unchanged
│   ├── ViewModels/
│   │   ├── SelectionModeViewModel.cs         # ENHANCE — primary viewmodel
│   │   ├── ShellViewModel.cs                 # MODIFY — wire Ctrl+. directly
│   │   ├── LineNavigationOverlayViewModel.cs # DELETE — no more hint overlay
│   │   └── ...                               # All existing VMs unchanged
│   ├── Views/
│   │   ├── SelectionModeOverlayView.xaml     # ENHANCE — add search bar
│   │   ├── SelectionModeOverlayView.xaml.cs  # ENHANCE — keyboard handling
│   │   ├── LineNavigationOverlayView.xaml    # DELETE — no more hint overlay
│   │   └── LineNavigationOverlayView.xaml.cs # DELETE
│   └── ...
├── Vimium.Tests/
│   ├── Services/
│   │   ├── TextSourceProviderServiceTest.cs  # RENAMED
│   │   └── ClipboardServiceTest.cs           # KEEP
│   ├── Models/
│   │   ├── SelectionStateTest.cs             # KEEP (enhance if needed)
│   │   ├── TextLineHintTest.cs               # DELETE
│   │   └── LineNavigationSessionTest.cs      # DELETE
│   └── ViewModels/
│       ├── SelectionModeViewModelTest.cs     # KEEP (enhance)
│       └── LineNavigationOverlayViewModelTest.cs # DELETE
└── NativeMethods/
    └── User32.cs                             # Existing (unchanged)
```

## Architecture Changes

### Removed Components (5 files deleted)
- `TextLineHint` model — no more per-line hint objects
- `LineNavigationSession` model — no more hint session container
- `LineNavigationOverlayViewModel` — no more hint display logic
- `LineNavigationOverlayView` (XAML + CS) — no more hint overlay window

### Simplified Service: TextSourceProviderService
Renamed from `UiAutomationLineHintProviderService`. Instead of enumerating hints, it returns a `TextSource` with:
- `FullText`: all visible text content
- `LineRects`: per-line bounding rectangles for cursor/highlight positioning

Three-layer discovery (first-wins):
1. TextPattern.GetVisibleRanges() → text + precise per-line rects
2. TextPattern.DocumentRange.GetText() + element tree for rects
3. ValuePattern.CurrentValue + bounding rect estimation

### Enhanced View: SelectionModeOverlayView
- Add search bar UI (text input at bottom of window)
- Add match count label ("3 of 15")
- Render match highlights, active match, cursor, selection range
- Keyboard hook for all interaction keys

### Simplified Flow (ShellViewModel)
```
Ctrl+.  →  TextSourceProviderService.GetTextSource(hWnd) [background]
        →  Open SelectionModeOverlayView directly
        →  User: search → Tab cycle → Shift+Arrow select → Enter copy
```

## Implementation Phases

### Phase 1: Teardown
1. Delete `TextLineHint.cs`, `LineNavigationSession.cs`
2. Delete `LineNavigationOverlayViewModel.cs`
3. Delete `LineNavigationOverlayView.xaml` + `.xaml.cs`
4. Remove deleted file references from .csproj, tests, ShellViewModel, App.xaml.cs

### Phase 2: Core
5. Create `TextSource` model (text + line rects)
6. Rename `ILineHintProviderService` → `ITextSourceProviderService`
7. Rename `UiAutomationLineHintProviderService` → `TextSourceProviderService`
8. Simplify to return `TextSource` instead of `LineNavigationSession`

### Phase 3: UI
9. Enhance `SelectionModeOverlayView.xaml` → add search bar
10. Enhance `SelectionModeOverlayView.xaml.cs` → render match highlights + cursor
11. Wire `ShellViewModel` → `Ctrl+.` opens selection overlay directly

### Phase 4: Polish
12. Update/delete tests to match new architecture
13. Build, test, manual validation

## Reused Components
- `ClipboardService` — unchanged
- `SelectionState` — mostly unchanged
- `SearchMatch` — unchanged
- `SelectionModeViewModel` — enhanced minimally
- `HintLabelService` — still used by element mode
- `ConfigService` — existing config fields kept
