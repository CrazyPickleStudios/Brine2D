using Brine2D.Engine;
using Brine2D.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlGameLoop : IGameLoop
{
    private readonly IGameContext _context;
    private readonly IGame _game;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SdlGameLoop> _logger;
    private readonly LoopOptions _loopOptions;
    private readonly SdlRenderer _renderer;
    private readonly SdlInput _sdlInput;

    public SdlGameLoop
    (
        IGame game,
        IGameContext context,
        IHostApplicationLifetime lifetime,
        IOptions<LoopOptions> loopOptions,
        ILogger<SdlGameLoop> logger,
        SdlRenderer renderer,
        SdlInput sdlInput)
    {
        _game = game;
        _context = context;
        _lifetime = lifetime;
        _loopOptions = loopOptions.Value;
        _logger = logger;
        _renderer = renderer;
        _sdlInput = sdlInput;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SDL game loop.");

        try
        {
            await _game.Initialize(_context).ConfigureAwait(false);
            _logger.LogInformation("Game initialized successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Game initialization canceled.");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception during game initialization.");
            return;
        }

        var freq = SDL.GetPerformanceFrequency();
        var last = SDL.GetPerformanceCounter();

        double accumulator = 0;

        var targetStep = _loopOptions is { UseFixedStep: true, FixedStepSeconds: > 0 }
            ? _loopOptions.FixedStepSeconds
            : 0;

        while (!cancellationToken.IsCancellationRequested && !_lifetime.ApplicationStopping.IsCancellationRequested)
        {
            _sdlInput.BeginFrame();

            bool continueLoop;
            try
            {
                continueLoop = PumpEvents(ref cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Event pump canceled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while pumping events.");
                break;
            }

            if (!continueLoop)
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

            try
            {
                if (targetStep > 0)
                {
                    accumulator += delta;

                    while (accumulator >= targetStep && !cancellationToken.IsCancellationRequested)
                    {
                        _game.Update(new GameTime(now / (double)freq, targetStep));

                        accumulator -= targetStep;
                    }
                }
                else
                {
                    _game.Update(new GameTime(now / (double)freq, delta));
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Update canceled.");

                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during Update.");

                break;
            }

            try
            {
                _renderer.DrainWorkQueue();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while draining render-thread work.");
            }

            try
            {
                _game.Render(_renderer);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Render canceled.");

                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during Render.");

                break;
            }

            _sdlInput.EndFrame();

            if (_loopOptions.MaxFps is > 0)
            {
                var targetMs = 1000.0 / _loopOptions.MaxFps.Value;

                SDL.Delay((uint)Math.Max(0, targetMs));
            }
            else
            {
                SDL.Delay(1);
            }
        }

        _logger.LogInformation("SDL game loop exiting.");
    }

    private bool PumpEvents(ref CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && SDL.PollEvent(out var e))
        {
            switch ((SDL.EventType)e.Type)
            {
                case SDL.EventType.Quit:
                    _lifetime.StopApplication();

                    return false;

                case SDL.EventType.KeyDown:
                    _sdlInput.Keyboard.OnKeyDown(e.Key.Key, e.Key.Scancode);

                    break;

                case SDL.EventType.KeyUp:
                    _sdlInput.Keyboard.OnKeyUp(e.Key.Key, e.Key.Scancode);

                    break;

                case SDL.EventType.MouseMotion:
                    _sdlInput.Mouse.OnMouseMotion(e.Motion.X, e.Motion.Y);

                    break;

                case SDL.EventType.MouseButtonDown:
                    _sdlInput.Mouse.OnMouseButtonDown(e.Button.Button);

                    break;

                case SDL.EventType.MouseButtonUp:
                    _sdlInput.Mouse.OnMouseButtonUp(e.Button.Button);

                    break;

                case SDL.EventType.MouseWheel:
                    _sdlInput.Mouse.OnMouseWheel(e.Wheel.X, e.Wheel.Y);

                    break;

                case SDL.EventType.GamepadAdded:
                    _sdlInput.Gamepads.OnDeviceAdded(e.GDevice.Which);

                    break;

                case SDL.EventType.GamepadRemoved:
                    _sdlInput.Gamepads.OnDeviceRemoved(e.GDevice.Which);

                    break;

                case SDL.EventType.GamepadAxisMotion:
                    _sdlInput.Gamepads.OnAxisMotion(e.GAxis.Which, (SDL.GamepadAxis)e.GAxis.Axis, e.GAxis.Value);

                    break;

                case SDL.EventType.GamepadButtonDown:
                    _sdlInput.Gamepads.OnButtonDown(e.GButton.Which, (SDL.GamepadButton)e.GButton.Button);

                    break;

                case SDL.EventType.GamepadButtonUp:
                    _sdlInput.Gamepads.OnButtonUp(e.GButton.Which, (SDL.GamepadButton)e.GButton.Button);

                    break;

                case SDL.EventType.FingerDown:
                    _sdlInput.Touch.OnFingerDown(e.TFinger.TouchID, e.TFinger.X, e.TFinger.Y);

                    break;

                case SDL.EventType.FingerUp:
                    _sdlInput.Touch.OnFingerUp(e.TFinger.TouchID, e.TFinger.X, e.TFinger.Y);

                    break;

                case SDL.EventType.FingerMotion:
                    _sdlInput.Touch.OnFingerMotion(e.TFinger.TouchID, e.TFinger.X, e.TFinger.Y);

                    break;

                case SDL.EventType.TextInput:
                    _sdlInput.TextInput.OnTextInput(e.Text.GetText());

                    break;

                case SDL.EventType.TextEditing:
                    _sdlInput.TextInput.OnTextEditing(e.Edit.GetText(), e.Edit.Start, e.Edit.Length);

                    break;
            }
        }

        return !cancellationToken.IsCancellationRequested;
    }
}