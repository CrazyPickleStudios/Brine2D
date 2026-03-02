using Brine2D.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Default implementation of game context.
/// </summary>
internal sealed class GameContext : IGameContext
{
    private readonly ILogger<GameContext> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public GameTime GameTime { get; private set; } = new GameTime(TimeSpan.Zero, TimeSpan.Zero);
    private volatile bool _isRunning = true;
    public bool IsRunning => _isRunning;

    public GameContext(ILogger<GameContext> logger, IHostApplicationLifetime lifetime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

        _lifetime.ApplicationStopping.Register(() => _isRunning = false);
    }

    public void RequestExit()
    {
        _logger.LogInformation("Exit requested");
        _isRunning = false;
        _lifetime.StopApplication();
    }

    void IGameContext.UpdateGameTime(GameTime gameTime) => GameTime = gameTime;
}