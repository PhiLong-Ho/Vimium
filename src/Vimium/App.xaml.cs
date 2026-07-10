using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Diagnostics;
using Vimium.Models;
using Vimium.Services;
using Vimium.ViewModels;
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
        private readonly FindTextProviderService _findTextProviderService = new FindTextProviderService();

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

        private void ApplyTheme(string theme)
        {
            var dicts = Current.Resources.MergedDictionaries;
            for (int i = dicts.Count - 1; i >= 0; i--)
            {
                var src = dicts[i].Source?.ToString() ?? "";
                if (src.Contains("Themes/"))
                    dicts.RemoveAt(i);
            }
            var path = theme switch
            {
                "Dark" => "Themes/DarkTheme.xaml",
                "Skadi" => "Themes/SkadiTheme.xaml",
                _ => "Themes/LightTheme.xaml",
            };
            dicts.Insert(0, new ResourceDictionary { Source = new Uri(path, UriKind.Relative) });
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

        private void ShowSelectionModeOverlay(SelectionModeViewModel vm)
        {
            var view = new SelectionModeOverlayView
            {
                DataContext = vm
            };
            // Chain the existing close action (e.g., resetting _selectionOverlayActive)
            // with view close — don't overwrite it.
            var existingClose = vm.CloseOverlay;
            vm.CloseOverlay = () =>
            {
                existingClose?.Invoke();
                view.Close();
            };
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
            // Apply saved theme and listen for changes
            ApplyTheme(ConfigService.Instance.Theme);
            ConfigService.Instance.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName is null or "" or "Theme")
                    Dispatcher.Invoke(() => ApplyTheme(ConfigService.Instance.Theme));
            };

            if (e.Args.Contains("/line-nav"))
            {
                // Diagnostic mode: open the find overlay against the foreground window.
                var hWnd = User32.GetForegroundWindow();
                Services.LogService.Info($"DIAG: /line-nav mode — testing hWnd=0x{hWnd:X}");

                var title = new System.Text.StringBuilder(256);
                NativeMethods.User32.GetWindowText(hWnd, title, title.Capacity);
                Services.LogService.Info($"DIAG: Window title = \"{title}\"");

                var className = new System.Text.StringBuilder(256);
                NativeMethods.User32.GetClassName(hWnd, className, className.Capacity);
                Services.LogService.Info($"DIAG: Window class = \"{className}\"");

                var rawBounds = new RECT();
                User32.GetWindowRect(hWnd, ref rawBounds);
                var vm = new ViewModels.SelectionModeViewModel(
                    _findTextProviderService,
                    (Rect)rawBounds,
                    hWnd);
                var view = new Views.SelectionModeOverlayView { DataContext = vm };
                vm.CloseOverlay = () => view.Close();
                view.Show();

                _ = Task.Delay(5000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => view.Close());
                    Dispatcher.Invoke(() => Current.Shutdown());
                });
                return;
            }
            else if (e.Args.Contains("/hint"))
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
                    var args = Environment.GetCommandLineArgs();
                    if (args.Contains("--force", StringComparer.OrdinalIgnoreCase))
                    {
                        // Kill existing Vimium processes so the new instance can start
                        var currentProc = Process.GetCurrentProcess();
                        foreach (var p in Process.GetProcessesByName("Vimium"))
                        {
                            if (p.Id != currentProc.Id)
                            {
                                try { p.Kill(); p.WaitForExit(2000); }
                                catch { }
                            }
                        }
                        // Brief pause to allow the mutex to be released
                        System.Threading.Thread.Sleep(500);
                    }
                    else
                    {
                        Current.Shutdown();
                        return;
                    }
                }

                // Create this as late as possible as it has a window
                _keyListenerService = new KeyListenerService();

                var shellViewModel = new ShellViewModel(
                    ShowOverlay,
                    ShowSelectionModeOverlay,
                    ShowDebugOverlay,
                    ShowOptions,
                    _hintLabelService,
                    _hintProviderService,
                    _hintProviderService,
                    _findTextProviderService,
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
