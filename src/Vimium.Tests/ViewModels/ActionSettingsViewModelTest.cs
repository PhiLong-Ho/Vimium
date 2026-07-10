using Vimium.Models;
using Vimium.Services;
using Vimium.ViewModels;
using Xunit;

namespace Vimium.Tests.ViewModels;

/// <summary>
/// Tests for the combined Keyboard & Actions settings ViewModel.
/// </summary>
[Collection(ConfigSingletonCollection.Name)]
public class ActionSettingsViewModelTest
{
    public ActionSettingsViewModelTest()
    {
        ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
        ConfigService.Instance.BenchmarkLogEnabled = true;
    }

    [Fact]
    public void DisplayName_IsKeyboardAndActions()
    {
        var vm = new ActionSettingsViewModel();
        Assert.Equal("Keyboard & Actions", vm.DisplayName);
    }

    [Fact]
    public void Slot0Action_Change_PersistsToConfig()
    {
        var vm = new ActionSettingsViewModel();
        vm.Slot0Action = HintAction.Hover;
        Assert.Equal(HintAction.Hover, ConfigService.Instance.ActionSlots[0].Action);

        ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
    }

    [Fact]
    public void Slot1Modifier_Change_PersistsToConfig()
    {
        var vm = new ActionSettingsViewModel();
        vm.Slot1Modifier = "Ctrl+Alt";
        Assert.Equal("Ctrl+Alt", ConfigService.Instance.ActionSlots[1].Modifier);

        ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
    }

    [Fact]
    public void Slot1Action_Change_PersistsToConfig()
    {
        var vm = new ActionSettingsViewModel();
        vm.Slot1Action = HintAction.Hover;
        Assert.Equal(HintAction.Hover, ConfigService.Instance.ActionSlots[1].Action);

        ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
    }

    [Fact]
    public void Slot1Mode_Change_PersistsToConfig()
    {
        var vm = new ActionSettingsViewModel();
        vm.Slot1Mode = "Type";
        Assert.Equal("Type", ConfigService.Instance.ActionSlots[1].Mode);

        ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
    }

    [Fact]
    public void Slot1Mode_DefaultIsHold()
    {
        var vm = new ActionSettingsViewModel();
        Assert.Equal("Hold", vm.Slot1Mode);
    }

    [Fact]
    public void Slot2Modifier_Change_PersistsToConfig()
    {
        var vm = new ActionSettingsViewModel();
        vm.Slot2Modifier = "Win";
        Assert.Equal("Win", ConfigService.Instance.ActionSlots[2].Modifier);

        ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
    }

    [Fact]
    public void Slot2Action_Change_PersistsToConfig()
    {
        var vm = new ActionSettingsViewModel();
        vm.Slot2Action = HintAction.RightClick;
        Assert.Equal(HintAction.RightClick, ConfigService.Instance.ActionSlots[2].Action);

        ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
    }

    [Fact]
    public void Slot0Modifier_IsAlwaysNone()
    {
        var vm = new ActionSettingsViewModel();
        Assert.Equal("(none)", vm.Slot0Modifier);
    }

    [Fact]
    public void AvailableActions_ContainsAllFour()
    {
        var actions = ActionSettingsViewModel.AvailableActions;
        Assert.Equal(4, actions.Length);
        Assert.Contains(HintAction.Invoke, actions);
        Assert.Contains(HintAction.LeftClick, actions);
        Assert.Contains(HintAction.RightClick, actions);
        Assert.Contains(HintAction.Hover, actions);
    }

    [Fact]
    public void AvailableModes_ContainsHoldAndType()
    {
        var modes = ActionSettingsViewModel.AvailableModes;
        Assert.Equal(2, modes.Length);
        Assert.Contains("Hold", modes);
        Assert.Contains("Type", modes);
    }

    [Fact]
    public void OverlayModifier_Change_PersistsToConfig()
    {
        var vm = new ActionSettingsViewModel();
        vm.OverlayModifier = "Alt+X";
        Assert.Equal("Alt+X", ConfigService.Instance.OverlayModifier);

        ConfigService.Instance.OverlayModifier = "Ctrl+;";
    }

    [Fact]
    public void OverlayModifier_CannotDuplicateFindText()
    {
        var vm = new ActionSettingsViewModel();
        var original = ConfigService.Instance.OverlayModifier;
        ConfigService.Instance.LineNavigationModifier = "Alt+.";

        vm.OverlayModifier = "Alt+."; // same as find text — should be rejected
        Assert.NotEqual("Alt+.", ConfigService.Instance.OverlayModifier);

        ConfigService.Instance.OverlayModifier = original;
        ConfigService.Instance.LineNavigationModifier = "Ctrl+.";
    }

    [Fact]
    public void LineNavigationModifier_CannotDuplicateOverlay()
    {
        var vm = new ActionSettingsViewModel();
        ConfigService.Instance.OverlayModifier = "Ctrl+;";

        vm.LineNavigationModifier = "Ctrl+;"; // same as overlay — rejected
        Assert.NotEqual("Ctrl+;", ConfigService.Instance.LineNavigationModifier);

        ConfigService.Instance.LineNavigationModifier = "Ctrl+.";
    }
}
