using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Vimium.Models;
using Vimium.NativeMethods;
using Vimium.Services;
using Vimium.Services.Interfaces;

namespace Vimium.ViewModels
{
    internal class OverlayViewModel : NotifyPropertyChanged
    {
        private readonly ConfigService _config = ConfigService.Instance;
        private Rect _bounds;
        private ObservableCollection<HintViewModel> _hints = new ObservableCollection<HintViewModel>();
        private bool _isLoading;

        /// <summary>
        /// Creates an overlay in the loading state — the overlay appears immediately
        /// with a "Generating hints…" indicator while enumeration runs on a background
        /// thread. Call <see cref="PopulateHints"/> when the session is ready.
        /// </summary>
        /// <param name="bounds">The owning window bounds (cheap to get)</param>
        public OverlayViewModel(Rect bounds)
        {
            _bounds = bounds;
            _isLoading = true;
            _config.PropertyChanged += OnConfigChanged;
        }

        public OverlayViewModel(
            HintSession session,
            IHintLabelService hintLabelService)
        {
            _bounds = session.OwningWindowBounds;
            PopulateHints(session, hintLabelService);
            _config.PropertyChanged += OnConfigChanged;
        }

        private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is null or "" or "FontSize" or "HintActiveBackground"
                or "HintInactiveBackground" or "HintTextColor" or "HintFontFamily")
            {
                NotifyOfPropertyChange(nameof(HintActiveBrush));
                NotifyOfPropertyChange(nameof(HintInactiveBrush));
                NotifyOfPropertyChange(nameof(HintTextBrush));
            }
        }

        // ── Dynamic hint colors (from ConfigService) ─────────

        public Brush HintActiveBrush => HexToBrush(_config.HintActiveBackground);
        public Brush HintInactiveBrush => HexToBrush(_config.HintInactiveBackground);
        public Brush HintTextBrush => HexToBrush(_config.HintTextColor);

        private static Brush HexToBrush(string hex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch
            {
                return Brushes.Yellow;
            }
        }

        /// <summary>
        /// Fills in the hint labels once the session has been enumerated.
        /// Call on the UI thread.
        /// </summary>
        public void PopulateHints(HintSession session, IHintLabelService hintLabelService)
        {
            var labels = hintLabelService.GetHintStrings(session.Hints.Count());

            // Parse font size once — used for label dimensions and multi-line detection
            double fontSize = 14;
            if (double.TryParse(_config.FontSize, out var parsedFontSize))
                fontSize = parsedFontSize;

            // T031: Build hint VMs and collect positions for overlap resolution
            var hintVMs = new List<HintViewModel>(labels.Count);
            var positions = new List<Models.HintLabelPosition>(labels.Count);

            for (int i = 0; i < labels.Count; ++i)
            {
                var hint = session.Hints[i];
                var vm = new HintViewModel(hint)
                {
                    Label = labels[i],
                    Active = false,
                };
                hintVMs.Add(vm);

                // Estimate label dimensions based on font size and text length.
                // Hint labels use Viewbox StretchDirection="DownOnly" so the
                // rendered size is constrained by the element bounds. We use a
                // conservative estimate to avoid false collision positives.

                // Bold Helvetica at fontSize: ~0.55 * fontSize per char, plus 2px padding
                var labelWidth = labels[i].Length * fontSize * 0.55 + 2;
                var labelHeight = fontSize * 1.2; // approximate line height

                positions.Add(new Models.HintLabelPosition
                {
                    OriginalLeft = hint.BoundingRectangle.Left,
                    OriginalTop = hint.BoundingRectangle.Top,
                    AdjustedLeft = hint.BoundingRectangle.Left,
                    AdjustedTop = hint.BoundingRectangle.Top,
                    LabelWidth = labelWidth,
                    LabelHeight = labelHeight,
                    ElementWidth = hint.BoundingRectangle.Width,
                    ElementHeight = hint.BoundingRectangle.Height,
                });
            }

            // Mitigation for multi-line wrapped elements: Chrome UIA reports the
            // bounding rect of the entire hyperlink element, which for wrapped text
            // spans full-width across 2+ lines. Placing the hint at the top-left
            // puts it on the wrong line. Instead, center the label vertically within
            // the element so it's visually associated with the link text.
            //
            // IMPORTANT: this runs BEFORE overlap resolution so the resolver sees
            // the corrected position, preventing false collisions between a wrapped
            // link's oversized bounding rect and adjacent single-line links.
            double typicalLineHeight = fontSize * 1.5;
            double multiLineThreshold = typicalLineHeight * 1.6;

            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i].ElementHeight > multiLineThreshold)
                {
                    // Center the label vertically within the multi-line element
                    double centeredTop = positions[i].OriginalTop
                        + (positions[i].ElementHeight - positions[i].LabelHeight) / 2.0;

                    // Clamp to keep within reasonable bounds
                    if (centeredTop < positions[i].OriginalTop)
                        centeredTop = positions[i].OriginalTop;

                    // Update both OriginalTop (for the overlap resolver's pre-scan)
                    // and AdjustedTop (for the final label position)
                    positions[i].OriginalTop = centeredTop;
                    positions[i].AdjustedTop = centeredTop;
                }
            }

            // T031: Resolve overlapping labels via spiral offsetting.
            // Runs after centering so multi-line elements are at their corrected
            // (centered) positions rather than the oversized bounding rect top-left.
            var overlapResolver = new Services.OverlapResolver();
            overlapResolver.ResolveOverlaps(positions, maxOffset: 20);

            // Apply adjusted positions to HintViewModels
            for (int i = 0; i < hintVMs.Count; i++)
            {
                hintVMs[i].AdjustedLeft = positions[i].AdjustedLeft;
                hintVMs[i].AdjustedTop = positions[i].AdjustedTop;
                _hints.Add(hintVMs[i]);
            }

            IsLoading = false;

            // T013: Apply any keystrokes buffered during the loading phase
            if (!string.IsNullOrEmpty(PendingInput))
            {
                var pending = PendingInput;
                PendingInput = "";
                MatchString = pending;
            }
        }

        /// <summary>
        /// Bounds in logical screen coordiantes
        /// </summary>
        public Rect Bounds
        {
            get
            {
                return _bounds;
            }
            set
            {
                _bounds = value;
                NotifyOfPropertyChange();
            }
        }

        public ObservableCollection<HintViewModel> Hints
        {
            get
            {
                return _hints;
            }
            set
            {
                _hints = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// True while hints are being enumerated on a background thread.
        /// The overlay shows a loading indicator instead of hint labels.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; NotifyOfPropertyChange(); }
        }

        /// <summary>
        /// Accumulated keystrokes typed while the overlay was in loading state.
        /// Applied to MatchString when PopulateHints completes.
        /// </summary>
        public string PendingInput { get; set; } = "";

        public Action CloseOverlay { get; set; }

        /// <summary>
        /// Invoked when the overlay window has actually closed.
        /// </summary>
        public Action Closed { get; set; }

        /// <summary>
        /// The configured action slots (from VimiumConfig.ActionSlots).
        /// Set by ShellViewModel before hints are populated.
        /// </summary>
        public Models.ActionSlot[] ActionSlots { get; set; } = Models.ActionSlot.CreateDefaults();

        /// <summary>
        /// For type-mode: the modifier that was "armed" by pressing and releasing
        /// a modifier key before typing the hint label. Set by the keyboard hook
        /// in OverlayView. Example: "Alt", "Ctrl", "Shift", "Win".
        /// Cleared after match resolution or on Escape.
        /// </summary>
        public string ArmedModifier { get; set; } = "";

        /// <summary>Guard against reentrant calls — Invoke() can trigger a modal
        /// dialog (e.g. Options) which runs a nested message loop.</summary>
        private bool _resolving;

        public string MatchString
        {
            set
            {
                if (_resolving) return;
                _resolving = true;

                try
                {

                foreach (var x in Hints)
                {
                    x.Active = false;
                }

                var matching = Hints.Where(x => x.Label.StartsWith(value, StringComparison.OrdinalIgnoreCase)).ToArray();
                foreach (var x in matching)
                {
                    x.Active = true;
                }

                if (matching.Count() == 1)
                {
                    var selectedHint = matching.First().Hint;

                    try
                    {
                        // T019: Multi-slot action resolution — find the first
                        // ActionSlot whose modifier combination matches the
                        // currently-held keys.
                        var action = ResolveAction();
                        ExecuteHintAction(selectedHint, action);
                    }
                    catch (Exception)
                    {
                        // The target element may have gone away or the action may not be
                        // valid in its current state (e.g. a stale UI Automation element
                        // throws COMException). Never let that crash the app -- just close.
                        CloseOverlay?.Invoke();
                    }
                }
                }
                finally
                {
                    _resolving = false;
                }
            }
        }

        /// <summary>
        /// Resolves the action to take. Checks in order:
        /// 1. Hold-mode: slots with actual modifiers first (1, 2, 3, …)
        /// 2. Type-mode: slots with Type mode and matching ArmedModifier
        /// 3. Fallback: slot 0 (default, no modifier — only when nothing else matches)
        /// </summary>
        private HintAction ResolveAction()
        {
            var slots = ActionSlots ?? Models.ActionSlot.CreateDefaults();

            // Phase 1: Hold-mode — check slots with non-empty modifiers first.
            // Skip slot 0 (empty modifier) to prevent it from short-circuiting
            // the loop before slots 1+ can be checked.
            foreach (var slot in slots)
            {
                if (slot.SlotIndex == 0)
                    continue; // handled as fallback
                if (string.IsNullOrEmpty(slot.Modifier))
                    continue;
                if (slot.Mode == "Type")
                    continue; // handled in phase 2
                if (IsModifierHeld(slot.Modifier))
                    return slot.Action;
            }

            // Phase 2: Type-mode — check ArmedModifier against Type-mode slots
            if (!string.IsNullOrEmpty(ArmedModifier))
            {
                foreach (var slot in slots)
                {
                    if (slot.Mode == "Type"
                        && !string.IsNullOrEmpty(slot.Modifier)
                        && string.Equals(slot.Modifier, ArmedModifier, StringComparison.OrdinalIgnoreCase))
                    {
                        ArmedModifier = ""; // consume the armed modifier
                        return slot.Action;
                    }
                }
            }

            // Phase 3: Fallback to slot 0 (default, no modifier required)
            return slots.Length > 0 ? slots[0].Action : HintAction.Invoke;
        }

        /// <summary>
        /// Checks whether the given modifier combination is currently held.
        /// Modifier string format: "Shift", "Ctrl+Shift", "Ctrl+Alt", etc.
        /// Left and right variants are treated symmetrically.
        /// </summary>
        private static bool IsModifierHeld(string modifier)
        {
            if (string.IsNullOrEmpty(modifier))
                return true; // slot 0 default — always matches when no other does

            var parts = modifier.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return true;

            foreach (var part in parts)
            {
                if (!IsSingleModifierHeld(part))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether a single modifier key (Shift, Ctrl, Alt, Win) is held,
        /// checking both left and right variants symmetrically.
        /// </summary>
        private static bool IsSingleModifierHeld(string modifier)
        {
            return modifier.ToLowerInvariant() switch
            {
                "shift" => (User32.GetAsyncKeyState(User32.VK_LSHIFT) & 0x8000) != 0
                        || (User32.GetAsyncKeyState(User32.VK_RSHIFT) & 0x8000) != 0,
                "ctrl" or "control" => (User32.GetAsyncKeyState(User32.VK_LCONTROL) & 0x8000) != 0
                                    || (User32.GetAsyncKeyState(User32.VK_RCONTROL) & 0x8000) != 0,
                "alt" or "menu" => (User32.GetAsyncKeyState(User32.VK_LMENU) & 0x8000) != 0
                                 || (User32.GetAsyncKeyState(User32.VK_RMENU) & 0x8000) != 0,
                "win" or "windows" => (User32.GetAsyncKeyState(User32.VK_LWIN) & 0x8000) != 0
                                    || (User32.GetAsyncKeyState(User32.VK_RWIN) & 0x8000) != 0,
                _ => false,
            };
        }

        /// <summary>
        /// T019: Executes the resolved hint action on a background thread.
        /// Supports Invoke, LeftClick, RightClick, MoveMouse, and Hover.
        /// </summary>
        private void ExecuteHintAction(Models.Hint selectedHint, HintAction action)
        {
            CloseOverlay?.Invoke();
            var h = selectedHint;

            switch (action)
            {
                case HintAction.Invoke:
                    System.Threading.Tasks.Task.Run(() => h.Invoke());
                    break;
                case HintAction.LeftClick:
                    System.Threading.Tasks.Task.Run(() => h.Click());
                    break;
                case HintAction.RightClick:
                    System.Threading.Tasks.Task.Run(() => h.RightClick());
                    break;
                case HintAction.MoveMouse:
                    // Move cursor to element center, no click. Triggers CSS :hover.
                    System.Threading.Tasks.Task.Run(() => h.MovePointerToCenter());
                    break;
                default:
                    System.Threading.Tasks.Task.Run(() => h.Invoke());
                    break;
            }
        }
    }
}
