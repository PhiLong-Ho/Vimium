using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Vimium.Models;
using Vimium.NativeMethods;
using Vimium.Services;
using Vimium.Services.Interfaces;
using Application = System.Windows.Application;

namespace Vimium.ViewModels
{
    internal class ShellViewModel : IDisposable
    {
        private readonly Action<OverlayViewModel> _showOverlay;
        private readonly Action<SelectionModeViewModel> _showSelectionModeOverlay;
        private readonly Action<DebugOverlayViewModel> _showDebugOverlay;
        private readonly Action<OptionsViewModel> _showOptions;
        private readonly IHintLabelService _hintLabelService;
        private readonly IHintProviderService _hintProviderService;
        private readonly IDebugHintProviderService _debugHintProviderService;
        private readonly IFindTextProviderService _findTextProviderService;
        private readonly IBenchmarkService _benchmarkService;
        private CancellationTokenSource _cts;

        public ShellViewModel(
            Action<OverlayViewModel> showOverlay,
            Action<SelectionModeViewModel> showSelectionModeOverlay,
            Action<DebugOverlayViewModel> showDebugOverlay,
            Action<OptionsViewModel> showOptions,
            IHintLabelService hintLabelService,
            IHintProviderService hintProviderService,
            IDebugHintProviderService debugHintProviderService,
            IFindTextProviderService findTextProviderService,
            IKeyListenerService keyListener,
            IBenchmarkService benchmarkService = null)
        {
            _showOverlay = showOverlay;
            _showSelectionModeOverlay = showSelectionModeOverlay;
            _showDebugOverlay = showDebugOverlay;
            _showOptions = showOptions;
            _hintLabelService = hintLabelService;
            var keyListener1 = keyListener;
            _hintProviderService = hintProviderService;
            _debugHintProviderService = debugHintProviderService;
            _findTextProviderService = findTextProviderService;
            _benchmarkService = benchmarkService ?? new BenchmarkService();

            // Wire benchmark service to the hint provider for logging
            if (_hintProviderService is UiAutomationHintProviderService uiAutomationService)
            {
                uiAutomationService.BenchmarkService = _benchmarkService;
            }

            // Read hotkeys from config (with fallback to defaults)
            ApplyHotkeys(keyListener1);

            // Re-register hotkeys when config changes
            ConfigService.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is null or "" or "OverlayModifier" or "TaskbarModifier"
                    or "LineNavigationModifier" or "TextSelectionModifier")
                    ApplyHotkeys(keyListener1);
            };

#if DEBUG
            keyListener1.DebugHotKey = new HotKey
            {
                Keys = Keys.OemSemicolon,
                Modifier = KeyModifier.Alt | KeyModifier.Shift
            };
#endif

            keyListener1.OnHotKeyActivated += _keyListener_OnHotKeyActivated;
            keyListener1.OnTaskbarHotKeyActivated += _keyListener_OnTaskbarHotKeyActivated;
            keyListener1.OnDebugHotKeyActivated += _keyListener_OnDebugHotKeyActivated;
            keyListener1.OnLineNavigationHotKeyActivated += _keyListener_OnLineNavigationHotKeyActivated;

            ShowOptionsCommand = new DelegateCommand(ShowOptions);
            ExitCommand = new DelegateCommand(Exit);
        }

        public DelegateCommand ShowOptionsCommand { get; }
        public DelegateCommand ExitCommand { get; }

        private static void ApplyHotkeys(IKeyListenerService keyListener)
        {
            var cfg = ConfigService.Instance;
            keyListener.HotKey = HotKey.Parse(cfg.OverlayModifier);
            keyListener.TaskbarHotKey = HotKey.Parse(cfg.TaskbarModifier);
            keyListener.LineNavigationHotKey = HotKey.Parse(cfg.LineNavigationModifier);
            LogService.Info($"Hotkeys registered: Overlay={cfg.OverlayModifier}, Taskbar={cfg.TaskbarModifier}, LineNav={cfg.LineNavigationModifier}, CopyMod={cfg.CopyModifier}");
        }

        /// <summary>
        /// True while a hint overlay is being prepared or shown. Used to ignore
        /// repeated hotkey presses instead of stacking multiple overlays.
        /// </summary>
        private bool _overlayActive;

        /// <summary>
        /// True while the text selection overlay is active.
        /// </summary>
        private bool _selectionOverlayActive;

        private async void _keyListener_OnHotKeyActivated(object sender, EventArgs e)
        {
            if (_overlayActive || _selectionOverlayActive) return;
            _overlayActive = true;

            var hWnd = User32.GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
            {
                _overlayActive = false;
                return;
            }

            // Create cancellation token source for this enumeration session.
            // Cancel on overlay close to stop enumeration early.
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            // T035: Set filter mode based on the default action slot.
            // InvokeFiltered for click-based actions, AllElements for move/hover.
            var defaultAction = ConfigService.Instance.ActionSlots.Length > 0
                ? ConfigService.Instance.ActionSlots[0].Action
                : Models.HintAction.Invoke;
            if (_hintProviderService is UiAutomationHintProviderService uiAutomation)
            {
                uiAutomation.FilterMode = defaultAction switch
                {
                    Models.HintAction.Hover => "AllElements",
                                        _ => "InvokeFiltered",
                };
            }

            // Get window bounds (fast, no COM) and show the overlay immediately
            // with a loading indicator while hints are enumerated.
            var rawBounds = new RECT();
            User32.GetWindowRect(hWnd, ref rawBounds);
            Rect windowBounds = rawBounds;

            var vm = new OverlayViewModel(windowBounds);
            vm.ActionSlots = ConfigService.Instance.ActionSlots;
            vm.Closed = () => _overlayActive = false;
            vm.CloseOverlay = () =>
            {
                // Cancel enumeration if it's still running
                try { _cts?.Cancel(); } catch { }
            };
            _showOverlay(vm);

            // Enumerate hints on a background thread to keep the UI responsive
            var session = await _hintProviderService.EnumHintsAsync(hWnd, ct);
            if (session != null)
            {
                // Update the already-visible overlay on the UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    vm.PopulateHints(session, _hintLabelService);
                });
            }
            else
            {
                // Window may have disappeared or enumeration was cancelled
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    vm.CloseOverlay?.Invoke();
                });
            }
        }

        private async void _keyListener_OnTaskbarHotKeyActivated(object sender, EventArgs e)
        {
            if (_overlayActive || _selectionOverlayActive) return;
            _overlayActive = true;

            var taskbarHWnd = User32.FindWindow("Shell_traywnd", "");
            if (taskbarHWnd == IntPtr.Zero)
            {
                _overlayActive = false;
                return;
            }

            // Create cancellation token source for this enumeration session
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            // T035: Set filter mode based on the default action slot.
            var defaultAction = ConfigService.Instance.ActionSlots.Length > 0
                ? ConfigService.Instance.ActionSlots[0].Action
                : Models.HintAction.Invoke;
            if (_hintProviderService is UiAutomationHintProviderService uiAutomation2)
            {
                uiAutomation2.FilterMode = defaultAction switch
                {
                    Models.HintAction.Hover => "AllElements",
                                        _ => "InvokeFiltered",
                };
            }

            // Get window bounds (fast) and show overlay with loading indicator
            var rawBounds = new RECT();
            User32.GetWindowRect(taskbarHWnd, ref rawBounds);
            Rect windowBounds = rawBounds;

            var vm = new OverlayViewModel(windowBounds);
            vm.ActionSlots = ConfigService.Instance.ActionSlots;
            vm.Closed = () => _overlayActive = false;
            vm.CloseOverlay = () =>
            {
                try { _cts?.Cancel(); } catch { }
            };
            _showOverlay(vm);

            // Enumerate hints on background thread
            var session = await _hintProviderService.EnumHintsAsync(taskbarHWnd, ct);
            if (session != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    vm.PopulateHints(session, _hintLabelService);
                });
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    vm.CloseOverlay?.Invoke();
                });
            }
        }

        private async void _keyListener_OnLineNavigationHotKeyActivated(object sender, EventArgs e)
        {
            if (_overlayActive || _selectionOverlayActive)
            {
                LogService.Warn($"FindText blocked: overlayActive={_overlayActive}, selectionOverlayActive={_selectionOverlayActive}");
                return;
            }
            _selectionOverlayActive = true;

            try
            {
                var hWnd = User32.GetForegroundWindow();
                LogService.Info($"FindText: foreground hWnd=0x{hWnd:X}");
                if (hWnd == IntPtr.Zero)
                {
                    LogService.Warn("FindText: no foreground window");
                    _selectionOverlayActive = false;
                    return;
                }

                var rawBounds = new RECT();
                User32.GetWindowRect(hWnd, ref rawBounds);
                Rect windowBounds = rawBounds;

                // Open the overlay immediately with an empty search bar — no text
                // extraction step. Search runs on demand once the user types ≥5 chars.
                var vm = new SelectionModeViewModel(_findTextProviderService, windowBounds, hWnd);
                vm.CloseOverlay = () => _selectionOverlayActive = false;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _showSelectionModeOverlay(vm);
                });
                LogService.Info("FindText: overlay shown (empty search bar)");
            }
            catch (Exception ex)
            {
                LogService.Error("FindText: exception in handler", ex);
                _selectionOverlayActive = false;
            }
        }

        private void _keyListener_OnDebugHotKeyActivated(object sender, EventArgs e)
        {
            var session = _debugHintProviderService.EnumDebugHints();
            if (session != null)
            {
                var vm = new DebugOverlayViewModel(session);
                _showDebugOverlay(vm);
            }
        }

        public void Exit()
        {
            Application.Current.Shutdown();
        }

        public void ShowOptions()
        {
            var vm = new OptionsViewModel();
            _showOptions(vm);
        }

        /// <summary>
        /// Disposes the pending cancellation token source. The shell view-model
        /// lives for the process lifetime; this provides deterministic cleanup of
        /// the disposable field it owns.
        /// </summary>
        public void Dispose()
        {
            _cts?.Dispose();
            _cts = null;
            GC.SuppressFinalize(this);
        }
    }
}
