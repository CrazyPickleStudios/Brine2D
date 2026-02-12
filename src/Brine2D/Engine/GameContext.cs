using Brine2D.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Default implementation of game context.
/// </summary>
/// /// TODO: Think about getting rid of this.
internal sealed class GameContext : IGameContext
{
    private readonly ILogger<GameContext> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public IServiceProvider Services { get; }
    public GameTime GameTime { get; internal set; } = new GameTime(TimeSpan.Zero, TimeSpan.Zero);
    public bool IsRunning { get; private set; }

    public GameContext(ILogger<GameContext> logger, IServiceProvider services, IHostApplicationLifetime lifetime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        _lifetime = lifetime;
        IsRunning = true;
    }

    public void RequestExit()
    {
        _logger.LogInformation("Exit requested");
        _lifetime.StopApplication();
    }
}