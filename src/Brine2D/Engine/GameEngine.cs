using Brine2D.Core;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Default implementation of the game engine.
/// </summary>
internal sealed class GameEngine
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

        try
        {
            // Initialize renderer if available (optional dependency)
            var renderer = _serviceProvider.GetService<IRenderer>();
            if (renderer != null)
            {
                _logger.LogDebug("Initializing renderer");
                
                try
                {
                    await renderer.InitializeAsync(cancellationToken);
                    _logger.LogInformation("Renderer initialized successfully ({RendererType})", 
                        renderer.GetType().Name);
                }
                catch (DllNotFoundException ex)
                {
                    _logger.LogCritical(ex, "SDL3 library not found");
                    throw new InvalidOperationException(
                        "Failed to initialize renderer: SDL3 library not found. " +
                        "Ensure SDL3 runtime libraries are installed and accessible. " +
                        "\n\nFor installation instructions, see: https://github.com/libsdl-org/SDL/releases " +
                        "\n\nOn Windows: SDL3.dll must be in the same directory as your executable or in PATH. " +
                        "\nOn Linux: Install libSDL3 via your package manager (e.g., 'sudo apt install libsdl3-dev'). " +
                        "\nOn macOS: Install via Homebrew (e.g., 'brew install sdl3').", 
                        ex);
                }
                catch (Exception ex) when (ex.Message.Contains("SDL") || ex.Message.Contains("video") || ex.Message.Contains("GPU"))
                {
                    _logger.LogCritical(ex, "SDL3 initialization failed");
                    throw new InvalidOperationException(
                        "Failed to initialize SDL3 renderer. Common causes: " +
                        "\n- Graphics drivers are out of date" +
                        "\n- Vulkan/DirectX runtime not available (for GPU backend)" +
                        "\n- Display server not running (Linux/WSL)" +
                        "\n- Insufficient permissions" +
                        "\n\nTry switching to legacy renderer in options.Rendering.Backend = GraphicsBackend.LegacyRenderer " +
                        "\n\nFor troubleshooting, see: https://wiki.libsdl.org/SDL3/README/main", 
                        ex);
                }
            }
            else
            {
                _logger.LogInformation("No renderer registered - running in headless mode");
            }

            // Check input service (optional for headless)
            var inputService = _serviceProvider.GetService<IInputContext>();
            if (inputService != null)
            {
                _logger.LogDebug("Input service available");
            }
            else
            {
                _logger.LogInformation("No input service registered - running in headless mode");
            }

            IsInitialized = true;
            _logger.LogInformation("Game engine initialized successfully");
        }
        catch (InvalidOperationException)
        {
            // Already formatted, re-throw as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error during game engine initialization");
            throw new InvalidOperationException(
                $"Game engine initialization failed with unexpected error: {ex.Message} " +
                "\n\nCheck your configuration and ensure all required services are registered. " +
                "\nFor help, see: https://github.com/your-repo/Brine2D/wiki/Troubleshooting", 
                ex);
        }
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down game engine");
        IsInitialized = false;
        return Task.CompletedTask;
    }
}