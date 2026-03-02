using Brine2D.Core;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Threading;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Brine2D.Engine;

/// <summary>
/// Main game loop. Runs synchronously on the main thread as required by SDL3.
/// Scene transitions are non-blocking; loading screens render while the next scene loads in the background.
/// </summary>
internal sealed class GameLoop
{
    private readonly ILogger<GameLoop> _logger;
    private readonly IGameContext _gameContext;
    private readonly SceneManager _sceneManager;
    private readonly IInputContext _inputService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IEventPump _eventPump;
    private readonly InputLayerManager? _inputLayerManager;
    private readonly Stopwatch _stopwatch;
    private CancellationTokenSource? _cancellationTokenSource;

    private ulong _frameCount;

    // Sleeps FrameSleepHeadroomMs short of the target deadline; spin-wait closes the sub-millisecond gap.
    private const int FrameSleepHeadroomMs = 2;

    /// <summary>
    /// Target frames per second. 0 = uncapped (VSync or no frame limit).
    /// Initialized from <see cref="RenderingOptions.TargetFPS"/>; can be changed at runtime.
    /// </summary>
    public int TargetFramesPerSecond { get; set; }

    /// <summary>
    /// Maximum delta time per frame. Clamps large spikes caused by pauses or debugger breaks.
    /// Initialized from <see cref="RenderingOptions.MaxDeltaTimeMs"/>; can be changed at runtime.
    /// </summary>
    public TimeSpan MaxDeltaTime { get; set; }

    public GameLoop(
        ILogger<GameLoop> logger,
        IGameContext gameContext,
        SceneManager sceneManager,
        IInputContext inputService,
        IEventPump eventPump,
        IMainThreadDispatcher mainThreadDispatcher,
        RenderingOptions renderingOptions,
        InputLayerManager? inputLayerManager = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _gameContext = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _eventPump = eventPump ?? throw new ArgumentNullException(nameof(eventPump));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _inputLayerManager = inputLayerManager;
        _stopwatch = new Stopwatch();

        var options = renderingOptions ?? throw new ArgumentNullException(nameof(renderingOptions));
        TargetFramesPerSecond = options.TargetFPS;
        MaxDeltaTime = TimeSpan.FromMilliseconds(options.MaxDeltaTimeMs);
    }

    /// <summary>
    /// Starts the game loop on the current thread. Blocks until the loop exits.
    /// Named <c>Run</c> rather than <c>RunAsync</c> intentionally: SDL3 requires all window
    /// and event operations on a single thread, so this must be synchronous.
    /// </summary>
    public void Run(CancellationToken cancellationToken = default)
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            throw new InvalidOperationException("Game loop is already running.");

        RunGameLoopSync(cancellationToken);
    }

    private void RunGameLoopSync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cancellationTokenSource.Token;

        var totalTime = TimeSpan.Zero;
        _stopwatch.Start();
        var lastFrameTime = _stopwatch.Elapsed;

        Task? pendingSceneTransition = null;

        try
        {
            while (!token.IsCancellationRequested)
            {
                var frameStartTime = _stopwatch.Elapsed;

                _mainThreadDispatcher.ProcessQueue();

                if (pendingSceneTransition?.IsCompleted == true)
                {
                    if (pendingSceneTransition.IsFaulted)
                    {
                        var innerEx = pendingSceneTransition.Exception?.InnerException
                                      ?? pendingSceneTransition.Exception!;

                        // Cancellation from our token is expected shutdown; anything else is fatal.
                        if (innerEx is OperationCanceledException oce && oce.CancellationToken == token)
                            _cancellationTokenSource.Cancel();
                        else
                        {
                            _logger.LogError(innerEx, "Scene transition failed");
                            ExceptionDispatchInfo.Capture(innerEx).Throw();
                        }
                    }
                    pendingSceneTransition = null;
                }

                _inputService.Update();
                _eventPump.ProcessEvents();
                _inputLayerManager?.ProcessInput();

                // Clamp delta time to prevent runaway updates after pauses or debugger breaks.
                var currentTime = _stopwatch.Elapsed;
                var elapsedTime = currentTime - lastFrameTime;
                if (elapsedTime > MaxDeltaTime)
                {
                    _logger.LogDebug("Delta time clamped: {Actual:F1}ms -> {Max:F1}ms",
                        elapsedTime.TotalMilliseconds, MaxDeltaTime.TotalMilliseconds);
                    elapsedTime = MaxDeltaTime;
                }
                lastFrameTime = currentTime;
                totalTime += elapsedTime;
                var gameTime = new GameTime(totalTime, elapsedTime, _frameCount++);

                _gameContext.UpdateGameTime(gameTime);

                _sceneManager.BeginFrame();
                _sceneManager.Update(gameTime);
                _sceneManager.Render(gameTime);

                // Deferred; runs in the background while the current frame finishes.
                pendingSceneTransition ??= _sceneManager.ProcessDeferredTransitionsAsync(token);

                // Frame pacing: 0 = uncapped; re-read each frame so runtime changes take effect immediately.
                if (TargetFramesPerSecond > 0)
                {
                    var targetFrameTime = TimeSpan.FromSeconds(1.0 / TargetFramesPerSecond);
                    var sleepTime = targetFrameTime - (_stopwatch.Elapsed - frameStartTime);
                    if (sleepTime > TimeSpan.Zero)
                    {
                        var sleepMs = (int)(sleepTime.TotalMilliseconds) - FrameSleepHeadroomMs;
                        if (sleepMs > 0)
                            Thread.Sleep(sleepMs);

                        var targetTime = frameStartTime + targetFrameTime;
                        while (_stopwatch.Elapsed < targetTime)
                            Thread.SpinWait(20);
                    }
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
            if (pendingSceneTransition is { IsCompleted: false })
                _logger.LogDebug("A scene transition was in-flight when the game loop exited; it has been abandoned.");

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