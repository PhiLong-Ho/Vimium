using System;
using System.Windows;
using Vimium.ViewModels;
using System.Linq;
using System.Diagnostics;
using Vimium.Services;
using Vimium.Views;
using Vimium.NativeMethods;

namespace Vimium
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly SingleLaunchMutex _singleLaunchMutex = new SingleLaunchMutex();
        private readonly UiAutomationHintProviderService _hintProviderService = new UiAutomationHintProviderService();

        private readonly HintLabelService _hintLabelService = new HintLabelService();
        private KeyListenerService _keyListenerService;

        public App()
        {
            // Safety net: an interaction with another app's UI (e.g. invoking a stale
            // UI Automation element) can throw from a background dispatcher callback.
            // Log and swallow rather than letting it terminate the whole app.
            DispatcherUnhandledException += (sender, args) =>
            {
                Debug.WriteLine("Unhandled exception: " + args.Exception);
                args.Handled = true;
            };
        }

        private void ShowOverlay(OverlayViewModel vm)
        {
            var view = new OverlayView
            {
                DataContext = vm
            };
            vm.CloseOverlay = () => view.Close();
            view.Closed += (s, e) => vm.Closed?.Invoke();
            view.Show();
        }

        private void ShowDebugOverlay(DebugOverlayViewModel vm)
        {
            var view = new DebugOverlayView
            {
                DataContext = vm
            };
            view.ShowDialog();
        }

        private void ShowOptions(OptionsViewModel vm)
        {
            var view = new OptionsView
            {
                DataContext = vm
            };
            view.ShowDialog();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Contains("/hint"))
            {
                // support headless mode — show overlay immediately, enumerate on background
                var hWnd = User32.GetForegroundWindow();
                if (hWnd != IntPtr.Zero)
                {
                    var rawBounds = new RECT();
                    User32.GetWindowRect(hWnd, ref rawBounds);
                    var vm = new OverlayViewModel((Rect)rawBounds);
                    var overlayWindow = new OverlayView { DataContext = vm };
                    overlayWindow.Show();

                    var session = await _hintProviderService.EnumHintsAsync(hWnd);
                    if (session != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() => vm.PopulateHints(session, _hintLabelService));
                    }
                }
            }
            else if (e.Args.Contains("/tray"))
            {
                // support headless tray mode — show overlay immediately, enumerate on background
                var taskbarHWnd = User32.FindWindow("Shell_traywnd", "");
                if (taskbarHWnd != IntPtr.Zero)
                {
                    var rawBounds = new RECT();
                    User32.GetWindowRect(taskbarHWnd, ref rawBounds);
                    var vm = new OverlayViewModel((Rect)rawBounds);
                    var overlayWindow = new OverlayView { DataContext = vm };
                    overlayWindow.Show();

                    var session = await _hintProviderService.EnumHintsAsync(taskbarHWnd);
                    if (session != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() => vm.PopulateHints(session, _hintLabelService));
                    }
                }
            }
            else
            {
                // Prevent multiple startup in non-headless mode
                if (_singleLaunchMutex.AlreadyRunning)
                {
                    Current.Shutdown();
                    return;
                }

                // Create this as late as possible as it has a window
                _keyListenerService = new KeyListenerService();

                var shellViewModel = new ShellViewModel(
                    ShowOverlay,
                    ShowDebugOverlay,
                    ShowOptions,
                    _hintLabelService,
                    _hintProviderService,
                    _hintProviderService,
                    _keyListenerService);

                var shellView = new ShellView
                {
                    DataContext = shellViewModel
                };
                shellView.Show();
            }
            base.OnStartup(e);
        }
    }
}
