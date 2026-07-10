# Quickstart: Hint Overlay Improvements

**Feature**: Hint Overlay Improvements | **Date**: 2026-07-10

## Prerequisites

- Windows 10 or Windows 11
- .NET 10 SDK installed
- Chrome browser installed (for benchmark procedure)
- Vimium built and running (`dotnet run --project src\Vimium`)

## Validation Scenarios

### VS-001: Verify Instant Hint Visibility (P1)

**Objective**: Confirm hints appear within 750ms on a complex application.

1. Launch Chrome and navigate to `https://en.wikipedia.org/wiki/Singapore`
2. Maximize the Chrome window to 1920×1080
3. Ensure Vimium is running in the system tray
4. Press the element-mode hotkey (default `Ctrl+;`)
5. **Expected**: A loading indicator appears immediately (<100ms), then all hint labels appear within 750ms
6. **Verify**: Open `%APPDATA%\Vimium\logs\benchmark.jsonl` — locate the entry with `"windowTitle"` containing "Singapore". Confirm `"elapsedMs" < 750`.
7. Repeat 5 times — re-activating on the same window should use cache (near-instant, `<100ms`)

### VS-002: Verify Action Configuration (P2)

**Objective**: Confirm modifier→action mapping works via key-capture UI.

1. Right-click Vimium tray icon → Options → navigate to "Actions" tab (new)
2. Click the key-capture control for Slot 1 (currently "Shift")
3. Press `Ctrl` — the control displays "Ctrl"
4. Select "Move mouse only" from the action dropdown for Slot 1
5. Close options (auto-saves)
6. Activate hints on any window (`Ctrl+;`)
7. Type a hint label while holding `Ctrl`
8. **Expected**: The cursor moves to the element's center but no click occurs. Verify by checking that the target button/link is NOT activated.
9. Repeat for Slot 2 (assign "Hover" to a modifier, verify cursor persists on target)

### VS-003: Verify Non-Overlapping Hints (P2)

**Objective**: Confirm hint labels don't overlap on Discord.

1. Launch Discord desktop app
2. Navigate to a channel with multiple messages, each showing action buttons (Reaction, Edit, More)
3. Hover over a message to reveal the action button row
4. Activate hints (`Ctrl+;`)
5. **Expected**: Each hint label is clearly readable. No two labels overlap. Labels are close to their target elements (within ~20px).
6. **Verify**: Visually inspect the hint overlay. Labels on dense button rows should be offset in different directions (above/below/right/left).

### VS-004: Verify Hover Action Reveals Hidden Elements (P2)

**Objective**: Confirm hover action reveals elements that appear only on mouse hover.

1. In Discord, configure Slot 1 to "Hover" action with `Ctrl` modifier (via Options)
2. Activate hints (`Ctrl+;`)
3. Type a hint label for a Discord server icon in the sidebar while holding `Ctrl`
4. **Expected**: The cursor moves to the server icon and stays there. The server name tooltip or context hover card appears.
5. Re-activate hints — the newly revealed tooltip/hover card elements should now have hint labels.

### VS-005: Verify Benchmark Logging (Observability)

**Objective**: Confirm benchmark log entries are written correctly.

1. Activate hints on any window 5 times
2. Open `%APPDATA%\Vimium\logs\benchmark.jsonl` in a text editor
3. **Expected**: 5 JSON lines, one per activation. Each has fields: `timestamp`, `windowTitle`, `elementCount`, `elapsedMs`, `cacheHit`, `filterMode`.
4. First activation: `"cacheHit": false` (cold start)
5. Subsequent activations on same window: `"cacheHit": true` (near-instant)

### VS-006: Run Full Benchmark Procedure

**Objective**: Measure 95th-percentile cold-start latency.

1. Build Vimium: `dotnet build src\Vimium.sln`
2. Launch Chrome, navigate to `https://en.wikipedia.org/wiki/Singapore`, maximize window
3. Ensure benchmark logging is enabled (default: true)
4. Delete any existing benchmark log: `del %APPDATA%\Vimium\logs\benchmark.jsonl`
5. Run PowerShell script: `powershell -File scripts\run-benchmark.ps1 -Repetitions 20`
   - Script clears cache between each repetition
   - Script activates hints via hotkey (simulates keystroke)
   - Script reads the benchmark log after each activation
6. **Expected output** from parse script:
   ```
   Repetitions: 20
   Mean:     [X]ms
   Median:   [X]ms
   P95:      [X]ms  ← must be <750ms
   Min:      [X]ms
   Max:      [X]ms
   ```
7. **Verify**: P95 < 750ms

## Build Verification

```bash
# Ensure all tests pass
dotnet test src\Vimium.sln

# Ensure build with zero warnings
dotnet build src\Vimium.sln /warnaserror
```

## Quick Smoke Test

Minimal 30-second check after any change:

1. `dotnet test src\Vimium.sln` — all tests green
2. Activate hints (`Ctrl+;`) on Notepad — hints appear quickly
3. Activate hints on Chrome Wikipedia page — hints appear within ~1s
4. Re-activate on same Chrome window — instant (cache hit)
5. Check `benchmark.jsonl` has entries
