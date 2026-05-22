namespace Brine2D.Animation;

/// <summary>
/// Controls how an <see cref="AnimationClip"/> loops.
/// </summary>
public enum PlaybackMode
{
    /// <summary>
    /// The clip plays once, freezes on the last frame, and sets
    /// <see cref="SpriteAnimator.IsFinished"/> to <c>true</c>. Symmetric counterpart to
    /// <see cref="OnceHoldFirst"/>; identical in behaviour to <see cref="Once"/> but expresses
    /// the intent of holding the last frame more clearly.
    /// When <see cref="SpriteAnimator.Reversed"/> is <c>true</c>, completion freezes on the
    /// <em>first</em> frame (index 0), because that is the logical "last" frame of a reversed pass.
    /// </summary>
    OnceHoldLast,

    /// <summary>
    /// The clip plays once and freezes on the first frame after completion.
    /// When <see cref="SpriteAnimator.Reversed"/> is <c>true</c>, playback runs from the last
    /// frame to the first; completion freezes on the <em>last</em> frame (index Count − 1),
    /// because that is the logical "first" frame of a reversed pass.
    /// </summary>
    OnceHoldFirst,
    
    /// <summary>
    /// The clip plays once and then clears the current frame entirely on completion, making
    /// <see cref="SpriteAnimator.CurrentFrame"/> return <c>null</c>. Useful for VFX clips that
    /// should disappear rather than freeze on the last frame.
    /// <see cref="SpriteAnimator.OnAnimationComplete"/> fires, then
    /// <see cref="SpriteAnimator.OnStopped"/> fires (unless a queued animation takes over).
    /// </summary>
    OnceStop,

    /// <summary>The clip loops indefinitely, jumping back to the first frame after the last.</summary>
    Loop,

    /// <summary>The clip plays forward to the end then backward to the start, repeating indefinitely.</summary>
    PingPong,

    /// <summary>
    /// The clip plays one full cycle (forward then backward, or backward then forward when
    /// <see cref="SpriteAnimator.Reversed"/> is <c>true</c>), then stops on the first frame of
    /// the final pass. <see cref="SpriteAnimator.OnAnimationComplete"/> fires at the end of the
    /// second pass. Queued animations and the state machine default state trigger normally.
    /// </summary>
    PingPongOnce,
}