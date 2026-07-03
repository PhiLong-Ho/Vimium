using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Vimium.Properties;
using Vimium.Models;

namespace Vimium.Services;

/// <summary>
/// Singleton service for loading, saving, and tracking Vimium configuration.
/// Migrates FontSize from the old Settings.settings on first run.
/// </summary>
public class ConfigService : INotifyPropertyChanged
{
    private static readonly Lazy<ConfigService> _instance = new(() => new ConfigService());

    public static ConfigService Instance => _instance.Value;

    private readonly string _configDir;
    private readonly string _configPath;

    private VimiumConfig _current;
    private VimiumConfig _saved;

    private ConfigService()
    {
        _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Vimium");
        _configPath = Path.Combine(_configDir, "config.json");

        _current = Load();
        _saved = Clone(_current);
    }

    // ── Current config (binding target) ──────────────────────

    public VimiumConfig Current => _current;

    /// <summary>True when Current differs from the last saved state.</summary>
    public bool IsDirty => _current.ToJson() != _saved.ToJson();

    // ── Convenience properties (for direct binding) ──────────

    public string FontSize
    {
        get => _current.FontSize;
        set { if (SetProperty(_current.FontSize, value, v => _current.FontSize = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public string Theme
    {
        get => _current.Theme;
        set { if (SetProperty(_current.Theme, value, v => _current.Theme = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public string Language
    {
        get => _current.Language;
        set { if (SetProperty(_current.Language, value, v => _current.Language = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public string HintFontFamily
    {
        get => _current.HintFontFamily;
        set { if (SetProperty(_current.HintFontFamily, value, v => _current.HintFontFamily = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public string HintActiveBackground
    {
        get => _current.HintActiveBackground;
        set { if (SetProperty(_current.HintActiveBackground, value, v => _current.HintActiveBackground = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public string HintInactiveBackground
    {
        get => _current.HintInactiveBackground;
        set { if (SetProperty(_current.HintInactiveBackground, value, v => _current.HintInactiveBackground = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public string HintTextColor
    {
        get => _current.HintTextColor;
        set { if (SetProperty(_current.HintTextColor, value, v => _current.HintTextColor = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public bool HintAnimationEnabled
    {
        get => _current.HintAnimationEnabled;
        set { if (SetProperty(_current.HintAnimationEnabled, value, v => _current.HintAnimationEnabled = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public string OverlayModifier
    {
        get => _current.OverlayModifier;
        set { if (SetProperty(_current.OverlayModifier, value, v => _current.OverlayModifier = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public string TaskbarModifier
    {
        get => _current.TaskbarModifier;
        set { if (SetProperty(_current.TaskbarModifier, value, v => _current.TaskbarModifier = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    // ── Load / Save / Reset ──────────────────────────────────

    private VimiumConfig Load()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                return VimiumConfig.FromJson(json);
            }
            catch
            {
                // Corrupt config — fall through to migration / defaults
            }
        }

        // Try one-time migration from old Settings.settings
        var migrated = MigrateFromLegacy();
        if (migrated != null)
        {
            SaveInternal(migrated);
            return migrated;
        }

        var defaults = VimiumConfig.Default;
        SaveInternal(defaults);
        return defaults;
    }

    public void Save()
    {
        SaveInternal(_current);
        _saved = Clone(_current);
        OnPropertyChanged(nameof(IsDirty));
    }

    public void Cancel()
    {
        _current = Clone(_saved);
        OnPropertyChanged(null); // refresh all bindings
        OnPropertyChanged(nameof(IsDirty));
    }

    public void ResetToDefaults()
    {
        _current = Clone(VimiumConfig.Default);
        OnPropertyChanged(null);
        OnPropertyChanged(nameof(IsDirty));
    }

    // ── Migration ────────────────────────────────────────────

    private VimiumConfig? MigrateFromLegacy()
    {
        try
        {
            var oldFontSize = Settings.Default.FontSize;
            if (string.IsNullOrEmpty(oldFontSize) || oldFontSize == "14")
                return null; // already at default — no migration needed

            var config = VimiumConfig.Default;
            config.FontSize = oldFontSize;
            return config;
        }
        catch
        {
            return null;
        }
    }

    // ── Helpers ──────────────────────────────────────────────

    private void SaveInternal(VimiumConfig config)
    {
        Directory.CreateDirectory(_configDir);
        File.WriteAllText(_configPath, config.ToJson());
    }

    private static VimiumConfig Clone(VimiumConfig source) =>
        VimiumConfig.FromJson(source.ToJson());

    private bool SetProperty<T>(T currentValue, T newValue, Action<T> setter, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            return false;

        setter(newValue);
        OnPropertyChanged(propertyName!);
        SaveInternal(_current);  // auto-save on every change
        return true;
    }

    // ── INotifyPropertyChanged ───────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
