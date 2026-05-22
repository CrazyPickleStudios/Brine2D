namespace Brine2D.Animation;

/// <summary>
/// An immutable snapshot of an <see cref="AnimationBlendSelector2D"/>'s runtime state.
/// Capture via <see cref="AnimationBlendSelector2D.CaptureSnapshot"/> and restore via
/// <see cref="AnimationBlendSelector2D.RestoreSnapshot"/>. Node definitions are not captured.
/// </summary>
/// <param name="X">The X blend parameter at the time of capture.</param>
/// <param name="Y">The Y blend parameter at the time of capture.</param>
/// <param name="ActiveClip">The active clip name at the time of capture, or <c>null</c>.</param>
/// <param name="CrossFadeDuration">The cross-fade duration at the time of capture.</param>
/// <param name="RespectNonLoopingClips">Whether non-looping clip respect was enabled.</param>
/// <param name="AllowZeroSpeed">Whether zero-speed was allowed at the time of capture.</param>
public sealed record AnimationBlendSelector2DSnapshot(
    float X,
    float Y,
    string? ActiveClip,
    float CrossFadeDuration,
    bool RespectNonLoopingClips,
    bool AllowZeroSpeed);