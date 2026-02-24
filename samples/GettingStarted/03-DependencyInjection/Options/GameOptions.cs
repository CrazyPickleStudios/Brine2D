namespace DependencyInjection.Options;

/// <summary>Game-specific settings. Register as a singleton in Program.cs.</summary>
public class GameOptions
{
    /// <summary>Player name shown on screen.</summary>
    public string PlayerName { get; set; } = "Player";

    /// <summary>How many points to award per second.</summary>
    public int PointsPerSecond { get; set; } = 10;
}