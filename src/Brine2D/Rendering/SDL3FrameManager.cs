using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering;

/// <summary>
/// Manages frame lifecycle including command buffers, swapchain acquisition, and presentation.
/// </summary>
internal sealed class SDL3FrameManager : IDisposable
{
    internal const int MaxInFlightFrames = 2;

    private readonly ILogger<SDL3FrameManager> _logger;

    private nint _device;
    private nint _window;
    private nint _commandBuffer;
    private nint _swapchainTexture;
    private uint _swapchainWidth;
    private uint _swapchainHeight;
    private readonly nint[] _inFlightFences = new nint[MaxInFlightFrames];
    private readonly nint[] _singleFenceBuf = new nint[1];
    private int _fenceSlot;
    private int _disposed;

    public nint CommandBuffer => _commandBuffer;
    public nint SwapchainTexture => _swapchainTexture;
    public uint SwapchainWidth => _swapchainWidth;
    public uint SwapchainHeight => _swapchainHeight;
    public bool HasActiveFrame => _commandBuffer != nint.Zero && _swapchainTexture != nint.Zero;
    public int CurrentFrameSlot => _fenceSlot;

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
    /// Waits for the oldest in-flight fence first to guarantee the corresponding
    /// swapchain image and transfer buffer have been released by the GPU.
    /// </summary>
    /// <returns>True if frame resources were successfully acquired, false otherwise.</returns>
    public bool BeginFrame()
    {
        if (_device == nint.Zero || _window == nint.Zero)
            return false;

        if (_commandBuffer != nint.Zero)
        {
            _logger.LogWarning("BeginFrame called with an active command buffer; cancelling previous buffer");
            SDL3.SDL.CancelGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
        }

        var oldFence = _inFlightFences[_fenceSlot];
        if (oldFence != nint.Zero)
        {
            _singleFenceBuf[0] = oldFence;
            SDL3.SDL.WaitForGPUFences(_device, true, _singleFenceBuf, 1);
            SDL3.SDL.ReleaseGPUFence(_device, oldFence);
            _inFlightFences[_fenceSlot] = nint.Zero;
        }

        _commandBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
        if (_commandBuffer == nint.Zero)
        {
            _logger.LogWarning("Failed to acquire command buffer, skipping frame");
            _swapchainTexture = nint.Zero;
            _swapchainWidth = 0;
            _swapchainHeight = 0;
            return false;
        }

        if (!SDL3.SDL.AcquireGPUSwapchainTexture(_commandBuffer, _window, out _swapchainTexture, out _swapchainWidth, out _swapchainHeight))
        {
            _logger.LogDebug("Failed to acquire swapchain texture (window may be minimized)");
            SDL3.SDL.CancelGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
            _swapchainTexture = nint.Zero;
            _swapchainWidth = 0;
            _swapchainHeight = 0;
            return false;
        }

        if (_swapchainTexture == nint.Zero)
        {
            SDL3.SDL.CancelGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
            _swapchainWidth = 0;
            _swapchainHeight = 0;
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Blit a texture to the swapchain. Source dimensions come from the caller;
    /// destination dimensions are the actual swapchain size acquired this frame.
    /// </summary>
    public void BlitToSwapchain(nint sourceTexture, int sourceWidth, int sourceHeight)
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
                W = (uint)sourceWidth,
                H = (uint)sourceHeight
            },
            Destination = new SDL3.SDL.GPUBlitRegion
            {
                Texture = _swapchainTexture,
                MipLevel = 0,
                LayerOrDepthPlane = 0,
                X = 0,
                Y = 0,
                W = _swapchainWidth,
                H = _swapchainHeight
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
    /// End the current frame and submit the command buffer.
    /// The fence is stored for the next <see cref="BeginFrame"/> call to wait on,
    /// bounding the CPU to at most <see cref="MaxInFlightFrames"/> frames ahead of the GPU.
    /// </summary>
    public void EndFrame()
    {
        if (_commandBuffer == nint.Zero)
        {
            _swapchainTexture = nint.Zero;
            _swapchainWidth = 0;
            _swapchainHeight = 0;
            return;
        }

        var fence = SDL3.SDL.SubmitGPUCommandBufferAndAcquireFence(_commandBuffer);
        _commandBuffer = nint.Zero;
        _swapchainTexture = nint.Zero;
        _swapchainWidth = 0;
        _swapchainHeight = 0;

        if (fence != nint.Zero)
            _inFlightFences[_fenceSlot] = fence;
        else
            _logger.LogError("SubmitGPUCommandBufferAndAcquireFence returned null: {Error}", SDL3.SDL.GetError());

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

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        if (_commandBuffer != nint.Zero)
        {
            SDL3.SDL.CancelGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
        }

        ReleaseInFlightFences();
        _device = nint.Zero;
        _window = nint.Zero;
    }
}