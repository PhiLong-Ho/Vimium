# Quickstart Validation Guide: Find-and-Navigate Text Mode (Ctrl+F Style)

**Feature**: Find-and-Navigate Text Mode | **Date**: 2026-07-09

## Prerequisites

1. Build: `dotnet build src/Vimium.sln` — must succeed with 0 errors
2. Run: `Vimium.exe --force` (kills existing instance, starts new one)
3. Element mode MUST still work: `Ctrl+;` → element hints appear on interactive elements

## Validation Scenarios

### VS-1: Activate find-text mode

- Open Chrome with any text-heavy page (e.g., a Wikipedia article)
- Press `Ctrl+.`
- **Expected**: Overlay appears with a search bar at the bottom. No hint labels. Search bar is empty, shows no match count (or blank). Underlying window text is still visible through the transparent overlay.
- Press Escape → overlay closes.

### VS-2: Search finds and highlights matches

- Press `Ctrl+.`
- Type at least 5 characters of a word visible on screen (e.g., "Singapore")
- Wait for the 150ms debounce to complete
- **Expected**: All visible occurrences highlighted in yellow. First occurrence highlighted in orange (active match). Search bar shows typed text and match count (e.g., "1 of 3").
- **Edge case**: If no matches found, "0 matches" displayed with no highlights.

### VS-3: Tab cycles through matches (circular wrap)

- With search results visible (from VS-2)
- Press Tab → next match becomes active (orange), previous returns to yellow. Match count updates ("2 of 3").
- Press Tab again → advances further.
- Keep pressing Tab until it wraps around to the first match → "1 of 3".
- Press Shift+Tab → goes to previous match (wraps from first to last).
- **Expected**: Circular cycling in both directions. Active highlight (orange) moves instantly (<50ms). Match count text updates correctly.

### VS-4: Enter navigates cursor to match and closes overlay

- With an active match (from VS-2)
- Press Enter
- **Expected**: 
  - In editable text (Notepad, text editor): the text cursor is positioned at the match
  - In non-editable text (web page): the match element is focused and scrolled into view
  - Overlay closes immediately. **No** "Copied!" toast appears (this feature does not copy).
  - No confirmation message of any kind.

### VS-5: Escape cancels without navigation

- With search results visible
- Press Escape
- **Expected**: Overlay closes. No navigation occurred. The cursor/focus remains where it was before activation. No clipboard interaction.

### VS-6: 5-character minimum before search triggers

- Press `Ctrl+.`
- Type 1-4 characters (e.g., "Sin")
- Wait >150ms (debounce elapsed)
- **Expected**: No search triggered. No "0 matches" shown. No match highlights. Search bar still shows typed characters. No loading indicator.
- Type a 5th character (e.g., "g" → "Sing" → wait, then "a" → "Singa")
- Wait 150ms
- **Expected**: Search triggers after the 5th character + debounce. Highlights appear.

### VS-7: Debounce prevents search on rapid typing

- Press `Ctrl+.`
- Rapidly type "Singapore" without pausing (all keystrokes <150ms apart)
- **Expected**: No intermediate search triggers between keystrokes. Search fires once, ~150ms after the last keystroke (when query reaches ≥5 chars). Only one set of highlights appears (not flickering through partial results).

### VS-8: Backspace updates matches

- With search results visible (from VS-2)
- Press Backspace to reduce query below 5 characters
- **Expected**: Highlights disappear (search cancelled — below 5-char minimum). If query stays ≥5 chars after Backspace, new debounced search triggers with updated results.
- Clear all text with repeated Backspace
- **Expected**: Overlay visible with empty search bar. No highlights. Ready for new query.

### VS-9: Notepad support (editable text context)

- Open Notepad, type several lines of text containing a repeated word (e.g., "the the the")
- Press `Ctrl+.`
- Type at least 5 characters of the repeated word
- **Expected**: On Windows 11 Notepad (ValuePattern), matches are found with shared bounding rectangles. On older Notepad, TextPattern may provide per-match rectangles. On classic Notepad, element-name fallback may produce partial results. This is best-effort.
- **Timeout behavior**: If the search times out, the overlay shows a tip recommending the app's built-in Ctrl+F.

### VS-10: VS Code support

- Open VS Code with a source file visible containing identifiable text
- Press `Ctrl+.`
- Type a 5+ character phrase visible in the editor
- **Expected**: The Monaco editor pane is canvas-rendered and exposes no UIA text — no matches will be found (0 matches). VS Code's menus, sidebar, and file tabs may still produce matches via element-name fallback. Use VS Code's own `Ctrl+F` instead — it's always more accurate.
- **Timeout behavior**: If the search times out on the element-name scan, the overlay shows a tip recommending the app's built-in Ctrl+F.

### VS-11: Element mode unchanged (regression)

- Press `Ctrl+;`
- **Expected**: Element hints appear on interactive elements. Works identically to before the find-text feature.
- Type a hint label → element is invoked/clicked.
- Escape dismisses.
- Press `Ctrl+.` (find-text) while element mode is active → find-text overlay replaces element overlay. Only one overlay at a time.

### VS-12: Auto-dismiss on Alt+Tab (window focus change)

- Press `Ctrl+.` → overlay appears
- Type a 5+ character query → results visible
- Press Alt+Tab to switch to a different window
- **Expected**: Overlay dismisses immediately and silently. No toast, no copy, no navigation. The other window receives focus normally.

### VS-13: Auto-dismiss on content change

- Press `Ctrl+.` in a browser with a static page → overlay appears
- Type a 5+ character query → results visible
- Without dismissing the overlay, trigger a page content change (e.g., click a link behind the overlay, or press F5 to refresh)
- **Expected**: Overlay dismisses silently when the content change is detected. No toast.

### VS-14: "No text found" when TextPattern unavailable

- Open Windows File Explorer (which does not expose TextPattern)
- Press `Ctrl+.`
- Type a 5+ character query
- **Expected**: Fallback searches element names (file/folder names). If the query matches folder/file names, those are highlighted. If no match exists, "0 matches" is shown. User can type a different query or press Escape.
  - If neither TextPattern nor element names produce results, "No text found" is displayed and overlay auto-dismisses after 2 seconds.

### VS-15: Theme consistency

- Switch Vimium theme: Light → Dark → Skadi (via tray icon right-click menu)
- Press `Ctrl+.` in each theme
- **Expected**: Search bar, match highlights (yellow/orange), text colors all use the active theme's colors. No hardcoded colors — the overlay looks native to each theme.

## Success Criteria Mapping

| Criterion | Validation |
|-----------|------------|
| SC-001: Overlay appears <100ms of hotkey | VS-1 — overlay should be visible instantly (just transparent window + search bar, no data load) |
| SC-002: Matches appear <200ms of keystroke | VS-2 — on 5,000+ element windows, first results within 200ms of debounce completing |
| SC-003: Find + navigate <3 seconds | VS-4 — measured from Ctrl+. press to cursor repositioned at match after Enter |
| SC-004: Tab cycling <50ms | VS-3 — active highlight update feels instant |
| SC-005: Element mode zero regressions | VS-11 — Ctrl+; works identically |
| SC-006: Works on Chrome, Firefox, Notepad, VS Code, Explorer | VS-2 (Chrome), VS-9 (Notepad), VS-10 (VS Code), VS-14 (Explorer) |
| SC-007: No "load all text" step | VS-2 — search bar opens empty; results appear only after typing ≥5 chars + debounce. No loading spinner for text extraction. |
