using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// SDL3 GPU API implementation of the renderer.
/// Modern, shader-based renderer with cross-platform support.
/// </summary>
public class SDL3GPURenderer : IRenderer
{
    private readonly ILogger<SDL3GPURenderer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly RenderingOptions _options;
    private nint _window;
    private nint _device;
    private nint _commandBuffer;
    private nint _renderPass;
    private nint _graphicsPipeline;
    private IShaderLoader? _shaderLoader;
    private IShader? _vertexShader;
    private IShader? _fragmentShader;
    private Color _clearColor = Color.CornflowerBlue;
    private bool _disposed;

    public bool IsInitialized { get; private set; }
    public Color ClearColor { get; set; }

    public SDL3GPURenderer(ILogger<SDL3GPURenderer> logger, ILoggerFactory loggerFactory, IOptions<RenderingOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            _logger.LogWarning("GPU renderer already initialized");
            return;
        }

        _logger.LogInformation("Initializing SDL3 GPU renderer");

        // Initialize SDL3 with video
        if (!SDL3.SDL.Init(SDL3.SDL.InitFlags.Video))
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to initialize SDL3: {Error}", error);
            throw new InvalidOperationException($"Failed to initialize SDL3: {error}");
        }

        // Create window
        var windowFlags = SDL3.SDL.WindowFlags.Vulkan; // GPU API compatible
        if (_options.Resizable)
            windowFlags |= SDL3.SDL.WindowFlags.Resizable;
        if (_options.Fullscreen)
            windowFlags |= SDL3.SDL.WindowFlags.Fullscreen;

        _window = SDL3.SDL.CreateWindow(
            _options.WindowTitle,
            _options.WindowWidth,
            _options.WindowHeight,
            windowFlags
        );

        if (_window == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create window: {Error}", error);
            throw new InvalidOperationException($"Failed to create window: {error}");
        }

        // Create GPU device
        var shaderFormats = SDL3.SDL.GPUShaderFormat.SPIRV |
                            SDL3.SDL.GPUShaderFormat.MSL |
                            SDL3.SDL.GPUShaderFormat.DXIL;

        _device = SDL3.SDL.CreateGPUDevice(shaderFormats, true, _options.PreferredGPUDriver);

        if (_device == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create GPU device: {Error}", error);
            throw new InvalidOperationException($"Failed to create GPU device: {error}");
        }

        // Claim window for GPU rendering
        if (!SDL3.SDL.ClaimWindowForGPUDevice(_device, _window))
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to claim window for GPU: {Error}", error);
            throw new InvalidOperationException($"Failed to claim window: {error}");
        }

        var driverName = SDL3.SDL.GetGPUDeviceDriver(_device);
        _logger.LogInformation("GPU renderer initialized with driver: {Driver}", driverName);

        // Initialize shader loader
        _shaderLoader = new SDL3ShaderLoader(
            _loggerFactory.CreateLogger<SDL3ShaderLoader>(),
            _loggerFactory,
            _device);

        // Load default shaders
        _logger.LogInformation("Loading default shaders");
        try
        {
            (_vertexShader, _fragmentShader) = _shaderLoader.CreateDefaultShaders();
            _logger.LogInformation("Default shaders loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load default shaders");
            throw;
        }

        // TODO: Create graphics pipeline with shaders
        // This will be implemented when we add vertex buffer support

        IsInitialized = true;
        await Task.CompletedTask;
    }

    public void Clear(Color color)
    {
        _clearColor = color;
    }

    public void BeginFrame()
    {
        ThrowIfNotInitialized();

        // Acquire command buffer
        _commandBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
        if (_commandBuffer == nint.Zero)
        {
            _logger.LogError("Failed to acquire command buffer");
            return;
        }

        // Acquire swapchain texture
        if (!SDL3.SDL.AcquireGPUSwapchainTexture(_commandBuffer, _window, out var swapchainTexture, out _, out _))
        {
            _logger.LogError("Failed to acquire swapchain texture");
            SDL3.SDL.SubmitGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
            return;
        }

        if (swapchainTexture == nint.Zero)
        {
            // No swapchain texture available (window minimized, etc.)
            SDL3.SDL.SubmitGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
            return;
        }

        // Create color target info
        var colorTargetInfo = new SDL3.SDL.GPUColorTargetInfo
        {
            Texture = swapchainTexture,
            ClearColor = new SDL3.SDL.FColor
            {
                R = _clearColor.R / 255f,
                G = _clearColor.G / 255f,
                B = _clearColor.B / 255f,
                A = _clearColor.A / 255f
            },
            LoadOp = SDL3.SDL.GPULoadOp.Clear,
            StoreOp = SDL3.SDL.GPUStoreOp.Store
        };

        // Begin render pass - pass the array with one element and no depth stencil target
        var colorTargets = new[] { colorTargetInfo };
        _renderPass = SDL3.SDL.BeginGPURenderPass(_commandBuffer, colorTargets, 1, IntPtr.Zero);
        
        if (_renderPass == nint.Zero)
        {
            _logger.LogError("Failed to begin render pass");
            SDL3.SDL.SubmitGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
        }
    }

    public void EndFrame()
    {
        ThrowIfNotInitialized();

        if (_renderPass != nint.Zero)
        {
            SDL3.SDL.EndGPURenderPass(_renderPass);
            _renderPass = nint.Zero;
        }

        if (_commandBuffer != nint.Zero)
        {
            SDL3.SDL.SubmitGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
        }
    }

    public void DrawRectangleFilled(float x, float y, float width, float height, Color color)
    {
        throw new NotImplementedException();
    }

    public void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1)
    {
        throw new NotImplementedException();
    }

    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color)
    {
        throw new NotImplementedException();
    }

    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1)
    {
        throw new NotImplementedException();
    }

    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1)
    {
        throw new NotImplementedException();
    }

    public void DrawRectangle(float x, float y, float width, float height, Color color)
    {
        ThrowIfNotInitialized();
        
        // TODO: Implement rectangle drawing with vertex buffers and shaders
    }

    public void DrawTexture(ITexture texture, float x, float y)
    {
        throw new NotImplementedException();
    }

    public void DrawTexture(ITexture texture, float x, float y, float width, float height)
    {
        throw new NotImplementedException();
    }

    public void DrawTexture(ITexture texture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, float destX,
        float destY, float destWidth, float destHeight)
    {
        throw new NotImplementedException();
    }

    public void DrawText(string text, float x, float y, Color color)
    {
        ThrowIfNotInitialized();
        // TODO: Implement text rendering
    }

    public void SetDefaultFont(IFont? font)
    {
        throw new NotImplementedException();
    }

    public void DrawCircle(float centerX, float centerY, float radius, Color color)
    {
        ThrowIfNotInitialized();
        // TODO: Implement circle drawing with vertex buffers and shaders
    }

    public ICamera? Camera { get; set; }

    private void ThrowIfNotInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("GPU renderer is not initialized");
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing SDL3 GPU renderer");

        // Dispose shaders
        _vertexShader?.Dispose();
        _fragmentShader?.Dispose();

        // Release graphics pipeline
        if (_graphicsPipeline != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUGraphicsPipeline(_device, _graphicsPipeline);
            _graphicsPipeline = nint.Zero;
        }

        // Release device and window
        if (_device != nint.Zero)
        {
            SDL3.SDL.ReleaseWindowFromGPUDevice(_device, _window);
            SDL3.SDL.DestroyGPUDevice(_device);
            _device = nint.Zero;
        }

        if (_window != nint.Zero)
        {
            SDL3.SDL.DestroyWindow(_window);
            _window = nint.Zero;
        }

        SDL3.SDL.Quit();

        IsInitialized = false;
        _disposed = true;
    }
}