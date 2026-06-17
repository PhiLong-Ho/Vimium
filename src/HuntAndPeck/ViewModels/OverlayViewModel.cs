using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HuntAndPeck.Models;
using HuntAndPeck.Services.Interfaces;

namespace HuntAndPeck.ViewModels
{
    internal class OverlayViewModel : NotifyPropertyChanged
    {
        private Rect _bounds;
        private ObservableCollection<HintViewModel> _hints = new ObservableCollection<HintViewModel>();

        public OverlayViewModel(
            HintSession session,
            IHintLabelService hintLabelService)
        {
            _bounds = session.OwningWindowBounds;

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

        public Action CloseOverlay { get; set; }

        public string MatchString
        {
            set
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

                    if (Keyboard.IsKeyDown(Key.RightShift))
                    {
                        // Hold Right Shift: real right click (e.g. open a context menu).
                        // Close the overlay first so the click lands on the target window.
                        CloseOverlay?.Invoke();
                        selectedHint.RightClick();
                    }
                    else if (Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        // Hold Left Shift: real left click. Works universally, including Electron/web
                        // apps like Microsoft Teams where the UI Automation InvokePattern does nothing.
                        // Close the overlay first so the click lands on the target window.
                        CloseOverlay?.Invoke();
                        selectedHint.Click();
                    }
                    else
                    {
                        // Default: UI Automation invoke.
                        selectedHint.Invoke();
                        CloseOverlay?.Invoke();
                    }
                }
            }
        }
    }
}
