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
    public partial class OverlayView : IDisposable
    {
        private readonly KeyboardHookService _keyboardHook = new KeyboardHookService();
        private DispatcherTimer _safetyTimer;
        private string _input = string.Empty;
        private bool _disposed;

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
            // Show Arknights loading icon only for Arknights theme
            if (ConfigService.Instance.Theme == "Arknights")
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
            Dispose();
        }

        /// <summary>
        /// Releases the global keyboard hook and safety timer. Called when the
        /// overlay window closes; also satisfies deterministic cleanup for the
        /// disposable fields this view owns.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _safetyTimer?.Stop();
            _safetyTimer = null;
            _keyboardHook.KeyDown -= KeyboardHook_KeyDown;
            _keyboardHook.Dispose();
            GC.SuppressFinalize(this);
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

            // T013: While hints are still being generated, buffer typed characters
            // so they aren't lost. Buffered input is applied when PopulateHints completes.
            var vm = DataContext as OverlayViewModel;
            if (vm != null && vm.IsLoading)
            {
                e.Handled = true;
                if (vk >= 'A' && vk <= 'Z')
                {
                    vm.PendingInput += (char)vk;
                }
                else if (vk == User32.VK_BACK && vm.PendingInput.Length > 0)
                {
                    vm.PendingInput = vm.PendingInput.Substring(0, vm.PendingInput.Length - 1);
                }
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

            // Type-mode: detect standalone modifier key presses (when no hint
            // has been typed yet). If a slot is configured with this modifier
            // in "Type" mode, arm it so the next hint match uses that action.
            if (_input.Length == 0 && vm != null && IsModifierKey(vk))
            {
                var modifierName = VkToModifierName(vk);
                if (!string.IsNullOrEmpty(modifierName))
                {
                    foreach (var slot in vm.ActionSlots)
                    {
                        if (slot.Mode == "Type"
                            && string.Equals(slot.Modifier, modifierName, StringComparison.OrdinalIgnoreCase))
                        {
                            vm.ArmedModifier = modifierName;
                            e.Handled = true;
                            return;
                        }
                    }
                }
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

        /// <summary>Returns true if the virtual key code is a modifier key.</summary>
        private static bool IsModifierKey(int vk)
        {
            return vk == User32.VK_LSHIFT || vk == User32.VK_RSHIFT
                || vk == User32.VK_LCONTROL || vk == User32.VK_RCONTROL
                || vk == User32.VK_LMENU || vk == User32.VK_RMENU
                || vk == User32.VK_LWIN || vk == User32.VK_RWIN;
        }

        /// <summary>Maps a virtual key code to its modifier name string.</summary>
        private static string VkToModifierName(int vk)
        {
            return vk switch
            {
                User32.VK_LSHIFT => "Shift",
                User32.VK_RSHIFT => "Shift",
                User32.VK_LCONTROL => "Ctrl",
                User32.VK_RCONTROL => "Ctrl",
                User32.VK_LMENU => "Alt",
                User32.VK_RMENU => "Alt",
                User32.VK_LWIN => "Win",
                User32.VK_RWIN => "Win",
                _ => null,
            };
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
