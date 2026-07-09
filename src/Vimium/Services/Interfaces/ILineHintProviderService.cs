using System;
using System.Threading.Tasks;
using Vimium.Models;

namespace Vimium.Services.Interfaces;

/// <summary>
/// Discovers visible text lines in a window via UI Automation TextPattern.
/// </summary>
public interface ILineHintProviderService
{
    /// <summary>
    /// Enumerate visible text line hints for the current foreground window.
    /// </summary>
    /// <returns>
    /// A LineNavigationSession containing all visible text line hints,
    /// or null if no foreground window exists.
    /// </returns>
    LineNavigationSession EnumLineHints();

    /// <summary>
    /// Enumerate visible text line hints for a specific window handle.
    /// </summary>
    LineNavigationSession EnumLineHints(IntPtr hWnd);

    /// <summary>
    /// Enumerate line hints on a background thread.
    /// </summary>
    Task<LineNavigationSession> EnumLineHintsAsync(IntPtr hWnd);
}
