using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vimium.NativeMethods;

namespace Vimium.Services
{
    /// <summary>
    /// Installs a global low level keyboard hook so hint keys can be captured
    /// without the overlay needing keyboard focus. This lets popup menus and
    /// drop downs stay open while hints are shown.
    /// </summary>
    internal sealed class KeyboardHookService : IDisposable
    {
        public sealed class KeyDownEventArgs : EventArgs
        {
            public int VirtualKeyCode { get; set; }

            /// <summary>
            /// Set to true to swallow the key so it does not reach the focused application.
            /// </summary>
            public bool Handled { get; set; }
        }

        public event EventHandler<KeyDownEventArgs> KeyDown;

        private IntPtr _hookHandle = IntPtr.Zero;

        // Keep a reference so the delegate is not garbage collected while the hook is installed
        private User32.LowLevelKeyboardProc _proc;

        public void Install()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                return;
            }

            _proc = HookCallback;
            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                _hookHandle = User32.SetWindowsHookEx(
                    User32.WH_KEYBOARD_LL,
                    _proc,
                    Kernel32.GetModuleHandle(module.ModuleName),
                    0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0)
                {
                    var msg = wParam.ToInt32();
                    if (msg == User32.WM_KEYDOWN || msg == User32.WM_SYSKEYDOWN)
                    {
                        var data = Marshal.PtrToStructure<User32.KBDLLHOOKSTRUCT>(lParam);
                        var args = new KeyDownEventArgs { VirtualKeyCode = (int)data.vkCode };
                        KeyDown?.Invoke(this, args);
                        if (args.Handled)
                        {
                            // Swallow the key so it does not reach the focused application
                            return new IntPtr(1);
                        }
                    }
                }
            }
            catch
            {
                // Never let an exception escape the hook callback
            }

            return User32.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                User32.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }

            _proc = null;
        }
    }
}
