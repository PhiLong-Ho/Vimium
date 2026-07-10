using Vimium.Models;
using Vimium.Services;
using Xunit;

namespace Vimium.Tests.Services;

/// <summary>
/// Tests for OverlapResolver spiral offsetting algorithm.
/// </summary>
public class OverlapResolverTest
{
    private readonly OverlapResolver _resolver = new();

    [Fact]
    public void ResolveOverlaps_EmptyList_DoesNotThrow()
    {
        var positions = new List<HintLabelPosition>();
        _resolver.ResolveOverlaps(positions, 20);
    }

    [Fact]
    public void ResolveOverlaps_NullList_DoesNotThrow()
    {
        _resolver.ResolveOverlaps(null!, 20);
    }

    [Fact]
    public void ResolveOverlaps_SingleLabel_StaysAtDefault()
    {
        var positions = new List<HintLabelPosition>
        {
            new()
            {
                OriginalLeft = 100, OriginalTop = 50,
                LabelWidth = 30, LabelHeight = 16,
                ElementWidth = 80, ElementHeight = 20,
            }
        };

        _resolver.ResolveOverlaps(positions, 20);

        Assert.Equal(100, positions[0].AdjustedLeft);
        Assert.Equal(50, positions[0].AdjustedTop);
        Assert.Equal(PlacementDirection.Default, positions[0].Placement);
    }

    [Fact]
    public void ResolveOverlaps_NonOverlapping_AllStayAtDefault()
    {
        var positions = new List<HintLabelPosition>
        {
            new() { OriginalLeft = 10, OriginalTop = 10, LabelWidth = 20, LabelHeight = 14, ElementWidth = 40, ElementHeight = 18 },
            new() { OriginalLeft = 100, OriginalTop = 10, LabelWidth = 20, LabelHeight = 14, ElementWidth = 40, ElementHeight = 18 },
            new() { OriginalLeft = 200, OriginalTop = 10, LabelWidth = 20, LabelHeight = 14, ElementWidth = 40, ElementHeight = 18 },
        };

        _resolver.ResolveOverlaps(positions, 20);

        for (int i = 0; i < positions.Count; i++)
        {
            Assert.Equal(PlacementDirection.Default, positions[i].Placement);
        }
    }

    [Fact]
    public void ResolveOverlaps_Overlapping_MovesToAbove()
    {
        var positions = new List<HintLabelPosition>
        {
            // Two labels at the same position should not both be Default
            new() { OriginalLeft = 50, OriginalTop = 50, LabelWidth = 30, LabelHeight = 16, ElementWidth = 60, ElementHeight = 20 },
            new() { OriginalLeft = 50, OriginalTop = 50, LabelWidth = 30, LabelHeight = 16, ElementWidth = 60, ElementHeight = 20 },
        };

        _resolver.ResolveOverlaps(positions, 20);

        // First label stays at Default
        Assert.Equal(PlacementDirection.Default, positions[0].Placement);

        // Second label should be offset (at least not Default)
        Assert.NotEqual(PlacementDirection.Default, positions[1].Placement);
    }

    [Fact]
    public void ResolveOverlaps_AdjacentLabels_NoOverlapAfterResolution()
    {
        var positions = new List<HintLabelPosition>();
        // Create a horizontal row of 10 labels all at the same position
        for (int i = 0; i < 10; i++)
        {
            positions.Add(new HintLabelPosition
            {
                OriginalLeft = 50,
                OriginalTop = 50,
                LabelWidth = 25,
                LabelHeight = 16,
                ElementWidth = 60,
                ElementHeight = 20,
            });
        }

        _resolver.ResolveOverlaps(positions, 20);

        // Verify no two labels overlap after resolution
        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                var a = positions[i].Bounds;
                var b = positions[j].Bounds;
                Assert.False(
                    a.IntersectsWith(b),
                    $"Labels {i} and {j} overlap: ({a.Left},{a.Top}) vs ({b.Left},{b.Top})");
            }
        }
    }

    [Fact]
    public void ResolveOverlaps_Deterministic_SameInputSameOutput()
    {
        var createPositions = () => new List<HintLabelPosition>
        {
            new() { OriginalLeft = 10, OriginalTop = 10, LabelWidth = 20, LabelHeight = 14, ElementWidth = 40, ElementHeight = 18 },
            new() { OriginalLeft = 15, OriginalTop = 10, LabelWidth = 20, LabelHeight = 14, ElementWidth = 40, ElementHeight = 18 },
            new() { OriginalLeft = 20, OriginalTop = 12, LabelWidth = 20, LabelHeight = 14, ElementWidth = 40, ElementHeight = 18 },
        };

        var positions1 = createPositions();
        var positions2 = createPositions();

        _resolver.ResolveOverlaps(positions1, 20);
        _resolver.ResolveOverlaps(positions2, 20);

        for (int i = 0; i < positions1.Count; i++)
        {
            Assert.Equal(positions1[i].AdjustedLeft, positions2[i].AdjustedLeft);
            Assert.Equal(positions1[i].AdjustedTop, positions2[i].AdjustedTop);
            Assert.Equal(positions1[i].Placement, positions2[i].Placement);
        }
    }

    [Fact]
    public void ResolveOverlaps_RespectsMaxOffset()
    {
        var positions = new List<HintLabelPosition>();
        for (int i = 0; i < 20; i++)
        {
            positions.Add(new HintLabelPosition
            {
                OriginalLeft = 10,
                OriginalTop = 10,
                LabelWidth = 20,
                LabelHeight = 14,
                ElementWidth = 40,
                ElementHeight = 18,
            });
        }

        _resolver.ResolveOverlaps(positions, 20);

        // Non-stacked labels should not drift more than maxOffset from original
        foreach (var pos in positions)
        {
            if (pos.Placement != PlacementDirection.Stacked)
            {
                double dx = pos.AdjustedLeft - pos.OriginalLeft;
                double dy = pos.AdjustedTop - pos.OriginalTop;
                Assert.True(Math.Abs(dx) <= 20, $"Label dx={dx} exceeds 20px");
                Assert.True(Math.Abs(dy) <= 20, $"Label dy={dy} exceeds 20px");
            }
        }
    }

    [Fact]
    public void ResolveOverlaps_ExtremeCase_UsesStacking()
    {
        // Many labels at the same spot should eventually stack
        var positions = new List<HintLabelPosition>();
        for (int i = 0; i < 30; i++)
        {
            positions.Add(new HintLabelPosition
            {
                OriginalLeft = 100,
                OriginalTop = 100,
                LabelWidth = 30,
                LabelHeight = 16,
                ElementWidth = 60,
                ElementHeight = 20,
            });
        }

        _resolver.ResolveOverlaps(positions, 20);

        // At least one label should be Stacked (all 5 positions exhausted)
        bool hasStacked = false;
        foreach (var pos in positions)
        {
            if (pos.Placement == PlacementDirection.Stacked)
            {
                hasStacked = true;
                break;
            }
        }
        Assert.True(hasStacked, "Expected at least one label to be Stacked");
    }

    [Fact]
    public void ResolveOverlaps_SpiralPositions_AreAllUsed()
    {
        var positions = new List<HintLabelPosition>();
        for (int i = 0; i < 20; i++)
        {
            positions.Add(new HintLabelPosition
            {
                OriginalLeft = 50,
                OriginalTop = 50,
                LabelWidth = 20,
                LabelHeight = 14,
                ElementWidth = 40,
                ElementHeight = 18,
            });
        }

        _resolver.ResolveOverlaps(positions, 20);

        var placementsUsed = positions.Select(p => p.Placement).Distinct().ToList();

        // Should use multiple spiral positions
        Assert.True(placementsUsed.Count >= 2,
            $"Expected at least 2 placement types, got {placementsUsed.Count}");
    }
}
