using System.Numerics;
using System.Text.Json.Serialization;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Rendering;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Component for sprite rendering data.
/// Contains all information needed to render a sprite.
/// Rendering logic is handled by SpriteRenderingSystem with batching.
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
    [JsonIgnore]
    public ITexture? Texture { get; set; }

    /// <summary>
    /// Source rectangle in the texture (null = entire texture).
    /// Use this for sprite sheets and texture atlases.
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
    /// Scale multiplier applied on top of the entity's TransformComponent scale.
    /// Supports non-uniform (squash-and-stretch) scaling per axis.
    /// </summary>
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// Origin/pivot point (0–1 range). Defaults to center.
    /// Overridden each frame by <see cref="Brine2D.Systems.Animation.AnimationSystem"/> when an
    /// <see cref="Brine2D.Animation.AnimatorComponent"/> is present.
    /// </summary>
    public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);

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
    /// Used by the batching system to sort sprites.
    /// </summary>
    public byte Layer { get; set; } = 0;

    /// <summary>
    /// Secondary sort key within a layer. Sprites with a lower value are drawn first (behind).
    /// Use this for Y-sorting in top-down games or to control overlap within the same layer.
    /// </summary>
    public int OrderInLayer { get; set; } = 0;

    /// <summary>
    /// Blend mode used when rendering this sprite. Defaults to standard alpha blending.
    /// </summary>
    public BlendMode BlendMode { get; set; } = BlendMode.Alpha;

    /// <summary>
    /// Texture filtering mode used when this sprite's texture is auto-loaded via
    /// <see cref="SpriteRenderingSystem.LoadTexturesAsync"/>. Defaults to
    /// <see cref="TextureScaleMode.Nearest"/> for pixel-art friendliness.
    /// Has no effect when <see cref="Texture"/> is assigned directly.
    /// </summary>
    public TextureScaleMode TextureScaleMode { get; set; } = TextureScaleMode.Nearest;

    /// <summary>
    /// Outgoing cross-fade ghosts, one per concurrent fade (base animator + each layer).
    /// Rendered by <see cref="SpriteRenderingSystem"/> as additional draw calls at fading-out
    /// opacity, producing true multi-source cross-fade blends.
    /// Set and cleared automatically by <see cref="Brine2D.Systems.Animation.AnimationSystem"/>.
    /// </summary>
    [JsonIgnore]
    public List<CrossFadeGhost> CrossFadeGhosts { get; } = new();

    /// <summary>
    /// Optional custom material for this sprite.
    /// Stored for use by custom render systems; the built-in
    /// <see cref="SpriteRenderingSystem"/> does not yet switch pipelines based on this value.
    /// </summary>
    [JsonIgnore]
    public IMaterial? Material { get; set; }
}