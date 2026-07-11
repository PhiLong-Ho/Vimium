using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Vimium.Services;

/// <summary>
/// Provides clipboard operations for the selection mode.
/// Wraps WPF Clipboard.SetText with a retry loop for clipboard contention.
/// </summary>
public class ClipboardService
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 50;

    /// <summary>
    /// Sets the system clipboard to the given text.
    /// Retries on failure (clipboard contention from other apps).
    /// </summary>
    /// <param name="text">Text to copy. Must not be null.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if clipboard access fails after all retries.
    /// </exception>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Exception lastException = null;
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return; // Success
            }
            catch (COMException ex)
            {
                lastException = ex;
                if (attempt < MaxRetries - 1)
                    Thread.Sleep(RetryDelayMs);
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < MaxRetries - 1)
                    Thread.Sleep(RetryDelayMs);
            }
        }

        // All retries exhausted
        throw new InvalidOperationException(
            "Failed to set clipboard text after multiple attempts. The clipboard may be locked by another application.",
            lastException);
    }
}
