using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Vimium.Services;

namespace Vimium.ViewModels;

internal class OptionsViewModel : NotifyPropertyChanged
{
    private readonly ConfigService _config = ConfigService.Instance;

    public OptionsViewModel()
    {
        DisplayName = "Options";

        Pages = new ObservableCollection<NotifyPropertyChanged>
        {
            new GeneralSettingsViewModel(),
            new OverlaySettingsViewModel(),
            new KeyboardSettingsViewModel(),
        };

        SelectedPage = Pages.First();

        SaveCommand = new DelegateCommand(Save);
        CancelCommand = new DelegateCommand(Cancel);
        ResetCommand = new DelegateCommand(Reset);
    }

    public string DisplayName { get; set; }

    // ── Sidebar ────────────────────────────────────────────

    public ObservableCollection<NotifyPropertyChanged> Pages { get; }

    private NotifyPropertyChanged _selectedPage;
    public NotifyPropertyChanged SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (_selectedPage != value)
            {
                _selectedPage = value;
                NotifyOfPropertyChange(nameof(SelectedPage));
            }
        }
    }

    public void SelectNextPage()
    {
        var idx = Pages.IndexOf(SelectedPage);
        if (idx < Pages.Count - 1)
            SelectedPage = Pages[idx + 1];
    }

    public void SelectPreviousPage()
    {
        var idx = Pages.IndexOf(SelectedPage);
        if (idx > 0)
            SelectedPage = Pages[idx - 1];
    }

    // ── Commands ────────────────────────────────────────────

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }

    private void Save()
    {
        _config.Save();
        CloseWindow();
    }

    private void Cancel()
    {
        if (_config.IsDirty)
        {
            var result = MessageBox.Show(
                "Discard unsaved changes?",
                "Vimium",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        _config.Cancel();
        CloseWindow();
    }

    private void Reset()
    {
        var result = MessageBox.Show(
            "Reset all settings to their default values?",
            "Vimium",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _config.ResetToDefaults();
        }
    }

    private void CloseWindow()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window.DataContext == this)
            {
                window.Close();
                break;
            }
        }
    }
}
