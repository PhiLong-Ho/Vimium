using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Interop.UIAutomationClient;
using Vimium.Models;
using Vimium.Services;
using Vimium.Services.Interfaces;

namespace Vimium.ViewModels;

/// <summary>
/// ViewModel for find-and-navigate mode (Chrome Ctrl+F style).
/// Opens with an empty search bar, debounces keystrokes (150ms), triggers a
/// query-driven UIA search once the query reaches 5 characters, cycles matches
/// with Tab/Shift+Tab, and navigates the cursor to the active match on Enter.
/// </summary>
internal class SelectionModeViewModel : NotifyPropertyChanged, IDisposable
{
    private const int MinQueryLength = 5;
    private const int MaxQueryLength = 200;
    private const int DebounceMs = 150;
    private const int NoTextDismissMs = 2000;

    private readonly IFindTextProviderService _findTextService;
    private readonly System.Timers.Timer _debounceTimer;
    private readonly object _searchLock = new object();

    private SelectionState _state;
    private Rect _windowBounds;
    private CancellationTokenSource _searchCts;
    private string _pendingQuery = "";
    private bool _noTextFound;
    private bool _searchTimedOut;
    private bool _disposed;

    public IntPtr SourceWindow { get; }
    public Rect WindowBounds => _windowBounds;

    public string SearchQuery => _state.SearchQuery;
    public IReadOnlyList<SearchMatch> Matches => _state.SearchMatches;
    public int ActiveMatchIndex => _state.ActiveMatchIndex;
    public bool IsSearching => _state.IsSearching;
    public string MatchCountText => _state.MatchCountText;

    /// <summary>True when neither TextPattern nor element names produced any result.</summary>
    public bool NoTextFound
    {
        get => _noTextFound;
        private set { _noTextFound = value; NotifyOfPropertyChange(); }
    }

    /// <summary>
    /// True when the most recent search exceeded the 3-second timeout and returned
    /// partial results. The overlay shows a tip recommending the app's built-in search.
    /// </summary>
    public bool SearchTimedOut
    {
        get => _searchTimedOut;
        private set { _searchTimedOut = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(StatusMessage)); }
    }

    /// <summary>
    /// Human-readable status line. Normally the match count ("2 of 5"), but on
    /// timeout appends a tip suggesting the user try the application's native
    /// search (Ctrl+F / Ctrl+Shift+F).
    /// </summary>
    public string StatusMessage
    {
        get
        {
            if (SearchTimedOut && !string.IsNullOrEmpty(_state.MatchCountText))
                return $"{_state.MatchCountText} — try the app's built-in Ctrl+F";
            if (SearchTimedOut)
                return "Timed out — try the app's built-in Ctrl+F";
            return _state.MatchCountText;
        }
    }

    /// <summary>Called to dismiss the overlay window.</summary>
    public Action CloseOverlay { get; set; }

    public SelectionModeViewModel(
        IFindTextProviderService findTextService,
        Rect windowBounds,
        IntPtr sourceWindow)
    {
        _findTextService = findTextService ?? throw new ArgumentNullException(nameof(findTextService));
        _windowBounds = windowBounds;
        SourceWindow = sourceWindow;
        _state = new SelectionState(sourceWindow);

        _debounceTimer = new System.Timers.Timer(DebounceMs) { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) => RunSearch();
    }

    // ── Input handlers ────────────────────────────────────────

    public void HandleCharacter(char c)
    {
        if (_state.SearchQuery.Length >= MaxQueryLength) return;
        SetQuery(_state.SearchQuery + c);
    }

    public void HandleBackspace()
    {
        if (_state.SearchQuery.Length == 0) return;
        SetQuery(_state.SearchQuery.Substring(0, _state.SearchQuery.Length - 1));
    }

    public void HandleTab(bool shift)
    {
        if (_state.SearchMatches.Count == 0) return;
        _state.CycleActive(shift);
        LogService.Info($"FindText: Tab → active {_state.ActiveMatchIndex + 1}/{_state.SearchMatches.Count}");
        NotifyAll();
    }

    public void HandleEnter()
    {
        var active = _state.ActiveMatch;
        if (active == null)
        {
            LogService.Info("FindText: Enter with no active match — no-op");
            return;
        }

        try
        {
            if (active.TextRangeProvider != null)
            {
                active.TextRangeProvider.ScrollIntoView(1); // alignToTop = TRUE
                active.TextRangeProvider.Select();
                LogService.Info("FindText: Enter → ScrollIntoView + Select (TextPattern)");
            }
            else if (active.AutomationElement != null)
            {
                active.AutomationElement.SetFocus();
                LogService.Info("FindText: Enter → SetFocus (ElementName)");
            }
        }
        catch (COMException ex)
        {
            LogService.Warn($"FindText: Enter navigation failed (stale element): {ex.Message}");
        }
        catch (Exception ex)
        {
            LogService.Warn($"FindText: Enter navigation failed: {ex.Message}");
        }
        finally
        {
            CloseOverlay?.Invoke();
        }
    }

    public void HandleEscape()
    {
        LogService.Info("FindText: Escape → dismiss");
        CloseOverlay?.Invoke();
    }

    public void HandleFocusLost()
    {
        LogService.Info("FindText: focus lost / content changed → auto-dismiss");
        CloseOverlay?.Invoke();
    }

    // ── Search orchestration ──────────────────────────────────

    private void SetQuery(string query)
    {
        _state.SearchQuery = query;
        NoTextFound = false;
        SearchTimedOut = false;

        // Cancel any in-flight search and reset the debounce window on every keystroke.
        CancelInFlightSearch();
        _debounceTimer.Stop();

        if (query.Length < MinQueryLength)
        {
            // Below the minimum: clear results, no search.
            _state.ClearMatches();
            _state.IsSearching = false;
            NotifyAll();
            return;
        }

        lock (_searchLock) { _pendingQuery = query; }
        _debounceTimer.Start();
        NotifyAll();
    }

    private void RunSearch()
    {
        string query;
        lock (_searchLock) { query = _pendingQuery; }
        if (query.Length < MinQueryLength) return;

        var cts = new CancellationTokenSource();
        lock (_searchLock)
        {
            _searchCts?.Dispose();
            _searchCts = cts;
        }

        _state.IsSearching = true;
        NotifyOfPropertyChange(nameof(IsSearching));
        LogService.Info($"FindText: debounce elapsed → searching \"{query}\"");

        _ = SearchAsync(query, cts.Token);
    }

    private async Task SearchAsync(string query, CancellationToken ct)
    {
        try
        {
            var result = await _findTextService.SearchAsync(SourceWindow, query, ct);
            if (ct.IsCancellationRequested) return;

            // Guard against a stale result arriving after the query changed.
            if (!string.Equals(_state.SearchQuery, query, StringComparison.Ordinal)) return;

            _state.IsSearching = false;

            // Surface the timeout flag regardless of whether matches were found,
            // so the UI can show the "try native Ctrl+F" tip even on "0 matches".
            SearchTimedOut = result?.TimedOut ?? false;

            if (result == null || result.Matches.Count == 0)
            {
                _state.ClearMatches();
                NoTextFound = true;
                LogService.Info($"FindText: 0 matches for \"{query}\" ({result?.ElapsedMs ?? 0}ms, timedOut={result?.TimedOut})");
                NotifyAll();
                // Don't auto-dismiss on timeout — let the user see the tip.
                if (!SearchTimedOut)
                    ScheduleNoTextDismiss(query);
                return;
            }

            _state.SetMatches(result.Matches);
            LogService.Info($"FindText: {result.Matches.Count} matches for \"{query}\" via {result.Source} ({result.ElapsedMs}ms, timedOut={result.TimedOut})");
            NotifyAll();
        }
        catch (OperationCanceledException)
        {
            // Expected on rapid typing — ignore.
        }
        catch (Exception ex)
        {
            _state.IsSearching = false;
            LogService.Error("FindText: search error", ex);
            NotifyAll();
        }
    }

    /// <summary>
    /// After a "No text found" result, auto-dismiss the overlay if the user hasn't
    /// changed the query in the meantime.
    /// </summary>
    private void ScheduleNoTextDismiss(string query)
    {
        _ = Task.Delay(NoTextDismissMs).ContinueWith(_ =>
        {
            if (_disposed) return;
            if (!NoTextFound) return;
            if (!string.Equals(_state.SearchQuery, query, StringComparison.Ordinal)) return;
            CloseOverlay?.Invoke();
        }, TaskScheduler.Default);
    }

    private void CancelInFlightSearch()
    {
        lock (_searchLock)
        {
            try { _searchCts?.Cancel(); } catch { }
        }
    }

    // ── Notification ──────────────────────────────────────────

    private void NotifyAll()
    {
        NotifyOfPropertyChange(nameof(SearchQuery));
        NotifyOfPropertyChange(nameof(Matches));
        NotifyOfPropertyChange(nameof(ActiveMatchIndex));
        NotifyOfPropertyChange(nameof(IsSearching));
        NotifyOfPropertyChange(nameof(MatchCountText));
        NotifyOfPropertyChange(nameof(SearchTimedOut));
        NotifyOfPropertyChange(nameof(StatusMessage));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelInFlightSearch();
        _debounceTimer.Stop();
        _debounceTimer.Dispose();
        lock (_searchLock) { _searchCts?.Dispose(); }
    }
}
