using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using Vimium.Services;

namespace Vimium.ViewModels;

public class OverlaySettingsViewModel : NotifyPropertyChanged
{
    private readonly ConfigService _config = ConfigService.Instance;

    public OverlaySettingsViewModel()
    {
        _config.PropertyChanged += OnConfigChanged;
    }

    public string Icon => "\U0001F5A5";  // 🖥 desktop
    public string DisplayName => "_Overlay";

    public string HintFontFamily
    {
        get => _config.HintFontFamily;
        set => _config.HintFontFamily = value;
    }

    public string HintActiveBackground
    {
        get => _config.HintActiveBackground;
        set => _config.HintActiveBackground = value;
    }

    public string HintInactiveBackground
    {
        get => _config.HintInactiveBackground;
        set => _config.HintInactiveBackground = value;
    }

    public string HintTextColor
    {
        get => _config.HintTextColor;
        set => _config.HintTextColor = value;
    }

    public bool HintAnimationEnabled
    {
        get => _config.HintAnimationEnabled;
        set => _config.HintAnimationEnabled = value;
    }

    public List<string> PresetColors => new()
    {
        "#FFFF00", "#FFFFE0", "#FFD700", "#FFA500",
        "#FF6347", "#90EE90", "#00FF00", "#00FFFF",
        "#87CEEB", "#FF69B4", "#FFFFFF", "#000000",
    };

    public static SolidColorBrush HexToBrush(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return Brushes.Transparent;
        }
    }

    private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
    {
        var name = e.PropertyName;
        if (name is null or "" or "HintFontFamily")
            NotifyOfPropertyChange(nameof(HintFontFamily));
        if (name is null or "" or "HintActiveBackground")
            NotifyOfPropertyChange(nameof(HintActiveBackground));
        if (name is null or "" or "HintInactiveBackground")
            NotifyOfPropertyChange(nameof(HintInactiveBackground));
        if (name is null or "" or "HintTextColor")
            NotifyOfPropertyChange(nameof(HintTextColor));
        if (name is null or "" or "HintAnimationEnabled")
            NotifyOfPropertyChange(nameof(HintAnimationEnabled));
    }
}
