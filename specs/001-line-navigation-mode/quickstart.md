# Quickstart Validation Guide: Text Selection Mode (Redesigned)

**Feature**: Text Selection Mode | **Date**: 2026-07-09

## Prerequisites

1. Build: `dotnet build src/Vimium.sln` — must succeed with 0 errors
2. Run: `Vimium.exe --force` (kills existing instance, starts new one)
3. Element mode MUST still work: `Ctrl+;` → element hints appear

## Validation Scenarios

### VS-1: Activate text selection mode
- Open Chrome with any text-heavy page (e.g., Wikipedia)
- Press `Ctrl+.`
- **Expected**: Overlay appears with a search bar at the bottom. No hint labels.
- Press Escape → overlay closes.

### VS-2: Search finds and highlights text
- Press `Ctrl+.`
- Type a word visible on screen (e.g., "Singapore")
- **Expected**: All occurrences highlighted in yellow. First occurrence in orange (active match). Search bar shows typed text.

### VS-3: Tab cycles through matches
- With search results visible
- Press Tab → next match becomes active (orange), cursor moves there
- Press Shift+Tab → previous match becomes active
- Keep pressing Tab until it wraps around to the first match
- **Expected**: Circular cycling works. Match count shows "N of M".

### VS-4: Arrow keys move cursor
- With a search match active
- Press Arrow Right → cursor moves one character right
- Press Arrow Left → cursor moves one character left
- Press Ctrl+Arrow Right → cursor jumps to next word
- **Expected**: Cursor indicator (blinking vertical line) moves accordingly.

### VS-5: Shift+Arrow selects text
- With cursor positioned
- Hold Shift + Arrow Right → text selected, shown in blue highlight
- Continue extending selection
- **Expected**: Blue selection rectangle grows with each keystroke.

### VS-6: Enter copies and closes
- With text selected (or cursor positioned)
- Press Enter
- **Expected**: "Copied!" toast appears briefly. Overlay closes. Verify by pasting (Ctrl+V) elsewhere.

### VS-7: Escape cancels without copy
- With search, cursor, or selection active
- Press Escape
- **Expected**: Overlay closes. Nothing copied to clipboard.

### VS-8: Notepad support
- Open Notepad, type several lines of text
- Press `Ctrl+.`
- Type a word from the Notepad text
- **Expected**: Matches found and highlighted. Copy works.

### VS-9: Element mode unchanged (regression)
- Press `Ctrl+;`
- **Expected**: Element hints appear on interactive elements. Works identically to before.
- Type a hint label → element is invoked/clicked.
- Escape dismisses.
