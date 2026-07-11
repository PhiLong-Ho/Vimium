using System.ComponentModel;
using System.IO;
using Vimium.Models;
using Vimium.Services;
using Xunit;

namespace Vimium.Tests.Services;

/// <summary>
/// Tests for <see cref="ConfigService"/> — persistence, property-change
/// notification, and save/load roundtrip integrity.
/// </summary>
[Collection(Vimium.Tests.ConfigSingletonCollection.Name)]
public class ConfigServiceTest
{
    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Vimium", "config.json");

    /// <summary>Snapshot the config file so tests can restore it afterward.</summary>
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

    // ── RunAsAdministrator (feature 005) ──────────────────────

    [Fact]
    public void RunAsAdministrator_Get_ReflectsCurrentConfig()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            svc.RunAsAdministrator = false;
            Assert.False(svc.RunAsAdministrator);

            svc.RunAsAdministrator = true;
            Assert.True(svc.RunAsAdministrator);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }

    [Fact]
    public void RunAsAdministrator_Set_RaisesPropertyChanged()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            // Ensure a known starting state so the toggle actually changes value.
            svc.RunAsAdministrator = true;

            var raised = new List<string?>();
            PropertyChangedEventHandler handler = (_, e) => raised.Add(e.PropertyName);
            svc.PropertyChanged += handler;
            try
            {
                svc.RunAsAdministrator = false;
            }
            finally
            {
                svc.PropertyChanged -= handler;
            }

            Assert.Contains(nameof(ConfigService.RunAsAdministrator), raised);
            Assert.Contains(nameof(ConfigService.IsDirty), raised);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }

    [Fact]
    public void RunAsAdministrator_Set_SameValue_DoesNotRaise()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            svc.RunAsAdministrator = true;

            var raised = new List<string?>();
            PropertyChangedEventHandler handler = (_, e) => raised.Add(e.PropertyName);
            svc.PropertyChanged += handler;
            try
            {
                svc.RunAsAdministrator = true; // no change
            }
            finally
            {
                svc.PropertyChanged -= handler;
            }

            Assert.DoesNotContain(nameof(ConfigService.RunAsAdministrator), raised);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }

    // ── Persistence: ResetToDefaults (Bug 2 fix verification) ─

    [Fact]
    public void ResetToDefaults_PersistsToDisk()
    {
        var svc = ConfigService.Instance;
        var snapshot = ReadConfigFile();
        try
        {
            // Make a change first so we can verify reset actually saves
            svc.FontSize = "24";
            svc.Save();

            // Now reset
            svc.ResetToDefaults();

            // Read config file and verify defaults were written
            var json = ReadConfigFile();
            Assert.NotEmpty(json);
            var cfg = VimiumConfig.FromJson(json);
            Assert.Equal("14", cfg.FontSize);        // default restored on disk
            Assert.Equal("Light", cfg.Theme);         // default restored on disk
            Assert.Equal("Ctrl+;", cfg.OverlayModifier); // default restored on disk
        }
        finally
        {
            WriteConfigFile(snapshot);
            // Reload to restore in-memory state
            svc.Cancel();
        }
    }

    [Fact]
    public void ResetToDefaults_MarksClean()
    {
        var svc = ConfigService.Instance;
        var snapshot = ReadConfigFile();
        try
        {
            svc.FontSize = "24";
            Assert.True(svc.IsDirty);

            svc.ResetToDefaults();
            Assert.False(svc.IsDirty); // reset should leave config clean
        }
        finally
        {
            WriteConfigFile(snapshot);
        }
    }

    // ── Persistence: ActionSlots (Bug 1 fix verification) ────

    [Fact]
    public void ActionSlots_Change_PersistsToDisk()
    {
        var svc = ConfigService.Instance;
        var original = svc.ActionSlots;
        var snapshot = ReadConfigFile();
        try
        {
            // Create a modified copy — new array reference triggers SetProperty save
            var modified = ActionSlot.CreateDefaults();
            modified[1].Action = HintAction.Hover;
            modified[1].Modifier = "Ctrl+Alt";
            svc.ActionSlots = modified;

            // Read back from disk
            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.NotNull(cfg.ActionSlots);
            Assert.True(cfg.ActionSlots.Length >= 2);
            Assert.Equal(HintAction.Hover, cfg.ActionSlots[1].Action);
            Assert.Equal("Ctrl+Alt", cfg.ActionSlots[1].Modifier);
        }
        finally
        {
            svc.ActionSlots = original;
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void ActionSlots_SameReference_DoesNotSave()
    {
        // This test verifies that Bug 1 is understood: assigning the same
        // array reference does NOT trigger a save (reference equality).
        var svc = ConfigService.Instance;
        var original = svc.ActionSlots;
        var snapshot = ReadConfigFile();
        try
        {
            // Make a change to force a known state
            var known = ActionSlot.CreateDefaults();
            known[0].Action = HintAction.Hover;
            svc.ActionSlots = known;
            var jsonAfterKnown = ReadConfigFile();

            // Now assign the SAME reference back — should be a no-op
            var sameRef = svc.ActionSlots; // same array reference
            sameRef[0].Action = HintAction.Invoke; // mutate in-place
            svc.ActionSlots = sameRef; // reassign same reference

            var jsonAfterSameRef = ReadConfigFile();
            // File should be unchanged because SetProperty skipped the save
            Assert.Equal(jsonAfterKnown, jsonAfterSameRef);
        }
        finally
        {
            svc.ActionSlots = original;
            WriteConfigFile(snapshot);
        }
    }

    // ── Persistence: string properties ───────────────────────

    [Fact]
    public void StringProperty_Change_PersistsToDisk()
    {
        var svc = ConfigService.Instance;
        var original = svc.OverlayModifier;
        var snapshot = ReadConfigFile();
        try
        {
            svc.OverlayModifier = "Alt+X";

            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.Equal("Alt+X", cfg.OverlayModifier);
        }
        finally
        {
            svc.OverlayModifier = original;
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void BoolProperty_Change_PersistsToDisk()
    {
        var svc = ConfigService.Instance;
        var original = svc.BenchmarkLogEnabled;
        var snapshot = ReadConfigFile();
        try
        {
            // Force a change to false first (to ensure the subsequent set
            // to true triggers SetProperty), then set to true. We use true
            // because WhenWritingDefault skips CLR defaults (false for bool),
            // so only non-default values are guaranteed to appear in JSON.
            svc.BenchmarkLogEnabled = false;
            svc.BenchmarkLogEnabled = true;

            var json = ReadConfigFile();
            var cfg = VimiumConfig.FromJson(json);
            Assert.True(cfg.BenchmarkLogEnabled);
        }
        finally
        {
            svc.BenchmarkLogEnabled = original;
            WriteConfigFile(snapshot);
        }
    }

    // ── Save / Cancel ────────────────────────────────────────

    [Fact]
    public void Save_MarksNotDirty()
    {
        var svc = ConfigService.Instance;
        var snapshot = ReadConfigFile();
        try
        {
            svc.FontSize = "20";
            Assert.True(svc.IsDirty);

            svc.Save();
            Assert.False(svc.IsDirty);
        }
        finally
        {
            WriteConfigFile(snapshot);
        }
    }

    [Fact]
    public void Cancel_RevertsUnsavedChanges()
    {
        var svc = ConfigService.Instance;
        var snapshot = ReadConfigFile();
        try
        {
            svc.Save(); // ensure clean state
            var originalFontSize = svc.FontSize;
            svc.FontSize = "99";
            Assert.Equal("99", svc.FontSize);

            svc.Cancel();
            Assert.Equal(originalFontSize, svc.FontSize);
            Assert.False(svc.IsDirty);
        }
        finally
        {
            WriteConfigFile(snapshot);
        }
    }
}
