# Data Model: Text Selection Mode (Redesigned)

**Feature**: Text Selection Mode | **Date**: 2026-07-09

## Entities

### TextSource
Represents the extracted text content and position data from the foreground window.

| Field | Type | Description |
|-------|------|-------------|
| FullText | string | All visible text content, concatenated from text blocks with newline separators |
| LineRects | IReadOnlyList\<Rect> | Per-line bounding rectangles in window coordinates, indexed by line number |
| LineTexts | IReadOnlyList\<string> | Per-line text content, parallel array with LineRects |
| SourceWindow | IntPtr | Handle of the source window |

**Lifecycle**: Created by `TextSourceProviderService`, consumed by `SelectionModeViewModel`, GC'd when overlay closes.

### SelectionState (unchanged from original)
Mutable state for cursor, selection, and search within the visible text.

| Field | Type | Description |
|-------|------|-------------|
| VisibleText | string | Full visible text (from TextSource.FullText) |
| CursorPosition | int | Current character offset (0-based) |
| SelectionStart | int? | Selection start offset, null if no selection |
| SelectionEnd | int? | Selection end offset, null if no selection |
| SearchQuery | string | Current incremental search string |
| SearchMatches | IReadOnlyList\<SearchMatch> | All occurrences of SearchQuery |
| ActiveMatchIndex | int | Index into SearchMatches of active match |
| AllVisibleLines | IReadOnlyList\<TextLineRect> | Per-line bounding rects for rendering |

**Derived Properties**:
- `SelectedText`: substring between SelectionStart and SelectionEnd
- `HasSelection`: true when SelectionStart != SelectionEnd
- `CursorLineIndex`: which line the cursor is on
- `CursorLinePosition`: character position within current line

### SearchMatch (unchanged)
A single occurrence of the search phrase.

| Field | Type | Description |
|-------|------|-------------|
| StartIndex | int | Character offset where match starts |
| EndIndex | int | Character offset where match ends |
| LineIndex | int | Which line the match is on |
| IsActive | bool | Whether this is the currently active match |

### TextLineRect (replaces TextLineHint)
A lightweight struct replacing the heavy TextLineHint model. No longer extends Hint — just position data.

| Field | Type | Description |
|-------|------|-------------|
| BoundingRectangle | Rect | Line bounds in window coordinates |
| Text | string | Text content of this line |

## Removed Entities
- **TextLineHint** — deleted. Was a full Hint subclass used for per-line labeling. No longer needed.
- **LineNavigationSession** — deleted. Was a container for hint lists. Replaced by TextSource.
