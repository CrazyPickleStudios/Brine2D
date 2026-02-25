using Brine2D.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Hosting;

/// <summary>
/// The main game application host.
/// </summary>
/// <example>
/// <code>
/// // Recommended: automatic disposal
/// await using var game = builder.Build();
/// await game.RunAsync&lt;MainScene&gt;();
///
/// // Alternative: manual disposal
/// var game = builder.Build();
/// try
/// {
///     await game.RunAsync&lt;MainScene&gt;();
/// }
/// finally
/// {
///     await game.DisposeAsync();
/// }
/// </code>
/// </example>
public sealed class GameApplication : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly ILogger<GameApplication> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private Thread? _gameThread;
    private CancellationTokenSource? _gameThreadCts;
    private TaskCompletionSource? _gameThreadTcs;

    internal GameApplication(IHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _logger = _host.Services.GetRequiredService<ILogger<GameApplication>>();
        _applicationLifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
    }

    /// <summary>Gets the service provider.</summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Creates a game application builder with pre-configured defaults.
    /// </summary>
    public static GameApplicationBuilder CreateBuilder(
        string[]? args = null,
        HostApplicationBuilderSettings? settings = null)
        => new GameApplicationBuilder(args ?? [], settings);

    /// <summary>
    /// Starts the game application with the specified initial scene.
    /// Runs the game on a dedicated thread and returns a task that completes when the game exits.
    /// </summary>
    /// <typeparam name="TScene">The initial scene type to load.</typeparam>
    /// <param name="cancellationToken">Token to cancel the game execution.</param>
    /// <remarks>
    /// The game runs on a dedicated thread to ensure all SDL3 operations
    /// (window creation, rendering, and event polling) occur on the same thread.
    /// SDL3 window events are posted to the thread that created the window and must
    /// be polled from that same thread.
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
    /// Factory function to create the scene with custom parameters,
    /// or <see langword="null"/> to resolve via DI.
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

    /// <summary>
    /// Shared thread scaffolding for all <see cref="RunAsync{TScene}"/> overloads.
    /// Starts the dedicated game thread, executes <paramref name="work"/> on it, and returns
    /// a Task that completes when the thread exits.
    /// </summary>
    private Task RunOnGameThread(Action<CancellationToken> work, CancellationToken cancellationToken)
    {
        if (_gameThread != null && _gameThread.IsAlive)
            throw new InvalidOperationException("Game is already running on another thread.");

        _gameThreadTcs = new TaskCompletionSource();
        _gameThreadCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _applicationLifetime.ApplicationStopping);

        var linkedToken = _gameThreadCts.Token;

        _gameThread = new Thread(() =>
        {
            var threadId = Environment.CurrentManagedThreadId;

            // Capture outcome so we can signal the TCS *after* stopping the host,
            // ensuring DisposeAsync never races _host.Dispose() against StopAsync.
            Exception? fatalException = null;
            bool cancelled = false;

            try
            {
                _logger.LogInformation("Starting Brine2D on dedicated game thread {ThreadId}", threadId);
                _host.StartAsync(linkedToken).GetAwaiter().GetResult();
                work(linkedToken);

                if (linkedToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Game cancelled on thread {ThreadId}", threadId);
                    cancelled = true;
                }
                else
                {
                    _logger.LogInformation("Game loop exited normally on thread {ThreadId}", threadId);
                }
            }
            catch (OperationCanceledException)
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
                // Stop the host before signalling the TCS. This guarantees that when
                // DisposeAsync sees the task as complete it is safe to call _host.Dispose()
                // without racing a concurrent StopAsync still running on this thread.
                try { _host.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
                catch (Exception ex) { _logger.LogError(ex, "Error stopping host on thread {ThreadId}", threadId); }

                if (fatalException != null)
                    _gameThreadTcs.TrySetException(fatalException);
                else if (cancelled)
                    _gameThreadTcs.TrySetCanceled(linkedToken);
                else
                    _gameThreadTcs.SetResult();
            }
        })
        {
            Name = "Brine2D-GameThread",
            IsBackground = false,
            Priority = ThreadPriority.Normal
        };

        _gameThread.Start();
        return _gameThreadTcs.Task;
    }

    /// <summary>
    /// Disposes the game application and releases all resources.
    /// Uses cooperative cancellation to stop the game thread.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing game application...");

        if (_gameThreadCts != null && !_gameThreadCts.Token.IsCancellationRequested)
        {
            _logger.LogDebug("Requesting game thread cancellation...");
            _gameThreadCts.Cancel();
        }

        if (_gameThreadTcs != null)
        {
            var timeout = TimeSpan.FromSeconds(5);
            _logger.LogDebug("Waiting for game thread to exit (timeout: {Timeout}s)...", timeout.TotalSeconds);

            var completed = await Task.WhenAny(_gameThreadTcs.Task, Task.Delay(timeout));

            if (completed != _gameThreadTcs.Task)
            {
                _logger.LogWarning("Game thread did not exit within timeout. Forcing shutdown.");
                _applicationLifetime.StopApplication();
            }

            // The game thread's finally block calls _host.StopAsync before signalling the TCS,
            // so by the time we reach here the host is already stopped. Don't call it again.
        }
        else
        {
            // Game was built but RunAsync was never called — host was never started,
            // so StopAsync is a no-op, but calling it is still the correct shutdown sequence.
            await _host.StopAsync(CancellationToken.None);
        }

        _gameThreadCts?.Dispose();
        _gameThreadCts = null;
        _gameThread = null;

        _host.Dispose();
        _logger.LogInformation("Game application disposed.");
    }
}