namespace Brine2D.Animation;

/// <summary>
/// A named callback attached to an <see cref="AnimationClip"/> at a specific time offset.
/// Fired by <see cref="SpriteAnimator"/> when playback crosses the event's <see cref="Time"/>.
/// </summary>
/// <param name="Name">Identifier for this event (e.g., "footstep", "hitbox_on").</param>
/// <param name="Time">Time in seconds from the clip start at which this event fires.</param>
/// <param name="Callback">The action to invoke when the event fires.</param>
/// <param name="FireBothDirections">
/// When <c>true</c> and the owning clip uses <see cref="PlaybackMode.PingPong"/>, the event also
/// fires during the backward sweep. Ignored for non-ping-pong clips.
/// </param>
/// <param name="FrameIndex">
/// When non-null this event was registered via <see cref="AnimationClip.AddEventAtFrame"/> and
/// its <see cref="Time"/> is automatically re-resolved whenever any frame's
/// <see cref="SpriteFrame.Duration"/> changes.
/// </param>
public sealed record ClipEvent(
    string Name,
    float Time,
    Action<ClipEventArgs> Callback,
    bool FireBothDirections = false,
    int? FrameIndex = null);