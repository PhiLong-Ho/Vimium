using System.Windows;
using Vimium.Models;
using Xunit;

namespace Vimium.Tests.Models;

public class SearchMatchTest
{
    [Fact]
    public void IsValid_RejectsEmptySourceText()
    {
        var m = new SearchMatch { SourceText = "", BoundingRect = new Rect(0, 0, 10, 10) };
        Assert.False(m.IsValid);

        m.SourceText = "hello";
        Assert.True(m.IsValid);
    }

    [Fact]
    public void IsValid_RejectsZeroSizeBoundingRect()
    {
        var m = new SearchMatch { SourceText = "hello", BoundingRect = new Rect(0, 0, 0, 0) };
        Assert.False(m.IsValid);

        m.BoundingRect = new Rect(0, 0, 20, 12);
        Assert.True(m.IsValid);
    }

    [Fact]
    public void IsActive_TogglesCorrectly()
    {
        var m = new SearchMatch { SourceText = "x", BoundingRect = new Rect(0, 0, 5, 5) };
        Assert.False(m.IsActive);
        m.IsActive = true;
        Assert.True(m.IsActive);
    }

    [Fact]
    public void TextRangeProvider_CanBeNull()
    {
        // ElementName-sourced matches have a null text range.
        var m = new SearchMatch
        {
            SourceText = "folder",
            BoundingRect = new Rect(0, 0, 40, 16),
            Source = SearchResultSource.ElementName,
            TextRangeProvider = null
        };
        Assert.Null(m.TextRangeProvider);
        Assert.True(m.IsValid);
    }

    [Fact]
    public void FromResult_CopiesFieldsAndSetsActive()
    {
        var result = new SearchResult
        {
            Text = "Singapore",
            BoundingRect = new Rect(5, 6, 80, 18),
            Source = SearchResultSource.TextPattern
        };

        var active = SearchMatch.FromResult(result, isActive: true);
        Assert.Equal("Singapore", active.SourceText);
        Assert.Equal(new Rect(5, 6, 80, 18), active.BoundingRect);
        Assert.Equal(SearchResultSource.TextPattern, active.Source);
        Assert.True(active.IsActive);

        var inactive = SearchMatch.FromResult(result, isActive: false);
        Assert.False(inactive.IsActive);
    }
}
