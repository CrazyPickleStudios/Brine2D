using Brine2D.Engine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering;

/// <summary>
/// Owns the SDL3 process-level lifetime.
/// <list type="bullet">
///   <item><see cref="IHostedService.StartAsync"/> calls <c>SDL_Init</c> before any SDL-dependent service is resolved.</item>
///   <item><see cref="IDisposable.Dispose"/> calls <c>SDL_Quit</c> during DI LIFO teardown, after all SDL-dependent
///     singletons have been disposed.</item>
/// </list>
/// <c>SDL_Quit</c> intentionally lives in <see cref="Dispose"/>, not <see cref="IHostedService.StopAsync"/>,
/// because <c>StopAsync</c> runs before DI disposal — calling it there would pull the rug from under
/// services like <c>AudioService</c> that release SDL resources in their own <see cref="IDisposable.Dispose"/>.
/// </summary>
internal sealed class SDL3Lifecycle(ILogger<SDL3Lifecycle> logger) : IHostedService, IDisposable
{
    private int _initialized;
    private int _disposed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing SDL3 (Video | Events | Audio)");

        if (!SDL3.SDL.Init(SDL3.SDL.InitFlags.Video | SDL3.SDL.InitFlags.Events | SDL3.SDL.InitFlags.Audio))
        {
            var error = SDL3.SDL.GetError();
            logger.LogCritical("Failed to initialize SDL3: {Error}", error);
            throw new EngineInitializationException($"Failed to initialize SDL3: {error}");
        }

        Volatile.Write(ref _initialized, 1);
        logger.LogInformation("SDL3 initialized successfully");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        if (Volatile.Read(ref _initialized) == 1)
        {
            logger.LogInformation("Calling SDL_Quit");
            SDL3.SDL.Quit();
        }
    }
}