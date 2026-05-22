namespace Brine2D.Animation;

/// <summary>
/// Contextual data passed to a clip event callback.
/// </summary>
/// <param name="EventName">The name of the event that fired.</param>
/// <param name="ClipName">The name of the animation clip that owns the event.</param>
/// <param name="Time">The time offset (in seconds) at which the event is registered.</param>
/// <param name="NormalizedTime">The normalized playback position [0, 1] when the event fired.</param>
public sealed record ClipEventArgs(string EventName, string ClipName, float Time, float NormalizedTime);