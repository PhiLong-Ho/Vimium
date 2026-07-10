using System.ComponentModel;
using System.Windows.Input;
using Vimium.Models;
using Vimium.Services;

namespace Vimium.ViewModels;

/// <summary>
/// ViewModel for the combined "Keyboard & Actions" settings page.
/// Includes overlay/taskbar/find hotkeys and configurable
/// modifier→action slots with simple text-based modifier input.
/// </summary>
public class ActionSettingsViewModel : NotifyPropertyChanged
{
    private readonly ConfigService _config = ConfigService.Instance;

    public ActionSettingsViewModel()
    {
        _config.PropertyChanged += OnConfigChanged;
    }

    public string Icon => "⌨";  // ⌨ keyboard
    public string DisplayName => "Keyboard & Actions";

    // ── Action types (all 5 for all 3 slots) ────────────────

    public static HintAction[] AvailableActions => new[]
    {
        HintAction.Invoke,
        HintAction.LeftClick,
        HintAction.RightClick,
        HintAction.Hover,
    };

    // ── Slot 0 (default — modifier is always empty) ─────────

    public string Slot0Modifier => "(none)";
    public HintAction Slot0Action
    {
        get => _config.ActionSlots.Length > 0 ? _config.ActionSlots[0].Action : HintAction.Invoke;
        set => SetSlotAction(0, value);
    }

    // ── Slot 1 ──────────────────────────────────────────────

    public string Slot1Modifier
    {
        get => _config.ActionSlots.Length > 1 ? _config.ActionSlots[1].Modifier : "";
        set => SetSlotModifier(1, value);
    }

    public HintAction Slot1Action
    {
        get => _config.ActionSlots.Length > 1 ? _config.ActionSlots[1].Action : HintAction.Invoke;
        set => SetSlotAction(1, value);
    }

    // ── Slot 2 ──────────────────────────────────────────────

    public string Slot2Modifier
    {
        get => _config.ActionSlots.Length > 2 ? _config.ActionSlots[2].Modifier : "";
        set => SetSlotModifier(2, value);
    }

    public HintAction Slot2Action
    {
        get => _config.ActionSlots.Length > 2 ? _config.ActionSlots[2].Action : HintAction.Invoke;
        set => SetSlotAction(2, value);
    }

    // ── Modifier mode (Hold / Type) ─────────────────────────

    public static string[] AvailableModes => new[] { "Hold", "Type" };

    public string Slot1Mode
    {
        get => _config.ActionSlots.Length > 1 ? (_config.ActionSlots[1].Mode ?? "Hold") : "Hold";
        set => SetSlotMode(1, value);
    }

    public string Slot2Mode
    {
        get => _config.ActionSlots.Length > 2 ? (_config.ActionSlots[2].Mode ?? "Hold") : "Hold";
        set => SetSlotMode(2, value);
    }

    // ── Slot 3 ──────────────────────────────────────────────

    public string Slot3Modifier
    {
        get => _config.ActionSlots.Length > 3 ? _config.ActionSlots[3].Modifier : "";
        set => SetSlotModifier(3, value);
    }

    public HintAction Slot3Action
    {
        get => _config.ActionSlots.Length > 3 ? _config.ActionSlots[3].Action : HintAction.Invoke;
        set => SetSlotAction(3, value);
    }

    public string Slot3Mode
    {
        get => _config.ActionSlots.Length > 3 ? (_config.ActionSlots[3].Mode ?? "Hold") : "Hold";
        set => SetSlotMode(3, value);
    }

    // ── Keyboard shortcuts (merged from old KeyboardSettingsViewModel) ──

    public string OverlayModifier
    {
        get => _config.OverlayModifier;
        set
        {
            if (value == _config.LineNavigationModifier) return;
            _config.OverlayModifier = value;
        }
    }

    public string TaskbarModifier
    {
        get => _config.TaskbarModifier;
        set => _config.TaskbarModifier = value;
    }

    public string LineNavigationModifier
    {
        get => _config.LineNavigationModifier;
        set
        {
            if (value == _config.OverlayModifier) return;
            _config.LineNavigationModifier = value;
        }
    }

    public string CopyModifier
    {
        get => _config.CopyModifier;
        set => _config.CopyModifier = value;
    }

    // ── Slot modification helpers ──────────────────────────

    private void SetSlotModifier(int index, string value)
    {
        var slots = _config.ActionSlots;
        if (index >= 0 && index < slots.Length)
        {
            slots[index].Modifier = value;
            _config.ActionSlots = slots;
        }
    }

    private void SetSlotAction(int index, HintAction value)
    {
        var slots = _config.ActionSlots;
        if (index >= 0 && index < slots.Length)
        {
            slots[index].Action = value;
            _config.ActionSlots = slots;
        }
    }

    private void SetSlotMode(int index, string value)
    {
        var slots = _config.ActionSlots;
        if (index >= 0 && index < slots.Length)
        {
            slots[index].Mode = value;
            _config.ActionSlots = slots;
        }
    }

    private void NotifySlotProperties()
    {
        NotifyOfPropertyChange(nameof(Slot0Action));
        NotifyOfPropertyChange(nameof(Slot1Modifier));
        NotifyOfPropertyChange(nameof(Slot1Action));
        NotifyOfPropertyChange(nameof(Slot1Mode));
        NotifyOfPropertyChange(nameof(Slot2Modifier));
        NotifyOfPropertyChange(nameof(Slot2Action));
        NotifyOfPropertyChange(nameof(Slot2Mode));
        NotifyOfPropertyChange(nameof(Slot3Modifier));
        NotifyOfPropertyChange(nameof(Slot3Action));
        NotifyOfPropertyChange(nameof(Slot3Mode));
    }

    private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
    {
        var name = e.PropertyName;
        if (name is null or "" or "ActionSlots")
            NotifySlotProperties();
        if (name is null or "" or "OverlayModifier")
            NotifyOfPropertyChange(nameof(OverlayModifier));
        if (name is null or "" or "TaskbarModifier")
            NotifyOfPropertyChange(nameof(TaskbarModifier));
        if (name is null or "" or "LineNavigationModifier")
            NotifyOfPropertyChange(nameof(LineNavigationModifier));
        if (name is null or "" or "CopyModifier")
            NotifyOfPropertyChange(nameof(CopyModifier));
    }
}
