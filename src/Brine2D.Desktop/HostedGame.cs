using Brine2D.Engine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Desktop;

public sealed class HostedGame : BackgroundService
{
    private readonly ILogger<HostedGame> _logger;
    private readonly IGameLoop _loop;

    public HostedGame(IGameLoop loop, ILogger<HostedGame> logger)
    {
        _loop = loop;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hosted game starting.");

        _loop.Run();

        _logger.LogInformation("Hosted game finished.");

        return Task.CompletedTask;
    }
}