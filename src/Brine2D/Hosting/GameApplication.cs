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
public sealed class GameApplication : IAsyncDisposable, IDisposable
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
    /// <remarks>
    /// The game runs on a dedicated thread; SDL3 window events are posted to and must be polled
    /// from the thread that created the window.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the game is already running.</exception>
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
    /// <example>
    /// <code>
    /// await game.RunAsync&lt;GameScene&gt;(sp =>
    ///     new GameScene(sp.GetRequiredService&lt;IRenderer&gt;(), levelNumber: 5));
    /// </code>
    /// </example>
    public Task RunAsync<TScene>(
        Func<IServiceProvider, TScene>? sceneFactory,
        CancellationToken cancellationToken = default)
        where TScene : Scene
        => RunOnGameThread(token =>
        {
            Services.GetRequiredService<GameEngine>()
                .InitializeAsync(token).GetAwaiter().GetResult();

            var sceneManager = Services.GetRequiredService<ISceneManager>();
            (sceneFactory is null
                ? sceneManager.LoadSceneAsync<TScene>(cancellationToken: token)
                : sceneManager.LoadSceneAsync(sceneFactory, cancellationToken: token))
                .GetAwaiter().GetResult();

            _logger.LogInformation("Game initialized on thread {ThreadId}, starting event loop...",
                Environment.CurrentManagedThreadId);

            Services.GetRequiredService<GameLoop>().Run(token);
        }, cancellationToken);

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
                try
                {
                    Services.GetRequiredService<IMainThreadDispatcher>().SignalShutdown();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error signaling dispatcher shutdown on thread {ThreadId}", threadId);
                }

                try { Services.GetRequiredService<GameEngine>().ShutdownAsync().GetAwaiter().GetResult(); }
                catch (Exception ex) { _logger.LogError(ex, "Error shutting down game engine on thread {ThreadId}", threadId); }

                try { _host.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
                catch (Exception ex) { _logger.LogError(ex, "Error stopping host on thread {ThreadId}", threadId); }

                // Capture before resetting _running; a concurrent RunAsync could replace _gameThreadTcs once _running is 0.
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

    /// <summary>
    /// Disposes the game application and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _logger.LogInformation("Disposing game application...");

        Exception? forcedShutdownException = null;

        if (_gameThreadCts != null && !_gameThreadCts.Token.IsCancellationRequested)
        {
            _logger.LogDebug("Requesting game thread cancellation...");
            _gameThreadCts.Cancel();
        }

        if (_gameThreadTcs != null)
        {
            var timeout = TimeSpan.FromSeconds(_options.ShutdownTimeoutSeconds);
            _logger.LogDebug("Waiting for game thread to exit (timeout: {Timeout}s)...", timeout.TotalSeconds);

            var completed = await Task.WhenAny(_gameThreadTcs.Task, Task.Delay(timeout));

            if (completed != _gameThreadTcs.Task)
            {
                _logger.LogWarning("Game thread did not exit within timeout. Forcing shutdown.");
                _applicationLifetime.StopApplication();

                await Task.WhenAny(_gameThreadTcs.Task,
                    Task.Delay(TimeSpan.FromSeconds(_options.ForceShutdownGracePeriodSeconds)));

                if (_gameThreadTcs.Task.IsCompleted && _gameThreadTcs.Task.Exception is { } forcedEx)
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

        _gameThreadCts?.Dispose();
        _gameThreadCts = null;

        _logger.LogInformation("Game application disposed.");

        if (_host is IAsyncDisposable asyncHost)
            await asyncHost.DisposeAsync();
        else
            _host.Dispose();

        if (forcedShutdownException != null)
            ExceptionDispatchInfo.Capture(forcedShutdownException).Throw();
    }

    [Obsolete("GameApplication requires async disposal. Use 'await using' instead of 'using'.", error: true)]
    public void Dispose() => throw new NotSupportedException(
        "GameApplication must be disposed asynchronously. Use 'await using var game = ...' instead.");
}