# Research: Find-and-Navigate Text Mode (Ctrl+F Style)

**Feature**: Find-and-Navigate Text Mode | **Date**: 2026-07-09

## R1: How to search text directly in the foreground window without loading all text?

**Decision**: Use `ITextProvider.GetVisibleRanges()` to scope search to the visible viewport, then `ITextRangeProvider.FindText()` in a loop to collect all occurrences. One cross-process COM call per match, bounded by a 200-match cap.

**Implementation outline**:
1. Get `AutomationElement` for the foreground window (`AutomationElement.FromHandle(hWnd)`)
2. Get `ITextProvider` via `element.GetCurrentPattern(TextPattern.Pattern)` or `TextPattern` from the document element
3. Call `textProvider.GetVisibleRanges()` → returns `ITextRangeProvider[]` covering only the visible viewport
4. For each visible range, loop: `range.FindText(query, backward=false, startRange)` → collect match until null
5. Each match: extract bounding rects via `GetBoundingRectangles()` and text via `GetText(-1)`
6. Cap at 200 matches, enforce 3-second `CancellationToken` timeout
7. Return `IReadOnlyList<SearchResult>` with `BoundingRect` and `SearchResultSource.TextPattern`

**Rationale**: The old approach (load all text via UIA/OcrEngine, then client-side IndexOf) caused massive delays because it had to enumerate every text element before searching. TextPattern.FindText is server-side — UIA performs the search inside the target process and only returns matches. Combined with `GetVisibleRanges()` to scope to the viewport, the search is fast even on content-heavy windows. A Wikipedia page with 950 total matches only returns the ~5–15 visible ones.

**Known limitation**: TextPattern support varies by application. Chrome, Firefox, Notepad, and VS Code expose TextPattern. File Explorer and some legacy apps do not → fallback path (see R4).

**Alternatives considered**:
- Load all text via FindAllBuildCache then client-side IndexOf: Too slow for large documents (the original problem).
- Windows.Media.Ocr OCR: 1-3 seconds per capture, lower accuracy, no element references for cursor navigation. Rejected for the primary path.
- Per-element TextPattern calls: Too granular; GetVisibleRanges + FindText is more efficient.

## R2: How to get accurate match bounding rectangles for highlight rendering?

**Decision**: Use `ITextRangeProvider.GetBoundingRectangles()` on each FindText match result. This returns a `Rect[]` in screen coordinates from the UIA provider — no estimation needed.

**Rationale**: UIA TextPattern natively provides bounding rectangles for text ranges. Each FindText result is an `ITextRangeProvider` whose `GetBoundingRectangles()` returns precisely where that text appears on screen. This is the same mechanism screen readers use to highlight text. No character-width estimation, no CJK width multipliers, no RTL concerns — UIA handles all of that.

For the **element name fallback path** (R4), use `AutomationElement.Cached.BoundingRectangle` from the `CacheRequest`. This gives the element's overall bounding rect, which is coarser than a per-match rect but sufficient for the fallback case.

**Alternatives considered**:
- Proportional-width estimation via GlyphTypeface: Complex, inaccurate for mixed scripts, required RTL handling. Rejected — UIA gives us actual rects.
- Per-character Move(Character, 1) + bounding rect: One COM call per character — far too slow.

## R3: How to navigate the system cursor to a match on Enter?

**Decision**: On the active match's `ITextRangeProvider`, call `ScrollIntoView()` followed by `Select()`. Then close the overlay. No clipboard interaction.

**Implementation outline**:
1. Store the `ITextRangeProvider` reference alongside each `SearchMatch` during search (R1)
2. When Enter is pressed on the active match:
   - Call `match.TextRange.ScrollIntoView()` — ensures the match is visible in the viewport
   - Call `match.TextRange.Select()` — positions the text cursor at the match location in editable text, or focuses the containing element in non-editable contexts
3. Close the overlay immediately (no toast, no confirmation)

**Rationale**: `ScrollIntoView()` + `Select()` is the standard UIA pattern for navigating to text, used by screen readers and accessibility tools. In editable contexts (text editors, input fields), `Select()` places the text cursor at the match position. In non-editable contexts (web pages, labels), it focuses the containing element. This matches the spec's requirement: "navigate the system cursor/focus to the active match location."

**Edge case**: If the element is no longer valid (stale reference), the COM call throws. Catch and silently dismiss — no error shown to the user (per spec edge case).

**Alternatives considered**:
- Send synth keyboard events: Unreliable across window types, fragile.
- SetFocus + mouse click simulation: Not accurate for text-level positioning.

## R4: What's the fallback when TextPattern is unavailable or times out?

**Decision**: Two-tier fallback:
1. **Primary**: `ITextProvider.GetVisibleRanges()` → `FindText()` loop (R1) — 3-second timeout
2. **Fallback**: `AutomationElement.FindAllBuildCache(TreeScope.Descendants, Condition.TrueCondition, cacheRequest)` with `CacheRequest` for `Name` + `BoundingRectangle` properties. Client-side filter by `element.Cached.Name.Contains(query, OrdinalIgnoreCase)`. Returns `SearchResult` with `SearchResultSource.ElementName`.
3. **Both fail**: Return empty `FindResult` → ViewModel shows "No text found" → auto-dismiss after 2 seconds

**Rationale**: TextPattern is available in ~70% of common apps (Chrome, Firefox, Notepad, VS Code) but not in File Explorer, legacy Win32 apps, or some WPF/UWP apps. `FindAllBuildCache` with `CacheRequest` is always available (~50ms for typical windows) and provides element names and bounding rects. It's coarser (element-level, not character-level) but provides a functional fallback for the remaining apps.

The spec explicitly documents this two-path approach (clarification Q8 from spec session 2026-07-09).

**Performance**:
- TextPattern path: ~10-50ms per FindText call (cross-process COM), total search time proportional to match count within visible viewport (typically 5-15 matches)
- Element name fallback: ~50ms total (single batch cache call + in-memory filter)
- Both paths well under the 3-second timeout

**Alternatives considered**:
- OCR as tertiary fallback: Added latency (1-3s capture + recognize) and no element references for Enter navigation. Rejected.
- Single-path TextPattern only: Would fail silently on Explorer, Settings, legacy apps. The element-name fallback covers these cases.

## R5: How to debounce search input and cancel in-flight searches?

**Decision**: Use `System.Timers.Timer` with 150ms interval, reset on each keystroke. `CancellationTokenSource` for in-flight UIA search cancellation. Minimum 5 characters before triggering any search.

**Implementation outline**:
```csharp
// In SelectionModeViewModel
private System.Timers.Timer _debounceTimer;
private CancellationTokenSource _searchCts;

public void HandleCharacter(char c)
{
    _searchQuery += c;
    _debounceTimer.Stop();
    _debounceTimer.Start(); // resets 150ms countdown
}

private async void OnDebounceElapsed(object sender, EventArgs e)
{
    _debounceTimer.Stop();
    if (_searchQuery.Length < 5) return; // 5-char minimum

    _searchCts?.Cancel();  // cancel any in-flight search
    _searchCts = new CancellationTokenSource();
    var ct = _searchCts.Token;

    IsSearching = true;
    try
    {
        var result = await _findTextService.SearchAsync(_sourceHwnd, _searchQuery, ct);
        if (!ct.IsCancellationRequested)
        {
            _state.UpdateMatches(result.Matches);
            NotifyAll();
        }
    }
    catch (OperationCanceledException) { /* new keystroke arrived — expected */ }
    finally { IsSearching = false; }
}
```

**Rationale**: 150ms debounce prevents flooding UIA with rapid-fire cross-process COM calls (one per keystroke without debounce). CancellationTokenSource ensures only the most recent search completes — stale results from an earlier query don't overwrite current results. The 5-character minimum avoids single-character queries that would return massive, useless result sets (e.g., searching for "e" on a page).

Per the spec clarifications (Q6, 2026-07-09), this design prevents crashes from rapid typing/deletion and avoids single-short-character query floods.

**Alternatives considered**:
- Reactive Extensions (Rx.NET) Throttle: Adds a dependency. Simple Timer is sufficient.
- No debounce, just cancellation: Still floods UIA with too many COM calls even if oldest are cancelled.
- 300ms debounce: Feels sluggish. 150ms balances responsiveness with protection.

## R6: How to detect window focus change and text content change for auto-dismiss?

**Decision**: Two mechanisms:
1. **Window focus change**: Poll `User32.GetForegroundWindow()` on each keyboard event and compare against the captured source window handle. If different, auto-dismiss immediately and silently.
2. **Text content change**: Register a UIA `TextChanged` event on the `TextPattern` during the first successful search. If the event fires while the overlay is active, set a `_contentChanged` flag. On next user interaction, auto-dismiss.

**Implementation**:
- Window focus: The keyboard hook already runs on every keystroke — add a `GetForegroundWindow()` call (single Win32 API, negligible overhead) and compare hWnd. This is "just in time" detection.
- Content change: `Automation.AddAutomationEventHandler(TextPattern.TextChangedEvent, element, TreeScope.Subtree, OnTextChanged)` during search. In the handler, set flag. On next keystroke, check flag → dismiss if true.

**Rationale**: Both triggers are critical per FR-009. The overlay must not outlive the context it was created for. Polling `GetForegroundWindow()` on keyboard events adds negligible overhead (single Win32 call per keystroke, already on the keyboard hook hot path). The UIA `TextChanged` event provides async notification without polling.

**Alternatives considered**:
- WinEvent hook for `EVENT_OBJECT_FOCUS`: More complex, requires message pump integration. Rejected.
- Timer-based polling: Unnecessary latency. Keyboard-event-driven polling is "just in time."
- Dismiss on ANY keystroke for content change: Race condition — the user could be mid-typing when the content changes. Flag-based deferred dismiss is safer.

## R7: How should the search bar overlay interact with the underlying window?

**Decision**: Use the same `ForegroundWindow` base class pattern as element mode — `WS_EX_TRANSPARENT`, `WindowStyle=None`, `AllowsTransparency=True`, `Topmost=True`, `ShowActivated=False`. A low-level `KeyboardHookService` captures all keyboard input and dispatches to the ViewModel. The overlay never steals keyboard focus.

**Rationale**: This is the proven pattern from element mode (`Ctrl+;`). The search bar is drawn on a transparent WPF window positioned over the foreground window. Since the overlay must not steal focus (FR-002 requires WS_EX_TRANSPARENT), a low-level keyboard hook (`SetWindowsHookEx` with `WH_KEYBOARD_LL`) is the only reliable way to capture Tab, Enter, Escape, and character input without focus.

**Key detail**: Only printable characters, Tab, Shift+Tab, Enter, and Escape are captured for find-text mode. All other keys (arrows, Ctrl combinations, Home, End) are deliberately ignored — the redesigned spec removed cursor movement and text selection from this feature.

## R8: Performance characteristics and constraints

**Decision**: The following constraints collectively ensure fast, bounded search behavior:
- **5-character minimum**: Prevents single-character query floods (e.g., "e" → thousands of matches)
- **150ms debounce**: Prevents rapid-fire cross-process COM calls during fast typing
- **3-second timeout**: Prevents hanging on unresponsive UIA providers
- **200-match cap**: Bounds rendering and memory costs (match highlighting is done via WPF overlays, each match is a small Border element)
- **Visible viewport only**: `GetVisibleRanges()` naturally bounds match count to what's on screen (~5-15 for typical queries)

**Performance budget per operation**:
| Operation | Budget | Rationale |
|-----------|--------|-----------|
| Overlay appearance | <100ms | Just the transparent window + empty search bar — no data loading |
| TextPattern search | <200ms typical, <3s max | 5-15 matches in visible viewport, 10-50ms per FindText call |
| Element name fallback | <50ms | Single batch CacheRequest + in-memory filter |
| Tab cycle (active highlight update) | <50ms | Pure ViewModel state change + WPF binding update |
| Enter (navigate cursor) | <200ms | ScrollIntoView + Select cross-process COM calls |

**Memory**: No "load all text" step means memory usage is proportional to match count (max 200 × ~200 bytes per SearchMatch = ~40KB), not total text content. The old OCR-based approach held the entire `TextSource` (50K chars + line rects) in memory.

**Rationale**: These constraints directly address the original performance problem that motivated the redesign. The spec's user story 3 (P1 priority) requires matches within 200ms on windows with 5,000+ visible text elements. By scoping to the visible viewport and using server-side FindText, performance depends on match count (bounded by screen real estate), not total text content size.

**Alternatives considered**:
- 2-character minimum: Returns too many matches, degrades performance. 5 chars balances usability with performance.
- 10-second timeout: User-perceptible hang. 3 seconds matches the spec.
- Unlimited match cap: Could produce thousands of highlight elements, tanking WPF rendering. 200 is more than sufficient for visible viewport matches.
- Full document search (including offscreen): Would negate the performance benefit. The spec explicitly requires visible-viewport-only.
