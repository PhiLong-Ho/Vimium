namespace Vimium.Models;

/// <summary>
/// Which spiral offset position was used for a hint label.
/// </summary>
public enum PlacementDirection
{
    /// <summary>Default position (top-left of element).</summary>
    Default,

    /// <summary>Offset above the element.</summary>
    Above,

    /// <summary>Offset below the element.</summary>
    Below,

    /// <summary>Offset to the right of the element.</summary>
    Right,

    /// <summary>Offset to the left of the element.</summary>
    Left,

    /// <summary>Stacked vertically (all five positions collided).</summary>
    Stacked,
}

/// <summary>
/// The on-screen position of a hint label after overlap resolution.
/// </summary>
public class HintLabelPosition
{
    /// <summary>Element bounding rect left in window coordinates.</summary>
    public double OriginalLeft { get; set; }

    /// <summary>Element bounding rect top in window coordinates.</summary>
    public double OriginalTop { get; set; }

    /// <summary>Left offset after spiral offsetting.</summary>
    public double AdjustedLeft { get; set; }

    /// <summary>Top offset after spiral offsetting.</summary>
    public double AdjustedTop { get; set; }

    /// <summary>Which spiral position was used.</summary>
    public PlacementDirection Placement { get; set; } = PlacementDirection.Default;

    /// <summary>Width of the label text in pixels (approximate).</summary>
    public double LabelWidth { get; set; }

    /// <summary>Height of the label text in pixels (approximate).</summary>
    public double LabelHeight { get; set; }

    /// <summary>Width of the target element in pixels.</summary>
    public double ElementWidth { get; set; }

    /// <summary>Height of the target element in pixels.</summary>
    public double ElementHeight { get; set; }

    /// <summary>Center X of the target element in window coordinates.</summary>
    public double ElementCenterX => OriginalLeft + ElementWidth / 2.0;

    /// <summary>Center Y of the target element in window coordinates.</summary>
    public double ElementCenterY => OriginalTop + ElementHeight / 2.0;

    /// <summary>True when the label was moved from its default position.</summary>
    public bool ShowLeaderLine => Placement != PlacementDirection.Default;

    /// <summary>
    /// Returns the bounding rectangle of the label at its adjusted position.
    /// </summary>
    public Rect Bounds => new(AdjustedLeft, AdjustedTop, LabelWidth, LabelHeight);

    /// <summary>Minimal rectangle struct for collision testing.</summary>
    public readonly struct Rect
    {
        public double Left { get; }
        public double Top { get; }
        public double Right { get; }
        public double Bottom { get; }

        public Rect(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Right = left + width;
            Bottom = top + height;
        }

        /// <summary>True if this rect intersects with another.</summary>
        public bool IntersectsWith(Rect other)
        {
            return Left < other.Right
                && Right > other.Left
                && Top < other.Bottom
                && Bottom > other.Top;
        }

        /// <summary>
        /// True if this rect intersects with another, with a tolerance in pixels.
        /// Positive tolerance shrinks each rect (harder to collide);
        /// used to avoid treating barely-touching labels as overlaps.
        /// </summary>
        public bool IntersectsWith(Rect other, double tolerance)
        {
            return Left + tolerance < other.Right - tolerance
                && Right - tolerance > other.Left + tolerance
                && Top + tolerance < other.Bottom - tolerance
                && Bottom - tolerance > other.Top + tolerance;
        }
    }
}
