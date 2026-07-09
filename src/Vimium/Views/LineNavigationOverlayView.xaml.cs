using System;
using System.Windows;
using System.Windows.Media;
using Vimium.NativeMethods;
using Vimium.Services;
using Vimium.ViewModels;

namespace Vimium.Views;

/// <summary>
/// Interaction logic for LineNavigationOverlayView.xaml
/// </summary>
public partial class LineNavigationOverlayView
{
    private readonly KeyboardHookService _keyboardHook = new KeyboardHookService();
    private System.Windows.Threading.DispatcherTimer _safetyTimer;
    private string _input = string.Empty;
    private bool _isClosed;

    public LineNavigationOverlayView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The overlay never steals focus, so the underlying popup/menu stays open.
    /// Input is captured via a global low level keyboard hook instead.
    /// </summary>
    protected override bool StealFocus => false;

    private void LineNavigationOverlayView_OnLoaded(object sender, RoutedEventArgs e)
    {
        var m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
        var scaleX = m.M11;
        var scaleY = m.M22;

        // scale the items for non-96 DPIs
        layoutGrid.LayoutTransform = new ScaleTransform(1 / scaleX, 1 / scaleY);

        // resize the window for non-96 DPIs
        var vm = DataContext as LineNavigationOverlayViewModel;
        Left = vm.Bounds.Left / scaleX;
        Top = vm.Bounds.Top / scaleY;
        Width = vm.Bounds.Width / scaleX;
        Height = vm.Bounds.Height / scaleY;

        _keyboardHook.KeyDown += KeyboardHook_KeyDown;
        _keyboardHook.Install();

        // Safety net: never leave the overlay (and its global hook) hanging around
        _safetyTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _safetyTimer.Tick += (s, args) => Close();
        _safetyTimer.Start();

        // Guard against hook events after window close
        this.Closed += (s, args) => _isClosed = true;
    }

    /// <summary>Click anywhere on the overlay background to dismiss it.</summary>
    private void Overlay_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Close();
    }

    private void LineNavigationOverlayView_OnClosed(object sender, EventArgs e)
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

        if (vk == User32.VK_ESCAPE)
        {
            e.Handled = true;
            Dispatcher.BeginInvoke(new Action(Close));
            return;
        }

        // While hints are still being generated, ignore all other keys
        var vm = DataContext as LineNavigationOverlayViewModel;
        if (vm != null && vm.IsLoading)
        {
            e.Handled = true;
            return;
        }

        if (vk == User32.VK_BACK)
        {
            e.Handled = true;
            if (_input.Length > 0)
            {
                _input = _input.Substring(0, _input.Length - 1);
            }
            UpdateInput();
            return;
        }

        // Hint labels are letters (A-Z)
        if (vk >= 'A' && vk <= 'Z')
        {
            e.Handled = true;
            _input += (char)vk;
            UpdateInput();
            return;
        }

        // Anything else (shift, arrows, etc.) is left untouched and passes through.
    }

    private void UpdateInput()
    {
        var vm = DataContext as LineNavigationOverlayViewModel;
        if (vm == null)
        {
            return;
        }

        // Run on the UI thread after the hook returns so input isn't blocked
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _safetyTimer?.Stop();
            _safetyTimer?.Start();

            MatchStringControl.Text = _input;

            // Setting this may resolve a hint and trigger CloseOverlay
            vm.MatchString = _input;
        }));
    }
}
