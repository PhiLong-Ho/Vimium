namespace Vimium.Models;

/// <summary>
/// Represents a single occurrence of a search phrase within the visible text.
/// </summary>
public class SearchMatch
{
    /// <summary>
    /// Character offset within SelectionState.VisibleText where the match begins.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Character offset where the match ends (exclusive).
    /// </summary>
    public int EndIndex { get; set; }

    /// <summary>
    /// Index into AllVisibleLines for the line containing this match.
    /// </summary>
    public int LineIndex { get; set; }

    /// <summary>
    /// Whether this match is the currently active one (highlighted distinctly).
    /// </summary>
    public bool IsActive { get; set; }
}
