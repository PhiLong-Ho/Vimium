# IMouseControlService

**Feature**: `specs/003-mouse-control-mode`
**Phase**: 1 — Design & Contracts

## Interface

```csharp
namespace Vimium.Services.Interfaces
{
    public enum SpeedMode
    {
        Normal = 0,
        Slow = 1,
        Fast = 2
    }

    public interface IMouseControlService
    {
        /// <summary>Activate mouse control mode. Returns true if activation succeeded.</summary>
        bool Activate();

        /// <summary>Deactivate mouse control mode, releasing any held buttons.</summary>
        void Deactivate();

        /// <summary>Whether mouse control mode is currently active.</summary>
        bool IsActive { get; }

        /// <summary>Current speed mode (Normal, Slow, Fast).</summary>
        SpeedMode CurrentSpeed { get; }

        /// <summary>Whether left mouse button is currently held (drag state).</summary>
        bool IsLeftButtonHeld { get; }

        /// <summary>Whether right mouse button is currently held (drag state).</summary>
        bool IsRightButtonHeld { get; }

        /// <summary>Move cursor up by current speed increment.</summary>
        void MoveUp();

        /// <summary>Move cursor down by current speed increment.</summary>
        void MoveDown();

        /// <summary>Move cursor left by current speed increment.</summary>
        void MoveLeft();

        /// <summary>Move cursor right by current speed increment.</summary>
        void MoveRight();

        /// <summary>Scroll the window under cursor upward.</summary>
        void ScrollUp();

        /// <summary>Scroll the window under cursor downward.</summary>
        void ScrollDown();

        /// <summary>Scroll the window under cursor left.</summary>
        void ScrollLeft();

        /// <summary>Scroll the window under cursor right.</summary>
        void ScrollRight();

        /// <summary>Perform a left click at current cursor position.</summary>
        void LeftClick();

        /// <summary>Hold left mouse button (for drag).</summary>
        void LeftButtonDown();

        /// <summary>Release left mouse button (complete drag).</summary>
        void LeftButtonUp();

        /// <summary>Perform a right click at current cursor position.</summary>
        void RightClick();

        /// <summary>Hold right mouse button (for drag).</summary>
        void RightButtonDown();

        /// <summary>Release right mouse button (complete drag).</summary>
        void RightButtonUp();

        /// <summary>Cycle to next speed mode (Normal→Slow→Fast→Normal).</summary>
        void CycleSpeed();

        /// <summary>Event raised when mouse control mode is activated.</summary>
        event EventHandler? Activated;

        /// <summary>Event raised when mouse control mode is deactivated.</summary>
        event EventHandler? Deactivated;

        /// <summary>Event raised when speed mode changes.</summary>
        event EventHandler<SpeedMode>? SpeedChanged;
    }
}
```

## Contract Notes

- **Thread safety**: Movement/click methods called from keyboard hook thread. Events raised on calling thread; subscribers marshal to UI as needed.
- **Screen bounds**: Movement clamps to `SystemParameters.VirtualScreen` across all monitors.
- **DPI scaling**: Pixel increments scaled by the DPI factor of the monitor currently under the cursor (consistent across mixed-DPI multi-monitor setups).
- **Speed reset**: `Activate()` resets `CurrentSpeed` to `Normal`. Speed does not persist between sessions.
- **Button cleanup**: `Deactivate()` releases any held mouse buttons.
- **Scroll**: `WindowFromPoint` → `SendMessage(WM_MOUSEWHEEL/HWHEEL)` with `WHEEL_DELTA * scrollLinesPerTick`.

## Mapping to Requirements

| Method | FR |
|--------|-----|
| MoveUp/Down/Left/Right | FR-008–011 |
| LeftClick, LeftButtonDown/Up | FR-014–015 |
| RightClick, RightButtonDown/Up | FR-016–017 |
| ScrollUp/Down/Left/Right | FR-018–021 |
| CycleSpeed | FR-022–027 |
| Activate/Deactivate | FR-001, FR-003–007 |

## Testability

- Full mocking in ViewModel tests.
- Win32 interop isolated behind interface in `MouseControlService`.
- Keyboard hook integration tested via manual scenarios.
