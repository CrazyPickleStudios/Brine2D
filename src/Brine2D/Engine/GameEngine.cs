using Brine2D.Core;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Engine;

/// <summary>
/// Default implementation of the game engine.
/// </summary>
internal sealed class GameEngine
{
    private readonly ILogger<GameEngine> _logger;
    private readonly IRenderer _renderer;
    private readonly IInputContext _inputContext;
    private bool _rendererDisposed;

    public bool IsInitialized { get; private set; }

    public GameEngine(ILogger<GameEngine> logger, IRenderer renderer, IInputContext inputContext)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _inputContext = inputContext ?? throw new ArgumentNullException(nameof(inputContext));
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
            _logger.LogDebug("Initializing renderer ({RendererType})", _renderer.GetType().Name);

            try
            {
                await _renderer.InitializeAsync(cancellationToken);
                _logger.LogInformation("Renderer initialized successfully ({RendererType})",
                    _renderer.GetType().Name);
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
            catch (ExternalException ex)
            {
                // Catches P/Invoke failures from SDL3 native calls; more precise and
                // locale-independent than filtering on exception message strings.
                _logger.LogCritical(ex, "SDL3 native call failed during initialization");
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

            _logger.LogDebug("Input service: {InputType}", _inputContext.GetType().Name);

            IsInitialized = true;
            _logger.LogInformation("Game engine initialized successfully");
        }
        catch (InvalidOperationException)
        {
            // Already formatted with user-friendly context, re-throw as-is.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error during game engine initialization");
            throw new InvalidOperationException(
                $"Game engine initialization failed with unexpected error: {ex.Message} " +
                "\n\nCheck your configuration and ensure all required services are registered. " +
                "\nFor help, see: https://github.com/CrazyPickleStudios/Brine2D/wiki/Troubleshooting",
                ex);
        }
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized)
            return Task.CompletedTask;

        _logger.LogInformation("Shutting down game engine");

        // Dispose explicitly so SDL3 native resources are freed before the DI container teardown.
        _logger.LogDebug("Shutting down renderer ({RendererType})", _renderer.GetType().Name);
        try
        {
            if (!_rendererDisposed)
            {
                _rendererDisposed = true;
                _renderer.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shutting down renderer");
        }

        IsInitialized = false;
        return Task.CompletedTask;
    }
}