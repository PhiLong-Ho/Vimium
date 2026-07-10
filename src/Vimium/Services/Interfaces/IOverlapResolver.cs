using System.Collections.Generic;

namespace Vimium.Services.Interfaces;

/// <summary>
/// Resolves overlapping hint labels using spiral offsetting.
/// </summary>
public interface IOverlapResolver
{
    /// <summary>
    /// Adjusts hint label positions so no two labels visually overlap.
    /// Labels are repositioned in priority order: default (top-left),
    /// above, below, right, left. Labels that still overlap after all
    /// five positions are stacked vertically.
    /// </summary>
    /// <param name="positions">Hint label positions with their original
    /// bounding rectangles (in window coordinates). Modified in place.</param>
    /// <param name="maxOffset">Maximum offset from element edge (px).
    /// Per FR-010: 20px.</param>
    void ResolveOverlaps(IReadOnlyList<Models.HintLabelPosition> positions, double maxOffset);
}
