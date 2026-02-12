using Brine2D.Core;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Brine2D.Engine;

/// <summary>
/// Game loop with non-blocking scene transitions and async asset loading support.
/// Uses synchronous loop pattern (like Unity/Godot) with main thread dispatcher for GPU operations.
/// Loading screens render while scenes load in background - window never freezes!
/// </summary>
internal sealed class GameLoop
{
    private readonly ILogger<GameLoop> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IGameContext _gameContext;
    private readonly ISceneManager _sceneManager;
    private readonly IInputContext _inputService;
    private readonly InputLayerManager? _inputLayerManager;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IEventPump? _eventPump;
    private readonly IMainThreadDispatcher? _mainThreadDispatcher;
    private readonly Stopwatch _stopwatch;
    private CancellationTokenSource? _cancellationTokenSource;

    private ulong _frameCount = 0;
    
    public int TargetFramesPerSecond { get; set; } = 60;

    public GameLoop(
        ILogger<GameLoop> logger,
        ILoggerFactory loggerFactory,
        IGameContext gameContext,
        ISceneManager sceneManager,
        IInputContext inputService,
        IHostApplicationLifetime applicationLifetime,
        InputLayerManager? inputLayerManager = null,
        IEventPump? eventPump = null,
        IMainThreadDispatcher? mainThreadDispatcher = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _gameContext = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        _inputLayerManager = inputLayerManager;
        _eventPump = eventPump;
        _mainThreadDispatcher = mainThreadDispatcher;
        _stopwatch = new Stopwatch();
    }

    /// <summary>
    /// Starts the game loop on the current thread.
    /// Returns a Task for async/await compatibility, but runs synchronously internally.
    /// </summary>
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Game loop is already running");
            return Task.CompletedTask;
        }

        // Run synchronous loop (Unity/Godot pattern)
        // This ensures SDL stays on the same thread
        RunGameLoopSync(cancellationToken);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// The actual synchronous game loop (like Unity's Update loop).
    /// Processes main thread work queue for GPU operations from async loading.
    /// Scene transitions happen in background - loading screens keep rendering!
    /// </summary>
    private void RunGameLoopSync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cancellationTokenSource.Token;

        var targetFrameTime = TimeSpan.FromSeconds(1.0 / TargetFramesPerSecond);
        var totalTime = TimeSpan.Zero;
        _stopwatch.Start();
        var lastFrameTime = _stopwatch.Elapsed;

        Task? pendingSceneTransition = null;

        try
        {
            while (!token.IsCancellationRequested)
            {
                var frameStartTime = _stopwatch.Elapsed;

                // 1. Process main thread work queue
                _mainThreadDispatcher?.ProcessQueue();

                // 2. Check if scene transition completed
                if (pendingSceneTransition != null)
                {
                    if (pendingSceneTransition.IsCompleted)
                    {
                        if (pendingSceneTransition.IsFaulted)
                        {
                            _logger.LogError(pendingSceneTransition.Exception, "Scene transition failed");
                        }
                        pendingSceneTransition = null;
                    }
                }

                // 3. Update input
                _inputService.Update();

                // 4. Process events
                _eventPump?.ProcessEvents();

                // 5. Process input layers
                _inputLayerManager?.ProcessInput();

                // 6. Calculate time
                var currentTime = _stopwatch.Elapsed;
                var elapsedTime = currentTime - lastFrameTime;
                lastFrameTime = currentTime;
                totalTime += elapsedTime;
                var gameTime = new GameTime(totalTime, elapsedTime, _frameCount++);

                // 7. Update game context
                if (_gameContext is GameContext context)
                    context.GameTime = gameTime;

                // 8. Begin frame
                if (_sceneManager is SceneManager sm)
                    sm.BeginFrame();

                // 9. Update scene (or loading screen if transitioning)
                _sceneManager.Update(gameTime);

                // 10. Render scene (or loading screen if transitioning)
                _sceneManager.Render(gameTime);

                // 11. Start scene transition (NON-BLOCKING!)
                if (_sceneManager is SceneManager sm2 && pendingSceneTransition == null)
                {
                    pendingSceneTransition = sm2.ProcessDeferredTransitionsAsync(token);
                }

                // 12. Frame pacing
                var frameTime = _stopwatch.Elapsed - frameStartTime;
                var sleepTime = targetFrameTime - frameTime;
                if (sleepTime > TimeSpan.Zero)
                    Thread.Sleep(sleepTime);
            }
            
            _logger.LogInformation("Game loop exited gracefully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Game loop cancelled");
        }
        finally
        {
            _stopwatch.Stop();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public void Stop()
    {
        _logger.LogInformation("Stopping game loop");
        _cancellationTokenSource?.Cancel();
    }
}