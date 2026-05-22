namespace Brine2D.Animation;

/// <summary>
/// An immutable snapshot of an <see cref="AnimationLayer"/>'s runtime configuration.
/// Capture via <see cref="AnimationLayer.CaptureSnapshot"/> and restore via
/// <see cref="AnimationLayer.RestoreSnapshot"/>. Useful for cutscenes or ability-override
/// systems that need to temporarily reconfigure a layer and then cleanly revert it.
/// </summary>
/// <param name="Weight">The blend weight [0, 1] at the time of capture.</param>
/// <param name="Mask">The <see cref="AnimationLayerMask"/> at the time of capture.</param>
/// <param name="BlendMode">The <see cref="AnimationLayerBlendMode"/> at the time of capture.</param>
/// <param name="IsEnabled">Whether the layer was enabled at the time of capture.</param>
public sealed record AnimationLayerSnapshot(
    float Weight,
    AnimationLayerMask Mask,
    AnimationLayerBlendMode BlendMode,
    bool IsEnabled);