using System.Numerics;
using Brine2D.Core.Animation;
using Brine2D.ECS;
using Brine2D.Rendering;

namespace Brine2D.Rendering.ECS;

/// <summary>
/// Component for sprite rendering data.
/// Contains all information needed to render a sprite.
/// Rendering logic is handled by SpriteRenderingSystem.
/// </summary>
public class SpriteComponent : Component
{
    /// <summary>
    /// Path to the texture (for loading).
    /// </summary>
    public string TexturePath { get; set; } = string.Empty;

    /// <summary>
    /// Loaded texture reference (set by SpriteRenderingSystem).
    /// </summary>
    public ITexture? Texture { get; set; }

    /// <summary>
    /// Source rectangle in the texture (null = entire texture).
    /// </summary>
    public Rectangle? SourceRect { get; set; }

    /// <summary>
    /// Tint color applied to the sprite.
    /// </summary>
    public Color Tint { get; set; } = Color.White;

    /// <summary>
    /// Draw offset from transform position (in pixels).
    /// </summary>
    public Vector2 Offset { get; set; }

    /// <summary>
    /// Scale multiplier (applied to transform scale).
    /// </summary>
    public float Scale { get; set; } = 1.0f;

    /// <summary>
    /// Whether to flip the sprite horizontally.
    /// </summary>
    public bool FlipX { get; set; }

    /// <summary>
    /// Whether to flip the sprite vertically.
    /// </summary>
    public bool FlipY { get; set; }

    /// <summary>
    /// Rendering layer/order (higher = drawn on top).
    /// </summary>
    public int Layer { get; set; } = 0;
}