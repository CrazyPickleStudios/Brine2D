namespace Brine2D.Engine;

public readonly struct GameTime
{
    public GameTime(double totalSeconds, double deltaSeconds)
    {
        TotalSeconds = totalSeconds;
        DeltaSeconds = deltaSeconds;
    }

    public double TotalSeconds { get; }
    public double DeltaSeconds { get; }
}