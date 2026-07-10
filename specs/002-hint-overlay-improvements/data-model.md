# Data Model: Hint Overlay Improvements

**Feature**: Hint Overlay Improvements | **Date**: 2026-07-10

## Entity Definitions

### HintAction (enum)

Represents the type of action taken when a hint is selected.

| Value | Description |
|-------|-------------|
| `Invoke` | UI Automation InvokePattern (default) |
| `LeftClick` | Real left mouse click via `mouse_event` |
| `RightClick` | Real right mouse click via `mouse_event` |
| `Hover` | Move cursor to element center (no click), triggers CSS :hover |

Serialized as string in JSON config (e.g., `"Invoke"`, `"LeftClick"`).

### ActionSlot

Maps a modifier combination to a hint action. One of three fixed slots.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `SlotIndex` | int | 0 (default, no modifier), 1, 2 | 0–2, unique |
| `Modifier` | string | Key combination e.g. `"Shift"`, `"Ctrl+Shift"` | For slot 0: empty string (forced). For slots 1–2: parsed via `HotKey.Parse`-like parser, must contain at least one modifier key |
| `Action` | HintAction | What happens when hint is selected with this modifier held | Valid enum value |

Defaults:
- Slot 0: `Modifier=""`, `Action=Invoke`
- Slot 1: `Modifier="Shift"`, `Action=LeftClick`
- Slot 2: `Modifier=""`, `Action=Invoke` (unassigned, falls back to Invoke)

### HintLabelPosition

The on-screen position of a hint label after overlap resolution.

| Field | Type | Description |
|-------|------|-------------|
| `OriginalLeft` | double | Element bounding rect left in window coords |
| `OriginalTop` | double | Element bounding rect top in window coords |
| `AdjustedLeft` | double | Left offset after spiral offsetting |
| `AdjustedTop` | double | Top offset after spiral offsetting |
| `Placement` | PlacementDirection | Which spiral position was used (enum: Default, Above, Below, Right, Left, Stacked) |

### BenchmarkLogEntry

A single enumeration session record for performance tracking.

| Field | Type | Description |
|-------|------|-------------|
| `Timestamp` | DateTime (ISO 8601) | When enumeration completed |
| `WindowTitle` | string | Foreground window title at time of activation |
| `ElementCount` | int | Total hint labels displayed |
| `ElapsedMs` | int | Milliseconds from FindAllBuildCache start to hint set complete |
| `CacheHit` | bool | True if this session reused cached results |
| `FilterMode` | string | `"InvokeFiltered"` or `"AllElements"` — which filter was active |

Serialized as one JSON object per line in `benchmark.jsonl`. The PowerShell parse script filters to `CacheHit=false` for cold-start measurements.

### VimiumConfig (extensions)

New properties added to the existing `VimiumConfig` model:

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `ActionSlots` | ActionSlot[] | [see defaults above] | Three configured modifier→action mappings |
| `BenchmarkLogEnabled` | bool | `true` | Whether enumeration sessions are logged |

### HintSession (extensions)

The existing `HintSession` model gains:

| Field | Type | Description |
|-------|------|-------------|
| `CachedHints` | IReadOnlyList<Hint> | Cached hint list for reuse when hWnd unchanged |
| `CachedHwnd` | IntPtr | Window handle this cache is valid for |
| `CachedFilterMode` | string | Filter mode used when cache was created |

## State Transitions

### Hint Enumeration Lifecycle

```
[Idle]
    │
    ▼ Hotkey pressed
[Loading] ── Overlay visible with spinner ──────────┐
    │                                                 │
    ▼ Cache check                                     │
    ├── Cache hit (same hWnd) → [Populated] (instant) │
    │                                                 │
    ▼ Cache miss                                      │
[Enumerating] ── FindAllBuildCache on background thread
    │
    ▼
[Filtering] ── Client-side fast-path rejection loop
    │
    ▼
[Resolving] ── Spiral offsetting + label assignment
    │
    ▼
[Populated] ── Hints visible, accepting match input
    │
    ├── Match complete → hint action executes → overlay closes → [Idle]
    ├── Escape / window change → overlay closes → [Idle]
    └── Single match with modifier → action via slot resolution
```

### Action Slot Resolution

```
Hint fully matched (single candidate)
    │
    ▼
Check held modifier keys via GetAsyncKeyState
    │
    ├── Modifier matches Slot 1 → use Slot 1.Action
    ├── Modifier matches Slot 2 → use Slot 2.Action
    └── No match → use Slot 0.Action (default)
```

## Relationships

```
VimiumConfig ──1:N── ActionSlot
OverlayViewModel ──1:N── HintViewModel
HintViewModel ──1:1── Hint (from HintSession)
HintViewModel ──1:1── HintLabelPosition
BenchmarkService ──1:N── BenchmarkLogEntry
UiAutomationHintProviderService ──1:1── HintSession (cached)
OverlapResolver ──1:N── HintLabelPosition (adjusts positions)
```
