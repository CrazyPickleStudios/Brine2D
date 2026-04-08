using Brine2D.Engine;
using Brine2D.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.ExceptionServices;

namespace Brine2D.Hosting;

/// <summary>
/// The main game application host.
/// </summary>
public sealed class GameApplication : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly ILogger<GameApplication> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly Brine2DOptions _options;
    private volatile CancellationTokenSource? _gameThreadCts;
    private volatile TaskCompletionSource? _gameThreadTcs;
    private volatile int _disposed;
    private int _running;

    internal GameApplication(IHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _logger = _host.Services.GetRequiredService<ILogger<GameApplication>>();
        _applicationLifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
        _options = _host.Services.GetRequiredService<Brine2DOptions>();
    }

    /// <summary>Gets the service provider.</summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Creates a game application builder with pre-configured defaults.
    /// </summary>
    public static GameApplicationBuilder CreateBuilder(string[]? args = null)
        => new GameApplicationBuilder(args ?? []);

    /// <summary>
    /// Starts the game application with the specified initial scene.
    /// Runs the game on a dedicated thread and returns a task that completes when the game exits.
    /// </summary>
    /// <typeparam name="TScene">The initial scene type to load.</typeparam>
    /// <param name="cancellationToken">Token to cancel the game execution.</param>
    /// <returns>
    /// A task that completes when the game exits. The task will fault if engine initialization
    /// or the initial scene load fails on the game thread.
    /// </returns>
    /// <remarks>
    /// The game runs on a dedicated thread; SDL3 window events are posted to and must be polled
    /// from the thread that created the window.
    /// <para>
    /// The initial scene is loaded synchronously before the game loop starts, so a
    /// <see cref="LoadingScene"/> cannot be displayed during this first load. To show a loading
    /// screen from startup, load a lightweight placeholder scene first and transition to the
    /// real scene (with a loading screen) from its <c>OnEnter</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the game is already running.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the application has been disposed.</exception>
    public Task RunAsync<TScene>(CancellationToken cancellationToken = default)
        where TScene : Scene
        => RunAsync<TScene>(sceneFactory: null, cancellationToken);

    /// <summary>
    /// Starts the game application with a custom scene factory.
    /// Use this when you need to pass runtime data to the initial scene that DI alone cannot provide.
    /// Pass <see langword="null"/> for <paramref name="sceneFactory"/> to use standard DI resolution.
    /// </summary>
    /// <typeparam name="TScene">The initial scene type to load.</typeparam>
    /// <param name="sceneFactory">
    /// Factory function to create the scene, or <see langword="null"/> to resolve via DI.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the game execution.</param>
    /// <returns>
    /// A task that completes when the game exits. The task will fault if engine initialization
    /// or the initial scene load fails on the game thread.
    /// </returns>
    /// <remarks>
    /// The initial scene is loaded synchronously before the game loop starts, so a
    /// <see cref="LoadingScene"/> cannot be displayed during this first load. To show a loading
    /// screen from startup, load a lightweight placeholder scene first and transition to the
    /// real scene (with a loading screen) from its <c>OnEnter</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// await game.RunAsync&lt;GameScene&gt;(sp =>
    ///     new GameScene(sp.GetRequiredService&lt;IRenderer&gt;(), levelNumber: 5));
    /// </code>
    /// </example>
    /// <exception cref="InvalidOperationException">Thrown if the game is already running.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the application has been disposed.</exception>
    public Task RunAsync<TScene>(
        Func<IServiceProvider, TScene>? sceneFactory,
        CancellationToken cancellationToken = default)
        where TScene : Scene
        => RunOnGameThread(token =>
        {
            Services.GetRequiredService<GameEngine>()
                .InitializeAsync(token).GetAwaiter().GetResult();

            Services.GetRequiredService<SceneManager>()
                .LoadInitialSceneAsync(sceneFactory, token)
                .GetAwaiter().GetResult();

            _logger.LogInformation("Game initialized on thread {ThreadId}, starting event loop...",
                Environment.CurrentManagedThreadId);

            Services.GetRequiredService<GameLoop>().Run(token);
        }, cancellationToken);

    /// <summary>
    /// Disposes the game application and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _logger.LogInformation("Disposing game application...");

        Exception? forcedShutdownException = null;

        var cts = _gameThreadCts;
        var tcs = _gameThreadTcs;

        if (cts != null && !cts.Token.IsCancellationRequested)
        {
            _logger.LogDebug("Requesting game thread cancellation...");
            cts.Cancel();
        }

        if (tcs != null)
        {
            var timeout = TimeSpan.FromSeconds(_options.ShutdownTimeoutSeconds);
            _logger.LogDebug("Waiting for game thread to exit (timeout: {Timeout}s)...", timeout.TotalSeconds);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout));

            if (completed != tcs.Task)
            {
                _logger.LogWarning("Game thread did not exit within timeout. Forcing shutdown.");
                _applicationLifetime.StopApplication();

                await Task.WhenAny(tcs.Task,
                    Task.Delay(TimeSpan.FromSeconds(_options.ForceShutdownGracePeriodSeconds)));

                if (tcs.Task.IsCompleted && tcs.Task.Exception is { } forcedEx)
                {
                    _logger.LogError(forcedEx, "Game thread threw a fatal exception during forced shutdown.");
                    forcedShutdownException = forcedEx.InnerExceptions.Count == 1
                        ? forcedEx.InnerException!
                        : forcedEx;
                }
            }
        }
        else
        {
            await _host.StopAsync(CancellationToken.None);
        }

        cts?.Dispose();
        _gameThreadCts = null;
        _gameThreadTcs = null;

        _logger.LogInformation("Game application disposed.");

        if (_host is IAsyncDisposable asyncHost)
            await asyncHost.DisposeAsync();
        else
            _host.Dispose();

        if (forcedShutdownException != null)
            ExceptionDispatchInfo.Capture(forcedShutdownException).Throw();
    }

    private Task RunOnGameThread(Action<CancellationToken> work, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            throw new InvalidOperationException("Game is already running on another thread.");

        _gameThreadTcs = new TaskCompletionSource();

        var oldCts = _gameThreadCts;
        _gameThreadCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _applicationLifetime.ApplicationStopping);
        oldCts?.Dispose();

        var linkedToken = _gameThreadCts.Token;

        var gameThread = new Thread(() =>
        {
            var threadId = Environment.CurrentManagedThreadId;
            Exception? fatalException = null;
            bool cancelled = false;

            try
            {
                _logger.LogInformation("Starting Brine2D on dedicated game thread {ThreadId}", threadId);
                _host.StartAsync(linkedToken).GetAwaiter().GetResult();
                work(linkedToken);
                _logger.LogInformation("Game loop exited normally on thread {ThreadId}", threadId);
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
                _logger.LogInformation("Game cancelled on thread {ThreadId}", threadId);
                cancelled = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in game application on thread {ThreadId}", threadId);
                fatalException = ex;
            }
            finally
            {
                // Dispose SceneManager first: tears down the active scene and disposes its
                // DI scope (releasing scoped AssetLoader refs) while the singleton AssetCache
                // is still alive. Without this, the host's DI container may dispose AssetCache
                // before SceneManager during reverse-creation-order teardown.
                try { Services.GetRequiredService<SceneManager>().DisposeAsync().AsTask().GetAwaiter().GetResult(); }
                catch (Exception ex) { _logger.LogError(ex, "Error disposing scene manager on thread {ThreadId}", threadId); }

                try { Services.GetRequiredService<GameEngine>().Shutdown(); }
                catch (Exception ex) { _logger.LogError(ex, "Error shutting down game engine on thread {ThreadId}", threadId); }

                try { _host.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
                catch (Exception ex) { _logger.LogError(ex, "Error stopping host on thread {ThreadId}", threadId); }

                // Capture the TCS before resetting _running. Once _running is 0 a concurrent
                // RunAsync call is free to replace _gameThreadTcs. The new caller's await on
                // RunAsync won't resume until their own game thread signals, so no work is lost —
                // but the previous caller's Task must still be completed via the captured reference.
                var tcs = _gameThreadTcs;
                Volatile.Write(ref _running, 0);

                if (fatalException != null)
                    tcs.TrySetException(fatalException);
                else if (cancelled)
                    tcs.TrySetCanceled(linkedToken);
                else
                    tcs.TrySetResult();
            }
        })
        {
            Name = "Brine2D-GameThread",
            IsBackground = false,
            Priority = _options.GameThreadPriority
        };

        gameThread.Start();
        return _gameThreadTcs.Task;
    }
}