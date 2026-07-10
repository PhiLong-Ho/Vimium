using System;
using System.Collections.Generic;
using System.Windows;
using Interop.UIAutomationClient;

namespace Vimium.Models;

/// <summary>
/// Source of a search result: element name from cache, or body text from TextPattern.FindText.
/// </summary>
public enum SearchResultSource
{
    /// <summary>Match found via FindAllBuildCache element name search. Has element-level
    /// bounding rect and element reference for SetFocus on Enter.</summary>
    ElementName,

    /// <summary>Match found via ITextProvider.GetVisibleRanges() → FindText(). Has accurate
    /// bounding rect and text range for ScrollIntoView + Select on Enter.</summary>
    TextPattern
}

/// <summary>
/// Raw search result produced by <see cref="Vimium.Services.Interfaces.IFindTextProviderService"/>.
/// Lightweight DTO — transformed into <see cref="SearchMatch"/> by the ViewModel (adding IsActive state).
/// </summary>
public class SearchResult
{
    /// <summary>The matched text content.</summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Accurate bounding rectangle in window coordinates (from UIA TextRange
    /// GetBoundingRectangles or element CachedBoundingRectangle).
    /// </summary>
    public Rect BoundingRect { get; set; }

    /// <summary>Where this result came from.</summary>
    public SearchResultSource Source { get; set; }

    /// <summary>
    /// UIA text range reference for Enter navigation (null for ElementName source).
    /// Used for ScrollIntoView + Select.
    /// </summary>
    public IUIAutomationTextRange TextRangeProvider { get; set; }

    /// <summary>
    /// The containing UIA element (non-null for ElementName source; used for SetFocus on Enter).
    /// </summary>
    public IUIAutomationElement AutomationElement { get; set; }
}

/// <summary>
/// Container returned by <see cref="Vimium.Services.Interfaces.IFindTextProviderService.SearchAsync"/>.
/// </summary>
public class FindResult
{
    /// <summary>Ordered list of search results (up to 200), in document order.</summary>
    public IReadOnlyList<SearchResult> Matches { get; set; } = Array.Empty<SearchResult>();

    /// <summary>Which provider produced the results.</summary>
    public SearchResultSource Source { get; set; }

    /// <summary>True if the 3-second timeout expired (partial results may be present).</summary>
    public bool TimedOut { get; set; }

    /// <summary>Actual search elapsed time in milliseconds (for debug logging).</summary>
    public long ElapsedMs { get; set; }

    /// <summary>Convenience: an empty result (both paths failed or no matches).</summary>
    public static FindResult Empty(SearchResultSource source = SearchResultSource.TextPattern, long elapsedMs = 0) =>
        new FindResult { Matches = Array.Empty<SearchResult>(), Source = source, ElapsedMs = elapsedMs };
}
