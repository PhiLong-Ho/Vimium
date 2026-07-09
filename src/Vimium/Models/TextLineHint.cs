using System;
using System.Windows;

namespace Vimium.Models;

/// <summary>
/// Represents a single visible line of text as a navigable target.
/// Extends <see cref="Hint"/> to reuse cursor-movement logic.
/// </summary>
public class TextLineHint : Hint
{
    /// <summary>
    /// The full text content of this line as reported by UIA TextRange.GetText(-1).
    /// </summary>
    public string TextContent { get; }

    /// <summary>
    /// Creates a text-line hint for a visible text line.
    /// </summary>
    /// <param name="owningWindow">The window handle this line belongs to.</param>
    /// <param name="boundingRectangle">On-screen bounding rectangle of the text line.</param>
    /// <param name="textContent">The text content of this line.</param>
    public TextLineHint(IntPtr owningWindow, Rect boundingRectangle, string textContent)
        : base(owningWindow, boundingRectangle)
    {
        TextContent = textContent ?? string.Empty;
    }

    /// <summary>
    /// Text-line hints do not support invoke. Navigation is the primary action.
    /// </summary>
    public override void Invoke()
    {
        // Text lines don't have an invoke action.
        // Navigation is handled via MovePointerToCenter().
    }
}
