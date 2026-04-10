using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Rendering.PostProcessing;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.Common;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering;

/// <summary>
/// Manages render targets and post-processing effects.
/// </summary>
internal sealed class SDL3RenderTargetManager : IDisposable
{
    private readonly ILogger<SDL3RenderTargetManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly PostProcessingOptions? _postProcessingOptions;
    private readonly SDL3PostProcessPipeline? _postProcessPipeline;
    
    private GpuDeviceHandle? _deviceHandle;
    private IRenderTarget? _currentRenderTarget;
    private readonly Stack<IRenderTarget?> _renderTargetStack = new();
    
    private RenderTarget? _mainRenderTarget;
    private RenderTarget? _pingPongTarget;
    
    private int _disposed;

    public bool UsePostProcessing => _postProcessingOptions?.Enabled == true && _postProcessPipeline != null;
    public IRenderTarget? CurrentRenderTarget => _currentRenderTarget;
    public RenderTarget? MainRenderTarget => _mainRenderTarget;
    public RenderTarget? PingPongTarget => _pingPongTarget;
    public int RenderTargetStackDepth => _renderTargetStack.Count;

    public SDL3.SDL.GPUTextureFormat PostProcessFormat =>
        _postProcessingOptions?.RenderTargetFormat ?? _defaultFormat;

    private SDL3.SDL.GPUTextureFormat _defaultFormat;

    public SDL3RenderTargetManager(
        ILogger<SDL3RenderTargetManager> logger,
        ILoggerFactory loggerFactory,
        PostProcessingOptions? postProcessingOptions,
        SDL3PostProcessPipeline? postProcessPipeline)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _postProcessingOptions = postProcessingOptions;
        _postProcessPipeline = postProcessPipeline;
    }
    
    public void Initialize(GpuDeviceHandle deviceHandle, SDL3.SDL.GPUTextureFormat defaultFormat)
    {
        _deviceHandle = deviceHandle;
        _defaultFormat = defaultFormat;
    }

    public void ResetFrameState()
    {
        if (_renderTargetStack.Count > 0)
        {
            _logger.LogWarning("Render target stack had {Count} unpopped entries at frame boundary", _renderTargetStack.Count);
            _renderTargetStack.Clear();
        }

        _currentRenderTarget = null;
    }

    public IRenderTarget CreateRenderTarget(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                width <= 0 ? nameof(width) : nameof(height),
                "Render target dimensions must be positive");
        }

        if (_deviceHandle == null)
            throw new InvalidOperationException("Render target manager must be initialized before creating render targets.");

        if (width > 16384 || height > 16384)
        {
            _logger.LogWarning("Large render target requested: {Width}x{Height} - may fail on some GPUs", 
                width, height);
        }
        
        var renderTarget = new RenderTarget(
            _deviceHandle, 
            width, 
            height, 
            _defaultFormat,
            _loggerFactory.CreateLogger<RenderTarget>());
        
        _logger.LogDebug("Created render target: {Width}x{Height}", width, height);
        return renderTarget;
    }
    
    public void SetRenderTarget(IRenderTarget? target)
    {
        _currentRenderTarget = target;
        _logger.LogTrace("Render target set to: {Target}", 
            target == null ? "Screen" : $"{target.Width}x{target.Height}");
    }
    
    public void PushRenderTarget(IRenderTarget? target)
    {
        _renderTargetStack.Push(_currentRenderTarget);
        SetRenderTarget(target);
    }
    
    public void PopRenderTarget()
    {
        if (_renderTargetStack.Count == 0)
        {
            throw new InvalidOperationException("Cannot pop render target: stack is empty");
        }
        
        var previousTarget = _renderTargetStack.Pop();
        SetRenderTarget(previousTarget);
    }

    public void CreatePostProcessingTargets(int width, int height)
    {
        if (!UsePostProcessing)
            return;

        if (_deviceHandle == null)
            throw new InvalidOperationException("Render target manager must be initialized before creating post-processing targets.");

        var format = _postProcessingOptions?.RenderTargetFormat ?? _defaultFormat;

        var newMain = new RenderTarget(_deviceHandle, width, height, format,
            _loggerFactory.CreateLogger<RenderTarget>());

        RenderTarget newPingPong;
        try
        {
            newPingPong = new RenderTarget(_deviceHandle, width, height, format,
                _loggerFactory.CreateLogger<RenderTarget>());
        }
        catch
        {
            newMain.Dispose();
            throw;
        }

        _mainRenderTarget?.Dispose();
        _mainRenderTarget = newMain;

        _pingPongTarget?.Dispose();
        _pingPongTarget = newPingPong;

        _postProcessPipeline?.SetEffectDimensions(width, height);

        _logger.LogInformation("Created render targets for post-processing: {Width}x{Height}",
            width, height);
    }

    public bool ApplyPostProcessing(
        IRenderer renderer, 
        nint swapchainTexture,
        nint commandBuffer,
        int swapchainWidth,
        int swapchainHeight)
    {
        if (!UsePostProcessing || _mainRenderTarget == null || _pingPongTarget == null || _postProcessPipeline == null)
            return false;
        
        return _postProcessPipeline.Execute(
            renderer,
            _mainRenderTarget.TextureHandle, 
            swapchainTexture, 
            commandBuffer, 
            _pingPongTarget,
            swapchainWidth,
            swapchainHeight);
    }
    
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;
        
        _mainRenderTarget?.Dispose();
        _pingPongTarget?.Dispose();
    }
}