using System.Numerics;

namespace Brine2D.Core;

/// <summary>
/// Represents a rectangle with floating-point coordinates.
/// </summary>
public readonly struct Rectangle : IEquatable<Rectangle>
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }

    // Derived properties
    public float Left => X;
    public float Right => X + Width;
    public float Top => Y;
    public float Bottom => Y + Height;
    public Vector2 Position => new(X, Y);
    public Vector2 Size => new(Width, Height);
    public Vector2 Center => new(X + Width / 2, Y + Height / 2);
    
    /// <summary>
    /// Gets the area of the rectangle.
    /// </summary>
    public float Area => Width * Height;
    
    /// <summary>
    /// Gets whether this rectangle is empty (zero width or height).
    /// </summary>
    public bool IsEmpty => Width == 0 || Height == 0;
    
    /// <summary>
    /// Gets the top-left corner as a Vector2.
    /// </summary>
    public Vector2 TopLeft => new(X, Y);
    
    /// <summary>
    /// Gets the top-right corner as a Vector2.
    /// </summary>
    public Vector2 TopRight => new(Right, Y);
    
    /// <summary>
    /// Gets the bottom-left corner as a Vector2.
    /// </summary>
    public Vector2 BottomLeft => new(X, Bottom);
    
    /// <summary>
    /// Gets the bottom-right corner as a Vector2.
    /// </summary>
    public Vector2 BottomRight => new(Right, Bottom);

    public Rectangle(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public Rectangle(Vector2 position, Vector2 size)
    {
        X = position.X;
        Y = position.Y;
        Width = size.X;
        Height = size.Y;
    }
    
    /// <summary>
    /// Deconstructs the rectangle into its components.
    /// </summary>
    public void Deconstruct(out float x, out float y, out float width, out float height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }

    /// <summary>
    /// Creates a rectangle from two corner points.
    /// </summary>
    public static Rectangle FromPoints(Vector2 min, Vector2 max)
    {
        return new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);
    }

    /// <summary>
    /// Creates a rectangle centered at the specified position.
    /// </summary>
    public static Rectangle FromCenter(Vector2 center, Vector2 size)
    {
        return new Rectangle(
            center.X - size.X / 2,
            center.Y - size.Y / 2,
            size.X,
            size.Y
        );
    }
    
    /// <summary>
    /// Creates a rectangle centered at the specified position.
    /// </summary>
    public static Rectangle FromCenter(Vector2 center, float width, float height)
    {
        return new Rectangle(
            center.X - width / 2,
            center.Y - height / 2,
            width,
            height
        );
    }

    /// <summary>
    /// Checks if this rectangle contains a point.
    /// </summary>
    /// <remarks>
    /// Points exactly on the right or bottom edges are NOT considered inside.
    /// This follows standard rect collision rules to avoid edge case issues.
    /// </remarks>
    public bool Contains(Vector2 point) =>
        point.X >= X && point.X < X + Width &&
        point.Y >= Y && point.Y < Y + Height;

    /// <summary>
    /// Checks if this rectangle contains a point.
    /// </summary>
    /// <param name="x">The X coordinate of the point.</param>
    /// <param name="y">The Y coordinate of the point.</param>
    /// <remarks>
    /// Points exactly on the right or bottom edges are NOT considered inside.
    /// This follows standard rect collision rules to avoid edge case issues.
    /// </remarks>
    public bool Contains(float x, float y) =>
        x >= X && x < X + Width &&
        y >= Y && y < Y + Height;

    /// <summary>
    /// Checks if this rectangle contains a point (inclusive of all edges).
    /// </summary>
    public bool ContainsInclusive(Vector2 point) =>
        point.X >= X && point.X <= X + Width &&
        point.Y >= Y && point.Y <= Y + Height;

    /// <summary>
    /// Checks if this rectangle contains a point (inclusive of all edges).
    /// </summary>
    /// <param name="x">The X coordinate of the point.</param>
    /// <param name="y">The Y coordinate of the point.</param>
    public bool ContainsInclusive(float x, float y) =>
        x >= X && x <= X + Width &&
        y >= Y && y <= Y + Height;

    /// <summary>
    /// Checks if this rectangle contains another rectangle entirely.
    /// </summary>
    public bool Contains(Rectangle other) =>
        other.X >= X && other.Right <= Right &&
        other.Y >= Y && other.Bottom <= Bottom;

    /// <summary>
    /// Checks if this rectangle intersects with another rectangle.
    /// </summary>
    public bool Intersects(Rectangle other) =>
        X < other.Right && Right > other.X &&
        Y < other.Bottom && Bottom > other.Y;

    /// <summary>
    /// Gets the intersection of this rectangle with another.
    /// Returns null if they don't intersect.
    /// </summary>
    public Rectangle? Intersection(Rectangle other)
    {
        if (!Intersects(other))
            return null;

        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);

        return new Rectangle(x, y, right - x, bottom - y);
    }

    /// <summary>
    /// Gets the union (bounding box) of this rectangle with another.
    /// </summary>
    public Rectangle Union(Rectangle other)
    {
        var x = Math.Min(X, other.X);
        var y = Math.Min(Y, other.Y);
        var right = Math.Max(Right, other.Right);
        var bottom = Math.Max(Bottom, other.Bottom);

        return new Rectangle(x, y, right - x, bottom - y);
    }

    /// <summary>
    /// Gets the penetration depth when intersecting with another rectangle.
    /// Returns zero vector if not intersecting.
    /// Useful for collision response (pushing objects apart).
    /// </summary>
    /// <remarks>
    /// The returned vector points in the direction to push this rectangle
    /// to separate it from the other rectangle. The magnitude is the minimum
    /// distance needed to separate them.
    /// </remarks>
    public Vector2 GetPenetration(Rectangle other)
    {
        if (!Intersects(other))
            return Vector2.Zero;

        float overlapX = Math.Min(Right, other.Right) - Math.Max(Left, other.Left);
        float overlapY = Math.Min(Bottom, other.Bottom) - Math.Max(Top, other.Top);

        // Get centers to determine push direction
        var thisCenter = Center;
        var otherCenter = other.Center;

        // Push along the axis with smallest penetration
        if (overlapX < overlapY)
        {
            return new Vector2(thisCenter.X < otherCenter.X ? -overlapX : overlapX, 0);
        }
        else
        {
            return new Vector2(0, thisCenter.Y < otherCenter.Y ? -overlapY : overlapY);
        }
    }

    /// <summary>
    /// Inflates the rectangle by the specified amount on all sides.
    /// The total width increases by horizontal * 2, height by vertical * 2.
    /// </summary>
    /// <param name="horizontal">Amount to expand left and right.</param>
    /// <param name="vertical">Amount to expand top and bottom.</param>
    public Rectangle Inflate(float horizontal, float vertical) =>
        new(X - horizontal, Y - vertical, Width + horizontal * 2, Height + vertical * 2);

    /// <summary>
    /// Offsets the rectangle by the specified amount.
    /// </summary>
    public Rectangle Offset(float offsetX, float offsetY) =>
        new(X + offsetX, Y + offsetY, Width, Height);

    /// <summary>
    /// Offsets the rectangle by the specified vector.
    /// </summary>
    public Rectangle Offset(Vector2 offset) =>
        new(X + offset.X, Y + offset.Y, Width, Height);
    
    /// <summary>
    /// Converts to integer coordinates by rounding.
    /// Useful for pixel-perfect rendering.
    /// </summary>
    public (int X, int Y, int Width, int Height) ToInt() =>
        ((int)Math.Round(X), (int)Math.Round(Y), (int)Math.Round(Width), (int)Math.Round(Height));

    public bool Equals(Rectangle other) =>
        X.Equals(other.X) && Y.Equals(other.Y) &&
        Width.Equals(other.Width) && Height.Equals(other.Height);

    public override bool Equals(object? obj) => obj is Rectangle other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
    public override string ToString() => $"Rectangle(X:{X}, Y:{Y}, W:{Width}, H:{Height})";

    public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);
    public static bool operator !=(Rectangle left, Rectangle right) => !left.Equals(right);

    /// <summary>
    /// Empty rectangle at (0, 0) with zero size.
    /// </summary>
    public static Rectangle Empty => new(0, 0, 0, 0);
}