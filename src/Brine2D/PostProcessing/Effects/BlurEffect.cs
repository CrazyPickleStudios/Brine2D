using Brine2D.Rendering;
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
        var device = _deviceHandle.Handle;
        var driverName = SDL3.SDL.GetGPUDeviceDriver(device);
        var target = ShaderFormatHelper.GetTargetFromDriver(driverName);
        var shaderFormat = ShaderFormatHelper.GetShaderFormat(target);

        _logger?.LogDebug("Compiling blur shaders for {Driver} via ShaderCross", driverName);

        var vertexBytecode = CompileHLSL(BlurVertexShaderHLSL, SDL3.ShaderCross.ShaderStage.Vertex, target);
        var fragmentBytecode = CompileHLSL(BlurFragmentShaderHLSL, SDL3.ShaderCross.ShaderStage.Fragment, target);

        _vertexShader = CreateShaderFromBytecode(ShaderStage.Vertex, vertexBytecode, "main", shaderFormat);
        _fragmentShader = CreateShaderFromBytecode(ShaderStage.Fragment, fragmentBytecode, "main", shaderFormat);

        _logger?.LogDebug("Blur shaders compiled via ShaderCross");
    }

    private static byte[] CompileHLSL(
        string hlsl,
        SDL3.ShaderCross.ShaderStage stage,
        ShaderFormatHelper.GraphicsShaderTarget target)
    {
        var sourcePtr = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8(hlsl);
        var entrypointPtr = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8("main");
        try
        {
            var hlslInfo = new SDL3.ShaderCross.HLSLInfo
            {
                Source = sourcePtr,
                Entrypoint = entrypointPtr,
                IncludeDir = IntPtr.Zero,
                Defines = IntPtr.Zero,
                ShaderStage = stage,
                Props = 0
            };

            nint resultPtr;
            nuint size;

            switch (target)
            {
                case ShaderFormatHelper.GraphicsShaderTarget.DXIL:
                    resultPtr = SDL3.ShaderCross.CompileDXILFromHLSL(ref hlslInfo, out size);
                    break;
                case ShaderFormatHelper.GraphicsShaderTarget.DXBC:
                    resultPtr = SDL3.ShaderCross.CompileDXBCFromHLSL(ref hlslInfo, out size);
                    break;
                default:
                    resultPtr = SDL3.ShaderCross.CompileSPIRVFromHLSL(ref hlslInfo, out size);
                    break;
            }

            if (resultPtr == IntPtr.Zero || size == 0)
                throw new InvalidOperationException($"ShaderCross failed to compile blur {stage} shader for {target}");

            var bytes = new byte[size];
            System.Runtime.InteropServices.Marshal.Copy(resultPtr, bytes, 0, (int)size);
            SDL3.SDL.Free(resultPtr);
            return bytes;
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(sourcePtr);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(entrypointPtr);
        }
    }

    private const string BlurVertexShaderHLSL = @"
struct VSOutput
{
    float4 position : SV_Position;
    float2 texCoord : TEXCOORD0;
};

VSOutput main(uint vertexID : SV_VertexID)
{
    VSOutput output;
    float x = (vertexID == 2) ? 3.0 : -1.0;
    float y = (vertexID == 1) ? -3.0 : 1.0;
    output.position = float4(x, y, 0.0, 1.0);
    float u = (vertexID == 2) ? 2.0 : 0.0;
    float v = (vertexID == 1) ? 2.0 : 0.0;
    output.texCoord = float2(u, v);
    return output;
}";

    private const string BlurFragmentShaderHLSL = @"
Texture2D inputTexture : register(t0, space2);
SamplerState inputSampler : register(s0, space2);

struct BlurUniforms
{
    float2 direction;
    float blurRadius;
    float _padding;
};

[[vk::binding(0, 3)]]
ConstantBuffer<BlurUniforms> uniforms : register(b0, space3);

struct PSInput
{
    float4 position : SV_Position;
    float2 texCoord : TEXCOORD0;
};

float4 main(PSInput input) : SV_Target
{
    float2 texelSize;
    inputTexture.GetDimensions(texelSize.x, texelSize.y);
    texelSize = 1.0 / texelSize;
    float2 offset = uniforms.direction * texelSize * uniforms.blurRadius;
    float4 color = float4(0, 0, 0, 0);
    color += inputTexture.Sample(inputSampler, input.texCoord - offset * 2.0) * 0.0545;
    color += inputTexture.Sample(inputSampler, input.texCoord - offset * 1.0) * 0.2442;
    color += inputTexture.Sample(inputSampler, input.texCoord) * 0.4026;
    color += inputTexture.Sample(inputSampler, input.texCoord + offset * 1.0) * 0.2442;
    color += inputTexture.Sample(inputSampler, input.texCoord + offset * 2.0) * 0.0545;
    return color;
}";

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