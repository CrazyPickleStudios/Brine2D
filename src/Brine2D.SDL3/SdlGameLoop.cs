using Brine2D.Abstractions;
using Brine2D.Engine;
using Brine2D.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SDL3;

namespace Brine2D.SDL3;

public sealed class SdlGameLoop : IGameLoop
{
    private readonly IGameContext _context;
    private readonly IGame _game;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SdlGameLoop> _logger;
    private readonly LoopOptions _loopOptions;
    private readonly SdlInput? _sdlInput;

    public SdlGameLoop
    (
        IGame game,
        IGameContext context,
        IHostApplicationLifetime lifetime,
        IOptions<LoopOptions> loopOptions,
        ILogger<SdlGameLoop> logger,
        SdlInput sdlInput
    )
    {
        _game = game;
        _context = context;
        _lifetime = lifetime;
        _loopOptions = loopOptions.Value;
        _logger = logger;
        _sdlInput = sdlInput;
    }

    public void Run()
    {
        _logger.LogInformation("Starting SDL game loop.");

        _game.Initialize(_context);

        var freq = SDL.GetPerformanceFrequency();
        var last = SDL.GetPerformanceCounter();

        double accumulator = 0;

        var targetStep = _loopOptions is { UseFixedStep: true, FixedStepSeconds: > 0 }
            ? _loopOptions.FixedStepSeconds
            : 0;

        while (!_lifetime.ApplicationStopping.IsCancellationRequested)
        {
            if (!PumpEvents())
            {
                break;
            }

            var now = SDL.GetPerformanceCounter();
            var delta = (now - last) / (double)freq;

            if (delta < 0)
            {
                delta = 0;
            }

            last = now;

            if (targetStep > 0)
            {
                accumulator += delta;

                while (accumulator >= targetStep)
                {
                    _game.Update(new GameTime(now / (double)freq, targetStep));

                    accumulator -= targetStep;
                }
            }
            else
            {
                _game.Update(new GameTime(now / (double)freq, delta));
            }

            _game.Render((IRenderContext)_context.Services.GetService(typeof(IRenderContext))!);

            if (_loopOptions.MaxFps is > 0)
            {
                var targetMs = 1000.0 / _loopOptions.MaxFps.Value;
                SDL.Delay((uint)Math.Max(0, targetMs));
            }
        }

        _logger.LogInformation("SDL game loop exiting.");
    }

    private bool PumpEvents()
    {
        while (SDL.PollEvent(out var e))
        {
            switch ((SDL.EventType)e.Type)
            {
                case SDL.EventType.Quit:
                    _lifetime.StopApplication();
                    return false;

                case SDL.EventType.KeyDown:
                    _sdlInput?.OnKeyDown(e.Key.Key, e.Key.Scancode);
                    break;

                case SDL.EventType.KeyUp:
                    _sdlInput?.OnKeyUp(e.Key.Key, e.Key.Scancode);
                    break;
            }
        }

        _sdlInput?.CommitFrame();

        return true;
    }
}