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
    /// When true (default), the app self-elevates on startup via the "runas"
    /// verb. When false, it runs in the user's current privilege context.
    /// Missing key on load defaults to true (preserves legacy always-elevated
    /// behavior for upgrading users).
    /// </summary>
    /// <remarks>
    /// Forced to always serialize (<see cref="JsonIgnoreCondition.Never"/>).
    /// The class-wide <c>WhenWritingDefault</c> policy keys off the CLR type
    /// default (<c>false</c> for bool), which would omit an explicit
    /// <c>false</c> and silently re-default it to <c>true</c> on reload —
    /// breaking the "disable admin mode" preference. Always writing the key
    /// guarantees a chosen <c>false</c> round-trips correctly.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public bool RunAsAdministrator { get; set; } = true;

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
