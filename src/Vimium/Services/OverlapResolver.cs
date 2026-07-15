using System.Collections.Generic;
using Vimium.Models;
using Vimium.Services.Interfaces;

namespace Vimium.Services;

/// <summary>
/// Resolves overlapping hint labels using spiral offsetting.
/// O(n²) deterministic algorithm — for each label, tries five positions
/// in priority order, stacking vertically as a last resort.
/// </summary>
public class OverlapResolver : IOverlapResolver
{
    /// <summary>
    /// Tolerance in pixels for collision detection. Labels that overlap by
    /// 1px or less are not considered colliding — they're close enough that
    /// the visual association to their element stays clear.
    /// </summary>
    private const double CollisionTolerance = 1.0;

    /// <inheritdoc />
    public void ResolveOverlaps(IReadOnlyList<HintLabelPosition> positions, double maxOffset)
    {
        if (positions == null || positions.Count == 0)
            return;

        // Single label or pre-scan: if no labels overlap at their default
        // positions, leave everything at default. This prevents false-positive
        // collisions from overestimated label dimensions from moving labels
        // to wrong positions on well-spaced UIs.
        if (positions.Count == 1)
        {
            positions[0].AdjustedLeft = positions[0].OriginalLeft;
            positions[0].AdjustedTop = positions[0].OriginalTop;
            positions[0].Placement = PlacementDirection.Default;
            return;
        }

        bool anyOverlap = false;
        for (int i = 0; i < positions.Count && !anyOverlap; i++)
        {
            var a = new HintLabelPosition.Rect(
                positions[i].OriginalLeft, positions[i].OriginalTop,
                positions[i].LabelWidth, positions[i].LabelHeight);
            for (int j = i + 1; j < positions.Count && !anyOverlap; j++)
            {
                var b = new HintLabelPosition.Rect(
                    positions[j].OriginalLeft, positions[j].OriginalTop,
                    positions[j].LabelWidth, positions[j].LabelHeight);
                if (a.IntersectsWith(b, CollisionTolerance))
                    anyOverlap = true;
            }
        }

        if (!anyOverlap)
        {
            // All labels are fine at default positions — leave them alone
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i].AdjustedLeft = positions[i].OriginalLeft;
                positions[i].AdjustedTop = positions[i].OriginalTop;
                positions[i].Placement = PlacementDirection.Default;
            }
            return;
        }

        var placed = new List<HintLabelPosition.Rect>();

        for (int i = 0; i < positions.Count; i++)
        {
            var pos = positions[i];

            // Try positions in priority order, clamped to maxOffset
            var candidates = new (double left, double top, PlacementDirection placement)[]
            {
                (pos.OriginalLeft, pos.OriginalTop, PlacementDirection.Default),
                (pos.OriginalLeft, ClampY(pos.OriginalTop - pos.ElementHeight - 2, pos.OriginalTop, maxOffset), PlacementDirection.Above),
                (pos.OriginalLeft, ClampY(pos.OriginalTop + pos.ElementHeight + 2, pos.OriginalTop, maxOffset), PlacementDirection.Below),
                (ClampX(pos.OriginalLeft + pos.ElementWidth + 2, pos.OriginalLeft, maxOffset), pos.OriginalTop, PlacementDirection.Right),
                (ClampX(pos.OriginalLeft - pos.LabelWidth - 2, pos.OriginalLeft, maxOffset), pos.OriginalTop, PlacementDirection.Left),
            };

            bool resolved = false;
            for (int c = 0; c < candidates.Length; c++)
            {
                var (left, top, placement) = candidates[c];
                var bounds = new HintLabelPosition.Rect(left, top, pos.LabelWidth, pos.LabelHeight);
                if (!HasCollision(bounds, placed, CollisionTolerance))
                {
                    pos.AdjustedLeft = left;
                    pos.AdjustedTop = top;
                    pos.Placement = placement;
                    placed.Add(bounds);
                    resolved = true;
                    break;
                }
            }

            // Stacking fallback: if all five positions collide,
            // stack vertically below ALL previously placed labels with 2px gap
            if (!resolved)
            {
                pos.AdjustedLeft = pos.OriginalLeft;
                double maxBottom = pos.OriginalTop;
                for (int j = 0; j < placed.Count; j++)
                {
                    if (placed[j].Bottom > maxBottom)
                        maxBottom = placed[j].Bottom;
                }
                pos.AdjustedTop = maxBottom + 2;
                pos.Placement = PlacementDirection.Stacked;
                placed.Add(pos.Bounds);
            }
        }
    }

    private static double ClampX(double value, double originalX, double maxOffset)
    {
        double minX = originalX - maxOffset;
        double maxX = originalX + maxOffset;
        if (value < minX) return minX;
        if (value > maxX) return maxX;
        return value;
    }

    private static double ClampY(double value, double originalY, double maxOffset)
    {
        double minY = originalY - maxOffset;
        double maxY = originalY + maxOffset;
        if (value < minY) return minY;
        if (value > maxY) return maxY;
        return value;
    }

    /// <summary>
    /// Tests whether the given bounds intersect with any already-placed label.
    /// </summary>
    private static bool HasCollision(HintLabelPosition.Rect bounds, List<HintLabelPosition.Rect> placed, double tolerance)
    {
        for (int i = 0; i < placed.Count; i++)
        {
            if (bounds.IntersectsWith(placed[i], tolerance))
                return true;
        }
        return false;
    }
}
