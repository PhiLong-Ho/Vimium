using System;

namespace Vimium.NativeMethods
{
    // https://stackoverflow.com/questions/61144651
    [Flags]
    public enum KeyModifier
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
        NoRepeat = 0x4000
    }
}
