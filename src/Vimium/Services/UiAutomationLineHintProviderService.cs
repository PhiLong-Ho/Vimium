using Vimium.Extensions;
using Vimium.Models;
using Vimium.NativeMethods;
using Vimium.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Interop.UIAutomationClient;

namespace Vimium.Services;

internal class UiAutomationLineHintProviderService : ILineHintProviderService
{
    private readonly IUIAutomation _automation;

    public UiAutomationLineHintProviderService()
    {
        _automation = new CUIAutomation();
    }

    /// <summary>
    /// Private constructor used by <see cref="EnumLineHintsAsync"/> to create a
    /// service whose COM object lives on the background thread.
    /// </summary>
    private UiAutomationLineHintProviderService(IUIAutomation automation)
    {
        _automation = automation;
    }

    public LineNavigationSession EnumLineHints()
    {
        var foregroundWindow = User32.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return null;
        }
        return EnumLineHints(foregroundWindow);
    }

    public LineNavigationSession EnumLineHints(IntPtr hWnd)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var session = EnumWindowLineHints(hWnd);
        sw.Stop();

        Debug.WriteLine("Enumeration of text-line hints took {0} ms", sw.ElapsedMilliseconds);
        return session;
    }

    /// <summary>
    /// Enumerates line hints on a background thread so the UI stays responsive.
    /// Creates a fresh CUIAutomation instance on the worker thread to avoid
    /// COM apartment marshaling issues. Has a 10-second timeout to prevent
    /// hanging on unresponsive UIA providers.
    /// </summary>
    public async Task<LineNavigationSession> EnumLineHintsAsync(IntPtr hWnd)
    {
        LogService.Info($"UIA-Line: EnumLineHintsAsync starting, hWnd=0x{hWnd:X}");
        try
        {
            var task = Task.Run(() =>
            {
                var service = new UiAutomationLineHintProviderService(new CUIAutomation());
                return service.EnumLineHints(hWnd);
            });

            var timeout = Task.Delay(TimeSpan.FromSeconds(10));
            var completed = await Task.WhenAny(task, timeout);

            if (completed == task)
            {
                var session = await task;
                LogService.Info($"UIA-Line: EnumLineHintsAsync completed, {session?.Hints?.Count ?? 0} hints");
                return session;
            }

            LogService.Warn("UIA-Line: EnumLineHintsAsync TIMED OUT after 10s");
            return CreateEmptySession(hWnd, GetWindowBounds(hWnd));
        }
        catch (Exception ex)
        {
            LogService.Error("UIA-Line: EnumLineHintsAsync failed", ex);
            return CreateEmptySession(hWnd, GetWindowBounds(hWnd));
        }
    }

    private static Rect GetWindowBounds(IntPtr hWnd)
    {
        var rawWindowBounds = new RECT();
        User32.GetWindowRect(hWnd, ref rawWindowBounds);
        return rawWindowBounds;
    }

    private LineNavigationSession EnumWindowLineHints(IntPtr hWnd)
    {
        LogService.Info($"UIA-Line: EnumWindowLineHints start, hWnd=0x{hWnd:X}");

        // Window bounds
        var rawWindowBounds = new RECT();
        User32.GetWindowRect(hWnd, ref rawWindowBounds);
        Rect windowBounds = rawWindowBounds;

        try
        {
            var automationElement = _automation.ElementFromHandle(hWnd);
            if (automationElement == null)
            {
                LogService.Warn("UIA-Line: ElementFromHandle returned null");
                return CreateEmptySession(hWnd, windowBounds);
            }

            // ── Layer 1: GetVisibleRanges (fast path for WPF / well-behaved Win32) ──
            var textPattern = GetTextPattern(automationElement);
            if (textPattern == null)
                FindBestTextPatternDescendant(automationElement, out textPattern);

            if (textPattern != null)
            {
                LogService.Info("UIA-Line: TextPattern acquired");
                var visibleRanges = GetVisibleRangesSafe(textPattern);
                if (visibleRanges != null && visibleRanges.Length > 0)
                {
                    LogService.Info($"UIA-Line: Layer1 GetVisibleRanges → {visibleRanges.Length} ranges");
                    var result = new List<TextLineHint>();
                    CollectHintsFromRanges(visibleRanges, hWnd, windowBounds, result);
                    LogService.Info($"UIA-Line: Layer1 complete, {result.Count} hints");
                    return new LineNavigationSession { Hints = result, OwningWindow = hWnd, OwningWindowBounds = windowBounds };
                }
                LogService.Warn("UIA-Line: Layer1 GetVisibleRanges returned 0 ranges");
            }
            else
            {
                LogService.Warn("UIA-Line: No TextPattern available");
            }

            // ── Layer 2: Element-tree text discovery (robust — works for browsers, Electron, etc.) ──
            var treeHints = CollectHintsFromElementTree(automationElement, hWnd, windowBounds);
            if (treeHints != null && treeHints.Count > 0)
            {
                LogService.Info($"UIA-Line: Layer2 ElementTree → {treeHints.Count} hints");
                return new LineNavigationSession { Hints = treeHints, OwningWindow = hWnd, OwningWindowBounds = windowBounds };
            }

            // ── Layer 3: ValuePattern fallback (Windows 11 Notepad, legacy apps) ──
            var valueHints = CollectHintsFromValuePattern(automationElement, hWnd, windowBounds);
            if (valueHints != null && valueHints.Count > 0)
            {
                LogService.Info($"UIA-Line: Layer3 ValuePattern → {valueHints.Count} hints");
                return new LineNavigationSession { Hints = valueHints, OwningWindow = hWnd, OwningWindowBounds = windowBounds };
            }

            LogService.Warn("UIA-Line: All layers returned 0 hints");
        }
        catch (Exception ex)
        {
            LogService.Error("UIA-Line: exception during enumeration", ex);
        }

        return CreateEmptySession(hWnd, windowBounds);
    }

    // ── Text range helpers ────────────────────────────────────

    /// <summary>Safely call GetVisibleRanges, returning null on any error.</summary>
    private static IUIAutomationTextRangeArray GetVisibleRangesSafe(IUIAutomationTextPattern textPattern)
    {
        try
        {
            return textPattern.GetVisibleRanges();
        }
        catch (Exception ex)
        {
            LogService.Warn($"UIA-Line: GetVisibleRanges threw: {ex.GetType().Name}");
            return null;
        }
    }

    /// <summary>Collect hints from a GetVisibleRanges result.</summary>
    private static void CollectHintsFromRanges(
        IUIAutomationTextRangeArray ranges, IntPtr hWnd, Rect windowBounds,
        List<TextLineHint> result)
    {
        for (int i = 0; i < ranges.Length; i++)
        {
            try
            {
                var range = ranges.GetElement(i);
                string textContent = range.GetText(-1) ?? string.Empty;

                var boundingRects = range.GetBoundingRectangles();
                if (boundingRects == null || boundingRects.Length < 4)
                    continue;

                double left = (double)boundingRects.GetValue(0);
                double top = (double)boundingRects.GetValue(1);
                double width = (double)boundingRects.GetValue(2);
                double height = (double)boundingRects.GetValue(3);

                if (width <= 0 || height <= 0)
                    continue;

                var hint = CreateHintFromScreenRect(hWnd, windowBounds, left, top, width, height, textContent);
                if (hint != null)
                    result.Add(hint);
            }
            catch { }
        }
    }

    /// <summary>
    /// Layer 2: Walk the UIA element tree looking for text-bearing elements.
    /// Uses <see cref="IUIAutomationElement.FindAll"/> with ControlView, ContentView,
    /// and RawView conditions — the same proven pattern as the element hint provider.
    /// Each element's <see cref="IUIAutomationElement.CurrentBoundingRectangle"/> is
    /// universally supported, making this approach robust across browsers, Electron
    /// apps, and Win32 controls. Deduplicates by vertical overlap so that multiple
    /// inline elements on the same visual line produce a single hint.
    /// </summary>
    private List<TextLineHint> CollectHintsFromElementTree(
        IUIAutomationElement root, IntPtr hWnd, Rect windowBounds)
    {
        var raw = new List<TextLineHint>();
        int totalScanned = 0;
        int maxPerView = 500;

        try
        {
            // Strategy A: ControlView — interactive controls with text (buttons, links, labels)
            totalScanned += ScanElementView(root, _automation.ControlViewCondition,
                "ControlView", hWnd, windowBounds, raw, maxPerView);

            // Strategy B: ContentView — content elements (paragraphs, headings, list items)
            if (raw.Count < 50)
            {
                totalScanned += ScanElementView(root, _automation.ContentViewCondition,
                    "ContentView", hWnd, windowBounds, raw, maxPerView);
            }

            // Strategy C: RawView — everything (last resort for text nodes in browsers)
            if (raw.Count < 20)
            {
                totalScanned += ScanElementView(root, _automation.RawViewCondition,
                    "RawView", hWnd, windowBounds, raw, maxPerView);
            }
        }
        catch (Exception ex)
        {
            LogService.Error("UIA-Line: CollectHintsFromElementTree failed", ex);
        }

        LogService.Info($"UIA-Line: ElementTree raw={raw.Count} hints (scanned {totalScanned} elements)");

        // Deduplicate hints that overlap vertically (inline elements on same line)
        var deduped = DeduplicateHints(raw);
        LogService.Info($"UIA-Line: ElementTree after dedup → {deduped.Count} hints");

        return deduped;
    }

    /// <summary>
    /// Scans descendant elements under <paramref name="root"/> matching the given
    /// <paramref name="condition"/>, extracting text from each element's
    /// <see cref="IUIAutomationElement.CurrentName"/> or ValuePattern.
    /// </summary>
    private static int ScanElementView(
        IUIAutomationElement root, IUIAutomationCondition condition, string viewName,
        IntPtr hWnd, Rect windowBounds, List<TextLineHint> results, int maxElements)
    {
        try
        {
            var descendants = root.FindAll(TreeScope.TreeScope_Descendants, condition);
            if (descendants == null || descendants.Length == 0)
                return 0;

            int limit = Math.Min(descendants.Length, maxElements);
            int added = 0;

            for (int i = 0; i < limit; i++)
            {
                var el = descendants.GetElement(i);
                var hint = TryCreateHintFromElement(el, hWnd, windowBounds);
                if (hint != null)
                {
                    results.Add(hint);
                    added++;
                }
            }

            if (added > 0)
                LogService.Info($"UIA-Line: {viewName} → {added} text-bearing elements in {limit}/{descendants.Length} scanned");

            return limit;
        }
        catch (Exception ex)
        {
            LogService.Warn($"UIA-Line: {viewName} scan: {ex.GetType().Name}");
            return 0;
        }
    }

    /// <summary>
    /// Tries to create a <see cref="TextLineHint"/> from a single UIA element.
    /// Checks for text content via <see cref="IUIAutomationElement.CurrentName"/>
    /// (primary — works for labels, buttons, paragraphs, headings) and
    /// <see cref="IUIAutomationValuePattern.CurrentValue"/> (secondary — edit controls).
    /// Filters out elements that are too small or off-screen.
    /// </summary>
    private static TextLineHint TryCreateHintFromElement(
        IUIAutomationElement element, IntPtr hWnd, Rect windowBounds)
    {
        try
        {
            // Get text content — try Name first (most universal)
            string text = (element.CurrentName ?? "").Trim();

            // If Name is empty or very short, try ValuePattern (edit controls, text boxes)
            if (text.Length < 2)
            {
                try
                {
                    var vp = element.GetCurrentPattern(UIA_PatternIds.UIA_ValuePatternId)
                        as IUIAutomationValuePattern;
                    if (vp != null)
                    {
                        string value = vp.CurrentValue?.Trim() ?? "";
                        if (value.Length > text.Length)
                            text = value;
                    }
                }
                catch { }
            }

            // Minimum text length to be a meaningful hint target
            if (text.Length < 2)
                return null;

            // Skip elements whose text looks like a URL (address bar noise)
            int controlType = element.CurrentControlType;
            if (controlType == 50004 && (text.StartsWith("http://") || text.StartsWith("https://")))
                return null; // Skip address bar text — likely not user content

            // Get bounding rectangle (universally supported)
            var bounds = element.CurrentBoundingRectangle;
            double width = bounds.right - bounds.left;
            double height = bounds.bottom - bounds.top;

            // Filter out tiny or invisible elements
            if (width < 20 || height < 8)
                return null;

            // Filter out elements positioned off-screen (above viewport significantly)
            if (bounds.top < -500 || bounds.left < -500)
                return null;

            return CreateHintFromScreenRect(hWnd, windowBounds,
                bounds.left, bounds.top, width, height, text);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deduplicates hints whose bounding rectangles overlap vertically by ≥60%.
    /// When multiple UIA elements sit on the same visual line (e.g., a
    /// &lt;span&gt; inside a &lt;p&gt;), only the largest hint is kept.
    /// Hints are sorted top-to-bottom, left-to-right.
    /// </summary>
    private static List<TextLineHint> DeduplicateHints(List<TextLineHint> hints)
    {
        if (hints.Count <= 1)
            return hints;

        // Sort by Y (top-to-bottom), then X (left-to-right)
        hints.Sort((a, b) =>
        {
            int yCompare = a.BoundingRectangle.Top.CompareTo(b.BoundingRectangle.Top);
            if (yCompare != 0) return yCompare;
            return a.BoundingRectangle.Left.CompareTo(b.BoundingRectangle.Left);
        });

        var result = new List<TextLineHint>(hints.Count);
        result.Add(hints[0]);

        for (int i = 1; i < hints.Count; i++)
        {
            var prev = result[result.Count - 1];
            var curr = hints[i];

            double prevTop = prev.BoundingRectangle.Top;
            double prevBottom = prev.BoundingRectangle.Bottom;
            double prevHeight = prev.BoundingRectangle.Height;

            double currTop = curr.BoundingRectangle.Top;
            double currBottom = curr.BoundingRectangle.Bottom;

            // Compute vertical overlap ratio
            double overlapTop = Math.Max(prevTop, currTop);
            double overlapBottom = Math.Min(prevBottom, currBottom);
            double overlap = overlapBottom - overlapTop;

            if (overlap > 0)
            {
                double minHeight = Math.Min(prevHeight, curr.BoundingRectangle.Height);
                double overlapRatio = overlap / Math.Max(minHeight, 1);

                // If ≥60% vertical overlap, merge: keep the one with more text
                if (overlapRatio >= 0.6)
                {
                    if (curr.TextContent.Length > prev.TextContent.Length)
                        result[result.Count - 1] = curr;
                    continue;
                }
            }

            result.Add(curr);
        }

        return result;
    }

    /// <summary>Create a TextLineHint from screen-coordinate rectangle values.</summary>
    private static TextLineHint CreateHintFromScreenRect(
        IntPtr hWnd, Rect windowBounds,
        double left, double top, double width, double height,
        string textContent)
    {
        var screenRect = new Rect(left, top, width, height);

        // Convert to logical coordinates
        var logicalRect = screenRect.PhysicalToLogicalRect(hWnd);
        if (logicalRect.IsEmpty)
            return null;

        var windowCoords = logicalRect.ScreenToWindowCoordinates(windowBounds);
        return new TextLineHint(hWnd, windowCoords, textContent);
    }

    /// <summary>
    /// Fallback for apps that don't support TextPattern (e.g., Windows 11 Notepad).
    /// Scans control-view descendants for ValuePattern with multi-line text, then
    /// estimates per-line positions by dividing the element's bounding rect.
    /// </summary>
    private static List<TextLineHint> CollectHintsFromValuePattern(
        IUIAutomationElement root, IntPtr hWnd, Rect windowBounds)
    {
        try
        {
            var auto = new CUIAutomation();
            var descendants = root.FindAll(
                TreeScope.TreeScope_Descendants, auto.ControlViewCondition);
            if (descendants == null || descendants.Length == 0) return null;

            int limit = Math.Min(descendants.Length, 100);
            LogService.Info($"UIA-Line: ValuePattern scan ({limit}/{descendants.Length} elements)...");

            for (int i = 0; i < limit; i++)
            {
                var el = descendants.GetElement(i);
                try
                {
                    var pattern = el.GetCurrentPattern(UIA_PatternIds.UIA_ValuePatternId);
                    if (pattern is IUIAutomationValuePattern valuePattern)
                    {
                        string text = valuePattern.CurrentValue;
                        if (!string.IsNullOrEmpty(text) && text.Contains("\n"))
                        {
                            var bounds = el.CurrentBoundingRectangle;
                            double elementWidth = bounds.right - bounds.left;
                            double elementHeight = bounds.bottom - bounds.top;

                            if (elementWidth > 50 && elementHeight > 50)
                            {
                                var result = new List<TextLineHint>();
                                string[] lines = text.Split('\n');
                                double lineHeight = Math.Max(elementHeight / Math.Max(lines.Length, 1), 14);

                                for (int li = 0; li < lines.Length; li++)
                                {
                                    string lineText = lines[li].TrimEnd('\r');
                                    if (string.IsNullOrEmpty(lineText)) continue;

                                    double yPos = bounds.top + (li * lineHeight);
                                    var hint = CreateHintFromScreenRect(
                                        hWnd, windowBounds,
                                        bounds.left, yPos,
                                        elementWidth, lineHeight,
                                        lineText);
                                    if (hint != null)
                                        result.Add(hint);
                                }

                                if (result.Count > 0)
                                {
                                    LogService.Info($"UIA-Line: ValuePattern[{i}]: {result.Count} lines from \"{text.Substring(0, Math.Min(text.Length, 50))}...\"");
                                    return result;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            LogService.Warn("UIA-Line: No multi-line ValuePattern element found");
        }
        catch (Exception ex)
        {
            LogService.Error("UIA-Line: ValuePattern fallback failed", ex);
        }

        return null;
    }

    // ── TextPattern discovery ─────────────────────────────────

    /// <summary>
    /// Attempts to get a TextPattern from the given automation element.
    /// Tries TextPattern2 first, then falls back to TextPattern.
    /// </summary>
    private static IUIAutomationTextPattern GetTextPattern(IUIAutomationElement element)
    {
        try
        {
            // Try TextPattern2 first (ID = 10024)
            var pattern = element.GetCurrentPattern(UIA_PatternIds.UIA_TextPattern2Id);
            if (pattern is IUIAutomationTextPattern textPattern2)
            {
                LogService.Info("UIA-Line: Got TextPattern2");
                return textPattern2;
            }

            // Fall back to TextPattern (ID = 10014)
            pattern = element.GetCurrentPattern(UIA_PatternIds.UIA_TextPatternId);
            if (pattern is IUIAutomationTextPattern textPattern)
            {
                LogService.Info("UIA-Line: Got TextPattern");
                return textPattern;
            }

            return null;
        }
        catch (Exception ex)
        {
            LogService.Error("UIA-Line: GetTextPattern failed", ex);
            return null;
        }
    }

    /// <summary>
    /// Searches descendants for the BEST element supporting TextPattern, not just
    /// the first one. Browsers have many TextPattern-enabled elements (URL bar,
    /// search box, document body). We want the main text content area.
    /// Selection priority: Document > non-Edit > Edit (by text volume).
    /// </summary>
    private IUIAutomationElement FindBestTextPatternDescendant(
        IUIAutomationElement root, out IUIAutomationTextPattern bestPattern)
    {
        bestPattern = null;
        IUIAutomationElement bestElement = null;
        int bestScore = -1;
        string bestReason = "";

        try
        {
            // Collect candidates from all three views
            var candidates = new List<(IUIAutomationElement element, IUIAutomationTextPattern pattern)>();

            CollectTextPatternCandidates(root, _automation.ControlViewCondition, "ControlView", candidates);
            if (candidates.Count == 0)
                CollectTextPatternCandidates(root, _automation.ContentViewCondition, "ContentView", candidates);
            if (candidates.Count == 0)
                CollectTextPatternCandidates(root, _automation.RawViewCondition, "RawView", candidates);

            if (candidates.Count == 0)
            {
                LogService.Warn("UIA-Line: No TextPattern candidates found in any view");
                return null;
            }

            LogService.Info($"UIA-Line: Found {candidates.Count} TextPattern candidates, selecting best...");

            foreach (var (element, pattern) in candidates)
            {
                int score = 0;
                string reason = "";

                try
                {
                    int controlType = element.CurrentControlType;
                    string className = (element.CurrentClassName ?? "").ToLowerInvariant();

                    // Heavily prefer Document elements (page body in browsers)
                    if (controlType == 50030) // UIA_DocumentControlTypeId
                    {
                        score += 1000;
                        reason = "Document";
                    }

                    // De-prioritize obvious input fields
                    if (controlType == 50004 || // Edit
                        className.Contains("urlbar") ||
                        className.Contains("search") ||
                        className.Contains("find"))
                    {
                        score -= 500;
                        reason = reason.Length > 0 ? reason + ",input" : "input";
                    }

                    // Prefer elements with large bounding rectangles
                    try
                    {
                        var bounds = element.CurrentBoundingRectangle;
                        double area = (bounds.right - bounds.left) * (bounds.bottom - bounds.top);
                        score += (int)(area / 10000); // scale down
                        if (area > 100000) reason += ",large";
                    }
                    catch { }

                    // NOTE: We intentionally do NOT call GetVisibleRanges() or
                    // DocumentRange.GetText() here for scoring. Both are heavy
                    // cross-process COM calls that can take 500ms+ each on large
                    // documents (VS Code, browsers). For 17+ candidates this adds
                    // up to 10+ seconds and triggers the async timeout. Scoring
                    // by ControlType + ClassName + bounding-rect area is fast
                    // (<1ms per candidate) and sufficient for correct selection.
                }
                catch { }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestElement = element;
                    bestPattern = pattern;
                    bestReason = reason;
                }
            }

            if (bestElement != null)
            {
                try
                {
                    int ct = bestElement.CurrentControlType;
                    string cn = bestElement.CurrentClassName ?? "(no class)";
                    LogService.Info($"UIA-Line: Selected best candidate: CtrlType={ct}, Class=\"{cn}\", Score={bestScore} ({bestReason})");
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            LogService.Error("UIA-Line: FindBestTextPatternDescendant failed", ex);
        }

        return bestElement;
    }

    /// <summary>Collect all TextPattern-capable elements from a view (up to limit).</summary>
    private static void CollectTextPatternCandidates(
        IUIAutomationElement root, IUIAutomationCondition condition, string viewName,
        List<(IUIAutomationElement, IUIAutomationTextPattern)> candidates)
    {
        try
        {
            var descendants = root.FindAll(TreeScope.TreeScope_Descendants, condition);
            if (descendants == null) return;

            int total = descendants.Length;
            int limit = Math.Min(total, 300);

            for (int i = 0; i < limit; i++)
            {
                var el = descendants.GetElement(i);
                var tp = GetTextPattern(el);
                if (tp != null)
                    candidates.Add((el, tp));
            }

            if (candidates.Count > 0)
                LogService.Info($"UIA-Line: {viewName}: {candidates.Count} TextPattern candidates in {limit}/{total} elements");
        }
        catch (Exception ex)
        {
            LogService.Warn($"UIA-Line: {viewName} candidate scan: {ex.GetType().Name}");
        }
    }

    private static LineNavigationSession CreateEmptySession(IntPtr hWnd, Rect windowBounds)
    {
        return new LineNavigationSession
        {
            Hints = new List<TextLineHint>(),
            OwningWindow = hWnd,
            OwningWindowBounds = windowBounds,
        };
    }
}
