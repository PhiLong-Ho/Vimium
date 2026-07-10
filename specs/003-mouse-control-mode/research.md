# Research: Mouse Control Mode

**Feature**: `specs/003-mouse-control-mode`
**Phase**: 0 — Outline & Research
**Date**: 2026-07-10

## Research Tasks

The following unknowns were identified in the Technical Context and spec assumptions. Each is resolved below.

---

### R1: Scroll Wheel Simulation

**Question**: How to simulate mouse scroll wheel actions from a Windows desktop application without physical scroll hardware?

**Decision**: Use `SendMessage` with `WM_MOUSEWHEEL` (vertical, 0x020A) and `WM_MOUSEHWHEEL` (horizontal, 0x020E) to the window under the cursor.

**Rationale**:
- `mouse_event` does not support scroll wheel events (only `MOUSEEVENTF_WHEEL` for legacy systems, unreliable on Windows 10+)
- `SendInput` with `MOUSEINPUT` and `mi.dwFlags = MOUSEEVENTF_WHEEL` is the modern alternative but requires constructing `INPUT` structs
- `SendMessage` is simpler: identify the window under the cursor via `WindowFromPoint`, send `WM_MOUSEWHEEL` with wParam encoding the scroll delta (`WHEEL_DELTA = 120` per notch)
- Both approaches work; `SendMessage` chosen for simplicity and because it directly targets the window, matching physical scroll wheel behavior

**Alternatives considered**:
- `SendInput` with `MOUSEEVENTF_WHEEL`: More "correct" (goes through input queue), but requires `INPUT` struct marshaling and doesn't offer benefits over `SendMessage` for this use case
- UI Automation `ScrollPattern`: Only works on UIA-supporting controls; many apps (Chromium-based, custom toolkits) don't expose it reliably. Rejected for coverage reasons.

**Implementation notes**:
- Add `WM_MOUSEWHEEL` (0x020A) and `WM_MOUSEHWHEEL` (0x020E) constants to `NativeMethods/User32.cs`
- Add `SendMessage` and `WindowFromPoint` P/Invoke declarations
- Scroll amount per key press: `WHEEL_DELTA * 3` (~3 notches, configurable)
- The `WM_MOUSEHWHEEL` support varies by application; if the target window ignores it, fall back is no-op (graceful degradation)

---

### R2: Cursor Position Reading

**Question**: How to read the current cursor position for calculating movement deltas?

**Decision**: Use `GetCursorPos` (Win32) via P/Invoke, already partially available — only `SetCursorPos` exists in `User32.cs`. Add `GetCursorPos` alongside it.

**Rationale**:
- `SetCursorPos` already exists in `NativeMethods/User32.cs` — adding the read counterpart is minimal
- `System.Windows.Forms.Cursor.Position` is available but introduces a dependency on `System.Windows.Forms` from WPF; P/Invoke avoids this
- `GetCursorPos` returns screen coordinates, which is what `SetCursorPos` consumes — no coordinate conversion needed

**Implementation notes**:
- Add to `User32.cs`:
  ```csharp
  [DllImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool GetCursorPos(out POINT lpPoint);
  ```
- The existing `POINT` struct in `NativeMethods/POINT.cs` is compatible

---

### R3: Key Isolation (Consume All Input)

**Question**: How to swallow all keyboard input while mouse control mode is active, so no keys reach the OS or active application?

**Decision**: Leverage the existing `KeyboardHookService` (`WH_KEYBOARD_LL` low-level hook) which already supports `Handled = true` to swallow keys. During mouse control mode, set `Handled = true` for ALL key events except the escape mechanisms (`Escape`, `Ctrl+/`).

**Rationale**:
- `KeyboardHookService` already exists and is used by hint overlay mode for key capture
- The `KeyDownEventArgs.Handled` flag returns `1` from the hook proc, which tells Windows to not pass the key to the target application
- `WH_KEYBOARD_LL` is a low-level hook that runs before any application sees the input — it can intercept all keys including system modifier combinations
- Reusing existing infrastructure avoids adding a second hook (one hook is already installed; adding a second low-level hook adds latency)

**Alternatives considered**:
- `RegisterHotKey` per key: Impractical — would need to register every possible key
- Raw Input API: Overly complex for this use case; the existing hook pattern is sufficient
- Separate hook instance: Adds per-event latency (two hooks chained). Rejected in favor of extending the existing hook.

**Implementation notes**:
- The existing `KeyboardHookService.KeyDown` event fires for every key press. During mouse control mode, the handler sets `Handled = true` for all keys except `VK_ESCAPE` and the activation hotkey (`Ctrl+/`).
- `Ctrl+Alt+Del` (SAS) cannot be intercepted by `WH_KEYBOARD_LL` — this is a Windows security boundary and is acceptable behavior.

---

### R4: Visual Indicator Overlay (Cursor-Attached + Bottom Banner)

**Question**: How to create a non-interactive, always-visible visual overlay that follows the cursor and shows a bottom-screen status banner, without stealing focus or interfering with mouse operations?

**Decision**: Create a single transparent WPF `Window` covering the entire virtual screen with `WS_EX_TRANSPARENT` (mouse clicks pass through), `WS_EX_NOACTIVATE` (never gains focus), `WS_EX_TOPMOST` (always on top), and `WS_EX_TOOLWINDOW` (doesn't appear in taskbar). Render the cursor indicator and bottom banner as child elements positioned within this window via bindings.

**Rationale**:
- Vimium already uses this pattern for the hint overlay (`OverlayView.xaml`) — transparent, topmost, non-activating window
- A single window covering all screens is simpler than two separate windows (one following cursor, one at bottom) and avoids z-order conflicts
- The `WS_EX_TRANSPARENT` style ensures mouse clicks pass through the indicator to the real cursor target — critical since the user is actively clicking on things
- Positioning the cursor indicator via `Canvas.Left` / `Canvas.Top` bound to cursor position updates (polled on each movement key press) is straightforward WPF

**Alternatives considered**:
- Two separate windows (cursor overlay + bottom banner): More complex z-order management, potential for flicker. Rejected.
- Custom-drawn cursor replacement: Windows limits cursor size and format. Cannot render text/color reliably. Rejected.
- System tray notification only: Not visible enough for a mode that consumes all keyboard input. Rejected.

**Implementation notes**:
- Window covers `VirtualScreen.Left/Top/Width/Height` (all monitors)
- Cursor indicator: small colored dot/circle (e.g., 12px radius), positioned at cursor coordinates + offset (e.g., 16px below and right)
- Bottom banner: semi-transparent bar at bottom of primary screen, ~30px tall, showing "Mouse Control | Speed: Normal | Auto-exit: 25s"
- Update frequency: on each mouse control action (key press), not continuous polling — no CPU waste
- Colors derive from `ThemeResourceDictionary` for theme consistency (per constitution Principle IV)
