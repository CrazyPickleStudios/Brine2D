using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering;

/// <summary>
/// Manages frame lifecycle including command buffers, swapchain acquisition, and presentation.
/// </summary>
internal sealed class SDL3FrameManager
{
    private readonly ILogger<SDL3FrameManager> _logger;
    
    private nint _device;
    private nint _window;
    private nint _commandBuffer;
    private nint _swapchainTexture;
    
    public nint CommandBuffer => _commandBuffer;
    public nint SwapchainTexture => _swapchainTexture;
    public bool HasActiveFrame => _commandBuffer != nint.Zero && _swapchainTexture != nint.Zero;
    public bool IsFirstFlush { get; private set; } = true;
    
    public SDL3FrameManager(ILogger<SDL3FrameManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public void Initialize(nint device, nint window)
    {
        _device = device;
        _window = window;
    }
    
    /// <summary>
    /// Begin a new frame by acquiring command buffer and swapchain texture.
    /// </summary>
    /// <returns>True if frame resources were successfully acquired, false otherwise.</returns>
    public bool BeginFrame()
    {
        IsFirstFlush = true;
        
        _commandBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
        if (_commandBuffer == nint.Zero)
        {
            _logger.LogWarning("Failed to acquire command buffer, skipping frame");
            _swapchainTexture = nint.Zero;
            return false;
        }

        if (!SDL3.SDL.AcquireGPUSwapchainTexture(_commandBuffer, _window, out _swapchainTexture, out _, out _))
        {
            _logger.LogDebug("Failed to acquire swapchain texture (window may be minimized)");
            SDL3.SDL.CancelGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
            _swapchainTexture = nint.Zero;
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Mark that the first flush has occurred (for clear vs load operations).
    /// </summary>
    public void MarkFirstFlushComplete()
    {
        IsFirstFlush = false;
    }
    
    /// <summary>
    /// Blit a texture to the swapchain.
    /// </summary>
    public void BlitToSwapchain(nint sourceTexture, int viewportWidth, int viewportHeight)
    {
        if (!HasActiveFrame)
        {
            _logger.LogWarning("Attempted to blit without an active frame");
            return;
        }
        
        var blitInfo = new SDL3.SDL.GPUBlitInfo
        {
            Source = new SDL3.SDL.GPUBlitRegion
            {
                Texture = sourceTexture,
                MipLevel = 0,
                LayerOrDepthPlane = 0,
                X = 0,
                Y = 0,
                W = (uint)viewportWidth,
                H = (uint)viewportHeight
            },
            Destination = new SDL3.SDL.GPUBlitRegion
            {
                Texture = _swapchainTexture,
                MipLevel = 0,
                LayerOrDepthPlane = 0,
                X = 0,
                Y = 0,
                W = (uint)viewportWidth,
                H = (uint)viewportHeight
            },
            LoadOp = SDL3.SDL.GPULoadOp.Load,
            ClearColor = new SDL3.SDL.FColor { R = 0, G = 0, B = 0, A = 1 },
            FlipMode = SDL3.SDL.FlipMode.None,
            Filter = SDL3.SDL.GPUFilter.Linear,
            Cycle = 0
        };

        SDL3.SDL.BlitGPUTexture(_commandBuffer, ref blitInfo);
    }
    
    /// <summary>
    /// End the current frame and submit the command buffer for presentation.
    /// </summary>
    public void EndFrame()
    {
        if (!HasActiveFrame)
        {
            return;
        }
        
        if (_commandBuffer != nint.Zero)
        {
            SDL3.SDL.SubmitGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
        }

        _swapchainTexture = nint.Zero;
    }
}