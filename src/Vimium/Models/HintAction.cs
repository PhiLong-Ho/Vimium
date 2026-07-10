using System.Text.Json.Serialization;

namespace Vimium.Models;

/// <summary>
/// Represents the type of action taken when a hint is selected.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HintAction
{
    /// <summary>UI Automation InvokePattern (default).</summary>
    Invoke,

    /// <summary>Real left mouse click via <c>mouse_event</c>.</summary>
    LeftClick,

    /// <summary>Real right mouse click via <c>mouse_event</c>.</summary>
    RightClick,

    /// <summary>Move cursor to element center (no click). Triggers CSS :hover effects.</summary>
    MoveMouse,
}

/// <summary>
/// Maps a modifier combination to a hint action. One of three fixed slots.
/// </summary>
public class ActionSlot
{
    /// <summary>0 (default, no modifier), 1, or 2.</summary>
    public int SlotIndex { get; set; }

    /// <summary>
    /// Key combination string (e.g. "Shift", "Ctrl+Shift").
    /// For slot 0 this is forced to empty string. For slots 1–2,
    /// must contain at least one modifier key.
    /// </summary>
    public string Modifier { get; set; } = "";

    /// <summary>What happens when a hint is selected with this modifier held/typed.</summary>
    public HintAction Action { get; set; } = HintAction.Invoke;

    /// <summary>
    /// How the modifier is activated: "Hold" (default, hold while typing hint)
    /// or "Type" (press and release modifier first, then type hint).
    /// </summary>
    public string Mode { get; set; } = "Hold";

    /// <summary>Creates the default four-slot configuration.</summary>
    public static ActionSlot[] CreateDefaults() => new[]
    {
        new ActionSlot { SlotIndex = 0, Modifier = "", Action = HintAction.Invoke, Mode = "Hold" },
        new ActionSlot { SlotIndex = 1, Modifier = "Shift", Action = HintAction.LeftClick, Mode = "Hold" },
        new ActionSlot { SlotIndex = 2, Modifier = "Ctrl", Action = HintAction.RightClick, Mode = "Hold" },
        new ActionSlot { SlotIndex = 3, Modifier = "Alt", Action = HintAction.MoveMouse, Mode = "Hold" },
    };
}
