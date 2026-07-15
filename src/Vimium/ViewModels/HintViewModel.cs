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
        private bool _showLeaderLine;
        private double _lineX1, _lineY1, _lineX2, _lineY2;

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

        /// <summary>
        /// Whether to draw a leader line from the label back to the element.
        /// Only true when the label was moved from its default position.
        /// </summary>
        public bool ShowLeaderLine
        {
            get { return _showLeaderLine; }
            set { _showLeaderLine = value; NotifyOfPropertyChange(); }
        }

        /// <summary>Leader line start X (label edge) in canvas coordinates.</summary>
        public double LineX1
        {
            get { return _lineX1; }
            set { _lineX1 = value; NotifyOfPropertyChange(); }
        }

        /// <summary>Leader line start Y (label edge) in canvas coordinates.</summary>
        public double LineY1
        {
            get { return _lineY1; }
            set { _lineY1 = value; NotifyOfPropertyChange(); }
        }

        /// <summary>Leader line end X (element center) in canvas coordinates.</summary>
        public double LineX2
        {
            get { return _lineX2; }
            set { _lineX2 = value; NotifyOfPropertyChange(); }
        }

        /// <summary>Leader line end Y (element center) in canvas coordinates.</summary>
        public double LineY2
        {
            get { return _lineY2; }
            set { _lineY2 = value; NotifyOfPropertyChange(); }
        }
    }
}
