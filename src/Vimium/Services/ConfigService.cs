using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        set
        {
            if (!SetProperty(_current.Theme, value, v => _current.Theme = v)) return;
            ApplyThemeHintDefaults(value);
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(AppIcon));
        }
    }

    /// <summary>
    /// Application icon that changes with the active theme.
    /// Returns the keyboard icon for Light/Dark and the Arknights icon for
    /// the Arknights theme. Falls back to the keyboard icon on load failure.
    /// </summary>
    public ImageSource AppIcon => ResolveAppIcon();

    // Lazy — defer BitmapImage creation until first WPF access (avoids
    // crashing in test runs that lack a Dispatcher).
    private static readonly Lazy<BitmapImage> KeyboardIcon = new(() =>
        CreatePackIcon("pack://application:,,,/Resources/keyboard.ico"));

    private static readonly Lazy<BitmapImage> SkadiIcon = new(() =>
        CreatePackIcon("pack://application:,,,/Resources/skadi.ico"));

    private static BitmapImage CreatePackIcon(string packUri)
    {
        var icon = new BitmapImage();
        icon.BeginInit();
        icon.UriSource = new Uri(packUri, UriKind.Absolute);
        icon.CacheOption = BitmapCacheOption.OnLoad;
        icon.EndInit();
        icon.Freeze();
        return icon;
    }

    private BitmapImage ResolveAppIcon()
    {
        if (_current.Theme == "Arknights")
        {
            try { return SkadiIcon.Value; }
            catch
            {
                LogService.Warn("Failed to load Arknights app icon, falling back to keyboard icon.");
            }
        }
        return KeyboardIcon.Value;
    }

    /// <summary>Apply hint color defaults matching the theme's exact UI colors.</summary>
    private void ApplyThemeHintDefaults(string theme)
    {
        switch (theme)
        {
            case "Dark":
                _current.HintActiveBackground = "#2A2A2A";   // card background
                _current.HintInactiveBackground = "#1A1A1A"; // window background
                _current.HintTextColor = "#F0F0F0";           // text primary
                break;
            case "Arknights":
                _current.HintActiveBackground = "#152535";   // card background
                _current.HintInactiveBackground = "#0D1B2A"; // window background
                _current.HintTextColor = "#E8F4FF";           // text primary
                break;
            default: // Light
                _current.HintActiveBackground = "#FFFFFF";   // card background
                _current.HintInactiveBackground = "#F0F0F0"; // window background
                _current.HintTextColor = "#1A1A1A";           // text primary
                break;
        }
        // Notify all hint color bindings and save
        OnPropertyChanged(nameof(HintActiveBackground));
        OnPropertyChanged(nameof(HintInactiveBackground));
        OnPropertyChanged(nameof(HintTextColor));
        OnPropertyChanged(nameof(IsDirty));
        SaveInternal(_current);
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

    public ActionSlot[] ActionSlots
    {
        get => _current.ActionSlots;
        set { if (SetProperty(_current.ActionSlots, value, v => _current.ActionSlots = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public bool BenchmarkLogEnabled
    {
        get => _current.BenchmarkLogEnabled;
        set { if (SetProperty(_current.BenchmarkLogEnabled, value, v => _current.BenchmarkLogEnabled = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public bool RunAsAdministrator
    {
        get => _current.RunAsAdministrator;
        set { if (SetProperty(_current.RunAsAdministrator, value, v => _current.RunAsAdministrator = v)) OnPropertyChanged(nameof(IsDirty)); }
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

    public string LineNavigationModifier
    {
        get => _current.LineNavigationModifier;
        set { if (SetProperty(_current.LineNavigationModifier, value, v => _current.LineNavigationModifier = v)) OnPropertyChanged(nameof(IsDirty)); }
    }

    public string CopyModifier
    {
        get => _current.CopyModifier;
        set { if (SetProperty(_current.CopyModifier, value, v => _current.CopyModifier = v)) OnPropertyChanged(nameof(IsDirty)); }
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

    private VimiumConfig MigrateFromLegacy()
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

    private bool SetProperty<T>(T currentValue, T newValue, Action<T> setter, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            return false;

        setter(newValue);
        OnPropertyChanged(propertyName!);
        SaveInternal(_current);  // auto-save on every change
        return true;
    }

    // ── INotifyPropertyChanged ───────────────────────────────

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
