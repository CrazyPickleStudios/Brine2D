using Brine2D.Rendering;
using Brine2D.Rendering.SDL.Shaders.PostProcessing;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering.SDL.PostProcessing.Effects;

/// <summary>
/// Post-processing effect that converts the image to grayscale.
/// Uses pre-compiled SPIRV/DXIL shaders embedded as resources.
/// Luminance calculation: 0.299*R + 0.587*G + 0.114*B
/// </summary>
public class GrayscaleEffect : ISDL3PostProcessEffect, IDisposable
{
    private readonly ILogger<GrayscaleEffect>? _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly GpuDeviceHandle _deviceHandle;
    private readonly SDL3.SDL.GPUTextureFormat _colorTargetFormat;
    private int _width;
    private int _height;
    
    private SDL3Shader? _vertexShader;
    private SDL3Shader? _fragmentShader;
    private nint _pipeline;
    private nint _sampler;
    private int _disposed;
    private bool _initialized;

    public int Order { get; set; } = 0;
    public string Name => "Grayscale";
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Grayscale intensity (0.0 = original color, 1.0 = full grayscale).
    /// Requires the grayscale fragment shader to declare a uniform buffer at slot 0.
    /// </summary>
    public float Intensity { get; set; } = 1.0f;

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct GrayscaleParams
    {
        public float Intensity;   // 4 bytes
        public float Padding1;    // 4 bytes
        public float Padding2;    // 4 bytes
        public float Padding3;    // 4 bytes (16 bytes total)
    }

    internal GrayscaleEffect(
        GpuDeviceHandle deviceHandle,
        int width,
        int height,
        SDL3.SDL.GPUTextureFormat colorTargetFormat,
        ILoggerFactory loggerFactory,
        ILogger<GrayscaleEffect>? logger = null)
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

        _logger?.LogInformation("Initializing Grayscale effect...");

        try
        {
            CreateSampler();
            CreateShaders();
            CreatePipeline();

            _initialized = true;
            _logger?.LogInformation("Grayscale effect ready");
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
            var error = SDL3.SDL.GetError();
            _logger?.LogError("Failed to create sampler: {Error}", error);
            throw new InvalidOperationException($"Failed to create sampler for grayscale effect: {error}");
        }

        _logger?.LogDebug("Grayscale sampler created");
    }

    private void CreateShaders()
    {
        var device = _deviceHandle.Handle;

        try
        {
            _logger?.LogInformation("Loading pre-compiled grayscale shaders...");
            
            var driverName = SDL3.SDL.GetGPUDeviceDriver(device);
            var targetFormat = ShaderFormatHelper.GetTargetFromDriver(driverName);
            var shaderFormat = ShaderFormatHelper.GetShaderFormat(targetFormat);

            byte[]? vertexBytecode = null;
            byte[]? fragmentBytecode = null;

            switch (targetFormat)
            {
                case ShaderFormatHelper.GraphicsShaderTarget.SPIRV:
                    vertexBytecode = GrayscaleShaders.LoadVertexShaderSPIRV();
                    fragmentBytecode = GrayscaleShaders.LoadFragmentShaderSPIRV();
                    break;

                case ShaderFormatHelper.GraphicsShaderTarget.DXIL:
                    vertexBytecode = GrayscaleShaders.LoadVertexShaderDXIL();
                    fragmentBytecode = GrayscaleShaders.LoadFragmentShaderDXIL();
                    break;

                case ShaderFormatHelper.GraphicsShaderTarget.DXBC:
                    vertexBytecode = GrayscaleShaders.LoadVertexShaderDXBC();
                    fragmentBytecode = GrayscaleShaders.LoadFragmentShaderDXBC();
                    break;

                case ShaderFormatHelper.GraphicsShaderTarget.MSL:
                    vertexBytecode = GrayscaleShaders.LoadVertexShaderMSL();
                    fragmentBytecode = GrayscaleShaders.LoadFragmentShaderMSL();
                    break;
            }

            if (vertexBytecode == null || fragmentBytecode == null)
            {
                throw new InvalidOperationException(
                    $"Pre-compiled {targetFormat} grayscale shader resources not found for {SDL3.SDL.GetGPUDeviceDriver(device)} backend. " +
                    $"Shaders may not have been compiled at build time.");
            }

            _vertexShader = CreateShaderFromBytecode(ShaderStage.Vertex, vertexBytecode, "main", shaderFormat);
            _fragmentShader = CreateShaderFromBytecode(ShaderStage.Fragment, fragmentBytecode, "main", shaderFormat);

            if (_vertexShader.Handle == nint.Zero || _fragmentShader.Handle == nint.Zero)
            {
                throw new InvalidOperationException("Failed to create grayscale shaders from bytecode");
            }

            _logger?.LogInformation("Grayscale shaders loaded successfully ({VertexSize} + {FragmentSize} bytes)",
                vertexBytecode.Length, fragmentBytecode.Length);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load grayscale shaders");
            throw;
        }
    }

    private SDL3Shader CreateShaderFromBytecode(ShaderStage stage, byte[] bytecode, string entryPoint, SDL3.SDL.GPUShaderFormat format)
    {
        var shader = new SDL3Shader($"grayscale_{stage}", stage, _loggerFactory.CreateLogger<SDL3Shader>());

        uint numUniformBuffers = stage == ShaderStage.Fragment ? 1u : 0u;

        if (!shader.Compile(_deviceHandle.Handle, bytecode, entryPoint, format, numUniformBuffers))
        {
            shader.Dispose();
            throw new InvalidOperationException($"Failed to compile grayscale {stage} shader");
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
                var error = SDL3.SDL.GetError();
                _logger?.LogError("Failed to create pipeline: {Error}", error);
                throw new InvalidOperationException($"Failed to create graphics pipeline: {error}");
            }

            _logger?.LogDebug("Grayscale pipeline created (format: {Format})", _colorTargetFormat);
        }
        finally
        {
            colorTargetHandle.Free();
        }
    }

    public void SetDimensions(int width, int height)
    {
        _width = width;
        _height = height;
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

        try
        {
            var grayscaleParams = new GrayscaleParams
            {
                Intensity = Intensity
            };

            FullScreenQuad.RenderWithShaderAndUniforms(
                commandBuffer,
                sourceTexture,
                targetTexture,
                _pipeline,
                _sampler,
                grayscaleParams,
                _width,
                _height,
                _logger);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to apply grayscale effect");
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        var device = _deviceHandle.Handle;
        if (device == nint.Zero)
            return;

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

        _logger?.LogDebug("Grayscale effect disposed");
    }
}