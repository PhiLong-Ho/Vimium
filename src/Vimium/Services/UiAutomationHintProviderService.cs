using Vimium.Extensions;
using Vimium.Models;
using Vimium.NativeMethods;
using Vimium.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Interop.UIAutomationClient;

namespace Vimium.Services
{
    internal class UiAutomationHintProviderService : IHintProviderService
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

            // Check cache before enumerating.
            // Cache key includes hWnd AND filter mode — a mode change
            // (e.g. after code update) must invalidate stale entries.
            bool cacheHit = false;
            if (_cachedSession != null
                && _cachedHwnd == hWnd
                && _cachedFilterMode == FilterMode)
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
        /// Enumerates the automation elements from the given window
        /// </summary>
        /// <param name="hWnd">The window handle</param>
        /// <returns>All of the automation elements found</returns>
        private List<IUIAutomationElement> EnumElements(IntPtr hWnd)
        {
            var result = new List<IUIAutomationElement>();
            var automationElement = _automation.ElementFromHandle(hWnd);

            // Build condition tree: ControlView + Enabled + OnScreen.
            // We intentionally do NOT filter by pattern-availability properties here
            // because custom UIA providers (VS Code, Electron apps, etc.) often
            // advertise pattern availability unreliably at the condition level.
            // Instead we use a broad condition (matching HuntAndPeck's approach) and
            // post-filter by actually retrieving cached patterns — which is both
            // accurate and fast since the cache request batches all data into a
            // single cross-process COM call.
            var conditionControlView = _automation.ControlViewCondition;
            var conditionEnabled = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_IsEnabledPropertyId, true);
            var enabledControlCondition = _automation.CreateAndCondition(conditionControlView, conditionEnabled);
            var conditionOnScreen = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_IsOffscreenPropertyId, false);
            var condition = _automation.CreateAndCondition(enabledControlCondition, conditionOnScreen);

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
            // LegacyIAccessiblePattern covers many custom controls and menus
            // (VS Code, Electron apps) that don't expose modern UIA patterns
            // but do support DoDefaultAction through MSAA compatibility.
            cacheRequest.AddPattern(UIA_PatternIds.UIA_LegacyIAccessiblePatternId);

            var elementArray = automationElement.FindAllBuildCache(TreeScope.TreeScope_Descendants, condition, cacheRequest);
            if (elementArray != null)
            {
                for (var i = 0; i < elementArray.Length; ++i)
                {
                    var element = elementArray.GetElement(i);

                    // No client-side tree trimming — the UIA condition
                    // (ControlView + Enabled + OnScreen) already guarantees
                    // these properties. Post-filtering happens in CreateHint
                    // via cached pattern retrieval, which is both more
                    // accurate and avoids false negatives from custom providers.

                    result.Add(element);
                }
            }

            return result;
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

                // LegacyIAccessiblePattern (MSAA): many custom controls, menus in VS Code,
                // and Electron apps expose interactivity solely through this compatibility
                // pattern via DoDefaultAction(). Check this last as it's the broadest match.
                var legacyPattern = (IUIAutomationLegacyIAccessiblePattern)automationElement.GetCachedPattern(UIA_PatternIds.UIA_LegacyIAccessiblePatternId);
                if (legacyPattern != null)
                {
                    // Only use LegacyIAccessible if the element has a default action
                    try
                    {
                        var defaultAction = legacyPattern.CurrentDefaultAction;
                        if (!string.IsNullOrEmpty(defaultAction))
                            return new UiAutomationLegacyIAccessibleHint(owningWindow, legacyPattern, hintBounds);
                    }
                    catch
                    {
                        // If we can't read DefaultAction, skip — element likely
                        // not meaningfully interactable via LegacyIAccessible.
                    }
                }

                // Menu fallback: menu items, menus, and menu bars are inherently
                // clickable even when they don't advertise any UIA pattern.
                // Use Win32 mouse click via UiAutomationClickHint.
                if (IsMenuControlType(automationElement))
                    return new UiAutomationClickHint(owningWindow, hintBounds);

                return null;
            }
            catch (Exception)
            {
                // May have gone
                return null;
            }
        }

        /// <summary>
        /// Returns true if the element's ControlType is a menu-related type
        /// that should always be hinted regardless of pattern support.
        /// </summary>
        private static bool IsMenuControlType(IUIAutomationElement element)
        {
            try
            {
                var ct = element.GetCachedPropertyValue(UIA_PropertyIds.UIA_ControlTypePropertyId);
                if (ct is int controlTypeId)
                {
                    return controlTypeId == UIA_ControlTypeIds.UIA_MenuControlTypeId
                        || controlTypeId == UIA_ControlTypeIds.UIA_MenuItemControlTypeId
                        || controlTypeId == UIA_ControlTypeIds.UIA_MenuBarControlTypeId;
                }
            }
            catch
            {
                // Can't read ControlType — skip
            }
            return false;
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

    }
}
