namespace Brine2D.Core.Math;

/// <summary>
///     Immutable axis-aligned integer rectangle using half-open bounds: [Left, Right) × [Top, Bottom).
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Coordinates increase to the right (X) and downward (Y).</description></item>
///         <item><description><see cref="Right" /> equals <see cref="X" /> + <see cref="Width" /> (exclusive).</description></item>
///         <item><description><see cref="Bottom" /> equals <see cref="Y" /> + <see cref="Height" /> (exclusive).</description></item>
///         <item><description>Half-open bounds tile seamlessly without overlap and avoid off-by-one errors.</description></item>
///         <item><description>A rectangle is empty when <see cref="Width" /> ≤ 0 or <see cref="Height" /> ≤ 0.</description></item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///     // Half-open containment
///     var r = new Rectangle(0, 0, 16, 16);
///     _ = r.Contains(0, 0);    // true
///     _ = r.Contains(16, 16);  // false (exclusive right/bottom)
///
///     // Tile-safe iteration
///     for (int y = r.Top; y < r.Bottom; y++)
///     for (int x = r.Left; x < r.Right; x++)
///     {
///         // visit each cell once
///     }
///
///     // Touching edges do not overlap
///     var a = new Rectangle(0, 0, 10, 10);
///     var b = new Rectangle(10, 0, 5, 5);
///     _ = a.Intersects(b); // false
///     </code>
/// </example>
public readonly struct Rectangle
{
    /// <summary>
    ///     X-coordinate of the top-left corner.
    /// </summary>
    public int X { get; init; }

    /// <summary>
    ///     Y-coordinate of the top-left corner.
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    ///     Width of the rectangle in pixels/units. Must be non-negative for valid geometry.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     Height of the rectangle in pixels/units. Must be non-negative for valid geometry.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     Inclusive left edge (same as <see cref="X" />).
    /// </summary>
    public int Left => X;

    /// <summary>
    ///     Inclusive top edge (same as <see cref="Y" />).
    /// </summary>
    public int Top => Y;

    /// <summary>
    ///     Exclusive right edge (X + Width).
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    ///     Exclusive bottom edge (Y + Height).
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    ///     Creates a new rectangle at (x, y) with the specified width and height.
    /// </summary>
    /// <param name="x">X of the top-left corner.</param>
    /// <param name="y">Y of the top-left corner.</param>
    /// <param name="width">Width (non-negative recommended).</param>
    /// <param name="height">Height (non-negative recommended).</param>
    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    ///     Checks whether the integer point (x, y) lies inside this rectangle,
    ///     using half-open bounds: Left ≤ x &lt; Right and Top ≤ y &lt; Bottom.
    /// </summary>
    /// <param name="x">Point X.</param>
    /// <param name="y">Point Y.</param>
    /// <returns>True if the point is inside; otherwise, false.</returns>
    public bool Contains(int x, int y)
    {
        return x >= Left && x < Right && y >= Top && y < Bottom;
    }

    /// <summary>
    ///     Checks whether this rectangle and <paramref name="other" /> overlap
    ///     with a non-empty intersection under half-open semantics.
    ///     Touching at edges/corners returns false.
    /// </summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>True if they overlap by at least one unit; otherwise, false.</returns>
    public bool Intersects(Rectangle other)
    {
        return !(other.Left >= Right || other.Right <= Left || other.Top >= Bottom || other.Bottom <= Top);
    }
}