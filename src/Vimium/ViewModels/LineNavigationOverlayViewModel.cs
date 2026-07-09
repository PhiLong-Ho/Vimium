using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Vimium.Models;
using Vimium.Services;
using Vimium.Services.Interfaces;

namespace Vimium.ViewModels;

internal class LineNavigationOverlayViewModel : NotifyPropertyChanged
{
    private readonly ConfigService _config = ConfigService.Instance;
    private Rect _bounds;
    private ObservableCollection<HintViewModel> _hints = new ObservableCollection<HintViewModel>();
    private bool _isLoading;

    /// <summary>
    /// Creates an overlay in the loading state — the overlay appears immediately
    /// with a "Generating hints…" indicator while line enumeration runs on a
    /// background thread. Call <see cref="PopulateHints"/> when the session is ready.
    /// </summary>
    /// <param name="bounds">The owning window bounds (cheap to get)</param>
    public LineNavigationOverlayViewModel(Rect bounds)
    {
        _bounds = bounds;
        _isLoading = true;
        _config.PropertyChanged += OnConfigChanged;
    }

    /// <summary>
    /// Creates an overlay in the ready state with hints already populated.
    /// </summary>
    public LineNavigationOverlayViewModel(
        LineNavigationSession session,
        IHintLabelService hintLabelService)
    {
        _bounds = session.OwningWindowBounds;
        PopulateHints(session, hintLabelService);
        _config.PropertyChanged += OnConfigChanged;
    }

    private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or "" or "FontSize" or "HintActiveBackground"
            or "HintInactiveBackground" or "HintTextColor" or "HintFontFamily")
        {
            NotifyOfPropertyChange(nameof(HintActiveBrush));
            NotifyOfPropertyChange(nameof(HintInactiveBrush));
            NotifyOfPropertyChange(nameof(HintTextBrush));
        }
    }

    // ── Dynamic hint colors (from ConfigService) ─────────

    public Brush HintActiveBrush => HexToBrush(_config.HintActiveBackground);
    public Brush HintInactiveBrush => HexToBrush(_config.HintInactiveBackground);
    public Brush HintTextBrush => HexToBrush(_config.HintTextColor);

    private static Brush HexToBrush(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return Brushes.Yellow;
        }
    }

    /// <summary>
    /// Fills in the hint labels once the session has been enumerated.
    /// Call on the UI thread.
    /// </summary>
    public void PopulateHints(LineNavigationSession session, IHintLabelService hintLabelService)
    {
        if (session.Hints == null || session.Hints.Count == 0)
        {
            IsLoading = false;
            IsEmpty = true;
            // Auto-dismiss after 1.5s when no text found
            _ = System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ =>
            {
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(() => CloseOverlay?.Invoke());
            });
            return;
        }

        var labels = hintLabelService.GetHintStrings(session.Hints.Count());
        for (int i = 0; i < labels.Count; ++i)
        {
            var hint = session.Hints[i];
            _hints.Add(new HintViewModel(hint)
            {
                Label = labels[i],
                Active = false
            });
        }
        IsLoading = false;
    }

    /// <summary>
    /// Bounds in logical screen coordinates
    /// </summary>
    public Rect Bounds
    {
        get => _bounds;
        set { _bounds = value; NotifyOfPropertyChange(); }
    }

    public ObservableCollection<HintViewModel> Hints
    {
        get => _hints;
        set { _hints = value; NotifyOfPropertyChange(); }
    }

    /// <summary>
    /// True while hints are being enumerated on a background thread.
    /// The overlay shows a loading indicator instead of hint labels.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; NotifyOfPropertyChange(); }
    }

    public Action CloseOverlay { get; set; }

    /// <summary>
    /// Invoked when a unique hint match is found.
    /// Parameters: (hint, copyModifierHeld)
    /// </summary>
    public Action<TextLineHint, bool> OnHintResolved { get; set; }

    /// <summary>
    /// True when no text lines were found — the overlay should show
    /// a message and auto-dismiss.
    /// </summary>
    public bool IsEmpty { get; private set; }

    /// <summary>
    /// Message shown when no text lines could be found.
    /// </summary>
    public string EmptyMessage => "No text lines found";

    /// <summary>Guard against reentrant calls.</summary>
    private bool _resolving;

    public string MatchString
    {
        set
        {
            if (_resolving) return;
            _resolving = true;

            try
            {
                foreach (var x in Hints)
                {
                    x.Active = false;
                }

                var matching = Hints.Where(x => x.Label.StartsWith(value, StringComparison.OrdinalIgnoreCase)).ToArray();
                foreach (var x in matching)
                {
                    x.Active = true;
                }

                if (matching.Length == 1)
                {
                    var selectedHint = matching.First().Hint as TextLineHint;
                    if (selectedHint != null)
                    {
                        CloseOverlay?.Invoke();

                        bool copyModifierHeld = IsCopyModifierHeld();
                        OnHintResolved?.Invoke(selectedHint, copyModifierHeld);
                    }
                }
            }
            finally
            {
                _resolving = false;
            }
        }
    }

    /// <summary>
    /// Checks whether the copy modifier key is currently held down.
    /// </summary>
    private static bool IsCopyModifierHeld()
    {
        var copyModifier = ConfigService.Instance.CopyModifier;
        return copyModifier switch
        {
            "Ctrl" => (NativeMethods.User32.GetAsyncKeyState(NativeMethods.User32.VK_LCONTROL) & 0x8000) != 0
                      || (NativeMethods.User32.GetAsyncKeyState(NativeMethods.User32.VK_RCONTROL) & 0x8000) != 0,
            "Alt" => (NativeMethods.User32.GetAsyncKeyState(NativeMethods.User32.VK_LMENU) & 0x8000) != 0
                     || (NativeMethods.User32.GetAsyncKeyState(NativeMethods.User32.VK_RMENU) & 0x8000) != 0,
            "Shift" => (NativeMethods.User32.GetAsyncKeyState(NativeMethods.User32.VK_LSHIFT) & 0x8000) != 0
                       || (NativeMethods.User32.GetAsyncKeyState(NativeMethods.User32.VK_RSHIFT) & 0x8000) != 0,
            _ => false
        };
    }
}
