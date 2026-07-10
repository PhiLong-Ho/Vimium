# Data Model: Find-and-Navigate Text Mode (Ctrl+F Style)

**Feature**: Find-and-Navigate Text Mode | **Date**: 2026-07-09

## Entities

### FindSession

Represents the state of an active find-text operation. Created when the overlay opens, destroyed when it closes.

| Field | Type | Description |
|-------|------|-------------|
| SearchQuery | string | Current incremental search string (user-typed characters) |
| Matches | IReadOnlyList\<SearchMatch> | All occurrences of SearchQuery within the visible viewport (max 200) |
| ActiveMatchIndex | int | Index into Matches of the currently active match (orange highlight) |
| SourceWindowHandle | IntPtr | Handle of the foreground window being searched |
| IsSearching | bool | True while an async search is in-flight (for loading indicator) |
| HasMatches | bool | Derived: Matches.Count > 0 |
| MatchCountText | string | Derived: "0 matches" when empty, "2 of 5" when ActiveMatchIndex=1 and count=5 |

**Lifecycle**: Created by `SelectionModeViewModel` when overlay opens. Updated on each search result. GC'd when overlay closes. No persisted state.

**State transitions**:
```
[Overlay opens] → IsSearching=false, SearchQuery="", Matches=empty
    → User types (≥5 chars, 150ms debounce) → IsSearching=true
    → Search completes → IsSearching=false, Matches populated, ActiveMatchIndex=0
    → User presses Tab → ActiveMatchIndex = (ActiveMatchIndex + 1) % Matches.Count
    → User presses Shift+Tab → ActiveMatchIndex = (ActiveMatchIndex - 1 + Matches.Count) % Matches.Count
    → User presses Enter → ScrollIntoView + Select on active match → [Overlay closes]
    → User presses Escape → [Overlay closes]
    → Window change → [Overlay closes immediately, silently]
    → UIA TextChanged event → [Overlay closes on next user interaction]
```

### SearchMatch

A single occurrence of the search phrase within the visible text of the foreground window. Contains everything needed to render a highlight and navigate the cursor on Enter.

| Field | Type | Description |
|-------|------|-------------|
| SourceText | string | The matched text content (e.g., "Singapore") |
| BoundingRect | Rect | Accurate bounding rectangle in window coordinates from UIA. Used directly for highlight rendering — no estimation. |
| Source | SearchResultSource | Where this match came from: `TextPattern` (primary) or `ElementName` (fallback) |
| IsActive | bool | Whether this match is the currently active one (highlighted orange vs yellow) |
| TextRangeProvider | ITextRangeProvider? | The UIA text range for this match (null for ElementName fallback). Used on Enter for ScrollIntoView + Select. |

**Removed fields** (deprecated from old design):
- ~~StartIndex~~ — no longer needed; we navigate via `ITextRangeProvider`, not character offsets
- ~~EndIndex~~ — no longer needed
- ~~LineIndex~~ — no longer needed; we use `BoundingRect` directly

**Validation rules**:
- `SourceText` must not be null or empty
- `BoundingRect` must have positive width and height (match must be visible in viewport)
- `IsActive` is true for exactly one match in the collection (when Matches.Count > 0)

### SearchResult

Raw search result produced by `IFindTextProviderService.SearchAsync()`. Lightweight DTO — transformed into `SearchMatch` by the ViewModel (adding `IsActive` state).

| Field | Type | Description |
|-------|------|-------------|
| Text | string | The matched text content |
| BoundingRect | Rect | Accurate bounding rectangle in window coordinates |
| Source | SearchResultSource | Provider source: `TextPattern` or `ElementName` |
| TextRangeProvider | ITextRangeProvider? | UIA text range reference for Enter navigation (null for ElementName source) |
| AutomationElement | AutomationElement? | The containing UIA element (non-null for ElementName source; used for focus on Enter) |

### SearchResultSource (enum)

| Value | Description |
|-------|-------------|
| TextPattern | Match found via `ITextProvider.GetVisibleRanges()` → `ITextRangeProvider.FindText()`. Has accurate bounding rect and `ITextRangeProvider` for ScrollIntoView + Select. |
| ElementName | Match found via `FindAllBuildCache` element name search. Has `AutomationElement` bounding rect (coarser, element-level) and element reference for SetFocus. |

### FindResult

Container returned by `IFindTextProviderService.SearchAsync()`.

| Field | Type | Description |
|-------|------|-------------|
| Matches | IReadOnlyList\<SearchResult> | Ordered list of search results (up to 200), in document order |
| Source | SearchResultSource | Which provider produced the results |
| TimedOut | bool | True if the 3-second timeout expired (partial results may be present) |
| ElapsedMs | long | Actual search elapsed time in milliseconds (for debug logging) |

## Removed Entities (from old design, 2026-07-09 redesign)

| Entity | Reason |
|--------|--------|
| `TextSource` | No "load all text" step in Ctrl+F design. Replaced by query-driven `FindResult`. |
| `TextLineRect` | Per-line bounding rectangles no longer needed. UIA provides per-match bounding rects directly. |
| `SelectionState` (old version) | Cursor position, selection range, AllVisibleLines, VisibleText — all removed. SelectionState becomes a thin container for find-only state (see FindSession). |
| `TextLineHint` | Already deleted. Per-line hint objects replaced by per-match SearchMatch. |
| `LineNavigationSession` | Already deleted. Hint session container replaced by FindSession. |

## Diagram: Entity Relationships

```
IFindTextProviderService.SearchAsync(hWnd, query, ct)
    │
    ▼
FindResult
    ├── Matches: IReadOnlyList<SearchResult>
    │       ├── Text: string
    │       ├── BoundingRect: Rect
    │       ├── Source: SearchResultSource { TextPattern | ElementName }
    │       ├── TextRangeProvider: ITextRangeProvider?   ← for Enter navigation
    │       └── AutomationElement: AutomationElement?     ← for ElementName fallback
    ├── Source: SearchResultSource
    ├── TimedOut: bool
    └── ElapsedMs: long
            │
            │ (transformed by SelectionModeViewModel)
            ▼
FindSession
    ├── SearchQuery: string
    ├── Matches: IReadOnlyList<SearchMatch>
    │       ├── SourceText: string
    │       ├── BoundingRect: Rect
    │       ├── Source: SearchResultSource
    │       ├── IsActive: bool
    │       └── TextRangeProvider: ITextRangeProvider?
    ├── ActiveMatchIndex: int
    ├── SourceWindowHandle: IntPtr
    └── IsSearching: bool
```
