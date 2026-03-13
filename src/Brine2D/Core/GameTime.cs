namespace Brine2D.Core;

/// <summary>
///     Represents timing information for the game loop.
/// </summary>
/// <param name="TotalTime">The total elapsed time since the game started.</param>
/// <param name="ElapsedTime">The elapsed time since the last frame.</param>
/// <param name="FrameCount">The total number of frames rendered since the game started.</param>
/// <param name="IsTimeClamped">
/// Whether <see cref="ElapsedTime"/> was clamped this frame.
/// True when the raw delta exceeded <c>MaxDeltaTime</c> (e.g., after a debugger pause).
/// Systems that synchronize with wall-clock time (audio, animation) can use this to skip
/// resynchronization rather than jumping forward.
/// </param>
public readonly record struct GameTime(
    TimeSpan TotalTime,
    TimeSpan ElapsedTime,
    long FrameCount = 0,
    bool IsTimeClamped = false)
{
    /// <summary>Gets the elapsed time as seconds (convenience property).</summary>
    public double DeltaTime => ElapsedTime.TotalSeconds;

    /// <summary>Gets the total time as seconds (convenience property).</summary>
    public double TotalSeconds => TotalTime.TotalSeconds;
}