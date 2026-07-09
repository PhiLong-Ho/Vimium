using Vimium.Services;
using Xunit;
using System.Linq;

namespace Vimium.Tests.Services;

public class HintLabelServiceTest
{
    private readonly HintLabelService _hintService = new();

    [Fact]
    public void GetHintStrings_ZeroCount_ReturnsEmpty()
    {
        var hints = _hintService.GetHintStrings(0);
        Assert.Empty(hints);
    }

    [Fact]
    public void GetHintStrings_NegativeCount_ReturnsEmpty()
    {
        var hints = _hintService.GetHintStrings(-5);
        Assert.Empty(hints);
    }

    [Fact]
    public void GetHintStrings_SingleHint_ReturnsOneHint()
    {
        var hints = _hintService.GetHintStrings(1);
        Assert.Single(hints);
        Assert.Equal("S", hints[0]);
    }

    [Fact]
    public void GetHintStrings_AllSameLength_ForSmallCount()
    {
        // 14 hints should all be 1 character (14^1 = 14 >= 14)
        var hints = _hintService.GetHintStrings(14);
        Assert.All(hints, h => Assert.Equal(1, h.Length));
    }

    [Fact]
    public void GetHintStrings_AllSameLength_WhenCrossingBoundary()
    {
        // 15 hints requires 2-char labels (14^1 = 14 < 15, 14^2 = 196 >= 15)
        var hints = _hintService.GetHintStrings(15);
        Assert.All(hints, h => Assert.Equal(2, h.Length));
    }

    [Fact]
    public void GetHintStrings_AllSameLength_ForLargeCount()
    {
        // 200 hints requires 2-char labels (14^2 = 196 < 200, 14^3 = 2744 >= 200)
        var hints = _hintService.GetHintStrings(200);
        Assert.All(hints, h => Assert.Equal(3, h.Length));
    }

    [Fact]
    public void GetHintStrings_UniqueStrings_ForSmallCount()
    {
        const int hintCount = 14;
        var hints = _hintService.GetHintStrings(hintCount);
        Assert.Equal(hintCount, hints.Distinct().Count());
    }

    [Fact]
    public void GetHintStrings_UniqueStrings_ForLargeCount()
    {
        const int hintCount = 256;
        var hints = _hintService.GetHintStrings(hintCount);
        Assert.Equal(hintCount, hints.Distinct().Count());
    }

    [Fact]
    public void GetHintStrings_UniqueStrings_ForBoundaryCount()
    {
        // Exactly at the 2-char pool boundary
        const int hintCount = 196;
        var hints = _hintService.GetHintStrings(hintCount);
        Assert.Equal(hintCount, hints.Distinct().Count());
    }

    [Fact]
    public void GetHintStrings_NoPrefixRelationship_BetweenAnyTwoHints()
    {
        // This is the core fix: no hint should be a prefix of another.
        // Test at various counts that span different pool lengths.
        var testCounts = new[] { 14, 15, 50, 196, 200, 500 };
        foreach (var count in testCounts)
        {
            var hints = _hintService.GetHintStrings(count);
            for (var i = 0; i < hints.Count; i++)
            {
                for (var j = 0; j < hints.Count; j++)
                {
                    if (i == j) continue;
                    Assert.False(
                        hints[j].StartsWith(hints[i]),
                        $"Hint '{hints[i]}' is a prefix of hint '{hints[j]}' at count={count}");
                }
            }
        }
    }

    [Fact]
    public void GetHintStrings_ReturnsCorrectCount()
    {
        var testCases = new[] { 1, 14, 15, 50, 100, 196, 197, 256, 500, 1000 };
        foreach (var count in testCases)
        {
            var hints = _hintService.GetHintStrings(count);
            Assert.Equal(count, hints.Count);
        }
    }

    [Fact]
    public void GetHintStrings_FirstHintIsAlwaysHomeRow()
    {
        // The first hint should start with 'S' (home row key, index 0)
        var hints1 = _hintService.GetHintStrings(1);
        Assert.StartsWith("S", hints1[0]);

        var hints10 = _hintService.GetHintStrings(10);
        Assert.StartsWith("S", hints10[0]);
    }

    [Fact]
    public void GetHintStrings_HintsAreSequential()
    {
        // The pool is generated in base-N counting order.
        // Verify the first few hints of the 2-char pool are sequential.
        var hints = _hintService.GetHintStrings(20);
        // First hint at length 2 is "SS" (00 in base-14)
        Assert.Equal("SS", hints[0]);
        Assert.Equal("SA", hints[1]);
        Assert.Equal("SD", hints[2]);
        Assert.Equal("SF", hints[3]);
    }

    [Fact]
    public void GetHintStrings_PoolReuse_ReturnsConsistentResults()
    {
        // Multiple calls should return identical results (pool is cached)
        var first = _hintService.GetHintStrings(50);
        var second = _hintService.GetHintStrings(50);
        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i], second[i]);
        }
    }

    [Fact]
    public void GetHintStrings_UniformLength_GuaranteesNoPrefixWithinBatch()
    {
        // The core guarantee: within a single GetHintStrings() call,
        // all hints have the same length, so no hint can be a prefix of another.
        // Cross-call prefix relationships (e.g., 1-char pool vs 3-char pool)
        // are irrelevant because each overlay uses exactly one call.
        var counts = new[] { 1, 5, 14, 15, 50, 196, 200, 500, 1000 };
        foreach (var count in counts)
        {
            var hints = _hintService.GetHintStrings(count);
            var lengths = hints.Select(h => h.Length).Distinct().ToList();
            Assert.Single(lengths);
        }
    }
}
