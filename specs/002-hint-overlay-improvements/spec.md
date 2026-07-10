# Feature Specification: Hint Overlay Improvements

**Feature Branch**: `002-hint-overlay-improvements`

**Created**: 2026-07-10

**Status**: Draft

**Input**: User description: "I want to improve current app performance. Compare to vimium which return almost instantly, our app take 1s to 3s to show hint label for normal mode. Also I want to make the app more customizable, as shift click does have it used like open a new browser instace, which is not what I want. I also want another key that just move my mouse there. Some element only show up when you hover over some element. Lastly I want to fix the hint overlap issue, happen mostly on discord. What is the suggestion for this case"

## Clarifications

### Session 2026-07-10

- Q: What is the acceptable upper bound for all hints to be ready (not just first batch)? → A: 750ms for most use cases (200+ elements); accuracy must never be sacrificed to hit a time target. Use tree trimming (skip non-interactive subtrees) and pattern-availability pre-filtering at the UIA provider level to reduce cross-process data transfer — this cuts the transferred element count by 40–60% without losing any interactive elements. (Note: parallel subtree retrieval was initially considered but is infeasible due to UIA COM apartment-threading constraints — all UIA calls must serialize through a single STA thread.)
- Q: [TECHNICAL CONSTRAINT] Can parallel subtree retrieval across UIA threads actually work? → A: No. UIA COM objects are apartment-threaded; `IUIAutomationElement` instances are tied to their creating STA thread. Parallel `FindAllBuildCache` calls from different threads marshal back to the same STA, serializing execution. The bottleneck is in the target process's UIA provider, not in our threading model. The optimization strategy instead uses provider-side filtering (push pattern-availability conditions into the `FindAll` call so filtering happens inside the target process before data crosses the boundary) and conservative tree trimming.
- Q: What benchmark logging approach should Vimium use to measure and track enumeration performance? → A: Structured JSON log entries written to a rolling log file in `%APPDATA%\Vimium\logs\`. Each entry includes timestamp, window title, element count, elapsed ms, cache hit/miss, and filter mode. Logs stay local (no telemetry) and are machine-parseable for trend analysis.
- Q: How should Vimium automate the verification of enumeration performance improvements? → A: Unit tests for the filtering logic (condition-tree construction, pattern-availability checks, tree trimming decisions) using mock UIA elements, plus a documented manual benchmark procedure using Wikipedia/Singapore in Chrome with a PowerShell script to parse benchmark logs and report 95th-percentile latency.
- Q: During a benchmark run, should the result cache be cleared between repetitions? → A: Yes — clear the cache between repetitions so every measurement is a cold-start enumeration. This isolates the optimization under test. Cache effectiveness is measured separately (first activation vs. subsequent).
- Q: [FEASIBILITY] Are pattern-availability properties (IsInvokePatternAvailable, etc.) reliable as FindAll conditions for element-mode patterns, given that FindTextProviderService documents IsTextPatternAvailable returning false negatives? → A: The TextPattern-specific unreliability does not apply to element-mode patterns (Invoke, Toggle, SelectionItem, ExpandCollapse, Value, RangeValue). These simpler patterns are reliably reported by UIA providers. FR-001's pattern-availability condition filtering is valid for element-mode enumeration. The TextPattern quirk is specific to complex text-provider implementations (Windows Terminal, conhost) and irrelevant to interactive element discovery.
- Q: How many configurable modifier-action slots should the system support, and should modifier combinations be allowed? → A: Three slots — default (no modifier) + two alternate actions supporting single modifiers or two-key combos (e.g., Ctrl+Shift). The options UI must use key capture (capture actual keystrokes) with visual feedback showing the captured modifier combination, inspired by PowerToys shortcut setup, instead of requiring manual key-name typing.
- Q: How should the overlap resolver handle densely-packed horizontal button rows like Discord's message actions? → A: Spiral offsetting — labels try positions in priority order: top-left (default), above, below, right, left, using the first non-overlapping position. Labels that still cannot find a clear position within 20px are stacked vertically at the element edge.
- Q: When should hint filtering be applied — based on the default action, or dynamically as modifiers are held? → A: Filter by the default (no-modifier) slot's action type when the overlay opens. Holding a different modifier does not change which hints are visible. This keeps the hint set stable and predictable.
- Q: After the Hover action fires and the overlay closes, what should happen to the mouse cursor? → A: Cursor persists at the target element's center so hover-revealed UI stays visible for the next hint activation. Returning the cursor would cause revealed UI to immediately disappear.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Instant Hint Visibility (Priority: P1)

Users press the element-mode hotkey (e.g., `Ctrl+;`) and expect to see usable hints almost immediately, as they do with browser-based Vimium. Currently the loading indicator appears quickly (within 100ms, as required by the constitution), but hint labels take 1–3 seconds to appear on complex applications like Discord, Slack, or VS Code. Users abandon Vimium and reach for the mouse during this wait.

**Why this priority**: Performance is the top complaint. If hints don't appear quickly, users won't use any other feature. The constitution already mandates <100ms overlay appearance and background enumeration, but the end-to-end time from hotkey press to *actionable hints* must be dramatically reduced to match the competition.

**Independent Test**: Can be fully tested by activating element mode on a complex Electron application (Discord, Slack) and measuring the time from hotkey press to the first visible hint label. Delivers immediate user value even without customization or overlap fixes.

**Acceptance Scenarios**:

1. **Given** an application with 200+ interactive elements (e.g., Discord), **When** the user presses the element-mode hotkey, **Then** all hint labels appear within 750ms of the hotkey press for typical applications, while preserving full enumeration accuracy (every interactive element receives a hint).
2. **Given** a simple application with fewer than 50 interactive elements (e.g., Notepad), **When** the user presses the element-mode hotkey, **Then** all hint labels appear within 400ms of the hotkey press.
3. **Given** the overlay is showing a loading indicator while hints are being enumerated, **When** the UIA provider returns only elements matching interactive pattern-availability conditions (filtered before crossing the process boundary), **Then** the result set is 40–60% smaller than the full control-view tree, containing only elements that actually support at least one interactive pattern.
4. **Given** the user types characters to filter hints, **When** characters are typed during enumeration, **Then** the input is buffered and applied as soon as hints appear, without requiring the user to re-type.

---

### User Story 2 - Configurable Hint Actions (Priority: P2)

Users want to customize what happens when they select a hint with different modifier keys. Currently, holding Left Shift performs a real left click (via `mouse_event`), holding Right Shift performs a real right click, and no modifier triggers UI Automation `Invoke()`. Users want to:
- Choose which modifier+key combination triggers which action
- Add a "Hover" action (move cursor to element center without clicking — triggers CSS `:hover` effects and leaves the cursor in place, useful for elements that only appear on hover like tooltips, menus, or reveal-on-hover UI)

**Why this priority**: Customization is the core differentiator between Vimium (Windows) and browser-based Vimium. Different applications require different interaction strategies. However, it's less critical than performance — users will tolerate limited actions if hints appear quickly, but not the reverse.

**Independent Test**: Can be tested by configuring custom actions in the options window, then activating element mode and selecting a hint with the configured modifier. Delivers value independently of performance and overlap fixes.

**Acceptance Scenarios**:

1. **Given** the options window has an action-configuration section with key-capture controls, **When** the user clicks the capture control for the first alternate slot and presses `Shift`, **Then** the control displays "Shift" visually and selecting a hint while holding `Shift` triggers the action assigned to that slot.
2. **Given** the user has assigned "Hover" to the `Shift` slot, **When** they select a hint while holding `Shift`, **Then** the cursor moves to the element's center without clicking, triggering CSS :hover effects.
3. **Given** the user has assigned "Hover" to the `Ctrl` slot via key capture, **When** they select a hint while holding `Ctrl`, **Then** the cursor moves to the element and triggers a hover event, causing hover-revealed UI to appear.
4. **Given** an element that only appears when another element is hovered, **When** the user first hovers the parent element (via the "Hover" action), then re-activates hints, **Then** the newly revealed child element is now visible in the hint overlay.
5. **Given** default settings, **When** the user selects a hint with no modifier held, **Then** the element is invoked via UI Automation (preserving the existing default behavior).
6. **Given** the user has configured custom modifier actions for both alternate slots, **When** no modifier matches the keys held during hint selection, **Then** the default action (the unmodified slot) is used as a fallback.

---

### User Story 3 - Non-Overlapping Hint Labels (Priority: P2)

On densely-packed UIs like Discord, hint labels overlap each other because they are placed at the top-left corner of each element's bounding rectangle with no collision avoidance. Users cannot read overlapping labels and must guess or re-activate hints.

**Why this priority**: Hint overlap makes the feature unusable on the very applications where it's most needed (complex, element-rich UIs). It shares P2 with customization because both address "hints are visible but not usable."

**Independent Test**: Can be tested by activating hints on Discord or a similar dense UI and visually verifying that no two hint labels overlap. Delivers value independently of performance and customization.

**Acceptance Scenarios**:

1. **Given** a UI with 50+ elements in close proximity (e.g., Discord message action buttons), **When** hints are displayed, **Then** no two hint labels visually overlap each other.
2. **Given** hint labels have been repositioned to avoid overlap, **When** the user views the overlay, **Then** each label remains close enough to its target element that the association is visually clear (within 20px of the element's edge).
3. **Given** two elements have identical or nearly-identical bounding rectangles (e.g., overlaid controls), **When** hints are displayed, **Then** labels are offset in different directions so each remains readable.
4. **Given** a very large number of elements in a small area (extreme density), **When** hints are displayed, **Then** labels are stacked or spread in a way that prioritizes readability, even if some labels extend beyond their element's immediate boundary.

---

### User Story 4 - Per-Action Hint Filtering (Priority: P3)

Not all interactive elements support all actions. For example, a static text label has no `InvokePattern` but could be useful for "Hover." Conversely, when the user intends to click, non-clickable elements clutter the hint overlay. The overlay should optionally filter hints based on the intended action.

**Why this priority**: This is a refinement that improves usability once the core performance and overlap issues are resolved. It reduces visual noise and speeds up hint selection.

**Independent Test**: Can be tested by configuring the overlay to only show clickable elements, then verifying that non-clickable elements receive no hints. Delivers value independently.

**Acceptance Scenarios**:

1. **Given** the default action slot is configured as Invoke, **When** the overlay opens, **Then** only elements that support invocation (invoke, toggle, select, expand/collapse, or editable value patterns) receive hint labels.
2. **Given** the default action slot is configured as Hover, **When** the overlay opens, **Then** all visible on-screen elements (including non-interactive ones) receive hint labels.
3. **Given** the overlay is open with Invoke-filtered hints, **When** the user holds an alternate modifier configured as Hover, **Then** the visible hint set does not change — filtering is fixed at overlay-open time.

---

### Edge Cases

- What happens when an element is removed from the UI between hint enumeration and hint selection? The action fails silently and the overlay closes (current behavior, must be preserved).
- What happens when the user switches windows (Alt+Tab) while hints are being enumerated? The overlay dismisses and enumeration is cancelled (current behavior via `GetForegroundWindow()` polling, must be preserved).
- What happens when the user configures two modifiers to the same action? The last-saved configuration wins; no error is needed.
- What happens when an element has zero dimensions (0×0 bounding rectangle)? It is excluded from hints (current behavior, must be preserved).
- What happens when hint labels would need to be placed off-screen to avoid overlap? Labels are clamped to screen bounds; if unavoidable, the label nearest its element takes priority.
- What happens when the hover action is used on an element that doesn't support hover? The cursor moves to the element center and stays there. The mouse position alone may trigger CSS `:hover` or equivalent in many UI frameworks. No synthetic hover message is sent beyond the cursor repositioning.
- What happens when the user starts typing before the filtered enumeration is complete? Characters are buffered and the match string is applied to the full hint set once enumeration completes and hints are rendered. On repeated activations of the same window (cache hit), hints appear instantly so this scenario is rare.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST reduce cross-process UIA data transfer by pushing pattern-availability conditions (IsInvokePatternAvailable, IsTogglePatternAvailable, IsSelectionItemPatternAvailable, IsExpandCollapsePatternAvailable, IsValuePatternAvailable, IsRangeValuePatternAvailable) into the `FindAll` condition tree so the UIA provider filters out non-interactive elements before data crosses the process boundary — no interactive element is lost (full accuracy).
- **FR-002**: System MUST trim the accessibility tree during enumeration by skipping subtrees rooted at elements that are not control elements, not enabled, or off-screen, avoiding deep walks into containers that cannot contain interactive controls.
- **FR-003**: System MUST render all hint labels within 750ms of hotkey activation for typical complex applications (200+ elements), without dropping or skipping any interactive element (full accuracy preserved).
- **FR-004**: System MUST render all hint labels within 400ms of hotkey activation for simple applications (fewer than 50 elements).
- **FR-005**: Accuracy MUST take priority over speed — if a particular application's accessibility tree cannot be fully enumerated within 750ms, the system MUST complete enumeration with full accuracy rather than returning a partial result set.
- **FR-006**: System MUST support a "Hover" action that moves the cursor to the hint element's center without performing any click or invoke. The cursor position triggers CSS `:hover` and native hover events in most UI frameworks, causing hover-revealed UI elements to appear. The cursor persists at the target position so revealed elements remain visible for subsequent hint activation.
- **FR-008**: System MUST support three configurable modifier-action slots: a default action (no modifier held) and two alternate actions, each mappable to a single modifier key or a two-key modifier combination (e.g., `Shift`, `Ctrl`, `Ctrl+Shift`).
- **FR-009**: System MUST provide a key-capture control in the options window that lets users set a modifier combination by physically pressing the keys, with immediate visual feedback showing the captured combination — no manual key-name typing required.
- **FR-010**: System MUST detect and resolve overlapping hint labels using spiral offsetting — each label tries positions in priority order (top-left default, above, below, right, left), using the first position that does not overlap any other label.
- **FR-011**: System MUST keep each repositioned hint label within 20px of its target element's edge to maintain clear visual association.
- **FR-012**: System MUST buffer keyboard input during hint enumeration and apply the match string when hints appear.
- **FR-013**: System MUST support cancelling an in-progress hint enumeration when the user dismisses the overlay (Escape or window change).
- **FR-014**: System MUST preserve backward compatibility: the default behavior (no modifier → UI Automation invoke) remains unchanged.
- **FR-015**: System MUST filter hints at overlay-open time based on the default action slot's configured action type — when the default is a click-based action (Invoke, LeftClick, RightClick), only elements supporting invocation patterns are shown; when the default is Hover, all visible elements are shown. Holding alternate modifiers does not change the visible hint set.
- **FR-016**: System MUST limit the time spent on element-discovery operations so that no single batch of accessibility-tree traversal blocks the background thread for more than 200ms.
- **FR-017**: System MUST cache and reuse enumeration results when the foreground window has not changed since the last hint activation, enabling near-instant re-display of hints on repeated activations.
- **FR-018**: System MUST log each enumeration session as a structured JSON entry to a rolling log file in the local app data directory. Each entry MUST include: timestamp, foreground window title, total element count, enumeration elapsed milliseconds, whether the result was a cache hit, and which filter mode was used (Invoke-filtered or all-elements). Logs MUST rotate at 10MB total (oldest entries evicted) and MUST NOT be transmitted off-machine.
- **FR-019**: The enumeration filtering logic (condition-tree construction with pattern-availability properties, tree trimming decisions, result caching) MUST be unit-testable in isolation via mock UIA elements, per constitution Principle III.
- **FR-020**: The project MUST include a documented benchmark procedure using a standardized Wikipedia page (`https://en.wikipedia.org/wiki/Singapore`) opened in Chrome as the target application. The procedure defines: how to set up the test environment, how many repetitions to run, and how to parse the benchmark log to compute 95th-percentile latency.

### Key Entities

- **Hint Action**: Represents the action taken when a hint is selected — comprises an action type (Invoke, LeftClick, RightClick, Hover) and a set of modifier key flags that trigger it. Stored in the user configuration.
- **Action Configuration**: Three fixed slots mapping modifier key combinations to hint action types — Slot 0 (default, no modifier), Slot 1 (first alternate), Slot 2 (second alternate). Each alternate slot stores a modifier combination (single key or two-key combo) and an assigned action type. Persisted in `config.json`. Defaults: Slot 0 = Invoke, Slot 1 = Shift → LeftClick, Slot 2 = unassigned.
- **Hint Label Position**: The on-screen placement of a hint label, derived from the element's bounding rectangle and adjusted for overlap avoidance. May differ from the element's top-left corner.
- **Enumeration Session**: A single background-thread UIA tree walk using provider-side pattern-availability conditions and conservative tree trimming. Results are delivered to the UI in one batch once the filtered `FindAll` call completes. The session result may be cached and reused when the foreground window has not changed since the previous activation.
- **Benchmark Log Entry**: A structured JSON record of one enumeration session, written to a rolling log file. Fields: `timestamp` (ISO 8601), `windowTitle` (string), `elementCount` (int), `elapsedMs` (int), `cacheHit` (bool), `filterMode` (enum: `InvokeFiltered` | `AllElements`). Used to track performance trends and validate SC-001/SC-002.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: End-to-end time from hotkey press to all actionable hint labels is under 750ms for applications with 200+ elements, measured on a standard Windows 11 machine, with zero elements missing (full accuracy).
- **SC-002**: 95% of hint enumeration sessions complete within 750ms for applications with up to 500 elements.
- **SC-003**: Zero overlapping hint labels on Discord's message view (50+ action buttons visible simultaneously).
- **SC-004**: Users can configure all modifier-action slots and assign any of the four action types (Invoke, Left Click, Right Click, Hover) via the options window, without editing configuration files manually.
- **SC-005**: The "Hover" action successfully reveals hover-dependent UI elements on at least 3 major applications (e.g., Discord server sidebar hover menu, Slack message hover actions, Windows taskbar thumbnail previews).
- **SC-006**: Hint overlay dismissal (Escape or window change) cancels enumeration within 200ms, freeing resources and preventing stale hint display.
- **SC-007**: No regression in existing behavior: all existing tests pass, and the default hint interaction flow (no modifier → Invoke) is identical to the current release.
- **SC-008**: Benchmark log entries are written for every enumeration session and can be parsed to produce a 95th-percentile latency metric. The log format is stable and documented so that external tooling (or manual inspection) can verify performance regressions.

## Assumptions

- The target applications (Discord, Slack, VS Code) are Electron-based and expose their UI through Windows UI Automation, even if the accessibility tree is large and deeply nested.
- Users have a standard US/QWERTY keyboard layout for modifier key configuration. International keyboard support for modifier mapping is out of scope.
- The "Hover" action relies on moving the physical cursor position, which triggers CSS `:hover` and native hover events in most UI frameworks. It does not synthesize `WM_MOUSEHOVER` messages directly.
- UIA COM objects are apartment-threaded (STA). All UIA calls during a single enumeration session execute on a single background thread. Parallelism across UIA threads is not viable — performance gains come from reducing the amount of data that crosses the process boundary through provider-side condition filtering.
- Pattern-availability pre-filtering (adding `Is*PatternAvailable` properties to the `FindAll` condition tree) safely excludes only elements that lack all six interactive patterns. No interactive element is lost. An element without Invoke, Toggle, SelectionItem, ExpandCollapse, Value, or RangeValue patterns has no actionable behavior Vimium can trigger.
- Tree trimming is conservative: only subtrees rooted at elements that are definitively non-interactive (not a control, not enabled, or off-screen) are skipped. Control-view condition is preserved — switching to content view is explicitly rejected as it may exclude valid interactive elements.
- Result caching across repeated activations on the same foreground window (hWnd unchanged) is safe because the UIA element tree for a window does not change without a visible UI update that would typically cause a window activation change.
- The benchmark procedure uses Wikipedia's Singapore article because it has a known, stable DOM structure with ~200–300 interactive elements (links, buttons, form controls), providing a reproducible mid-complexity test case. The page must be loaded in Chrome with the window maximized at 1920×1080 for consistent viewport-dependent element counts.
- Each benchmark repetition clears the enumeration result cache so every measurement is a cold-start enumeration. This ensures the benchmark measures the actual enumeration optimization, not cache performance.
- A PowerShell script (`scripts/parse-benchmark-log.ps1`) accompanies the benchmark procedure to read the JSON log file and compute summary statistics (mean, median, 95th percentile, min, max) from the most recent N entries matching the benchmark window title, filtering to cache-hit=false entries only.
- Overlap avoidance uses spiral offsetting (priority: top-left → above → below → right → left). Labels that cannot find a non-overlapping position within 20px stack vertically. Global force-directed layout is out of scope.
- The existing `Hint.MovePointerToCenter()` method provides the foundation for the Hover action and requires no modification.
- Configuration persistence uses the existing `ConfigService` and `config.json` infrastructure established in the options window modernization.
- The existing `CacheRequest` mechanism in `UiAutomationHintProviderService.EnumElements` is already optimal for batch property retrieval. Performance gains come from parallel subtree retrieval and reduced blocking time, not from changing the cache strategy.
