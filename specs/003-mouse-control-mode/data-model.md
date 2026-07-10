# Data Model: Mouse Control Mode

**Feature**: `specs/003-mouse-control-mode`
**Phase**: 1 — Design & Contracts
**Date**: 2026-07-10

## Entities

### 1. MouseControlConfiguration (new, nested in VimiumConfig)

Stores all user-configurable settings for mouse control mode. Persisted as a JSON object under the `mouseControl` key in `config.json`.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `ActivationHotKey` | `string` | `"Ctrl+/"` | Hotkey to toggle mouse control mode on/off (FR-001) |
| `MoveUp` | `string` | `"W"` | Key to move cursor up (FR-008) |
| `MoveDown` | `string` | `"S"` | Key to move cursor down (FR-010) |
| `MoveLeft` | `string` | `"A"` | Key to move cursor left (FR-009) |
| `MoveRight` | `string` | `"D"` | Key to move cursor right (FR-011) |
| `ScrollUp` | `string` | `"I"` | Key to scroll up (FR-018) |
| `ScrollDown` | `string` | `"K"` | Key to scroll down (FR-019) |
| `ScrollLeft` | `string` | `"J"` | Key to scroll left (FR-020) |
| `ScrollRight` | `string` | `"L"` | Key to scroll right (FR-021) |
| `LeftClick` | `string` | `"LShiftKey"` | Key for left click/drag (FR-014, FR-015) |
| `RightClick` | `string` | `"RShiftKey"` | Key for right click/drag (FR-016, FR-017) |
| `SpeedToggle` | `string` | `"Space"` | Key to cycle speed modes (FR-022) |
| `NormalSpeedPx` | `int` | `15` | Pixel increment per key press at normal speed (FR-023) |
| `SlowSpeedPx` | `int` | `5` | Pixel increment per key press at slow speed (FR-024) |
| `FastSpeedPx` | `int` | `50` | Pixel increment per key press at fast speed (FR-025) |
| `ScrollLinesPerTick` | `int` | `3` | Scroll lines per key press, in WHEEL_DELTA units (FR-018–021) |
| `AutoExitSeconds` | `int` | `30` | Inactivity timeout before auto-exit (FR-007) |
| `AutoExitWarningSeconds` | `int` | `10` | Warning shown this many seconds before auto-exit (FR-007) |

**Identity**: Singleton — one instance per config file.

**Validation rules**:
- Key values must use `System.Windows.Forms.Keys` enum parseable strings (existing `HotKey.Parse` pattern)
- No two bindings may share the same key (within-mode conflict detection per FR-031); the activation hotkey MUST NOT collide with another interaction mode's hotkey or another global hotkey (per FR-032)
- Speed values must be positive integers; `SlowSpeedPx < NormalSpeedPx < FastSpeedPx`
- `AutoExitSeconds` ≥ `AutoExitWarningSeconds` ≥ 1

**State transitions**: N/A — static configuration, no runtime state.

---

### 2. MouseControlSession (runtime only, not persisted)

Represents the transient state of an active mouse control session. Exists only in memory while the mode is active. Destroyed on exit.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `IsActive` | `bool` | `false` | Whether mouse control mode is currently active |
| `CurrentSpeed` | `enum SpeedMode` | `Normal` | Current cursor movement speed (FR-023, FR-027) |
| `IsLeftButtonHeld` | `bool` | `false` | Whether left mouse button is currently pressed (drag state) (FR-015) |
| `IsRightButtonHeld` | `bool` | `false` | Whether right mouse button is currently pressed (drag state) (FR-017) |
| `LastInputTime` | `DateTime` | _now on activation_ | Timestamp of last mouse control input (for auto-exit timer) |
| `AutoExitWarningShown` | `bool` | `false` | Whether the 10-second auto-exit warning has been displayed |

**State transitions**:

```
[Inactive] ──Ctrl+/──▶ [Active: NormalSpeed]
                            │
                            ├──Space──▶ [Active: SlowSpeed]
                            │                │
                            │                ├──Space──▶ [Active: FastSpeed]
                            │                │                │
                            │                │                ├──Space──▶ [Active: NormalSpeed]
                            │                │                │
                            │                ├──WASD──▶ (move cursor, update LastInputTime)
                            │                ├──Shift↓──▶ (press button, set IsHeld=true)
                            │                ├──Shift↑──▶ (release button, set IsHeld=false)
                            │                └──IJKL──▶ (scroll, update LastInputTime)
                            │
                            ├──Esc / Ctrl+/──▶ [Inactive] (release any held buttons)
                            └──30s inactivity──▶ [Inactive] (auto-exit, release any held buttons)
```

**Enum: SpeedMode**

| Value | Description |
|-------|-------------|
| `Normal` | Default speed (~15px per step). Initial mode on activation. |
| `Slow` | Precision speed (~5px per step). Activated by first `Space` press. |
| `Fast` | Rapid navigation (~50px per step). Activated by second `Space` press. |

---

## JSON Schema (config.json additions)

```jsonc
{
  // ... existing keys ...
  "mouseControl": {                 // NEW: object
    "activationHotKey": "Ctrl+/",
    "moveUp": "W",
    "moveDown": "S",
    "moveLeft": "A",
    "moveRight": "D",
    "scrollUp": "I",
    "scrollDown": "K",
    "scrollLeft": "J",
    "scrollRight": "L",
    "leftClick": "LShiftKey",
    "rightClick": "RShiftKey",
    "speedToggle": "Space",
    "normalSpeedPx": 15,
    "slowSpeedPx": 5,
    "fastSpeedPx": 50,
    "scrollLinesPerTick": 3,
    "autoExitSeconds": 30,
    "autoExitWarningSeconds": 10
  }
}
```

**Backward compatibility**: A missing `mouseControl` key on existing config files → use the defaults shown above. No migration needed.
