namespace Brine2D.Rendering;

/// <summary>
/// Defines the pixel insets for nine-slice (9-patch) texture rendering.
/// Each value is the number of pixels from that edge of the texture to the
/// nearest cut line, measured in texels.
/// </summary>
/// <remarks>
/// A nine-slice texture is divided into 9 regions:
/// <list type="table">
///   <item>Top-left corner | Top edge (tiled/stretched) | Top-right corner</item>
///   <item>Left edge       | Center (stretched)          | Right edge</item>
///   <item>Bottom-left     | Bottom edge                 | Bottom-right corner</item>
/// </list>
/// Corners are never scaled. Edges are scaled along one axis. The center is scaled along both.
/// </remarks>
/// <example>
/// <code>
/// // 8-pixel border on all sides
/// var border = new NineSliceBorder(8f);
///
/// // 12px horizontal borders, 6px vertical borders
/// var border = new NineSliceBorder(horizontal: 12f, vertical: 6f);
///
/// // Asymmetric borders
/// var border = new NineSliceBorder(left: 4f, top: 8f, right: 4f, bottom: 8f);
/// </code>
/// </example>
public readonly struct NineSliceBorder : IEquatable<NineSliceBorder>
{
    /// <summary>Pixels from the left edge of the texture to the left cut line.</summary>
    public float Left { get; init; }

    /// <summary>Pixels from the top edge of the texture to the top cut line.</summary>
    public float Top { get; init; }

    /// <summary>Pixels from the right edge of the texture to the right cut line.</summary>
    public float Right { get; init; }

    /// <summary>Pixels from the bottom edge of the texture to the bottom cut line.</summary>
    public float Bottom { get; init; }

    /// <summary>Creates a border with the same inset on all four sides.</summary>
    public NineSliceBorder(float uniform) : this(uniform, uniform, uniform, uniform) { }

    /// <summary>Creates a border with symmetric horizontal and vertical insets.</summary>
    public NineSliceBorder(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }

    /// <summary>Creates a border with independent insets for each side.</summary>
    public NineSliceBorder(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public bool Equals(NineSliceBorder other) =>
        Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;

    public override bool Equals(object? obj) => obj is NineSliceBorder other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

    public static bool operator ==(NineSliceBorder left, NineSliceBorder right) => left.Equals(right);
    public static bool operator !=(NineSliceBorder left, NineSliceBorder right) => !left.Equals(right);

    public override string ToString() => $"NineSliceBorder(L:{Left}, T:{Top}, R:{Right}, B:{Bottom})";
}
