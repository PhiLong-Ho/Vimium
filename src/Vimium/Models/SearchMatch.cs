using System;
using System.Windows;
using Interop.UIAutomationClient;

namespace Vimium.Models;

/// <summary>
/// A single occurrence of the search phrase within the visible text of the foreground window.
/// Contains everything needed to render a highlight and navigate the cursor on Enter.
/// </summary>
public class SearchMatch
{
    /// <summary>The matched text content (e.g., "Singapore").</summary>
    public string SourceText { get; set; } = "";

    /// <summary>
    /// Accurate bounding rectangle in window coordinates from UIA.
    /// Used directly for highlight rendering — no estimation.
    /// </summary>
    public Rect BoundingRect { get; set; }

    /// <summary>Where this match came from: TextPattern (primary) or ElementName (fallback).</summary>
    public SearchResultSource Source { get; set; }

    /// <summary>
    /// Whether this match is the currently active one (highlighted orange vs yellow).
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The UIA text range for this match (null for ElementName fallback).
    /// Used on Enter for ScrollIntoView + Select.
    /// </summary>
    public IUIAutomationTextRange TextRangeProvider { get; set; }

    /// <summary>
    /// The containing UIA element (non-null for ElementName source; used for SetFocus on Enter).
    /// </summary>
    public IUIAutomationElement AutomationElement { get; set; }

    /// <summary>
    /// Validates required invariants: SourceText must be non-empty and BoundingRect
    /// must have positive width and height.
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrEmpty(SourceText) &&
        BoundingRect.Width > 0 &&
        BoundingRect.Height > 0;

    /// <summary>
    /// Creates a SearchMatch from a provider SearchResult.
    /// </summary>
    public static SearchMatch FromResult(SearchResult result, bool isActive)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        return new SearchMatch
        {
            SourceText = result.Text,
            BoundingRect = result.BoundingRect,
            Source = result.Source,
            TextRangeProvider = result.TextRangeProvider,
            AutomationElement = result.AutomationElement,
            IsActive = isActive
        };
    }
}
