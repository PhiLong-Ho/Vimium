using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vimium.Models;

/// <summary>
/// Root configuration for Vimium. Serialized to %APPDATA%\Vimium\config.json.
/// </summary>
public class VimiumConfig
{
    // ── General ──────────────────────────────────────────────

    public string FontSize { get; set; } = "14";

    public string Theme { get; set; } = "Light";

    public string Language { get; set; } = "en";

    // ── Overlay ──────────────────────────────────────────────

    /// <summary>Hint font family name. Empty = system default.</summary>
    public string HintFontFamily { get; set; } = "";

    /// <summary>Background color for the active hint (hex #RRGGBB).</summary>
    public string HintActiveBackground { get; set; } = "#FFFFFF";

    /// <summary>Background color for inactive hints (hex #RRGGBB).</summary>
    public string HintInactiveBackground { get; set; } = "#F0F0F0";

    /// <summary>Text color for hints (hex #RRGGBB).</summary>
    public string HintTextColor { get; set; } = "#000000";

    /// <summary>Whether the loading indicator pulse animation is enabled.</summary>
    public bool HintAnimationEnabled { get; set; } = true;

    // ── Keyboard ─────────────────────────────────────────────

    /// <summary>Modifier key(s) for overlay activation (e.g. "Ctrl+;").</summary>
    public string OverlayModifier { get; set; } = "Ctrl+;";

    /// <summary>Modifier key(s) for taskbar overlay activation.</summary>
    public string TaskbarModifier { get; set; } = "Ctrl+'";

    // ── Actions ───────────────────────────────────────────────

    /// <summary>
    /// Three configured modifier→action mappings for hint selection.
    /// Slot 0 is the default (no modifier). Slot 1 and 2 are alternate actions
    /// triggered by holding the configured modifier key(s).
    /// </summary>
    public ActionSlot[] ActionSlots { get; set; } = ActionSlot.CreateDefaults();

    // ── Line Navigation ──────────────────────────────────────

    /// <summary>Hotkey for line-navigation activation (e.g. "Ctrl+.").</summary>
    public string LineNavigationModifier { get; set; } = "Ctrl+.";

    /// <summary>Modifier key held while typing hint label for copy action (e.g. "Ctrl").</summary>
    public string CopyModifier { get; set; } = "Ctrl";

    // ── Benchmark ─────────────────────────────────────────────

    /// <summary>
    /// Whether enumeration sessions are logged to the benchmark JSONL file.
    /// Local-only — no telemetry. Default: true.
    /// </summary>
    public bool BenchmarkLogEnabled { get; set; } = true;

    // ── Elevation ─────────────────────────────────────────────

    /// <summary>
    /// Whether the application should launch with administrator privileges.
    /// When false (the default), Vimium runs in the user's current privilege
    /// context with no UAC prompt — the enterprise-friendly behavior most
    /// managed environments require. When true, the app self-elevates on
    /// startup via the "runas" verb. Missing key on load defaults to false.
    /// </summary>
    /// <remarks>
    /// Forced to always serialize (<see cref="JsonIgnoreCondition.Never"/>) so
    /// the key is always present in config.json — discoverable for users who
    /// want to opt into elevation, and guaranteeing whichever value the user
    /// chooses round-trips. Without this, the class-wide <c>WhenWritingDefault</c>
    /// policy would omit the CLR-default <c>false</c> from the file entirely.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public bool RunAsAdministrator { get; set; } = false;

    // ── Factory ──────────────────────────────────────────────

    public static VimiumConfig Default => new();

    // ── Serialization ────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static VimiumConfig FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Default;

        try
        {
            var config = JsonSerializer.Deserialize<VimiumConfig>(json, JsonOptions);

            if (config == null)
                return Default;

            // FR-008: Reset only the Theme field if it holds an unrecognized value
            // (including the legacy "Skadi"). All other fields are preserved.
            if (config.Theme is not ("Light" or "Dark" or "Arknights"))
                config.Theme = "Light";

            return config;
        }
        catch
        {
            return Default;
        }
    }
}
