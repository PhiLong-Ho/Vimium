using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Vimium.NativeMethods;
using Vimium.Services;
using Vimium.ViewModels;

namespace Vimium.Views
{
    /// <summary>
    /// Interaction logic for OverlayView.xaml
    /// </summary>
    public partial class OverlayView
    {
        private readonly KeyboardHookService _keyboardHook = new KeyboardHookService();
        private DispatcherTimer _safetyTimer;
        private string _input = string.Empty;

        public OverlayView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The overlay never steals focus, so the underlying popup/menu stays open.
        /// Input is captured via a global low level keyboard hook instead.
        /// </summary>
        protected override bool StealFocus => false;

        private void OverlayView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Show Skadi loading icon only for Skadi theme
            if (ConfigService.Instance.Theme == "Skadi")
            {
                try
                {
                    LoadingIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/skadi.ico"));
                    LoadingIcon.Visibility = Visibility.Visible;
                }
                catch { }
            }

            var m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            var scaleX = m.M11;
            var scaleY = m.M22;

            // scale the items for non-96 DPIs
            layoutGrid.LayoutTransform = new ScaleTransform(1 / scaleX, 1 / scaleY);

            // resize the window for non-96 DPIs
            var vm = DataContext as OverlayViewModel;
            Left = vm.Bounds.Left / scaleX;
            Top = vm.Bounds.Top / scaleY;
            Width = vm.Bounds.Width / scaleX;
            Height = vm.Bounds.Height / scaleY;

            _keyboardHook.KeyDown += KeyboardHook_KeyDown;
            _keyboardHook.Install();

            // Safety net: never leave the overlay (and its global hook) hanging around
            _safetyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _safetyTimer.Tick += (s, args) => Close();
            _safetyTimer.Start();
        }

        /// <summary>Click anywhere on the overlay background to dismiss it.</summary>
        private void Overlay_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Close();
        }

        private void OverlayView_OnClosed(object sender, EventArgs e)
        {
            _safetyTimer?.Stop();
            _safetyTimer = null;
            _keyboardHook.KeyDown -= KeyboardHook_KeyDown;
            _keyboardHook.Dispose();
        }

        private void KeyboardHook_KeyDown(object sender, KeyboardHookService.KeyDownEventArgs e)
        {
            var vk = e.VirtualKeyCode;

            if (vk == User32.VK_ESCAPE)
            {
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(Close));
                return;
            }

            // While hints are still being generated, ignore all other keys
            var vm = DataContext as OverlayViewModel;
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
            var vm = DataContext as OverlayViewModel;
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
}
