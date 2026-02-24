using Brine2D.Core;
using Brine2D.Rendering.Text;
using Brine2D.Rendering.PostProcessing;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.Common;
using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Runtime.InteropServices;
using Brine2D.Events;
using Brine2D.Rendering;

namespace Brine2D.Rendering;

/// <summary>
/// SDL3 GPU API implementation of the renderer.
/// Modern, shader-based renderer with cross-platform support including texture and text rendering.
/// </summary>
public class SDL3Renderer : IRenderer, ISDL3WindowProvider, ITextureContext
{
    private readonly ILogger<SDL3Renderer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly RenderingOptions _renderingOptions;
    private readonly WindowOptions _windowOptions;
    private readonly IFontLoader? _fontLoader;
    private readonly EventBus? _eventBus;

    private readonly SDL3RenderTargetManager _renderTargetManager;
    private readonly SDL3StateManager _stateManager;
    private SDL3TextRenderer _textRenderer;
    private ViewportState _viewport;
    private readonly SDL3BatchRenderer _batchRenderer;
    private readonly SDL3FrameManager _frameManager;

    private nint _window;
    private nint _device;
    private nint _sampler;
    private nint _samplerNearest;
    private nint _whiteTexture;
    private nint _graphicsPipeline;
    private nint _vertexBuffer;

    private nint _renderPass;
    
    private IShaderLoader? _shaderLoader;
    private IShader? _vertexShader;
    private IShader? _fragmentShader;

    private Matrix4x4 _projectionMatrix;
    private Color _clearColor = new Color(52, 78, 65, 255);

    private readonly Dictionary<BlendMode, nint> _graphicsPipelines = new();
    
    public Color ClearColor
    {
        get => _clearColor;
        set => _clearColor = value;
    }

    public IRenderTarget CreateRenderTarget(int width, int height)
    {
        ThrowIfNotInitialized();
        return _renderTargetManager.CreateRenderTarget(width, height);
    }

    public void SetRenderTarget(IRenderTarget? target)
    {
        ThrowIfNotInitialized();
        
        // Flush any pending draws before changing render target
        FlushBatch();
        
        _renderTargetManager.SetRenderTarget(target);
    }

    public IRenderTarget? GetRenderTarget()
    {
        return _renderTargetManager.CurrentRenderTarget;
    }

    /// <summary>
    /// Push the current render target onto a stack and set a new one.
    /// Useful for nested render-to-texture operations.
    /// </summary>
    public void PushRenderTarget(IRenderTarget? target)
    {
        _renderTargetManager.PushRenderTarget(target);
    }

    /// <summary>
    /// Pop the previous render target from the stack.
    /// </summary>
    public void PopRenderTarget()
    {
        _renderTargetManager.PopRenderTarget();
    }

    public void SetRenderLayer(byte layer)
    {
        _stateManager.SetRenderLayer(layer);
    }

    public byte GetRenderLayer()
    {
        return _stateManager.CurrentRenderLayer;
    }

    // ============================================================
    // TEXTURE DRAWING API (Clean, Non-Ambiguous)
    // ============================================================

    /// <summary>
    /// Draw a texture with full control over transform, origin, scale, and flip.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="position">Position in world/screen space.</param>
    /// <param name="sourceRect">Source rectangle (null = entire texture).</param>
    /// <param name="origin">Rotation/scale origin (0-1 normalized, 0.5,0.5 = center, 0,0 = top-left). Defaults to center.</param>
    /// <param name="rotation">Rotation angle in radians.</param>
    /// <param name="scale">Scale multiplier (null = no scaling).</param>
    /// <param name="color">Tint color (null = white).</param>
    /// <param name="flip">Sprite flip flags.</param>
    public void DrawTexture(
        ITexture texture,
        Vector2 position,
        Rectangle? sourceRect = null,
        Vector2? origin = null,
        float rotation = 0f,
        Vector2? scale = null,
        Color? color = null,
        SpriteFlip flip = SpriteFlip.None)
    {
        ThrowIfNotInitialized();

        if (texture == null || !texture.IsLoaded)
        {
            _logger.LogWarning("Attempted to draw invalid texture");
            return;
        }

        var textureHandle = GetTextureHandle(texture);

        // Defaults
        var actualOrigin = origin ?? new Vector2(0.5f, 0.5f);
        var actualScale = scale ?? Vector2.One;
        var actualColor = color ?? Color.White;
        var srcRect = sourceRect ?? new Rectangle(0, 0, texture.Width, texture.Height);

        // Calculate destination size
        var destWidth = srcRect.Width * actualScale.X;
        var destHeight = srcRect.Height * actualScale.Y;

        // UV coordinates
        float u1 = srcRect.X / (float)texture.Width;
        float v1 = srcRect.Y / (float)texture.Height;
        float u2 = (srcRect.X + srcRect.Width) / (float)texture.Width;
        float v2 = (srcRect.Y + srcRect.Height) / (float)texture.Height;

        // Apply flip
        if ((flip & SpriteFlip.Horizontal) != 0) (u1, u2) = (u2, u1);
        if ((flip & SpriteFlip.Vertical) != 0) (v1, v2) = (v2, v1);

        // Calculate pivot
        var pivotX = destWidth * actualOrigin.X;
        var pivotY = destHeight * actualOrigin.Y;
        var adjustedX = position.X - pivotX;
        var adjustedY = position.Y - pivotY;

        // Draw - Changed: Use batch renderer
        _batchRenderer.DrawTexturedQuad(
            textureHandle,
            texture.ScaleMode,
            adjustedX,
            adjustedY,
            destWidth,
            destHeight,
            actualColor,
            u1, v1, u2, v2,
            rotation,
            FlushBatch);
    }

    /// <summary>
    /// Draw texture at position (Vector2, top-left anchor).
    /// </summary>
    public void DrawTexture(ITexture texture, Vector2 position)
    {
        if (texture == null || !texture.IsLoaded)
            return;

        DrawTexture(texture, position,
            origin: Vector2.Zero,
            scale: Vector2.One);
    }

    /// <summary>
    /// Draw texture at position (float x, y, top-left anchor).
    /// </summary>
    public void DrawTexture(ITexture texture, float x, float y)
    {
        DrawTexture(texture, new Vector2(x, y));
    }

    /// <summary>
    /// Draw texture at position with explicit width/height (top-left anchor).
    /// </summary>
    public void DrawTexture(ITexture texture, float x, float y, float width, float height)
    {
        if (texture == null || !texture.IsLoaded)
            return;

        DrawTexture(texture, new Vector2(x, y),
            scale: new Vector2(width / texture.Width, height / texture.Height),
            origin: Vector2.Zero);
    }

    private bool _disposed;

    public nint Window => _window;
    public nint Device => _device;
    public bool IsInitialized { get; private set; }

    public ICamera? Camera
    {
        get => _stateManager.Camera;
        set => _stateManager.Camera = value;
    }

    public int Width => _viewport.Width;
    public int Height => _viewport.Height;

    public SDL3Renderer(
        ILogger<SDL3Renderer> logger,
        ILoggerFactory loggerFactory,
        RenderingOptions renderingOptions,
        WindowOptions windowOptions,
        PostProcessingOptions? postProcessingOptions = null,
        SDL3PostProcessPipeline? postProcessPipeline = null,
        IFontLoader? fontLoader = null,
        EventBus? eventBus = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _renderingOptions = renderingOptions ?? throw new ArgumentNullException(nameof(renderingOptions));
        _windowOptions = windowOptions ?? throw new ArgumentNullException(nameof(windowOptions));

        _fontLoader = fontLoader;
        _eventBus = eventBus;
        
        _viewport = new ViewportState(_windowOptions.Width, _windowOptions.Height);
        
        _stateManager = new SDL3StateManager(_loggerFactory.CreateLogger<SDL3StateManager>());

        _textRenderer = new SDL3TextRenderer(
            _loggerFactory.CreateLogger<SDL3TextRenderer>(),
            _loggerFactory,
            fontLoader);
    
        _batchRenderer = new SDL3BatchRenderer(
            _loggerFactory.CreateLogger<SDL3BatchRenderer>(),
            _stateManager);

        _renderTargetManager = new SDL3RenderTargetManager(
            _loggerFactory.CreateLogger<SDL3RenderTargetManager>(),
            _loggerFactory,
            postProcessingOptions,
            postProcessPipeline);
    
        _frameManager = new SDL3FrameManager(_loggerFactory.CreateLogger<SDL3FrameManager>());
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            _logger.LogWarning("GPU renderer already initialized");
            return;
        }

        _logger.LogInformation("Initializing SDL3 GPU renderer");

        _logger.LogInformation("SDL3 GPU Renderer initializing on Thread {ThreadId}", 
            Thread.CurrentThread.ManagedThreadId);
        
        if (!SDL3.SDL.Init(SDL3.SDL.InitFlags.Video | SDL3.SDL.InitFlags.Events))
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to initialize SDL3: {Error}", error);
            throw new InvalidOperationException($"Failed to initialize SDL3: {error}");
        }
        
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

        _device = SDL3.SDL.CreateGPUDevice(shaderFormats, true, _renderingOptions.PreferredGPUDriver.ToSDL3DriverName());

        if (_device == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create GPU device: {Error}", error);
            throw new InvalidOperationException($"Failed to create GPU device: {error}");
        }

        if (!SDL3.SDL.ClaimWindowForGPUDevice(_device, _window))
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to claim window for GPU: {Error}", error);
            throw new InvalidOperationException($"Failed to claim window: {error}");
        }

        var driverName = SDL3.SDL.GetGPUDeviceDriver(_device);
        _logger.LogInformation("GPU renderer initialized with driver: {Driver}", driverName);

        _shaderLoader = new SDL3ShaderLoader(
            _loggerFactory.CreateLogger<SDL3ShaderLoader>(),
            _loggerFactory,
            _device);

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

        CreateSamplers();
        CreateWhiteTexture();
        CreateGraphicsPipeline();
        CreateVertexBuffer();
        _batchRenderer.Initialize(_device, _vertexBuffer, _whiteTexture, _sampler, _samplerNearest);
        _renderTargetManager.Initialize(_device);  // NEW
        _frameManager.Initialize(_device, _window);
        UpdateProjectionMatrix(_viewport.Width, _viewport.Height);

        await _textRenderer.LoadDefaultFontAsync(cancellationToken);

        if (_renderTargetManager.UsePostProcessing)  // Changed
        {
            _renderTargetManager.CreatePostProcessingTargets(_viewport.Width, _viewport.Height);  // Changed
            _logger.LogInformation("Post-processing enabled");
        }

        _eventBus?.Subscribe<WindowResizedEvent>(OnWindowResized);

        IsInitialized = true;
        _logger.LogInformation("SDL3 GPU renderer fully initialized");
    }

    private void OnWindowResized(WindowResizedEvent evt)
    {
        _viewport.Update(evt.Width, evt.Height);
        UpdateProjectionMatrix(_viewport.Width, evt.Height);

        if (_renderTargetManager.UsePostProcessing)  // Changed
        {
            _renderTargetManager.CreatePostProcessingTargets(evt.Width, evt.Height);  // Changed
        }

        _logger.LogInformation("Viewport resized to {Width}x{Height}, projection matrix updated",
            evt.Width, evt.Height);
    }

    private void CreateSamplers()
    {
        _sampler = CreateGPUSampler(SDL3.SDL.GPUFilter.Linear);
        _samplerNearest = CreateGPUSampler(SDL3.SDL.GPUFilter.Nearest);
        _logger.LogInformation("Texture samplers created successfully (Linear + Nearest)");
    }

    private nint CreateGPUSampler(SDL3.SDL.GPUFilter filter)
    {
        var samplerCreateInfo = new SDL3.SDL.GPUSamplerCreateInfo
        {
            MinFilter = filter,
            MagFilter = filter,
            MipmapMode = filter == SDL3.SDL.GPUFilter.Linear
                ? SDL3.SDL.GPUSamplerMipmapMode.Linear
                : SDL3.SDL.GPUSamplerMipmapMode.Nearest,
            AddressModeU = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeW = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
            MipLodBias = 0.0f,
            MaxAnisotropy = 1.0f,
            CompareOp = SDL3.SDL.GPUCompareOp.Never,
            MinLod = 0.0f,
            MaxLod = 1.0f
        };

        var sampler = SDL3.SDL.CreateGPUSampler(_device, ref samplerCreateInfo);
        if (sampler == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create {Filter} sampler: {Error}", filter, error);
            throw new InvalidOperationException($"Failed to create {filter} sampler: {error}");
        }

        return sampler;
    }

    private void CreateWhiteTexture()
    {
        var textureCreateInfo = new SDL3.SDL.GPUTextureCreateInfo
        {
            Type = SDL3.SDL.GPUTextureType.TextureType2D,
            Format = SDL3.SDL.GPUTextureFormat.R8G8B8A8Unorm,
            Usage = SDL3.SDL.GPUTextureUsageFlags.Sampler,
            Width = 1,
            Height = 1,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL3.SDL.GPUSampleCount.SampleCount1
        };

        _whiteTexture = SDL3.SDL.CreateGPUTexture(_device, ref textureCreateInfo);
        if (_whiteTexture == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create white texture: {Error}", error);
            throw new InvalidOperationException($"Failed to create white texture: {error}");
        }

        var pixelData = new byte[] { 255, 255, 255, 255 };
        UploadTextureDataImmediate(_whiteTexture, pixelData, 1, 1);

        _logger.LogInformation("White texture created successfully");
    }

    private void UploadTextureDataImmediate(nint texture, byte[] pixelData, int width, int height)
    {
        var transferCreateInfo = new SDL3.SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUTransferBufferUsage.Upload,
            Size = (uint)pixelData.Length
        };

        var transferBuffer = SDL3.SDL.CreateGPUTransferBuffer(_device, ref transferCreateInfo);
        if (transferBuffer == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create transfer buffer for texture upload");
        }

        try
        {
            var mappedData = SDL3.SDL.MapGPUTransferBuffer(_device, transferBuffer, false);
            if (mappedData != nint.Zero)
            {
                Marshal.Copy(pixelData, 0, mappedData, pixelData.Length);
                SDL3.SDL.UnmapGPUTransferBuffer(_device, transferBuffer);
            }

            var uploadCmdBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
            if (uploadCmdBuffer == nint.Zero)
            {
                throw new InvalidOperationException("Failed to acquire command buffer for texture upload");
            }

            var copyPass = SDL3.SDL.BeginGPUCopyPass(uploadCmdBuffer);
            if (copyPass != nint.Zero)
            {
                var source = new SDL3.SDL.GPUTextureTransferInfo
                {
                    TransferBuffer = transferBuffer,
                    Offset = 0
                };

                var destination = new SDL3.SDL.GPUTextureRegion
                {
                    Texture = texture,
                    MipLevel = 0,
                    Layer = 0,
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = (uint)width,
                    H = (uint)height,
                    D = 1
                };

                SDL3.SDL.UploadToGPUTexture(copyPass, ref source, ref destination, false);
                SDL3.SDL.EndGPUCopyPass(copyPass);
            }
            SDL3.SDL.SubmitGPUCommandBuffer(uploadCmdBuffer);
            SDL3.SDL.WaitForGPUIdle(_device);
        }
        finally
        {
            SDL3.SDL.ReleaseGPUTransferBuffer(_device, transferBuffer);
        }
    }

    private void CreateGraphicsPipeline()
    {
        _logger.LogDebug("Creating default graphics pipeline");

        // Create default Alpha blend mode pipeline
        var defaultPipeline = CreateGraphicsPipelineForBlendMode(BlendMode.Alpha);
        _graphicsPipelines[BlendMode.Alpha] = defaultPipeline;
        _graphicsPipeline = defaultPipeline;
    }

    private nint CreateGraphicsPipelineForBlendMode(BlendMode blendMode)
    {
        _logger.LogDebug("Creating graphics pipeline for blend mode: {BlendMode}", blendMode);

        var vertexShader = (_vertexShader as SDL3Shader)?.Handle ?? nint.Zero;
        var fragmentShader = (_fragmentShader as SDL3Shader)?.Handle ?? nint.Zero;

        if (vertexShader == nint.Zero || fragmentShader == nint.Zero)
        {
            throw new InvalidOperationException("Shaders must be compiled before creating pipeline");
        }

        var vertexAttributes = new SDL3.SDL.GPUVertexAttribute[]
        {
            new() { Location = 0, BufferSlot = 0, Format = SDL3.SDL.GPUVertexElementFormat.Float2, Offset = 0 },
            new() { Location = 1, BufferSlot = 0, Format = SDL3.SDL.GPUVertexElementFormat.Float4, Offset = 8 },
            new() { Location = 2, BufferSlot = 0, Format = SDL3.SDL.GPUVertexElementFormat.Float2, Offset = 24 }
        };

        var vertexBufferDescriptions = new SDL3.SDL.GPUVertexBufferDescription[]
        {
            new()
            {
                Slot = 0,
                Pitch = (uint)SDL3BatchRenderer.VertexSize,
                InputRate = SDL3.SDL.GPUVertexInputRate.Vertex,
                InstanceStepRate = 0
            }
        };

        // Create blend state based on mode
        var blendState = blendMode switch
        {
            BlendMode.Alpha => new SDL3.SDL.GPUColorTargetBlendState
            {
                EnableBlend = true,
                SrcColorBlendFactor = SDL3.SDL.GPUBlendFactor.SrcAlpha,
                DstColorBlendFactor = SDL3.SDL.GPUBlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = SDL3.SDL.GPUBlendOp.Add,
                SrcAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.One,
                DstAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.OneMinusSrcAlpha,
                AlphaBlendOp = SDL3.SDL.GPUBlendOp.Add,
                ColorWriteMask = SDL3.SDL.GPUColorComponentFlags.R |
                               SDL3.SDL.GPUColorComponentFlags.G |
                               SDL3.SDL.GPUColorComponentFlags.B |
                               SDL3.SDL.GPUColorComponentFlags.A
            },

            BlendMode.Additive => new SDL3.SDL.GPUColorTargetBlendState
            {
                EnableBlend = true,
                SrcColorBlendFactor = SDL3.SDL.GPUBlendFactor.SrcAlpha,
                DstColorBlendFactor = SDL3.SDL.GPUBlendFactor.One,
                ColorBlendOp = SDL3.SDL.GPUBlendOp.Add,
                SrcAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.One,
                DstAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.One,
                AlphaBlendOp = SDL3.SDL.GPUBlendOp.Add,
                ColorWriteMask = SDL3.SDL.GPUColorComponentFlags.R |
                               SDL3.SDL.GPUColorComponentFlags.G |
                               SDL3.SDL.GPUColorComponentFlags.B |
                               SDL3.SDL.GPUColorComponentFlags.A
            },

            BlendMode.Multiply => new SDL3.SDL.GPUColorTargetBlendState
            {
                EnableBlend = true,
                SrcColorBlendFactor = SDL3.SDL.GPUBlendFactor.DstColor,
                DstColorBlendFactor = SDL3.SDL.GPUBlendFactor.Zero,
                ColorBlendOp = SDL3.SDL.GPUBlendOp.Add,
                SrcAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.One,
                DstAlphaBlendFactor = SDL3.SDL.GPUBlendFactor.Zero,
                AlphaBlendOp = SDL3.SDL.GPUBlendOp.Add,
                ColorWriteMask = SDL3.SDL.GPUColorComponentFlags.R |
                               SDL3.SDL.GPUColorComponentFlags.G |
                               SDL3.SDL.GPUColorComponentFlags.B |
                               SDL3.SDL.GPUColorComponentFlags.A
            },

            BlendMode.None => new SDL3.SDL.GPUColorTargetBlendState
            {
                EnableBlend = false,
                ColorWriteMask = SDL3.SDL.GPUColorComponentFlags.R |
                               SDL3.SDL.GPUColorComponentFlags.G |
                               SDL3.SDL.GPUColorComponentFlags.B |
                               SDL3.SDL.GPUColorComponentFlags.A
            },

            _ => throw new ArgumentException($"Unsupported blend mode: {blendMode}")
        };

        var colorTargetDescriptions = new SDL3.SDL.GPUColorTargetDescription[]
        {
            new()
            {
                Format = SDL3.SDL.GPUTextureFormat.B8G8R8A8Unorm,
                BlendState = blendState
            }
        };

        var vertexAttribHandle = GCHandle.Alloc(vertexAttributes, GCHandleType.Pinned);
        var vertexBufferHandle = GCHandle.Alloc(vertexBufferDescriptions, GCHandleType.Pinned);
        var colorTargetHandle = GCHandle.Alloc(colorTargetDescriptions, GCHandleType.Pinned);

        try
        {
            var vertexInputState = new SDL3.SDL.GPUVertexInputState
            {
                VertexBufferDescriptions = vertexBufferHandle.AddrOfPinnedObject(),
                NumVertexBuffers = 1,
                VertexAttributes = vertexAttribHandle.AddrOfPinnedObject(),
                NumVertexAttributes = 3,
            };

            var pipelineCreateInfo = new SDL3.SDL.GPUGraphicsPipelineCreateInfo
            {
                VertexShader = vertexShader,
                FragmentShader = fragmentShader,
                VertexInputState = vertexInputState,
                PrimitiveType = SDL3.SDL.GPUPrimitiveType.TriangleList,
                RasterizerState = new SDL3.SDL.GPURasterizerState
                {
                    FillMode = SDL3.SDL.GPUFillMode.Fill,
                    CullMode = SDL3.SDL.GPUCullMode.None,
                    FrontFace = SDL3.SDL.GPUFrontFace.CounterClockwise
                },
                MultisampleState = new SDL3.SDL.GPUMultisampleState
                {
                    SampleCount = SDL3.SDL.GPUSampleCount.SampleCount1,
                    SampleMask = 0
                },
                DepthStencilState = new SDL3.SDL.GPUDepthStencilState
                {
                    CompareOp = SDL3.SDL.GPUCompareOp.Always,
                    EnableStencilTest = false
                },
                TargetInfo = new SDL3.SDL.GPUGraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions = colorTargetHandle.AddrOfPinnedObject(),
                    NumColorTargets = 1,
                    DepthStencilFormat = SDL3.SDL.GPUTextureFormat.Invalid,
                    HasDepthStencilTarget = false
                }
            };

            var pipeline = SDL3.SDL.CreateGPUGraphicsPipeline(_device, ref pipelineCreateInfo);

            if (pipeline == nint.Zero)
            {
                var error = SDL3.SDL.GetError();
                _logger.LogError("Failed to create graphics pipeline for {BlendMode}: {Error}", blendMode, error);
                throw new InvalidOperationException($"Failed to create graphics pipeline: {error}");
            }

            _logger.LogInformation("Graphics pipeline created successfully for blend mode: {BlendMode}", blendMode);
            return pipeline;
        }
        finally
        {
            vertexAttribHandle.Free();
            vertexBufferHandle.Free();
            colorTargetHandle.Free();
        }
    }

    private void CreateVertexBuffer()
    {
        var bufferCreateInfo = new SDL3.SDL.GPUBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUBufferUsageFlags.Vertex,
            Size = (uint)(SDL3BatchRenderer.VertexSize * SDL3BatchRenderer.MaxVertices)  // Changed
        };

        _vertexBuffer = SDL3.SDL.CreateGPUBuffer(_device, ref bufferCreateInfo);

        if (_vertexBuffer == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create vertex buffer: {Error}", error);
            throw new InvalidOperationException($"Failed to create vertex buffer: {error}");
        }

        _logger.LogDebug("Vertex buffer created ({Size} vertices)", SDL3BatchRenderer.MaxVertices);  // Changed
    }

    private void UpdateProjectionMatrix(int width, int height)
    {
        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            0, width, height, 0, -1, 1);
    }

    public void BeginFrame()
    {
        ThrowIfNotInitialized();

        _batchRenderer.Clear();

        if (!_frameManager.BeginFrame())  // Changed
        {
            return;  // Frame acquisition failed (minimized window, etc.)
        }
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
    /// </para>
    /// </remarks>
    public void ApplyPostProcessing()
    {
        ThrowIfNotInitialized();

        if (!_frameManager.HasActiveFrame)  // Changed
        {
            return;
        }

        FlushBatch();

        // Use render target manager and frame manager
        bool effectsApplied = _renderTargetManager.ApplyPostProcessing(
            this,
            _frameManager.SwapchainTexture,  // Changed
            _frameManager.CommandBuffer);     // Changed

        // If no effects were applied, blit manually
        if (!effectsApplied && _renderTargetManager.MainRenderTarget != null)
        {
            _frameManager.BlitToSwapchain(  // Changed
                _renderTargetManager.MainRenderTarget.TextureHandle,
                _viewport.Width,
                _viewport.Height);
        }
    }

    public void EndFrame()
    {
        ThrowIfNotInitialized();
        _frameManager.EndFrame();  // Changed - that's it!
    }

    private void FlushBatch()
    {
        if (_batchRenderer.VertexCount == 0) return;

        if (!_frameManager.HasActiveFrame)  // Changed
        {
            return;
        }

        _batchRenderer.UploadToGPU(_frameManager.CommandBuffer);  // Changed

        // Determine render target based on current state
        nint renderTarget;
        if (_renderTargetManager.CurrentRenderTarget != null)
        {
            renderTarget = (_renderTargetManager.CurrentRenderTarget as RenderTarget)?.TextureHandle ?? nint.Zero;
            if (renderTarget == nint.Zero)
            {
                _logger.LogError("Invalid render target");
                return;
            }
        }
        else if (_renderTargetManager.UsePostProcessing && _renderTargetManager.MainRenderTarget != null)
        {
            renderTarget = _renderTargetManager.MainRenderTarget.TextureHandle;
        }
        else
        {
            renderTarget = _frameManager.SwapchainTexture;  // Changed
        }

        var colorTargetInfo = new SDL3.SDL.GPUColorTargetInfo
        {
            Texture = renderTarget,
            ClearColor = new SDL3.SDL.FColor
            {
                R = _clearColor.R / 255f,
                G = _clearColor.G / 255f,
                B = _clearColor.B / 255f,
                A = _clearColor.A / 255f
            },
            LoadOp = _frameManager.IsFirstFlush ? SDL3.SDL.GPULoadOp.Clear : SDL3.SDL.GPULoadOp.Load,  // Changed
            StoreOp = SDL3.SDL.GPUStoreOp.Store
        };

        _frameManager.MarkFirstFlushComplete();  // NEW - add after creating colorTargetInfo

        var colorTargets = new[] { colorTargetInfo };
        var colorTargetHandle = GCHandle.Alloc(colorTargets, GCHandleType.Pinned);

        try
        {
            _renderPass = SDL3.SDL.BeginGPURenderPass(
                _frameManager.CommandBuffer,  // Changed
                colorTargetHandle.AddrOfPinnedObject(),
                1,
                IntPtr.Zero);

            if (_renderPass == nint.Zero)
            {
                var error = SDL3.SDL.GetError();
                _logger.LogError("Failed to begin render pass: {Error}", error);
                throw new InvalidOperationException($"Failed to begin render pass: {error}");
            }

            SDL3.SDL.BindGPUGraphicsPipeline(_renderPass, _graphicsPipeline);
            PushUniformData();

            var textureToUse = _batchRenderer.CurrentBoundTexture != nint.Zero
                ? _batchRenderer.CurrentBoundTexture
                : _whiteTexture;
            var samplerToUse = _batchRenderer.CurrentTextureScaleMode == TextureScaleMode.Nearest
                ? _samplerNearest
                : _sampler;

            var textureBinding = new SDL3.SDL.GPUTextureSamplerBinding
            {
                Texture = textureToUse,
                Sampler = samplerToUse
            };
            var textureBindings = new[] { textureBinding };
            var textureBindingHandle = GCHandle.Alloc(textureBindings, GCHandleType.Pinned);

            try
            {
                SDL3.SDL.BindGPUFragmentSamplers(_renderPass, 0, textureBindings, 1);

                var viewport = new SDL3.SDL.GPUViewport
                {
                    X = 0,
                    Y = 0,
                    W = _viewport.Width,
                    H = _viewport.Height,
                    MinDepth = 0.0f,
                    MaxDepth = 1.0f
                };
                SDL3.SDL.SetGPUViewport(_renderPass, ref viewport);

                SDL3.SDL.Rect scissor;

                if (_stateManager.CurrentScissorRect.HasValue)
                {
                    var rect = _stateManager.CurrentScissorRect.Value;
                    scissor = new SDL3.SDL.Rect
                    {
                        X = (int)rect.X,
                        Y = (int)rect.Y,
                        W = (int)rect.Width,
                        H = (int)rect.Height
                    };
                }
                else
                {
                    // No clipping - use full viewport
                    scissor = new SDL3.SDL.Rect
                    {
                        X = 0,
                        Y = 0,
                        W = _viewport.Width,
                        H = _viewport.Height
                    };
                }
                SDL3.SDL.SetGPUScissor(_renderPass, ref scissor);

                var bufferBinding = new SDL3.SDL.GPUBufferBinding
                {
                    Buffer = _vertexBuffer,
                    Offset = 0
                };
                var bufferBindings = new[] { bufferBinding };
                var bufferBindingHandle = GCHandle.Alloc(bufferBindings, GCHandleType.Pinned);

                try
                {
                    SDL3.SDL.BindGPUVertexBuffers(_renderPass, 0, bufferBindings, 1);
                    SDL3.SDL.DrawGPUPrimitives(_renderPass, (uint)_batchRenderer.VertexCount, 1, 0, 0);
                }
                finally
                {
                    bufferBindingHandle.Free();
                }
            }
            finally
            {
                textureBindingHandle.Free();
            }

            SDL3.SDL.EndGPURenderPass(_renderPass);
            _renderPass = nint.Zero;
        }
        finally
        {
            colorTargetHandle.Free();
        }

        _batchRenderer.Clear();
    }

    private void PushUniformData()
    {
        unsafe
        {
            fixed (Matrix4x4* matrixPtr = &_projectionMatrix)
            {
                SDL3.SDL.PushGPUVertexUniformData(
                    _frameManager.CommandBuffer,  // Changed
                    0,
                    (nint)matrixPtr,
                    (uint)sizeof(Matrix4x4)
                );
            }
        }
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        if (_stateManager.CurrentBlendMode == blendMode)
            return;

        FlushBatch();

        if (!_graphicsPipelines.TryGetValue(blendMode, out var pipeline))
        {
            pipeline = CreateGraphicsPipelineForBlendMode(blendMode);
            _graphicsPipelines[blendMode] = pipeline;
            _logger.LogDebug("Lazily created graphics pipeline for blend mode: {BlendMode}", blendMode);
        }

        _graphicsPipeline = pipeline;
        _stateManager.SetBlendMode(blendMode);
    }

    public void SetScissorRect(Rectangle? rect)
    {
        ThrowIfNotInitialized();

        FlushBatch();

        _stateManager.SetScissorRect(rect, _viewport.Width, _viewport.Height);
    }

    public Rectangle? GetScissorRect()
    {
        return _stateManager.CurrentScissorRect;
    }

    public void PushScissorRect(Rectangle? rect)
    {
        _stateManager.PushScissorRect(rect, _viewport.Width, _viewport.Height);
    }

    public void PopScissorRect()
    {
        _stateManager.PopScissorRect();
    }
    
    public void DrawRectangleFilled(float x, float y, float width, float height, Color color)
    {
        ThrowIfNotInitialized();
        _batchRenderer.DrawRectangleFilled(x, y, width, height, color, FlushBatch);
    }

    public void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1)
    {
        ThrowIfNotInitialized();

        DrawLine(x, y, x + width, y, color, thickness);
        DrawLine(x + width, y, x + width, y + height, color, thickness);
        DrawLine(x + width, y + height, x, y + height, color, thickness);
        DrawLine(x, y + height, x, y, color, thickness);
    }

    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1)
    {
        ThrowIfNotInitialized();
        _batchRenderer.DrawLine(x1, y1, x2, y2, color, thickness, FlushBatch);
    }

    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color)
    {
        ThrowIfNotInitialized();
        _batchRenderer.DrawCircleFilled(centerX, centerY, radius, color, FlushBatch);
    }

    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1)
    {
        ThrowIfNotInitialized();
        _batchRenderer.DrawCircleOutline(centerX, centerY, radius, color, thickness, FlushBatch);
    }

    public void DrawText(string text, float x, float y, Color color)
    {
        DrawText(text, x, y, new TextRenderOptions
        {
            Color = color,
            Font = _textRenderer.DefaultFont
        });
    }

    public void DrawText(string text, float x, float y, TextRenderOptions options)
    {
        ThrowIfNotInitialized();

        if (string.IsNullOrEmpty(text))
            return;

        var runs = _textRenderer.ParseText(text, options);

        // Render with layout
        RenderTextRuns(runs, x, y, options);
    }

    public Vector2 MeasureText(string text, float? fontSize = null)
    {
        return _textRenderer.MeasureText(text, fontSize);
    }

    public Vector2 MeasureText(string text, TextRenderOptions options)
    {
        return _textRenderer.MeasureTextWithOptions(text, options);
    }

    // ============================================================
    // TEXT RENDERING INTERNALS
    // ============================================================

    private void RenderTextRuns(
    IReadOnlyList<TextRun> runs,
    float x,
    float y,
    TextRenderOptions options)
    {
        if (runs.Count == 0)
            return;

        // CHANGE THIS:
        // Ensure font atlas is ready
        _textRenderer.EnsureFontAtlasGenerated(this);  // Changed

        if (_textRenderer.DefaultFontAtlas == null || _textRenderer.DefaultFontAtlas.Texture == null)  // Changed
        {
            _logger.LogWarning("No font atlas available");
            return;
        }

        // Calculate total text bounds for alignment
        var textSize = _textRenderer.MeasureTextRuns(runs);  // Changed

        // ... rest of the method stays the same
        float startX = x;
        float startY = y;

        // Apply horizontal alignment
        if (options.MaxWidth.HasValue)
        {
            startX = options.HorizontalAlign switch
            {
                TextAlignment.Center => x + (options.MaxWidth.Value - textSize.X) / 2,
                TextAlignment.Right => x + options.MaxWidth.Value - textSize.X,
                _ => x
            };
        }

        // Apply vertical alignment
        if (options.MaxHeight.HasValue)
        {
            startY = options.VerticalAlign switch
            {
                VerticalAlignment.Middle => y + (options.MaxHeight.Value - textSize.Y) / 2,
                VerticalAlignment.Bottom => y + options.MaxHeight.Value - textSize.Y,
                _ => y
            };
        }

        float cursorX = startX;
        float cursorY = startY;
        float lineHeight = _textRenderer.DefaultFontAtlas.LineHeight * options.LineSpacing;  // Changed

        var atlasTexture = _textRenderer.DefaultFontAtlas.Texture;  // Changed

        foreach (var run in runs)
        {
            if (string.IsNullOrEmpty(run.Text))
                continue;

            // Handle wrapping
            if (options.MaxWidth.HasValue)
            {
                RenderRunWithWrapping(run, ref cursorX, ref cursorY, startX, lineHeight, options, atlasTexture);
            }
            else
            {
                RenderRunDirect(run, ref cursorX, ref cursorY, startX, lineHeight, options, atlasTexture);
            }
        }
    }

    private void RenderRunDirect(
        TextRun run,
        ref float cursorX,
        ref float cursorY,
        float startX,
        float lineHeight,
        TextRenderOptions options,
        ITexture atlasTexture)
    {
        // Render shadow if enabled
        if (options.ShadowOffset.HasValue)
        {
            float shadowStartX = cursorX;
            RenderGlyphs(run.Text, cursorX + options.ShadowOffset.Value.X,
                         cursorY + options.ShadowOffset.Value.Y,
                         options.ShadowColor, atlasTexture, ref cursorX, ref cursorY, startX, lineHeight, false);
            cursorX = shadowStartX; // Reset cursor after shadow
        }

        // Render main text
        float textStartX = cursorX;
        float textY = cursorY;
        RenderGlyphs(run.Text, cursorX, cursorY, run.Color, atlasTexture,
                     ref cursorX, ref cursorY, startX, lineHeight, true);

        // Render underline
        if ((run.Style & TextStyle.Underline) != 0)
        {
            float underlineY = textY + lineHeight - 2;
            DrawLine(textStartX, underlineY, cursorX, underlineY, run.Color, 1f);
        }

        // Render strikethrough
        if ((run.Style & TextStyle.Strikethrough) != 0)
        {
            float strikeY = textY + lineHeight / 2;
            DrawLine(textStartX, strikeY, cursorX, strikeY, run.Color, 1f);
        }
    }

    private void RenderRunWithWrapping(
        TextRun run,
        ref float cursorX,
        ref float cursorY,
        float startX,
        float lineHeight,
        TextRenderOptions options,
        ITexture atlasTexture)
    {
        // Simple word wrapping
        var words = run.Text.Split(' ');

        foreach (var word in words)
        {
            var wordSize = MeasureText(word + " ");

            if (cursorX + wordSize.X > startX + options.MaxWidth!.Value && cursorX > startX)
            {
                // Wrap to next line
                cursorX = startX;
                cursorY += lineHeight;
            }

            RenderRunDirect(new TextRun
            {
                Text = word + " ",
                Color = run.Color,
                Style = run.Style
            }, ref cursorX, ref cursorY, startX, lineHeight, options, atlasTexture);
        }
    }

    private void RenderGlyphs(
        string text,
        float x,
        float y,
        Color color,
        ITexture atlasTexture,
        ref float cursorX,
        ref float cursorY,
        float startX,
        float lineHeight,
        bool advanceCursor)
    {
        float localCursorX = x;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                if (advanceCursor)
                {
                    cursorX = startX;
                    cursorY += lineHeight;
                }
                localCursorX = startX;
                continue;
            }

            if (!_textRenderer.DefaultFontAtlas!.TryGetGlyph(c, out var glyph))
                continue;

            float u1 = glyph.AtlasX / (float)atlasTexture.Width;
            float v1 = glyph.AtlasY / (float)atlasTexture.Height;
            float u2 = (glyph.AtlasX + glyph.Width) / (float)atlasTexture.Width;
            float v2 = (glyph.AtlasY + glyph.Height) / (float)atlasTexture.Height;

            var atlasGpuTexture = (SDL3Texture)atlasTexture;

            // Changed: Use batch renderer
            _batchRenderer.DrawTexturedQuad(
                atlasGpuTexture.Handle,
                atlasTexture.ScaleMode,
                localCursorX,
                y,
                glyph.Width,
                glyph.Height,
                color,
                u1, v1, u2, v2,
                0f,  // No rotation
                FlushBatch);

            localCursorX += glyph.Advance;
        }

        if (advanceCursor)
        {
            cursorX = localCursorX;
        }
    }

    public void SetDefaultFont(Font? font)
    {
        _textRenderer.SetDefaultFont(font);
    }

    public ITexture CreateTextureFromSurface(nint surface, int width, int height, TextureScaleMode scaleMode)
    {
        ThrowIfNotInitialized();

        var textureCreateInfo = new SDL3.SDL.GPUTextureCreateInfo
        {
            Type = SDL3.SDL.GPUTextureType.TextureType2D,
            Format = SDL3.SDL.GPUTextureFormat.R8G8B8A8Unorm,
            Usage = SDL3.SDL.GPUTextureUsageFlags.Sampler,
            Width = (uint)width,
            Height = (uint)height,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL3.SDL.GPUSampleCount.SampleCount1
        };

        var gpuTexture = SDL3.SDL.CreateGPUTexture(_device, ref textureCreateInfo);
        if (gpuTexture == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create GPU texture from surface: {Error}", error);
            throw new InvalidOperationException($"Failed to create GPU texture: {error}");
        }

        UploadTextureData(gpuTexture, surface, width, height);

        _logger.LogInformation("GPU texture created and uploaded: {Width}x{Height}", width, height);

        return new SDL3Texture(
            "surface_texture",
            gpuTexture,
            width,
            height,
            scaleMode,
            _loggerFactory.CreateLogger<SDL3Texture>());
    }

    private void UploadTextureData(nint gpuTexture, nint surface, int width, int height)
    {
        var surfaceStruct = Marshal.PtrToStructure<SDL3.SDL.Surface>(surface);
        var pixelDataSize = (uint)(width * height * 4);

        var transferCreateInfo = new SDL3.SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUTransferBufferUsage.Upload,
            Size = pixelDataSize
        };

        var transferBuffer = SDL3.SDL.CreateGPUTransferBuffer(_device, ref transferCreateInfo);
        if (transferBuffer == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create texture transfer buffer");
        }

        try
        {
            var mappedData = SDL3.SDL.MapGPUTransferBuffer(_device, transferBuffer, false);
            if (mappedData != nint.Zero)
            {
                unsafe
                {
                    Buffer.MemoryCopy(
                        (void*)surfaceStruct.Pixels,
                        (void*)mappedData,
                        pixelDataSize,
                        pixelDataSize
                    );
                }

                SDL3.SDL.UnmapGPUTransferBuffer(_device, transferBuffer);
            }

            var uploadCmdBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
            if (uploadCmdBuffer == nint.Zero)
            {
                throw new InvalidOperationException("Failed to acquire command buffer for texture upload");
            }

            var copyPass = SDL3.SDL.BeginGPUCopyPass(uploadCmdBuffer);
            if (copyPass != nint.Zero)
            {
                var source = new SDL3.SDL.GPUTextureTransferInfo
                {
                    TransferBuffer = transferBuffer,
                    Offset = 0
                };

                var destination = new SDL3.SDL.GPUTextureRegion
                {
                    Texture = gpuTexture,
                    MipLevel = 0,
                    Layer = 0,
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = (uint)width,
                    H = (uint)height,
                    D = 1
                };

                SDL3.SDL.UploadToGPUTexture(copyPass, ref source, ref destination, false);
                SDL3.SDL.EndGPUCopyPass(copyPass);
            }

            SDL3.SDL.SubmitGPUCommandBuffer(uploadCmdBuffer);
            SDL3.SDL.WaitForGPUIdle(_device);
        }
        finally
        {
            SDL3.SDL.ReleaseGPUTransferBuffer(_device, transferBuffer);
        }
    }

    public ITexture CreateBlankTexture(int width, int height, TextureScaleMode scaleMode)
    {
        ThrowIfNotInitialized();

        var textureCreateInfo = new SDL3.SDL.GPUTextureCreateInfo
        {
            Type = SDL3.SDL.GPUTextureType.TextureType2D,
            Format = SDL3.SDL.GPUTextureFormat.R8G8B8A8Unorm,
            Usage = SDL3.SDL.GPUTextureUsageFlags.Sampler | SDL3.SDL.GPUTextureUsageFlags.ColorTarget,
            Width = (uint)width,
            Height = (uint)height,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL3.SDL.GPUSampleCount.SampleCount1
        };

        var gpuTexture = SDL3.SDL.CreateGPUTexture(_device, ref textureCreateInfo);
        if (gpuTexture == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create blank GPU texture: {Error}", error);
            throw new InvalidOperationException($"Failed to create GPU texture: {error}");
        }

        return new SDL3Texture(
            $"blank_{width}x{height}",
            gpuTexture,
            width,
            height,
            scaleMode,
            _loggerFactory.CreateLogger<SDL3Texture>());
    }

    public void ReleaseTexture(ITexture texture)
    {
        if (texture is SDL3Texture gpuTexture)
        {
            SDL3.SDL.ReleaseGPUTexture(_device, gpuTexture.Handle);
            _logger.LogDebug("Released GPU texture: {Source}", texture.Source);
        }
    }

    private void ThrowIfNotInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("GPU renderer is not initialized");
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing SDL3 GPU renderer");

        _eventBus?.Unsubscribe<WindowResizedEvent>(OnWindowResized);

        if (_device != nint.Zero)
        {
            SDL3.SDL.WaitForGPUIdle(_device);
        }

        _renderTargetManager.Dispose();  // NEW
        
        _textRenderer.Dispose();
        _vertexShader?.Dispose();
        _fragmentShader?.Dispose();

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

        foreach (var pipeline in _graphicsPipelines.Values)
        {
            if (pipeline != nint.Zero)
            {
                SDL3.SDL.ReleaseGPUGraphicsPipeline(_device, pipeline);
            }
        }
        _graphicsPipelines.Clear();
        _graphicsPipeline = nint.Zero;

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

    private sealed class ViewportState
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public ViewportState(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void Update(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    // ============================================================
    // VECTOR2 OVERLOADS (delegate to float overloads)
    // ============================================================

    // Rectangles
    public void DrawRectangleFilled(Rectangle rect, Color color)
    {
        DrawRectangleFilled(rect.X, rect.Y, rect.Width, rect.Height, color);
    }

    public void DrawRectangleOutline(Rectangle rect, Color color, float thickness = 1f)
    {
        DrawRectangleOutline(rect.X, rect.Y, rect.Width, rect.Height, color, thickness);
    }

    // Circles
    public void DrawCircleFilled(Vector2 center, float radius, Color color)
    {
        DrawCircleFilled(center.X, center.Y, radius, color);
    }

    public void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness = 1f)
    {
        DrawCircleOutline(center.X, center.Y, radius, color, thickness);
    }

    // Lines
    public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
    {
        DrawLine(start.X, start.Y, end.X, end.Y, color, thickness);
    }

    private nint GetTextureHandle(ITexture texture)
    {
        return texture switch
        {
            SDL3Texture gpuTexture => gpuTexture.Handle,
            RenderTargetTextureView rtView => rtView.Handle,
            _ => throw new ArgumentException(
                $"Unsupported texture type: {texture.GetType().Name}. " +
                $"Only SDL3GPUTexture and RenderTarget textures are supported.",
                nameof(texture))
        };
    }
}