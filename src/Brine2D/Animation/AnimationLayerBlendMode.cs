namespace Brine2D.Animation;

/// <summary>
/// Controls how an <see cref="AnimationLayer"/> combines its frame values with the values
/// already written to the <see cref="Brine2D.Systems.Rendering.SpriteComponent"/> by lower-priority
/// layers.
/// </summary>
public enum AnimationLayerBlendMode
{
    /// <summary>
    /// The layer overwrites (or lerps via <see cref="AnimationLayer.Weight"/>) the sprite's
    /// current values. This is the default behaviour.
    /// </summary>
    Override,

    /// <summary>
    /// The layer's tint RGB channels are added to the sprite's current tint. Alpha is lerped
    /// by <see cref="AnimationLayer.Weight"/>. Useful for glow, flash, or hit-effect overlays
    /// that should intensify an existing colour rather than replace it.
    /// <para>
    /// Non-tint properties (<see cref="AnimationLayerMask.SourceRect"/>,
    /// <see cref="AnimationLayerMask.Origin"/>, <see cref="AnimationLayerMask.FlipX"/>,
    /// <see cref="AnimationLayerMask.FlipY"/>, <see cref="AnimationLayerMask.Texture"/>) fall
    /// back to <see cref="Override"/> behaviour when <see cref="AnimationLayerMask"/> includes
    /// them, since addition is not meaningful for those properties.
    /// </para>
    /// </summary>
    Additive,
}