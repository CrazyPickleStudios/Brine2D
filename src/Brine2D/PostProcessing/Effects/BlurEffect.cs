using Brine2D.Rendering;
using Brine2D.Rendering.SDL.Shaders.PostProcessing;
using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering.SDL.PostProcessing.Effects;

/// <summary>
/// Two-pass Gaussian blur post-processing effect.
/// Uses a single shader with uniforms to control blur direction.
/// </summary>
public class BlurEffect : ISDL3PostProcessEffect, IDisposable
{
    private readonly ILogger<BlurEffect>? _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly GpuDeviceHandle _deviceHandle;
    private readonly SDL3.SDL.GPUTextureFormat _colorTargetFormat;
    private int _width;
    private int _height;

    private SDL3Shader? _vertexShader;
    private SDL3Shader? _fragmentShader;
    private nint _pipeline;
    private nint _sampler;
    private RenderTarget? _intermediateTarget;
    private int _disposed;
    private bool _initialized;

    public int Order { get; set; } = 0;
    public string Name => "Blur";
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Blur radius (1.0 = subtle, 5.0 = strong blur).
    /// Dynamically controllable via uniforms.
    /// </summary>
    public float BlurRadius { get; set; } = 2.0f;

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct BlurParams
    {
        public Vector2 Direction;  // 8 bytes
        public float BlurRadius;   // 4 bytes
        public float Padding;      // 4 bytes (16 bytes total)
    }

    internal BlurEffect(
        GpuDeviceHandle deviceHandle,
        int width,
        int height,
        SDL3.SDL.GPUTextureFormat colorTargetFormat,
        ILoggerFactory loggerFactory,
        ILogger<BlurEffect>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(deviceHandle);
        _deviceHandle = deviceHandle;
        _width = width;
        _height = height;
        _colorTargetFormat = colorTargetFormat;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        _logger?.LogInformation("Initializing Blur effect with dynamic uniforms (space3)");

        try
        {
            CreateSampler();
            CreateShaders();
            CreatePipeline();
            CreateIntermediateTarget();

            _initialized = true;
            _logger?.LogInformation("Blur effect initialized successfully");
        }
        catch
        {
            ReleasePartialResources();
            throw;
        }
    }

    private void ReleasePartialResources()
    {
        var device = _deviceHandle.Handle;
        if (device == nint.Zero) return;

        _intermediateTarget?.Dispose();
        _intermediateTarget = null;

        if (_pipeline != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUGraphicsPipeline(device, _pipeline);
            _pipeline = nint.Zero;
        }

        _fragmentShader?.Dispose();
        _fragmentShader = null;

        _vertexShader?.Dispose();
        _vertexShader = null;

        if (_sampler != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUSampler(device, _sampler);
            _sampler = nint.Zero;
        }
    }

    private void CreateSampler()
    {
        var device = _deviceHandle.Handle;
        var samplerInfo = new SDL3.SDL.GPUSamplerCreateInfo
        {
            MinFilter = SDL3.SDL.GPUFilter.Linear,
            MagFilter = SDL3.SDL.GPUFilter.Linear,
            MipmapMode = SDL3.SDL.GPUSamplerMipmapMode.Linear,
            AddressModeU = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeW = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
        };

        _sampler = SDL3.SDL.CreateGPUSampler(device, ref samplerInfo);
        if (_sampler == nint.Zero)
        {
            throw new InvalidOperationException($"Failed to create sampler: {SDL3.SDL.GetError()}");
        }
    }

    private void CreateShaders()
    {
        try
        {
            _logger?.LogInformation("Loading pre-compiled blur shaders...");

            var device = _deviceHandle.Handle;
            var driverName = SDL3.SDL.GetGPUDeviceDriver(device);
            var targetFormat = ShaderFormatHelper.GetTargetFromDriver(driverName);
            var shaderFormat = ShaderFormatHelper.GetShaderFormat(targetFormat);

            byte[]? vertexBytecode = null;
            byte[]? fragmentBytecode = null;

            switch (targetFormat)
            {
                case ShaderFormatHelper.GraphicsShaderTarget.SPIRV:
                    vertexBytecode = BlurShaders.LoadVertexShaderSPIRV();
                    fragmentBytecode = BlurShaders.LoadFragmentShaderSPIRV();
                    break;
                case ShaderFormatHelper.GraphicsShaderTarget.DXIL:
                    vertexBytecode = BlurShaders.LoadVertexShaderDXIL();
                    fragmentBytecode = BlurShaders.LoadFragmentShaderDXIL();
                    break;
                case ShaderFormatHelper.GraphicsShaderTarget.DXBC:
                    vertexBytecode = BlurShaders.LoadVertexShaderDXBC();
                    fragmentBytecode = BlurShaders.LoadFragmentShaderDXBC();
                    break;
                case ShaderFormatHelper.GraphicsShaderTarget.MSL:
                    vertexBytecode = BlurShaders.LoadVertexShaderMSL();
                    fragmentBytecode = BlurShaders.LoadFragmentShaderMSL();
                    break;
            }

            if (vertexBytecode == null || fragmentBytecode == null)
            {
                throw new InvalidOperationException($"Pre-compiled blur shaders not found for {targetFormat}");
            }

            _vertexShader = CreateShaderFromBytecode(ShaderStage.Vertex, vertexBytecode, "main", shaderFormat);
            _fragmentShader = CreateShaderFromBytecode(ShaderStage.Fragment, fragmentBytecode, "main", shaderFormat);

            if (_vertexShader.Handle == nint.Zero || _fragmentShader.Handle == nint.Zero)
            {
                throw new InvalidOperationException("Failed to create blur shaders");
            }

            _logger?.LogInformation("Blur shaders loaded successfully ({VertexSize} + {FragmentSize} bytes)",
                vertexBytecode.Length, fragmentBytecode.Length);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load blur shaders");
            throw;
        }
    }

    private SDL3Shader CreateShaderFromBytecode(ShaderStage stage, byte[] bytecode, string entryPoint, SDL3.SDL.GPUShaderFormat format)
    {
        var shader = new SDL3Shader($"blur_{stage}", stage, _loggerFactory.CreateLogger<SDL3Shader>());
        
        uint numUniformBuffers = stage == ShaderStage.Fragment ? 1u : 0u;
        
        if (!shader.Compile(_deviceHandle.Handle, bytecode, entryPoint, format, numUniformBuffers))
        {
            shader.Dispose();
            throw new InvalidOperationException($"Failed to compile blur {stage} shader");
        }
        
        return shader;
    }

    private void CreatePipeline()
    {
        var colorTargetDescriptions = new SDL3.SDL.GPUColorTargetDescription[]
        {
            new()
            {
                Format = _colorTargetFormat,
                BlendState = new SDL3.SDL.GPUColorTargetBlendState
                {
                    EnableBlend = false,
                    ColorWriteMask = SDL3.SDL.GPUColorComponentFlags.R |
                                   SDL3.SDL.GPUColorComponentFlags.G |
                                   SDL3.SDL.GPUColorComponentFlags.B |
                                   SDL3.SDL.GPUColorComponentFlags.A
                }
            }
        };

        var colorTargetHandle = GCHandle.Alloc(colorTargetDescriptions, GCHandleType.Pinned);

        try
        {
            var pipelineCreateInfo = new SDL3.SDL.GPUGraphicsPipelineCreateInfo
            {
                VertexShader = _vertexShader!.Handle,
                FragmentShader = _fragmentShader!.Handle,
                VertexInputState = new SDL3.SDL.GPUVertexInputState
                {
                    VertexBufferDescriptions = IntPtr.Zero,
                    NumVertexBuffers = 0,
                    VertexAttributes = IntPtr.Zero,
                    NumVertexAttributes = 0
                },
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
                    EnableDepthTest = false,
                    EnableDepthWrite = false,
                    EnableStencilTest = false
                },
                TargetInfo = new SDL3.SDL.GPUGraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions = colorTargetHandle.AddrOfPinnedObject(),
                    NumColorTargets = 1,
                    DepthStencilFormat = SDL3.SDL.GPUTextureFormat.Invalid,
                    HasDepthStencilTarget = false
                },
                Props = 0
            };

            _pipeline = SDL3.SDL.CreateGPUGraphicsPipeline(_deviceHandle.Handle, ref pipelineCreateInfo);

            if (_pipeline == nint.Zero)
            {
                throw new InvalidOperationException($"Failed to create graphics pipeline: {SDL3.SDL.GetError()}");
            }
        }
        finally
        {
            colorTargetHandle.Free();
        }
    }

    private void CreateIntermediateTarget()
    {
        _intermediateTarget = new RenderTarget(
            _deviceHandle,
            _width,
            _height,
            _colorTargetFormat,
            _loggerFactory.CreateLogger<RenderTarget>());
    }

    public void SetDimensions(int width, int height)
    {
        _width = width;
        _height = height;

        if (_initialized)
        {
            var old = _intermediateTarget;
            CreateIntermediateTarget();
            old?.Dispose();
        }
    }

    public void Apply(IRenderer renderer, nint sourceTexture, nint targetTexture, nint commandBuffer)
    {
        if (sourceTexture == nint.Zero || targetTexture == nint.Zero || commandBuffer == nint.Zero)
        {
            _logger?.LogWarning("Invalid handles passed to {EffectName} - skipping", Name);
            return;
        }

        if (!Enabled) return;

        EnsureInitialized();

        if (_intermediateTarget == null)
        {
            _logger?.LogWarning("Intermediate target not initialized");
            return;
        }

        var hParams = new BlurParams
        {
            Direction = new Vector2(1, 0),
            BlurRadius = BlurRadius,
            Padding = 0
        };

        FullScreenQuad.RenderWithShaderAndUniforms(
            commandBuffer,
            sourceTexture,
            _intermediateTarget.TextureHandle,
            _pipeline,
            _sampler,
            hParams,
            _width,
            _height,
            _logger);

        var vParams = new BlurParams
        {
            Direction = new Vector2(0, 1),
            BlurRadius = BlurRadius,
            Padding = 0
        };

        FullScreenQuad.RenderWithShaderAndUniforms(
            commandBuffer,
            _intermediateTarget.TextureHandle,
            targetTexture,
            _pipeline,
            _sampler,
            vParams,
            _width,
            _height,
            _logger);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        var device = _deviceHandle.Handle;
        if (device == nint.Zero)
            return;

        _intermediateTarget?.Dispose();

        if (_sampler != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUSampler(device, _sampler);
            _sampler = nint.Zero;
        }

        if (_pipeline != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUGraphicsPipeline(device, _pipeline);
            _pipeline = nint.Zero;
        }

        _fragmentShader?.Dispose();
        _fragmentShader = null;

        _vertexShader?.Dispose();
        _vertexShader = null;
    }
}