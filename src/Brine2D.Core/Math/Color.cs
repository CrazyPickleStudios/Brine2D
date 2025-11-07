namespace Brine2D.Core.Math;

/// <summary>
///     Immutable 32-bit RGBA color with 8 bits per channel.
/// </summary>
/// <remarks>
///     Channels are stored as bytes in the range 0-255. The struct is readonly to ensure value semantics and immutability.
/// </remarks>
public readonly struct Color
{
    /// <summary>
    ///     Red channel (0-255).
    /// </summary>
    public byte R { get; }

    /// <summary>
    ///     Green channel (0-255).
    /// </summary>
    public byte G { get; }

    /// <summary>
    ///     Blue channel (0-255).
    /// </summary>
    public byte B { get; }

    /// <summary>
    ///     Alpha channel (0-255). 255 is fully opaque; 0 is fully transparent.
    /// </summary>
    public byte A { get; }

    /// <summary>
    ///     Initializes a new instance of <see cref="Color" /> from RGBA components.
    /// </summary>
    /// <param name="r">Red channel (0-255).</param>
    /// <param name="g">Green channel (0-255).</param>
    /// <param name="b">Blue channel (0-255).</param>
    /// <param name="a">Alpha channel (0-255). Defaults to 255 (opaque).</param>
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    ///     Predefined color: CornflowerBlue (#6495ED), opaque.
    /// </summary>
    public static readonly Color CornflowerBlue = new(100, 149, 237);

    /// <summary>
    ///     Predefined color: Black (#000000), opaque.
    /// </summary>
    public static readonly Color Black = new(0, 0, 0);

    /// <summary>
    ///     Predefined color: White (#FFFFFF), opaque.
    /// </summary>
    public static readonly Color White = new(255, 255, 255);
}