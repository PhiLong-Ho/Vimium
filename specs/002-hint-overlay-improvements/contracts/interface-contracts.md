# Interface Contracts: Hint Overlay Improvements

**Feature**: Hint Overlay Improvements | **Date**: 2026-07-10

## IOverlapResolver

New interface for hint label overlap resolution.

```csharp
namespace Vimium.Services.Interfaces
{
    /// <summary>
    /// Resolves overlapping hint labels using spiral offsetting.
    /// </summary>
    public interface IOverlapResolver
    {
        /// <summary>
        /// Adjusts hint label positions so no two labels visually overlap.
        /// Labels are repositioned in priority order: default (top-left),
        /// above, below, right, left. Labels that still overlap after all
        /// five positions are stacked vertically.
        /// </summary>
        /// <param name="positions">Hint label positions with their original
        /// bounding rectangles (in window coordinates). Modified in place.</param>
        /// <param name="maxOffset">Maximum offset from element edge (px).
        /// Per FR-010: 20px.</param>
        void ResolveOverlaps(IReadOnlyList<HintLabelPosition> positions, double maxOffset);
    }
}
```

### Contract

- **Input**: Ordered list of hint label positions (process in order — earlier elements get preferred placement). Each position has `OriginalLeft`, `OriginalTop`, and label dimensions (width/height from the text content).
- **Behavior**: For each label (i from 0 to N-1):
  1. Start at default position (top-left of element)
  2. Test for collision against all already-placed labels (0 to i-1)
  3. If collision detected, try: above, below, right, left (each offset by `maxOffset` or less)
  4. If all five positions collide, stack vertically below the last conflicting label with a 2px gap
  5. Set `AdjustedLeft`, `AdjustedTop`, and `Placement`
- **Output**: Same list with `AdjustedLeft`, `AdjustedTop`, `Placement` populated
- **Complexity**: O(n²) where n = label count. Acceptable for n ≤ 500.
- **Deterministic**: Same input always produces same output. No randomness.
- **Thread safety**: Called on UI thread after `PopulateHints`. Single-threaded usage.

## IBenchmarkService

New interface for structured performance logging.

```csharp
namespace Vimium.Services.Interfaces
{
    /// <summary>
    /// Logs enumeration session metrics as structured JSON entries
    /// to a rolling log file. Local-only — no telemetry.
    /// </summary>
    public interface IBenchmarkService
    {
        /// <summary>
        /// Writes a benchmark entry for the just-completed enumeration session.
        /// No-op if benchmark logging is disabled in config.
        /// </summary>
        void LogSession(BenchmarkLogEntry entry);

        /// <summary>
        /// Clears the enumeration result cache, forcing the next activation
        /// to perform a full enumeration. Used by the benchmark procedure
        /// to ensure cold-start measurements.
        /// </summary>
        void InvalidateCache();

        /// <summary>
        /// True if benchmark logging is enabled in user configuration.
        /// </summary>
        bool IsEnabled { get; }
    }
}
```

### Contract

- **LogSession**: Appends one JSON line to `%APPDATA%\Vimium\logs\benchmark.jsonl`. If file doesn't exist, creates it. If file exceeds 10MB, truncates oldest half of entries before appending. Thread-safe (lock on file write). Never throws — exceptions are caught and silently dropped (logging must never break the feature).
- **InvalidateCache**: Delegates to `IHintProviderService` to clear the cached `HintSession`. Next enumeration will be a cold start.
- **IsEnabled**: Reads from `ConfigService.BenchmarkLogEnabled`. Default `true`.

## IHintProviderService (extensions)

Existing interface gains one method:

```csharp
/// <summary>
/// Clears the cached enumeration result, forcing the next call
/// to EnumHintsAsync to perform a full UIA tree walk.
/// </summary>
void InvalidateCache();
```

### Contract

- After calling `InvalidateCache()`, the next `EnumHintsAsync(hWnd)` performs a fresh `FindAllBuildCache` call regardless of whether `hWnd` matches the cached window handle.
- Calling `InvalidateCache()` when no cache exists is a no-op.

## Hint.MovePointerToCenter() (existing)

Existing method on the `Hint` base class. Used by the Hover action. No contract changes.

```csharp
/// <summary>
/// Moves the mouse pointer to the center of this hint.
/// </summary>
public void MovePointerToCenter();
```

## OverlayViewModel.MatchString (extended contract)

The existing `MatchString` setter in `OverlayViewModel` gains multi-slot action resolution.

**Current behavior** (preserved):
- User types characters → matching hints are highlighted
- Single match + Left Shift held → `hint.Click()` (real left click)
- Single match + Right Shift held → `hint.RightClick()` (real right click)
- Single match + no modifier → `hint.Invoke()` (UIA invoke)

**New behavior**:
1. Read `ActionSlots` from `ConfigService` (passed via constructor or injected)
2. Check held modifier keys via `GetAsyncKeyState` at match time
3. Resolve: find the first slot whose modifier matches the currently-held keys
4. Execute the resolved action:
   - `Invoke` → `hint.Invoke()`
   - `LeftClick` → `hint.Click()`
   - `RightClick` → `hint.RightClick()`
   - `Hover` → `hint.MovePointerToCenter()` (no click, cursor persists)
5. Default fallback: if no slot matches, use Slot 0's action (always present)

### Modifier Checking Contract

All modifier checks use `GetAsyncKeyState` with `0x8000` mask:
- `VK_LSHIFT` / `VK_RSHIFT` → "Shift"
- `VK_LCONTROL` / `VK_RCONTROL` → "Ctrl"
- `VK_LMENU` / `VK_RMENU` → "Alt"
- `VK_LWIN` / `VK_RWIN` → "Win"

Modifier matching is symmetric (left/right treated the same). Two-key combos (e.g., "Ctrl+Shift") require BOTH modifiers to be held simultaneously.
