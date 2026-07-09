using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Vimium.Models;
using Vimium.Services;

namespace Vimium.ViewModels;

/// <summary>
/// ViewModel for the selection mode overlay. Manages cursor, search, selection,
/// and clipboard operations on visible text.
/// </summary>
internal class SelectionModeViewModel : NotifyPropertyChanged
{
    private readonly SelectionState _state;
    private readonly ClipboardService _clipboard;
    private readonly Rect _windowBounds;

    public SelectionModeViewModel(
        TextLineHint targetedLine,
        IReadOnlyList<TextLineHint> allLines,
        Rect windowBounds,
        ClipboardService clipboard)
    {
        _state = new SelectionState(targetedLine, allLines);
        _windowBounds = windowBounds;
        _clipboard = clipboard;
    }

    // ── Public properties for data binding ────────────────────

    public string VisibleText => _state.VisibleText;

    public int CursorPosition => _state.CursorPosition;

    public int? SelectionStart => _state.SelectionStart;

    public int? SelectionEnd => _state.SelectionEnd;

    public string SearchQuery => _state.SearchQuery;

    public IReadOnlyList<SearchMatch> Matches => _state.SearchMatches;

    public int ActiveMatchIndex => _state.ActiveMatchIndex;

    public Rect WindowBounds => _windowBounds;

    public IReadOnlyList<TextLineHint> AllLines => _state.AllVisibleLines;

    public Action CloseOverlay { get; set; }

    /// <summary>
    /// Called when text is copied to clipboard (for feedback UI).
    /// </summary>
    public Action<string> OnCopied { get; set; }

    // ── Input handlers ────────────────────────────────────────

    public void HandleCharacter(char c)
    {
        _state.UpdateSearch(_state.SearchQuery + c);
        NotifyAll();
    }

    public void HandleBackspace()
    {
        if (_state.SearchQuery.Length > 0)
        {
            _state.UpdateSearch(_state.SearchQuery.Substring(0, _state.SearchQuery.Length - 1));
            NotifyAll();
        }
    }

    public void HandleArrow(Key key)
    {
        _state.HandleArrow(key);
        NotifyAll();
    }

    public void HandleCtrlArrow(Key key)
    {
        _state.HandleCtrlArrow(key);
        NotifyAll();
    }

    public void HandleShiftArrow(Key key)
    {
        _state.HandleShiftArrow(key);
        NotifyAll();
    }

    public void HandleCtrlShiftArrow(Key key)
    {
        _state.HandleCtrlShiftArrow(key);
        NotifyAll();
    }

    public void HandleHome()
    {
        _state.HandleHome();
        NotifyAll();
    }

    public void HandleEnd()
    {
        _state.HandleEnd();
        NotifyAll();
    }

    public void HandleTab(bool shift)
    {
        _state.HandleTab(shift);
        NotifyAll();
    }

    public void HandleEnter()
    {
        try
        {
            string textToCopy = _state.SelectedText;
            _clipboard.SetText(textToCopy);
            OnCopied?.Invoke(textToCopy);
        }
        catch (InvalidOperationException)
        {
            // Clipboard unavailable — silently ignore
        }
        finally
        {
            CloseOverlay?.Invoke();
        }
    }

    public void HandleEscape()
    {
        CloseOverlay?.Invoke();
    }

    // ── Private ───────────────────────────────────────────────

    private void NotifyAll()
    {
        NotifyOfPropertyChange(nameof(CursorPosition));
        NotifyOfPropertyChange(nameof(SelectionStart));
        NotifyOfPropertyChange(nameof(SelectionEnd));
        NotifyOfPropertyChange(nameof(SearchQuery));
        NotifyOfPropertyChange(nameof(Matches));
        NotifyOfPropertyChange(nameof(ActiveMatchIndex));
    }
}
