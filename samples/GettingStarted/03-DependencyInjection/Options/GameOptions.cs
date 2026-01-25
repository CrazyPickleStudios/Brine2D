namespace DependencyInjection.Options;

/// <summary>
///     Strongly-typed configuration options.
///     Bound from gamesettings.json using IOptions<T> pattern (just like ASP.NET!).
/// </summary>
public class GameOptions
{
    /// <summary>
    ///     Player name shown on screen.
    /// </summary>
    public string PlayerName { get; set; } = "Player";

    /// <summary>
    ///     How many points to award per second.
    /// </summary>
    public int PointsPerSecond { get; set; } = 10;
}