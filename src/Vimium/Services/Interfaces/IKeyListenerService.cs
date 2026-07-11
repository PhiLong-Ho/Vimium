using System;
using System.Windows.Forms;
using Vimium.NativeMethods;

namespace Vimium.Services.Interfaces
{
    internal class HotKey
    {
        public KeyModifier Modifier { get; set; }
        public Keys Keys { get; set; }

        /// <summary>
        /// Id of the hot key registration
        /// </summary>
        public int RegistrationId { get; set; }

        /// <summary>
        /// Parse a shortcut string like "Ctrl+;" or "Ctrl+'" into a HotKey.
        /// Supported modifiers: Ctrl, Alt, Shift, Win (comma-separated).
        /// </summary>
        public static HotKey Parse(string shortcut)
        {
            var result = new HotKey { Modifier = KeyModifier.Control };
            var parts = shortcut.Split('+');
            if (parts.Length < 2) return result;

            // Parse modifiers (all parts except the last)
            KeyModifier mod = 0;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                mod |= parts[i].Trim().ToLowerInvariant() switch
                {
                    "ctrl" => KeyModifier.Control,
                    "alt" => KeyModifier.Alt,
                    "shift" => KeyModifier.Shift,
                    "win" => KeyModifier.Windows,
                    _ => 0
                };
            }

            // Parse the key character (last part)
            var keyChar = parts[^1].Trim();
            result.Modifier = mod != 0 ? mod : KeyModifier.Control;
            result.Keys = keyChar switch
            {
                ";" => Keys.OemSemicolon,
                "'" => Keys.Oem7,
                "," => Keys.Oemcomma,
                "." => Keys.OemPeriod,
                "/" => Keys.OemQuestion,
                "\\" => Keys.OemBackslash,
                "[" => Keys.OemOpenBrackets,
                "]" => Keys.OemCloseBrackets,
                "-" => Keys.OemMinus,
                "=" => Keys.Oemplus,
                "`" => Keys.Oemtilde,
                _ when keyChar.Length == 1 && char.IsDigit(keyChar[0]) =>
                    System.Enum.Parse<Keys>("D" + keyChar[0]),
                _ when keyChar.Length == 1 && char.IsLetter(keyChar[0]) =>
                    System.Enum.Parse<Keys>(char.ToUpperInvariant(keyChar[0]).ToString()),
                _ when keyChar.Length == 1 => Keys.OemSemicolon, // fallback
                _ => Enum.TryParse<Keys>(keyChar, true, out var k) ? k : Keys.OemSemicolon
            };

            return result;
        }
    }

    /// <summary>
    /// Service for listening to global keyboard shortcuts
    /// </summary>
    internal interface IKeyListenerService
    {
        event EventHandler OnHotKeyActivated;
        event EventHandler OnTaskbarHotKeyActivated;
        event EventHandler OnDebugHotKeyActivated;
        event EventHandler OnLineNavigationHotKeyActivated;

        HotKey TaskbarHotKey { get; set; }
        HotKey HotKey { get; set; }
        HotKey DebugHotKey { get; set; }
        HotKey LineNavigationHotKey { get; set; }
    }
}
