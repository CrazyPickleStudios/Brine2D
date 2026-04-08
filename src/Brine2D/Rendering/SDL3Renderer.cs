using Brine2D.Common;
using Brine2D.Core;
using Brine2D.Events;
using Brine2D.Rendering.SDL.PostProcessing;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Brine2D.Rendering;

/// <summary>
/// SDL3 GPU API implementation of the renderer.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Requires a live SDL3 GPU context; covered by manual/hardware testing.")]
internal sealed partial class SDL3Renderer : IRenderer, ISDL3WindowProvider, ITextureContext
{
    private readonly ILogger<SDL3Renderer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly RenderingOptions _renderingOptions;
    private readonly WindowOptions _windowOptions;
    private readonly IEventBus? _eventBus;

    private readonly SDL3RenderTargetManager _renderTargetManager;
    private readonly SDL3StateManager _stateManager;
    private readonly SDL3TextRenderer _textRenderer;
    private readonly SDL3BatchRenderer _batchRenderer;
    private readonly SDL3FrameManager _frameManager;
    private readonly SDL3PostProcessPipeline? _postProcessPipeline;

    private nint _window;
    private nint _device;
    private GpuDeviceHandle? _gpuDeviceHandle;
    private SDL3.SDL.GPUTextureFormat _swapchainFormat;
    private nint _sampler;
    private nint _samplerNearest;
    private nint _whiteTexture;
    private nint _vertexBuffer;

    private readonly SDL3.SDL.GPUColorTargetInfo[] _colorTargetInfoBuf = new SDL3.SDL.GPUColorTargetInfo[1];
    private readonly SDL3.SDL.GPUTextureSamplerBinding[] _textureSamplerBindingBuf = new SDL3.SDL.GPUTextureSamplerBinding[1];
    private readonly SDL3.SDL.GPUBufferBinding[] _vertexBufferBindingBuf = new SDL3.SDL.GPUBufferBinding[1];

    private readonly List<DrawCallRecord> _pendingDrawCalls = new(64);
    private readonly HashSet<nint> _clearedSystemTargetsThisFrame = new(4);
    private readonly HashSet<RenderTarget> _clearedUserTargetsThisFrame = new(4);
    private readonly List<PendingTextureUpload> _pendingUploads = new();

    private IShaderLoader? _shaderLoader;
    private IShader? _vertexShader;
    private IShader? _fragmentShader;

    private Color _clearColor = new Color(52, 78, 65, 255);

    private readonly nint[] _blendModePipelines = new nint[Enum.GetValues<BlendMode>().Length];
    private readonly nint[] _postProcessBlendModePipelines = new nint[Enum.GetValues<BlendMode>().Length];
    private SDL3.SDL.GPUTextureFormat _postProcessFormat;
    private bool _hasDistinctPostProcessFormat;
    private readonly Action _flushBatchAction;

    private int _disposed;
    private bool _isRenderSuspended;
    private bool _postProcessingAppliedThisFrame;
    private bool _postProcessingFallbackWarningLogged;
    private bool _postProcessingResizePending;
    private bool _viewportResizePending;
    private bool _windowClaimed;
    private bool _zeroHandleWarningLoggedThisFrame;
    private bool _customFontWarningLogged;

    private int _viewportWidth;
    private int _viewportHeight;
    private int _pendingViewportWidth;
    private int _pendingViewportHeight;

    private readonly record struct DrawCallRecord(
        int FirstVertex,
        int VertexCount,
        nint TextureHandle,
        TextureScaleMode ScaleMode,
        nint RenderTargetHandle,
        int TargetWidth,
        int TargetHeight,
        nint Pipeline,
        SDL3.SDL.Rect Scissor,
        bool IsUserRenderTarget,
        RenderTarget? UserRenderTarget,
        ITexture? TextureRef
    )
    {
        public nint ResolvedRenderTargetHandle => IsUserRenderTarget && UserRenderTarget != null
            ? UserRenderTarget.TextureHandle
            : RenderTargetHandle;

        public nint ResolvedTextureHandle => TextureRef switch
        {
            SDL3Texture t => t.Handle,
            RenderTargetTextureView v => v.Handle,
            _ => TextureHandle
        };
    }

    private readonly record struct PendingTextureUpload(nint Fence, nint TransferBuffer, SDL3Texture Texture);

    public Color ClearColor
    {
        get => _clearColor;
        set => _clearColor = value;
    }

    public nint Window => _window;
    public nint Device => _device;
    internal GpuDeviceHandle? GpuDevice => _gpuDeviceHandle;
    public bool IsInitialized { get; private set; }

    public ICamera? Camera
    {
        get => _stateManager.Camera;
        set => _stateManager.Camera = value;
    }

    public int Width => _viewportWidth;
    public int Height => _viewportHeight;

    public SDL3Renderer(
        ILogger<SDL3Renderer> logger,
        ILoggerFactory loggerFactory,
        RenderingOptions renderingOptions,
        WindowOptions windowOptions,
        PostProcessingOptions? postProcessingOptions = null,
        SDL3PostProcessPipeline? postProcessPipeline = null,
        IFontLoader? fontLoader = null,
        IEventBus? eventBus = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _renderingOptions = renderingOptions ?? throw new ArgumentNullException(nameof(renderingOptions));
        _windowOptions = windowOptions ?? throw new ArgumentNullException(nameof(windowOptions));

        _eventBus = eventBus;
        if (_eventBus == null)
            _logger.LogWarning("No event bus provided; window resize/hide/show events will not be handled");
        
        _viewportWidth = _windowOptions.Width;
        _viewportHeight = _windowOptions.Height;
        
        _stateManager = new SDL3StateManager(_loggerFactory.CreateLogger<SDL3StateManager>());

        _textRenderer = new SDL3TextRenderer(
            _loggerFactory.CreateLogger<SDL3TextRenderer>(),
            _loggerFactory,
            fontLoader);
    
        _batchRenderer = new SDL3BatchRenderer(
            _loggerFactory.CreateLogger<SDL3BatchRenderer>(),
            renderingOptions);

        _renderTargetManager = new SDL3RenderTargetManager(
            _loggerFactory.CreateLogger<SDL3RenderTargetManager>(),
            _loggerFactory,
            postProcessingOptions,
            postProcessPipeline);

        _postProcessPipeline = postProcessPipeline;
        _frameManager = new SDL3FrameManager(_loggerFactory.CreateLogger<SDL3FrameManager>());
        _flushBatchAction = FlushBatch;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            _logger.LogWarning("GPU renderer already initialized");
            return;
        }

        _logger.LogInformation("Initializing SDL3 GPU renderer on thread {ThreadId}",
            Thread.CurrentThread.ManagedThreadId);

        try
        {
            var windowFlags = SDL3.SDL.WindowFlags.Vulkan;
            if (_windowOptions.Resizable)
                windowFlags |= SDL3.SDL.WindowFlags.Resizable;
            if (_windowOptions.Fullscreen)
                windowFlags |= SDL3.SDL.WindowFlags.Fullscreen;

            _window = SDL3.SDL.CreateWindow(
                _windowOptions.Title,    
                _windowOptions.Width,    
                _windowOptions.Height,  
                windowFlags
            );

            if (_window == nint.Zero)
            {
                var error = SDL3.SDL.GetError();
                _logger.LogError("Failed to create window: {Error}", error);
                throw new InvalidOperationException($"Failed to create window: {error}");
            }

            var shaderFormats = SDL3.SDL.GPUShaderFormat.SPIRV |
                                SDL3.SDL.GPUShaderFormat.MSL |
                                SDL3.SDL.GPUShaderFormat.DXIL;

#if DEBUG
            const bool gpuDebugMode = true;
#else
            const bool gpuDebugMode = false;
#endif
            _device = SDL3.SDL.CreateGPUDevice(shaderFormats, gpuDebugMode, _renderingOptions.PreferredGPUDriver.ToSDL3DriverName());

            if (_device == nint.Zero)
            {
                var error = SDL3.SDL.GetError();
                _logger.LogError("Failed to create GPU device: {Error}", error);
                throw new InvalidOperationException($"Failed to create GPU device: {error}");
            }

            _gpuDeviceHandle = new GpuDeviceHandle(_device);

            if (!SDL3.SDL.ClaimWindowForGPUDevice(_device, _window))
            {
                var error = SDL3.SDL.GetError();
                _logger.LogError("Failed to claim window for GPU: {Error}", error);
                throw new InvalidOperationException($"Failed to claim window: {error}");
            }

            _windowClaimed = true;

            _swapchainFormat = SDL3.SDL.GetGPUSwapchainTextureFormat(_device, _window);

            var driverName = SDL3.SDL.GetGPUDeviceDriver(_device);
            _logger.LogInformation("GPU renderer initialized with driver: {Driver}, swapchain format: {Format}",
                driverName, _swapchainFormat);

            _shaderLoader = new SDL3ShaderLoader(
                _loggerFactory.CreateLogger<SDL3ShaderLoader>(),
                _loggerFactory,
                _device);

            _logger.LogInformation("Loading default shaders");
            (_vertexShader, _fragmentShader) = _shaderLoader.CreateDefaultShaders();
            _logger.LogDebug("Default shaders loaded");

            CreateSamplers();
            CreateWhiteTexture();
            CreateGraphicsPipeline();
            CreateVertexBuffer();
            _batchRenderer.Initialize(_device, _vertexBuffer, _whiteTexture);
            _renderTargetManager.Initialize(_gpuDeviceHandle!, _swapchainFormat);
            _frameManager.Initialize(_device, _window, _renderingOptions.VSync);
            CreatePostProcessPipelines();

            await _textRenderer.LoadDefaultFontAsync(cancellationToken);

            if (_renderTargetManager.UsePostProcessing)
            {
                _renderTargetManager.CreatePostProcessingTargets(_viewportWidth, _viewportHeight);
                _logger.LogInformation("Post-processing enabled");
            }

            _eventBus?.Subscribe<WindowResizedEvent>(OnWindowResized);
            _eventBus?.Subscribe<WindowHiddenEvent>(OnWindowHidden);
            _eventBus?.Subscribe<WindowShownEvent>(OnWindowShown);

            IsInitialized = true;
            _logger.LogInformation("SDL3 GPU renderer fully initialized");
        }
        catch
        {
            _logger.LogError("Initialization failed, releasing partially created GPU resources");
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Begins a new frame. Silently returns (no-op) when the renderer is not initialized,
    /// suspended, or unable to acquire GPU resources. Draw calls issued without a successful
    /// <see cref="BeginFrame"/> are dropped.
    /// </summary>
    public void BeginFrame()
    {
        if (Volatile.Read(ref _disposed) == 1 || !IsInitialized)
            return;

        PollPendingUploads();

        if (_isRenderSuspended)
            return;

        if (_viewportResizePending)
        {
            _viewportWidth = _pendingViewportWidth;
            _viewportHeight = _pendingViewportHeight;
            _viewportResizePending = false;
        }

        if (_postProcessingResizePending)
        {
            _renderTargetManager.CreatePostProcessingTargets(_viewportWidth, _viewportHeight);
            _postProcessingResizePending = false;
        }

        _batchRenderer.Clear();
        _pendingDrawCalls.Clear();
        _clearedSystemTargetsThisFrame.Clear();
        _clearedUserTargetsThisFrame.Clear();
        _postProcessingAppliedThisFrame = false;
        _zeroHandleWarningLoggedThisFrame = false;
        _customFontWarningLogged = false;

        _stateManager.ResetFrameState();
        _renderTargetManager.ResetFrameState();

        if (!_frameManager.BeginFrame())
            return;

        _batchRenderer.NewFrame(_frameManager.CurrentFrameSlot);
    }

    /// <summary>
    /// Applies post-processing effects to the current frame.
    /// Only works if post-processing is enabled via options.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is called automatically by the framework unless
    /// <see cref="ISceneLifecycleControl.EnableAutomaticFrameManagement"/> is false.
    /// </para>
    /// <para>
    /// Use this for advanced scenarios where you want to render UI or other
    /// elements after post-processing effects are applied to the game world.
    /// After this call, subsequent draw calls render directly to the swapchain
    /// (bypassing the post-processing render target).
    /// </para>
    /// </remarks>
    public void ApplyPostProcessing()
    {
        ThrowIfNotInitialized();

        if (!_frameManager.HasActiveFrame || _postProcessingAppliedThisFrame)
        {
            return;
        }

        FlushBatch();
        ExecuteDrawCalls();

        bool effectsApplied = _renderTargetManager.ApplyPostProcessing(
            this,
            _frameManager.SwapchainTexture,
            _frameManager.CommandBuffer);

        if (!effectsApplied && _renderTargetManager.MainRenderTarget != null)
        {
            var mainRT = _renderTargetManager.MainRenderTarget;
            _frameManager.BlitToSwapchain(
                mainRT.TextureHandle,
                mainRT.Width,
                mainRT.Height);
        }

        _clearedSystemTargetsThisFrame.Add(_frameManager.SwapchainTexture);
        _postProcessingAppliedThisFrame = true;
        _postProcessingFallbackWarningLogged = false;
    }

    /// <summary>
    /// Ends the current frame and submits the command buffer.
    /// Silently returns (no-op) when the renderer is not initialized or disposed.
    /// When suspended, pending draw calls are discarded but the command buffer
    /// is still submitted so GPU resources are not leaked.
    /// </summary>
    public void EndFrame()
    {
        if (Volatile.Read(ref _disposed) == 1 || !IsInitialized)
            return;

        if (!_isRenderSuspended)
        {
            if (_batchRenderer.VertexCount > 0 || _pendingDrawCalls.Count > 0)
            {
                FlushBatch();
                ExecuteDrawCalls();
            }

            if (_renderTargetManager.UsePostProcessing &&
                !_postProcessingAppliedThisFrame &&
                _frameManager.HasActiveFrame &&
                _renderTargetManager.MainRenderTarget != null)
            {
                if (!_postProcessingFallbackWarningLogged)
                {
                    _logger.LogWarning("Post-processing is enabled but ApplyPostProcessing() was not called this frame; blitting main RT to swapchain as fallback");
                    _postProcessingFallbackWarningLogged = true;
                }

                var mainRT = _renderTargetManager.MainRenderTarget;
                if (_clearedSystemTargetsThisFrame.Contains(mainRT.TextureHandle))
                {
                    _frameManager.BlitToSwapchain(
                        mainRT.TextureHandle,
                        mainRT.Width,
                        mainRT.Height);
                    _clearedSystemTargetsThisFrame.Add(_frameManager.SwapchainTexture);
                }
            }

            if (_frameManager.HasActiveFrame &&
                !_clearedSystemTargetsThisFrame.Contains(_frameManager.SwapchainTexture))
            {
                EnsureSwapchainCleared();
            }

            Debug.Assert(_renderTargetManager.RenderTargetStackDepth == 0,
                $"Render target stack has {_renderTargetManager.RenderTargetStackDepth} unpopped entries at EndFrame");
            Debug.Assert(_stateManager.ScissorRectStackDepth == 0,
                $"Scissor rect stack has {_stateManager.ScissorRectStackDepth} unpopped entries at EndFrame");

            _renderTargetManager.ResetFrameState();
            _stateManager.ResetFrameState();
        }

        _frameManager.EndFrame();
    }

    private unsafe void EnsureSwapchainCleared()
    {
        var clearColor = new SDL3.SDL.FColor
        {
            R = _clearColor.R / 255f,
            G = _clearColor.G / 255f,
            B = _clearColor.B / 255f,
            A = _clearColor.A / 255f
        };

        _colorTargetInfoBuf[0] = new SDL3.SDL.GPUColorTargetInfo
        {
            Texture = _frameManager.SwapchainTexture,
            ClearColor = clearColor,
            LoadOp = SDL3.SDL.GPULoadOp.Clear,
            StoreOp = SDL3.SDL.GPUStoreOp.Store
        };

        nint renderPass;
        fixed (SDL3.SDL.GPUColorTargetInfo* colorTargetPtr = _colorTargetInfoBuf)
        {
            renderPass = SDL3.SDL.BeginGPURenderPass(
                _frameManager.CommandBuffer,
                (nint)colorTargetPtr,
                1,
                nint.Zero);
        }

        if (renderPass != nint.Zero)
            SDL3.SDL.EndGPURenderPass(renderPass);
    }

    public IRenderTarget CreateRenderTarget(int width, int height)
    {
        ThrowIfNotInitialized();
        return _renderTargetManager.CreateRenderTarget(width, height);
    }

    public void SetRenderTarget(IRenderTarget? target)
    {
        ThrowIfNotInitialized();

        FlushBatch();
        ExecuteDrawCalls();

        _renderTargetManager.SetRenderTarget(target);
    }

    public IRenderTarget? GetRenderTarget()
    {
        ThrowIfNotInitialized();
        return _renderTargetManager.CurrentRenderTarget;
    }

    /// <summary>
    /// Push the current render target onto a stack and set a new one.
    /// Useful for nested render-to-texture operations.
    /// </summary>
    public void PushRenderTarget(IRenderTarget? target)
    {
        ThrowIfNotInitialized();
        FlushBatch();
        ExecuteDrawCalls();
        _renderTargetManager.PushRenderTarget(target);
    }

    /// <summary>
    /// Pop the current render target from the stack.
    /// </summary>
    public void PopRenderTarget()
    {
        ThrowIfNotInitialized();
        FlushBatch();
        ExecuteDrawCalls();
        _renderTargetManager.PopRenderTarget();
    }

    public void SetScissorRect(Rectangle? rect)
    {
        ThrowIfNotInitialized();

        FlushBatch();

        var (w, h) = GetActiveViewportDimensions();
        _stateManager.SetScissorRect(rect, w, h);
    }

    public Rectangle? GetScissorRect()
    {
        ThrowIfNotInitialized();
        return _stateManager.CurrentScissorRect;
    }

    public void PushScissorRect(Rectangle? rect)
    {
        ThrowIfNotInitialized();
        FlushBatch();
        var (w, h) = GetActiveViewportDimensions();
        _stateManager.PushScissorRect(rect, w, h);
    }

    public void PopScissorRect()
    {
        ThrowIfNotInitialized();
        FlushBatch();
        var (w, h) = GetActiveViewportDimensions();
        _stateManager.PopScissorRect(w, h);
    }

    private void FlushBatch()
    {
        if (_batchRenderer.VertexCount == 0) return;
        if (!_frameManager.HasActiveFrame) return;

        if (_batchRenderer.FrameVertexOffset + _batchRenderer.VertexCount > _batchRenderer.MaxVertices)
        {
            _logger.LogDebug("VBO transfer buffer full, cycling mid-frame");
            ExecuteDrawCalls();
            _batchRenderer.ResetFrameVertexOffset();
        }

        var (renderTarget, targetWidth, targetHeight, isUserRenderTarget, userRenderTarget) = ResolveRenderTargetInfo();
        if (renderTarget == nint.Zero)
        {
            _logger.LogWarning("FlushBatch discarding {Count} vertices: render target handle is zero",
                _batchRenderer.VertexCount);
            _batchRenderer.Clear();
            return;
        }

        var (firstVertex, vertexCount) = _batchRenderer.StageForUpload();
        if (vertexCount == 0)
        {
            _batchRenderer.Clear();
            return;
        }

        SDL3.SDL.Rect scissor;
        if (_stateManager.CurrentScissorRect.HasValue)
        {
            var rect = _stateManager.CurrentScissorRect.Value;
            int x = (int)MathF.Round(rect.X);
            int y = (int)MathF.Round(rect.Y);
            int right = (int)MathF.Round(rect.X + rect.Width);
            int bottom = (int)MathF.Round(rect.Y + rect.Height);
            scissor = new SDL3.SDL.Rect
            {
                X = x,
                Y = y,
                W = right - x,
                H = bottom - y
            };
        }
        else
        {
            scissor = new SDL3.SDL.Rect { X = 0, Y = 0, W = targetWidth, H = targetHeight };
        }

        bool isPostProcessTarget = _hasDistinctPostProcessFormat &&
            !isUserRenderTarget &&
            _renderTargetManager.UsePostProcessing &&
            !_postProcessingAppliedThisFrame &&
            _renderTargetManager.MainRenderTarget != null;

        nint pipeline = isPostProcessTarget
            ? _postProcessBlendModePipelines[(int)_stateManager.CurrentBlendMode]
            : _blendModePipelines[(int)_stateManager.CurrentBlendMode];

        _pendingDrawCalls.Add(new DrawCallRecord(
            firstVertex,
            vertexCount,
            _batchRenderer.CurrentBoundTexture,
            _batchRenderer.CurrentTextureScaleMode,
            renderTarget,
            targetWidth,
            targetHeight,
            pipeline,
            scissor,
            isUserRenderTarget,
            userRenderTarget,
            _batchRenderer.CurrentBoundTextureRef
        ));

        _batchRenderer.Clear();
    }

    private (nint Handle, int Width, int Height, bool IsUserRenderTarget, RenderTarget? UserRenderTarget) ResolveRenderTargetInfo()
    {
        if (_renderTargetManager.CurrentRenderTarget != null)
        {
            var rt = _renderTargetManager.CurrentRenderTarget;
            if (rt is not RenderTarget concreteRt)
                throw new InvalidOperationException(
                    $"Unsupported render target type: {rt.GetType().Name} does not expose a GPU texture handle");
            return (concreteRt.TextureHandle, rt.Width, rt.Height, true, concreteRt);
        }
        if (_renderTargetManager.UsePostProcessing &&
            !_postProcessingAppliedThisFrame &&
            _renderTargetManager.MainRenderTarget != null)
        {
            var mainRT = _renderTargetManager.MainRenderTarget;
            return (mainRT.TextureHandle, mainRT.Width, mainRT.Height, false, null);
        }
        return (_frameManager.SwapchainTexture,
                (int)_frameManager.SwapchainWidth,
                (int)_frameManager.SwapchainHeight,
                false,
                null);
    }

    private unsafe void ExecuteDrawCalls()
    {
        if (_pendingDrawCalls.Count == 0) return;

        if (!_frameManager.HasActiveFrame)
        {
            _pendingDrawCalls.Clear();
            return;
        }

        var commandBuffer = _frameManager.CommandBuffer;

        var copyPass = SDL3.SDL.BeginGPUCopyPass(commandBuffer);
        if (copyPass == nint.Zero)
        {
            _logger.LogError("Failed to begin copy pass for vertex upload: {Error}", SDL3.SDL.GetError());
            _pendingDrawCalls.Clear();
            return;
        }

        foreach (var dc in _pendingDrawCalls)
            _batchRenderer.UploadWithinCopyPass(copyPass, dc.FirstVertex, dc.VertexCount);
        SDL3.SDL.EndGPUCopyPass(copyPass);

        nint activeRenderPass = nint.Zero;
        nint activeRenderTarget = nint.Zero;
        nint failedRenderTarget = nint.Zero;
        nint activePipeline = nint.Zero;
        nint activeTexture = nint.Zero;
        TextureScaleMode activeScaleMode = default;
        SDL3.SDL.Rect activeScissor = default;
        bool scissorBound = false;

        foreach (var dc in _pendingDrawCalls)
        {
            var renderTargetHandle = dc.ResolvedRenderTargetHandle;

            if (renderTargetHandle == nint.Zero)
            {
                if (!_zeroHandleWarningLoggedThisFrame)
                {
                    _logger.LogWarning("Skipping draw call for a disposed or invalid render target");
                    _zeroHandleWarningLoggedThisFrame = true;
                }
                continue;
            }

            if (renderTargetHandle != activeRenderTarget)
            {
                if (activeRenderPass != nint.Zero)
                {
                    SDL3.SDL.EndGPURenderPass(activeRenderPass);
                    activeRenderPass = nint.Zero;
                }

                bool isFirstOnTarget = dc.IsUserRenderTarget && dc.UserRenderTarget != null
                    ? _clearedUserTargetsThisFrame.Add(dc.UserRenderTarget)
                    : _clearedSystemTargetsThisFrame.Add(renderTargetHandle);

                SDL3.SDL.FColor clearColor;
                if (dc.IsUserRenderTarget)
                {
                    clearColor = new SDL3.SDL.FColor { R = 0, G = 0, B = 0, A = 0 };
                }
                else
                {
                    clearColor = new SDL3.SDL.FColor
                    {
                        R = _clearColor.R / 255f,
                        G = _clearColor.G / 255f,
                        B = _clearColor.B / 255f,
                        A = _clearColor.A / 255f
                    };
                }

                _colorTargetInfoBuf[0] = new SDL3.SDL.GPUColorTargetInfo
                {
                    Texture = renderTargetHandle,
                    ClearColor = clearColor,
                    LoadOp = isFirstOnTarget ? SDL3.SDL.GPULoadOp.Clear : SDL3.SDL.GPULoadOp.Load,
                    StoreOp = SDL3.SDL.GPUStoreOp.Store
                };

                fixed (SDL3.SDL.GPUColorTargetInfo* colorTargetPtr = _colorTargetInfoBuf)
                {
                    activeRenderPass = SDL3.SDL.BeginGPURenderPass(
                        commandBuffer,
                        (nint)colorTargetPtr,
                        1,
                        nint.Zero);
                }

                activeRenderTarget = renderTargetHandle;

                if (activeRenderPass == nint.Zero)
                {
                    _logger.LogError("Failed to begin render pass: {Error}", SDL3.SDL.GetError());
                    failedRenderTarget = renderTargetHandle;
                    continue;
                }

                failedRenderTarget = nint.Zero;
                activePipeline = nint.Zero;
                activeTexture = nint.Zero;
                scissorBound = false;

                _vertexBufferBindingBuf[0] = new SDL3.SDL.GPUBufferBinding { Buffer = _vertexBuffer, Offset = 0 };
                SDL3.SDL.BindGPUVertexBuffers(activeRenderPass, 0, _vertexBufferBindingBuf, 1);

                var indexBinding = new SDL3.SDL.GPUBufferBinding { Buffer = _batchRenderer.IndexBuffer, Offset = 0 };
                SDL3.SDL.BindGPUIndexBuffer(activeRenderPass, ref indexBinding, SDL3.SDL.GPUIndexElementSize.IndexElementSize32Bit);

                var viewport = new SDL3.SDL.GPUViewport
                {
                    X = 0,
                    Y = 0,
                    W = dc.TargetWidth,
                    H = dc.TargetHeight,
                    MinDepth = 0.0f,
                    MaxDepth = 1.0f
                };
                SDL3.SDL.SetGPUViewport(activeRenderPass, ref viewport);
                PushUniformData(commandBuffer, dc.TargetWidth, dc.TargetHeight);
            }

            if (failedRenderTarget == renderTargetHandle)
                continue;

            if (dc.Pipeline != activePipeline)
            {
                SDL3.SDL.BindGPUGraphicsPipeline(activeRenderPass, dc.Pipeline);
                activePipeline = dc.Pipeline;
            }

            var resolvedTexture = dc.ResolvedTextureHandle;
            if (resolvedTexture != activeTexture || dc.ScaleMode != activeScaleMode)
            {
                if (resolvedTexture == nint.Zero && !_zeroHandleWarningLoggedThisFrame)
                {
                    _logger.LogWarning("Draw call submitted with a zero texture handle; falling back to white texture");
                    _zeroHandleWarningLoggedThisFrame = true;
                }

                _textureSamplerBindingBuf[0] = new SDL3.SDL.GPUTextureSamplerBinding
                {
                    Texture = resolvedTexture != nint.Zero ? resolvedTexture : _whiteTexture,
                    Sampler = dc.ScaleMode == TextureScaleMode.Nearest ? _samplerNearest : _sampler
                };
                SDL3.SDL.BindGPUFragmentSamplers(activeRenderPass, 0, _textureSamplerBindingBuf, 1);
                activeTexture = resolvedTexture;
                activeScaleMode = dc.ScaleMode;
            }

            if (!scissorBound ||
                dc.Scissor.X != activeScissor.X || dc.Scissor.Y != activeScissor.Y ||
                dc.Scissor.W != activeScissor.W || dc.Scissor.H != activeScissor.H)
            {
                activeScissor = dc.Scissor;
                scissorBound = true;
                SDL3.SDL.SetGPUScissor(activeRenderPass, ref activeScissor);
            }

            int firstIndex = SDL3BatchRenderer.VertexOffsetToFirstIndex(dc.FirstVertex);
            int indexCount = SDL3BatchRenderer.IndicesToDraw(dc.VertexCount);
            SDL3.SDL.DrawGPUIndexedPrimitives(activeRenderPass, (uint)indexCount, 1, (uint)firstIndex, 0, 0);
        }

        if (activeRenderPass != nint.Zero)
            SDL3.SDL.EndGPURenderPass(activeRenderPass);

        _pendingDrawCalls.Clear();
    }

    /// <summary>
    /// Pushes the view-projection uniform to the GPU for the current render pass.
    /// Computes the orthographic projection from the target dimensions so render targets
    /// with different sizes get a correctly mapped coordinate space.
    /// </summary>
    /// <remarks>
    /// Called once per render pass, not per draw call. Camera or projection changes
    /// made after the pass begins will not take effect until the next render pass
    /// (triggered by a render-target switch or a new frame).
    /// </remarks>
    private void PushUniformData(nint commandBuffer, int targetWidth, int targetHeight)
    {
        var viewMatrix = _stateManager.Camera?.GetViewMatrix() ?? Matrix4x4.Identity;
        var projection = Matrix4x4.CreateOrthographicOffCenter(0, targetWidth, targetHeight, 0, -1, 1);
        var viewProjection = viewMatrix * projection;

        unsafe
        {
            SDL3.SDL.PushGPUVertexUniformData(
                commandBuffer,
                0,
                (nint)(&viewProjection),
                (uint)sizeof(Matrix4x4)
            );
        }
    }

    /// <remarks>
    /// Must be dispatched on the game loop thread. If the event bus ever changes to
    /// cross-thread dispatch, <c>_viewportWidth</c>/<c>_viewportHeight</c> will need
    /// atomic or lock-based synchronization to prevent torn reads during rendering.
    /// </remarks>
    private void OnWindowResized(WindowResizedEvent evt)
    {
        if (evt.Width <= 0 || evt.Height <= 0)
        {
            _logger.LogDebug("Ignoring degenerate resize event: {Width}x{Height}", evt.Width, evt.Height);
            return;
        }

        _pendingViewportWidth = evt.Width;
        _pendingViewportHeight = evt.Height;
        _viewportResizePending = true;

        if (_renderTargetManager.UsePostProcessing)
            _postProcessingResizePending = true;

        _logger.LogInformation("Viewport resize pending: {Width}x{Height}",
            evt.Width, evt.Height);
    }

    private void OnWindowHidden(WindowHiddenEvent evt)
    {
        _isRenderSuspended = true;
        _logger.LogDebug("Rendering suspended (window hidden)");
    }

    private void OnWindowShown(WindowShownEvent evt)
    {
        _isRenderSuspended = false;
        _logger.LogDebug("Rendering resumed (window shown)");
    }

    private void ThrowIfNotInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("GPU renderer is not initialized");
    }

    private (int Width, int Height) GetActiveViewportDimensions()
    {
        if (_renderTargetManager.CurrentRenderTarget is { } rt)
            return (rt.Width, rt.Height);
        return (_viewportWidth, _viewportHeight);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _logger.LogInformation("Disposing SDL3 GPU renderer");

        _eventBus?.Unsubscribe<WindowResizedEvent>(OnWindowResized);
        _eventBus?.Unsubscribe<WindowHiddenEvent>(OnWindowHidden);
        _eventBus?.Unsubscribe<WindowShownEvent>(OnWindowShown);

        if (_device != nint.Zero)
        {
            SDL3.SDL.WaitForGPUIdle(_device);
            DrainPendingUploads();
            _frameManager.Dispose();
        }

        _renderTargetManager.Dispose();
        (_postProcessPipeline as IDisposable)?.Dispose();
        _batchRenderer.Dispose();
        _textRenderer.Dispose();
        _vertexShader?.Dispose();
        _fragmentShader?.Dispose();
        (_shaderLoader as IDisposable)?.Dispose();

        if (_device != nint.Zero)
        {
            if (_whiteTexture != nint.Zero)
            {
                SDL3.SDL.ReleaseGPUTexture(_device, _whiteTexture);
                _whiteTexture = nint.Zero;
            }

            if (_samplerNearest != nint.Zero)
            {
                SDL3.SDL.ReleaseGPUSampler(_device, _samplerNearest);
                _samplerNearest = nint.Zero;
            }

            if (_sampler != nint.Zero)
            {
                SDL3.SDL.ReleaseGPUSampler(_device, _sampler);
                _sampler = nint.Zero;
            }

            if (_vertexBuffer != nint.Zero)
            {
                SDL3.SDL.ReleaseGPUBuffer(_device, _vertexBuffer);
                _vertexBuffer = nint.Zero;
            }

            for (int i = 0; i < _blendModePipelines.Length; i++)
            {
                if (_blendModePipelines[i] != nint.Zero)
                {
                    SDL3.SDL.ReleaseGPUGraphicsPipeline(_device, _blendModePipelines[i]);
                    _blendModePipelines[i] = nint.Zero;
                }
            }

            for (int i = 0; i < _postProcessBlendModePipelines.Length; i++)
            {
                if (_postProcessBlendModePipelines[i] != nint.Zero)
                {
                    SDL3.SDL.ReleaseGPUGraphicsPipeline(_device, _postProcessBlendModePipelines[i]);
                    _postProcessBlendModePipelines[i] = nint.Zero;
                }
            }

            _gpuDeviceHandle?.Invalidate();

            if (_windowClaimed)
                SDL3.SDL.ReleaseWindowFromGPUDevice(_device, _window);

            SDL3.SDL.DestroyGPUDevice(_device);
            _device = nint.Zero;
        }

        if (_window != nint.Zero)
        {
            SDL3.SDL.DestroyWindow(_window);
            _window = nint.Zero;
        }

        IsInitialized = false;
    }
}