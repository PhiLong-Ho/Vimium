using Vimium.Extensions;
using Vimium.Models;
using Vimium.NativeMethods;
using Vimium.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Interop.UIAutomationClient;

namespace Vimium.Services
{
    internal class UiAutomationHintProviderService : IHintProviderService, IDebugHintProviderService
    {
        private readonly IUIAutomation _automation;

        // ── Caching ──────────────────────────────────────────

        private HintSession _cachedSession;
        private IntPtr _cachedHwnd;
        private string _cachedFilterMode;

        // ── Benchmark integration ────────────────────────────

        /// <summary>
        /// Optional benchmark service for logging enumeration metrics.
        /// Set by ShellViewModel before first use.
        /// </summary>
        public IBenchmarkService BenchmarkService { get; set; }

        /// <summary>
        /// Current filter mode ("InvokeFiltered" or "AllElements").
        /// Set before enumeration begins.
        /// </summary>
        public string FilterMode { get; set; } = "InvokeFiltered";

        public UiAutomationHintProviderService()
        {
            _automation = new CUIAutomation();
        }

        /// <summary>
        /// Private constructor used by <see cref="EnumHintsAsync"/> to create a
        /// service whose COM object lives on the background thread.
        /// </summary>
        private UiAutomationHintProviderService(IUIAutomation automation)
        {
            _automation = automation;
        }

        public HintSession EnumHints()
        {
            var foregroundWindow = User32.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return null;
            }
            return EnumHints(foregroundWindow);
        }

        public HintSession EnumHints(IntPtr hWnd)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Check cache before enumerating
            bool cacheHit = false;
            if (_cachedSession != null && _cachedHwnd == hWnd)
            {
                cacheHit = true;
                sw.Stop();

                // Log benchmark for cache hit
                LogBenchmark(hWnd, _cachedSession.Hints.Count, sw.ElapsedMilliseconds, cacheHit);

                Debug.WriteLine("Enumeration of hints (cached) took {0} ms", sw.ElapsedMilliseconds);
                return _cachedSession;
            }

            var session = EnumWindowHints(hWnd, CreateHint);
            sw.Stop();

            // Update cache
            _cachedSession = session;
            _cachedHwnd = hWnd;
            _cachedFilterMode = FilterMode;

            // Log benchmark for cache miss (cold start)
            LogBenchmark(hWnd, session?.Hints?.Count ?? 0, sw.ElapsedMilliseconds, cacheHit);

            Debug.WriteLine("Enumeration of hints took {0} ms", sw.ElapsedMilliseconds);
            return session;
        }

        /// <summary>
        /// Writes a benchmark log entry if the benchmark service is configured and enabled.
        /// </summary>
        private void LogBenchmark(IntPtr hWnd, int elementCount, long elapsedMs, bool cacheHit)
        {
            try
            {
                if (BenchmarkService == null || !BenchmarkService.IsEnabled)
                    return;

                var windowTitle = GetWindowTitle(hWnd);
                BenchmarkService.LogSession(new BenchmarkLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    WindowTitle = windowTitle,
                    ElementCount = elementCount,
                    ElapsedMs = (int)elapsedMs,
                    CacheHit = cacheHit,
                    FilterMode = FilterMode,
                });
            }
            catch
            {
                // Benchmark logging must never break the feature
            }
        }

        /// <summary>
        /// Gets the window title for the given handle, or empty string on failure.
        /// </summary>
        private static string GetWindowTitle(IntPtr hWnd)
        {
            try
            {
                const int nChars = 256;
                var sb = new System.Text.StringBuilder(nChars);
                if (User32.GetWindowText(hWnd, sb, nChars) > 0)
                    return sb.ToString();
            }
            catch
            {
            }
            return "";
        }

        /// <summary>
        /// Enumerates hints on a background thread so the UI stays responsive.
        /// Creates a fresh CUIAutomation instance on the worker thread to avoid
        /// COM apartment marshaling issues.
        /// </summary>
        public Task<HintSession> EnumHintsAsync(IntPtr hWnd, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                // Check cancellation before any COM work
                if (cancellationToken.IsCancellationRequested)
                    return null;

                // Create a fresh automation object on this background thread
                // so COM stays in the right apartment.
                var service = new UiAutomationHintProviderService(new CUIAutomation())
                {
                    BenchmarkService = this.BenchmarkService,
                    FilterMode = this.FilterMode,
                    _cachedSession = this._cachedSession,
                    _cachedHwnd = this._cachedHwnd,
                    _cachedFilterMode = this._cachedFilterMode,
                };
                return service.EnumHints(hWnd);
            }, cancellationToken);
        }

        /// <summary>
        /// Clears the cached enumeration result.
        /// </summary>
        public void InvalidateCache()
        {
            _cachedSession = null;
            _cachedHwnd = IntPtr.Zero;
            _cachedFilterMode = null;
        }

        public HintSession EnumDebugHints()
        {
            var foregroundWindow = User32.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return null;
            }
            return EnumDebugHints(foregroundWindow);
        }

        public HintSession EnumDebugHints(IntPtr hWnd)
        {
            return EnumWindowHints(hWnd, CreateDebugHint);
        }

        /// <summary>
        /// Enumerates all the hints from the given window
        /// </summary>
        /// <param name="hWnd">The window to get hints from</param>
        /// <param name="hintFactory">The factory to use to create each hint in the session</param>
        /// <returns>A hint session</returns>
        private HintSession EnumWindowHints(IntPtr hWnd, Func<IntPtr, Rect, IUIAutomationElement, Hint> hintFactory)
        {
            var result = new List<Hint>();
            var elements = EnumElements(hWnd);

            // Window bounds
            var rawWindowBounds = new RECT();
            User32.GetWindowRect(hWnd, ref rawWindowBounds);
            Rect windowBounds = rawWindowBounds;
            double windowArea = windowBounds.Width * windowBounds.Height;

            // ── Occlusion detection: find overlay candidates ─────
            // Scan elements for large containers (>33% of window area)
            // that may be modal overlays occluding content behind them.
            tagRECT? overlayBounds = null;
            for (int i = 0; i < elements.Count; i++)
            {
                var br = elements[i].CachedBoundingRectangle;
                double w = br.right - br.left;
                double h = br.bottom - br.top;
                if (w > 0 && h > 0 && (w * h) > windowArea * 0.33)
                {
                    overlayBounds = br;
                    break;
                }
            }

            // Fallback: do a lightweight second pass to find non-interactive
            // overlay backdrops (common in web modals). These are typically
            // Pane/Group elements with no interactive patterns, so they're
            // excluded by our main InvokeFiltered condition.
            if (!overlayBounds.HasValue)
            {
                var rootEl = _automation.ElementFromHandle(hWnd);
                if (rootEl != null)
                    overlayBounds = FindOverlayBackdrop(rootEl, windowArea);
            }

            foreach (var element in elements)
            {
                var boundingRectObject = element.CachedBoundingRectangle;
                if ((boundingRectObject.right > boundingRectObject.left) && (boundingRectObject.bottom > boundingRectObject.top))
                {
                    var niceRect = new Rect(new Point(boundingRectObject.left, boundingRectObject.top), new Point(boundingRectObject.right, boundingRectObject.bottom));
                    // Convert the bounding rect to logical coords
                    var logicalRect = niceRect.PhysicalToLogicalRect(hWnd);
                    if (!logicalRect.IsEmpty)
                    {
                        // Occlusion check: if an overlay exists and this element's
                        // center falls within it, verify it's not behind the overlay.
                        if (overlayBounds.HasValue && IsBehindOverlay(niceRect, overlayBounds.Value))
                        {
                            if (IsElementOccludedByPointCheck(element, niceRect))
                                continue;
                        }

                        var windowCoords = niceRect.ScreenToWindowCoordinates(windowBounds);
                        var hint = hintFactory(hWnd, windowCoords, element);
                        if (hint != null)
                        {
                            // Deduplication: skip hints whose bounding rectangle substantially
                            // overlaps an already-added hint (UIA trees can produce duplicate
                            // elements for the same visual control — e.g. parent + child both
                            // matching pattern-availability conditions).
                            if (IsDuplicate(result, windowCoords))
                                continue;

                            result.Add(hint);
                        }
                    }
                }
            }

            return new HintSession
            {
                Hints = result,
                OwningWindow = hWnd,
                OwningWindowBounds = windowBounds,
            };
        }

        /// <summary>
        /// Returns true if the element's center is within the overlay's bounding rect.
        /// </summary>
        private static bool IsBehindOverlay(Rect elementRect, tagRECT overlayRect)
        {
            double centerX = (elementRect.Left + elementRect.Right) / 2.0;
            double centerY = (elementRect.Top + elementRect.Bottom) / 2.0;
            return centerX >= overlayRect.left && centerX <= overlayRect.right
                && centerY >= overlayRect.top && centerY <= overlayRect.bottom;
        }

        /// <summary>
        /// Checks whether <paramref name="element"/> is visually occluded by calling
        /// UIA's ElementFromPoint at the element's center and comparing RuntimeIds.
        /// Returns true if the element is behind another element (occluded).
        /// </summary>
        private bool IsElementOccludedByPointCheck(IUIAutomationElement element, Rect elementRect)
        {
            try
            {
                double centerX = (elementRect.Left + elementRect.Right) / 2.0;
                double centerY = (elementRect.Top + elementRect.Bottom) / 2.0;
                var pt = new Interop.UIAutomationClient.tagPOINT { x = (int)centerX, y = (int)centerY };

                var topElement = _automation.ElementFromPoint(pt);
                if (topElement == null)
                    return false; // Can't determine — err on side of showing hint

                // Compare RuntimeIds: if they differ, our element is behind something else
                var ourId = GetCachedRuntimeId(element);
                var topId = GetCurrentRuntimeId(topElement);

                if (ourId == null || topId == null)
                    return false; // Can't compare — err on side of showing

                return !RuntimeIdsEqual(ourId, topId);
            }
            catch
            {
                // If anything fails, keep the element (safety first)
                return false;
            }
        }

        /// <summary>
        /// Reads the RuntimeId from a cached element property.
        /// </summary>
        private static int[] GetCachedRuntimeId(IUIAutomationElement element)
        {
            try
            {
                var val = element.GetCachedPropertyValue(UIA_PropertyIds.UIA_RuntimeIdPropertyId);
                // UIA RuntimeId is returned as an array of ints via COM
                if (val is Array arr)
                {
                    var result = new int[arr.Length];
                    for (int i = 0; i < arr.Length; i++)
                        result[i] = Convert.ToInt32(arr.GetValue(i), CultureInfo.InvariantCulture);
                    return result;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads the RuntimeId from a live (non-cached) element.
        /// ElementFromPoint returns a live element, so we need Current property access.
        /// </summary>
        private static int[] GetCurrentRuntimeId(IUIAutomationElement element)
        {
            try
            {
                var val = element.GetCurrentPropertyValue(UIA_PropertyIds.UIA_RuntimeIdPropertyId);
                if (val is Array arr)
                {
                    var result = new int[arr.Length];
                    for (int i = 0; i < arr.Length; i++)
                        result[i] = Convert.ToInt32(arr.GetValue(i), CultureInfo.InvariantCulture);
                    return result;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lightweight second-pass scan for non-interactive overlay backdrops.
        /// Web modals often use a full-viewport &lt;div&gt; as a backdrop that
        /// doesn't match any interactive pattern, so it's excluded from the
        /// main InvokeFiltered enumeration. This pass uses a minimal condition
        /// (ControlView + Enabled + OnScreen, no pattern filter) and caches
        /// only BoundingRectangle and ControlType for speed.
        /// </summary>
        private tagRECT? FindOverlayBackdrop(IUIAutomationElement rootElement, double windowArea)
        {
            try
            {
                // Minimal condition: enabled, on-screen control elements only.
                // No pattern filtering — we want ALL elements, including static
                // containers like modal backdrops.
                var cv = _automation.ControlViewCondition;
                var enabled = _automation.CreatePropertyCondition(
                    UIA_PropertyIds.UIA_IsEnabledPropertyId, true);
                var onScreen = _automation.CreatePropertyCondition(
                    UIA_PropertyIds.UIA_IsOffscreenPropertyId, false);
                var baseCond = _automation.CreateAndCondition(cv, enabled);
                var condition = _automation.CreateAndCondition(baseCond, onScreen);

                // Lightweight cache: only the properties we need for overlay detection
                var cache = _automation.CreateCacheRequest();
                cache.AddProperty(UIA_PropertyIds.UIA_BoundingRectanglePropertyId);
                cache.AddProperty(UIA_PropertyIds.UIA_ControlTypePropertyId);

                var results = rootElement.FindAllBuildCache(
                    TreeScope.TreeScope_Descendants, condition, cache);

                if (results == null) return null;

                for (int i = 0; i < results.Length; i++)
                {
                    var el = results.GetElement(i);
                    var br = el.CachedBoundingRectangle;
                    double w = br.right - br.left;
                    double h = br.bottom - br.top;
                    // Look for elements covering >50% of the window — these are
                    // likely modal backdrops or overlay containers.
                    if (w > 0 && h > 0 && (w * h) > windowArea * 0.5)
                    {
                        return br;
                    }
                }
            }
            catch
            {
                // If the second pass fails, fall through — we just skip
                // occlusion filtering for this window.
            }
            return null;
        }

        /// <summary>
        /// Compares two UIA RuntimeId arrays for equality.
        /// </summary>
        private static bool RuntimeIdsEqual(int[] a, int[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>
        /// Enumerates the automation elements from the given window
        /// </summary>
        /// <param name="hWnd">The window handle</param>
        /// <returns>All of the automation elements found</returns>
        private List<IUIAutomationElement> EnumElements(IntPtr hWnd)
        {
            var result = new List<IUIAutomationElement>();
            var automationElement = _automation.ElementFromHandle(hWnd);

            // Build condition tree: ControlView + Enabled + OnScreen
            var conditionControlView = _automation.ControlViewCondition;
            var conditionEnabled = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_IsEnabledPropertyId, true);
            var enabledControlCondition = _automation.CreateAndCondition(conditionControlView, conditionEnabled);

            var conditionOnScreen = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_IsOffscreenPropertyId, false);

            // T035/T036: Build the condition tree based on the current filter mode.
            // FilterMode is fixed at overlay-open time — it does not change when
            // alternate modifier keys are held during hint matching.
            IUIAutomationCondition condition;
            if (FilterMode == "AllElements")
            {
                // AllElements mode: no pattern-availability filtering.
                // Show all visible, enabled control elements (including static text).
                condition = _automation.CreateAndCondition(enabledControlCondition, conditionOnScreen);
            }
            else
            {
                // InvokeFiltered mode (default): only elements that support at least
                // one interactive UIA pattern.
                var invokeAvailable = _automation.CreatePropertyCondition(
                    UIA_PropertyIds.UIA_IsInvokePatternAvailablePropertyId, true);
                var toggleAvailable = _automation.CreatePropertyCondition(
                    UIA_PropertyIds.UIA_IsTogglePatternAvailablePropertyId, true);
                var selectionItemAvailable = _automation.CreatePropertyCondition(
                    UIA_PropertyIds.UIA_IsSelectionItemPatternAvailablePropertyId, true);
                var expandCollapseAvailable = _automation.CreatePropertyCondition(
                    UIA_PropertyIds.UIA_IsExpandCollapsePatternAvailablePropertyId, true);
                var valueAvailable = _automation.CreatePropertyCondition(
                    UIA_PropertyIds.UIA_IsValuePatternAvailablePropertyId, true);
                var rangeValueAvailable = _automation.CreatePropertyCondition(
                    UIA_PropertyIds.UIA_IsRangeValuePatternAvailablePropertyId, true);

                // Nest: OR(OR(OR(OR(OR(invoke, toggle), selectionItem), expandCollapse), value), rangeValue)
                var interactivePatternCondition = _automation.CreateOrCondition(invokeAvailable, toggleAvailable);
                interactivePatternCondition = _automation.CreateOrCondition(interactivePatternCondition, selectionItemAvailable);
                interactivePatternCondition = _automation.CreateOrCondition(interactivePatternCondition, expandCollapseAvailable);
                interactivePatternCondition = _automation.CreateOrCondition(interactivePatternCondition, valueAvailable);
                interactivePatternCondition = _automation.CreateOrCondition(interactivePatternCondition, rangeValueAvailable);

                // AND: (ControlView AND Enabled) AND (OnScreen AND has-interactive-pattern)
                var baseCondition = _automation.CreateAndCondition(enabledControlCondition, conditionOnScreen);
                condition = _automation.CreateAndCondition(baseCondition, interactivePatternCondition);
            }

            // Batch all the data we need into a single cross-process call. Without this,
            // each element's bounding rectangle and every pattern lookup is a separate
            // (slow) COM round-trip, which is what made enumeration take ~1s.
            var cacheRequest = _automation.CreateCacheRequest();
            cacheRequest.AddProperty(UIA_PropertyIds.UIA_BoundingRectanglePropertyId);
            cacheRequest.AddProperty(UIA_PropertyIds.UIA_ValueIsReadOnlyPropertyId);
            cacheRequest.AddProperty(UIA_PropertyIds.UIA_RangeValueIsReadOnlyPropertyId);
            // T008: Cache ControlType and IsControlElement for tree trimming
            cacheRequest.AddProperty(UIA_PropertyIds.UIA_ControlTypePropertyId);
            cacheRequest.AddProperty(UIA_PropertyIds.UIA_IsControlElementPropertyId);
            cacheRequest.AddProperty(UIA_PropertyIds.UIA_IsEnabledPropertyId);
            cacheRequest.AddProperty(UIA_PropertyIds.UIA_IsOffscreenPropertyId);
            cacheRequest.AddPattern(UIA_PatternIds.UIA_InvokePatternId);
            cacheRequest.AddPattern(UIA_PatternIds.UIA_TogglePatternId);
            cacheRequest.AddPattern(UIA_PatternIds.UIA_SelectionItemPatternId);
            cacheRequest.AddPattern(UIA_PatternIds.UIA_ExpandCollapsePatternId);
            cacheRequest.AddPattern(UIA_PatternIds.UIA_ValuePatternId);
            cacheRequest.AddPattern(UIA_PatternIds.UIA_RangeValuePatternId);
            // RuntimeId is needed for occlusion detection (ElementFromPoint comparison)
            cacheRequest.AddProperty(UIA_PropertyIds.UIA_RuntimeIdPropertyId);

            var elementArray = automationElement.FindAllBuildCache(TreeScope.TreeScope_Descendants, condition, cacheRequest);
            if (elementArray != null)
            {
                for (var i = 0; i < elementArray.Length; ++i)
                {
                    var element = elementArray.GetElement(i);

                    // T008: Tree trimming — conservatively skip elements that are
                    // definitively non-interactive based on cached properties.
                    if (!IsElementPotentiallyInteractive(element))
                        continue;

                    result.Add(element);
                }
            }

            return result;
        }

        /// <summary>
        /// T008: Conservative check — returns false only when an element is
        /// definitively non-interactive (not a control element, not enabled,
        /// or off-screen). We err on the side of keeping elements rather than
        /// dropping something the user might need.
        /// </summary>
        private static bool IsElementPotentiallyInteractive(IUIAutomationElement element)
        {
            try
            {
                // If it's not a control element and not enabled, skip
                var isControl = element.GetCachedPropertyValue(UIA_PropertyIds.UIA_IsControlElementPropertyId);
                if (isControl is bool isControlBool && !isControlBool)
                    return false;

                var isEnabled = element.GetCachedPropertyValue(UIA_PropertyIds.UIA_IsEnabledPropertyId);
                if (isEnabled is bool isEnabledBool && !isEnabledBool)
                    return false;

                var isOffscreen = element.GetCachedPropertyValue(UIA_PropertyIds.UIA_IsOffscreenPropertyId);
                if (isOffscreen is bool isOffscreenBool && isOffscreenBool)
                    return false;

                return true;
            }
            catch
            {
                // If we can't read properties, keep the element (safety first)
                return true;
            }
        }

        /// <summary>
        /// Creates a UI Automation element from the given automation element
        /// </summary>
        /// <param name="owningWindow">The owning window</param>
        /// <param name="hintBounds">The hint bounds</param>
        /// <param name="automationElement">The associated automation element</param>
        /// <returns>The created hint, else null if the hint could not be created</returns>
        private Hint CreateHint(IntPtr owningWindow, Rect hintBounds, IUIAutomationElement automationElement)
        {
            try
            {
                // Read patterns from the cache (populated by FindAllBuildCache) to avoid
                // a cross-process COM call per pattern per element.
                var invokePattern = (IUIAutomationInvokePattern)automationElement.GetCachedPattern(UIA_PatternIds.UIA_InvokePatternId);
                if (invokePattern != null)
                {
                    return new UiAutomationInvokeHint(owningWindow, invokePattern, hintBounds);
                }

                var togglePattern = (IUIAutomationTogglePattern)automationElement.GetCachedPattern(UIA_PatternIds.UIA_TogglePatternId);
                if (togglePattern != null)
                {
                    return new UiAutomationToggleHint(owningWindow, togglePattern, hintBounds);
                }

                var selectPattern = (IUIAutomationSelectionItemPattern)automationElement.GetCachedPattern(UIA_PatternIds.UIA_SelectionItemPatternId);
                if (selectPattern != null)
                {
                    return new UiAutomationSelectHint(owningWindow, selectPattern, hintBounds);
                }

                var expandCollapsePattern = (IUIAutomationExpandCollapsePattern)automationElement.GetCachedPattern(UIA_PatternIds.UIA_ExpandCollapsePatternId);
                if (expandCollapsePattern != null)
                {
                    return new UiAutomationExpandCollapseHint(owningWindow, expandCollapsePattern, hintBounds);
                }

                var valuePattern = (IUIAutomationValuePattern)automationElement.GetCachedPattern(UIA_PatternIds.UIA_ValuePatternId);
                if (valuePattern != null && !IsCachedReadOnly(automationElement, UIA_PropertyIds.UIA_ValueIsReadOnlyPropertyId))
                {
                    return new UiAutomationFocusHint(owningWindow, automationElement, hintBounds);
                }

                var rangeValuePattern = (IUIAutomationRangeValuePattern)automationElement.GetCachedPattern(UIA_PatternIds.UIA_RangeValuePatternId);
                if (rangeValuePattern != null && !IsCachedReadOnly(automationElement, UIA_PropertyIds.UIA_RangeValueIsReadOnlyPropertyId))
                {
                    return new UiAutomationFocusHint(owningWindow, automationElement, hintBounds);
                }

                return null;
            }
            catch (Exception)
            {
                // May have gone
                return null;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="candidate"/>'s bounding rect overlaps more than
        /// 80% with any already-added hint — i.e. it's likely a duplicate UIA element
        /// for the same visual control (common in complex UIA trees like YouTube Music).
        /// </summary>
        /// <summary>
        /// Returns true if <paramref name="candidate"/> and an existing hint have
        /// nearly-identical bounding rectangles (within a 4px tolerance on all four
        /// edges). This catches genuine UIA duplicates (parent + child element both
        /// matching) without filtering distinct adjacent links whose rects may
        /// overlap due to multi-line text wrapping.
        /// </summary>
        private static bool IsDuplicate(List<Hint> existing, Rect candidate)
        {
            const double tolerance = 4.0;

            for (int i = 0; i < existing.Count; i++)
            {
                var r = existing[i].BoundingRectangle;

                if (Math.Abs(candidate.Left - r.Left) < tolerance
                    && Math.Abs(candidate.Top - r.Top) < tolerance
                    && Math.Abs(candidate.Right - r.Right) < tolerance
                    && Math.Abs(candidate.Bottom - r.Bottom) < tolerance)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Reads a cached "is read only" boolean property, defaulting to read only (true)
        /// if the value is missing so we don't offer a focus hint we can't use.
        /// </summary>
        private static bool IsCachedReadOnly(IUIAutomationElement element, int propertyId)
        {
            try
            {
                var value = element.GetCachedPropertyValue(propertyId);
                if (value is bool b)
                {
                    return b;
                }
                if (value is int i)
                {
                    return i != 0;
                }
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        /// Creates a debug hint
        /// </summary>
        /// <param name="owningWindow">The window that owns the hint</param>
        /// <param name="hintBounds">The hint bounds</param>
        /// <param name="automationElement">The automation element</param>
        /// <returns>A debug hint</returns>
        private DebugHint CreateDebugHint(IntPtr owningWindow, Rect hintBounds, IUIAutomationElement automationElement)
        {
            // Enumerate all possible patterns. Note that the performance of this is *very* bad -- hence debug only.
            var programmaticNames = new List<string>();

            foreach (var pn in UiAutomationPatternIds.PatternNames)
            {
                try
                {
                    var pattern = automationElement.GetCurrentPattern(pn.Key);
                    if (pattern != null)
                    {
                        programmaticNames.Add(pn.Value);
                    }
                }
                catch (Exception)
                {
                    // Pattern not supported by this element — expected; skip it.
                    continue;
                }
            }

            if (programmaticNames.Count > 0)
            {
                return new DebugHint(owningWindow, hintBounds, programmaticNames.ToList());
            }

            return null;
        }
    }
}
