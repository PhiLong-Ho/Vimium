using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Vimium.Models;
using Vimium.NativeMethods;
using Vimium.Services;
using Vimium.ViewModels;

namespace Vimium.Views;

/// <summary>
/// Interaction logic for SelectionModeOverlayView.xaml
/// </summary>
public partial class SelectionModeOverlayView
{
    private readonly KeyboardHookService _keyboardHook = new KeyboardHookService();
    private System.Windows.Threading.DispatcherTimer _safetyTimer;
    private bool _isClosed;

    public SelectionModeOverlayView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The overlay never steals focus.
    /// </summary>
    protected override bool StealFocus => false;

    private void SelectionModeOverlayView_OnLoaded(object sender, RoutedEventArgs e)
    {
        var m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
        var scaleX = m.M11;
        var scaleY = m.M22;

        layoutCanvas.LayoutTransform = new ScaleTransform(1 / scaleX, 1 / scaleY);

        var vm = DataContext as SelectionModeViewModel;
        if (vm != null)
        {
            Left = vm.WindowBounds.Left / scaleX;
            Top = vm.WindowBounds.Top / scaleY;
            Width = vm.WindowBounds.Width / scaleX;
            Height = vm.WindowBounds.Height / scaleY;

            // Wire copy feedback
            vm.OnCopied = (text) =>
            {
                Dispatcher.Invoke(() =>
                {
                    CopiedToast.Visibility = Visibility.Visible;
                    var sb = (System.Windows.Media.Animation.Storyboard)CopiedToast.FindResource("FadeOutStoryboard");
                    sb.Completed += (s, args) => Dispatcher.Invoke(() => CopiedToast.Visibility = Visibility.Collapsed);
                    sb.Begin();
                });
            };
        }

        _keyboardHook.KeyDown += KeyboardHook_KeyDown;
        _keyboardHook.Install();

        // Safety net
        _safetyTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _safetyTimer.Tick += (s, args) => Close();
        _safetyTimer.Start();

        RenderOverlay();
    }

    private void SelectionModeOverlayView_OnClosed(object sender, EventArgs e)
    {
        _isClosed = true;
        _safetyTimer?.Stop();
        _safetyTimer = null;
        _keyboardHook.KeyDown -= KeyboardHook_KeyDown;
        _keyboardHook.Dispose();
    }

    private void KeyboardHook_KeyDown(object sender, KeyboardHookService.KeyDownEventArgs e)
    {
        if (_isClosed) return;
        var vk = e.VirtualKeyCode;
        var vm = DataContext as SelectionModeViewModel;
        if (vm == null) return;

        // Check if Ctrl is held
        bool ctrlHeld = (User32.GetAsyncKeyState(User32.VK_LCONTROL) & 0x8000) != 0
                        || (User32.GetAsyncKeyState(User32.VK_RCONTROL) & 0x8000) != 0;

        // Check if Shift is held
        bool shiftHeld = (User32.GetAsyncKeyState(User32.VK_LSHIFT) & 0x8000) != 0
                         || (User32.GetAsyncKeyState(User32.VK_RSHIFT) & 0x8000) != 0;

        // Handle all selection mode keys
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
                Dispatcher.BeginInvoke(new Action(() => { vm.HandleBackspace(); RenderOverlay(); }));
                return;

            case User32.VK_TAB:
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() => { vm.HandleTab(shiftHeld); RenderOverlay(); }));
                return;

            case User32.VK_HOME:
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() => { vm.HandleHome(); RenderOverlay(); }));
                return;

            case User32.VK_END:
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() => { vm.HandleEnd(); RenderOverlay(); }));
                return;

            case User32.VK_LEFT:
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ctrlHeld && shiftHeld) vm.HandleCtrlShiftArrow(Key.Left);
                    else if (ctrlHeld) vm.HandleCtrlArrow(Key.Left);
                    else if (shiftHeld) vm.HandleShiftArrow(Key.Left);
                    else vm.HandleArrow(Key.Left);
                    RenderOverlay();
                }));
                return;

            case User32.VK_RIGHT:
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ctrlHeld && shiftHeld) vm.HandleCtrlShiftArrow(Key.Right);
                    else if (ctrlHeld) vm.HandleCtrlArrow(Key.Right);
                    else if (shiftHeld) vm.HandleShiftArrow(Key.Right);
                    else vm.HandleArrow(Key.Right);
                    RenderOverlay();
                }));
                return;
        }

        // Character input for search (letters A-Z)
        if (vk >= 'A' && vk <= 'Z')
        {
            e.Handled = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                vm.HandleCharacter((char)vk);
                RenderOverlay();
            }));
            return;
        }

        // Digits for search
        if (vk >= '0' && vk <= '9')
        {
            e.Handled = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                vm.HandleCharacter((char)vk);
                RenderOverlay();
            }));
            return;
        }
    }

    private void RenderOverlay()
    {
        layoutCanvas.Children.Clear();
        var vm = DataContext as SelectionModeViewModel;
        if (vm == null || vm.AllLines == null || vm.AllLines.Count == 0) return;

        // ── 1. Search match highlights ────────────────────────
        if (vm.Matches != null)
        {
            foreach (var match in vm.Matches)
            {
                RenderMatchHighlight(vm, match, match.IsActive);
            }
        }

        // ── 2. Selection range highlight ──────────────────────
        if (vm.SelectionStart.HasValue && vm.SelectionEnd.HasValue
            && vm.SelectionStart.Value != vm.SelectionEnd.Value)
        {
            int selStart = Math.Min(vm.SelectionStart.Value, vm.SelectionEnd.Value);
            int selEnd = Math.Max(vm.SelectionStart.Value, vm.SelectionEnd.Value);
            RenderRangeHighlight(vm, selStart, selEnd, false);
        }

        // ── 3. Cursor indicator ───────────────────────────────
        RenderCursor(vm, vm.CursorPosition);
    }

    /// <summary>Renders a highlighted rectangle for a single search match.</summary>
    private void RenderMatchHighlight(
        SelectionModeViewModel vm, SearchMatch match, bool isActive)
    {
        var rect = GetCharRect(vm, match.StartIndex, match.EndIndex - match.StartIndex);
        if (rect.Width <= 0 || rect.Height <= 0) return;

        var highlight = new System.Windows.Shapes.Rectangle
        {
            Fill = isActive
                ? new SolidColorBrush(Color.FromArgb(120, 255, 140, 0))   // Orange — active match
                : new SolidColorBrush(Color.FromArgb(70, 255, 255, 0)),    // Yellow — other matches
            RadiusX = 2,
            RadiusY = 2,
            Width = rect.Width,
            Height = rect.Height,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(highlight, rect.Left);
        Canvas.SetTop(highlight, rect.Top);
        layoutCanvas.Children.Add(highlight);
    }

    /// <summary>Renders a selection-range highlight between two character offsets.</summary>
    private void RenderRangeHighlight(
        SelectionModeViewModel vm, int fromOffset, int toOffset, bool isActive)
    {
        int length = toOffset - fromOffset;
        if (length <= 0) return;

        var rect = GetCharRect(vm, fromOffset, length);
        if (rect.Width <= 0 || rect.Height <= 0) return;

        var highlight = new System.Windows.Shapes.Rectangle
        {
            Fill = new SolidColorBrush(Color.FromArgb(80, 51, 153, 255)), // Blue selection
            Width = rect.Width,
            Height = rect.Height,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(highlight, rect.Left);
        Canvas.SetTop(highlight, rect.Top);
        layoutCanvas.Children.Add(highlight);
    }

    /// <summary>Renders a blinking cursor at the given character offset.</summary>
    private void RenderCursor(SelectionModeViewModel vm, int charOffset)
    {
        var rect = GetCharRect(vm, charOffset, 1);
        if (rect.Height <= 0) return;

        // Thin vertical line for the cursor
        var cursor = new System.Windows.Shapes.Line
        {
            X1 = rect.Left,
            Y1 = rect.Top + 2,
            X2 = rect.Left,
            Y2 = rect.Top + rect.Height - 2,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            IsHitTestVisible = false,
        };
        layoutCanvas.Children.Add(cursor);

        // Blinking animation
        var blink = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromSeconds(0.5),
            AutoReverse = true,
            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
        };
        cursor.BeginAnimation(UIElement.OpacityProperty, blink);
    }

    /// <summary>
    /// Estimates the bounding rectangle for a character range within the visible text.
    /// Uses line-level bounding rectangles from UIA and estimates horizontal position
    /// based on character index and average character width.
    /// </summary>
    private static Rect GetCharRect(SelectionModeViewModel vm, int charOffset, int charLength)
    {
        // Map the global character offset to a specific line + position within that line
        int remaining = charOffset;
        for (int i = 0; i < vm.AllLines.Count; i++)
        {
            var line = vm.AllLines[i];
            int lineLen = line.TextContent.Length;

            if (remaining <= lineLen)
            {
                // The character is on this line
                var lineRect = line.BoundingRectangle;

                // Estimate character width from the line's text and rect width
                double charWidth = lineLen > 0
                    ? lineRect.Width / lineLen
                    : 8.0;
                charWidth = Math.Max(charWidth, 5.0); // minimum sensible width

                double x = lineRect.Left + (remaining * charWidth);
                double w = Math.Max(charLength * charWidth, 2.0);

                return new Rect(x, lineRect.Top, w, lineRect.Height);
            }

            remaining -= (lineLen + 1); // +1 for the newline separator between lines
        }

        return Rect.Empty;
    }
}
