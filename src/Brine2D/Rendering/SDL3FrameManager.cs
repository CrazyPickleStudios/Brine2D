using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering;

/// <summary>
/// Manages frame lifecycle including command buffers, swapchain acquisition, and presentation.
/// </summary>
internal sealed class SDL3FrameManager
{
    private const int MaxInFlightFrames = 2;

    private readonly ILogger<SDL3FrameManager> _logger;

    private nint _device;
    private nint _window;
    private nint _commandBuffer;
    private nint _swapchainTexture;
    private readonly nint[] _inFlightFences = new nint[MaxInFlightFrames];
    private int _fenceSlot;

    public nint CommandBuffer => _commandBuffer;
    public nint SwapchainTexture => _swapchainTexture;
    public bool HasActiveFrame => _commandBuffer != nint.Zero && _swapchainTexture != nint.Zero;
    public bool IsFirstFlush { get; private set; } = true;
    
    public SDL3FrameManager(ILogger<SDL3FrameManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public void Initialize(nint device, nint window, bool vsync)
    {
        _device = device;
        _window = window;

        var presentMode = vsync
            ? SDL3.SDL.GPUPresentMode.VSync
            : SDL3.SDL.GPUPresentMode.Immediate;

        if (!SDL3.SDL.SetGPUSwapchainParameters(_device, _window, SDL3.SDL.GPUSwapchainComposition.SDR, presentMode))
        {
            var error = SDL3.SDL.GetError();
            _logger.LogWarning("Failed to set swapchain parameters ({PresentMode}): {Error}", presentMode, error);
        }
    }
    
    /// <summary>
    /// Begin a new frame by acquiring command buffer and swapchain texture.
    /// </summary>
    /// <returns>True if frame resources were successfully acquired, false otherwise.</returns>
    public bool BeginFrame()
    {
        if (_device == nint.Zero || _window == nint.Zero)
            return false;

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
            SDL3.SDL.SubmitGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
            _swapchainTexture = nint.Zero;
            return false;
        }

        if (_swapchainTexture == nint.Zero)
        {
            SDL3.SDL.SubmitGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
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
    /// End the current frame, submit the command buffer, and block until the
    /// fence from <see cref="MaxInFlightFrames"/> frames ago is signaled.
    /// This bounds the CPU to at most <see cref="MaxInFlightFrames"/> frames ahead
    /// of the GPU regardless of VSync or frame-rate cap settings.
    /// </summary>
    public void EndFrame()
    {
        if (_commandBuffer == nint.Zero)
        {
            _swapchainTexture = nint.Zero;
            return;
        }

        var fence = SDL3.SDL.SubmitGPUCommandBufferAndAcquireFence(_commandBuffer);
        _commandBuffer = nint.Zero;
        _swapchainTexture = nint.Zero;

        var oldFence = _inFlightFences[_fenceSlot];
        if (oldFence != nint.Zero)
        {
            var fenceArray = new nint[] { oldFence };
            SDL3.SDL.WaitForGPUFences(_device, true, fenceArray, (uint)fenceArray.Length);
            SDL3.SDL.ReleaseGPUFence(_device, oldFence);
        }

        _inFlightFences[_fenceSlot] = fence;
        _fenceSlot = (_fenceSlot + 1) % MaxInFlightFrames;
    }

    /// <summary>
    /// Releases any fence handles still held in the in-flight circular buffer.
    /// Call this after <see cref="SDL3.SDL.WaitForGPUIdle"/> at shutdown so all
    /// fences are already signaled before releasing them.
    /// </summary>
    public void ReleaseInFlightFences()
    {
        for (int i = 0; i < MaxInFlightFrames; i++)
        {
            if (_inFlightFences[i] != nint.Zero)
            {
                SDL3.SDL.ReleaseGPUFence(_device, _inFlightFences[i]);
                _inFlightFences[i] = nint.Zero;
            }
        }
    }
}