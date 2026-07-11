using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Vimium.NativeMethods;

namespace Vimium.Views
{
    /// <summary>
    /// Window that is always foreground, and closes when it's not
    /// </summary>
    public class ForegroundWindow : Window
    {
        private bool _closing;
        private bool _initialized;

        /// <summary>
        /// When false, the window never takes activation/focus and will not auto close
        /// when deactivated. Used so popup menus and drop downs stay open while hints show.
        /// </summary>
        protected virtual bool StealFocus => true;

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!_initialized)
            {
                if (StealFocus)
                {
                    // Always want this on top. SetForegroundWindow has a few conditions:
                    // https://msdn.microsoft.com/en-us/library/ms633539(VS.85).aspx
                    if (!User32.SetForegroundWindow(new WindowInteropHelper(this).Handle))
                    {
                        ForceForeground();
                    }
                }
                else
                {
                    // Stay topmost but never steal focus, so the underlying menu/popup keeps focus
                    var handle = new WindowInteropHelper(this).Handle;
                    var exStyle = User32.GetWindowLong(handle, User32.GWL_EXSTYLE);
                    _ = User32.SetWindowLong(handle, User32.GWL_EXSTYLE, exStyle | User32.WS_EX_NOACTIVATE | User32.WS_EX_TOOLWINDOW);
                    User32.SetWindowPos(
                        handle,
                        User32.HWND_TOPMOST,
                        0, 0, 0, 0,
                        User32.SWP_NOMOVE | User32.SWP_NOSIZE | User32.SWP_NOACTIVATE | User32.SWP_SHOWWINDOW);
                }
                _initialized = true;
            }
            base.OnRender(drawingContext);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            // We could have lost focus because we're already closing, make sure this doesn't call close twice
            if (StealFocus && _initialized && !_closing)
            {
                Close();
            }
            base.OnDeactivated(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _closing = true;
            base.OnClosing(e);
        }

        /// <summary>
        /// Forces the window to the foreground by attaching to the foreground window thread
        /// </summary>
        private void ForceForeground()
        {
            // This is required as there's a few restrictions on when this can be called
            // Per https://msdn.microsoft.com/en-us/library/windows/desktop/ms633539%28v=vs.85%29.aspx

            var targetThread = User32.GetWindowThreadProcessId(User32.GetForegroundWindow(), IntPtr.Zero);
            var appThread = Kernel32.GetCurrentThreadId();
            var attached = false;

            try
            {
                if (targetThread == appThread)
                {
                    // already attached
                    return;
                }

                attached = User32.AttachThreadInput(targetThread, appThread, true);

                if (!attached)
                {
                    // hmm
                    Close();
                    return;
                }

                var ourHandle = new WindowInteropHelper(this).Handle;

                // force us to the forground
                User32.BringWindowToTop(ourHandle);
                User32.SetFocus(ourHandle);
            }
            finally
            {
                if (attached)
                {
                    // unattach
                    User32.AttachThreadInput(targetThread, appThread, false);
                }
            }
        }
    }
}
