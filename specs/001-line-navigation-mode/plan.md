# Implementation Plan: Find-and-Navigate Text Mode (Ctrl+F Style)

**Branch**: `001-line-navigation-mode` | **Date**: 2026-07-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-line-navigation-mode/spec.md`

## Summary

Replace the per-line hint labeling approach with a **Chrome Ctrl+F-style find-and-navigate mode**: activate via `Ctrl+.`, type a search query (≥5 chars), the system finds matching visible text directly via UIA TextPattern.FindText (without loading all text), the user cycles through matches with Tab/Shift+Tab, and presses Enter to navigate the system cursor/focus to the active match and close the overlay. No text selection. No clipboard copy. Just find and jump.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (net10.0-windows)
**Primary Dependencies**: WPF, `Interop.UIAutomationClient` (managed UIA), `System.Windows.Automation` for TextPattern. No third-party libraries, no OCR.
**Storage**: `%APPDATA%\Vimium\config.json` (existing `VimiumConfig`, extended with FindText hotkey)
**Testing**: xUnit (`Vimium.Tests`), `dotnet-coverage`
**Target Platform**: Windows 10+ / Windows 11, x64. Elevated process.
**UIA APIs**: `ITextProvider.GetVisibleRanges()` → `ITextRangeProvider.FindText()` loop (one cross-process COM call per match, bounded by 200-match cap, 3s timeout, 5-char minimum, 150ms debounce). Fallback: `FindAllBuildCache` with `CacheRequest` for element names. Enter: `ITextRangeProvider.ScrollIntoView()` then `Select()`.

## Constitution Check

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. MVVM Separation | ✅ PASS | `SelectionModeViewModel` holds all state (search query, matches, active index, debounce timer). Code-behind limited to window lifecycle and low-level keyboard hook dispatch. No business logic in views. |
| II. Interface-Driven Services | ✅ PASS | `IFindTextProviderService` (renamed from `ITextSourceProviderService`) with query-driven `SearchAsync(IntPtr hWnd, string query, CancellationToken)` method. Existing interfaces reused (`IHintLabelService`, `IKeyListenerService`). |
| III. Testing Standards | ✅ PASS | All new services, models, and ViewModels will have xUnit tests. Coverage target ≥80%. Mock IFindTextProviderService isolates ViewModel tests from UIA COM calls. |
| IV. UX Consistency | ✅ PASS | Search-bar overlay uses same `ForegroundWindow` base class (WS_EX_TRANSPARENT, Topmost, ShowActivated=False). Theme-consistent via ResourceDictionary keys. Distinct hotkey (`Ctrl+.`) with zero overlap with element mode (`Ctrl+;`). Only one overlay at a time. Auto-dismiss on window/content change (FR-009). Enter navigates cursor (not copy — this is find-and-navigate, matching the spec's redesigned interaction contract). |
| V. Performance & Non-Blocking | ✅ PASS | UIA TextPattern.FindText runs on background thread via `Task.Run`. Overlay appears <100ms (just the transparent window + search bar, no text loading). Search debounced 150ms after last keystroke. In-flight searches cancelled via CancellationToken on new input. No synchronous cross-process calls on UI thread. Match count capped at 200. Search scoped to visible viewport only. |

**Constitution note**: Principle IV's "Text selection & copy contract" paragraph in the constitution describes the OLD design (OCR-based, cursor arrows, text selection, copy to clipboard). The spec was redesigned on 2026-07-09 to a simpler find-and-navigate interaction. The constitution should be updated to reflect the redesigned contract (tracked as follow-up — see `.specify/memory/constitution.md` amendment procedure).

## Project Structure

### Source Code (repository root)

```text
src/
├── Vimium/
│   ├── Models/
│   │   ├── SelectionState.cs                 # REWRITE — find-only state (no cursor, no selection)
│   │   ├── SearchMatch.cs                    # REWRITE — remove deprecated fields, keep BoundingRect + Source
│   │   ├── SearchResult.cs                   # KEEP — search result from provider (UIA or element name)
│   │   ├── FindSession.cs                    # NEW — active session: query, matches, window handle
│   │   ├── TextSource.cs                     # DELETE — no longer needed (no "load all text" step)
│   │   └── TextLineRect.cs                   # DELETE — no longer needed (per-match rects from UIA)
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IFindTextProviderService.cs   # RENAMED from ITextSourceProviderService — query-driven
│   │   │   └── IHintLabelService.cs          # Existing (unchanged)
│   │   ├── FindTextProviderService.cs        # REWRITTEN — UIA TextPattern.FindText + element name fallback
│   │   ├── KeyboardHookService.cs            # KEEP — unchanged
│   │   └── ...                               # All other existing services unchanged
│   ├── ViewModels/
│   │   ├── SelectionModeViewModel.cs         # REWRITE — debounce, query-driven search, Enter=navigate
│   │   ├── ShellViewModel.cs                 # MODIFY — wire Ctrl+. to new flow
│   │   └── ...                               # All existing VMs unchanged
│   ├── Views/
│   │   ├── SelectionModeOverlayView.xaml     # ENHANCE — search bar, match highlights (yellow/orange)
│   │   ├── SelectionModeOverlayView.xaml.cs  # ENHANCE — keyboard handling (Tab/Shift+Tab/Enter/Escape)
│   │   └── ...
│   └── ...
├── Vimium.Tests/
│   ├── Services/
│   │   ├── FindTextProviderServiceTest.cs    # NEW — mock UIA TextPattern, test timeout/fallback
│   │   └── ...
│   ├── Models/
│   │   ├── SelectionStateTest.cs             # REWRITE — find-only state tests
│   │   └── ...
│   └── ViewModels/
│       ├── SelectionModeViewModelTest.cs     # REWRITE — debounce, Tab cycling, Enter navigation
│       └── ...
└── NativeMethods/
    └── User32.cs                             # Existing (unchanged)
```

## Architecture Changes

### Removed Components (4 files)
- `TextSource` model — no "load all text" step in Ctrl+F design
- `TextLineRect` struct — per-match bounding rects from UIA, no per-line rects needed
- `ITextSourceProviderService` → renamed to `IFindTextProviderService`
- `TextSourceProviderService` → rewritten as `FindTextProviderService`

### New/Modified Service: FindTextProviderService (query-driven)

Renamed from `TextSourceProviderService`. Instead of extracting all text upfront (load-all-then-search), it accepts a search query and finds only matching text directly:

**Primary path — UIA TextPattern.FindText**:
1. Get `ITextProvider` from the foreground window's root element
2. Call `GetVisibleRanges()` to get only visible-viewport `ITextRangeProvider[]`
3. For each visible range, call `FindText(query, backward=false, startRange)` in a loop to collect all occurrences
4. Each match returns an `ITextRangeProvider` — extract bounding rects via `GetBoundingRectangles()` and text via `GetText(-1)`
5. Cap at 200 matches, enforce 3-second timeout, cancel via `CancellationToken`
6. Return `SearchResult` list with `BoundingRect` and `SearchResultSource.TextPattern`

**Fallback path — UIA Element Names**:
1. If TextPattern is unsupported OR FindText times out (3s):
2. Call `FindAllBuildCache(TreeScope.Descendants, Condition.TrueCondition, cacheRequest)` to get all elements with cached names
3. Client-side filter: `element.Cached.Name.Contains(query, StringComparison.OrdinalIgnoreCase)`
4. Return `SearchResult` list with `Cached.BoundingRectangle` and `SearchResultSource.ElementName`
5. If fallback also returns 0 matches: signal "No text found" to ViewModel → auto-dismiss after 2s

**Interface**:
```csharp
public interface IFindTextProviderService
{
    /// <summary>
    /// Searches for text matches in the visible viewport of the foreground window.
    /// Returns empty list if no matches found or both paths fail.
    /// </summary>
    Task<FindResult> SearchAsync(IntPtr hWnd, string query, CancellationToken ct);
}
```

### Simplified Data Flow (ShellViewModel)
```
Ctrl+.  →  Open SelectionModeOverlayView immediately (empty, loading state)
        →  User types query (≥5 chars, 150ms debounce) 
        →  IFindTextProviderService.SearchAsync(hWnd, query, ct) [background]
        →  Primary: UIA TextPattern.FindText loop (visible ranges, 3s timeout)
        →  Fallback: FindAllBuildCache element name search
        →  Result: FindResult with Matches (max 200)
        →  ViewModel updates: highlights rendered (yellow=all, orange=active)
        →  User: Tab/Shift+Tab to cycle → Enter to navigate cursor to match
        →  Enter: ScrollIntoView() + Select() on active ITextRangeProvider
        →  Overlay closes immediately (no toast)
        →  Auto-dismiss on: Escape, window change (GetForegroundWindow poll), UIA TextChanged
```

### Simplified ViewModel: SelectionModeViewModel
- **Constructor**: Takes `IFindTextProviderService`, window bounds, source hWnd. No ClipboardService.
- **Search debounce**: 150ms `System.Timers.Timer` after last keystroke. CancellationTokenSource to cancel in-flight search on new input.
- **5-char minimum**: Search only triggers when query.Length ≥ 5.
- **State**: `SelectionState` simplified — removes `CursorPosition`, `SelectionStart/End`, `AllVisibleLines`, `VisibleText`. Keeps `SearchQuery`, `SearchMatches`, `ActiveMatchIndex`. Adds `IsSearching` (for loading indicator) and `MatchCount` display ("0 matches", "3 of 15").
- **Input handlers**: `HandleCharacter`, `HandleBackspace`, `HandleTab(bool shift)`, `HandleEnter`, `HandleEscape`, `HandleFocusLost`.
  - REMOVED: `HandleArrow`, `HandleCtrlArrow`, `HandleShiftArrow`, `HandleCtrlShiftArrow`, `HandleHome`, `HandleEnd`, `HandleContentChanged` (simplified — just dismiss on focus lost).
- **Enter action**: Calls `ScrollIntoView()` then `Select()` on the active match's UIA element. No clipboard interaction. Overlay closes immediately.

### Implementation Phases

#### Phase 1: Teardown (ALREADY COMPLETED — verified in working tree)
1. ~~Delete `TextLineHint.cs`, `LineNavigationSession.cs`~~ ✅
2. ~~Delete `LineNavigationOverlayViewModel.cs`~~ ✅
3. ~~Delete `LineNavigationOverlayView.xaml` + `.xaml.cs`~~ ✅
4. ~~Remove deleted file references from .csproj, tests, ShellViewModel, App.xaml.cs~~ ✅

#### Phase 2: Model Cleanup
5. Delete `TextSource.cs` and `TextLineRect.cs` — replaced by query-driven `SearchResult` + `FindSession`
6. Create `FindSession` model (query, matches list, active index, source hWnd, isSearching flag)
7. Rewrite `SearchMatch` — remove deprecated `StartIndex`/`EndIndex`/`LineIndex` fields; keep `SourceText`, `BoundingRect`, `Source`, `IsActive`. Add `AutomationElement` reference for Enter navigation.
8. Rewrite `SelectionState` — remove cursor/selection state; become simple container for search query + match list + active index

#### Phase 3: Service Layer
9. Rename `ITextSourceProviderService` → `IFindTextProviderService` with `SearchAsync(IntPtr hWnd, string query, CancellationToken ct)` returning `FindResult`
10. Rewrite `TextSourceProviderService` → `FindTextProviderService`:
    - Primary: `ITextProvider.GetVisibleRanges()` → `ITextRangeProvider.FindText()` loop (200 cap, 3s timeout)
    - Fallback: `FindAllBuildCache` element name search
    - CancellationToken support for debounce cancellation

#### Phase 4: ViewModel
11. Rewrite `SelectionModeViewModel`:
    - Remove `ClipboardService` dependency, `OnCopied` action, `SelectedText`, `HandleArrow*`, `HandleHome/End`
    - Add `IFindTextProviderService` dependency
    - Add debounce timer (150ms, `System.Timers.Timer`) with `CancellationTokenSource` for in-flight cancellation
    - Add 5-char minimum gate before triggering search
    - Add `IsSearching` property for loading indicator
    - Add `MatchCountText` property ("0 matches", "2 of 5")
    - `HandleEnter`: navigate cursor (ScrollIntoView + Select), close overlay
    - `HandleTab`: cycle active match index (circular wrap)
    - `HandleFocusLost` / `HandleEscape`: dismiss

#### Phase 5: UI
12. Update `SelectionModeOverlayView.xaml`:
    - Search bar with `TextBox` (no focus-steal — keyboard hook captures input)
    - Match count label ("2 of 5" / "0 matches")
    - Loading indicator for `IsSearching`
    - Match highlight rendering: `ItemsControl` bound to `Matches`, each rendered as a `Border` with yellow background (orange if `IsActive`)
13. Update `SelectionModeOverlayView.xaml.cs`:
    - Wire keyboard hook to ViewModel handlers
    - Tab/Shift+Tab → `HandleTab`
    - Enter → `HandleEnter`
    - Escape → `HandleEscape`
    - Focus monitoring per keystroke → `HandleFocusLost`

#### Phase 6: Wiring & Polish
14. Update `ShellViewModel`: wire `Ctrl+.` to open `SelectionModeOverlayView` directly (no text extraction step)
15. Implement window change auto-dismiss: poll `GetForegroundWindow()` on each keyboard event
16. Implement content change auto-dismiss: register UIA `TextChanged` event after first search
17. Add `Microsoft.Extensions.Logging` debug logging for search path selection, timing, match counts
18. Update/rewrite tests to match new architecture
19. Build, test, manual validation (including quickstart.md scenarios)

## Reused Components
- `SearchResult` / `SearchResultSource` — already created with correct fields (Text, BoundingRect, Source enum)
- `HintLabelService` — still used by element mode
- `ConfigService` — existing config fields kept, extended with find-text hotkey
- `KeyboardHookService` — unchanged
- `ForegroundWindow` base class — unchanged (WS_EX_TRANSPARENT, Topmost, ShowActivated=False)
- All element mode components — zero changes
