using System.Collections.Generic;
using System.ComponentModel;
using Vimium.Services;

namespace Vimium.ViewModels;

public class GeneralSettingsViewModel : NotifyPropertyChanged
{
    private readonly ConfigService _config = ConfigService.Instance;

    public GeneralSettingsViewModel()
    {
        _config.PropertyChanged += OnConfigChanged;
    }

    public string Icon => "⚙";
    public string DisplayName => "_General";

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

    public List<string> FontSizes => new()
    {
        "8", "9", "10", "11", "12", "13", "14", "15", "16",
        "17", "18", "19", "20", "21", "22", "23", "24"
    };

    public List<string> Themes => new() { "Light", "Dark", "Skadi" };

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
    }
}
