using System;
using System.Windows;
using Interop.UIAutomationClient;

namespace Vimium.Models
{
    /// <summary>
    /// Hint backed by the LegacyIAccessible pattern (MSAA compatibility).
    /// Many custom controls and menus (VS Code, Electron apps) expose
    /// interactivity through this pattern rather than modern UIA patterns.
    /// Invoke() calls DoDefaultAction().
    /// </summary>
    internal class UiAutomationLegacyIAccessibleHint : Hint
    {
        private readonly IUIAutomationLegacyIAccessiblePattern _pattern;

        public UiAutomationLegacyIAccessibleHint(
            IntPtr owningWindow,
            IUIAutomationLegacyIAccessiblePattern pattern,
            Rect boundingRectangle)
            : base(owningWindow, boundingRectangle)
        {
            _pattern = pattern;
        }

        public override void Invoke()
        {
            _pattern.DoDefaultAction();
        }
    }
}
