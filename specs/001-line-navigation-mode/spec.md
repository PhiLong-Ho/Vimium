# Feature Specification: Text Selection Mode

**Feature Branch**: `001-line-navigation-mode`

**Created**: 2026-07-05 | **Revised**: 2026-07-09

**Status**: Redesigned

**Input**: User description: "I want to mimic mouse text selection as closely as possible. When using a mouse, the user moves to text they see, highlights it, and copies with Ctrl+C. They can also refine a small highlight using arrow keys and Shift."

## Redesign Rationale (2026-07-09)

The original design labeled every visible text line with hint labels (like element mode). This proved unreliable across apps and created visual overlap with element mode. The redesign replaces hint labels with a **search-first** approach that directly mimics mouse-driven text selection:

| Mouse action | Keyboard equivalent |
|-------------|-------------------|
| Move cursor to text you see | Type search phrase → match found and highlighted |
| Click to position | Tab/Shift+Tab cycle through matches |
| Click-drag to adjust selection | Shift+Arrow extend/shrink from match position |
| Ctrl+C to copy | Enter to copy |

## User Scenarios & Testing

### User Story 1 - Activate Text Selection Mode (Priority: P1)

As a keyboard-first user, I want to activate text selection mode with a hotkey and see a search interface, so I can find and select text anywhere on screen without using the mouse.

**Acceptance Scenarios**:

1. **Given** Vimium is running and a window with visible text is focused, **When** the user presses the text-selection hotkey (`Ctrl+.`), **Then** a text selection overlay appears with a search bar, and the underlying text is accessible for finding and copying.
2. **Given** Vimium is running, **When** the user presses the existing element-mode hotkey (`Ctrl+;`), **Then** the existing element-navigation overlay appears (unchanged behavior).
3. **Given** the text selection overlay is visible, **When** the user presses Escape, **Then** the overlay is dismissed and nothing is copied.

---

### User Story 2 - Find Text by Search (Priority: P1)

As a keyboard-first user, I want to type a phrase I can see on screen and have it highlighted, so I can position the text cursor to the right location — just like moving the mouse to the text I want.

**Acceptance Scenarios**:

1. **Given** the text selection overlay is visible with a search bar, **When** the user types a search phrase (e.g., "Singapore"), **Then** all occurrences of that phrase across the visible text are highlighted (find-in-page style).
2. **Given** matches exist for the current search phrase, **When** the user presses Tab, **Then** the active match advances to the next occurrence. Shift+Tab cycles backward. Cycling wraps circularly.
3. **Given** a search phrase with zero matches, **When** the user continues typing or presses Escape, **Then** no text is highlighted and the overlay remains active.
4. **Given** the search bar has text, **When** the user presses Backspace, **Then** the last character is removed and matches update accordingly.

---

### User Story 3 - Select and Copy Text (Priority: P1)

As a keyboard-first user, I want to refine my selection from the search match position using arrow keys (like adjusting a mouse drag), and copy the selection to clipboard, so I can capture exactly the text I need.

**Acceptance Scenarios**:

1. **Given** a search match is active, **When** the user presses Arrow keys (`←`/`→`), **Then** the cursor moves character by character relative to the match position and no text is selected.
2. **Given** a cursor position is set, **When** the user holds Shift and presses Arrow keys, **Then** text is selected from the original cursor position to the new position.
3. **Given** the user has moved the cursor with arrows, **When** they press Ctrl+Arrow (`Ctrl+←`/`Ctrl+→`), **Then** the cursor jumps by word boundaries.
4. **Given** a selected text range or an active cursor position, **When** the user presses Enter, **Then** the selected text (or all text in the current text block if no selection) is copied to the system clipboard and the overlay closes.
5. **Given** the text selection overlay is visible, **When** the user presses Escape, **Then** the overlay closes without copying anything to the clipboard.

---

### User Story 4 - Configure Text Selection Mode (Priority: P2)

As a user, I want to configure the text-selection hotkey through the Options window, so I can tailor Vimium to my workflow.

**Acceptance Scenarios**:

1. **Given** the user opens Options → Keyboard, **When** they see the "Text Selection" hotkey field, **Then** they can change it and the new hotkey takes effect immediately.
2. **Given** the user changes the text-selection hotkey, **When** they press the old hotkey, **Then** it no longer activates text selection mode.

---

### User Story 5 - Toggle Between Element and Text Modes (Priority: P2)

As a user, I want to seamlessly switch between element mode (`Ctrl+;`) and text selection mode (`Ctrl+.`) without opening settings, so both interaction styles are always available.

**Acceptance Scenarios**:

1. **Given** the element overlay is visible, **When** the user dismisses it and presses the text-selection hotkey, **Then** the text selection overlay appears.
2. **Given** the text selection overlay is visible, **When** the user dismisses it and presses the element hotkey, **Then** the element overlay appears.

---

## Edge Cases

- What happens when the foreground window has no accessible text? The overlay shows "No text found" and auto-dismisses after 1.5s.
- What happens with very long text content? Search is limited to the first 50,000 characters from the text source.
- What happens when the user types non-searchable characters? Only printable characters update the search query. Arrow, Tab, Enter, and Escape have their own behaviors.
- How does position mapping work when per-character positions aren't available from UIA? Character positions are estimated from line bounding rectangles and average character width.
- What happens when Tab cycles past the last match? It wraps to the first match (circular cycling).

## Requirements

### Functional Requirements

- **FR-001**: System MUST provide a distinct text-selection activation hotkey (default: `Ctrl+.`).
- **FR-002**: System MUST discover visible text content from the foreground window using UI Automation (TextPattern or ValuePattern).
- **FR-003**: System MUST display a search bar overlay over the foreground window when text selection mode is activated.
- **FR-004**: System MUST highlight all occurrences of the search phrase across the visible text.
- **FR-005**: System MUST support cycling through search matches with Tab (forward) and Shift+Tab (backward), with circular wrapping.
- **FR-006**: System MUST support cursor movement using Arrow keys (character) and Ctrl+Arrow (word).
- **FR-007**: System MUST support text selection from the cursor position using Shift+Arrow (character) and Ctrl+Shift+Arrow (word).
- **FR-008**: System MUST copy the selected text to the clipboard when Enter is pressed. If no explicit selection, copies all text from the current text block.
- **FR-009**: System MUST dismiss the overlay without copying when Escape is pressed.
- **FR-010**: System MUST provide visual feedback when text is copied (brief "Copied!" toast).
- **FR-011**: System MUST allow configuration of the text-selection hotkey through Options → Keyboard.
- **FR-012**: System MUST persist configuration in `%APPDATA%\Vimium\config.json`.
- **FR-013**: System MUST NOT steal keyboard focus (overlay uses `WS_EX_TRANSPARENT`).
- **FR-014**: System MUST support the same visual theme (colors, fonts) as element mode.

### Key Entities

- **TextSource**: The text content and line-level position data extracted from the foreground window. Contains the full visible text string and per-line bounding rectangles for cursor/highlight positioning.
- **SelectionState**: The mutable state of a text selection operation — cursor position, selection range, search query, search matches, and active match index.
- **SearchMatch**: A single occurrence of the search phrase within the visible text. Contains start/end offsets and the line index for rendering highlights.

## Success Criteria

- **SC-001**: Text selection overlay appears within 100ms of hotkey activation.
- **SC-002**: Text content extraction completes within 500ms for typical windows (up to 100 text blocks).
- **SC-003**: Users can find and copy a visible text phrase within 5 seconds of activating the mode.
- **SC-004**: Search match highlighting updates within 100ms of each keystroke.
- **SC-005**: Element mode (`Ctrl+;`) continues to work identically — zero regressions.
- **SC-006**: Text selection works on Chrome, Firefox, Notepad, and VS Code.

## Assumptions

- Text discovery relies on UI Automation (TextPattern or ValuePattern). Apps that expose neither will show "No text found."
- The default hotkey is `Ctrl+.` (adjacent to `Ctrl+;` for element mode).
- Per-character position estimation uses average character width derived from line bounding rects. This is approximate but sufficient for cursor and highlight positioning.
- Selection mode operates on the foreground window only. Cross-window selection is out of scope.
- Users are familiar with standard Windows text navigation keys.
