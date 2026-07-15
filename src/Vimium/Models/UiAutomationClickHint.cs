using System;
using System.Windows;

namespace Vimium.Models
{
    /// <summary>
    /// Hint that uses Win32 mouse click for Invoke().
    /// Used as a fallback for inherently clickable elements (menus, menu items)
    /// that don't expose any standard UIA pattern but are visually interactive.
    /// </summary>
    internal class UiAutomationClickHint : Hint
    {
        public UiAutomationClickHint(IntPtr owningWindow, Rect boundingRectangle)
            : base(owningWindow, boundingRectangle)
        {
        }

        public override void Invoke()
        {
            // Use Win32 mouse click as fallback — works for any visible element
            Click();
        }
    }
}
