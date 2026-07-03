using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Vimium.Models;
using Vimium.NativeMethods;
using Vimium.Services;
using Vimium.Services.Interfaces;

namespace Vimium.ViewModels
{
    internal class OverlayViewModel : NotifyPropertyChanged
    {
        private readonly ConfigService _config = ConfigService.Instance;
        private Rect _bounds;
        private ObservableCollection<HintViewModel> _hints = new ObservableCollection<HintViewModel>();
        private bool _isLoading;

        /// <summary>
        /// Creates an overlay in the loading state — the overlay appears immediately
        /// with a "Generating hints…" indicator while enumeration runs on a background
        /// thread. Call <see cref="PopulateHints"/> when the session is ready.
        /// </summary>
        /// <param name="bounds">The owning window bounds (cheap to get)</param>
        public OverlayViewModel(Rect bounds)
        {
            _bounds = bounds;
            _isLoading = true;
            _config.PropertyChanged += OnConfigChanged;
        }

        public OverlayViewModel(
            HintSession session,
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
        public void PopulateHints(HintSession session, IHintLabelService hintLabelService)
        {
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
        /// Bounds in logical screen coordiantes
        /// </summary>
        public Rect Bounds
        {
            get
            {
                return _bounds;
            }
            set
            {
                _bounds = value;
                NotifyOfPropertyChange();
            }
        }

        public ObservableCollection<HintViewModel> Hints
        {
            get
            {
                return _hints;
            }
            set
            {
                _hints = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// True while hints are being enumerated on a background thread.
        /// The overlay shows a loading indicator instead of hint labels.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; NotifyOfPropertyChange(); }
        }

        public Action CloseOverlay { get; set; }

        /// <summary>
        /// Invoked when the overlay window has actually closed.
        /// </summary>
        public Action Closed { get; set; }

        /// <summary>Guard against reentrant calls — Invoke() can trigger a modal
        /// dialog (e.g. Options) which runs a nested message loop.</summary>
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

                if (matching.Count() == 1)
                {
                    var selectedHint = matching.First().Hint;

                    try
                    {
                        if ((User32.GetAsyncKeyState(User32.VK_RSHIFT) & 0x8000) != 0)
                        {
                            // Hold Right Shift: real right click (e.g. open a context menu).
                            CloseOverlay?.Invoke();
                            var h = selectedHint;
                            System.Threading.Tasks.Task.Run(() => h.RightClick());
                        }
                        else if ((User32.GetAsyncKeyState(User32.VK_LSHIFT) & 0x8000) != 0)
                        {
                            // Hold Left Shift: real left click.
                            CloseOverlay?.Invoke();
                            var h = selectedHint;
                            System.Threading.Tasks.Task.Run(() => h.Click());
                        }
                        else
                        {
                            // Default: UI Automation invoke.
                            // Run on background thread so it can't deadlock if invoke
                            // triggers our own UI (e.g. Options ShowDialog).
                            CloseOverlay?.Invoke();
                            var h = selectedHint;
                            System.Threading.Tasks.Task.Run(() => h.Invoke());
                        }
                    }
                    catch (Exception)
                    {
                        // The target element may have gone away or the action may not be
                        // valid in its current state (e.g. a stale UI Automation element
                        // throws COMException). Never let that crash the app -- just close.
                        CloseOverlay?.Invoke();
                    }
                }
                }
                finally
                {
                    _resolving = false;
                }
            }
        }
    }
}
