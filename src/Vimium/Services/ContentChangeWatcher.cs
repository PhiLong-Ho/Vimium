using System;
using Interop.UIAutomationClient;
using Vimium.NativeMethods;

namespace Vimium.Services;

/// <summary>
/// Registers a UIA TextChanged event handler on the source window and raises a
/// managed event when content changes. Used by the find overlay to auto-dismiss
/// when the underlying document mutates (e.g., page refresh).
///
/// Registration and unregistration run on a dedicated MTA thread because cross-process
/// UIA event subscriptions must not run on the WPF STA UI thread. All failures are
/// swallowed — apps without TextPattern simply never fire the event.
/// </summary>
internal sealed class ContentChangeWatcher : IDisposable
{
    private const int TextChangedEventId = 20015; // UIA_Text_TextChangedEventId

    private sealed class Handler : IUIAutomationEventHandler
    {
        private readonly Action _onChanged;
        public Handler(Action onChanged) { _onChanged = onChanged; }
        public void HandleAutomationEvent(IUIAutomationElement sender, int eventId) => _onChanged();
    }

    private CUIAutomation _automation;
    private Handler _handler;
    private System.Threading.Thread _thread;
    private bool _disposed;

    /// <summary>Raised (on a UIA thread) when the source window's text content changes.</summary>
    public event Action ContentChanged;

    public void Start(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return;

        _thread = new System.Threading.Thread(() =>
        {
            try
            {
                _automation = new CUIAutomation();
                var element = _automation.ElementFromHandle(hWnd);
                if (element == null) return;

                _handler = new Handler(() => ContentChanged?.Invoke());
                _automation.AddAutomationEventHandler(
                    TextChangedEventId, element, TreeScope.TreeScope_Subtree, null, _handler);
                LogService.Info("FindText: content-change watcher registered");
            }
            catch (Exception ex)
            {
                LogService.Warn($"FindText: content-change watcher registration failed: {ex.Message}");
            }
        });
        _thread.SetApartmentState(System.Threading.ApartmentState.MTA);
        _thread.IsBackground = true;
        _thread.Start();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _automation?.RemoveAllEventHandlers();
        }
        catch { }
        _automation = null;
        _handler = null;
    }
}
