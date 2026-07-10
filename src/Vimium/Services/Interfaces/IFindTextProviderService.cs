using System;
using System.Threading;
using System.Threading.Tasks;
using Vimium.Models;

namespace Vimium.Services.Interfaces;

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
    ///   - Each FindText call is cross-process COM, bounded by a 200-match cap
    ///   - 3-second timeout enforced internally (falls back to element-name search)
    ///   - Returns SearchResult with Source=TextPattern, accurate bounding rects,
    ///     and IUIAutomationTextRange reference for ScrollIntoView+Select on Enter
    ///
    /// Fallback path (if TextPattern unsupported or timeout):
    ///   - FindAllBuildCache(TreeScope.Descendants, TrueCondition, cacheRequest)
    ///   - Client-side case-insensitive Contains filter on Cached.Name
    ///   - Returns SearchResult with Source=ElementName, Cached.BoundingRectangle,
    ///     and AutomationElement reference for SetFocus on Enter
    ///
    /// Both paths fail: returns FindResult with empty Matches.
    /// </summary>
    /// <param name="hWnd">Handle of the foreground window to search.</param>
    /// <param name="query">The search string. Must be non-empty.</param>
    /// <param name="ct">Cancellation token for debounce cancellation.</param>
    /// <returns>FindResult with matches (max 200), source info, and timing data.</returns>
    Task<FindResult> SearchAsync(IntPtr hWnd, string query, CancellationToken ct);
}
