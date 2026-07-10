# Feature Specification: Mouse Control Mode

**Feature Branch**: `003-mouse-control-mode`

**Created**: 2026-07-10

**Status**: Draft

**Input**: User description: "2nd feature is mouse control mode, with configurable key. The default setting is wasd move mouse, ijkl scroll updown left right, left shift left click, right shift right click, and hold/type mode to toggle mouse speed in 2 mode. constant slow and constant fast. Default is normal speed"

## Clarifications

### Session 2026-07-10

- Q: How should mouse control mode interact with other Vimium modes and system keyboard input? → A: Mouse control mode is fully isolated. When active, all keyboard input is consumed by the mode — no typing or key commands pass through to applications or other Vimium features. This is a "last resort" navigation mode for special use cases where element mode and text selection mode cannot operate (e.g., Windows Snipping Tool overlay, drag-and-drop operations).
- Q: Should mouse control mode support drag-and-drop, and what interaction model? → A: Press-and-hold model. Holding left Shift keeps the left mouse button pressed; releasing releases it. Quick tap performs a normal click. Same for right Shift. Drag by holding Shift + moving with WASD, then releasing to drop.
- Q: Should mouse control mode auto-exit after inactivity? → A: Yes, auto-exit after 30 seconds of no mouse control input (no movement, clicks, or scroll). Visual indicator warns at 10 seconds remaining before exit.
- Q: What is the default activation hotkey for mouse control mode? → A: `Ctrl+/`
- Q: Where should the visual indicator for active mouse control mode be displayed? → A: Both cursor-attached and bottom-screen banner. A small indicator near the cursor for immediate awareness, plus a thin status banner at the bottom of the screen showing supplementary info (current speed mode, auto-exit countdown).
- Q: What is the default key for toggling mouse speed modes? → A: `Space`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Control Mouse Cursor with Keyboard (Priority: P1)

A user wants to move the mouse cursor and perform clicks entirely from the keyboard, without touching the physical mouse, to reduce hand movement and improve ergonomics. This mode is a fully isolated "last resort" input mode — when active, all keyboard input is consumed and does not reach applications. It is intended for special use cases where Vimium's element mode and text selection mode cannot operate (e.g., Windows Snipping Tool overlay, drag-and-drop scenarios, or inaccessible custom UI).

**Why this priority**: This is the core interaction for the mouse control feature. Without cursor movement, the mode has no value. It is the largest user-facing addition in v1.4 and the primary reason users will upgrade.

**Independent Test**: Activate mouse control mode via its hotkey, press W/A/S/D keys, and observe the mouse cursor moves up/left/down/right on screen. Press left Shift and verify a left-click is performed at the cursor position. Press right Shift and verify a right-click is performed. Press Escape to exit the mode.

**Acceptance Scenarios**:

1. **Given** Vimium is running in the system tray, **When** the user presses `Ctrl+/`, **Then** mouse control mode activates with a cursor-attached indicator and a bottom-screen status banner confirming entry.
2. **Given** mouse control mode is active, **When** the user presses W, **Then** the mouse cursor moves upward by a fixed number of pixels at the current speed.
3. **Given** mouse control mode is active, **When** the user presses A, **Then** the mouse cursor moves left by a fixed number of pixels at the current speed.
4. **Given** mouse control mode is active, **When** the user presses S, **Then** the mouse cursor moves downward by a fixed number of pixels at the current speed.
5. **Given** mouse control mode is active, **When** the user presses D, **Then** the mouse cursor moves right by a fixed number of pixels at the current speed.
6. **Given** mouse control mode is active, **When** the user presses and quickly releases left Shift, **Then** a left mouse click is performed at the current cursor position.
7. **Given** mouse control mode is active, **When** the user holds left Shift, moves the cursor with WASD, then releases left Shift, **Then** a drag-and-drop operation is performed (left button held during movement, released at drop point).
8. **Given** mouse control mode is active, **When** the user presses and quickly releases right Shift, **Then** a right mouse click is performed at the current cursor position.
9. **Given** mouse control mode is active, **When** the user presses Escape, **Then** mouse control mode exits and normal keyboard operation resumes.
10. **Given** mouse control mode is active, **When** the cursor reaches any screen edge, **Then** further movement in that direction stops at the edge (no wrapping or error).
11. **Given** mouse control mode is active and no input has occurred for 20 seconds, **Then** a visual warning appears indicating imminent auto-exit (10 seconds remaining); **When** 10 further seconds elapse without input (30 seconds total inactivity), **Then** the mode automatically exits.

---

### User Story 2 - Scroll Content with Keyboard (Priority: P2)

A user wants to scroll window content (vertically and horizontally) using keyboard keys while in mouse control mode, enabling full navigation without the mouse scroll wheel.

**Why this priority**: Scrolling is a fundamental navigation action. Without it, the mouse control mode is incomplete — users would still need the mouse wheel for reading documents, web pages, and long forms. It builds on the same mode infrastructure from Story 1.

**Independent Test**: Activate mouse control mode, press I/J/K/L keys, and verify the active window scrolls up/left/down/right accordingly.

**Acceptance Scenarios**:

1. **Given** mouse control mode is active and a scrollable window is in the foreground, **When** the user presses I, **Then** the window scrolls upward.
2. **Given** mouse control mode is active and a scrollable window is in the foreground, **When** the user presses K, **Then** the window scrolls downward.
3. **Given** mouse control mode is active and a scrollable window is in the foreground, **When** the user presses J, **Then** the window scrolls left.
4. **Given** mouse control mode is active and a scrollable window is in the foreground, **When** the user presses L, **Then** the window scrolls right.
5. **Given** mouse control mode is active and the foreground window has no scrollable content, **When** the user presses any scroll key, **Then** no error occurs and the application continues to function normally.

---

### User Story 3 - Toggle Mouse Movement Speed (Priority: P2)

A user wants to switch between normal, slow, and fast cursor movement speeds to handle both precise clicking (slow) and rapid cross-screen navigation (fast).

**Why this priority**: Speed control separates a usable mouse replacement from a toy. Normal speed works for most tasks, but precision work (small UI elements) requires slow mode, and multi-monitor setups benefit from fast mode. It is a quality-of-life multiplier on top of Story 2.

**Independent Test**: Activate mouse control mode (defaults to normal speed), press `Space` once to enter slow mode and verify cursor moves in smaller increments, press again for fast mode and verify larger increments, press again to return to normal speed.

**Acceptance Scenarios**:

1. **Given** mouse control mode is first activated, **When** the user moves the cursor, **Then** it moves at the default "normal" speed.
2. **Given** mouse control mode is active at normal speed, **When** the user presses `Space`, **Then** the mode switches to "slow" speed and cursor movement increments decrease noticeably.
3. **Given** mouse control mode is active at slow speed, **When** the user presses `Space`, **Then** the mode switches to "fast" speed and cursor movement increments increase noticeably.
4. **Given** mouse control mode is active at fast speed, **When** the user presses `Space`, **Then** the mode cycles back to "normal" speed.
5. **Given** any speed mode is active, **When** the speed changes, **Then** a brief visual indicator communicates the new speed to the user.

---

### User Story 4 - Configure Mouse Control Key Bindings (Priority: P3)

A user wants to customize the keys used for mouse control to match their preferences or keyboard layout (e.g., Dvorak, AZERTY, or personal ergonomic setup).

**Why this priority**: Configurability is a core Vimium principle (all hotkeys are user-configurable). However, the default bindings cover the majority of users, and customization can be added as a follow-up within the same release cycle. The mode must work with defaults before customization matters.

**Independent Test**: Open Vimium settings, locate the mouse control key binding section, change the "move up" key from W to a different key, save, activate mouse control mode, and verify the new key moves the cursor up.

**Acceptance Scenarios**:

1. **Given** the user opens the settings window, **When** they navigate to the mouse control section, **Then** all configurable keys are displayed with their current bindings.
2. **Given** the user is viewing mouse control settings, **When** they change a key binding and save, **Then** the new binding takes effect immediately in mouse control mode.
3. **Given** the user has customized key bindings, **When** they reset to defaults, **Then** all bindings revert to WASD/IJKL/Shift defaults.
4. **Given** the user attempts to assign the same key to two different mouse actions, **When** the conflict is detected, **Then** a warning is shown and the previous binding is restored for the conflicting action.

---

### Edge Cases

- **Multi-monitor setups**: Mouse cursor must move freely across all connected displays. Scroll actions apply to the window under the cursor, regardless of which monitor it's on.
- **Hotkey conflicts**: If the mouse control mode activation hotkey conflicts with an existing Vimium hotkey (element mode, text selection mode), the conflicting binding must be rejected or the user warned. Mouse control internal keys (WASD/IJKL) are only active within mouse control mode and do not conflict with global hotkeys.
- **Mouse control mode and other overlays**: If element mode (`Ctrl+;`) or text selection mode (`Ctrl+.`) is activated while mouse control mode is active, mouse control mode exits first (only one modality active at a time, per constitution).
- **Rapid key presses in mouse mode**: Holding a movement key should produce smooth, repeated cursor movement (key repeat behavior), not a single step.
- **Non-QWERTY keyboard layouts**: Default key bindings (WASD/IJKL) are based on physical key positions, not character output. Users on non-QWERTY layouts can remap via Story 4.
- **Screen DPI scaling**: Cursor movement increments must account for Windows DPI scaling — based on the DPI of the monitor currently under the cursor — so that movement feels consistent across 100%, 125%, 150%, and 200% scaling configurations, including mixed-DPI multi-monitor setups.
- **User forgets they are in mouse control mode**: Mitigated by the dual-indicator design (cursor-attached element always in line of sight + bottom-screen banner with mode info and auto-exit countdown). Both indicators are persistent while the mode is active. As a further safety net, the mode auto-exits after 30 seconds of inactivity with a 10-second warning.
- **Drag-and-drop scenarios**: Mouse control mode supports drag-and-drop via press-and-hold on the click keys. Holding left/right Shift keeps the button pressed while the user moves the cursor; releasing Shift releases the button to complete the drop. This covers file dragging, window resizing, and text selection via mouse.

## Requirements *(mandatory)*

### Functional Requirements

**Mouse Control Mode Activation**

- **FR-001**: The application MUST provide a user-configurable hotkey to activate and deactivate mouse control mode. The default hotkey is `Ctrl+/`.
- **FR-002**: Activating mouse control mode MUST provide immediate visual feedback to the user via two indicators: (a) a small cursor-attached indicator (e.g., colored dot or icon near the pointer) for immediate in-line-of-sight awareness, and (b) a thin status banner at the bottom of the screen showing the current speed mode and, when applicable, the auto-exit countdown.
- **FR-003**: Mouse control mode MUST be deactivatable by pressing the Escape key.
- **FR-004**: Mouse control mode MUST be deactivatable by pressing the same hotkey used to activate it (toggle behavior).
- **FR-005**: Only one interaction mode (element, text selection, mouse control) may be active at a time. Activating mouse control mode while another mode is active MUST exit the previous mode.
- **FR-006**: While mouse control mode is active, ALL keyboard input MUST be consumed by the mode. No keystrokes (including typing characters, shortcuts, or other Vimium hotkeys except the explicit exit mechanisms defined in FR-003 and FR-004) may pass through to the operating system or active application.
- **FR-007**: Mouse control mode MUST automatically exit after 30 seconds of inactivity (no cursor movement, clicks, or scroll actions). A visual warning MUST appear at 10 seconds before auto-exit, giving the user time to cancel by pressing any mouse control key.

**Mouse Cursor Movement**

- **FR-008**: While mouse control mode is active, the W key MUST move the cursor upward (default binding).
- **FR-009**: While mouse control mode is active, the A key MUST move the cursor left (default binding).
- **FR-010**: While mouse control mode is active, the S key MUST move the cursor downward (default binding).
- **FR-011**: While mouse control mode is active, the D key MUST move the cursor right (default binding).
- **FR-012**: Holding a movement key MUST produce repeated cursor movement (key repeat), not a single step.
- **FR-013**: Cursor movement MUST stop at screen boundaries on all connected displays.

**Mouse Clicks & Drag**

- **FR-014**: While mouse control mode is active, pressing and releasing the left Shift key quickly MUST perform a left mouse click at the current cursor position (default binding).
- **FR-015**: Holding the left Shift key MUST press and hold the left mouse button; releasing left Shift MUST release the button. This enables drag-and-drop when combined with cursor movement keys.
- **FR-016**: While mouse control mode is active, pressing and releasing the right Shift key quickly MUST perform a right mouse click at the current cursor position (default binding).
- **FR-017**: Holding the right Shift key MUST press and hold the right mouse button; releasing right Shift MUST release the button.

**Scrolling**

- **FR-018**: While mouse control mode is active, the I key MUST scroll upward (default binding).
- **FR-019**: While mouse control mode is active, the K key MUST scroll downward (default binding).
- **FR-020**: While mouse control mode is active, the J key MUST scroll left (default binding).
- **FR-021**: While mouse control mode is active, the L key MUST scroll right (default binding).

**Mouse Speed Toggle**

- **FR-022**: The application MUST provide a configurable key to cycle through mouse cursor speed modes. The default key is `Space`.
- **FR-023**: Three speed modes MUST be available: normal (default), slow, and fast.
- **FR-024**: The slow speed mode MUST move the cursor in smaller pixel increments than normal.
- **FR-025**: The fast speed mode MUST move the cursor in larger pixel increments than normal.
- **FR-026**: Speed mode changes MUST produce a brief visual indication of the new speed (e.g., the status banner updates to show the current speed mode).
- **FR-027**: The speed mode MUST persist only for the duration of the current mouse control session and reset to normal on next activation.

**Key Binding Configuration**

- **FR-028**: All mouse control keys (movement, clicks, scroll, speed toggle, activation hotkey) MUST be user-configurable through the settings window.
- **FR-029**: The settings window MUST display all mouse control key bindings with their current assignments.
- **FR-030**: Users MUST be able to restore all mouse control key bindings to their default values with a single action.
- **FR-031**: The system MUST detect and warn the user about conflicting key assignments within mouse control bindings.
- **FR-032**: The system MUST detect and reject (or warn about) an activation hotkey assignment that conflicts with another Vimium interaction mode's hotkey (element mode, text selection mode) or another registered global hotkey, before the binding takes effect.

**Scrolling & Multi-Monitor Behavior**

- **FR-033**: Scroll actions MUST apply to the window currently under the cursor, regardless of which monitor it is on. The cursor MUST be able to move freely across all connected displays, stopping only at the outer boundary of the combined desktop (consistent with FR-013).

### Key Entities *(include if feature involves data)*

- **MouseControlConfiguration**: Stores the user's key bindings for all mouse control actions (move up/down/left/right, scroll up/down/left/right, left click, right click, speed toggle, mode activation). Persisted to the application config file. Includes the speed mode step sizes (pixel increments for slow, normal, fast), the scroll amount per key press, and the inactivity auto-exit and warning timer values.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can activate mouse control mode and begin moving the cursor within 1 second of pressing the activation hotkey.
- **SC-002**: Mouse cursor movement responds to key presses within 50ms (imperceptible lag), matching the feel of physical mouse movement for the user.
- **SC-003**: Users can perform a complete mouse task (move to target, left-click, move to second target, right-click) without touching the physical mouse.
- **SC-004**: Scrolling via keyboard responds within 100ms of key press in the target window.
- **SC-005**: 90% of users can successfully switch between all three speed modes and identify the current mode from the provided feedback on first use.
- **SC-006**: Users can customize mouse control key bindings and see the changes take effect immediately without requiring an application restart.
- **SC-007**: Conflicting key assignments are detected and warned before they cause runtime errors; 100% of conflicts are caught during configuration, not during mouse mode usage.

## Assumptions

- **Mouse control activation hotkey**: Default is `Ctrl+/`. This does not conflict with existing element mode (`Ctrl+;`) or text selection mode (`Ctrl+.`). Users can reconfigure it in settings.
- **Speed mode values**: The exact pixel increments for slow, normal, and fast speeds will be determined during implementation based on common screen resolutions. Reasonable defaults: slow ≈ 5px, normal ≈ 15px, fast ≈ 50px per key press.
- **Key repeat behavior**: Mouse control movement keys leverage the existing keyboard hook infrastructure already used by Vimium for global hotkey detection.
- **Scrolling mechanism**: Scrolling is performed by sending scroll wheel messages (`WM_MOUSEWHEEL` / `WM_MOUSEHWHEEL`) to the window under the cursor, consistent with how physical mouse scroll wheels operate.
- **Mouse click simulation**: Left and right clicks are performed using Win32 `SendInput` or `mouse_event` APIs, which are already available in the `NativeMethods` project.
- **Settings persistence**: Mouse control key bindings and speed values are stored in the existing `%APPDATA%\Vimium\config.json` file under a new `mouseControl` key, using the existing `System.Text.Json` configuration infrastructure.
- **DPI awareness**: The application already declares DPI awareness in its manifest. Mouse movement increments will be scaled according to the DPI of the monitor currently under the cursor, so movement feels consistent across mixed-DPI multi-monitor setups and all scaling configurations (100%–200%).
- **Single-session speed state**: Speed mode resets to normal each time mouse control mode is activated, keeping the behavior predictable. Users who prefer a different default speed can configure the normal speed step size instead.
