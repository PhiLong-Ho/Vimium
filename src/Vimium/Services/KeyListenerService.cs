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
        public event EventHandler OnLineNavigationHotKeyActivated;

        /// <summary>
        /// Global counter for assigning ids to identiy the hot key registration
        /// </summary>
        private int _hotkeyIdCounter;

        private HotKey _hotKey;
        private HotKey _taskbarHotKey;
        private HotKey _lineNavigationHotKey;

        /// <summary>
        /// Re-registers the current hotkey, unregistering any previous key.
        /// Accepts the old hotkey separately so the old OS-level registration
        /// can be released even when a brand-new HotKey object is supplied.
        /// </summary>
        private void ReRegisterHotKey(HotKey newHotKey, HotKey oldHotKey)
        {
            // Unregister the *previous* OS registration first,
            // otherwise the old shortcut stays blocked until restart.
            if (oldHotKey?.RegistrationId > 0)
            {
                User32.UnregisterHotKey(Handle, oldHotKey.RegistrationId);
            }

            newHotKey.RegistrationId = _hotkeyIdCounter++;
            User32.RegisterHotKey(Handle, newHotKey.RegistrationId,
                (uint)newHotKey.Modifier, (uint)newHotKey.Keys);
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
                ReRegisterHotKey(value, _hotKey);
                _hotKey = value;
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
                ReRegisterHotKey(value, _taskbarHotKey);
                _taskbarHotKey = value;
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
                ReRegisterHotKey(value, _lineNavigationHotKey);
                _lineNavigationHotKey = value;
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
