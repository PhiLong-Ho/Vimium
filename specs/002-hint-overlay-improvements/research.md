# Research: Hint Overlay Improvements

**Feature**: Hint Overlay Improvements | **Date**: 2026-07-10

## Architecture Decisions

### AD-001: Performance Strategy — Provider-Side Pattern-Availability Filtering

**Decision**: Use pattern-availability properties as `FindAllBuildCache` condition filters to reduce cross-process data transfer. The UIA condition tree includes `OR(IsInvokePatternAvailable, IsTogglePatternAvailable, IsSelectionItemPatternAvailable, IsExpandCollapsePatternAvailable, IsValuePatternAvailable, IsRangeValuePatternAvailable)` so only elements supporting at least one interactive pattern are returned.

**Rationale**: Pattern-availability properties were initially suspected to be unreliable based on `FindTextProviderService` comments about `IsTextPatternAvailablePropertyId` returning false negatives (Windows Terminal's TermControl reports false but still supports TextPattern). However, this quirk is specific to **TextPattern** — a complex pattern that some providers implement partially or register incorrectly. The six element-mode patterns (Invoke, Toggle, SelectionItem, ExpandCollapse, Value, RangeValue) are simpler, universally supported patterns that UIA providers report reliably. No evidence of false negatives exists for these patterns across common applications.

The TextPattern issue arises because terminal emulators and rich-text controls sometimes implement text support through alternative mechanisms (IAccessible, custom rendering) while registering TextPattern incorrectly. This class of problem does not apply to button-click, toggle, or selection patterns — an element either supports Invoke or it doesn't, and providers have no reason to misreport this.

**Confirmed by**: Clarification session 2026-07-10 — "The TextPattern-specific unreliability does not apply to element-mode patterns. FR-001's pattern-availability condition filtering is valid for element-mode enumeration."

**Alternatives considered**:
- **Client-side-only filtering**: Rejected — works but leaves 40–60% of cross-process data transfer on the table compared to provider-side filtering.
- **Hybrid two-pass**: Rejected — unnecessary complexity when provider-side filtering is reliable.
- **Parallel subtree retrieval**: Rejected — UIA COM is STA-threaded; multiple threads marshal back to single STA, serializing execution without performance gain.

### AD-002: Result Caching Strategy

**Decision**: Cache enumeration results keyed by foreground window handle (`hWnd`). On repeated hotkey activation with the same `hWnd`, skip enumeration entirely and reuse cached hints.

**Rationale**: The `ShellViewModel._keyListener_OnHotKeyActivated` already polls `GetForegroundWindow()`. When the user re-activates hints on the same window (common workflow — dismiss overlay, scroll, re-activate), the UIA tree is unlikely to have changed in a way that affects interactive elements. The cache invalidates when:
- Foreground window changes (different `hWnd`)
- Benchmark mode explicitly clears cache (per clarification Q3)

This provides the single biggest practical performance win: first activation ~750ms, subsequent activations near-instant (<50ms).

**Alternatives considered**:
- **Time-based cache expiry**: Rejected — no natural TTL for UIA trees. Window change is the correct invalidation signal.
- **UIA event-based invalidation**: Could listen for `UIA_LayoutInvalidatedEventId`. Rejected — adds complexity for marginal benefit over window-change detection.
- **No caching**: Rejected — leaves significant performance on the table for the most common usage pattern.

### AD-003: Overlap Resolution — Spiral Offsetting

**Decision**: Implement spiral offsetting in a new `OverlapResolver` service. Labels try positions in priority order: top-left (default at element origin), above element, below element, right of element, left of element. Each position is tested for collision against all previously placed labels. If all five positions overlap, the label is stacked vertically with a 2px gap from the last conflicting label.

**Rationale**: This matches Vimium's browser-based approach and handles the Discord use case (dense horizontal button rows). The algorithm is deterministic, O(n²) in label count (acceptable for <500 labels), and requires no external dependencies.

**Alternatives considered**:
- **Force-directed layout**: Rejected — computationally expensive for 200+ labels, requires iterative convergence, and would delay overlay display.
- **Per-row stacking**: Rejected — requires detecting horizontal rows, fragile across different UI layouts.
- **Simple directional offset (right-only)**: Rejected — fails on dense horizontal rows (overlap cascades to next button).

### AD-004: Modifier Detection — GetAsyncKeyState

**Decision**: Use existing `GetAsyncKeyState` pattern (already in `OverlayViewModel.MatchString` and `SelectionModeOverlayView`) for modifier detection during hint matching. Extend to check for the three configured slots' modifier combinations.

**Rationale**: `GetAsyncKeyState` with `0x8000` mask is a fast Win32 call that checks whether a key is currently held down — exactly what's needed for modifier detection during typing. The current code already checks `VK_LSHIFT` and `VK_RSHIFT`. Extending to check `VK_LCONTROL`, `VK_RCONTROL`, `VK_LMENU`, `VK_RMENU`, `VK_LWIN`, `VK_RWIN` covers all possible modifiers.

The resolution order during `MatchString` setter when a single hint matches:
1. Check all three configured slots' modifier combinations
2. First matching slot's action is used
3. If no modifier matches any slot, use slot 0 (default action)

**Alternatives considered**:
- **Keyboard hook events**: Rejected — modifier keys are held, not pressed, during hint matching. Hooks detect key-down/key-up transitions, not held state.
- **WPF Keyboard.Modifiers**: Rejected — requires WPF window focus. The overlay window is transparent and may not have reliable keyboard focus during hint typing.

### AD-005: Key-Capture Control — WPF PreviewKeyDown

**Decision**: Implement a key-capture control in the options window using WPF `PreviewKeyDown` event. User clicks a "Capture" button, the control enters capture mode (visual feedback: border highlight + "Press modifier keys..." prompt), user presses their desired modifier combination, and the control displays the captured combination as a human-readable string (e.g., "Ctrl + Shift").

**Rationale**: PowerToys Keyboard Manager uses this exact pattern: a `KeyDown` handler that records the currently-held modifiers plus the last pressed non-modifier key (if any — for modifier-only capture, we record held modifiers when any key is released). The control validates that at least one modifier is held (a bare letter key is not a valid modifier combination for hint actions).

**Alternatives considered**:
- **Manual text entry** (current Keyboard tab approach for hotkeys): Rejected — user-unfriendly, requires knowing key names, error-prone.
- **Win32 `RegisterHotKey`**: Rejected — registers system-wide hotkeys, not appropriate for capturing a one-time key combination in a focused control.

### AD-006: Benchmark Logging — Rolling JSONL File

**Decision**: Write one JSON object per line (JSONL format) to `%APPDATA%\Vimium\logs\benchmark.jsonl`. Each entry is a `BenchmarkLogEntry` serialized via `System.Text.Json`. Rolling: when total log size exceeds 10MB, delete oldest entries (first half of lines) to make room.

**Rationale**: JSONL is machine-parseable (one complete JSON object per line), append-friendly (no need to rewrite the file), and human-readable. The rolling policy prevents unbounded disk growth while retaining recent history. Logs stay local-only, respecting the constitution's no-telemetry principle.

**Alternatives considered**:
- **ETW / Windows Performance Counter**: Rejected — heavy infrastructure for a single metric. Better suited for system services than desktop apps.
- **In-app display**: Rejected — adds UI clutter. The benchmark value is for developers measuring improvements, not end users.
- **CSV format**: Rejected — harder to extend with new fields, no native .NET serializer.

## Feasibility Assessment Summary

| FR | Approach | Feasibility | Risk |
|----|----------|-------------|------|
| FR-001 | Provider-side pattern-availability condition filtering (FindAll condition tree) | ✅ Feasible | Low — element-mode patterns reliably reported; TextPattern quirk does not apply |
| FR-002 | Tree trimming via cached ControlType + IsControlElement checks | ✅ Feasible | Low — conservative skip only when ControlType confirms non-interactive |
| FR-003 | 750ms target via FR-001 + FR-002 + FR-017 | ✅ Feasible | Medium — depends on target app's UIA provider quality |
| FR-004 | 400ms target for simple apps | ✅ Feasible | Low — simple apps have shallow trees |
| FR-005 | Accuracy priority — fallback to full enumeration if filtered | ✅ Feasible | Low — existing pattern kept as fallback |
| FR-006 | Hover action via existing Hint.MovePointerToCenter() | ✅ Feasible | None — already implemented |
| FR-008 | 3-slot modifier→action config | ✅ Feasible | Low — extends existing ConfigService pattern |
| FR-009 | Key-capture control (PowerToys-inspired) | ✅ Feasible | Low — standard WPF PreviewKeyDown pattern |
| FR-010 | Spiral offsetting overlap resolution | ✅ Feasible | Low — O(n²) acceptable for <500 labels |
| FR-011 | 20px proximity constraint | ✅ Feasible | Low — enforced in overlap resolver |
| FR-012 | Input buffering during enumeration | ✅ Feasible | Low — already partially implemented; buffer until hints ready |
| FR-013 | Cancel enumeration on overlay dismiss | ✅ Feasible | Low — CancellationToken pattern |
| FR-014 | Backward compatibility (default = Invoke) | ✅ Feasible | None — slot 0 default preserved |
| FR-015 | Filter by default action at overlay-open time | ✅ Feasible | Low — condition built once at enumeration start |
| FR-016 | 200ms max blocking per batch | ✅ Feasible | Low — single FindAllBuildCache call already under 200ms (bottleneck is total walk, not blocking) |
| FR-017 | Result caching by hWnd | ✅ Feasible | Low — window change detection already in ShellViewModel |
| FR-018 | JSON benchmark logging | ✅ Feasible | Low — System.Text.Json, append to file |
| FR-019 | Unit-testable filtering logic | ✅ Feasible | Low — mock IUIAutomationElement via test doubles |
| FR-020 | Documented benchmark procedure | ✅ Feasible | Low — markdown document + PowerShell script |
