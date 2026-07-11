using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vimium.Models;

/// <summary>
/// A single enumeration session record for performance tracking.
/// Serialized as one JSON object per line in benchmark.jsonl.
/// </summary>
public class BenchmarkLogEntry
{
    /// <summary>Shared serializer options — reused to avoid per-call allocation (CA1869).</summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>When enumeration completed (ISO 8601).</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Foreground window title at time of activation.</summary>
    public string WindowTitle { get; set; } = "";

    /// <summary>Total hint labels displayed.</summary>
    public int ElementCount { get; set; }

    /// <summary>Milliseconds from FindAllBuildCache start to hint set complete.</summary>
    public int ElapsedMs { get; set; }

    /// <summary>True if this session reused cached results.</summary>
    public bool CacheHit { get; set; }

    /// <summary>"InvokeFiltered" or "AllElements" — which filter was active.</summary>
    public string FilterMode { get; set; } = "";

    /// <summary>Serializes this entry to a single-line JSON string.</summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, SerializerOptions);
    }

    /// <summary>Deserializes a BenchmarkLogEntry from a JSON string.</summary>
    public static BenchmarkLogEntry FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<BenchmarkLogEntry>(json, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }
}
