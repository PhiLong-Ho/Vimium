namespace Vimium.Services;

/// <summary>
/// Maps virtual-key codes to printable characters for the find-text search bar,
/// honoring Shift for letters, digits (US layout symbols), and OEM punctuation.
/// Pure/static so it can be unit-tested without a UI or keyboard hook.
/// </summary>
public static class KeyCharMapper
{
    private const int VK_SPACE = 0x20;

    /// <summary>
    /// Returns the printable character for a virtual-key code, or null if the key
    /// does not produce a printable character.
    /// </summary>
    public static char? MapPrintable(int vk, bool shift)
    {
        if (vk >= 'A' && vk <= 'Z')
            return shift ? (char)vk : (char)(vk + 32);
        if (vk >= '0' && vk <= '9')
            return shift ? ShiftedDigit((char)vk) : (char)vk;
        if (vk == VK_SPACE)
            return ' ';

        switch ((System.Windows.Forms.Keys)vk)
        {
            case System.Windows.Forms.Keys.OemPeriod: return shift ? '>' : '.';
            case System.Windows.Forms.Keys.Oemcomma: return shift ? '<' : ',';
            case System.Windows.Forms.Keys.OemMinus: return shift ? '_' : '-';
            case System.Windows.Forms.Keys.Oemplus: return shift ? '+' : '=';
            case System.Windows.Forms.Keys.OemQuestion: return shift ? '?' : '/';
            case System.Windows.Forms.Keys.OemSemicolon: return shift ? ':' : ';';
            case System.Windows.Forms.Keys.Oem7: return shift ? '"' : '\'';
            case System.Windows.Forms.Keys.OemOpenBrackets: return shift ? '{' : '[';
            case System.Windows.Forms.Keys.OemCloseBrackets: return shift ? '}' : ']';
            case System.Windows.Forms.Keys.OemPipe: return shift ? '|' : '\\';
            case System.Windows.Forms.Keys.Oemtilde: return shift ? '~' : '`';
            default: return null;
        }
    }

    /// <summary>Maps a top-row digit key to its shifted symbol (US keyboard layout).</summary>
    public static char ShiftedDigit(char digit)
    {
        switch (digit)
        {
            case '1': return '!';
            case '2': return '@';
            case '3': return '#';
            case '4': return '$';
            case '5': return '%';
            case '6': return '^';
            case '7': return '&';
            case '8': return '*';
            case '9': return '(';
            case '0': return ')';
            default: return digit;
        }
    }
}
