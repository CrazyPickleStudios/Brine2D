using Microsoft.Extensions.Logging;
using SDL3;

namespace Brine2D.SDL3;

public sealed class SdlInitializer : IDisposable
{
    private readonly ILogger<SdlInitializer> _logger;
    private bool _initialized;

    public SdlInitializer(ILogger<SdlInitializer> logger)
    {
        _logger = logger;
    }

    public void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events))
        {
            throw new InvalidOperationException($"SDL_Init failed: {SDL.GetError()}");
        }
        _logger.LogInformation("SDL initialized.");
        _initialized = true;
    }

    public void Dispose()
    {
        if (_initialized)
        {
            SDL.Quit();
            _initialized = false;
        }
    }
}