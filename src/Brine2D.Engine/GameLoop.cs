using Brine2D.Core;
using Brine2D.Core.Input;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Brine2D.Engine;

/// <summary>
/// Default implementation of the game loop.
/// </summary>
public class GameLoop : IGameLoop
{
    private readonly ILogger<GameLoop> _logger;
    private readonly IGameContext _gameContext;
    private readonly ISceneManager _sceneManager;
    private readonly IInputService _inputService;
    private readonly Stopwatch _stopwatch;
    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsRunning { get; private set; }
    public int TargetFramesPerSecond { get; set; } = 60;

    public GameLoop(
        ILogger<GameLoop> logger,
        IGameContext gameContext,
        ISceneManager sceneManager,
        IInputService inputService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _gameContext = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
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
            while (IsRunning && !token.IsCancellationRequested && _gameContext.IsRunning)
            {
                // Update input (polls SDL events)
                _inputService.Update();
                
                // Check for quit event from window close
                if (_inputService.IsQuitRequested)
                {
                    _logger.LogInformation("Quit event detected");
                    _gameContext.RequestExit();
                }

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

                // Update
                _sceneManager.Update(gameTime);

                // Render
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
