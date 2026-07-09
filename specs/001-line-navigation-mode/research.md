# Research: Text Selection Mode (Redesigned)

**Feature**: Text Selection Mode | **Date**: 2026-07-09

## R1: How to reliably extract visible text from the foreground window?

**Decision**: Use a 3-layer fallback chain, first-wins:

1. **TextPattern.GetVisibleRanges()** — Returns per-visual-line ranges with text and precise bounding rects. Works on Chrome, WPF apps. Fast (~50ms).
2. **TextPattern.DocumentRange.GetText(-1)** + element tree for bounding rects — Works on Firefox, VS Code. Slower (~300ms) but reliable. Uses `FindAll` to scan for text-bearing elements and their bounding rects.
3. **ValuePattern.CurrentValue** + bounding rect estimation — Works on Win11 Notepad, legacy Win32. Splits text by newlines and estimates per-line positions.

**Rationale**: A single approach doesn't work across all apps. The 3-layer chain covers Chrome, Firefox, Edge, VS Code, and Notepad — the most common use cases.

**Alternatives considered**:
- Single approach for all apps: Too unreliable. Each UIA provider implements text patterns differently.
- OCR-based text discovery: Too slow, too complex, wrong granularity. Rejected.

## R2: How to map character offsets to screen positions?

**Decision**: Use per-line bounding rectangles from UIA with estimated character width. For each line, estimate `charWidth = lineRect.Width / lineText.Length`. Then `x = lineRect.Left + (charOffset * charWidth)`.

**Rationale**: UIA does not provide per-character bounding rectangles. Per-line rects are available from GetVisibleRanges or element bounding boxes. Character width estimation is approximate but sufficient for cursor positioning and match highlighting.

**Alternatives considered**:
- Per-character bounding rect via TextRange.Move(Character, 1): Too slow — one COM call per character.
- Fixed-width font assumption: Inaccurate for proportional fonts.

## R3: How should the search bar overlay interact with the underlying window?

**Decision**: Use the same `ForegroundWindow` base class (`WS_EX_TRANSPARENT`, `WindowStyle=None`, `AllowsTransparency=True`, `Topmost=True`, `ShowActivated=False`). A `KeyboardHookService` captures low-level keyboard input and dispatches to the ViewModel. The overlay never steals focus.

**Rationale**: Same proven pattern as the element hint overlay. The search bar is drawn on the transparent WPF window, positioned over the foreground window.

## R4: Should we keep or remove the line hint overlay?

**Decision**: Remove entirely. Delete `LineNavigationOverlayView`, `LineNavigationOverlayViewModel`, `TextLineHint`, `LineNavigationSession`.

**Rationale**: Per-line hint labeling is unreliable (depends on TextPattern line-level support), visually cluttered (200+ hints on Wikipedia), and overlaps with element mode. The search-first approach is simpler, faster, and more closely mimics mouse selection behavior.

## R5: What text content should be searchable?

**Decision**: All visible text from the foreground window, up to 50,000 characters. Text is obtained from the first successful layer in the 3-layer chain.

**Rationale**: 50k chars covers a full Wikipedia article or a large code file. Limiting prevents memory issues with enormous documents.
