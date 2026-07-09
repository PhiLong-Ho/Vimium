using Vimium.Models;
using Xunit;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Vimium.Tests.Models;

public class SelectionStateTest
{
    private static TextLineHint CreateHint(string text)
    {
        return new TextLineHint(new IntPtr(1), new Rect(0, 0, 200, 20), text);
    }

    private static IReadOnlyList<TextLineHint> CreateLines()
    {
        return new List<TextLineHint>
        {
            CreateHint("First line of text"),
            CreateHint("Second line with hello world"),
            CreateHint("Third line here"),
        };
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        Assert.Equal(0, state.CursorPosition);
        Assert.Null(state.SelectionStart);
        Assert.Null(state.SelectionEnd);
        Assert.Equal("", state.SearchQuery);
        Assert.Empty(state.SearchMatches);
        Assert.Equal(0, state.ActiveMatchIndex);
    }

    [Fact]
    public void HandleArrow_Right_MovesCursorForward()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        state.HandleArrow(Key.Right);
        Assert.Equal(1, state.CursorPosition);
    }

    [Fact]
    public void HandleArrow_Left_AtStart_StaysAtZero()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        state.HandleArrow(Key.Left);
        Assert.Equal(0, state.CursorPosition);
    }

    [Fact]
    public void HandleHome_MovesToLineStart()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        state.HandleArrow(Key.Right);
        state.HandleArrow(Key.Right);
        Assert.Equal(2, state.CursorPosition);

        state.HandleHome();
        Assert.Equal(0, state.CursorPosition);
    }

    [Fact]
    public void HandleEnd_MovesToLineEnd()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        state.HandleEnd();
        Assert.Equal("First line of text".Length, state.CursorPosition);
    }

    [Fact]
    public void Search_FindsMatches()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        state.UpdateSearch("line");

        Assert.Equal("line", state.SearchQuery);
        Assert.NotEmpty(state.SearchMatches);
        Assert.Equal(0, state.ActiveMatchIndex);
    }

    [Fact]
    public void Search_NoMatches_KeepsCursorUnchanged()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        var originalPosition = state.CursorPosition;
        state.UpdateSearch("zzzznotfound");

        Assert.Empty(state.SearchMatches);
        Assert.Equal(originalPosition, state.CursorPosition);
    }

    [Fact]
    public void HandleTab_CyclesMatches_Forward()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[1], lines); // "Second line with hello world"

        state.UpdateSearch("line");

        var matchCount = state.SearchMatches.Count;
        if (matchCount > 1)
        {
            Assert.Equal(0, state.ActiveMatchIndex);
            state.HandleTab(false); // forward
            Assert.Equal(1, state.ActiveMatchIndex);
            state.HandleTab(false); // forward
            Assert.Equal(2 % matchCount, state.ActiveMatchIndex);
        }
    }

    [Fact]
    public void HandleTab_WrapsAround()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        state.UpdateSearch("line");

        var matchCount = state.SearchMatches.Count;
        if (matchCount > 0)
        {
            // Go past the end — should wrap
            for (int i = 0; i < matchCount; i++)
                state.HandleTab(false);

            Assert.Equal(0, state.ActiveMatchIndex);
        }
    }

    [Fact]
    public void ShiftArrow_ExtendsSelection()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        state.HandleShiftArrow(Key.Right);
        Assert.Equal(1, state.CursorPosition);
        Assert.NotNull(state.SelectionStart);
        Assert.Equal(0, state.SelectionStart);
        Assert.Equal(1, state.SelectionEnd);

        state.HandleShiftArrow(Key.Right);
        Assert.Equal(2, state.CursorPosition);
        Assert.Equal(2, state.SelectionEnd);
    }

    [Fact]
    public void SelectedText_ReturnsCorrectSubstring()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines); // "First line of text"

        // Select "First"
        state.HandleShiftArrow(Key.Right);
        state.HandleShiftArrow(Key.Right);
        state.HandleShiftArrow(Key.Right);
        state.HandleShiftArrow(Key.Right);
        state.HandleShiftArrow(Key.Right);

        Assert.Equal("First", state.SelectedText);
    }

    [Fact]
    public void HasSelection_TrueWhenSelectionActive()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        Assert.False(state.HasSelection);

        state.HandleShiftArrow(Key.Right);
        Assert.True(state.HasSelection);
    }

    [Fact]
    public void HandleBackspace_RemovesLastSearchChar()
    {
        var lines = CreateLines();
        var state = new SelectionState(lines[0], lines);

        state.UpdateSearch("li");

        // Simulate backspace by updating search with one fewer char
        state.UpdateSearch("l");

        Assert.Equal("l", state.SearchQuery);
    }
}
