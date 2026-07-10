namespace Vimium.Services.Interfaces;

/// <summary>
/// Logs enumeration session metrics as structured JSON entries
/// to a rolling log file. Local-only — no telemetry.
/// </summary>
public interface IBenchmarkService
{
    /// <summary>
    /// Writes a benchmark entry for the just-completed enumeration session.
    /// No-op if benchmark logging is disabled in config.
    /// </summary>
    void LogSession(Models.BenchmarkLogEntry entry);

    /// <summary>
    /// Clears the enumeration result cache, forcing the next activation
    /// to perform a full enumeration. Used by the benchmark procedure
    /// to ensure cold-start measurements.
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// True if benchmark logging is enabled in user configuration.
    /// </summary>
    bool IsEnabled { get; }
}
