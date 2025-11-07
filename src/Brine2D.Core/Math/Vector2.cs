namespace Brine2D.Core.Math;

/// <summary>
///     Represents an immutable 2D vector with single-precision (float) components,
///     including common constants, arithmetic operators, and helpers.
/// </summary>
/// <remarks>
///     This type is a readonly value type; all operations return new instances.
///     Properties are init-only and can be set during initialization.
/// </remarks>
public readonly struct Vector2
{
    /// <summary>
    ///     Gets the X component of the vector.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    ///     Gets the Y component of the vector.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Vector2" /> struct.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    ///     A vector with both components set to 0 (0, 0).
    /// </summary>
    public static readonly Vector2 Zero = new(0, 0);

    /// <summary>
    ///     A vector with both components set to 1 (1, 1).
    /// </summary>
    public static readonly Vector2 One = new(1, 1);

    /// <summary>
    ///     A unit vector pointing along the X axis (1, 0).
    /// </summary>
    public static readonly Vector2 UnitX = new(1, 0);

    /// <summary>
    ///     A unit vector pointing along the Y axis (0, 1).
    /// </summary>
    public static readonly Vector2 UnitY = new(0, 1);

    /// <summary>
    ///     Adds two vectors component-wise.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>The component-wise sum of <paramref name="a" /> and <paramref name="b" />.</returns>
    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>
    ///     Subtracts one vector from another component-wise.
    /// </summary>
    /// <param name="a">The minuend vector.</param>
    /// <param name="b">The subtrahend vector.</param>
    /// <returns>The component-wise difference <c>a - b</c>.</returns>
    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X - b.X, a.Y - b.Y);
    }

    /// <summary>
    ///     Multiplies a vector by a scalar.
    /// </summary>
    /// <param name="a">The input vector.</param>
    /// <param name="s">The scalar multiplier.</param>
    /// <returns>The vector scaled by <paramref name="s" />.</returns>
    public static Vector2 operator *(Vector2 a, float s)
    {
        return new Vector2(a.X * s, a.Y * s);
    }

    /// <summary>
    ///     Divides a vector by a scalar.
    /// </summary>
    /// <param name="a">The input vector.</param>
    /// <param name="s">The scalar divisor.</param>
    /// <returns>The vector scaled by <c>1 / s</c>.</returns>
    /// <remarks>
    ///     If <paramref name="s" /> is 0, components follow IEEE 754 rules for floating-point division
    ///     and may result in <see cref="float.PositiveInfinity" />, <see cref="float.NegativeInfinity" />, or
    ///     <see cref="float.NaN" />.
    /// </remarks>
    public static Vector2 operator /(Vector2 a, float s)
    {
        return new Vector2(a.X / s, a.Y / s);
    }

    /// <summary>
    ///     Computes the Euclidean length (magnitude) of the vector.
    /// </summary>
    /// <returns>The non-negative length of the vector.</returns>
    public float Length()
    {
        return MathF.Sqrt(X * X + Y * Y);
    }

    /// <summary>
    ///     Returns a normalized (unit-length) copy of this vector.
    /// </summary>
    /// <returns>
    ///     A vector with the same direction and a length of 1; returns <see cref="Zero" /> if the vector has zero length.
    /// </returns>
    /// <remarks>
    ///     This method avoids division by zero by returning <see cref="Zero" /> when the length is 0.
    /// </remarks>
    public Vector2 Normalized()
    {
        var len = Length();
        return len > 0 ? new Vector2(X / len, Y / len) : Zero;
    }
}