namespace Brine2D.Animation;

/// <summary>
/// Immutable capture of a <see cref="SpriteAnimator"/>'s runtime playback state.
/// Use <see cref="SpriteAnimator.CapturePlaybackSnapshot"/> and
/// <see cref="SpriteAnimator.RestorePlaybackSnapshot"/> to save and restore mid-frame position,
/// speed, direction, ping-pong phase, and cross-fade state atomically.
/// </summary>
/// <remarks>
/// <para>
/// The snapshot is a value-level copy: it holds the clip name (not a clip reference) so that
/// it survives clip replacement and serialization round-trips. When
/// <see cref="SpriteAnimator.RestorePlaybackSnapshot"/> is called it re-resolves the clip by
/// name; if the clip is no longer registered the restore is a no-op and returns <c>false</c>.
/// </para>
/// <para>
/// Primary use cases are save/load (rehydrating exact mid-animation state from a save file),
/// rollback netcode (rewinding all game state to a prior tick for resimulation), and
/// cutscene/ability override systems (temporarily hijacking an animator then restoring the
/// interrupted playback state, including any cross-fade in progress).
/// </para>
/// <para>
/// Cross-fade state (<see cref="CrossFadeAlpha"/>, <see cref="CrossFadeOutgoingClipName"/>,
/// <see cref="CrossFadeOutgoingFrameIndex"/>) is included so that in-progress fades survive a
/// save/load cycle. If the outgoing clip is no longer registered on restore, the cross-fade
/// is discarded and playback resumes from the incoming clip with full alpha.
/// </para>
/// </remarks>
public sealed record AnimatorPlaybackSnapshot(
    string? ClipName,
    int FrameIndex,
    float ClipTime,
    float FrameTimer,
    bool IsPlaying,
    bool IsPaused,
    bool Reversed,
    bool PingPongForward,
    bool PingPongFirstPassDone,
    int LoopCountRemaining,
    float Speed,
    float CrossFadeAlpha,
    float CrossFadeTimer,
    float CrossFadeDuration,
    float CrossFadeBaseAlpha,
    string? CrossFadeOutgoingClipName,
    int CrossFadeOutgoingFrameIndex);