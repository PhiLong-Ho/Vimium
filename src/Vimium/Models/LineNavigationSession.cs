using System;
using System.Collections.Generic;
using System.Windows;

namespace Vimium.Models;

/// <summary>
/// Represents a single line-navigation enumeration.
/// Analogous to <see cref="HintSession"/> for element mode.
/// </summary>
public class LineNavigationSession
{
    /// <summary>
    /// Ordered list of text line hints (top-to-bottom, left-to-right in reading order).
    /// </summary>
    public IList<TextLineHint> Hints { get; set; }

    /// <summary>
    /// Handle of the foreground window the hints were enumerated from.
    /// </summary>
    public IntPtr OwningWindow { get; set; }

    /// <summary>
    /// Bounds of the owning window in logical screen coordinates.
    /// </summary>
    public Rect OwningWindowBounds { get; set; }
}
