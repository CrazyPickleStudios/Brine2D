using Microsoft.Extensions.Hosting;

namespace Brine2D.Hosting;

/// <summary>
/// Validates <see cref="Brine2DOptions"/> at host startup.
/// Ensures configuration is validated when <see cref="GameApplicationBuilder"/> is not used
/// (e.g., standalone DI or test hosts).
/// </summary>
internal sealed class Brine2DOptionsValidatorService(Brine2DOptions options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        options.Validate();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}