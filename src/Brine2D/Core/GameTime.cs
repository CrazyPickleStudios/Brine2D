namespace Brine2D.Core;

/// <summary>
///     Represents timing information for the game loop.
/// </summary>
public readonly struct GameTime
{
    /// <summary>
    ///     Gets the total elapsed time since the game started.
    /// </summary>
    public TimeSpan TotalTime { get; init; }

    /// <summary>
    ///     Gets the elapsed time since the last frame.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>
    ///     Gets the elapsed time as seconds (convenience property).
    /// </summary>
    public double DeltaTime => ElapsedTime.TotalSeconds;

    /// <summary>
    ///     Gets the total time as seconds (convenience property).
    /// </summary>
    public double TotalSeconds => TotalTime.TotalSeconds;

    public GameTime(TimeSpan totalTime, TimeSpan elapsedTime)
    {
        TotalTime = totalTime;
        ElapsedTime = elapsedTime;
    }
}