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
    /// Loads and starts with the specified scene.
    /// This method runs synchronously on the calling thread to ensure SDL stays on the main thread.
    /// </summary>
    public Task RunAsync<TScene>(CancellationToken cancellationToken = default) where TScene : IScene
    {
        var threadId = Environment.CurrentManagedThreadId;
        _logger.LogInformation("Starting Brine2D game application on thread {ThreadId}", threadId);

        // Block synchronously to keep SDL on the same thread
        _host.StartAsync(cancellationToken).GetAwaiter().GetResult();

        try
        {
            // Initialize game engine (which handles renderer, etc.)
            var engine = Services.GetRequiredService<IGameEngine>();
            engine.InitializeAsync(cancellationToken).GetAwaiter().GetResult();

            // Load initial scene
            var sceneManager = Services.GetRequiredService<ISceneManager>();
            sceneManager.LoadSceneAsync<TScene>(cancellationToken).GetAwaiter().GetResult();

            _logger.LogInformation("Game initialized on thread {ThreadId}, starting loop...", Environment.CurrentManagedThreadId);

            // Start game loop - MUST stay on this thread
            var gameLoop = Services.GetRequiredService<IGameLoop>();
            
            // Run synchronously to stay on this thread
            gameLoop.RunAsync(cancellationToken).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in game application");
            throw;
        }
        finally
        {
            _host.StopAsync(cancellationToken).GetAwaiter().GetResult();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Runs the game application.
    /// </summary>
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "No scene specified. Use RunAsync<TScene>() or MapScene() to set an initial scene.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
        else
        {
            _host?.Dispose();
        }
    }
}
