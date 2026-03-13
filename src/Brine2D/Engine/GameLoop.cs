using Brine2D.Core;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Threading;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Brine2D.Hosting;

namespace Brine2D.Engine;

/// <summary>
/// Main game loop. Runs synchronously on the main thread as required by SDL3.
/// Scene transitions are non-blocking; loading screens render while the next scene loads in the background.
/// </summary>
internal sealed partial class GameLoop
{
    private readonly ILogger<GameLoop> _logger;
    private readonly IGameContext _gameContext;
    private readonly ISceneLoop _sceneLoop;
    private readonly IInputContext _inputService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IEventPump _eventPump;
    private readonly InputLayerManager? _inputLayerManager;
    private readonly Stopwatch _stopwatch;
    private CancellationTokenSource? _cancellationTokenSource;
    private volatile bool _isRunning;

    private long _frameCount;
    private int _targetFramesPerSecond;

    // Sleeps FrameSleepHeadroomMs short of the target deadline; spin-wait closes the sub-millisecond gap.
    private const int FrameSleepHeadroomMs = 2;

    // Iterations per spin-wait cycle during the sub-millisecond busy-wait.
    // Low enough to yield frequently, high enough to avoid excessive loop overhead.
    private const int FrameSpinWaitIterations = 20;

    /// <summary>Gets whether the game loop is currently running.</summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Target frames per second. 0 = uncapped (VSync or no frame limit).
    /// Initialized from <see cref="RenderingOptions.TargetFPS"/>; can be changed at runtime.
    /// </summary>
    public int TargetFramesPerSecond
    {
        get => _targetFramesPerSecond;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "TargetFramesPerSecond must be 0 (uncapped) or greater.");
            _targetFramesPerSecond = value;
        }
    }

    /// <summary>
    /// Maximum delta time per frame. Clamps large spikes caused by pauses or debugger breaks.
    /// Initialized from <see cref="RenderingOptions.MaxDeltaTimeMs"/>; can be changed at runtime.
    /// </summary>
    public TimeSpan MaxDeltaTime { get; set; }

    public GameLoop(
        ILogger<GameLoop> logger,
        IGameContext gameContext,
        ISceneLoop sceneLoop,
        IInputContext inputService,
        IEventPump eventPump,
        IMainThreadDispatcher mainThreadDispatcher,
        RenderingOptions renderingOptions,
        InputLayerManager? inputLayerManager = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _gameContext = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
        _sceneLoop = sceneLoop ?? throw new ArgumentNullException(nameof(sceneLoop));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _eventPump = eventPump ?? throw new ArgumentNullException(nameof(eventPump));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _inputLayerManager = inputLayerManager;
        _stopwatch = new Stopwatch();

        var renderOptions = renderingOptions ?? throw new ArgumentNullException(nameof(renderingOptions));
        TargetFramesPerSecond = renderOptions.TargetFPS;
        MaxDeltaTime = TimeSpan.FromMilliseconds(renderOptions.MaxDeltaTimeMs);
    }

    /// <summary>
    /// Starts the game loop on the current thread. Blocks until the loop exits.
    /// Named <c>Run</c> rather than <c>RunAsync</c> intentionally: SDL3 requires all window
    /// and event operations on a single thread, so this must be synchronous.
    /// </summary>
    public void Run(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
            throw new InvalidOperationException("Game loop is already running.");

        RunGameLoopSync(cancellationToken);
    }

    private void RunGameLoopSync(CancellationToken cancellationToken)
    {
        var timerPeriodSet = false;
        if (OperatingSystem.IsWindows())
        {
            timeBeginPeriod(1);
            timerPeriodSet = true;
            _logger.LogDebug("Windows multimedia timer resolution set to 1ms");
        }

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isRunning = true;
        var token = _cancellationTokenSource.Token;

        var totalTime = TimeSpan.Zero;
        _stopwatch.Restart();
        var lastFrameTime = _stopwatch.Elapsed;

        // Two ??= assignments keep this current across a frame:
        //   end-of-frame: captures tasks launched by ProcessDeferredTransitions in the current frame.
        //   start-of-frame: captures tasks that started from RunOnMainThreadAsync callbacks during
        //     Update/Render and therefore arrived after the end-of-frame assignment ran.
        Task? trackedLoadTask = null;

        try
        {
            while (!token.IsCancellationRequested)
            {
                var frameStartTime = _stopwatch.Elapsed;

                _mainThreadDispatcher.ProcessQueue();

                // Start-of-frame: picks up tasks that became active after the end-of-frame
                // assignment ran on the previous iteration.
                trackedLoadTask ??= _sceneLoop.ActiveLoadTask;

                var sceneTransitionFailed = false;
                if (trackedLoadTask?.IsCompleted == true)
                {
                    if (trackedLoadTask.IsFaulted)
                    {
                        var innerEx = trackedLoadTask.Exception?.InnerException
                                      ?? trackedLoadTask.Exception!;

                        // A cancellation caused by an already-cancelled token is not a load failure;
                        // the while condition will be false on the next iteration and the loop will exit.
                        if (innerEx is not OperationCanceledException || !token.IsCancellationRequested)
                            sceneTransitionFailed = true;
                    }
                    trackedLoadTask = null;
                }

                _inputService.Update();
                _eventPump.ProcessEvents();
                _inputLayerManager?.ProcessInput();

                var currentTime = _stopwatch.Elapsed;
                var elapsedTime = currentTime - lastFrameTime;
                var wasClamped = false;
                if (elapsedTime > MaxDeltaTime)
                {
                    _logger.LogDebug("Delta time clamped: {Actual:F1}ms -> {Max:F1}ms",
                        elapsedTime.TotalMilliseconds, MaxDeltaTime.TotalMilliseconds);
                    elapsedTime = MaxDeltaTime;
                    wasClamped = true;
                }
                lastFrameTime = currentTime;
                totalTime += elapsedTime;
                var gameTime = new GameTime(totalTime, elapsedTime, _frameCount++, wasClamped);

                _gameContext.UpdateGameTime(gameTime);

                _sceneLoop.BeginFrame();

                if (sceneTransitionFailed)
                    _sceneLoop.RaiseSceneLoadFailedIfPending();

                _sceneLoop.Update(gameTime);
                _sceneLoop.Render(gameTime);

                _sceneLoop.ProcessDeferredTransitions(token);
                // End-of-frame: captures any task just launched by ProcessDeferredTransitions.
                trackedLoadTask ??= _sceneLoop.ActiveLoadTask;

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
                            Thread.SpinWait(FrameSpinWaitIterations);
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
            if (timerPeriodSet)
                timeEndPeriod(1);

            _mainThreadDispatcher.SignalShutdown();

            var pendingLoad = trackedLoadTask ?? _sceneLoop.ActiveLoadTask;
            if (pendingLoad is { IsCompleted: false })
            {
                _logger.LogDebug("Awaiting in-flight scene transition after game loop exit...");
                try { pendingLoad.Wait(_sceneLoop.ShutdownTimeout); }
                catch (Exception ex) { _logger.LogDebug(ex, "Exception while awaiting in-flight scene transition on shutdown"); }
            }

            _stopwatch.Stop();
            _isRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _logger.LogInformation("Stopping game loop");
        try { _cancellationTokenSource?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    [LibraryImport("winmm.dll")]
    [SupportedOSPlatform("windows")]
    private static partial uint timeBeginPeriod(uint uPeriod);

    [LibraryImport("winmm.dll")]
    [SupportedOSPlatform("windows")]
    private static partial uint timeEndPeriod(uint uPeriod);
}