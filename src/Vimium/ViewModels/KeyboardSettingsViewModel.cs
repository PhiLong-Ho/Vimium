using Vimium.Services;

namespace Vimium.ViewModels;

public class KeyboardSettingsViewModel : NotifyPropertyChanged
{
    private readonly ConfigService _config = ConfigService.Instance;

    public string Icon => "⌨";  // ⌨ keyboard
    public string DisplayName => "Keyboard";

    public string OverlayModifier
    {
        get => _config.OverlayModifier;
        set => _config.OverlayModifier = value;
    }

    public string TaskbarModifier
    {
        get => _config.TaskbarModifier;
        set => _config.TaskbarModifier = value;
    }
}
