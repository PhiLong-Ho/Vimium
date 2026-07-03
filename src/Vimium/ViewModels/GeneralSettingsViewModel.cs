using System.Collections.Generic;
using Vimium.Services;

namespace Vimium.ViewModels;

public class GeneralSettingsViewModel : NotifyPropertyChanged
{
    private readonly ConfigService _config = ConfigService.Instance;

    public string Icon => "⚙";  // ⚙ gear
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

    /// <summary>Font sizes for the combo box.</summary>
    public List<string> FontSizes => new()
    {
        "8", "9", "10", "11", "12", "13", "14", "15", "16",
        "17", "18", "19", "20", "21", "22", "23", "24"
    };

    /// <summary>Available themes.</summary>
    public List<string> Themes => new() { "Light", "Dark", "Skadi" };

    /// <summary>Available languages (placeholder until translations exist).</summary>
    public List<string> Languages => new() { "English" };
}
