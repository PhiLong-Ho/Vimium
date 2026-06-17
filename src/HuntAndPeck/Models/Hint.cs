using System;
using System.Windows;
using HuntAndPeck.NativeMethods;

namespace HuntAndPeck.Models
{
    /// <summary>
    /// Represents a hint that has 1 or more capabilities
    /// </summary>
    public abstract class Hint
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="owningWindow">The owning window</param>
        /// <param name="boundingRectangle">The bounding rectangle of the hint in owner window coordinates</param>
        protected Hint(IntPtr owningWindow, Rect boundingRectangle)
        {
            OwningWindow = owningWindow;
            BoundingRectangle = boundingRectangle;
        }

        /// <summary>
        /// The bounding rectangle for the hint in Window coordinates for the owning window
        /// </summary>
        public Rect BoundingRectangle { get; private set; }

        /// <summary>
        /// The window handle of the owning window
        /// </summary>
        public IntPtr OwningWindow { get; private set; }

        /// <summary>
        /// Moves the mouse pointer to the center of this hint.
        /// </summary>
        public void MovePointerToCenter()
        {
            var rect = BoundingRectangle;
            var centerX = rect.Left + (rect.Width / 2.0);
            var centerY = rect.Top + (rect.Height / 2.0);

            var absoluteX = (int)Math.Round(centerX);
            var absoluteY = (int)Math.Round(centerY);

            // Hint bounds are in owning-window coordinates, so offset to screen coordinates.
            if (OwningWindow != IntPtr.Zero)
            {
                var windowRect = new RECT();
                User32.GetWindowRect(OwningWindow, ref windowRect);
                absoluteX += windowRect.left;
                absoluteY += windowRect.top;
            }

            User32.SetCursorPos(absoluteX, absoluteY);
        }

        /// <summary>
        /// Moves the mouse pointer to the center of this hint and performs a real left mouse click.
        /// Works for any control (burger menus, drop downs, custom controls) regardless of UI Automation pattern.
        /// </summary>
        public void Click()
        {
            MovePointerToCenter();
            User32.mouse_event(User32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            User32.mouse_event(User32.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        /// <summary>
        /// Moves the mouse pointer to the center of this hint and performs a real right mouse click
        /// (e.g. to open a context menu).
        /// </summary>
        public void RightClick()
        {
            MovePointerToCenter();
            User32.mouse_event(User32.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            User32.mouse_event(User32.MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        }

        /// <summary>
        /// Invokes the hint
        /// </summary>
        public abstract void Invoke();
    }
}
