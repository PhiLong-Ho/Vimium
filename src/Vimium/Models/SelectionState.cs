using System;
using System.Collections.Generic;

namespace Vimium.Models;

/// <summary>
/// Thin find-only state container. Wraps a <see cref="FindSession"/> and exposes
/// the search query, matches, active index, searching flag, and derived match-count text.
/// Cursor position, selection ranges, and full-text extraction were removed in the
/// 2026-07-09 Ctrl+F redesign.
/// </summary>
public class SelectionState
{
    private readonly FindSession _session;

    public SelectionState(IntPtr sourceWindowHandle)
    {
        _session = new FindSession(sourceWindowHandle);
    }

    /// <summary>Underlying observable session state.</summary>
    public FindSession Session => _session;

    public string SearchQuery
    {
        get => _session.SearchQuery;
        set => _session.SearchQuery = value;
    }

    public IReadOnlyList<SearchMatch> SearchMatches
    {
        get => _session.Matches;
        set => _session.Matches = value;
    }

    public int ActiveMatchIndex
    {
        get => _session.ActiveMatchIndex;
        set => _session.ActiveMatchIndex = value;
    }

    public bool IsSearching
    {
        get => _session.IsSearching;
        set => _session.IsSearching = value;
    }

    public bool HasMatches => _session.HasMatches;

    public string MatchCountText => _session.MatchCountText;

    /// <summary>The currently active match, or null when there are no matches.</summary>
    public SearchMatch ActiveMatch =>
        _session.HasMatches && _session.ActiveMatchIndex >= 0 && _session.ActiveMatchIndex < _session.Matches.Count
            ? _session.Matches[_session.ActiveMatchIndex]
            : null;

    /// <summary>
    /// Replaces the current matches from provider results, activating the first match.
    /// Clears matches when the list is empty.
    /// </summary>
    public void SetMatches(IReadOnlyList<SearchResult> results)
    {
        if (results == null || results.Count == 0)
        {
            ClearMatches();
            return;
        }

        var matches = new List<SearchMatch>(results.Count);
        for (int i = 0; i < results.Count; i++)
            matches.Add(SearchMatch.FromResult(results[i], isActive: i == 0));

        _session.Matches = matches;
        _session.ActiveMatchIndex = 0;
    }

    /// <summary>Clears all matches and resets the active index.</summary>
    public void ClearMatches()
    {
        _session.Matches = Array.Empty<SearchMatch>();
        _session.ActiveMatchIndex = 0;
    }

    /// <summary>
    /// Cycles the active match with circular wrap. Forward (shift=false):
    /// (index + 1) % count. Backward (shift=true): (index - 1 + count) % count.
    /// No-op when there are no matches.
    /// </summary>
    public void CycleActive(bool shift)
    {
        int count = _session.Matches.Count;
        if (count == 0) return;

        int newIndex = shift
            ? (_session.ActiveMatchIndex - 1 + count) % count
            : (_session.ActiveMatchIndex + 1) % count;

        for (int i = 0; i < count; i++)
            _session.Matches[i].IsActive = (i == newIndex);

        _session.ActiveMatchIndex = newIndex;
    }
}
