using Brine2D.Core;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Default implementation of game context.
/// </summary>
public class GameContext : IGameContext
{
    private readonly ILogger<GameContext> _logger;

    public IServiceProvider Services { get; }
    public GameTime GameTime { get; internal set; } = new GameTime(TimeSpan.Zero, TimeSpan.Zero);
    public bool IsRunning { get; private set; }

    public GameContext(ILogger<GameContext> logger, IServiceProvider services)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        IsRunning = true;
    }

    public void RequestExit()
    {
        _logger.LogInformation("Exit requested");
        IsRunning = false;
    }
}