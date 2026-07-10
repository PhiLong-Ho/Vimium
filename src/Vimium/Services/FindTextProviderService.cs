using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Interop.UIAutomationClient;
using Vimium.Models;
using Vimium.NativeMethods;
using Vimium.Services.Interfaces;

namespace Vimium.Services;

/// <summary>
/// Query-driven text search over the foreground window's visible viewport.
/// Primary path: UIA TextPattern.GetVisibleRanges() → FindText() loop.
/// Fallback path: FindAllBuildCache element-name search.
/// All UIA work runs off the UI thread via Task.Run.
/// </summary>
internal class FindTextProviderService : IFindTextProviderService
{
    private const int TextPatternId = 10014;      // UIA_TextPatternId
    private const int ValuePatternId = 10002;      // UIA_ValuePatternId
    private const int IsTextPatternAvailablePropertyId = 30028; // UIA_IsTextPatternAvailablePropertyId
    private const int IsValuePatternAvailablePropertyId = 30012; // UIA_IsValuePatternAvailablePropertyId

    private const int MaxMatches = 200;
    private const int TimeoutMs = 3000;
    private const int MaxTextProviders = 60;
    private const int MaxElementScan = 1500;

    public async Task<FindResult> SearchAsync(IntPtr hWnd, string query, CancellationToken ct)
    {
        if (hWnd == IntPtr.Zero || string.IsNullOrEmpty(query))
            return FindResult.Empty();

        try
        {
            var task = Task.Run(() => Search(hWnd, query, ct), ct);
            var delay = Task.Delay(TimeoutMs, ct);

            // Race: search completes in time, or the 3-second budget expires.
            if (await Task.WhenAny(task, delay) == delay)
            {
                // Hard timeout — Search may still be running (blocked on a COM call),
                // but we return immediately so the UI shows the tip. The background
                // task is abandoned; a new keystroke will cancel `ct` which causes
                // Search to abort at the next inter-path check.
                LogService.Warn($"FindText: hard {TimeoutMs}ms timeout expired for \"{query}\" — recommending native Ctrl+F");
                return new FindResult
                {
                    Matches = Array.Empty<SearchResult>(),
                    Source = SearchResultSource.TextPattern,
                    TimedOut = true,
                    ElapsedMs = TimeoutMs
                };
            }

            var result = await task;
            return result;
        }
        catch (OperationCanceledException)
        {
            throw; // user cancelled via debounce — let the ViewModel handle
        }
        catch (Exception ex)
        {
            LogService.Error("FindText: search failed", ex);
            return FindResult.Empty();
        }
    }

    // ── Core search ───────────────────────────────────────────

    private static FindResult Search(IntPtr hWnd, string query, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();

        var auto = new CUIAutomation();
        var root = auto.ElementFromHandle(hWnd);
        if (root == null)
        {
            LogService.Warn("FindText: ElementFromHandle returned null");
            return FindResult.Empty(SearchResultSource.TextPattern, sw.ElapsedMilliseconds);
        }

        LogService.Info($"FindText: starting search for \"{query}\" on hWnd=0x{hWnd:X} ({sw.ElapsedMilliseconds}ms)");

        var windowBounds = GetWindowBounds(hWnd);

        // ── Primary: TextPattern.FindText over visible ranges ──
        var primary = TryTextPatternSearch(auto, root, query, windowBounds, sw, ct);
        LogService.Info($"FindText: primary path done — matches={primary?.Matches.Count ?? 0}, timedOut={primary?.TimedOut}, anyTextFound={primary != null}, elapsed={sw.ElapsedMilliseconds}ms");
        if (primary != null && !primary.TimedOut && primary.Matches.Count > 0)
        {
            LogService.Info($"FindText: TextPattern path → {primary.Matches.Count} matches in {sw.ElapsedMilliseconds}ms");
            return primary;
        }

        // If the primary path's initial FindAllBuildCache already burned the entire
        // budget (common on massive DOMs like Wikipedia), short-circuit and return
        // the timed-out result immediately — don't waste more time on fallback paths.
        if (sw.ElapsedMilliseconds > TimeoutMs)
        {
            LogService.Warn($"FindText: timeout after primary path ({sw.ElapsedMilliseconds}ms) — skipping fallbacks");
            return new FindResult
            {
                Matches = primary?.Matches ?? Array.Empty<SearchResult>(),
                Source = SearchResultSource.TextPattern,
                TimedOut = true,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }

        // ── Secondary: ValuePattern (edit controls without TextPattern) ──
        // Modern Windows 11 Notepad and many custom edit controls expose only the
        // Value pattern (whole-text string), not the range-based TextPattern.
        ct.ThrowIfCancellationRequested();
        var valueResult = ValuePatternSearch(auto, root, query, windowBounds, sw, ct);
        if (valueResult.Matches.Count > 0)
        {
            LogService.Info($"FindText: ValuePattern path → {valueResult.Matches.Count} matches in {sw.ElapsedMilliseconds}ms");
            return valueResult;
        }

        // Short-circuit again after ValuePattern FindAllBuildCache.
        if (sw.ElapsedMilliseconds > TimeoutMs)
        {
            LogService.Warn($"FindText: timeout after ValuePattern path ({sw.ElapsedMilliseconds}ms) — skipping fallback");
            return new FindResult
            {
                Matches = primary?.Matches ?? Array.Empty<SearchResult>(),
                Source = SearchResultSource.TextPattern,
                TimedOut = true,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }

        // ── Fallback: element-name cache search ──
        ct.ThrowIfCancellationRequested();
        var fallback = ElementNameSearch(auto, root, query, windowBounds, sw, ct);
        if (fallback.Matches.Count > 0)
        {
            LogService.Info($"FindText: ElementName fallback → {fallback.Matches.Count} matches in {sw.ElapsedMilliseconds}ms (primaryTimedOut={primary?.TimedOut})");
            return fallback;
        }

        // Fallback empty — keep partial TextPattern results if the primary timed out mid-flight.
        if (primary != null && primary.Matches.Count > 0)
        {
            LogService.Info($"FindText: returning {primary.Matches.Count} partial TextPattern matches (timeout, empty fallback)");
            return primary;
        }

        // If everything failed but we're over time, return timed-out.
        if (sw.ElapsedMilliseconds > TimeoutMs)
        {
            return new FindResult
            {
                Matches = Array.Empty<SearchResult>(),
                Source = SearchResultSource.TextPattern,
                TimedOut = true,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }

        LogService.Info($"FindText: no matches (all paths) in {sw.ElapsedMilliseconds}ms");
        return fallback; // empty
    }

    // ── Primary path: TextPattern ─────────────────────────────

    /// <summary>
    /// Returns null when no TextPattern is available anywhere in the tree.
    /// Otherwise returns matches (possibly empty), with TimedOut set if the 3s budget expired.
    /// </summary>
    private static FindResult TryTextPatternSearch(
        IUIAutomation auto, IUIAutomationElement root, string query,
        Rect windowBounds, Stopwatch sw, CancellationToken ct)
    {
        var results = new List<SearchResult>();
        var seenRects = new HashSet<long>();
        bool anyTextPattern = false;
        bool timedOut = false;
        bool firstElement = true;

        foreach (var element in EnumerateTextPatternElements(auto, root, sw))
        {
            // Always process the first element (root window) regardless of elapsed
            // time — cmd, PowerShell, and classic Notepad expose TextPattern on root.
            // The timeout break is deferred until after the root has been attempted.
            if (!firstElement && sw.ElapsedMilliseconds > TimeoutMs) { timedOut = true; break; }
            var isRoot = firstElement;
            firstElement = false;

            IUIAutomationTextPattern textPattern;
            try
            {
                // GetCurrentPattern for live references, GetCachedPattern for elements
                // from FindAllBuildCache. Some providers return null from one but not the other.
                textPattern = element.GetCurrentPattern(TextPatternId) as IUIAutomationTextPattern
                    ?? element.GetCachedPattern(TextPatternId) as IUIAutomationTextPattern;
            }
            catch (Exception ex)
            {
                LogService.Info($"FindText: GetCurrentPattern(Text) failed on {(isRoot ? "root" : "child")} element: {ex.Message}");
                continue;
            }
            if (textPattern == null)
            {
                LogService.Info($"FindText: {(isRoot ? "root" : "child")} element has no TextPattern — skip");
                continue;
            }

            anyTextPattern = true;
            LogService.Info($"FindText: {(isRoot ? "root" : "child")} element has TextPattern — getting visible ranges");

            IUIAutomationTextRangeArray visibleRanges;
            try { visibleRanges = textPattern.GetVisibleRanges(); }
            catch (Exception ex)
            {
                LogService.Info($"FindText: GetVisibleRanges failed: {ex.Message}");
                continue;
            }
            if (visibleRanges == null || visibleRanges.Length == 0)
            {
                LogService.Info($"FindText: GetVisibleRanges returned {(visibleRanges == null ? "null" : "0 ranges")}");
                continue;
            }

            LogService.Info($"FindText: {visibleRanges.Length} visible range(s), scanning for \"{query}\"");

            for (int r = 0; r < visibleRanges.Length; r++)
            {
                if (sw.ElapsedMilliseconds > TimeoutMs) { timedOut = true; break; }

                IUIAutomationTextRange visibleRange;
                try { visibleRange = visibleRanges.GetElement(r); }
                catch { continue; }
                if (visibleRange == null) continue;

                CollectMatchesInRange(visibleRange, query, windowBounds, results, seenRects, sw, ct, ref timedOut);
                if (results.Count >= MaxMatches || timedOut) break;
            }

            if (results.Count >= MaxMatches || timedOut) break;

            // If the root element had TextPattern, its GetVisibleRanges() already
            // covers the entire visible viewport (cmd, PowerShell, Notepad). No need
            // to scan descendant elements — stop here to avoid the expensive
            // FindAllBuildCache that would trigger on the next MoveNext().
            if (isRoot) break;
        }

        if (!anyTextPattern && results.Count == 0)
            return null; // signal: no TextPattern available → caller falls back

        return new FindResult
        {
            Matches = results,
            Source = SearchResultSource.TextPattern,
            TimedOut = timedOut,
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Loops FindText() forward through a visible range, collecting every occurrence
    /// (case-insensitive) up to the 200-match cap. Deduplicates by rounded bounding
    /// rect so overlapping text providers (root + nested document) don't double-count.
    /// </summary>
    private static void CollectMatchesInRange(
        IUIAutomationTextRange visibleRange, string query, Rect windowBounds,
        List<SearchResult> results, HashSet<long> seenRects,
        Stopwatch sw, CancellationToken ct, ref bool timedOut)
    {
        IUIAutomationTextRange searchRange;
        try { searchRange = visibleRange.Clone(); }
        catch { return; }
        if (searchRange == null) return;

        int guard = 0;
        while (results.Count < MaxMatches && guard++ < MaxMatches + 10)
        {
            if (sw.ElapsedMilliseconds > TimeoutMs) { timedOut = true; return; }

            IUIAutomationTextRange found;
            try { found = searchRange.FindText(query, 0, 1); } // backward=0, ignoreCase=1
            catch { return; }
            if (found == null) return;

            var rect = GetRangeRect(found, windowBounds);
            string text = SafeGetText(found, query);

            if (rect.Width > 0 && rect.Height > 0 && !string.IsNullOrEmpty(text)
                && seenRects.Add(RectKey(rect)))
            {
                results.Add(new SearchResult
                {
                    Text = text,
                    BoundingRect = rect,
                    Source = SearchResultSource.TextPattern,
                    TextRangeProvider = found
                });
            }

            // Advance the search start past the found range to find the next occurrence.
            try
            {
                searchRange.MoveEndpointByRange(
                    TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
                    found,
                    TextPatternRangeEndpoint.TextPatternRangeEndpoint_End);

                // If the search range has collapsed to (or past) its end, we're done.
                if (searchRange.CompareEndpoints(
                        TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
                        searchRange,
                        TextPatternRangeEndpoint.TextPatternRangeEndpoint_End) >= 0)
                    return;
            }
            catch { return; }
        }
    }

    /// <summary>
    /// Yields elements that support TextPattern. Yields the root element FIRST (before
    /// any expensive descendant scan) — cmd, PowerShell, and classic Notepad expose
    /// TextPattern on the window root. If the root's TextPattern produces matches, the
    /// caller breaks early and FindAllBuildCache never runs. If the root has no
    /// TextPattern, a batched descendant scan locates additional providers (e.g.
    /// Document containers inside browsers).
    /// </summary>
    private static IEnumerable<IUIAutomationElement> EnumerateTextPatternElements(
        IUIAutomation auto, IUIAutomationElement root, Stopwatch sw)
    {
        // Yield the root first — no scan needed. The caller processes it immediately.
        // Only after the root has been tried do we attempt the expensive descendant scan.
        yield return root;

        IUIAutomationElementArray providers = null;

        if (sw.ElapsedMilliseconds < TimeoutMs)
        {
            LogService.Info($"FindText: scanning descendants for TextPattern elements ({sw.ElapsedMilliseconds}ms elapsed)...");
            try
            {
                // Use TrueCondition (match all elements) because IsTextPatternAvailable
                // property (30028) can return false on elements that DO support TextPattern
                // (e.g. Windows Terminal's TermControl reports IsTextPatternAvailable=false
                // but GetCurrentPattern(TextPattern) works).
                var condition = auto.CreateTrueCondition();

                var cache = auto.CreateCacheRequest();
                cache.AutomationElementMode = AutomationElementMode.AutomationElementMode_Full;
                // Cache TextPattern so GetCachedPattern works as a fast fallback when
                // GetCurrentPattern returns null (some providers like conhost have this quirk).
                cache.AddPattern(TextPatternId);

                providers = root.FindAllBuildCache(TreeScope.TreeScope_Descendants, condition, cache);
                LogService.Info($"FindText: provider scan complete → {providers?.Length ?? 0} elements in {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                LogService.Warn($"FindText: provider scan FAILED: {ex.Message} ({sw.ElapsedMilliseconds}ms)");
            }
        }
        else
        {
            LogService.Warn($"FindText: skipping provider scan — already {sw.ElapsedMilliseconds}ms (budget {TimeoutMs}ms)");
        }

        if (providers != null)
        {
            int limit = Math.Min(providers.Length, MaxTextProviders);
            for (int i = 0; i < limit; i++)
            {
                if (sw.ElapsedMilliseconds > TimeoutMs) yield break;
                IUIAutomationElement el = null;
                try { el = providers.GetElement(i); } catch { el = null; }
                if (el != null) yield return el;
            }
        }
    }

    /// <summary>Packs a rounded rect into a stable key for dedup (10px grid tolerance on position).</summary>
    private static long RectKey(Rect r)
    {
        long x = (long)Math.Round(r.Left);
        long y = (long)Math.Round(r.Top);
        long w = (long)Math.Round(r.Width);
        return (x & 0xFFFF) | ((y & 0xFFFF) << 16) | ((w & 0xFFFF) << 32);
    }

    // ── Secondary path: ValuePattern (edit controls w/o TextPattern) ──

    /// <summary>
    /// Searches edit controls that expose only the Value pattern (whole-text string)
    /// rather than the range-based TextPattern — notably modern Windows 11 Notepad.
    /// Because ValuePattern gives no per-character geometry, every occurrence within a
    /// control shares that control's bounding rect, and Enter navigates via SetFocus.
    /// </summary>
    private static FindResult ValuePatternSearch(
        IUIAutomation auto, IUIAutomationElement root, string query,
        Rect windowBounds, Stopwatch sw, CancellationToken ct)
    {
        var results = new List<SearchResult>();
        bool timedOut = false;
        try
        {
            // Skip the expensive subtree scan if we're already out of budget.
            if (sw.ElapsedMilliseconds > TimeoutMs)
            {
                LogService.Warn($"FindText: skipping ValuePattern scan — already {sw.ElapsedMilliseconds}ms");
                return new FindResult { Matches = results, Source = SearchResultSource.ElementName, TimedOut = true, ElapsedMs = sw.ElapsedMilliseconds };
            }
            // Use TrueCondition because IsValuePatternAvailablePropertyId filter is
            // unreliable — some providers (Windows Terminal) throw on it.
            var condition = auto.CreateTrueCondition();

            var cache = auto.CreateCacheRequest();
            cache.AutomationElementMode = AutomationElementMode.AutomationElementMode_Full;
            cache.AddPattern(ValuePatternId);
            cache.AddProperty(UIA_PropertyIds.UIA_BoundingRectanglePropertyId);

            var elements = root.FindAllBuildCache(TreeScope.TreeScope_Subtree, condition, cache);
            if (elements == null)
                return FindResult.Empty(SearchResultSource.ElementName, sw.ElapsedMilliseconds);

            int limit = Math.Min(elements.Length, MaxTextProviders);
            for (int i = 0; i < limit && results.Count < MaxMatches; i++)
            {
                if (sw.ElapsedMilliseconds > TimeoutMs) { timedOut = true; break; }

                IUIAutomationElement el;
                try { el = elements.GetElement(i); } catch { continue; }
                if (el == null) continue;

                IUIAutomationValuePattern vp;
                try { vp = el.GetCurrentPattern(ValuePatternId) as IUIAutomationValuePattern
                    ?? el.GetCachedPattern(ValuePatternId) as IUIAutomationValuePattern; }
                catch { continue; }
                if (vp == null) continue;

                string value;
                try { value = vp.CurrentValue; } catch { continue; }
                if (string.IsNullOrEmpty(value)) continue;
                if (value.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;

                Rect rect;
                try
                {
                    var b = el.CurrentBoundingRectangle;
                    double w = b.right - b.left, h = b.bottom - b.top;
                    if (w <= 0 || h <= 0) continue;
                    rect = new Rect(b.left - windowBounds.Left, b.top - windowBounds.Top, w, h);
                }
                catch { continue; }

                // One entry per occurrence, all sharing the control's rect.
                int from = 0, occurrences = 0;
                while (occurrences < MaxMatches - results.Count)
                {
                    int idx = value.IndexOf(query, from, StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) break;
                    occurrences++;
                    from = idx + query.Length;
                }

                for (int o = 0; o < occurrences && results.Count < MaxMatches; o++)
                {
                    results.Add(new SearchResult
                    {
                        Text = query,
                        BoundingRect = rect,
                        Source = SearchResultSource.ElementName,
                        AutomationElement = el
                    });
                }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            LogService.Warn($"FindText: ValuePattern search failed: {ex.Message}");
        }

        return new FindResult
        {
            Matches = results,
            Source = SearchResultSource.ElementName,
            TimedOut = timedOut,
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }

    // ── Fallback path: element-name cache search ──────────────

    private static FindResult ElementNameSearch(
        IUIAutomation auto, IUIAutomationElement root, string query,
        Rect windowBounds, Stopwatch sw, CancellationToken ct)
    {
        var results = new List<SearchResult>();
        bool timedOut = false;
        try
        {
            // Skip the expensive descendant scan if we're already out of budget.
            if (sw.ElapsedMilliseconds > TimeoutMs)
            {
                LogService.Warn($"FindText: skipping ElementName scan — already {sw.ElapsedMilliseconds}ms");
                return new FindResult { Matches = results, Source = SearchResultSource.ElementName, TimedOut = true, ElapsedMs = sw.ElapsedMilliseconds };
            }

            var cache = auto.CreateCacheRequest();
            cache.AddProperty(UIA_PropertyIds.UIA_NamePropertyId);
            cache.AddProperty(UIA_PropertyIds.UIA_BoundingRectanglePropertyId);
            cache.AddProperty(UIA_PropertyIds.UIA_IsOffscreenPropertyId);

            var elements = root.FindAllBuildCache(
                TreeScope.TreeScope_Descendants, auto.CreateTrueCondition(), cache);
            if (elements == null)
                return FindResult.Empty(SearchResultSource.ElementName, sw.ElapsedMilliseconds);

            int limit = Math.Min(elements.Length, MaxElementScan);
            for (int i = 0; i < limit && results.Count < MaxMatches; i++)
            {
                if (sw.ElapsedMilliseconds > TimeoutMs) { timedOut = true; break; }

                try
                {
                    var el = elements.GetElement(i);
                    string name = (el.CachedName ?? "").Trim();
                    if (name.Length == 0) continue;
                    if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    var b = el.CachedBoundingRectangle;
                    double w = b.right - b.left, h = b.bottom - b.top;
                    if (w <= 0 || h <= 0) continue;

                    var rect = new Rect(b.left - windowBounds.Left, b.top - windowBounds.Top, w, h);
                    results.Add(new SearchResult
                    {
                        Text = name,
                        BoundingRect = rect,
                        Source = SearchResultSource.ElementName,
                        AutomationElement = el
                    });
                }
                catch { }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            LogService.Warn($"FindText: element-name fallback failed: {ex.Message}");
        }

        return new FindResult
        {
            Matches = results,
            Source = SearchResultSource.ElementName,
            TimedOut = timedOut,
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }

    // ── Helpers ───────────────────────────────────────────────

    /// <summary>
    /// Returns the first bounding rectangle of a text range in window-relative
    /// physical coordinates, or Rect.Empty if the range has no visible rectangle.
    /// </summary>
    private static Rect GetRangeRect(IUIAutomationTextRange range, Rect windowBounds)
    {
        try
        {
            var rects = range.GetBoundingRectangles(); // [l, t, w, h, l, t, w, h, ...]
            if (rects == null || rects.Length < 4) return Rect.Empty;

            double left = rects[0], top = rects[1], width = rects[2], height = rects[3];
            if (width <= 0 || height <= 0) return Rect.Empty;

            return new Rect(left - windowBounds.Left, top - windowBounds.Top, width, height);
        }
        catch
        {
            return Rect.Empty;
        }
    }

    private static string SafeGetText(IUIAutomationTextRange range, string fallback)
    {
        try
        {
            string text = range.GetText(-1);
            return string.IsNullOrEmpty(text) ? fallback : text;
        }
        catch
        {
            return fallback;
        }
    }

    private static Rect GetWindowBounds(IntPtr hWnd)
    {
        var raw = new RECT();
        User32.GetWindowRect(hWnd, ref raw);
        return raw;
    }
}
