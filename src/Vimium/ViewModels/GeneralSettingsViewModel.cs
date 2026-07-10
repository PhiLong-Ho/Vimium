using System.Collections.Generic;
using System.ComponentModel;
using Vimium.Services;

namespace Vimium.ViewModels;

public class GeneralSettingsViewModel : NotifyPropertyChanged
{
    private readonly ConfigService _config = ConfigService.Instance;

    // Snapshot of the admin setting when this page was constructed. Used to
    // detect an in-session change so the "restart required" hint only appears
    // after the user actually toggles the checkbox.
    private readonly bool _initialRunAsAdmin;

    public GeneralSettingsViewModel()
    {
        _initialRunAsAdmin = _config.RunAsAdministrator;
        _config.PropertyChanged += OnConfigChanged;
    }

    public string Icon => "⚙";
    public string DisplayName => "General";

    public string FontSize
    {
        get => _config.FontSize;
        set => _config.FontSize = value;
    }

    public string Theme
    {
        get => _config.Theme;
        set => _config.Theme = value;
    }

    public string Language
    {
        get => _config.Language;
        set => _config.Language = value;
    }

    // ── Administrator Mode (feature 005) ─────────────────────

    /// <summary>
    /// Whether Vimium should launch elevated. Delegates to
    /// <see cref="ConfigService.RunAsAdministrator"/> (auto-saved on change).
    /// </summary>
    public bool RunAsAdministrator
    {
        get => _config.RunAsAdministrator;
        set
        {
            _config.RunAsAdministrator = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(ShowRestartMessage));
        }
    }

    /// <summary>
    /// True once the admin setting differs from its value when the page opened,
    /// i.e. the user toggled it and a restart is needed to take effect. Absent
    /// on a freshly opened settings window that matches the saved state.
    /// </summary>
    public bool ShowRestartMessage => _config.RunAsAdministrator != _initialRunAsAdmin;

    public List<string> FontSizes => new()
    {
        "8", "9", "10", "11", "12", "13", "14", "15", "16",
        "17", "18", "19", "20", "21", "22", "23", "24"
    };

    public List<string> Themes => new() { "Light", "Dark", "Arknights" };

    public List<string> Languages => new() { "English" };

    private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
    {
        var name = e.PropertyName;
        if (name is null or "" or "FontSize")
            NotifyOfPropertyChange(nameof(FontSize));
        if (name is null or "" or "Theme")
            NotifyOfPropertyChange(nameof(Theme));
        if (name is null or "" or "Language")
            NotifyOfPropertyChange(nameof(Language));
        if (name is null or "" or "RunAsAdministrator")
        {
            NotifyOfPropertyChange(nameof(RunAsAdministrator));
            NotifyOfPropertyChange(nameof(ShowRestartMessage));
        }
    }
}
