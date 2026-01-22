using Brine2D.Rendering;
using Brine2D.Rendering.SDL.Shaders.PostProcessing;
using Brine2D.SDL.Rendering;
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
    private readonly nint _device;
    private int _width;
    private int _height;
    
    private nint _vertexShader;
    private nint _fragmentShader;
    private nint _pipeline;
    private nint _sampler;
    private bool _disposed;
    private bool _initialized;

    public int Order { get; set; } = 0;
    public string Name => "Grayscale";
    public bool Enabled { get; set; } = true;

    public float Intensity { get; set; } = 1.0f;

    public GrayscaleEffect(nint device, int width, int height, ILoggerFactory loggerFactory, ILogger<GrayscaleEffect>? logger = null)
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

        _logger?.LogInformation("Initializing Grayscale effect...");

        CreateSampler();
        CreateShaders();
        CreatePipeline();

        _initialized = true;
        _logger?.LogInformation("✓ Grayscale effect ready");
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
            var error = SDL3.SDL.GetError();
            _logger?.LogError("Failed to create sampler: {Error}", error);
            throw new InvalidOperationException($"Failed to create sampler for grayscale effect: {error}");
        }

        _logger?.LogDebug("Grayscale sampler created");
    }

    private void CreateShaders()
    {
        var shaderLoader = new SDL3ShaderLoader(
            _loggerFactory.CreateLogger<SDL3ShaderLoader>(),
            _loggerFactory,
            _device);

        try
        {
            _logger?.LogInformation("Loading pre-compiled grayscale shaders...");
            
            // Use the same pattern as CreateDefaultShaders
            var driverName = SDL3.SDL.GetGPUDeviceDriver(_device);
            var targetFormat = GetTargetFormatFromDriver(driverName);
            var shaderFormat = GetShaderFormat(targetFormat);

            byte[]? vertexBytecode = null;
            byte[]? fragmentBytecode = null;

            // Load the correct pre-compiled shader format based on the backend
            switch (targetFormat)
            {
                case GraphicsShaderTarget.SPIRV:
                    vertexBytecode = GrayscaleShaders.LoadVertexShaderSPIRV();
                    fragmentBytecode = GrayscaleShaders.LoadFragmentShaderSPIRV();
                    break;

                case GraphicsShaderTarget.DXIL:
                    vertexBytecode = GrayscaleShaders.LoadVertexShaderDXIL();
                    fragmentBytecode = GrayscaleShaders.LoadFragmentShaderDXIL();
                    break;

                case GraphicsShaderTarget.DXBC:
                    vertexBytecode = GrayscaleShaders.LoadVertexShaderDXBC();
                    fragmentBytecode = GrayscaleShaders.LoadFragmentShaderDXBC();
                    break;

                case GraphicsShaderTarget.MSL:
                    vertexBytecode = GrayscaleShaders.LoadVertexShaderMSL();
                    fragmentBytecode = GrayscaleShaders.LoadFragmentShaderMSL();
                    break;
            }

            if (vertexBytecode == null || fragmentBytecode == null)
            {
                throw new InvalidOperationException(
                    $"Pre-compiled {targetFormat} grayscale shader resources not found for {driverName} backend. " +
                    $"Shaders may not have been compiled at build time.");
            }

            // Create SDL3 shaders from bytecode
            var vertexShaderObj = CreateShaderFromBytecode(ShaderStage.Vertex, vertexBytecode, "main", shaderFormat);
            var fragmentShaderObj = CreateShaderFromBytecode(ShaderStage.Fragment, fragmentBytecode, "main", shaderFormat);

            _vertexShader = (vertexShaderObj as SDL3Shader)?.Handle ?? nint.Zero;
            _fragmentShader = (fragmentShaderObj as SDL3Shader)?.Handle ?? nint.Zero;

            if (_vertexShader == nint.Zero || _fragmentShader == nint.Zero)
            {
                throw new InvalidOperationException("Failed to create grayscale shaders from bytecode");
            }

            _logger?.LogInformation("✓ Grayscale shaders loaded successfully ({VertexSize} + {FragmentSize} bytes)",
                vertexBytecode.Length, fragmentBytecode.Length);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load grayscale shaders");
            throw;
        }
    }

    private GraphicsShaderTarget GetTargetFormatFromDriver(string driverName)
    {
        return driverName.ToLowerInvariant() switch
        {
            "vulkan" => GraphicsShaderTarget.SPIRV,
            "direct3d12" or "d3d12" => GraphicsShaderTarget.DXIL,
            "direct3d11" or "d3d11" => GraphicsShaderTarget.DXBC,
            "metal" => GraphicsShaderTarget.MSL,
            _ => GraphicsShaderTarget.SPIRV // Default to SPIRV
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

    private IShader CreateShaderFromBytecode(ShaderStage stage, byte[] bytecode, string entryPoint, SDL3.SDL.GPUShaderFormat format)
    {
        // Create shader object first
        var shader = new SDL3Shader($"grayscale_{stage}", stage, _loggerFactory.CreateLogger<SDL3Shader>());
        
        // Then compile it with the bytecode
        if (!shader.Compile(_device, bytecode, entryPoint, format))
        {
            throw new InvalidOperationException($"Failed to compile grayscale {stage} shader");
        }
        
        return shader;
    }

    private enum GraphicsShaderTarget
    {
        SPIRV,
        DXIL,
        DXBC,
        MSL
    }

    private void CreatePipeline()
    {
        var colorTargetDescriptions = new SDL3.SDL.GPUColorTargetDescription[]
        {
            new()
            {
                // CHANGED: Match the render target format (R8G8B8A8Unorm by default)
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
                var error = SDL3.SDL.GetError();
                _logger?.LogError("Failed to create pipeline: {Error}", error);
                throw new InvalidOperationException($"Failed to create graphics pipeline: {error}");
            }

            _logger?.LogDebug("Grayscale pipeline created");
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
        if (!Enabled) 
        {
            return; // Remove the log here too
        }

        EnsureInitialized();

        try
        {
            FullScreenQuad.RenderWithShader(
                commandBuffer,
                sourceTexture,
                targetTexture,
                _pipeline,
                _sampler,
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
        if (_disposed) return;

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
        _logger?.LogDebug("Grayscale effect disposed");
    }
}