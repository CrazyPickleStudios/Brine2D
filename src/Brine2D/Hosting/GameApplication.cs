using Brine2D.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Hosting;

/// <summary>
/// The main game application host.
/// </summary>
/// <remarks>
/// <para>
/// This class manages the game's lifetime and should be disposed when done.
/// Disposal is automatic when using the 'await using' statement.
/// </para>
/// <para>
/// The game host manages SDL resources, the game loop, and scene management.
/// When disposed, all resources are cleaned up gracefully.
/// </para>
/// </remarks>
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
public sealed class GameApplication
{
    private readonly IHost _host;
    private readonly ILogger<GameApplication> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime; 
    private Thread? _gameThread;

    internal GameApplication(IHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _logger = _host.Services.GetRequiredService<ILogger<GameApplication>>();
        _applicationLifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>(); 
    }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Creates a game application builder with pre-configured defaults.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="settings">Optional host builder settings for advanced configuration.</param>
    /// <returns>A configured <see cref="GameApplicationBuilder"/>.</returns>
    public static GameApplicationBuilder CreateBuilder(
        string[]? args = null,
        HostApplicationBuilderSettings? settings = null)
    {
        return new GameApplicationBuilder(args ?? [], settings);
    }

    /// <summary>
    /// Creates a minimal game application builder without pre-configured defaults.
    /// Use this for full control over configuration sources, logging, and services.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A minimal <see cref="GameApplicationBuilder"/> with no defaults.</returns>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="CreateBuilder"/>, this does not automatically add:
    /// </para>
    /// <list type="bullet">
    /// <item><description>JSON configuration files (gamesettings.json)</description></item>
    /// <item><description>Console logging</description></item>
    /// <item><description>Default log levels</description></item>
    /// </list>
    /// <para>
    /// Use this when you need complete control over the builder configuration,
    /// similar to WebApplication.CreateSlimBuilder() in ASP.NET.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Minimal builder - add only what you need
    /// var builder = GameApplication.CreateSlimBuilder(args);
    /// builder.Logging.AddJsonConsole(); // Custom logging only
    /// builder.Services.AddBrine2D().UseSDL();
    /// 
    /// var game = builder.Build();
    /// await game.RunAsync&lt;MyScene&gt;();
    /// </code>
    /// </example>
    public static GameApplicationBuilder CreateSlimBuilder(string[]? args = null)
    {
        return new GameApplicationBuilder(args ?? [], settings: null, isSlim: true);
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
        where TScene : Scene
    {
        if (_gameThread != null && _gameThread.IsAlive)
        {
            throw new InvalidOperationException("Game is already running on another thread.");
        }

        var tcs = new TaskCompletionSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _applicationLifetime.ApplicationStopping);

        _gameThread = new Thread(() =>
        {
            var threadId = Environment.CurrentManagedThreadId;

            try
            {
                _logger.LogInformation("Starting Brine2D on dedicated game thread {ThreadId}", threadId);

                // All SDL operations happen on THIS thread
                _host.StartAsync(linkedCts.Token).GetAwaiter().GetResult();

                // Initialize game engine (creates window - must stay on this thread!)
                var engine = Services.GetRequiredService<GameEngine>();
                engine.InitializeAsync(linkedCts.Token).GetAwaiter().GetResult();

                // Load initial scene
                var sceneManager = Services.GetRequiredService<ISceneManager>();
                sceneManager.LoadSceneAsync<TScene>(cancellationToken: linkedCts.Token)
                    .GetAwaiter().GetResult();

                _logger.LogInformation("Game initialized on thread {ThreadId}, starting event loop...", threadId);

                // Run game loop - MUST stay on this thread for event processing
                var gameLoop = Services.GetRequiredService<GameLoop>();
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
    /// Starts the game application with a custom scene factory.
    /// Use this when you need to pass runtime data to the initial scene.
    /// </summary>
    /// <typeparam name="TScene">The initial scene type to load.</typeparam>
    /// <param name="sceneFactory">Factory function to create the scene with custom parameters.</param>
    /// <param name="cancellationToken">Token to cancel the game execution.</param>
    /// <returns>A task that completes when the game exits.</returns>
    /// <remarks>
    /// <para>
    /// This overload allows passing runtime data to scenes that DI alone cannot provide,
    /// such as level numbers, save data, or multiplayer session info.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Pass level number to scene
    /// var levelNumber = 5;
    /// await game.RunAsync&lt;GameScene&gt;(sp => 
    /// {
    ///     var renderer = sp.GetRequiredService&lt;IRenderer&gt;();
    ///     var input = sp.GetRequiredService&lt;IInputService&gt;();
    ///     var logger = sp.GetRequiredService&lt;ILogger&lt;GameScene&gt;&gt;();
    ///     return new GameScene(renderer, input, logger, levelNumber);
    /// });
    /// </code>
    /// </example>
    public Task RunAsync<TScene>(
        Func<IServiceProvider, TScene> sceneFactory,
        CancellationToken cancellationToken = default)
        where TScene : Scene
    {
        if (sceneFactory == null)
            throw new ArgumentNullException(nameof(sceneFactory));
        
        if (_gameThread != null && _gameThread.IsAlive)
        {
            throw new InvalidOperationException("Game is already running on another thread.");
        }

        var tcs = new TaskCompletionSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _applicationLifetime.ApplicationStopping);

        _gameThread = new Thread(() =>
        {
            var threadId = Environment.CurrentManagedThreadId;

            try
            {
                _logger.LogInformation("Starting Brine2D on dedicated game thread {ThreadId}", threadId);

                // All SDL operations happen on THIS thread
                _host.StartAsync(linkedCts.Token).GetAwaiter().GetResult();

                // Initialize game engine (creates window - must stay on this thread!)
                var engine = Services.GetRequiredService<GameEngine>();
                engine.InitializeAsync(linkedCts.Token).GetAwaiter().GetResult();

                // Load initial scene using factory
                var sceneManager = Services.GetRequiredService<ISceneManager>();
                sceneManager.LoadSceneAsync(sceneFactory, cancellationToken: linkedCts.Token)
                    .GetAwaiter().GetResult();

                _logger.LogInformation("Game initialized on thread {ThreadId}, starting event loop...", threadId);

                // Run game loop - MUST stay on this thread for event processing
                var gameLoop = Services.GetRequiredService<GameLoop>();
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
            IsBackground = false,
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
        where TScene : Scene
    {
        var threadId = Environment.CurrentManagedThreadId;
        _logger.LogInformation("Starting Brine2D on calling thread {ThreadId} (blocking mode)", threadId);

        try
        {
            // Block synchronously to keep window and event loop on same thread
            _host.StartAsync(cancellationToken).GetAwaiter().GetResult();

            var engine = Services.GetRequiredService<GameEngine>();
            engine.InitializeAsync(cancellationToken).GetAwaiter().GetResult();

            var sceneManager = Services.GetRequiredService<ISceneManager>();
            sceneManager.LoadSceneAsync<TScene>(cancellationToken: cancellationToken)
                .GetAwaiter().GetResult();

            _logger.LogInformation("Game initialized on thread {ThreadId}, starting event loop...", threadId);

            var gameLoop = Services.GetRequiredService<GameLoop>();
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