using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Vimium.Models;

/// <summary>
/// Represents the state of an active find-text operation. Created when the overlay
/// opens, destroyed when it closes. Thin, observable state container backing
/// SelectionModeViewModel.
/// </summary>
public class FindSession : INotifyPropertyChanged
{
    private string _searchQuery = "";
    private IReadOnlyList<SearchMatch> _matches = Array.Empty<SearchMatch>();
    private int _activeMatchIndex;
    private bool _isSearching;

    /// <summary>Handle of the foreground window being searched.</summary>
    public IntPtr SourceWindowHandle { get; }

    public FindSession(IntPtr sourceWindowHandle)
    {
        SourceWindowHandle = sourceWindowHandle;
    }

    /// <summary>Current incremental search string (user-typed characters).</summary>
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value) return;
            _searchQuery = value ?? "";
            OnPropertyChanged();
            OnPropertyChanged(nameof(MatchCountText));
        }
    }

    /// <summary>All occurrences of SearchQuery within the visible viewport (max 200).</summary>
    public IReadOnlyList<SearchMatch> Matches
    {
        get => _matches;
        set
        {
            _matches = value ?? Array.Empty<SearchMatch>();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMatches));
            OnPropertyChanged(nameof(MatchCountText));
        }
    }

    /// <summary>Index into Matches of the currently active match (orange highlight).</summary>
    public int ActiveMatchIndex
    {
        get => _activeMatchIndex;
        set
        {
            if (_activeMatchIndex == value) return;
            _activeMatchIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MatchCountText));
        }
    }

    /// <summary>True while an async search is in-flight (for loading indicator).</summary>
    public bool IsSearching
    {
        get => _isSearching;
        set
        {
            if (_isSearching == value) return;
            _isSearching = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Derived: whether any matches exist.</summary>
    public bool HasMatches => _matches.Count > 0;

    /// <summary>
    /// Derived display string:
    /// "" when the query is empty, "0 matches" when a query has no results,
    /// "2 of 5" when ActiveMatchIndex=1 and count=5.
    /// </summary>
    public string MatchCountText
    {
        get
        {
            if (string.IsNullOrEmpty(_searchQuery)) return "";
            if (_matches.Count == 0) return "0 matches";
            return $"{_activeMatchIndex + 1} of {_matches.Count}";
        }
    }

    // ── INotifyPropertyChanged ───────────────────────────────

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
