using Brine2D.Engine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Desktop;

/// <summary>
///     Background hosted service that runs the game loop.
/// </summary>
public sealed class HostedGame : IHostedService
{
    /// <summary>
    ///     Logger for hosted game lifecycle events.
    /// </summary>
    private readonly ILogger<HostedGame> _logger;

    /// <summary>
    ///     The game loop to execute within the hosted service.
    /// </summary>
    private readonly IGameLoop _loop;

    /// <summary>
    ///     Lifetime events for the host application.
    /// </summary>
    private readonly IHostApplicationLifetime _lifetime;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HostedGame" /> class.
    /// </summary>
    /// <param name="loop">The game loop implementation.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="lifetime">The host application lifetime.</param>
    public HostedGame(IGameLoop loop, ILogger<HostedGame> logger, IHostApplicationLifetime lifetime)
    {
        _loop = loop;
        _logger = logger;
        _lifetime = lifetime;
    }

    /// <summary>
    ///     Executes the game loop as a background task.
    /// </summary>
    /// <param name="cancellationToken">Token used to signal cancellation.</param>
    /// <returns>A completed task once the loop finishes.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Announce start of hosted game execution.
        _logger.LogInformation("Hosted game starting.");

        await _loop.RunAsync(cancellationToken).ConfigureAwait(false);

        // Announce completion of hosted game execution.
        _logger.LogInformation("Hosted game finished.");

        // Request shutdown after the loop exits
        _lifetime.StopApplication();
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="cancellationToken">Token used to signal cancellation.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}