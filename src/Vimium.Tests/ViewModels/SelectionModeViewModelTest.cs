using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Vimium.Models;
using Vimium.Services.Interfaces;
using Vimium.ViewModels;
using Xunit;

namespace Vimium.Tests.ViewModels;

public class SelectionModeViewModelTest
{
    /// <summary>Records calls and returns a configurable result after an optional delay.</summary>
    private sealed class FakeFindService : IFindTextProviderService
    {
        public int CallCount;
        public string LastQuery = "";
        public int MatchCount = 3;
        public int DelayMs = 0;
        public bool ReturnEmpty = false;

        public async Task<FindResult> SearchAsync(IntPtr hWnd, string query, CancellationToken ct)
        {
            Interlocked.Increment(ref CallCount);
            LastQuery = query;
            if (DelayMs > 0) await Task.Delay(DelayMs, ct);
            ct.ThrowIfCancellationRequested();

            if (ReturnEmpty)
                return FindResult.Empty();

            var matches = new List<SearchResult>();
            for (int i = 0; i < MatchCount; i++)
                matches.Add(new SearchResult
                {
                    Text = query,
                    BoundingRect = new Rect(0, i * 20, 60, 18),
                    Source = SearchResultSource.TextPattern
                });
            return new FindResult { Matches = matches, Source = SearchResultSource.TextPattern };
        }
    }

    private static SelectionModeViewModel CreateVm(FakeFindService svc) =>
        new SelectionModeViewModel(svc, new Rect(0, 0, 800, 600), new IntPtr(1));

    private static void Type(SelectionModeViewModel vm, string s)
    {
        foreach (var c in s) vm.HandleCharacter(c);
    }

    [Fact]
    public async Task HandleCharacter_Below5Chars_NoSearchTriggered()
    {
        var svc = new FakeFindService();
        var vm = CreateVm(svc);

        Type(vm, "abcd"); // 4 chars
        await Task.Delay(250);

        Assert.Equal(0, svc.CallCount);
        Assert.Empty(vm.Matches);
    }

    [Fact]
    public async Task HandleCharacter_At5Chars_SearchTriggeredAfterDebounce()
    {
        var svc = new FakeFindService { MatchCount = 2 };
        var vm = CreateVm(svc);

        Type(vm, "abcde"); // 5 chars
        await Task.Delay(400);

        Assert.Equal(1, svc.CallCount);
        Assert.Equal("abcde", svc.LastQuery);
        Assert.Equal(2, vm.Matches.Count);
    }

    [Fact]
    public async Task HandleCharacter_RapidTyping_TriggersSingleSearch()
    {
        var svc = new FakeFindService();
        var vm = CreateVm(svc);

        // Type quickly (well under the 150ms debounce between keystrokes)
        Type(vm, "singapore");
        await Task.Delay(400);

        Assert.Equal(1, svc.CallCount); // debounced to a single call
        Assert.Equal("singapore", svc.LastQuery);
    }

    [Fact]
    public async Task HandleTab_CyclesForwardWithWrap()
    {
        var svc = new FakeFindService { MatchCount = 3 };
        var vm = CreateVm(svc);
        Type(vm, "abcde");
        await Task.Delay(400);

        Assert.Equal(0, vm.ActiveMatchIndex);
        vm.HandleTab(false); Assert.Equal(1, vm.ActiveMatchIndex);
        vm.HandleTab(false); Assert.Equal(2, vm.ActiveMatchIndex);
        vm.HandleTab(false); Assert.Equal(0, vm.ActiveMatchIndex); // wrap
    }

    [Fact]
    public async Task HandleTab_Shift_CyclesBackwardWithWrap()
    {
        var svc = new FakeFindService { MatchCount = 3 };
        var vm = CreateVm(svc);
        Type(vm, "abcde");
        await Task.Delay(400);

        vm.HandleTab(true); // 0 → last
        Assert.Equal(2, vm.ActiveMatchIndex);
    }

    [Fact]
    public void HandleTab_NoMatches_NoOp()
    {
        var svc = new FakeFindService();
        var vm = CreateVm(svc);
        vm.HandleTab(false); // no exception, no state change
        Assert.Equal(0, vm.ActiveMatchIndex);
        Assert.Empty(vm.Matches);
    }

    [Fact]
    public async Task HandleEnter_WithActiveMatch_Closes()
    {
        var svc = new FakeFindService { MatchCount = 2 };
        var vm = CreateVm(svc);
        bool closed = false;
        vm.CloseOverlay = () => closed = true;

        Type(vm, "abcde");
        await Task.Delay(400);
        vm.HandleEnter();

        Assert.True(closed);
    }

    [Fact]
    public void HandleEnter_NoMatches_NoOp()
    {
        var svc = new FakeFindService();
        var vm = CreateVm(svc);
        bool closed = false;
        vm.CloseOverlay = () => closed = true;

        vm.HandleEnter(); // no active match → no navigation, no close
        Assert.False(closed);
    }

    [Fact]
    public void HandleEscape_DismissesWithoutNavigation()
    {
        var svc = new FakeFindService();
        var vm = CreateVm(svc);
        bool closed = false;
        vm.CloseOverlay = () => closed = true;

        vm.HandleEscape();
        Assert.True(closed);
    }

    [Fact]
    public async Task HandleBackspace_Below5Chars_ClearsMatches()
    {
        var svc = new FakeFindService { MatchCount = 3 };
        var vm = CreateVm(svc);

        Type(vm, "abcde");
        await Task.Delay(400);
        Assert.NotEmpty(vm.Matches);

        vm.HandleBackspace(); // "abcd" → below 5
        Assert.Empty(vm.Matches);
        Assert.Equal("abcd", vm.SearchQuery);
    }

    [Fact]
    public void HandleBackspace_EmptyQuery_NoOp()
    {
        var svc = new FakeFindService();
        var vm = CreateVm(svc);
        vm.HandleBackspace();
        Assert.Equal("", vm.SearchQuery);
    }

    [Fact]
    public void HandleFocusLost_DismissesImmediately()
    {
        var svc = new FakeFindService();
        var vm = CreateVm(svc);
        bool closed = false;
        vm.CloseOverlay = () => closed = true;

        vm.HandleFocusLost();
        Assert.True(closed);
    }

    [Fact]
    public async Task MatchCountText_UpdatesWithSearchResults()
    {
        var svc = new FakeFindService { MatchCount = 4 };
        var vm = CreateVm(svc);
        Type(vm, "abcde");
        await Task.Delay(400);

        Assert.Equal("1 of 4", vm.MatchCountText);
    }

    [Fact]
    public async Task Search_NoMatches_SetsZeroMatchesText()
    {
        var svc = new FakeFindService { ReturnEmpty = true };
        var vm = CreateVm(svc);
        Type(vm, "abcde");
        await Task.Delay(400);

        Assert.Empty(vm.Matches);
        Assert.Equal("0 matches", vm.MatchCountText);
    }

    [Fact]
    public async Task Backspace_StillAbove5Chars_ReSearches()
    {
        var svc = new FakeFindService { MatchCount = 1 };
        var vm = CreateVm(svc);
        Type(vm, "abcdef"); // 6 chars
        await Task.Delay(400);
        int firstCalls = svc.CallCount;

        vm.HandleBackspace(); // "abcde" — still ≥5, should re-search
        await Task.Delay(400);

        Assert.True(svc.CallCount > firstCalls);
        Assert.Equal("abcde", svc.LastQuery);
    }
}
