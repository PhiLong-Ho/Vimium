# Feature Specification: Find-and-Navigate Text Mode (Ctrl+F Style)

**Feature Branch**: `001-line-navigation-mode`

**Created**: 2026-07-05 | **Revised**: 2026-07-09

**Status**: Redesigned — Simplified

**Input**: User description: "The previous agent provided an implementation that doesn't work. I want to modify how I use my feature, by searching first, then move from element to element with tab or shift tab. When I press enter the cursor should highlight the element and close overlay. The idea is the same as Ctrl+F search in Chrome. I want to use it to navigate to text. This will reduce the massive search time for loading all text."

## Redesign Rationale (2026-07-09)

**Problem**: The original approach loaded all visible text from the foreground window before searching, causing massive delays. Users couldn't navigate to text quickly.

**Solution**: Adopt a Chrome Ctrl+F-style interaction model: the user types a search query, the system finds matching text directly (without loading all text), the user cycles through matches with Tab/Shift+Tab, and presses Enter to navigate the cursor to the match and close the overlay. This drastically reduces search time by only finding matching text.

**What changed from the previous revision**:
- **REMOVED**: Text selection via Shift+Arrow keys. This is a navigation feature, not a text editor.
- **REMOVED**: Copy to clipboard via Enter. Enter now navigates the cursor to the match.
- **REMOVED**: Arrow-only cursor movement within text. Cursor movement is now driven by search matches.
- **CHANGED**: Text retrieval strategy — from "load all text then search client-side" to "search with query, return only matches."
- **KEPT**: Search-first approach, overlay with search bar, Tab/Shift+Tab cycling, Escape to cancel, Ctrl+. hotkey.

## Clarifications

### Session 2026-07-09 (Redesign)

- Q: What does Enter do? → A: Enter navigates the system cursor/focus to the active match and closes the overlay. In editable text contexts (text editors, input fields), this means positioning the text cursor at the match position. In non-editable contexts (web pages, labels), this means focusing the containing element and scrolling it into view.
- Q: Should the overlay have a "Copied!" toast? → A: No. Copy functionality is removed from this feature. The overlay closes immediately on Enter with no toast.
- Q: What happens when there are no matches for the search query? → A: The search bar shows "0 matches" with no highlights. Tab has no effect. The user can continue typing or press Escape to dismiss.
- Q: How does text discovery work? → A: Use UIA TextPattern.FindText (or equivalent query-driven API) to search for matches directly, rather than loading all text first. Element names from UIA cache can supplement results. OCR may serve as fallback for apps without UIA text support.
- Q: Window focus change or text content change during active overlay? → A: Auto-dismiss immediately and silently. No toast/feedback.
- Q: Search is case-sensitive or case-insensitive? → A: Case-insensitive by default. This matches Chrome's Ctrl+F behavior.
- Q: What happens if the user rapidly types and deletes text? Could this crash the system? What is the debounce plan? → A: Debounce with 150ms delay after last keystroke before searching. Require minimum 5 characters before triggering any search. Cancel any in-flight UIA search when a new keystroke arrives. This prevents flooding UIA with rapid-fire cross-process COM calls and avoids single-short-character queries that would return massive (and useless) result sets.
- Q: What about text out of range? What is more performance-friendly — search all text or only visible range? → A: Search only text within the visible viewport (foreground window bounds). Offscreen/scrolled-out text is excluded from matching. This keeps UIA search fast, render costs low, and result counts naturally bounded by screen real estate. Combined with the 5-char minimum, a Wikipedia page search returns only the ~5–15 visible matches, not 950.
- Q: What is the search timeout and fallback when TextPattern is unavailable or slow? → A: 3-second timeout on TextPattern.FindText loop. If it times out or TextPattern is not supported by the target app, fall back to searching cached element names via FindAllBuildCache (fast, always available). If both fail, show "No text found" and auto-dismiss. No error message shown to user.
- Q: Are the available UIA text APIs sufficient for this approach? → A: Yes. The approach uses `ITextProvider.GetVisibleRanges()` to scope search to the visible viewport, `ITextRangeProvider.FindText()` in a loop to collect all matches (one cross-process call per match, bounded by the 200-match cap), and `ITextRangeProvider.ScrollIntoView()` + `Select()` on Enter to navigate the cursor to the match. Element names from `FindAllBuildCache` with `CacheRequest` serve as the fast fallback path.

## User Scenarios & Testing

### User Story 1 - Find and Navigate to Text (Priority: P1)

As a keyboard-first user, I want to search for visible text on screen, cycle through matches, and navigate my cursor to a match — just like using Ctrl+F in Chrome — so I can quickly jump to any text I see without using the mouse.

**Why this priority**: This is the entire feature. Without it, nothing else matters.

**Independent Test**: Can be fully tested by activating the mode, typing a search phrase, cycling matches with Tab, pressing Enter to navigate, and verifying the cursor moved to the correct location. Delivers immediate value — the user can find and jump to text anywhere on screen.

**Acceptance Scenarios**:

1. **Given** Vimium is running and a window with visible text is focused, **When** the user presses the find-text hotkey (`Ctrl+.`), **Then** a search bar overlay appears over the foreground window.
2. **Given** the search bar overlay is visible, **When** the user types a search phrase (e.g., "Singapore"), **Then** all visible occurrences of that phrase are highlighted yellow, and the first match is highlighted orange (active match).
3. **Given** matches exist for the current search phrase, **When** the user presses Tab, **Then** the active match advances to the next occurrence (highlighted orange), and Tab wraps from the last match back to the first.
4. **Given** matches exist for the current search phrase, **When** the user presses Shift+Tab, **Then** the active match moves to the previous occurrence, and Shift+Tab wraps from the first match back to the last.
5. **Given** an active match is highlighted, **When** the user presses Enter, **Then** the system cursor/focus navigates to that match location, and the overlay closes immediately with no toast.
6. **Given** the search bar has text with no matches, **When** the user views the overlay, **Then** "0 matches" is displayed, no highlights appear, and Tab has no effect.
7. **Given** the search bar has text, **When** the user presses Backspace, **Then** the last character is removed and matches update accordingly.
8. **Given** the overlay is visible, **When** the user presses Escape, **Then** the overlay closes without any navigation action.

---

### User Story 2 - Activate and Configure Find-Text Mode (Priority: P2)

As a user, I want to activate find-text mode via a configurable hotkey and configure it through the Options window.

**Why this priority**: The feature must be invocable and configurable, but the core interaction (P1) delivers the primary value.

**Independent Test**: Can be tested by changing the hotkey in Options → Keyboard and verifying the new hotkey activates the mode and the old one no longer does.

**Acceptance Scenarios**:

1. **Given** Vimium is running, **When** the user presses the default find-text hotkey (`Ctrl+.`), **Then** the find-text overlay appears.
2. **Given** the element-mode hotkey (`Ctrl+;`) is pressed, **When** the find-text hotkey is also pressed, **Then** each activates its respective mode independently. Only one overlay is visible at a time.
3. **Given** the user opens Options → Keyboard, **When** they change the "Find Text" hotkey, **Then** the new hotkey takes effect immediately and the old one no longer activates find-text mode.

---

### User Story 3 - Fast Performance with Large Text (Priority: P1)

As a user working with large documents or busy UIs, I want the find-text feature to respond quickly without loading all visible text, so I can navigate to text instantly even in content-heavy windows.

**Why this priority**: Performance is the primary motivation for the redesign. The previous approach (loading all text) was too slow.

**Independent Test**: Can be tested by activating find-text mode on a window with thousands of text elements and verifying that typing a search query returns matches in under 200ms without a loading spinner.

**Acceptance Scenarios**:

1. **Given** a foreground window with 5,000+ visible text elements, **When** the user activates find-text mode and types a 5-character search query (meeting the minimum character threshold), **Then** the first matches appear highlighted within 200ms of the debounce delay completing.
2. **Given** the find-text overlay is active, **When** the user modifies the search query, **Then** match highlights update without visible delay (under 200ms per keystroke).

---

## Edge Cases

- What happens when the foreground window has no accessible text? The overlay shows "No text found" and auto-dismisses after 2 seconds.
- What happens when there are zero matches for the search query? The search bar displays "0 matches" with no highlights. Tab has no effect. The user can modify the query or press Escape.
- What happens with very long search queries? The search bar input is capped at 200 characters.
- What happens when Tab cycles past the last match? It wraps circularly to the first match. Same for Shift+Tab wrapping from first to last.
- What happens when the foreground window changes (Alt+Tab) while the overlay is active? The overlay auto-dismisses immediately and silently.
- What happens when the underlying text content changes (e.g., page scrolls, new content loads) while the overlay is active? The overlay auto-dismisses immediately and silently. Stale matches are misleading.
- What happens when the user types non-printable characters? Only printable characters update the search query. Arrow keys, Tab, Enter, and Escape have their own defined behaviors.
- What happens when the user rapidly types and deletes characters? A 150ms debounce delays search execution until typing pauses. In-flight searches are cancelled when a new keystroke arrives. Queries shorter than 5 characters do not trigger a search at all. This prevents flooding UIA with rapid-fire cross-process COM calls and avoids pointless single-character result sets.
- What happens when multiple search results overlap (e.g., "aa" in "aaa")? Each distinct occurrence is a separate match. Tab cycles through all of them.
- What happens with hidden/offscreen text matches? Only text visible within the foreground window viewport is searched and matched. Offscreen/scrolled-out text is excluded entirely — this keeps search fast and result counts naturally bounded. Users who need to find scrolled-out text should scroll first, then search.
- What happens when Enter is pressed on a match in a non-editable context (web page, label)? The containing element receives focus. If the element supports it, it's scrolled into view.
- What happens if navigation (Enter) fails (e.g., element is no longer valid)? The overlay closes silently. No error is shown to the user.

## Requirements

### Functional Requirements

- **FR-001**: System MUST provide a distinct find-text activation hotkey (default: `Ctrl+.`), configurable via Options → Keyboard.
- **FR-002**: System MUST display a search bar overlay positioned over the foreground window when find-text mode is activated. The overlay MUST NOT steal keyboard focus (uses `WS_EX_TRANSPARENT`).
- **FR-003**: System MUST discover matching text within the visible viewport of the foreground window using a query-driven approach — searching for the user's input directly via UIA TextPattern.FindText (or equivalent) scoped to the visible bounds, without first loading all text into memory. Offscreen/scrolled-out text is excluded. Element names from UIA cache MAY supplement results as a fast secondary source.
- **FR-004**: System MUST highlight all matching occurrences of the search phrase on screen. All matches are highlighted in yellow. The currently active match is highlighted in orange.
- **FR-005**: System MUST support cycling through search matches with Tab (next match) and Shift+Tab (previous match), with circular wrapping in both directions.
- **FR-006**: System MUST, when Enter is pressed, navigate the system cursor/focus to the active match location and close the overlay immediately with no toast or confirmation message.
- **FR-007**: System MUST dismiss the overlay without any action when Escape is pressed.
- **FR-008**: System MUST display the current match count (e.g., "2 of 5") in the search bar.
- **FR-009**: System MUST auto-dismiss the overlay immediately and silently when the foreground window changes or the underlying text content changes.
- **FR-010**: System MUST support the same visual theme (colors, fonts) as element mode.
- **FR-011**: System MUST persist the find-text hotkey configuration in `%APPDATA%\Vimium\config.json`.
- **FR-012**: System MUST handle the "no matches" state gracefully — display "0 matches" with no highlights and ignore Tab/Enter inputs.
- **FR-013**: System MUST debounce search input with a 150ms delay after the last keystroke before executing a UIA search. System MUST cancel any in-flight search when a new keystroke arrives. System MUST require a minimum of 5 characters before triggering a search.
- **FR-014**: System MUST enforce a 3-second timeout on text search operations. If the primary search path times out or is unsupported by the target application, system MUST fall back to searching cached element names (fast, always available). If both paths fail, system MUST show "No text found" and auto-dismiss after 2 seconds. No error message is shown to the user.
- **FR-015**: System MUST, on Enter, scroll the active match into view (if partially obscured) and position the text cursor at the match location before closing the overlay.

### Key Entities

- **SearchMatch**: A single occurrence of the search phrase within the visible text of the foreground window. Contains the matched text, its bounding rectangle (for highlight rendering), and its source element (for cursor navigation on Enter).
- **FindSession**: The state of an active find-text operation — search query, list of matches, active match index, and reference to the foreground window handle.

## Success Criteria

- **SC-001**: Find-text overlay appears within 100ms of hotkey activation.
- **SC-002**: Search results (match highlights) appear within 200ms of each keystroke in the search bar, even on windows with 5,000+ visible text elements.
- **SC-003**: Users can find and navigate to a visible text phrase within 3 seconds of activating the mode (measured from hotkey press to cursor repositioned at match).
- **SC-004**: Tab cycling between matches updates the active highlight within 50ms (instant to human perception).
- **SC-005**: Element mode (`Ctrl+;`) continues to work identically — zero regressions.
- **SC-006**: Find-text mode works on Chrome, Firefox, Notepad, VS Code, Windows Explorer, and any application with visible text — using UIA TextPattern as the primary path with element-name search as a fast secondary path.
- **SC-007**: Find-text mode does NOT load all visible text into memory before searching. Memory usage is proportional to the number of search matches, not the total text content on screen.

## Assumptions

- Text discovery uses `ITextProvider.GetVisibleRanges()` to scope search to the visible viewport, then `ITextRangeProvider.FindText()` in a loop to collect all matches. Each FindText call is a cross-process COM operation — performance depends on match count, not total text size. On Enter, `ITextRangeProvider.ScrollIntoView()` then `Select()` navigates the cursor to the match. Element names from `FindAllBuildCache` with `CacheRequest` serve as the fast fallback (always available, ~50ms). TextPattern search has a 3-second timeout.
- Windows built-in OCR (Windows.Media.Ocr) is NOT used for this feature. UIA TextPattern is faster and more accurate for text search; element-name fallback handles non-TextPattern apps.
- The default hotkey is `Ctrl+.` (adjacent to `Ctrl+;` for element mode).
- Search is case-insensitive by default, matching Chrome's Ctrl+F behavior.
- Find-text mode operates on the foreground window only. Cross-window search is out of scope.
- Cursor navigation on Enter means: for editable text (TextPattern-supporting elements), position the text cursor at the match offset; for non-editable elements, focus the element and scroll it into view.
- Maximum match count per search is 200 to bound rendering and memory costs. Since search is scoped to the visible viewport only, 200 is more than sufficient for any realistic screen density. If more matches exist within the viewport, only the first 200 are shown.
- The search bar has a maximum input length of 200 characters.
- Search triggers only after 5+ characters are typed and a 150ms pause since the last keystroke. In-flight UIA searches are cancelled on new input.
