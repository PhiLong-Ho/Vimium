using Vimium.Models;
using Vimium.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Vimium.Services;

/// <summary>
/// Writes enumeration session metrics to a rolling JSONL log file.
/// Local-only — no telemetry. Thread-safe.
/// </summary>
public class BenchmarkService : IBenchmarkService
{
    private readonly string _logDir;
    private readonly string _logPath;
    private readonly object _lock = new();
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public BenchmarkService()
    {
        _logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Vimium", "logs");
        _logPath = Path.Combine(_logDir, "benchmark.jsonl");
    }

    /// <summary>
    /// True if benchmark logging is enabled in user configuration.
    /// </summary>
    public bool IsEnabled => ConfigService.Instance.BenchmarkLogEnabled;

    /// <inheritdoc />
    public void LogSession(BenchmarkLogEntry entry)
    {
        if (!IsEnabled)
            return;

        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(_logDir);

                // Rolling policy: if file exceeds 10 MB, evict oldest half
                if (File.Exists(_logPath))
                {
                    var fileInfo = new FileInfo(_logPath);
                    if (fileInfo.Length > MaxFileSize)
                    {
                        var allLines = File.ReadAllLines(_logPath);
                        var keepStart = allLines.Length / 2;
                        var recent = allLines.Skip(keepStart).ToList();
                        File.WriteAllLines(_logPath, recent);
                    }
                }

                // Append one JSON line
                File.AppendAllText(_logPath, entry.ToJson() + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never break the feature — silently drop
        }
    }

    /// <inheritdoc />
    public void InvalidateCache()
    {
        // Cache invalidation is delegated to IHintProviderService.
        // This method exists for the benchmark script to trigger cold starts
        // by clearing the cache before each measurement.
        // The actual implementation wires through ShellViewModel.
    }
}
