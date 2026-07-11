using Vimium.NativeMethods;
using System;
using System.ComponentModel;
using System.Windows.Forms;
using Vimium.Services.Interfaces;

namespace Vimium.Services
{
    internal class KeyListenerService : Form, IKeyListenerService, IDisposable
    {
        public event EventHandler OnHotKeyActivated;
        public event EventHandler OnTaskbarHotKeyActivated;
        public event EventHandler OnDebugHotKeyActivated;
        public event EventHandler OnLineNavigationHotKeyActivated;

        /// <summary>
        /// Global counter for assigning ids to identiy the hot key registration
        /// </summary>
        private int _hotkeyIdCounter;

        private HotKey _hotKey;
        private HotKey _taskbarHotKey;
        private HotKey _debugHotKey;
        private HotKey _lineNavigationHotKey;

        /// <summary>
        /// Re-registers the current hotkey, unregistering any previous key
        /// </summary>
        private void ReRegisterHotKey(HotKey hotKey)
        {
            // Already registered, have to unregister first
            if (hotKey.RegistrationId > 0)
            {
                User32.UnregisterHotKey(Handle, hotKey.RegistrationId);
            }

            hotKey.RegistrationId = _hotkeyIdCounter++;
            User32.RegisterHotKey(Handle, hotKey.RegistrationId, (uint)hotKey.Modifier, (uint)hotKey.Keys);
        }

        /// <summary>
        /// Gets/sets the current hotkey
        /// </summary>
        /// <remarks>Changing this will cause the current hotkey to be unregistered</remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public HotKey HotKey
        {
            get
            {
                return _hotKey;
            }
            set
            {
                _hotKey = value;
                ReRegisterHotKey(_hotKey);
            }
        }

        /// <summary>
        /// Gets/sets the current task bar hotkey
        /// </summary>
        /// <remarks>Changing this will cause the current hotkey to be unregistered</remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public HotKey TaskbarHotKey
        {
            get
            {
                return _taskbarHotKey;
            }
            set
            {
                _taskbarHotKey = value;
                ReRegisterHotKey(_taskbarHotKey);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public HotKey DebugHotKey
        {
            get
            {
                return _debugHotKey;
            }
            set
            {
                _debugHotKey = value;
                ReRegisterHotKey(_debugHotKey);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public HotKey LineNavigationHotKey
        {
            get
            {
                return _lineNavigationHotKey;
            }
            set
            {
                _lineNavigationHotKey = value;
                ReRegisterHotKey(_lineNavigationHotKey);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Constants.WM_HOTKEY)
            {
                var e = new HotKeyEventArgs(m.LParam);

                // Normal hotkey
                if (_hotKey != null &&
                    e.Key == _hotKey.Keys &&
                    e.Modifiers == _hotKey.Modifier &&
                    OnHotKeyActivated != null)
                {
                    OnHotKeyActivated(this, new EventArgs());
                    return;
                }

                // Task bar hotkey
                if (_taskbarHotKey != null &&
                    e.Key == _taskbarHotKey.Keys &&
                    e.Modifiers == _taskbarHotKey.Modifier &&
                    OnHotKeyActivated != null)
                {
                    OnTaskbarHotKeyActivated(this, new EventArgs());
                    return;
                }

                // Debug hotkey
                if (_debugHotKey != null &&
                    e.Key == _debugHotKey.Keys &&
                    e.Modifiers == _debugHotKey.Modifier &&
                    OnDebugHotKeyActivated != null)
                {
                    OnDebugHotKeyActivated(this, new EventArgs());
                    return;
                }

                // Line navigation hotkey
                if (_lineNavigationHotKey != null &&
                    e.Key == _lineNavigationHotKey.Keys &&
                    e.Modifiers == _lineNavigationHotKey.Modifier &&
                    OnLineNavigationHotKeyActivated != null)
                {
                    Services.LogService.Info($"WM_HOTKEY: LineNav matched! Key={e.Key}, Mod={e.Modifiers}");
                    OnLineNavigationHotKeyActivated(this, new EventArgs());
                    return;
                }

                // Unrecognized hotkey
                Services.LogService.Warn($"WM_HOTKEY: No match — Key={e.Key}, Mod={e.Modifiers}. Registered: LineNav=(Key={_lineNavigationHotKey?.Keys}, Mod={_lineNavigationHotKey?.Modifier}), Overlay=(Key={_hotKey?.Keys}, Mod={_hotKey?.Modifier})");
            }

            base.WndProc(ref m);
        }

        protected override void SetVisibleCore(bool value)
        {
            // Ensures that the window will never be displayed
            base.SetVisibleCore(false);
        }
    }
}
