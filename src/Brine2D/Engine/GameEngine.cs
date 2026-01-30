using Brine2D.Core;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Default implementation of the game engine.
/// </summary>
public class GameEngine : IGameEngine
{
    private readonly ILogger<GameEngine> _logger;
    private readonly IServiceProvider _serviceProvider;

    public bool IsInitialized { get; private set; }

    public GameEngine(ILogger<GameEngine> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            _logger.LogWarning("Game engine already initialized");
            return;
        }

        _logger.LogInformation("Initializing game engine");

        // Initialize renderer if available (optional dependency)
        var renderer = _serviceProvider.GetService<IRenderer>();
        if (renderer != null)
        {
            _logger.LogDebug("Initializing renderer");
            await renderer.InitializeAsync(cancellationToken);
        }
        else
        {
            _logger.LogWarning("No renderer registered");
        }

        // Initialize input service (call Update once to set up initial state)
        var inputService = _serviceProvider.GetService<IInputContext>();
        if (inputService != null)
        {
            _logger.LogDebug("Input service available");
            // The input service will be updated in the game loop
        }

        IsInitialized = true;
        _logger.LogInformation("Game engine initialized successfully");
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down game engine");
        IsInitialized = false;
        return Task.CompletedTask;
    }
}