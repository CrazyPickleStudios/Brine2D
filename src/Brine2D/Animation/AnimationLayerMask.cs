namespace Brine2D.Animation;

/// <summary>
/// Controls which <see cref="Brine2D.Systems.Rendering.SpriteComponent"/> properties an
/// <see cref="AnimationLayer"/> is allowed to overwrite each frame.
/// </summary>
[Flags]
public enum AnimationLayerMask
{
    None = 0,
    SourceRect = 1 << 0,
    Origin = 1 << 1,
    FlipX = 1 << 2,
    FlipY = 1 << 3,
    Tint = 1 << 4,
    Texture = 1 << 5,

    /// <summary>
    /// The default layer mask applied to new <see cref="AnimationLayer"/> instances:
    /// <see cref="SourceRect"/> | <see cref="Origin"/>.
    /// Excludes <see cref="Tint"/>, <see cref="FlipX"/>, <see cref="FlipY"/>, and
    /// <see cref="Texture"/> to avoid unintentionally clobbering the base sprite values.
    /// Add those flags explicitly when your layer drives them.
    /// </summary>
    Default = SourceRect | Origin,

    /// <summary>All visual properties.</summary>
    All = SourceRect | Origin | FlipX | FlipY | Tint | Texture
}