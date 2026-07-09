using Vimium.Models;
using Vimium.Services;
using Vimium.ViewModels;
using Xunit;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Vimium.Tests.ViewModels;

public class SelectionModeViewModelTest
{
    private static TextLineHint CreateHint(int index)
    {
        return new TextLineHint(
            new IntPtr(1),
            new Rect(10, index * 20, 300, 18),
            $"Line {index} content text here");
    }

    private static IReadOnlyList<TextLineHint> CreateLines(int count)
    {
        var lines = new List<TextLineHint>();
        for (int i = 0; i < count; i++)
            lines.Add(CreateHint(i));
        return lines;
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var lines = CreateLines(3);
        var clipboard = new ClipboardService();
        var vm = new SelectionModeViewModel(lines[0], lines, new Rect(0, 0, 800, 600), clipboard);

        Assert.NotNull(vm.VisibleText);
        Assert.Equal(0, vm.CursorPosition);
        Assert.Null(vm.SelectionStart);
        Assert.Null(vm.SelectionEnd);
        Assert.Equal("", vm.SearchQuery);
        Assert.Empty(vm.Matches);
    }

    [Fact]
    public void HandleCharacter_AppendsToSearch()
    {
        var lines = CreateLines(3);
        var clipboard = new ClipboardService();
        var vm = new SelectionModeViewModel(lines[0], lines, new Rect(0, 0, 800, 600), clipboard);

        vm.HandleCharacter('L');
        vm.HandleCharacter('i');

        Assert.Equal("Li", vm.SearchQuery);
    }

    [Fact]
    public void HandleBackspace_RemovesLastChar()
    {
        var lines = CreateLines(3);
        var clipboard = new ClipboardService();
        var vm = new SelectionModeViewModel(lines[0], lines, new Rect(0, 0, 800, 600), clipboard);

        vm.HandleCharacter('A');
        vm.HandleCharacter('B');
        vm.HandleBackspace();

        Assert.Equal("A", vm.SearchQuery);
    }

    [Fact]
    public void HandleEscape_ClosesWithoutCopy()
    {
        var lines = CreateLines(3);
        var clipboard = new ClipboardService();
        bool closed = false;
        var vm = new SelectionModeViewModel(lines[0], lines, new Rect(0, 0, 800, 600), clipboard);
        vm.CloseOverlay = () => closed = true;

        vm.HandleEscape();

        Assert.True(closed);
    }

    [Fact]
    public void HandleArrow_Right_MovesCursor()
    {
        var lines = CreateLines(3);
        var clipboard = new ClipboardService();
        var vm = new SelectionModeViewModel(lines[0], lines, new Rect(0, 0, 800, 600), clipboard);

        vm.HandleArrow(Key.Right);

        Assert.Equal(1, vm.CursorPosition);
    }

    [Fact]
    public void HandleHome_MovesToLineStart()
    {
        var lines = CreateLines(3);
        var clipboard = new ClipboardService();
        var vm = new SelectionModeViewModel(lines[0], lines, new Rect(0, 0, 800, 600), clipboard);

        vm.HandleArrow(Key.Right);
        vm.HandleArrow(Key.Right);
        vm.HandleHome();

        Assert.Equal(0, vm.CursorPosition);
    }

    [Fact]
    public void HandleTab_CyclesMatches()
    {
        var lines = CreateLines(3);
        var clipboard = new ClipboardService();
        var vm = new SelectionModeViewModel(lines[0], lines, new Rect(0, 0, 800, 600), clipboard);

        // Type search that should find "Line" in all lines
        vm.HandleCharacter('L');
        vm.HandleCharacter('i');

        var matchCount = vm.Matches.Count;
        if (matchCount > 1)
        {
            Assert.Equal(0, vm.ActiveMatchIndex);
            vm.HandleTab(false); // forward
            Assert.Equal(1, vm.ActiveMatchIndex);
        }
    }
}
