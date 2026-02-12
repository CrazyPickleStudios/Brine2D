using Brine2D.Rendering;
using Brine2D.Rendering.SDL.Shaders.PostProcessing;
using Brine2D.SDL.Rendering;
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
    private readonly nint _device;
    private int _width;
    private int _height;

    private nint _vertexShader;
    private nint _fragmentShader;
    private nint _pipeline;
    private nint _sampler;
    private RenderTarget? _intermediateTarget;
    private bool _disposed;
    private bool _initialized;

    public int Order { get; set; } = 0;
    public string Name => "Blur";
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Blur radius (1.0 = subtle, 5.0 = strong blur).
    /// Now dynamically controllable via uniforms!
    /// </summary>
    public float BlurRadius { get; set; } = 2.0f;

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct BlurParams
    {
        public Vector2 Direction;  // 8 bytes
        public float BlurRadius;   // 4 bytes
        public float Padding;      // 4 bytes (16 bytes total)
    }

    public BlurEffect(nint device, int width, int height, ILoggerFactory loggerFactory, ILogger<BlurEffect>? logger = null)
    {
        _device = device;
        _width = width;
        _height = height;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        _logger?.LogInformation("Initializing Blur effect with dynamic uniforms (space3)");

        CreateSampler();
        CreateShaders();
        CreatePipeline();
        CreateIntermediateTarget();

        _initialized = true;
        _logger?.LogInformation("Blur effect initialized successfully");
    }

    private void CreateSampler()
    {
        var samplerInfo = new SDL3.SDL.GPUSamplerCreateInfo
        {
            MinFilter = SDL3.SDL.GPUFilter.Linear,
            MagFilter = SDL3.SDL.GPUFilter.Linear,
            MipmapMode = SDL3.SDL.GPUSamplerMipmapMode.Linear,
            AddressModeU = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeW = SDL3.SDL.GPUSamplerAddressMode.ClampToEdge,
        };

        _sampler = SDL3.SDL.CreateGPUSampler(_device, ref samplerInfo);
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

            var driverName = SDL3.SDL.GetGPUDeviceDriver(_device);
            var targetFormat = GetTargetFormatFromDriver(driverName);
            var shaderFormat = GetShaderFormat(targetFormat);

            byte[]? vertexBytecode = null;
            byte[]? fragmentBytecode = null;

            switch (targetFormat)
            {
                case GraphicsShaderTarget.SPIRV:
                    vertexBytecode = BlurShaders.LoadVertexShaderSPIRV();
                    fragmentBytecode = BlurShaders.LoadFragmentShaderSPIRV();
                    break;
                case GraphicsShaderTarget.DXIL:
                    vertexBytecode = BlurShaders.LoadVertexShaderDXIL();
                    fragmentBytecode = BlurShaders.LoadFragmentShaderDXIL();
                    break;
                case GraphicsShaderTarget.DXBC:
                    vertexBytecode = BlurShaders.LoadVertexShaderDXBC();
                    fragmentBytecode = BlurShaders.LoadFragmentShaderDXBC();
                    break;
                case GraphicsShaderTarget.MSL:
                    vertexBytecode = BlurShaders.LoadVertexShaderMSL();
                    fragmentBytecode = BlurShaders.LoadFragmentShaderMSL();
                    break;
            }

            if (vertexBytecode == null || fragmentBytecode == null)
            {
                throw new InvalidOperationException($"Pre-compiled blur shaders not found for {targetFormat}");
            }

            var vertexShaderObj = CreateShaderFromBytecode(ShaderStage.Vertex, vertexBytecode, "main", shaderFormat);
            var fragmentShaderObj = CreateShaderFromBytecode(ShaderStage.Fragment, fragmentBytecode, "main", shaderFormat);

            _vertexShader = (vertexShaderObj as SDL3Shader)?.Handle ?? nint.Zero;
            _fragmentShader = (fragmentShaderObj as SDL3Shader)?.Handle ?? nint.Zero;

            if (_vertexShader == nint.Zero || _fragmentShader == nint.Zero)
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

    private IShader CreateShaderFromBytecode(ShaderStage stage, byte[] bytecode, string entryPoint, SDL3.SDL.GPUShaderFormat format)
    {
        var shader = new SDL3Shader($"blur_{stage}", stage, _loggerFactory.CreateLogger<SDL3Shader>());
        
        // Fragment shader needs 1 uniform buffer for blur parameters
        uint numUniformBuffers = stage == ShaderStage.Fragment ? 1u : 0u;
        
        if (!shader.Compile(_device, bytecode, entryPoint, format, numUniformBuffers))
        {
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
                Format = SDL3.SDL.GPUTextureFormat.R8G8B8A8Unorm,
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
                VertexShader = _vertexShader,
                FragmentShader = _fragmentShader,
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

            _pipeline = SDL3.SDL.CreateGPUGraphicsPipeline(_device, ref pipelineCreateInfo);

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
            _device,
            _width,
            _height,
            SDL3.SDL.GPUTextureFormat.R8G8B8A8Unorm,
            _loggerFactory.CreateLogger<RenderTarget>());
    }

    private GraphicsShaderTarget GetTargetFormatFromDriver(string driverName)
    {
        return driverName.ToLowerInvariant() switch
        {
            "vulkan" => GraphicsShaderTarget.SPIRV,
            "direct3d12" or "d3d12" => GraphicsShaderTarget.DXIL,
            "direct3d11" or "d3d11" => GraphicsShaderTarget.DXBC,
            "metal" => GraphicsShaderTarget.MSL,
            _ => GraphicsShaderTarget.SPIRV
        };
    }

    private SDL3.SDL.GPUShaderFormat GetShaderFormat(GraphicsShaderTarget target)
    {
        return target switch
        {
            GraphicsShaderTarget.SPIRV => SDL3.SDL.GPUShaderFormat.SPIRV,
            GraphicsShaderTarget.DXIL => SDL3.SDL.GPUShaderFormat.DXIL,
            GraphicsShaderTarget.DXBC => SDL3.SDL.GPUShaderFormat.DXBC,
            GraphicsShaderTarget.MSL => SDL3.SDL.GPUShaderFormat.MSL,
            _ => SDL3.SDL.GPUShaderFormat.SPIRV
        };
    }

    private enum GraphicsShaderTarget
    {
        SPIRV,
        DXIL,
        DXBC,
        MSL
    }

    public void SetDimensions(int width, int height)
    {
        _width = width;
        _height = height;

        if (_initialized)
        {
            _intermediateTarget?.Dispose();
            CreateIntermediateTarget();
        }
    }

    public void Apply(IRenderer renderer, nint sourceTexture, nint targetTexture, nint commandBuffer)
    {
        if (!Enabled) return;

        EnsureInitialized();

        if (_intermediateTarget == null)
        {
            _logger?.LogWarning("Intermediate target not initialized");
            return;
        }

        // Pass 1: Horizontal blur (source -> intermediate)
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

        // Pass 2: Vertical blur (intermediate -> target)
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
        if (_disposed) return;

        _intermediateTarget?.Dispose();

        if (_sampler != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUSampler(_device, _sampler);
            _sampler = nint.Zero;
        }

        if (_pipeline != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUGraphicsPipeline(_device, _pipeline);
            _pipeline = nint.Zero;
        }

        if (_fragmentShader != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUShader(_device, _fragmentShader);
            _fragmentShader = nint.Zero;
        }

        if (_vertexShader != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUShader(_device, _vertexShader);
            _vertexShader = nint.Zero;
        }

        _disposed = true;
    }
}