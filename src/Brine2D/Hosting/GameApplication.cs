using Brine2D.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Hosting;

/// <summary>
/// The main game application host.
/// </summary>
public class GameApplication : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly ILogger<GameApplication> _logger;
    private Thread? _gameThread;

    internal GameApplication(IHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _logger = _host.Services.GetRequiredService<ILogger<GameApplication>>();
    }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Creates a game application builder with pre-configured defaults.
    /// </summary>
    public static GameApplicationBuilder CreateBuilder(string[]? args = null)
    {
        return new GameApplicationBuilder(args ?? []);
    }

    /// <summary>
    /// Starts the game application with the specified initial scene.
    /// Runs the game on a dedicated thread and returns a task that completes when the game exits.
    /// </summary>
    /// <typeparam name="TScene">The initial scene type to load.</typeparam>
    /// <param name="cancellationToken">Token to cancel the game execution.</param>
    /// <returns>A task that completes when the game exits.</returns>
    /// <remarks>
    /// <para>
    /// This method runs the game on a dedicated thread to ensure all SDL3 operations
    /// (window creation, rendering, and event polling) occur on the same thread.
    /// This is required because SDL3 window events are posted to the thread that
    /// created the window, and must be polled from that same thread.
    /// </para>
    /// <para>
    /// Unlike blocking the calling thread, this approach allows the caller to await
    /// the game's lifetime without blocking their own thread, making it truly async.
    /// This is particularly useful for testing, hosting scenarios, or integration
    /// with other async systems.
    /// </para>
    /// <para>
    /// The dedicated thread is named "Brine2D-GameThread" and is marked as a
    /// foreground thread to keep the process alive while the game is running.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the game is already running.
    /// </exception>
    public Task RunAsync<TScene>(CancellationToken cancellationToken = default)
        where TScene : IScene
    {
        if (_gameThread != null && _gameThread.IsAlive)
        {
            throw new InvalidOperationException("Game is already running on another thread.");
        }

        var tcs = new TaskCompletionSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _gameThread = new Thread(() =>
        {
            var threadId = Environment.CurrentManagedThreadId;

            try
            {
                _logger.LogInformation("Starting Brine2D on dedicated game thread {ThreadId}", threadId);

                // All SDL operations happen on THIS thread
                _host.StartAsync(linkedCts.Token).GetAwaiter().GetResult();

                // Initialize game engine (creates window - must stay on this thread!)
                var engine = Services.GetRequiredService<IGameEngine>();
                engine.InitializeAsync(linkedCts.Token).GetAwaiter().GetResult();

                // Load initial scene
                var sceneManager = Services.GetRequiredService<ISceneManager>();
                sceneManager.LoadSceneAsync<TScene>(cancellationToken: linkedCts.Token)
                    .GetAwaiter().GetResult();

                _logger.LogInformation("Game initialized on thread {ThreadId}, starting event loop...", threadId);

                // Run game loop - MUST stay on this thread for event processing
                var gameLoop = Services.GetRequiredService<IGameLoop>();
                gameLoop.RunAsync(linkedCts.Token).GetAwaiter().GetResult();

                _logger.LogInformation("Game loop exited normally on thread {ThreadId}", threadId);
                tcs.SetResult();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Game cancelled on thread {ThreadId}", threadId);
                tcs.TrySetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in game application on thread {ThreadId}", threadId);
                tcs.TrySetException(ex);
            }
            finally
            {
                try
                {
                    _logger.LogDebug("Stopping host on thread {ThreadId}", threadId);
                    _host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping host on thread {ThreadId}", threadId);
                }
                finally
                {
                    linkedCts.Dispose();
                }
            }
        })
        {
            Name = "Brine2D-GameThread",
            IsBackground = false, // Foreground thread - keeps process alive
            Priority = ThreadPriority.Normal
        };

        _gameThread.Start();

        _logger.LogInformation("Game thread started. Caller can await completion without blocking.");

        return tcs.Task;
    }

    /// <summary>
    /// Starts the game application with the specified initial scene.
    /// This is a synchronous version that blocks the calling thread.
    /// </summary>
    /// <typeparam name="TScene">The initial scene type to load.</typeparam>
    /// <param name="cancellationToken">Token to cancel the game execution.</param>
    /// <remarks>
    /// <para>
    /// This method blocks the calling thread and runs all SDL operations on it.
    /// Use this if you want explicit blocking behavior, or if you're calling
    /// from a context where you need the game to run on the current thread
    /// (e.g., for platform-specific requirements or debugging).
    /// </para>
    /// <para>
    /// For most scenarios, prefer <see cref="RunAsync{TScene}"/> which runs
    /// the game on a dedicated thread without blocking the caller.
    /// </para>
    /// </remarks>
    public void Run<TScene>(CancellationToken cancellationToken = default)
        where TScene : IScene
    {
        var threadId = Environment.CurrentManagedThreadId;
        _logger.LogInformation("Starting Brine2D on calling thread {ThreadId} (blocking mode)", threadId);

        try
        {
            // Block synchronously to keep window and event loop on same thread
            _host.StartAsync(cancellationToken).GetAwaiter().GetResult();

            var engine = Services.GetRequiredService<IGameEngine>();
            engine.InitializeAsync(cancellationToken).GetAwaiter().GetResult();

            var sceneManager = Services.GetRequiredService<ISceneManager>();
            sceneManager.LoadSceneAsync<TScene>(cancellationToken: cancellationToken)
                .GetAwaiter().GetResult();

            _logger.LogInformation("Game initialized on thread {ThreadId}, starting event loop...", threadId);

            var gameLoop = Services.GetRequiredService<IGameLoop>();
            gameLoop.RunAsync(cancellationToken).GetAwaiter().GetResult();

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
        }
    }
    
    /// <summary>
    /// Disposes the game application and releases all resources.
    /// </summary>
    /// <remarks>
    /// If the game is running on a dedicated thread, this method will wait for
    /// the thread to complete before disposing resources.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        // If game thread is running, give it a moment to finish
        if (_gameThread != null && _gameThread.IsAlive)
        {
            _logger.LogInformation("Waiting for game thread to complete...");

            // Give it 5 seconds to finish gracefully
            if (!_gameThread.Join(TimeSpan.FromSeconds(5)))
            {
                _logger.LogWarning("Game thread did not complete within timeout. Continuing disposal.");
            }
        }

        if (_host is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
        else
        {
            _host?.Dispose();
        }

        _logger.LogInformation("Game application disposed");
    }
}