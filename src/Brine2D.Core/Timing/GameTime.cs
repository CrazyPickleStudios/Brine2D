namespace Brine2D.Core.Timing;

/// <summary>
///     Immutable timing data for a single game loop tick/frame.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description><see cref="TotalSeconds" />: total elapsed time since the game started (seconds).</description></item>
///         <item><description><see cref="DeltaSeconds" />: elapsed time since the previous update/frame (seconds).</description></item>
///     </list>
///     <para>This is a readonly value type; safe to pass across systems and threads. No validation or clamping is performed—your loop/clock is responsible for sanitizing values (e.g., clamping large pauses).</para>
/// </remarks>
/// <example>
///     <code>
///     // Variable- or fixed-timestep update
///     void Update(GameTime time)
///     {
///         position += velocity * (float)time.DeltaSeconds;
///     }
///
///     // Time-based animation in draw
///     void Draw(GameTime time)
///     {
///         var t = (float)time.TotalSeconds;
///         // use t for oscillations, timers, etc.
///     }
///     </code>
/// </example>
public readonly struct GameTime
{
    /// <summary>
    ///     Total elapsed time since the game started, in seconds.
    /// </summary>
    public double TotalSeconds { get; }

    /// <summary>
    ///     Elapsed time since the previous update/frame, in seconds.
    /// </summary>
    public double DeltaSeconds { get; }

    /// <summary>
    ///     Creates a new immutable snapshot of game timing values.
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time since game start (seconds).</param>
    /// <param name="deltaSeconds">Elapsed time since previous update/frame (seconds).</param>
    public GameTime(double totalSeconds, double deltaSeconds)
    {
        // Keep this lightweight: no validation or clamping; upstream clock is responsible.
        TotalSeconds = totalSeconds;
        DeltaSeconds = deltaSeconds;
    }
}