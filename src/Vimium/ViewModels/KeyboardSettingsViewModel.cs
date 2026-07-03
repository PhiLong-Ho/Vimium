using System.ComponentModel;
using Vimium.Services;

namespace Vimium.ViewModels;

public class KeyboardSettingsViewModel : NotifyPropertyChanged
{
    private readonly ConfigService _config = ConfigService.Instance;

    public KeyboardSettingsViewModel()
    {
        _config.PropertyChanged += OnConfigChanged;
    }

    public string Icon => "⌨";
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

    private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
    {
        var name = e.PropertyName;
        if (name is null or "" or "OverlayModifier")
            NotifyOfPropertyChange(nameof(OverlayModifier));
        if (name is null or "" or "TaskbarModifier")
            NotifyOfPropertyChange(nameof(TaskbarModifier));
    }
}
