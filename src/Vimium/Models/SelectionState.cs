using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Vimium.Models;

/// <summary>
/// Represents the mutable state of a single sub-line selection operation.
/// All user interactions (search, cursor movement, selection extension) mutate this object.
/// </summary>
public class SelectionState
{
    private readonly StringBuilder _visibleText;

    /// <summary>
    /// The line the user initially targeted with the hint label.
    /// </summary>
    public TextLineHint TargetedLine { get; }

    /// <summary>
    /// All visible lines in reading order. Used to map character offsets back
    /// to line-level bounding rectangles for cursor/highlight positioning.
    /// </summary>
    public IReadOnlyList<TextLineHint> AllVisibleLines { get; }

    /// <summary>
    /// Current character offset (0-based) within VisibleText.
    /// </summary>
    public int CursorPosition { get; private set; }

    /// <summary>
    /// Start offset of the current text selection, or null if no selection is active.
    /// </summary>
    public int? SelectionStart { get; private set; }

    /// <summary>
    /// End offset of the current text selection, or null if no selection is active.
    /// </summary>
    public int? SelectionEnd { get; private set; }

    /// <summary>
    /// The current incremental search string (empty if no search active).
    /// </summary>
    public string SearchQuery { get; private set; } = "";

    /// <summary>
    /// All occurrences of SearchQuery in VisibleText.
    /// </summary>
    public IReadOnlyList<SearchMatch> SearchMatches { get; private set; } = Array.Empty<SearchMatch>();

    /// <summary>
    /// Index into SearchMatches of the currently highlighted match.
    /// </summary>
    public int ActiveMatchIndex { get; set; }

    /// <summary>
    /// The full visible text (concatenated lines joined with newlines).
    /// </summary>
    public string VisibleText => _visibleText.ToString();

    /// <summary>
    /// The selected text substring, or the whole targeted line if no selection.
    /// </summary>
    public string SelectedText
    {
        get
        {
            if (HasSelection && SelectionStart.HasValue && SelectionEnd.HasValue)
            {
                int start = Math.Min(SelectionStart.Value, SelectionEnd.Value);
                int end = Math.Max(SelectionStart.Value, SelectionEnd.Value);
                if (start >= 0 && end <= VisibleText.Length && start <= end)
                    return VisibleText.Substring(start, end - start);
            }
            return TargetedLine.TextContent;
        }
    }

    /// <summary>
    /// True when there is an active text selection.
    /// </summary>
    public bool HasSelection =>
        SelectionStart.HasValue && SelectionEnd.HasValue && SelectionStart.Value != SelectionEnd.Value;

    /// <summary>
    /// The line index within AllVisibleLines where the cursor currently sits.
    /// </summary>
    public int CursorLineIndex
    {
        get
        {
            int offset = CursorPosition;
            for (int i = 0; i < AllVisibleLines.Count; i++)
            {
                int lineLen = AllVisibleLines[i].TextContent.Length;
                if (offset <= lineLen)
                    return i;
                offset -= (lineLen + 1); // +1 for the newline separator
            }
            return Math.Max(0, AllVisibleLines.Count - 1);
        }
    }

    /// <summary>
    /// The character position within the current line.
    /// </summary>
    public int CursorLinePosition
    {
        get
        {
            int offset = CursorPosition;
            for (int i = 0; i < AllVisibleLines.Count; i++)
            {
                int lineLen = AllVisibleLines[i].TextContent.Length;
                if (offset <= lineLen)
                    return offset;
                offset -= (lineLen + 1);
            }
            return 0;
        }
    }

    public SelectionState(TextLineHint targetedLine, IReadOnlyList<TextLineHint> allVisibleLines)
    {
        TargetedLine = targetedLine;
        AllVisibleLines = allVisibleLines;
        _visibleText = new StringBuilder();

        for (int i = 0; i < allVisibleLines.Count; i++)
        {
            if (i > 0)
                _visibleText.Append('\n');
            _visibleText.Append(allVisibleLines[i].TextContent);
        }

        // Set initial cursor position to the start of the targeted line
        int targetOffset = 0;
        for (int i = 0; i < allVisibleLines.Count; i++)
        {
            if (ReferenceEquals(allVisibleLines[i], targetedLine))
                break;
            targetOffset += allVisibleLines[i].TextContent.Length + 1; // +1 for newline
        }
        CursorPosition = targetOffset;
    }

    // ── Mutation methods ──────────────────────────────────────

    public void HandleArrow(Key key)
    {
        ClearSelection();
        if (key == Key.Right)
            CursorPosition = Math.Min(CursorPosition + 1, VisibleText.Length);
        else if (key == Key.Left)
            CursorPosition = Math.Max(CursorPosition - 1, 0);
    }

    public void HandleCtrlArrow(Key key)
    {
        ClearSelection();
        if (key == Key.Right)
            CursorPosition = NextWordBoundary(CursorPosition, forward: true);
        else if (key == Key.Left)
            CursorPosition = NextWordBoundary(CursorPosition, forward: false);
    }

    public void HandleShiftArrow(Key key)
    {
        if (!HasSelection)
        {
            SelectionStart = CursorPosition;
        }

        if (key == Key.Right)
            CursorPosition = Math.Min(CursorPosition + 1, VisibleText.Length);
        else if (key == Key.Left)
            CursorPosition = Math.Max(CursorPosition - 1, 0);

        SelectionEnd = CursorPosition;
    }

    public void HandleCtrlShiftArrow(Key key)
    {
        if (!HasSelection)
        {
            SelectionStart = CursorPosition;
        }

        if (key == Key.Right)
            CursorPosition = NextWordBoundary(CursorPosition, forward: true);
        else if (key == Key.Left)
            CursorPosition = NextWordBoundary(CursorPosition, forward: false);

        SelectionEnd = CursorPosition;
    }

    public void HandleHome()
    {
        ClearSelection();
        // Move to start of current line
        int offset = 0;
        for (int i = 0; i < CursorLineIndex; i++)
        {
            offset += AllVisibleLines[i].TextContent.Length + 1;
        }
        CursorPosition = offset;
    }

    public void HandleEnd()
    {
        ClearSelection();
        // Move to end of current line
        int offset = 0;
        for (int i = 0; i <= CursorLineIndex && i < AllVisibleLines.Count; i++)
        {
            if (i == CursorLineIndex)
            {
                offset += AllVisibleLines[i].TextContent.Length;
                break;
            }
            offset += AllVisibleLines[i].TextContent.Length + 1;
        }
        CursorPosition = Math.Min(offset, VisibleText.Length);
    }

    public void HandleTab(bool shift)
    {
        if (SearchMatches.Count == 0)
            return;

        ClearSelection();

        if (shift)
        {
            // Backward
            ActiveMatchIndex = (ActiveMatchIndex - 1 + SearchMatches.Count) % SearchMatches.Count;
        }
        else
        {
            // Forward
            ActiveMatchIndex = (ActiveMatchIndex + 1) % SearchMatches.Count;
        }

        // Move cursor to the active match
        var activeMatch = SearchMatches[ActiveMatchIndex];
        CursorPosition = activeMatch.StartIndex;
    }

    public void UpdateSearch(string query)
    {
        SearchQuery = query ?? "";
        ClearSelection();

        if (string.IsNullOrEmpty(SearchQuery))
        {
            SearchMatches = Array.Empty<SearchMatch>();
            ActiveMatchIndex = 0;
            return;
        }

        // Find all occurrences (case-insensitive)
        var matches = new List<SearchMatch>();
        int searchFrom = 0;
        while (searchFrom < VisibleText.Length)
        {
            int found = VisibleText.IndexOf(SearchQuery, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
                break;

            int lineIndex = GetLineIndexForOffset(found);

            matches.Add(new SearchMatch
            {
                StartIndex = found,
                EndIndex = found + SearchQuery.Length,
                LineIndex = lineIndex,
                IsActive = false
            });

            searchFrom = found + 1;
        }

        // Mark the first match as active
        if (matches.Count > 0)
        {
            matches[0].IsActive = true;
            CursorPosition = matches[0].StartIndex;
            ActiveMatchIndex = 0;
        }

        SearchMatches = matches;
    }

    // ── Private helpers ───────────────────────────────────────

    private void ClearSelection()
    {
        SelectionStart = null;
        SelectionEnd = null;
    }

    private int GetLineIndexForOffset(int offset)
    {
        int currentOffset = 0;
        for (int i = 0; i < AllVisibleLines.Count; i++)
        {
            int lineLen = AllVisibleLines[i].TextContent.Length;
            int lineEnd = currentOffset + lineLen;
            if (offset >= currentOffset && offset <= lineEnd)
                return i;
            currentOffset = lineEnd + 1; // +1 for newline
        }
        return Math.Max(0, AllVisibleLines.Count - 1);
    }

    private int NextWordBoundary(int position, bool forward)
    {
        var text = VisibleText;
        if (text.Length == 0)
            return 0;

        if (forward)
        {
            // Skip non-whitespace (current word)
            while (position < text.Length && !char.IsWhiteSpace(text[position]))
                position++;
            // Skip whitespace
            while (position < text.Length && char.IsWhiteSpace(text[position]))
                position++;
            return Math.Min(position, text.Length);
        }
        else
        {
            position = Math.Max(position - 1, 0);
            // Skip whitespace
            while (position > 0 && char.IsWhiteSpace(text[position]))
                position--;
            // Skip non-whitespace (current word)
            while (position > 0 && !char.IsWhiteSpace(text[position - 1]))
                position--;
            return Math.Max(position, 0);
        }
    }
}
