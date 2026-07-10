using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Vimium.Models;
using Vimium.NativeMethods;
using Vimium.Services;
using Vimium.ViewModels;

namespace Vimium.Views;

/// <summary>
/// Overlay for find-and-navigate mode. Renders per-match highlights from UIA
/// bounding rectangles (yellow=all, orange=active) and a bottom search bar.
/// Keyboard input is captured via a global low-level hook so the overlay never
/// steals focus from the underlying window.
/// </summary>
public partial class SelectionModeOverlayView
{
    private readonly KeyboardHookService _keyboardHook = new KeyboardHookService();
    private readonly ContentChangeWatcher _contentWatcher = new ContentChangeWatcher();
    private bool _isClosed;
    private bool _contentChanged;
    private bool _watcherStarted;
    private IntPtr _sourceWindow;
    private double _scaleX = 1.0;
    private double _scaleY = 1.0;

    public SelectionModeOverlayView()
    {
        InitializeComponent();
    }

    protected override bool StealFocus => false;

    private SelectionModeViewModel Vm => DataContext as SelectionModeViewModel;

    private void SelectionModeOverlayView_OnLoaded(object sender, RoutedEventArgs e)
    {
        var m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
        _scaleX = m.M11;
        _scaleY = m.M22;

        layoutCanvas.LayoutTransform = new ScaleTransform(1 / _scaleX, 1 / _scaleY);

        var vm = Vm;
        if (vm != null)
        {
            _sourceWindow = vm.SourceWindow;

            Left = vm.WindowBounds.Left / _scaleX;
            Top = vm.WindowBounds.Top / _scaleY;
            Width = vm.WindowBounds.Width / _scaleX;
            Height = vm.WindowBounds.Height / _scaleY;

            vm.PropertyChanged += (s, args) => Dispatcher.Invoke(RenderOverlay);
        }

        _contentWatcher.ContentChanged += () => _contentChanged = true;

        var cursorBlink = new DoubleAnimation
        {
            From = 1.0, To = 0.0, Duration = TimeSpan.FromSeconds(0.5),
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever,
        };
        SearchCursor.BeginAnimation(UIElement.OpacityProperty, cursorBlink);

        _keyboardHook.KeyDown += KeyboardHook_KeyDown;
        _keyboardHook.Install();
        RenderOverlay();
    }

    private void SelectionModeOverlayView_OnClosed(object sender, EventArgs e)
    {
        _isClosed = true;
        _keyboardHook.KeyDown -= KeyboardHook_KeyDown;
        _keyboardHook.Dispose();
        _contentWatcher.Dispose();
        Vm?.Dispose();
    }

    // ── Keyboard dispatch ─────────────────────────────────────

    private void KeyboardHook_KeyDown(object sender, KeyboardHookService.KeyDownEventArgs e)
    {
        if (_isClosed) return;

        var vm = Vm;
        if (vm == null) return;

        // Deferred content-change dismiss: if the underlying document mutated since the
        // last interaction, dismiss now rather than acting on stale matches.
        if (_contentChanged)
        {
            e.Handled = false;
            Dispatcher.BeginInvoke(new Action(() => vm.HandleFocusLost()));
            return;
        }

        // Auto-dismiss when the foreground window changes (Alt+Tab, click-through, etc.)
        var currentForeground = User32.GetForegroundWindow();
        if (_sourceWindow != IntPtr.Zero && currentForeground != _sourceWindow)
        {
            e.Handled = false; // let the key reach the newly-focused window
            Dispatcher.BeginInvoke(new Action(() => vm.HandleFocusLost()));
            return;
        }

        var vk = e.VirtualKeyCode;

        bool shiftHeld = (User32.GetAsyncKeyState(User32.VK_LSHIFT) & 0x8000) != 0
                         || (User32.GetAsyncKeyState(User32.VK_RSHIFT) & 0x8000) != 0;
        bool ctrlHeld = (User32.GetAsyncKeyState(User32.VK_LCONTROL) & 0x8000) != 0
                        || (User32.GetAsyncKeyState(User32.VK_RCONTROL) & 0x8000) != 0;
        bool altHeld = (User32.GetAsyncKeyState(User32.VK_LMENU) & 0x8000) != 0
                       || (User32.GetAsyncKeyState(User32.VK_RMENU) & 0x8000) != 0;

        // Control keys handled by the overlay
        switch (vk)
        {
            case User32.VK_ESCAPE:
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() => vm.HandleEscape()));
                return;
            case User32.VK_RETURN:
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() => vm.HandleEnter()));
                return;
            case User32.VK_BACK:
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() => vm.HandleBackspace()));
                return;
            case User32.VK_TAB:
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() => vm.HandleTab(shiftHeld)));
                return;
        }

        // Deliberately NOT captured: arrows, Home/End, Ctrl/Alt combos — pass through.
        if (ctrlHeld || altHeld) return;
        if (vk == User32.VK_LEFT || vk == User32.VK_RIGHT || vk == User32.VK_UP || vk == User32.VK_DOWN
            || vk == User32.VK_HOME || vk == User32.VK_END)
            return;

        // Printable characters → append to query
        char? typed = KeyCharMapper.MapPrintable(vk, shiftHeld);
        if (typed.HasValue)
        {
            e.Handled = true;
            char c = typed.Value;
            Dispatcher.BeginInvoke(new Action(() => vm.HandleCharacter(c)));
        }
    }

    // ── Rendering ─────────────────────────────────────────────

    private void RenderOverlay()
    {
        layoutCanvas.Children.Clear();

        var vm = Vm;
        if (vm == null) return;

        SearchQueryText.Text = vm.SearchQuery ?? "";
        MatchCountLabel.Text = vm.MatchCountText ?? "";
        LoadingSpinner.Visibility = vm.IsSearching ? Visibility.Visible : Visibility.Collapsed;

        // Show a tip when the search timed out (e.g. on massive pages like Wikipedia).
        if (vm.SearchTimedOut)
        {
            StatusTipLabel.Text = "Search timed out. Try the app's built-in Ctrl+F for better results.";
            StatusTipLabel.Visibility = Visibility.Visible;
        }
        else
        {
            StatusTipLabel.Visibility = Visibility.Collapsed;
        }

        if (vm.Matches == null) return;

        // Register the content-change watcher once, after the first successful search.
        if (!_watcherStarted && vm.Matches.Count > 0)
        {
            _watcherStarted = true;
            _contentWatcher.Start(_sourceWindow);
        }

        var normalBrush = TryFindBrush("FindMatchHighlightBrush", Color.FromArgb(70, 255, 235, 59));
        var activeBrush = TryFindBrush("FindMatchActiveHighlightBrush", Color.FromArgb(120, 255, 140, 0));
        var borderBrush = TryFindBrush("FindMatchBorderBrush", Color.FromArgb(150, 255, 179, 0));

        foreach (var match in vm.Matches)
            RenderMatchHighlight(match, match.IsActive ? activeBrush : normalBrush, borderBrush);
    }

    private void RenderMatchHighlight(SearchMatch match, Brush fill, Brush border)
    {
        var r = match.BoundingRect;
        if (r.Width <= 0 || r.Height <= 0) return;

        var highlight = new Rectangle
        {
            Fill = fill,
            Stroke = border,
            StrokeThickness = match.IsActive ? 1.5 : 0.5,
            RadiusX = 2, RadiusY = 2,
            Width = r.Width,
            Height = r.Height,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(highlight, r.Left);
        Canvas.SetTop(highlight, r.Top);
        layoutCanvas.Children.Add(highlight);
    }

    private Brush TryFindBrush(string key, Color fallback)
    {
        var res = TryFindResource(key) as Brush;
        return res ?? new SolidColorBrush(fallback);
    }
}
