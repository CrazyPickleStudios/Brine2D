using Brine2D.Core;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Brine2D.Engine;

/// <summary>
/// Main game loop. Runs synchronously on the main thread as required by SDL3.
/// Scene transitions are non-blocking; loading screens render while the next scene loads in the background.
/// </summary>
internal sealed class GameLoop
{
    private readonly ILogger<GameLoop> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IGameContext _gameContext;
    private readonly SceneManager _sceneManager;
    private readonly IInputContext _inputService;
    private readonly InputLayerManager? _inputLayerManager;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IEventPump? _eventPump;
    private readonly IMainThreadDispatcher? _mainThreadDispatcher;
    private readonly Stopwatch _stopwatch;
    private CancellationTokenSource? _cancellationTokenSource;

    private ulong _frameCount = 0;

    public int TargetFramesPerSecond { get; set; } = 60;

    /// <summary>
    /// Maximum allowed delta time per frame. Clamps runaway deltas caused by debugger pauses,
    /// OS preemption, or battery-saver throttling. Default: 100ms (10fps floor).
    /// Without this, a 2-second pause produces a single 2-second deltaTime that teleports
    /// entities, tunnels physics, and corrupts audio timing.
    /// </summary>
    public TimeSpan MaxDeltaTime { get; set; } = TimeSpan.FromSeconds(0.1);

    public GameLoop(
        ILogger<GameLoop> logger,
        ILoggerFactory loggerFactory,
        IGameContext gameContext,
        SceneManager sceneManager,
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
    /// Starts the game loop on the current thread. Blocks until the loop exits.
    /// Named Run rather than RunAsync intentionally. SDL3 requires all window and event
    /// operations on a single thread, so this must be synchronous.
    /// </summary>
    public void Run(CancellationToken cancellationToken = default)
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Game loop is already running");
            return;
        }

        RunGameLoopSync(cancellationToken);
    }

    /// <summary>
    /// The actual synchronous game loop.
    /// Processes the main thread work queue for GPU operations from async loading.
    /// Loading screens keep rendering while transitions happen in the background.
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
                if (pendingSceneTransition?.IsCompleted == true)
                {
                    if (pendingSceneTransition.IsFaulted)
                        _logger.LogError(pendingSceneTransition.Exception, "Scene transition failed");
                    pendingSceneTransition = null;
                }

                // 3. Update input
                _inputService.Update();

                // 4. Process events
                _eventPump?.ProcessEvents();

                // 5. Process input layers
                _inputLayerManager?.ProcessInput();

                // 6. Calculate time; clamp delta to prevent runaway updates after pauses
                var currentTime = _stopwatch.Elapsed;
                var elapsedTime = currentTime - lastFrameTime;
                if (elapsedTime > MaxDeltaTime)
                {
                    _logger.LogDebug("Delta time clamped: {Actual:F1}ms → {Max:F1}ms",
                        elapsedTime.TotalMilliseconds, MaxDeltaTime.TotalMilliseconds);
                    elapsedTime = MaxDeltaTime;
                }
                lastFrameTime = currentTime;
                totalTime += elapsedTime;
                var gameTime = new GameTime(totalTime, elapsedTime, _frameCount++);

                // 7. Update game context
                if (_gameContext is GameContext context)
                    context.GameTime = gameTime;

                // 8. Begin frame
                _sceneManager.BeginFrame();

                // 9. Update scene (or loading screen if transitioning)
                _sceneManager.Update(gameTime);

                // 10. Render scene (or loading screen if transitioning)
                _sceneManager.Render(gameTime);

                // 11. Fire-and-forget scene transition (deferred, non-blocking)
                pendingSceneTransition ??= _sceneManager.ProcessDeferredTransitionsAsync(token);

                // 12. Frame pacing: coarse sleep to yield the CPU, then spin for sub-millisecond precision.
                // Thread.Sleep alone has ~15ms OS resolution on Windows; spinning closes the gap cleanly.
                var frameTime = _stopwatch.Elapsed - frameStartTime;
                var sleepTime = targetFrameTime - frameTime;
                if (sleepTime > TimeSpan.Zero)
                {
                    var sleepMs = (int)(sleepTime.TotalMilliseconds) - 2;
                    if (sleepMs > 0)
                        Thread.Sleep(sleepMs);

                    var targetTime = frameStartTime + targetFrameTime;
                    while (_stopwatch.Elapsed < targetTime)
                        Thread.SpinWait(20);
                }
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