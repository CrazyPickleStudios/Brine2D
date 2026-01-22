using Brine2D.Core;
using Brine2D.Hosting;
using Brine2D.Input;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Brine2D.Engine;

/// <summary>
/// Default implementation of the game loop.
/// Processes events, updates game state, and renders frames.
/// </summary>
public class GameLoop : IGameLoop
{
    private readonly ILogger<GameLoop> _logger;
    private readonly IGameContext _gameContext;
    private readonly ISceneManager _sceneManager;
    private readonly IInputService _inputService;
    private readonly InputLayerManager? _inputLayerManager;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IEventPump? _eventPump; 
    private readonly Stopwatch _stopwatch;
    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsRunning { get; private set; }
    public int TargetFramesPerSecond { get; set; } = 60;

    public GameLoop(
        ILogger<GameLoop> logger,
        IGameContext gameContext,
        ISceneManager sceneManager,
        IInputService inputService,
        IHostApplicationLifetime applicationLifetime,
        InputLayerManager? inputLayerManager = null,
        IEventPump? eventPump = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _gameContext = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        _inputLayerManager = inputLayerManager;
        _eventPump = eventPump;
        _stopwatch = new Stopwatch();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Game loop is already running");
            return;
        }

        _logger.LogInformation("Starting game loop");
        IsRunning = true;

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cancellationTokenSource.Token;

        var targetFrameTime = TimeSpan.FromSeconds(1.0 / TargetFramesPerSecond);
        var totalTime = TimeSpan.Zero;

        _stopwatch.Start();
        var lastFrameTime = _stopwatch.Elapsed;

        try
        {
            while (IsRunning
                && !token.IsCancellationRequested
                && _gameContext.IsRunning
                && !_applicationLifetime.IsExitRequested)
            {
                // Update input state (clears per-frame data, updates mouse position)
                _inputService.Update();

                // Process platform events FIRST (window, input, etc.)
                _eventPump?.ProcessEvents();
                
                // Process input layers (middleware pattern)
                _inputLayerManager?.ProcessInput();

                var currentTime = _stopwatch.Elapsed;
                var elapsedTime = currentTime - lastFrameTime;
                lastFrameTime = currentTime;
                totalTime += elapsedTime;

                var gameTime = new GameTime(totalTime, elapsedTime);

                // Update game context time
                if (_gameContext is GameContext context)
                {
                    context.GameTime = gameTime;
                }

                // Update game logic
                _sceneManager.Update(gameTime);

                // Render frame
                _sceneManager.Render(gameTime);

                // Frame limiting
                var frameTime = _stopwatch.Elapsed - currentTime;
                if (frameTime < targetFrameTime)
                {
                    var sleepTime = targetFrameTime - frameTime;
                    if (sleepTime > TimeSpan.FromMilliseconds(1))
                    {
                        Thread.Sleep(sleepTime - TimeSpan.FromMilliseconds(1));
                    }

                    // Spin for precision timing on the last millisecond
                    while (_stopwatch.Elapsed - currentTime < targetFrameTime)
                    {
                        Thread.SpinWait(100);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Game loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in game loop");
            throw;
        }
        finally
        {
            _stopwatch.Stop();
            IsRunning = false;
            _logger.LogInformation("Game loop stopped");
        }

        await Task.CompletedTask;
    }

    public void Stop()
    {
        _logger.LogInformation("Stopping game loop");
        IsRunning = false;
        _cancellationTokenSource?.Cancel();
    }
}