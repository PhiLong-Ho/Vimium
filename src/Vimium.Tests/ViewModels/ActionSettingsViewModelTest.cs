using System.IO;
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
    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Vimium", "config.json");

    private static string ReadConfigFile()
    {
        try { return File.ReadAllText(ConfigPath); }
        catch { return ""; }
    }

    private static void WriteConfigFile(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, json);
    }

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

    // ── Disk persistence (regression tests for Bug #1 — array reference equality) ──

    [Fact]
    public void Slot1Modifier_Change_PersistsToDisk()
    {
        var snapshot = ReadConfigFile();
        try
        {
            var vm = new ActionSettingsViewModel();
            vm.Slot1Modifier = "Ctrl+Shift";

            // Verify the change was written to the config file on disk
            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.NotNull(cfg.ActionSlots);
            Assert.True(cfg.ActionSlots.Length >= 2);
            Assert.Equal("Ctrl+Shift", cfg.ActionSlots[1].Modifier);
        }
        finally
        {
            ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void Slot1Action_Change_PersistsToDisk()
    {
        var snapshot = ReadConfigFile();
        try
        {
            var vm = new ActionSettingsViewModel();
            vm.Slot1Action = HintAction.Hover;

            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.NotNull(cfg.ActionSlots);
            Assert.True(cfg.ActionSlots.Length >= 2);
            Assert.Equal(HintAction.Hover, cfg.ActionSlots[1].Action);
        }
        finally
        {
            ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void Slot1Mode_Change_PersistsToDisk()
    {
        var snapshot = ReadConfigFile();
        try
        {
            var vm = new ActionSettingsViewModel();
            vm.Slot1Mode = "Type";

            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.NotNull(cfg.ActionSlots);
            Assert.True(cfg.ActionSlots.Length >= 2);
            Assert.Equal("Type", cfg.ActionSlots[1].Mode);
        }
        finally
        {
            ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void Slot2Modifier_Change_PersistsToDisk()
    {
        var snapshot = ReadConfigFile();
        try
        {
            var vm = new ActionSettingsViewModel();
            vm.Slot2Modifier = "Win+Shift";

            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.NotNull(cfg.ActionSlots);
            Assert.True(cfg.ActionSlots.Length >= 3);
            Assert.Equal("Win+Shift", cfg.ActionSlots[2].Modifier);
        }
        finally
        {
            ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void Slot3Modifier_Change_PersistsToDisk()
    {
        var snapshot = ReadConfigFile();
        try
        {
            var vm = new ActionSettingsViewModel();
            vm.Slot3Modifier = "Alt+Ctrl";

            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.NotNull(cfg.ActionSlots);
            Assert.True(cfg.ActionSlots.Length >= 4);
            Assert.Equal("Alt+Ctrl", cfg.ActionSlots[3].Modifier);
        }
        finally
        {
            ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void Slot3Action_Change_PersistsToDisk()
    {
        var snapshot = ReadConfigFile();
        try
        {
            var vm = new ActionSettingsViewModel();
            vm.Slot3Action = HintAction.LeftClick;

            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.NotNull(cfg.ActionSlots);
            Assert.True(cfg.ActionSlots.Length >= 4);
            Assert.Equal(HintAction.LeftClick, cfg.ActionSlots[3].Action);
        }
        finally
        {
            ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void Slot0Action_Change_PersistsToDisk()
    {
        var snapshot = ReadConfigFile();
        try
        {
            var vm = new ActionSettingsViewModel();
            vm.Slot0Action = HintAction.Hover;

            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.NotNull(cfg.ActionSlots);
            Assert.True(cfg.ActionSlots.Length >= 1);
            Assert.Equal(HintAction.Hover, cfg.ActionSlots[0].Action);
        }
        finally
        {
            ConfigService.Instance.ActionSlots = ActionSlot.CreateDefaults();
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void OverlayModifier_Change_PersistsToDisk()
    {
        var snapshot = ReadConfigFile();
        try
        {
            var vm = new ActionSettingsViewModel();
            vm.OverlayModifier = "Ctrl+Alt+O";

            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.Equal("Ctrl+Alt+O", cfg.OverlayModifier);
        }
        finally
        {
            ConfigService.Instance.OverlayModifier = "Ctrl+;";
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void TaskbarModifier_Change_PersistsToDisk()
    {
        var snapshot = ReadConfigFile();
        try
        {
            var vm = new ActionSettingsViewModel();
            vm.TaskbarModifier = "Ctrl+Alt+T";

            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.Equal("Ctrl+Alt+T", cfg.TaskbarModifier);
        }
        finally
        {
            ConfigService.Instance.TaskbarModifier = "Ctrl+'";
            WriteConfigFile(snapshot);
        }
    }
}
