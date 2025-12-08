namespace Brine2D.Engine;

/// <summary>
///     Represents timing information for a single game tick/update.
///     Holds the total elapsed time since the start of the game and the
///     amount of time elapsed since the previous update.
/// </summary>
public readonly struct GameTime
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GameTime" /> struct.
    /// </summary>
    /// <param name="totalSeconds">The total elapsed time since the game started, in seconds.</param>
    /// <param name="deltaSeconds">The elapsed time since the last update, in seconds.</param>
    public GameTime(double totalSeconds, double deltaSeconds)
    {
        TotalSeconds = totalSeconds;
        DeltaSeconds = deltaSeconds;
    }

    /// <summary>
    ///     Gets the total elapsed time since the game started, in seconds.
    /// </summary>
    public double TotalSeconds { get; }

    /// <summary>
    ///     Gets the elapsed time since the previous update, in seconds.
    /// </summary>
    public double DeltaSeconds { get; }
}