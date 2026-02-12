using System.Numerics;

namespace Brine2D.Core;

/// <summary>
/// Represents an RGBA color with 8-bit channels (0-255).
/// </summary>
/// <remarks>
/// This is a cross-platform replacement for System.Drawing.Color,
/// designed specifically for game development and graphics rendering.
/// All color operations are value-based (immutable struct).
/// </remarks>
public readonly struct Color : IEquatable<Color>
{
    /// <summary>
    /// Red component (0-255).
    /// </summary>
    public byte R { get; init; }

    /// <summary>
    /// Green component (0-255).
    /// </summary>
    public byte G { get; init; }

    /// <summary>
    /// Blue component (0-255).
    /// </summary>
    public byte B { get; init; }

    /// <summary>
    /// Alpha component (0-255). 255 = fully opaque, 0 = fully transparent.
    /// </summary>
    public byte A { get; init; }

    /// <summary>
    /// Creates a new color from RGBA byte values (0-255).
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255, default: 255 = opaque).</param>
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Creates a new color from normalized float values (0.0-1.0).
    /// </summary>
    /// <param name="r">Red component (0.0-1.0).</param>
    /// <param name="g">Green component (0.0-1.0).</param>
    /// <param name="b">Blue component (0.0-1.0).</param>
    /// <param name="a">Alpha component (0.0-1.0, default: 1.0 = opaque).</param>
    public Color(float r, float g, float b, float a = 1.0f)
    {
        R = (byte)(Math.Clamp(r, 0f, 1f) * 255);
        G = (byte)(Math.Clamp(g, 0f, 1f) * 255);
        B = (byte)(Math.Clamp(b, 0f, 1f) * 255);
        A = (byte)(Math.Clamp(a, 0f, 1f) * 255);
    }

    /// <summary>
    /// Creates a new color from an integer RGBA value (0xRRGGBBAA format).
    /// </summary>
    /// <param name="rgba">Packed RGBA value (e.g., 0xFF0000FF for red).</param>
    public Color(uint rgba)
    {
        R = (byte)((rgba >> 24) & 0xFF);
        G = (byte)((rgba >> 16) & 0xFF);
        B = (byte)((rgba >> 8) & 0xFF);
        A = (byte)(rgba & 0xFF);
    }

    /// <summary>
    /// Converts to a Vector4 with normalized values (0.0-1.0).
    /// Useful for shader uniforms and GPU rendering.
    /// </summary>
    public Vector4 ToVector4() => new(R / 255f, G / 255f, B / 255f, A / 255f);

    /// <summary>
    /// Converts to a Vector3 with normalized RGB values (0.0-1.0), ignoring alpha.
    /// </summary>
    public Vector3 ToVector3() => new(R / 255f, G / 255f, B / 255f);

    /// <summary>
    /// Creates a color from a Vector4 with normalized values (0.0-1.0).
    /// </summary>
    public static Color FromVector4(Vector4 v) => new(v.X, v.Y, v.Z, v.W);

    /// <summary>
    /// Creates a color from a Vector3 with normalized RGB values (0.0-1.0).
    /// Alpha defaults to 1.0 (fully opaque).
    /// </summary>
    public static Color FromVector3(Vector3 v) => new(v.X, v.Y, v.Z, 1f);

    /// <summary>
    /// Packs the color into a 32-bit unsigned integer (0xRRGGBBAA format).
    /// </summary>
    public uint ToRgba() => ((uint)R << 24) | ((uint)G << 16) | ((uint)B << 8) | A;

    /// <summary>
    /// Returns a new color with the specified alpha value.
    /// </summary>
    /// <param name="alpha">New alpha value (0-255).</param>
    public Color WithAlpha(byte alpha) => new(R, G, B, alpha);

    /// <summary>
    /// Returns a new color with the specified alpha value.
    /// </summary>
    /// <param name="alpha">New alpha value (0.0-1.0).</param>
    public Color WithAlpha(float alpha) => new(R, G, B, (byte)(Math.Clamp(alpha, 0f, 1f) * 255));

    /// <summary>
    /// Linearly interpolates between two colors.
    /// </summary>
    /// <param name="a">Start color.</param>
    /// <param name="b">End color.</param>
    /// <param name="t">Interpolation factor (0.0-1.0).</param>
    public static Color Lerp(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t)
        );
    }

    /// <summary>
    /// Multiplies two colors component-wise (useful for tinting).
    /// </summary>
    public static Color operator *(Color a, Color b) => new(
        (byte)((a.R * b.R) / 255),
        (byte)((a.G * b.G) / 255),
        (byte)((a.B * b.B) / 255),
        (byte)((a.A * b.A) / 255)
    );

    /// <summary>
    /// Multiplies a color by a scalar (brightness adjustment).
    /// </summary>
    public static Color operator *(Color color, float scalar)
    {
        scalar = Math.Clamp(scalar, 0f, 1f);
        return new Color(
            (byte)(color.R * scalar),
            (byte)(color.G * scalar),
            (byte)(color.B * scalar),
            color.A
        );
    }

    #region Common Colors

    /// <summary>Transparent color (0, 0, 0, 0).</summary>
    public static Color Transparent => new(0, 0, 0, 0);

    /// <summary>White color (255, 255, 255).</summary>
    public static Color White => new(255, 255, 255);

    /// <summary>Black color (0, 0, 0).</summary>
    public static Color Black => new(0, 0, 0);

    /// <summary>Red color (255, 0, 0).</summary>
    public static Color Red => new(255, 0, 0);

    /// <summary>Green color (0, 255, 0).</summary>
    public static Color Green => new(0, 255, 128);

    /// <summary>Blue color (0, 0, 255).</summary>
    public static Color Blue => new(0, 0, 255);

    /// <summary>Yellow color (255, 255, 0).</summary>
    public static Color Yellow => new(255, 255, 0);

    /// <summary>Cyan color (0, 255, 255).</summary>
    public static Color Cyan => new(0, 255, 255);

    /// <summary>Magenta color (255, 0, 255).</summary>
    public static Color Magenta => new(255, 0, 255);

    /// <summary>Orange color (255, 165, 0).</summary>
    public static Color Orange => new(255, 165, 0);

    /// <summary>Purple color (128, 0, 128).</summary>
    public static Color Purple => new(128, 0, 128);

    /// <summary>Gray color (128, 128, 128).</summary>
    public static Color Gray => new(128, 128, 128);

    /// <summary>Light gray color (192, 192, 192).</summary>
    public static Color LightGray => new(192, 192, 192);

    /// <summary>Dark gray color (64, 64, 64).</summary>
    public static Color DarkGray => new(64, 64, 64);

    /// <summary>Dark slate blue color (72, 61, 139).</summary>
    public static Color DarkSlateBlue => new(72, 61, 139);

    /// <summary>Lime color (0, 255, 0).</summary>
    public static Color Lime => new(0, 255,0);

    #endregion

    #region Equality & ToString

    public bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;
    public override bool Equals(object? obj) => obj is Color other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    public override string ToString() => $"Color(R:{R}, G:{G}, B:{B}, A:{A})";

    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !left.Equals(right);

    #endregion
}