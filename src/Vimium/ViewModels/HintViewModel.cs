using Vimium.Models;
using Vimium.Services;

namespace Vimium.ViewModels
{
    public class HintViewModel : NotifyPropertyChanged
    {
        private string _label;
        private bool _active;
        private string _fontSizeReadValue;
        private double _adjustedLeft;
        private double _adjustedTop;

        public HintViewModel(Hint hint)
        {
            Hint = hint;
            FontSizeReadValue = ConfigService.Instance.FontSize;
            AdjustedLeft = hint.BoundingRectangle.Left;
            AdjustedTop = hint.BoundingRectangle.Top;
        }

        public Hint Hint { get; set; }

        public bool Active
        {
            get { return _active; }
            set { _active = value; NotifyOfPropertyChange(); }
        }

        public string Label
        {
            get { return _label; }
            set { _label = value; NotifyOfPropertyChange(); }
        }

        public string FontSizeReadValue
        {
            get { return _fontSizeReadValue; }
            set { _fontSizeReadValue = value; NotifyOfPropertyChange(); }
        }

        /// <summary>
        /// Left offset after spiral offsetting (T032).
        /// Defaults to Hint.BoundingRectangle.Left when no adjustment applied.
        /// </summary>
        public double AdjustedLeft
        {
            get { return _adjustedLeft; }
            set { _adjustedLeft = value; NotifyOfPropertyChange(); }
        }

        /// <summary>
        /// Top offset after spiral offsetting (T032).
        /// Defaults to Hint.BoundingRectangle.Top when no adjustment applied.
        /// </summary>
        public double AdjustedTop
        {
            get { return _adjustedTop; }
            set { _adjustedTop = value; NotifyOfPropertyChange(); }
        }
    }
}
