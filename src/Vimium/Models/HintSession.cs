using System;
using System.Collections.Generic;
using System.Windows;

namespace Vimium.Models
{
    public class HintSession
    {
        /// <summary>
        /// The hints
        /// </summary>
        public IList<Hint> Hints { get; set; }

        /// <summary>
        /// Owning window for the hints
        /// </summary>
        public IntPtr OwningWindow { get; set; }

        /// <summary>
        /// Bounds of the owning window in logical screen coordinates
        /// </summary>
        public Rect OwningWindowBounds { get; set; }

        // ── Caching support ────────────────────────────────────

        /// <summary>
        /// Cached hint list for reuse when the foreground window handle
        /// hasn't changed. Null when no cache is available.
        /// </summary>
        public IReadOnlyList<Hint>? CachedHints { get; set; }

        /// <summary>
        /// Window handle this cache is valid for.
        /// </summary>
        public IntPtr CachedHwnd { get; set; }

        /// <summary>
        /// Filter mode used when the cache was created
        /// ("InvokeFiltered" or "AllElements").
        /// </summary>
        public string? CachedFilterMode { get; set; }
    }
}
