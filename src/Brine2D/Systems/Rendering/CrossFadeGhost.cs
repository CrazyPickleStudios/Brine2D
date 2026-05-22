using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Captures the outgoing sprite state for a cross-fade blend.
/// Held on <see cref="SpriteComponent.CrossFadeGhosts"/> while a fade is in progress and rendered
/// by <see cref="SpriteRenderingSystem"/> as a second draw call at fading-out opacity.
/// </summary>
public sealed class CrossFadeGhost
{
    /// <summary>Outgoing pre-loaded texture. Takes priority over <see cref="TexturePath"/>.</summary>
    public ITexture? Texture { get; set; }

    /// <summary>
    /// Outgoing texture path. Used as a fallback when <see cref="Texture"/> is null, resolved
    /// against <see cref="SpriteRenderingSystem"/>'s texture cache.
    /// </summary>
    public string? TexturePath { get; set; }

    /// <summary>Outgoing source rectangle.</summary>
    public Rectangle? SourceRect { get; set; }

    /// <summary>Outgoing origin.</summary>
    public Vector2 Origin { get; set; }

    /// <summary>
    /// Outgoing draw offset (from <see cref="Brine2D.Animation.SpriteFrame.DrawOffset"/>).
    /// Applied to the ghost's world position so that Aseprite-trimmed frames render at the
    /// correct canvas position during a cross-fade.
    /// </summary>
    public Vector2 DrawOffset { get; set; }

    /// <summary>Outgoing tint (RGB channels). Alpha is driven by <see cref="Alpha"/>.</summary>
    public Color Tint { get; set; } = Color.White;

    /// <summary>Outgoing flip state.</summary>
    public bool FlipX { get; set; }

    /// <summary>Outgoing flip state.</summary>
    public bool FlipY { get; set; }

    /// <summary>
    /// Opacity of the ghost draw [0, 1]. Driven each frame by
    /// <see cref="Brine2D.Systems.Animation.AnimationSystem"/> as <c>1 - CrossFadeAlpha</c>.
    /// </summary>
    public float Alpha { get; set; }

    /// <summary>
    /// The sprite's <see cref="SpriteComponent.Tint"/> alpha at the moment the cross-fade began
    /// [0, 1]. Restored to <see cref="SpriteComponent.Tint"/> when the fade completes, so that
    /// any pre-existing semi-transparent tint (e.g. a hit-flash) is not clobbered.
    /// </summary>
    public float BaseAlpha { get; set; } = 1f;
}