using System.Collections.Generic;
using System.Windows.Media;
using Vimium.Services;

namespace Vimium.ViewModels;

public class OverlaySettingsViewModel : NotifyPropertyChanged
{
    private readonly ConfigService _config = ConfigService.Instance;

    public string Icon => "🖥";  // 🖥 desktop
    public string DisplayName => "Overlay";

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

    /// <summary>Preset colors for quick selection.</summary>
    public List<string> PresetColors => new()
    {
        "#FFFF00", // Yellow
        "#FFFFE0", // LightYellow
        "#FFD700", // Gold
        "#FFA500", // Orange
        "#FF6347", // Tomato
        "#90EE90", // LightGreen
        "#00FF00", // Lime
        "#00FFFF", // Cyan
        "#87CEEB", // SkyBlue
        "#FF69B4", // HotPink
        "#FFFFFF", // White
        "#000000", // Black
    };

    /// <summary>Safely converts a hex string to a SolidColorBrush, falls back to Transparent.</summary>
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
}
