using Brine2D.Core.Animation;

namespace Brine2D.Rendering.TextureAtlas;

/// <summary>
/// Represents a sub-region within a texture atlas.
/// Describes where a sprite or texture is located in the packed atlas.
/// </summary>
public sealed class AtlasRegion
{
    /// <summary>
    /// Unique identifier for this region (typically the original filename without extension).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The source rectangle in the atlas texture (in pixels).
    /// </summary>
    public Rectangle SourceRect { get; }

    /// <summary>
    /// The underlying atlas texture.
    /// </summary>
    public ITexture AtlasTexture { get; }

    /// <summary>
    /// Original width of the sprite before packing (useful for maintaining aspect ratios).
    /// </summary>
    public int OriginalWidth { get; }

    /// <summary>
    /// Original height of the sprite before packing.
    /// </summary>
    public int OriginalHeight { get; }

    /// <summary>
    /// Gets the normalized UV coordinates for the region (0-1 range).
    /// Useful for GPU rendering and shader-based batching.
    /// </summary>
    public UVRect UVCoordinates { get; }

    public AtlasRegion(
        string name,
        Rectangle sourceRect,
        ITexture atlasTexture,
        int originalWidth = 0,
        int originalHeight = 0)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        SourceRect = sourceRect;
        AtlasTexture = atlasTexture ?? throw new ArgumentNullException(nameof(atlasTexture));
        OriginalWidth = originalWidth > 0 ? originalWidth : sourceRect.Width;
        OriginalHeight = originalHeight > 0 ? originalHeight : sourceRect.Height;

        // Calculate normalized UV coordinates
        float u1 = (float)sourceRect.X / atlasTexture.Width;
        float v1 = (float)sourceRect.Y / atlasTexture.Height;
        float u2 = (float)(sourceRect.X + sourceRect.Width) / atlasTexture.Width;
        float v2 = (float)(sourceRect.Y + sourceRect.Height) / atlasTexture.Height;

        UVCoordinates = new UVRect(u1, v1, u2, v2);
    }
}

/// <summary>
/// Represents UV texture coordinates (normalized 0-1 range).
/// Used for GPU rendering and shader-based sprite batching.
/// </summary>
public readonly struct UVRect
{
    /// <summary>
    /// Left U coordinate (0-1).
    /// </summary>
    public float U1 { get; }

    /// <summary>
    /// Top V coordinate (0-1).
    /// </summary>
    public float V1 { get; }

    /// <summary>
    /// Right U coordinate (0-1).
    /// </summary>
    public float U2 { get; }

    /// <summary>
    /// Bottom V coordinate (0-1).
    /// </summary>
    public float V2 { get; }

    public UVRect(float u1, float v1, float u2, float v2)
    {
        U1 = u1;
        V1 = v1;
        U2 = u2;
        V2 = v2;
    }

    /// <summary>
    /// Gets the width of the UV rect (U2 - U1).
    /// </summary>
    public float Width => U2 - U1;

    /// <summary>
    /// Gets the height of the UV rect (V2 - V1).
    /// </summary>
    public float Height => V2 - V1;
}