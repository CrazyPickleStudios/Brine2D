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
/// // Recommended: Automatic disposal
/// await using var game = builder.Build();
/// await game.RunAsync&lt;MainScene&gt;();
/// // Automatically disposed here
/// 
/// // Alternative: Manual disposal
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
    /// This method runs the game on a dedicated thread to ensure all SDL3 operations
    /// (window creation, rendering, and event polling) occur on the same thread.
    /// SDL3 window events are posted to the thread that created the window and must
    /// be polled from that same thread.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the game is already running.</exception>
    public Task RunAsync<TScene>(CancellationToken cancellationToken = default)
        where TScene : Scene
        => RunOnGameThread(token =>
        {
            var engine = Services.GetRequiredService<GameEngine>();
            engine.InitializeAsync(token).GetAwaiter().GetResult();

            Services.GetRequiredService<ISceneManager>()
                .LoadSceneAsync<TScene>(cancellationToken: token)
                .GetAwaiter().GetResult();

            _logger.LogInformation("Game initialized on thread {ThreadId}, starting event loop...",
                Environment.CurrentManagedThreadId);

            Services.GetRequiredService<GameLoop>().Run(token);
        }, cancellationToken);

    /// <summary>
    /// Starts the game application with a custom scene factory.
    /// Use this when you need to pass runtime data to the initial scene that DI alone cannot provide.
    /// </summary>
    /// <typeparam name="TScene">The initial scene type to load.</typeparam>
    /// <param name="sceneFactory">Factory function to create the scene with custom parameters.</param>
    /// <param name="cancellationToken">Token to cancel the game execution.</param>
    /// <example>
    /// <code>
    /// await game.RunAsync&lt;GameScene&gt;(sp =>
    ///     new GameScene(sp.GetRequiredService&lt;IRenderer&gt;(), levelNumber: 5));
    /// </code>
    /// </example>
    public Task RunAsync<TScene>(
        Func<IServiceProvider, TScene> sceneFactory,
        CancellationToken cancellationToken = default)
        where TScene : Scene
    {
        ArgumentNullException.ThrowIfNull(sceneFactory);
        return RunOnGameThread(token =>
        {
            var engine = Services.GetRequiredService<GameEngine>();
            engine.InitializeAsync(token).GetAwaiter().GetResult();

            Services.GetRequiredService<ISceneManager>()
                .LoadSceneAsync(sceneFactory, cancellationToken: token)
                .GetAwaiter().GetResult();

            _logger.LogInformation("Game initialized on thread {ThreadId}, starting event loop...",
                Environment.CurrentManagedThreadId);

            Services.GetRequiredService<GameLoop>().Run(token);
        }, cancellationToken);
    }

    /// <summary>
    /// Starts the game application with the specified initial scene.
    /// Blocks the calling thread; all SDL operations run on it directly.
    /// </summary>
    /// <remarks>
    /// For most scenarios, prefer <see cref="RunAsync{TScene}"/> which runs
    /// the game on a dedicated thread without blocking the caller.
    /// </remarks>
    public void Run<TScene>(CancellationToken cancellationToken = default)
        where TScene : Scene
    {
        var threadId = Environment.CurrentManagedThreadId;
        _logger.LogInformation("Starting Brine2D on calling thread {ThreadId} (blocking mode)", threadId);

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _applicationLifetime.ApplicationStopping);

        try
        {
            var linkedToken = linkedCts.Token;

            _host.StartAsync(linkedToken).GetAwaiter().GetResult();

            var engine = Services.GetRequiredService<GameEngine>();
            engine.InitializeAsync(linkedToken).GetAwaiter().GetResult();

            Services.GetRequiredService<ISceneManager>()
                .LoadSceneAsync<TScene>(cancellationToken: linkedToken)
                .GetAwaiter().GetResult();

            _logger.LogInformation("Game initialized on thread {ThreadId}, starting event loop...", threadId);

            Services.GetRequiredService<GameLoop>().Run(linkedToken);

            _logger.LogInformation("Game loop exited normally on thread {ThreadId}", threadId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Game cancelled on thread {ThreadId}", threadId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in game application on thread {ThreadId}", threadId);
            throw;
        }
        finally
        {
            _host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            linkedCts.Dispose();
        }
    }

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
            try
            {
                _logger.LogInformation("Starting Brine2D on dedicated game thread {ThreadId}", threadId);
                _host.StartAsync(linkedToken).GetAwaiter().GetResult();
                work(linkedToken);
                _logger.LogInformation("Game loop exited normally on thread {ThreadId}", threadId);
                _gameThreadTcs.SetResult();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Game cancelled on thread {ThreadId}", threadId);
                _gameThreadTcs.TrySetCanceled(linkedToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in game application on thread {ThreadId}", threadId);
                _gameThreadTcs.TrySetException(ex);
            }
            finally
            {
                try { _host.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
                catch (Exception ex) { _logger.LogError(ex, "Error stopping host on thread {ThreadId}", threadId); }
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
        }

        _gameThreadCts?.Dispose();
        _gameThreadCts = null;

        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();

        _logger.LogInformation("Game application disposed.");
    }
}