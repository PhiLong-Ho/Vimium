# Interface Contracts: Text Selection Mode (Redesigned)

**Feature**: Text Selection Mode | **Date**: 2026-07-09

## Interfaces

### ITextSourceProviderService (renamed from ILineHintProviderService)

```csharp
namespace Vimium.Services.Interfaces;

/// <summary>
/// Extracts visible text content and position data from the foreground window
/// using UI Automation (TextPattern or ValuePattern).
/// </summary>
public interface ITextSourceProviderService
{
    /// <summary>
    /// Extracts text content and per-line bounding rectangles from the given window.
    /// </summary>
    /// <param name="hWnd">Handle of the foreground window.</param>
    /// <returns>A TextSource containing full text and position data.</returns>
    TextSource GetTextSource(IntPtr hWnd);

    /// <summary>
    /// Extracts text content on a background thread to keep the UI responsive.
    /// Has a 10-second timeout to prevent hanging on unresponsive UIA providers.
    /// </summary>
    Task<TextSource> GetTextSourceAsync(IntPtr hWnd);
}
```

### SelectionModeViewModel (existing, enhanced)

```csharp
namespace Vimium.ViewModels;

internal class SelectionModeViewModel : NotifyPropertyChanged
{
    // Constructor simplified: takes TextSource instead of individual line hints
    public SelectionModeViewModel(TextSource textSource, ClipboardService clipboard);

    // Properties (unchanged)
    public string VisibleText { get; }
    public int CursorPosition { get; }
    public int? SelectionStart { get; }
    public int? SelectionEnd { get; }
    public string SearchQuery { get; }
    public IReadOnlyList<SearchMatch> Matches { get; }
    public int ActiveMatchIndex { get; }
    public Rect WindowBounds { get; }
    public IReadOnlyList<TextLineRect> AllLines { get; }

    // Actions (unchanged)
    public Action CloseOverlay { get; set; }
    public Action<string> OnCopied { get; set; }

    // Input handlers (unchanged)
    public void HandleCharacter(char c);
    public void HandleBackspace();
    public void HandleArrow(Key key);
    public void HandleCtrlArrow(Key key);
    public void HandleShiftArrow(Key key);
    public void HandleCtrlShiftArrow(Key key);
    public void HandleHome();
    public void HandleEnd();
    public void HandleTab(bool shift);
    public void HandleEnter();
    public void HandleEscape();
}
```

## Removed Interfaces
- **ILineHintProviderService** — renamed to `ITextSourceProviderService`
