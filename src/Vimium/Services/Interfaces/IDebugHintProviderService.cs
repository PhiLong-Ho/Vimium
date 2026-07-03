using System;
using Vimium.Models;

namespace Vimium.Services.Interfaces
{
    public interface IDebugHintProviderService
    {
        HintSession EnumDebugHints();
        HintSession EnumDebugHints(IntPtr hWnd);
    }
}
