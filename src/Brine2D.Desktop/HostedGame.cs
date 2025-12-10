using Brine2D.Engine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Desktop;

public sealed class HostedGame : IHostedService
{
    private readonly ILogger<HostedGame> _logger;
    private readonly IGameLoop _loop;
    private readonly IHostApplicationLifetime _lifetime;

    public HostedGame(IGameLoop loop, ILogger<HostedGame> logger, IHostApplicationLifetime lifetime)
    {
        _loop = loop;
        _logger = logger;
        _lifetime = lifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hosted game starting.");

        await _loop.RunAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Hosted game finished.");

        _lifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}