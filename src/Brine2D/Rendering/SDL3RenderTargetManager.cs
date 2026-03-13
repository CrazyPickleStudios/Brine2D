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
    
    private nint _device;
    private IRenderTarget? _currentRenderTarget;
    private readonly Stack<IRenderTarget?> _renderTargetStack = new();
    
    private RenderTarget? _mainRenderTarget;
    private RenderTarget? _pingPongTarget;
    
    private bool _disposed;
    
    public bool UsePostProcessing { get; }
    public IRenderTarget? CurrentRenderTarget => _currentRenderTarget;
    public RenderTarget? MainRenderTarget => _mainRenderTarget;
    public RenderTarget? PingPongTarget => _pingPongTarget;
    
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
        
        UsePostProcessing = _postProcessingOptions?.Enabled == true && _postProcessPipeline != null;
    }
    
    public void Initialize(nint device)
    {
        _device = device;
    }
    
    public IRenderTarget CreateRenderTarget(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                width <= 0 ? nameof(width) : nameof(height),
                "Render target dimensions must be positive");
        }
        
        if (width > 16384 || height > 16384)
        {
            _logger.LogWarning("Large render target requested: {Width}x{Height} - may fail on some GPUs", 
                width, height);
        }
        
        var format = SDL3.SDL.GPUTextureFormat.R8G8B8A8Unorm;
        var renderTarget = new RenderTarget(
            _device, 
            width, 
            height, 
            format,
            _loggerFactory.CreateLogger<RenderTarget>());
        
        _logger.LogDebug("Created render target: {Width}x{Height}", width, height);
        return renderTarget;
    }
    
    public void SetRenderTarget(IRenderTarget? target)
    {
        _currentRenderTarget = target;
        _logger.LogDebug("Render target set to: {Target}", 
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
            _logger.LogWarning("PopRenderTarget called with empty stack");
            return;
        }
        
        var previousTarget = _renderTargetStack.Pop();
        SetRenderTarget(previousTarget);
    }
    
    public void CreatePostProcessingTargets(int width, int height)
    {
        if (!UsePostProcessing)
            return;
        
        var format = _postProcessingOptions?.RenderTargetFormat ?? SDL3.SDL.GPUTextureFormat.R8G8B8A8Unorm;

        _mainRenderTarget?.Dispose();
        _mainRenderTarget = new RenderTarget(_device, width, height, format,
            _loggerFactory.CreateLogger<RenderTarget>());

        _pingPongTarget?.Dispose();
        _pingPongTarget = new RenderTarget(_device, width, height, format,
            _loggerFactory.CreateLogger<RenderTarget>());

        _logger.LogInformation("Created render targets for post-processing: {Width}x{Height}",
            width, height);
    }
    
    public bool ApplyPostProcessing(
        IRenderer renderer, 
        nint swapchainTexture,
        nint commandBuffer)
    {
        if (!UsePostProcessing || _mainRenderTarget == null || _pingPongTarget == null || _postProcessPipeline == null)
            return false;
        
        // Execute pipeline - it returns true if any effects were applied
        return _postProcessPipeline.Execute(
            renderer,
            _mainRenderTarget.TextureHandle, 
            swapchainTexture, 
            commandBuffer, 
            _pingPongTarget);
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _mainRenderTarget?.Dispose();
        _pingPongTarget?.Dispose();
        
        if (_postProcessPipeline is IDisposable disposablePipeline)
        {
            disposablePipeline.Dispose();
        }
        
        _disposed = true;
    }
}