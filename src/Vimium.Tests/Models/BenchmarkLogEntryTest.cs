using Vimium.Models;
using Xunit;

namespace Vimium.Tests.Models;

/// <summary>
/// Tests for BenchmarkLogEntry JSON serialization roundtrip.
/// </summary>
public class BenchmarkLogEntryTest
{
    [Fact]
    public void SerializeDeserialize_ProducesIdenticalValues()
    {
        var original = new BenchmarkLogEntry
        {
            Timestamp = new DateTime(2026, 7, 10, 14, 30, 0, DateTimeKind.Utc),
            WindowTitle = "Wikipedia — Google Chrome",
            ElementCount = 200,
            ElapsedMs = 650,
            CacheHit = false,
            FilterMode = "InvokeFiltered",
        };

        var json = original.ToJson();
        var restored = BenchmarkLogEntry.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal(original.WindowTitle, restored!.WindowTitle);
        Assert.Equal(original.ElementCount, restored.ElementCount);
        Assert.Equal(original.ElapsedMs, restored.ElapsedMs);
        Assert.Equal(original.CacheHit, restored.CacheHit);
        Assert.Equal(original.FilterMode, restored.FilterMode);
    }

    [Fact]
    public void SerializeDeserialize_WithCacheHit_ProducesIdenticalValues()
    {
        var original = new BenchmarkLogEntry
        {
            Timestamp = DateTime.UtcNow,
            WindowTitle = "Notepad",
            ElementCount = 15,
            ElapsedMs = 5,
            CacheHit = true,
            FilterMode = "InvokeFiltered",
        };

        var json = original.ToJson();
        var restored = BenchmarkLogEntry.FromJson(json);

        Assert.NotNull(restored);
        Assert.True(restored!.CacheHit);
        Assert.Equal(5, restored.ElapsedMs);
    }

    [Fact]
    public void SerializeDeserialize_AllElementsMode_ProducesIdenticalValues()
    {
        var original = new BenchmarkLogEntry
        {
            Timestamp = DateTime.UtcNow,
            WindowTitle = "Taskbar",
            ElementCount = 30,
            ElapsedMs = 200,
            CacheHit = false,
            FilterMode = "AllElements",
        };

        var json = original.ToJson();
        var restored = BenchmarkLogEntry.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal("AllElements", restored!.FilterMode);
    }

    [Fact]
    public void ToJson_IsSingleLine()
    {
        var entry = new BenchmarkLogEntry
        {
            Timestamp = DateTime.UtcNow,
            WindowTitle = "Test",
            ElementCount = 1,
            ElapsedMs = 10,
            CacheHit = false,
            FilterMode = "InvokeFiltered",
        };

        var json = entry.ToJson();

        Assert.DoesNotContain("\n", json);
        Assert.DoesNotContain("\r", json);
    }

    [Fact]
    public void FromJson_InvalidJson_ReturnsNull()
    {
        var result = BenchmarkLogEntry.FromJson("{invalid json content");
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_EmptyString_ReturnsNull()
    {
        var result = BenchmarkLogEntry.FromJson("");
        Assert.Null(result);
    }

    [Fact]
    public void Default_Properties_HaveExpectedDefaults()
    {
        var entry = new BenchmarkLogEntry();

        Assert.Equal("", entry.WindowTitle);
        Assert.Equal(0, entry.ElementCount);
        Assert.Equal(0, entry.ElapsedMs);
        Assert.False(entry.CacheHit);
        Assert.Equal("", entry.FilterMode);
    }
}
