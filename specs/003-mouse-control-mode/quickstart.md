# Quickstart & Validation Guide: Mouse Control Mode

**Feature**: `specs/003-mouse-control-mode`
**Phase**: 1 — Design & Contracts
**Date**: 2026-07-10

## Prerequisites

- Windows 10 or Windows 11
- .NET 10 SDK installed
- Repository cloned at `D:\Workspace\Vimium`
- Built solution: `dotnet build src\Vimium.sln`

## Setup

```bash
# Build the solution
dotnet build src\Vimium.sln

# Run tests (must pass before starting)
dotnet test src\Vimium.sln
```

## Validation Scenarios

### VS-1: Mouse Control Mode Activation & Movement (US1, SC-001, SC-002)

**Goal**: Verify mouse control mode activates and cursor moves with keyboard.

1. Press `Ctrl+/`
2. **Expected**: 
   - A small colored indicator appears near the mouse cursor
   - A thin status banner appears at the bottom of the screen showing "Mouse Control | Speed: Normal"
   - All keyboard input is consumed (try typing — nothing should appear)
3. Press **W** — cursor moves **up**
4. Press **A** — cursor moves **left**
5. Press **S** — cursor moves **down**
6. Press **D** — cursor moves **right**
7. Hold **W** — cursor moves up continuously (key repeat)
8. Move cursor to a screen edge — cursor stops at the edge
9. Press **Escape** — mode exits, visual indicators disappear, keyboard works normally

**Pass if**: Activation happens within 1 second of `Ctrl+/`, cursor moves within 50ms of key press, screen edge clamping works, Escape exits cleanly.

---

### VS-2: Mouse Clicks & Drag (US1, SC-003)

**Goal**: Verify left/right click and drag-and-drop work.

1. Activate mouse control mode (`Ctrl+/`)
2. Move cursor over a desktop icon
3. Press and quickly release **left Shift**
4. **Expected**: Icon is selected (single left click)
5. Move cursor over another icon, press and quickly release **right Shift**
6. **Expected**: Context menu appears (single right click)
7. Move cursor over a desktop icon, **hold left Shift**, press **D** a few times, **release left Shift**
8. **Expected**: Icon is dragged to the new position (drag-and-drop)

**Pass if**: Both click types work; press-and-hold enables drag; releasing completes the drop.

---

### VS-3: Scrolling (US2, SC-004)

**Goal**: Verify keyboard scrolling works in a scrollable window.

1. Open a scrollable window (e.g., Notepad with a long document, or a browser window)
2. Activate mouse control mode (`Ctrl+/`)
3. Move cursor over the scrollable window
4. Press **I** — window scrolls **up**
5. Press **K** — window scrolls **down**
6. Press **J** — window scrolls **left** (if applicable)
7. Press **L** — window scrolls **right** (if applicable)
8. Move cursor over desktop (not scrollable), press **I** — nothing happens (no error)

**Pass if**: Scroll responds within 100ms and correctly scrolls the window under the cursor.

---

### VS-4: Speed Toggle (US3, SC-005)

**Goal**: Verify speed cycling and feedback.

1. Activate mouse control mode (`Ctrl+/`)
2. Observe bottom banner: **Speed: Normal**
3. Press **Space** — banner shows **Speed: Slow**; cursor moves in smaller increments
4. Press **Space** — banner shows **Speed: Fast**; cursor moves in larger increments
5. Press **Space** — banner shows **Speed: Normal**; cursor returns to normal speed
6. Exit mode (`Escape`), re-activate (`Ctrl+/`)
7. **Expected**: Speed resets to **Normal** (session state does not persist between activations)

**Pass if**: All three modes are distinguishable and cycle correctly; speed resets on each new activation.

---

### VS-5: Auto-Exit on Inactivity (US1, FR-007)

**Goal**: Verify mouse control mode exits after 30 seconds of inactivity.

1. Activate mouse control mode (`Ctrl+/`)
2. Do nothing for 20 seconds
3. At **20 seconds of inactivity**, the bottom banner should show an auto-exit warning with countdown (10... 9... ... 1...)
4. Do nothing further — at **30 seconds total**, the mode exits automatically
5. Activate mode again, wait 15 seconds, press any mouse control key
6. **Expected**: The inactivity timer resets; no auto-exit warning appears
7. Wait another 20 seconds → warning appears, press any key
8. **Expected**: Warning dismisses and timer resets

**Pass if**: Auto-exit triggers at exactly 30s with no input; warning appears at 10s remaining; any mouse control key resets the timer.

---

### VS-6: Key Binding Configuration (US4, SC-006, SC-007)

**Goal**: Verify mouse control keys can be customized.

1. Open Vimium **Options** → navigate to **Keyboard** or **Mouse Control** section
2. Verify all 12 configurable keys are displayed with their current defaults (WASD, IJKL, Shift keys, Space, Ctrl+/)
3. Change "Move Up" from **W** to **NumPad8**
4. Activate mouse control mode, press NumPad8 — cursor moves up
5. Change "Move Up" to the same key as "Move Down" (conflict)
6. **Expected**: Warning is shown; the conflicting assignment is rejected
7. Click "Reset to Defaults" for mouse control keys
8. **Expected**: All bindings revert to WASD/IJKL/Shift/Space/Ctrl+/ defaults

**Pass if**: Changes take effect immediately (no restart); conflicts are detected and warned; reset works.

---

### VS-7: Mode Isolation (US1, FR-006)

**Goal**: Verify that no keyboard input reaches applications while mouse control mode is active.

1. Open Notepad and position cursor in the text area
2. Activate mouse control mode (`Ctrl+/`)
3. Type random letters (A, B, C, etc.) — **nothing should appear in Notepad**
4. Press `Ctrl+S` (normally Save) — **nothing should happen**
5. Press `Alt+Tab` — **nothing should happen** (or Alt+Tab is consumed)
6. Press `Ctrl+/` or `Escape` — mode exits
7. Now type in Notepad — **typing works normally again**

**Pass if**: No keystrokes (except Escape and Ctrl+/) reach the active application while mouse control mode is active.

---

### VS-8: Interaction with Other Vimium Modes (Edge Case, FR-005)

**Goal**: Verify mutual exclusion with element mode and text selection mode.

1. Activate mouse control mode (`Ctrl+/`)
2. While mouse control is active, press `Ctrl+;` (element mode hotkey)
3. **Expected**: Mouse control mode exits first; then element mode may activate
4. Activate element mode (`Ctrl+;`) — hints appear
5. While hints are visible, press `Ctrl+/`
6. **Expected**: Element mode exits first; then mouse control mode may activate

**Pass if**: Only one mode is active at a time; activating one exits the other.

---

## Manual Test Checklist (for PR)

- [ ] VS-1: Mouse movement + visual indicators
- [ ] VS-2: Clicks + drag-and-drop
- [ ] VS-3: Scrolling
- [ ] VS-4: Speed toggle + reset
- [ ] VS-5: Auto-exit after 30s inactivity
- [ ] VS-6: Key binding configuration + conflict detection
- [ ] VS-7: Full key isolation
- [ ] VS-8: Mutual exclusion with other modes

## Running Tests

```bash
# Run all tests
dotnet test src\Vimium.sln

# Run specific test files
dotnet test src\Vimium.sln --filter "FullyQualifiedName~MouseControl"

# Check coverage (if tooling installed)
dotnet test src\Vimium.sln --collect:"XPlat Code Coverage"
```
