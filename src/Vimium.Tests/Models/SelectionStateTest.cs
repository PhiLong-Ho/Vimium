using System;
using System.Collections.Generic;
using System.Windows;
using Vimium.Models;
using Xunit;

namespace Vimium.Tests.Models;

public class SelectionStateTest
{
    private static SearchResult Result(string text) => new SearchResult
    {
        Text = text,
        BoundingRect = new Rect(0, 0, 60, 18),
        Source = SearchResultSource.TextPattern
    };

    private static IReadOnlyList<SearchResult> Results(int n)
    {
        var list = new List<SearchResult>();
        for (int i = 0; i < n; i++) list.Add(Result($"match{i}"));
        return list;
    }

    [Fact]
    public void InitialState_EmptyQueryNoMatches()
    {
        var state = new SelectionState(new IntPtr(1));
        Assert.Equal("", state.SearchQuery);
        Assert.Empty(state.SearchMatches);
        Assert.Equal(0, state.ActiveMatchIndex);
        Assert.False(state.HasMatches);
        Assert.Null(state.ActiveMatch);
    }

    [Fact]
    public void SetMatches_PopulatesAndActivatesFirst()
    {
        var state = new SelectionState(IntPtr.Zero);
        state.SearchQuery = "match";
        state.SetMatches(Results(3));

        Assert.Equal(3, state.SearchMatches.Count);
        Assert.Equal(0, state.ActiveMatchIndex);
        Assert.True(state.SearchMatches[0].IsActive);
        Assert.False(state.SearchMatches[1].IsActive);
        Assert.Equal("match0", state.ActiveMatch!.SourceText);
    }

    [Fact]
    public void SetMatches_Empty_ClearsMatches()
    {
        var state = new SelectionState(IntPtr.Zero);
        state.SetMatches(Results(2));
        state.SetMatches(Array.Empty<SearchResult>());
        Assert.Empty(state.SearchMatches);
        Assert.False(state.HasMatches);
    }

    [Fact]
    public void CycleActive_WrapsForward()
    {
        var state = new SelectionState(IntPtr.Zero);
        state.SetMatches(Results(3));

        Assert.Equal(0, state.ActiveMatchIndex);
        state.CycleActive(false); Assert.Equal(1, state.ActiveMatchIndex);
        state.CycleActive(false); Assert.Equal(2, state.ActiveMatchIndex);
        state.CycleActive(false); Assert.Equal(0, state.ActiveMatchIndex); // wrap
        Assert.True(state.SearchMatches[0].IsActive);
        Assert.False(state.SearchMatches[2].IsActive);
    }

    [Fact]
    public void CycleActive_Shift_WrapsBackward()
    {
        var state = new SelectionState(IntPtr.Zero);
        state.SetMatches(Results(3));

        state.CycleActive(true); // from 0 backward → last
        Assert.Equal(2, state.ActiveMatchIndex);
        Assert.True(state.SearchMatches[2].IsActive);
    }

    [Fact]
    public void CycleActive_NoMatches_NoOp()
    {
        var state = new SelectionState(IntPtr.Zero);
        state.CycleActive(false);
        state.CycleActive(true);
        Assert.Equal(0, state.ActiveMatchIndex);
        Assert.Empty(state.SearchMatches);
    }

    [Fact]
    public void IsSearching_FlagToggles()
    {
        var state = new SelectionState(IntPtr.Zero);
        Assert.False(state.IsSearching);
        state.IsSearching = true;
        Assert.True(state.IsSearching);
    }

    [Fact]
    public void MatchCountText_FormatsCorrectly()
    {
        var state = new SelectionState(IntPtr.Zero);
        Assert.Equal("", state.MatchCountText);

        state.SearchQuery = "hello";
        Assert.Equal("0 matches", state.MatchCountText);

        state.SetMatches(Results(5));
        Assert.Equal("1 of 5", state.MatchCountText);

        state.CycleActive(false);
        Assert.Equal("2 of 5", state.MatchCountText);
    }
}
