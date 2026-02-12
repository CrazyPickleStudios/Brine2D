using Brine2D.Core;
using Brine2D.Rendering.Text;
using Brine2D.Rendering.PostProcessing;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.SDL.Common;
using Brine2D.SDL.Common.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Numerics;
using System.Runtime.InteropServices;
using Brine2D.Events;
using Brine2D.Rendering;

namespace Brine2D.SDL.Rendering;

/// <summary>
/// SDL3 GPU API implementation of the renderer.
/// Modern, shader-based renderer with cross-platform support including texture and text rendering.
/// </summary>
public class SDL3GPURenderer : IRenderer, ISDL3WindowProvider, ITextureContext
{
    private readonly ILogger<SDL3GPURenderer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly RenderingOptions _renderingOptions;
    private readonly WindowOptions _windowOptions;
    private readonly IFontLoader? _fontLoader;
    private readonly EventBus? _eventBus;
    private readonly List<Vertex> _vertexBatch = new(MaxVertices);

    private ViewportState _viewport;

    private readonly MarkupParser _markupParser;

    private nint _window;
    private nint _device;
    private nint _sampler;
    private nint _samplerNearest;
    private nint _whiteTexture;
    private nint _graphicsPipeline;
    private nint _vertexBuffer;

    private nint _commandBuffer;
    private nint _renderPass;
    private nint _swapchainTexture;
    private nint _currentBoundTexture = nint.Zero;
    private TextureScaleMode _currentTextureScaleMode = TextureScaleMode.Linear;
    private bool _isFirstFlush = true;

    private IShaderLoader? _shaderLoader;
    private IShader? _vertexShader;
    private IShader? _fragmentShader;

    private IFont? _defaultFont;
    private FontAtlas? _defaultFontAtlas;

    private Matrix4x4 _projectionMatrix;
    private Color _clearColor = new Color(52, 78, 65, 255);

    private readonly Dictionary<BlendMode, nint> _graphicsPipelines = new();
    private BlendMode _currentBlendMode = BlendMode.Alpha;

    private RenderTarget? _mainRenderTarget;
    private RenderTarget? _pingPongTarget;
    private SDL3PostProcessPipeline? _postProcessPipeline;
    private PostProcessingOptions? _postProcessingOptions;
    private bool _usePostProcessing;

    private byte _currentRenderLayer = 128; // Default: middle layer

    private IRenderTarget? _currentRenderTarget;
    private Stack<IRenderTarget?> _renderTargetStack = new();

    private Rectangle? _currentScissorRect;
    private readonly Stack<Rectangle?> _scissorRectStack = new();

    private readonly IMarkupParser _defaultMarkupParser;

    public Color ClearColor
    {
        get => _clearColor;
        set => _clearColor = value;
    }

    public IRenderTarget CreateRenderTarget(int width, int height)
    {
        ThrowIfNotInitialized();
        
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
        ThrowIfNotInitialized();
        
        // Flush any pending draws before changing render target
        FlushBatch();
        
        _currentRenderTarget = target;
        _logger.LogDebug("Render target set to: {Target}", 
            target == null ? "Screen" : $"{target.Width}x{target.Height}");
    }

    public IRenderTarget? GetRenderTarget()
    {
        return _currentRenderTarget;
    }

    /// <summary>
    /// Push the current render target onto a stack and set a new one.
    /// Useful for nested render-to-texture operations.
    /// </summary>
    public void PushRenderTarget(IRenderTarget? target)
    {
        _renderTargetStack.Push(_currentRenderTarget);
        SetRenderTarget(target);
    }

    /// <summary>
    /// Pop the previous render target from the stack.
    /// </summary>
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

    public void SetRenderLayer(byte layer)
    {
        _currentRenderLayer = layer;
    }

    public byte GetRenderLayer()
    {
        return _currentRenderLayer;
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

        // Bind texture
        EnsureTextureBound(textureHandle, texture.ScaleMode);

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

        // Draw
        if (rotation != 0f)
            AddQuadRotated(adjustedX, adjustedY, destWidth, destHeight, rotation, actualColor, u1, v1, u2, v2);
        else
            AddQuad(adjustedX, adjustedY, destWidth, destHeight, actualColor, u1, v1, u2, v2);
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

    private const int MaxVertices = 10000;
    private const TextureScaleMode WhiteTextureScaleMode = TextureScaleMode.Nearest;
    private static readonly int VertexSize = Marshal.SizeOf<Vertex>();

    public nint Window => _window;
    public nint Device => _device;
    public bool IsInitialized { get; private set; }
    public ICamera? Camera { get; set; }
    public int Width => _viewport.Width;
    public int Height => _viewport.Height;

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public Vector2 Position;
        public Vector4 Color;
        public Vector2 TexCoord;
    }

    public SDL3GPURenderer(
        ILogger<SDL3GPURenderer> logger,
        ILoggerFactory loggerFactory,
        IOptions<RenderingOptions> renderingOptions,
        IOptions<WindowOptions> windowOptions,
        IOptions<PostProcessingOptions>? postProcessingOptions = null,
        SDL3PostProcessPipeline? postProcessPipeline = null,
        IFontLoader? fontLoader = null,
        EventBus? eventBus = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _renderingOptions = renderingOptions?.Value ?? throw new ArgumentNullException(nameof(renderingOptions));
        _windowOptions = windowOptions?.Value ?? throw new ArgumentNullException(nameof(windowOptions));
        _postProcessingOptions = postProcessingOptions?.Value;
        _postProcessPipeline = postProcessPipeline;

        _fontLoader = fontLoader;
        _eventBus = eventBus;

        _usePostProcessing = _postProcessingOptions?.Enabled == true && _postProcessPipeline != null;

        _viewport = new ViewportState(_windowOptions.Width, _windowOptions.Height);

        _markupParser = new MarkupParser(_logger);

        _defaultMarkupParser = new BBCodeParser(_logger);
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

        _device = SDL3.SDL.CreateGPUDevice(shaderFormats, true, _renderingOptions.PreferredGPUDriver);

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
        UpdateProjectionMatrix(_viewport.Width, _viewport.Height);

        await LoadDefaultFontAsync(cancellationToken);

        if (_usePostProcessing)
        {
            CreateRenderTargets();
            _logger.LogInformation("Post-processing enabled with {EffectCount} effects", _postProcessPipeline?.Effects.Count ?? 0);
        }

        _eventBus?.Subscribe<WindowResizedEvent>(OnWindowResized);

        IsInitialized = true;
        _logger.LogInformation("SDL3 GPU renderer fully initialized");
    }

    private void OnWindowResized(WindowResizedEvent evt)
    {
        _viewport.Update(evt.Width, evt.Height);
        UpdateProjectionMatrix(_viewport.Width, evt.Height);

        if (_usePostProcessing)
        {
            CreateRenderTargets();
        }

        _logger.LogInformation("Viewport resized to {Width}x{Height}, projection matrix updated",
            evt.Width, evt.Height);
    }

    private void CreateRenderTargets()
    {
        var format = _postProcessingOptions?.RenderTargetFormat ?? SDL3.SDL.GPUTextureFormat.R8G8B8A8Unorm;

        _mainRenderTarget?.Dispose();
        _mainRenderTarget = new RenderTarget(_device, _viewport.Width, _viewport.Height, format,
            _loggerFactory.CreateLogger<RenderTarget>());

        _pingPongTarget?.Dispose();
        _pingPongTarget = new RenderTarget(_device, _viewport.Width, _viewport.Height, format,
            _loggerFactory.CreateLogger<RenderTarget>());

        _logger.LogInformation("Created render targets for post-processing: {Width}x{Height}",
            _viewport.Width, _viewport.Height);
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
                Pitch = (uint)VertexSize,
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
            Size = (uint)(VertexSize * MaxVertices)
        };

        _vertexBuffer = SDL3.SDL.CreateGPUBuffer(_device, ref bufferCreateInfo);

        if (_vertexBuffer == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create vertex buffer: {Error}", error);
            throw new InvalidOperationException($"Failed to create vertex buffer: {error}");
        }

        _logger.LogDebug("Vertex buffer created ({Size} vertices)", MaxVertices);
    }

    private void UpdateProjectionMatrix(int width, int height)
    {
        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            0, width, height, 0, -1, 1);
    }

    public void BeginFrame()
    {
        ThrowIfNotInitialized();

        _vertexBatch.Clear();
        _currentBoundTexture = nint.Zero;
        _currentTextureScaleMode = TextureScaleMode.Linear;
        _isFirstFlush = true;

        _commandBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
        if (_commandBuffer == nint.Zero)
        {
            _logger.LogWarning("Failed to acquire command buffer, skipping frame");
            _swapchainTexture = nint.Zero;
            return;
        }

        // Always acquire swapchain (we'll need it for final present)
        if (!SDL3.SDL.AcquireGPUSwapchainTexture(_commandBuffer, _window, out _swapchainTexture, out _, out _))
        {
            _logger.LogDebug("Failed to acquire swapchain texture (window may be minimized)");
            SDL3.SDL.CancelGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
            _swapchainTexture = nint.Zero;
            return;
        }

        // When post-processing is enabled, we'll render to the render target instead
        // The swapchain will be used in EndFrame after effects are applied
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

        if (_swapchainTexture == nint.Zero)
        {
            return;
        }

        FlushBatch();

        // Apply post-processing if enabled
        if (_usePostProcessing && _mainRenderTarget != null && _pingPongTarget != null && _postProcessPipeline != null)
        {
            // Execute pipeline - it returns true if any effects were applied
            bool effectsApplied = _postProcessPipeline.Execute(
                this, 
                _mainRenderTarget.TextureHandle, 
                _swapchainTexture, 
                _commandBuffer, 
                _pingPongTarget);
            
            // If no effects were applied, blit manually
            if (!effectsApplied)
            {
                BlitTextureToSwapchain(_mainRenderTarget.TextureHandle, _swapchainTexture);
            }
        }
    }

    public void EndFrame()
    {
        ThrowIfNotInitialized();

        if (_swapchainTexture == nint.Zero)
        {
            return;
        }
        
        if (_commandBuffer != nint.Zero)
        {
            SDL3.SDL.SubmitGPUCommandBuffer(_commandBuffer);
            _commandBuffer = nint.Zero;
        }

        _swapchainTexture = nint.Zero;
        _currentBoundTexture = nint.Zero;
    }

    private void BlitTextureToSwapchain(nint sourceTexture, nint targetTexture)
    {
        // Use GPU blit operation to copy render target to swapchain
        var blitInfo = new SDL3.SDL.GPUBlitInfo
        {
            Source = new SDL3.SDL.GPUBlitRegion
            {
                Texture = sourceTexture,
                MipLevel = 0,
                LayerOrDepthPlane = 0,
                X = 0,
                Y = 0,
                W = (uint)_viewport.Width,
                H = (uint)_viewport.Height
            },
            Destination = new SDL3.SDL.GPUBlitRegion
            {
                Texture = targetTexture,
                MipLevel = 0,
                LayerOrDepthPlane = 0,
                X = 0,
                Y = 0,
                W = (uint)_viewport.Width,
                H = (uint)_viewport.Height
            },
            LoadOp = SDL3.SDL.GPULoadOp.Load, 
            ClearColor = new SDL3.SDL.FColor { R = 0, G = 0, B = 0, A = 1 },
            FlipMode = SDL3.SDL.FlipMode.None,
            Filter = SDL3.SDL.GPUFilter.Linear,
            Cycle = 0
        };

        SDL3.SDL.BlitGPUTexture(_commandBuffer, ref blitInfo);
    }

    private void FlushBatch()
    {
        if (_vertexBatch.Count == 0) return;

        if (_swapchainTexture == nint.Zero)
        {
            return;
        }

        if (_commandBuffer == nint.Zero)
        {
            _logger.LogError("Command buffer is null in FlushBatch");
            throw new InvalidOperationException("Command buffer is null");
        }

        UploadVertexData();

        // Determine render target based on current state
        nint renderTarget;
        if (_currentRenderTarget != null)
        {
            renderTarget = (_currentRenderTarget as RenderTarget)?.TextureHandle ?? nint.Zero;
            if (renderTarget == nint.Zero)
            {
                _logger.LogError("Invalid render target");
                return;
            }
        }
        else if (_usePostProcessing && _mainRenderTarget != null)
        {
            renderTarget = _mainRenderTarget.TextureHandle;
        }
        else
        {
            renderTarget = _swapchainTexture;
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
            LoadOp = _isFirstFlush ? SDL3.SDL.GPULoadOp.Clear : SDL3.SDL.GPULoadOp.Load,
            StoreOp = SDL3.SDL.GPUStoreOp.Store
        };

        _isFirstFlush = false;

        var colorTargets = new[] { colorTargetInfo };
        var colorTargetHandle = GCHandle.Alloc(colorTargets, GCHandleType.Pinned);

        try
        {
            _renderPass = SDL3.SDL.BeginGPURenderPass(
                _commandBuffer,
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

            var textureToUse = _currentBoundTexture != nint.Zero ? _currentBoundTexture : _whiteTexture;
            var samplerToUse = _currentTextureScaleMode == TextureScaleMode.Nearest
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

                if (_currentScissorRect.HasValue)
                {
                    var rect = _currentScissorRect.Value;
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
                    SDL3.SDL.DrawGPUPrimitives(_renderPass, (uint)_vertexBatch.Count, 1, 0, 0);
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

        _vertexBatch.Clear();
    }

    private void UploadVertexData()
    {
        if (_vertexBatch.Count == 0) return;

        var vertexDataSize = (uint)(VertexSize * _vertexBatch.Count);

        var transferCreateInfo = new SDL3.SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUTransferBufferUsage.Upload,
            Size = vertexDataSize
        };

        var transferBuffer = SDL3.SDL.CreateGPUTransferBuffer(_device, ref transferCreateInfo);
        if (transferBuffer == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create transfer buffer for vertex upload");
        }

        try
        {
            var mappedData = SDL3.SDL.MapGPUTransferBuffer(_device, transferBuffer, false);
            if (mappedData != nint.Zero)
            {
                unsafe
                {
                    fixed (Vertex* vertexPtr = CollectionsMarshal.AsSpan(_vertexBatch))
                    {
                        Buffer.MemoryCopy(
                            vertexPtr,
                            (void*)mappedData,
                            vertexDataSize,
                            vertexDataSize
                        );
                    }
                }
                SDL3.SDL.UnmapGPUTransferBuffer(_device, transferBuffer);
            }

            var copyPass = SDL3.SDL.BeginGPUCopyPass(_commandBuffer);
            if (copyPass != nint.Zero)
            {
                var source = new SDL3.SDL.GPUTransferBufferLocation
                {
                    TransferBuffer = transferBuffer,
                    Offset = 0
                };

                var destination = new SDL3.SDL.GPUBufferRegion
                {
                    Buffer = _vertexBuffer,
                    Offset = 0,
                    Size = vertexDataSize
                };

                SDL3.SDL.UploadToGPUBuffer(copyPass, ref source, ref destination, false);
                SDL3.SDL.EndGPUCopyPass(copyPass);
            }
        }
        finally
        {
            SDL3.SDL.ReleaseGPUTransferBuffer(_device, transferBuffer);
        }
    }

    private void PushUniformData()
    {
        unsafe
        {
            fixed (Matrix4x4* matrixPtr = &_projectionMatrix)
            {
                SDL3.SDL.PushGPUVertexUniformData(
                    _commandBuffer,
                    0,
                    (nint)matrixPtr,
                    (uint)sizeof(Matrix4x4)
                );
            }
        }
    }

    private void AddQuad(float x, float y, float width, float height, Color color,
        float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
    {
        if (_vertexBatch.Count + 6 > MaxVertices)
        {
            FlushBatch();
        }

        AddVertex(x, y, color, u1, v1);
        AddVertex(x + width, y, color, u2, v1);
        AddVertex(x, y + height, color, u1, v2);

        AddVertex(x + width, y, color, u2, v1);
        AddVertex(x + width, y + height, color, u2, v2);
        AddVertex(x, y + height, color, u1, v2);
    }

    private void AddQuadRotated(float x, float y, float width, float height, float rotation, Color color,
        float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
    {
        EnsureVertexCapacity(6);

        float centerX = x + width / 2f;
        float centerY = y + height / 2f;
        float halfW = width / 2f;
        float halfH = height / 2f;

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        var topLeft = RotatePoint(-halfW, -halfH, cos, sin, centerX, centerY);
        var topRight = RotatePoint(halfW, -halfH, cos, sin, centerX, centerY);
        var bottomLeft = RotatePoint(-halfW, halfH, cos, sin, centerX, centerY);
        var bottomRight = RotatePoint(halfW, halfH, cos, sin, centerX, centerY);

        AddVertex(topLeft.X, topLeft.Y, color, u1, v1);
        AddVertex(topRight.X, topRight.Y, color, u2, v1);
        AddVertex(bottomLeft.X, bottomLeft.Y, color, u1, v2);

        AddVertex(topRight.X, topRight.Y, color, u2, v1);
        AddVertex(bottomRight.X, bottomRight.Y, color, u2, v2);
        AddVertex(bottomLeft.X, bottomLeft.Y, color, u1, v2);
    }

    private static (float X, float Y) RotatePoint(float x, float y, float cos, float sin, float centerX, float centerY)
    {
        return (
            x * cos - y * sin + centerX,
            x * sin + y * cos + centerY
        );
    }

    private void AddVertex(float x, float y, Color color, float u = 0, float v = 0)
    {
        var position = new Vector2(x, y);

        if (Camera != null)
        {
            position = Camera.WorldToScreen(position);
        }

        position = new Vector2(MathF.Round(position.X), MathF.Round(position.Y));

        _vertexBatch.Add(new Vertex
        {
            Position = position,
            Color = ColorToVector4(color),
            TexCoord = new Vector2(u, v)
        });
    }

    private void EnsureTextureBound(nint textureHandle, TextureScaleMode scaleMode)
    {
        if (_currentBoundTexture != nint.Zero &&
            (_currentBoundTexture != textureHandle || _currentTextureScaleMode != scaleMode))
        {
            FlushBatch();
        }

        _currentBoundTexture = textureHandle;
        _currentTextureScaleMode = scaleMode;
    }

    private void EnsureVertexCapacity(int verticesNeeded)
    {
        if (_vertexBatch.Count + verticesNeeded > MaxVertices)
        {
            FlushBatch();
        }
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        if (_currentBlendMode == blendMode)
            return;

        FlushBatch();

        if (!_graphicsPipelines.TryGetValue(blendMode, out var pipeline))
        {
            pipeline = CreateGraphicsPipelineForBlendMode(blendMode);
            _graphicsPipelines[blendMode] = pipeline;
            _logger.LogDebug("Lazily created graphics pipeline for blend mode: {BlendMode}", blendMode);
        }

        _graphicsPipeline = pipeline;
        _currentBlendMode = blendMode;
    }

    // New methods for scissor rectangle control

    public void SetScissorRect(Rectangle? rect)
    {
        ThrowIfNotInitialized();
        
        // Validate rectangle if provided
        if (rect.HasValue)
        {
            var r = rect.Value;
            if (r.Width < 0 || r.Height < 0)
            {
                throw new ArgumentException(
                    "Scissor rectangle dimensions cannot be negative", 
                    nameof(rect));
            }
            
            // Clamp to viewport bounds
            if (r.X < 0 || r.Y < 0 || 
                r.X + r.Width > _viewport.Width || 
                r.Y + r.Height > _viewport.Height)
            {
                _logger.LogWarning(
                    "Scissor rect ({X}, {Y}, {Width}, {Height}) extends beyond viewport ({ViewportWidth}x{ViewportHeight})",
                    r.X, r.Y, r.Width, r.Height, _viewport.Width, _viewport.Height);
            }
        }
        
        FlushBatch();
        
        _currentScissorRect = rect;
        
        _logger.LogDebug("Scissor rect set to: {Rect}", 
            rect.HasValue ? $"{rect.Value.X}, {rect.Value.Y}, {rect.Value.Width}x{rect.Value.Height}" : "None");
    }

    public Rectangle? GetScissorRect()
    {
        return _currentScissorRect;
    }

    public void PushScissorRect(Rectangle? rect)
    {
        _scissorRectStack.Push(_currentScissorRect);
        SetScissorRect(rect);
        
        _logger.LogDebug("Scissor rect pushed (stack depth: {Depth})", _scissorRectStack.Count);
    }

    public void PopScissorRect()
    {
        if (_scissorRectStack.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot pop scissor rect: stack is empty");
        }
        
        var previousRect = _scissorRectStack.Pop();
        SetScissorRect(previousRect);
        
        _logger.LogDebug("Scissor rect popped (stack depth: {Depth})", _scissorRectStack.Count);
    }

    private static Vector4 ColorToVector4(Color color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    private static int CalculateCircleSegments(float radius) =>
        Math.Max(16, (int)(radius * 2));

    public void DrawRectangleFilled(float x, float y, float width, float height, Color color)
    {
        ThrowIfNotInitialized();
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode);
        AddQuad(x, y, width, height, color);
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
        EnsureVertexCapacity(6);
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode);

        var dx = x2 - x1;
        var dy = y2 - y1;
        var length = MathF.Sqrt(dx * dx + dy * dy);

        if (length == 0) return;

        var angle = MathF.Atan2(dy, dx);
        var halfThickness = Math.Max(thickness, 0.5f) / 2f;
        var perpX = -MathF.Sin(angle) * halfThickness;
        var perpY = MathF.Cos(angle) * halfThickness;

        AddVertex(x1 + perpX, y1 + perpY, color);
        AddVertex(x2 + perpX, y2 + perpY, color);
        AddVertex(x1 - perpX, y1 - perpY, color);

        AddVertex(x2 + perpX, y2 + perpY, color);
        AddVertex(x2 - perpX, y2 - perpY, color);
        AddVertex(x1 - perpX, y1 - perpY, color);
    }

    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color)
    {
        ThrowIfNotInitialized();

        int segments = CalculateCircleSegments(radius);
        EnsureVertexCapacity(segments * 3);
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode);

        float angleStep = MathF.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;

            AddVertex(centerX, centerY, color);
            AddVertex(centerX + MathF.Cos(angle1) * radius, centerY + MathF.Sin(angle1) * radius, color);
            AddVertex(centerX + MathF.Cos(angle2) * radius, centerY + MathF.Sin(angle2) * radius, color);
        }
    }

    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1)
    {
        ThrowIfNotInitialized();

        int segments = CalculateCircleSegments(radius);
        EnsureVertexCapacity(segments * 6);

        float angleStep = MathF.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;

            float x1 = centerX + MathF.Cos(angle1) * radius;
            float y1 = centerY + MathF.Sin(angle1) * radius;
            float x2 = centerX + MathF.Cos(angle2) * radius;
            float y2 = centerY + MathF.Sin(angle2) * radius;

            DrawLine(x1, y1, x2, y2, color, thickness);
        }
    }

    // ============================================================
    // TEXT RENDERING
    // ============================================================

    public void DrawText(string text, float x, float y, Color color)
    {
        DrawText(text, x, y, new TextRenderOptions
        {
            Color = color,
            Font = _defaultFont
        });
    }

    public void DrawText(string text, float x, float y, TextRenderOptions options)
    {
        ThrowIfNotInitialized();

        if (string.IsNullOrEmpty(text))
            return;

        // Use custom parser if provided, otherwise use default BBCode parser
        var parser = options.ParseMarkup 
            ? (options.MarkupParser ?? _defaultMarkupParser)
            : new PlainTextParser();

        var runs = parser.Parse(text, options);

        // Render with layout
        RenderTextRuns(runs, x, y, options);
    }

    public Vector2 MeasureText(string text, float? fontSize = null)
    {
        if (string.IsNullOrEmpty(text) || _defaultFont == null)
            return Vector2.Zero;

        if (_defaultFont is not SDL3Font sdlFont)
            return Vector2.Zero;

        // Simple SDL measurement for plain text
        if (SDL3.TTF.GetStringSize(sdlFont.Handle, text, 0, out int w, out int h))
        {
            float scale = fontSize.HasValue ? (fontSize.Value / _defaultFont.Size) : 1.0f;
            return new Vector2(w * scale, h * scale);
        }

        return Vector2.Zero;
    }

    public Vector2 MeasureText(string text, TextRenderOptions options)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        var runs = options.ParseMarkup
            ? _markupParser.Parse(text, options)
            : new[] { new TextRun { Text = text, FontSize = options.FontSize } };

        return MeasureTextRuns(runs, options);
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

        // Ensure font atlas is ready
        EnsureFontAtlasGenerated();

        if (_defaultFontAtlas == null || _defaultFontAtlas.Texture == null)
        {
            _logger.LogWarning("No font atlas available");
            return;
        }

        // Calculate total text bounds for alignment
        var textSize = MeasureTextRuns(runs, options);

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
        float lineHeight = _defaultFontAtlas.LineHeight * options.LineSpacing;

        var atlasTexture = _defaultFontAtlas.Texture;

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

            if (!_defaultFontAtlas.TryGetGlyph(c, out var glyph))
                continue;

            float u1 = glyph.AtlasX / (float)atlasTexture.Width;
            float v1 = glyph.AtlasY / (float)atlasTexture.Height;
            float u2 = (glyph.AtlasX + glyph.Width) / (float)atlasTexture.Width;
            float v2 = (glyph.AtlasY + glyph.Height) / (float)atlasTexture.Height;

            var atlasGpuTexture = (SDL3GPUTexture)atlasTexture;
            EnsureTextureBound(atlasGpuTexture.Handle, atlasTexture.ScaleMode);

            AddQuad(localCursorX, y, glyph.Width, glyph.Height, color, u1, v1, u2, v2);

            localCursorX += glyph.Advance;
        }

        if (advanceCursor)
        {
            cursorX = localCursorX;
        }
    }

    private Vector2 MeasureTextRuns(IReadOnlyList<TextRun> runs, TextRenderOptions options)
    {
        float totalWidth = 0;
        float totalHeight = _defaultFontAtlas?.LineHeight ?? 16;

        foreach (var run in runs)
        {
            var size = MeasureText(run.Text, run.FontSize);
            totalWidth += size.X;
            totalHeight = MathF.Max(totalHeight, size.Y);
        }

        return new Vector2(totalWidth, totalHeight);
    }

    public void SetDefaultFont(IFont? font)
    {
        if (font != null && font is not SDL3Font)
        {
            _logger.LogWarning("Font must be an SDL3Font for GPU renderer");
            return;
        }

        _defaultFont = font as SDL3Font;

        _defaultFontAtlas?.Dispose();
        _defaultFontAtlas = null;

        if (_defaultFont != null)
        {
            _logger.LogInformation("Default font set to {Font}, atlas will be generated on first use", _defaultFont.Name);
        }
    }

    private void EnsureFontAtlasGenerated()
    {
        if (_defaultFont == null || _defaultFontAtlas != null)
            return;

        if (_defaultFont is not SDL3Font sdlFont)
        {
            _logger.LogWarning("Default font is not an SDL3Font, cannot generate atlas");
            return;
        }

        _logger.LogInformation("Generating font atlas for {Font}", sdlFont.Name);
        _defaultFontAtlas = new FontAtlas(_loggerFactory.CreateLogger<FontAtlas>());

        if (!_defaultFontAtlas.Generate(sdlFont, this, TextureScaleMode.Nearest))
        {
            _logger.LogError("Failed to generate font atlas");
            _defaultFontAtlas?.Dispose();
            _defaultFontAtlas = null;
        }
    }

    private async Task LoadDefaultFontAsync(CancellationToken cancellationToken)
    {
        if (_fontLoader == null)
        {
            _logger.LogInformation("No font loader available, text rendering will require SetDefaultFont()");
            return;
        }

        try
        {
            var assembly = typeof(SDL3GPURenderer).Assembly;
            var resourceName = "Brine2D.SDL.Fonts.Roboto.ttf";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogWarning("Default font not found in embedded resources");
                return;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), "Brine2D", "Roboto.ttf");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

            using (var fileStream = File.Create(tempPath))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            _logger.LogDebug("Font extracted to: {TempPath}", tempPath);

            var loadedFont = await _fontLoader.LoadFontAsync(tempPath, 16, cancellationToken);

            if (loadedFont is SDL3Font sdlFont)
            {
                _defaultFont = sdlFont;
                _logger.LogInformation("Default font loaded from embedded resource at 16pt");
            }
            else
            {
                _logger.LogWarning("Loaded font is not an SDL3Font");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load default font");
        }
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

        return new SDL3GPUTexture(
            "surface_texture",
            gpuTexture,
            width,
            height,
            scaleMode,
            _loggerFactory.CreateLogger<SDL3GPUTexture>());
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

        return new SDL3GPUTexture(
            $"blank_{width}x{height}",
            gpuTexture,
            width,
            height,
            scaleMode,
            _loggerFactory.CreateLogger<SDL3GPUTexture>());
    }

    public void ReleaseTexture(ITexture texture)
    {
        if (texture is SDL3GPUTexture gpuTexture)
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

        if (_postProcessPipeline is IDisposable disposablePipeline)
        {
            disposablePipeline.Dispose();
        }

        _defaultFontAtlas?.Dispose();
        _defaultFontAtlas = null;

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

        _mainRenderTarget?.Dispose();
        _pingPongTarget?.Dispose();

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
            SDL3GPUTexture gpuTexture => gpuTexture.Handle,
            RenderTargetTextureView rtView => rtView.Handle,
            _ => throw new ArgumentException(
                $"Unsupported texture type: {texture.GetType().Name}. " +
                $"Only SDL3GPUTexture and RenderTarget textures are supported.",
                nameof(texture))
        };
    }
}