# Interface Contracts: Find-and-Navigate Text Mode (Ctrl+F Style)

**Feature**: Find-and-Navigate Text Mode | **Date**: 2026-07-09

## Service Interfaces

### IFindTextProviderService (renamed from ITextSourceProviderService)

```csharp
namespace Vimium.Services.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;
using Vimium.Models;

/// <summary>
/// Searches for text matches in the visible viewport of the foreground window
/// using UIA TextPattern.FindText (primary) with element-name cache fallback.
/// </summary>
public interface IFindTextProviderService
{
    /// <summary>
    /// Searches for <paramref name="query"/> in the visible text of the given window.
    /// Scoped to the visible viewport only (offscreen text excluded).
    /// 
    /// Primary path: ITextProvider.GetVisibleRanges() → ITextRangeProvider.FindText() loop
    ///   - Each FindText call is cross-process COM, bounded by 200-match cap
    ///   - 3-second timeout enforced via <paramref name="ct"/>
    ///   - Returns SearchResult with Source=TextPattern, accurate bounding rects,
    ///     and ITextRangeProvider reference for ScrollIntoView+Select on Enter
    ///
    /// Fallback path (if TextPattern unsupported or timeout):
    ///   - FindAllBuildCache(TreeScope.Descendants, Condition.TrueCondition, cacheRequest)
    ///   - Client-side case-insensitive Contains filter on Cached.Name
    ///   - Returns SearchResult with Source=ElementName, Cached.BoundingRectangle,
    ///     and AutomationElement reference for SetFocus on Enter
    ///
    /// Both paths fail: returns FindResult with empty Matches
    /// </summary>
    /// <param name="hWnd">Handle of the foreground window to search.</param>
    /// <param name="query">The search string. Must be non-empty.</param>
    /// <param name="ct">Cancellation token for timeout and debounce cancellation.</param>
    /// <returns>FindResult with matches (max 200), source info, and timing data.</returns>
    Task<FindResult> SearchAsync(IntPtr hWnd, string query, CancellationToken ct);
}
```

### Removed Interface

- **ITextSourceProviderService** — renamed to `IFindTextProviderService`. The old `GetTextSource(IntPtr)` / `GetTextSourceAsync(IntPtr)` methods that returned a full `TextSource` (all text loaded upfront) are replaced by the single query-driven `SearchAsync(IntPtr, string, CancellationToken)` method.

## ViewModel Contracts

### SelectionModeViewModel

```csharp
namespace Vimium.ViewModels;

using System;
using System.Collections.Generic;
using System.Windows;
using Vimium.Models;
using Vimium.Services.Interfaces;

internal class SelectionModeViewModel : NotifyPropertyChanged
{
    // ── Constructor ─────────────────────────────────────────────

    /// <summary>
    /// Creates the ViewModel for find-and-navigate mode.
    /// </summary>
    /// <param name="findTextService">Service for query-driven UIA text search.</param>
    /// <param name="windowBounds">Bounding rectangle of the foreground window.</param>
    /// <param name="sourceWindow">Handle of the foreground window.</param>
    public SelectionModeViewModel(
        IFindTextProviderService findTextService,
        Rect windowBounds,
        IntPtr sourceWindow);

    // ── Properties (bound by XAML) ──────────────────────────────

    public string SearchQuery { get; }          // Current search text (for search bar display)
    public IReadOnlyList<SearchMatch> Matches { get; }  // All matches for highlight rendering
    public int ActiveMatchIndex { get; }        // Index of active match (orange)
    public Rect WindowBounds { get; }           // Foreground window bounds
    public bool IsSearching { get; }            // True during async search (loading indicator)
    public string MatchCountText { get; }       // "0 matches" / "2 of 5" / "" (empty when no query)

    // ── Lifecycle actions (set by view) ─────────────────────────

    public Action CloseOverlay { get; set; }    // Called to dismiss the overlay window

    // ── Input handlers (called by view code-behind / keyboard hook) ──

    public void HandleCharacter(char c);        // Append to search query, reset debounce timer
    public void HandleBackspace();              // Remove last char, reset debounce
    public void HandleTab(bool shift);          // Cycle active match forward (false) or backward (true)
    public void HandleEnter();                  // Navigate cursor to active match, close overlay
    public void HandleEscape();                 // Dismiss overlay without navigation
    public void HandleFocusLost();              // Window change detected → auto-dismiss
}
```

### Keyboard Input Contract

The overlay captures keyboard input via a low-level keyboard hook (`WH_KEYBOARD_LL`). The following key bindings apply:

| Key | Handler | Behavior |
|-----|---------|----------|
| Printable characters (a-z, 0-9, symbols, space) | `HandleCharacter(c)` | Append to search query. Triggers debounced search (150ms delay, ≥5 chars). |
| Backspace | `HandleBackspace()` | Remove last character. Triggers debounced search if remaining ≥5 chars. Clears matches if <5 chars. |
| Tab | `HandleTab(shift: false)` | Advance to next match (circular wrap). No-op if no matches. |
| Shift+Tab | `HandleTab(shift: true)` | Go to previous match (circular wrap). No-op if no matches. |
| Enter | `HandleEnter()` | Navigate cursor (ScrollIntoView + Select on active `ITextRangeProvider`), close overlay. No-op if no active match. |
| Escape | `HandleEscape()` | Dismiss overlay without navigation. Works in any state. |

**Deliberately NOT captured** (ignored, pass through to underlying window):
- Arrow keys (← → ↑ ↓) — removed per spec redesign
- Ctrl+Arrow — removed
- Shift+Arrow — removed
- Home / End — removed
- Ctrl+C / Ctrl+V / other Ctrl combos — pass through
- Alt combos — pass through

**Auto-dismiss triggers** (checked on each keyboard event):
- `GetForegroundWindow() != sourceHwnd` → `HandleFocusLost()`
- UIA `TextChanged` event fired since last interaction → `HandleFocusLost()`

## UI Contract: SelectionModeOverlayView

The overlay window MUST:

1. **Position**: Cover the foreground window bounds exactly. Update position if the window moves (poll on each keyboard event).
2. **Transparency**: Use `WS_EX_TRANSPARENT` so mouse clicks pass through to the underlying window. The overlay never receives focus.
3. **Search bar**: Positioned at the bottom of the overlay. Contains:
   - Input text display (read-only, shows `SearchQuery` with a blinking cursor)
   - Match count label (shows `MatchCountText`: "0 matches" / "2 of 5" / empty)
   - Loading spinner (visible when `IsSearching == true`)
4. **Match highlights** (rendered when `Matches.Count > 0`):
   - Each `SearchMatch` with `IsActive == false`: yellow semi-transparent border/background at `BoundingRect`
   - The one `SearchMatch` with `IsActive == true`: orange semi-transparent border/background at `BoundingRect`
   - Highlights are `Border` elements positioned absolutely within the overlay canvas
5. **Theme**: All colors (yellow, orange, search bar background, text color) MUST come from the active `ResourceDictionary` theme. No hardcoded colors.

## Removed Contracts

- **ILineHintProviderService** — already deleted. Was a per-line hint enumeration interface. No equivalent in the new design.
- **ClipboardService dependency** — removed from SelectionModeViewModel. The redesigned feature does not interact with the clipboard.
- **OnCopied callback** — removed. No copy functionality in the redesigned feature.
