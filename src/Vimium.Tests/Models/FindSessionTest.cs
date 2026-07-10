using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Vimium.Models;
using Xunit;

namespace Vimium.Tests.Models;

public class FindSessionTest
{
    private static SearchMatch Match(string text = "hello", bool active = false) => new SearchMatch
    {
        SourceText = text,
        BoundingRect = new Rect(0, 0, 50, 18),
        Source = SearchResultSource.TextPattern,
        IsActive = active
    };

    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var s = new FindSession(new IntPtr(42));
        Assert.Equal("", s.SearchQuery);
        Assert.Empty(s.Matches);
        Assert.False(s.IsSearching);
        Assert.Equal(0, s.ActiveMatchIndex);
        Assert.Equal(new IntPtr(42), s.SourceWindowHandle);
    }

    [Fact]
    public void HasMatches_FalseWhenEmpty()
    {
        var s = new FindSession(IntPtr.Zero);
        Assert.False(s.HasMatches);

        s.Matches = new List<SearchMatch> { Match() };
        Assert.True(s.HasMatches);
    }

    [Fact]
    public void MatchCountText_FormatsAllStates()
    {
        var s = new FindSession(IntPtr.Zero);

        // Empty query → ""
        Assert.Equal("", s.MatchCountText);

        // Query set, no matches → "0 matches"
        s.SearchQuery = "hello";
        Assert.Equal("0 matches", s.MatchCountText);

        // One match → "1 of 1"
        s.Matches = new List<SearchMatch> { Match() };
        Assert.Equal("1 of 1", s.MatchCountText);

        // Five matches, active index 1 → "2 of 5"
        s.Matches = new List<SearchMatch> { Match(), Match(), Match(), Match(), Match() };
        s.ActiveMatchIndex = 1;
        Assert.Equal("2 of 5", s.MatchCountText);
    }

    [Fact]
    public void PropertyChanged_RaisedOnSetters()
    {
        var s = new FindSession(IntPtr.Zero);
        var changed = new List<string>();
        s.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? "");

        s.SearchQuery = "abcde";
        s.Matches = new List<SearchMatch> { Match() };
        s.ActiveMatchIndex = 0; // no change (already 0) — should NOT raise
        s.IsSearching = true;

        Assert.Contains(nameof(FindSession.SearchQuery), changed);
        Assert.Contains(nameof(FindSession.Matches), changed);
        Assert.Contains(nameof(FindSession.IsSearching), changed);
        Assert.Contains(nameof(FindSession.MatchCountText), changed);
        Assert.Contains(nameof(FindSession.HasMatches), changed);
    }

    [Fact]
    public void ActiveMatchIndex_NoChange_DoesNotRaise()
    {
        var s = new FindSession(IntPtr.Zero);
        bool raised = false;
        s.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(FindSession.ActiveMatchIndex)) raised = true; };
        s.ActiveMatchIndex = 0; // already 0
        Assert.False(raised);
    }
}
