using Brine2D.Rendering;
using Brine2D.Rendering.SDL.Shaders;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering;

/// <summary>
/// Compiles and loads HLSL shaders at runtime via SDL_ShaderCross.
/// ShaderCross transpiles HLSL to the format required by the active GPU backend
/// (SPIRV for Vulkan, DXIL for D3D12, DXBC for D3D11, MSL for Metal).
/// </summary>
public class SDL3ShaderLoader : IShaderLoader, IDisposable
{
    private readonly ILogger<SDL3ShaderLoader> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly nint _device;
    public SDL3ShaderLoader(ILogger<SDL3ShaderLoader> logger, ILoggerFactory loggerFactory, nint device)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _device = device;
    }

    public IShader LoadFromSource(ShaderSource source)
    {
        var driverName = SDL3.SDL.GetGPUDeviceDriver(_device);
        var targetFormat = GetTargetFormat(driverName);

        _logger.LogDebug("Compiling {Stage} shader for {Driver} backend (target: {Target})",
            source.Stage, driverName, targetFormat);

        byte[]? bytecode = null;
        bool success = source.Language switch
        {
            ShaderLanguage.HLSL => CompileFromHLSL(source, targetFormat, out bytecode),
            ShaderLanguage.SPIRV => CompileFromSPIRV(source, targetFormat, out bytecode),
            _ => throw new NotSupportedException($"Unsupported shader language: {source.Language}")
        };

        if (!success || bytecode == null)
            throw new InvalidOperationException(
                $"Shader compilation failed: {source.Language} \u2192 {targetFormat}, stage={source.Stage}, driver={driverName}");

        return LoadFromBytecodeInternal($"{source.Stage}Shader", source.Stage, bytecode,
            source.EntryPoint, GetShaderFormat(targetFormat));
    }

    private bool CompileFromHLSL(ShaderSource source, GraphicsShaderTarget target, out byte[]? bytecode)
    {
        bytecode = null;

        var shaderSourceText = System.Text.Encoding.UTF8.GetString(source.Code);
        var sourcePtr = Marshal.StringToCoTaskMemUTF8(shaderSourceText);
        var entrypointPtr = Marshal.StringToCoTaskMemUTF8(source.EntryPoint);
        try
        {
            var hlslInfo = new SDL3.ShaderCross.HLSLInfo
            {
                Source = sourcePtr,
                Entrypoint = entrypointPtr,
                IncludeDir = IntPtr.Zero,
                Defines = IntPtr.Zero,
                ShaderStage = MapToShaderCrossStage(source.Stage),
                Props = 0
            };

            nint resultPtr;
            nuint size;

            bool success = target switch
            {
                GraphicsShaderTarget.SPIRV => CompileSPIRVFromHLSL(ref hlslInfo, out resultPtr, out size),
                GraphicsShaderTarget.DXBC => CompileDXBCFromHLSL(ref hlslInfo, out resultPtr, out size),
                GraphicsShaderTarget.DXIL => CompileDXILFromHLSL(ref hlslInfo, out resultPtr, out size),
                GraphicsShaderTarget.MSL => CompileHLSLToMSL(source, out resultPtr, out size),
                _ => throw new NotSupportedException($"Cannot compile HLSL to {target}")
            };

            if (!success || resultPtr == IntPtr.Zero || size == 0)
            {
                _logger.LogError("ShaderCross HLSL compilation failed for {Target}", target);
                return false;
            }

            bytecode = new byte[size];
            Marshal.Copy(resultPtr, bytecode, 0, (int)size);
            SDL3.SDL.Free(resultPtr);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during HLSL compilation to {Target}", target);
            return false;
        }
        finally
        {
            Marshal.FreeCoTaskMem(sourcePtr);
            Marshal.FreeCoTaskMem(entrypointPtr);
        }
    }

    private bool CompileSPIRVFromHLSL(ref SDL3.ShaderCross.HLSLInfo hlslInfo, out nint result, out nuint size)
    {
        result = SDL3.ShaderCross.CompileSPIRVFromHLSL(ref hlslInfo, out size);
        if (result == IntPtr.Zero)
            _logger.LogError("SPIRV compilation failed for stage {Stage}", hlslInfo.ShaderStage);
        return result != IntPtr.Zero;
    }

    private bool CompileDXBCFromHLSL(ref SDL3.ShaderCross.HLSLInfo hlslInfo, out nint result, out nuint size)
    {
        result = SDL3.ShaderCross.CompileDXBCFromHLSL(ref hlslInfo, out size);
        if (result == IntPtr.Zero)
            _logger.LogError("DXBC compilation failed for stage {Stage}", hlslInfo.ShaderStage);
        return result != IntPtr.Zero;
    }

    private bool CompileDXILFromHLSL(ref SDL3.ShaderCross.HLSLInfo hlslInfo, out nint result, out nuint size)
    {
        result = SDL3.ShaderCross.CompileDXILFromHLSL(ref hlslInfo, out size);
        if (result == IntPtr.Zero)
            _logger.LogError("DXIL compilation failed for stage {Stage}", hlslInfo.ShaderStage);
        return result != IntPtr.Zero;
    }

    private bool CompileHLSLToMSL(ShaderSource source, out nint result, out nuint size)
    {
        var sourcePtr = Marshal.StringToCoTaskMemUTF8(System.Text.Encoding.UTF8.GetString(source.Code));
        var entrypointPtr = Marshal.StringToCoTaskMemUTF8(source.EntryPoint);
        try
        {
            var hlslInfo = new SDL3.ShaderCross.HLSLInfo
            {
                Source = sourcePtr,
                Entrypoint = entrypointPtr,
                IncludeDir = IntPtr.Zero,
                Defines = IntPtr.Zero,
                ShaderStage = MapToShaderCrossStage(source.Stage),
                Props = 0
            };

            var spirvPtr = SDL3.ShaderCross.CompileSPIRVFromHLSL(ref hlslInfo, out var spirvSize);
            if (spirvPtr == IntPtr.Zero)
            {
                _logger.LogError("HLSL → SPIRV (intermediate for MSL) failed");
                result = IntPtr.Zero;
                size = 0;
                return false;
            }

            try
            {
                var spirvInfo = new SDL3.ShaderCross.SPIRVInfo
                {
                    ByteCode = spirvPtr,
                    ByteCodeSize = spirvSize,
                    Entrypoint = entrypointPtr,
                    ShaderStage = MapToShaderCrossStage(source.Stage),
                    Props = 0
                };

                result = SDL3.ShaderCross.TranspileMSLFromSPIRV(ref spirvInfo);
                if (result != IntPtr.Zero)
                {
                    var mslString = Marshal.PtrToStringUTF8(result);
                    size = mslString != null ? (nuint)System.Text.Encoding.UTF8.GetByteCount(mslString) : 0;
                    return true;
                }

                _logger.LogError("SPIRV → MSL transpilation failed");
                size = 0;
                return false;
            }
            finally
            {
                SDL3.SDL.Free(spirvPtr);
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(sourcePtr);
            Marshal.FreeCoTaskMem(entrypointPtr);
        }
    }

    private bool CompileFromSPIRV(ShaderSource source, GraphicsShaderTarget target, out byte[]? bytecode)
    {
        bytecode = null;

        var handle = GCHandle.Alloc(source.Code, GCHandleType.Pinned);
        var entrypointPtr = Marshal.StringToCoTaskMemUTF8(source.EntryPoint);
        try
        {
            var spirvInfo = new SDL3.ShaderCross.SPIRVInfo
            {
                ByteCode = handle.AddrOfPinnedObject(),
                ByteCodeSize = (nuint)source.Code.Length,
                Entrypoint = entrypointPtr,
                ShaderStage = MapToShaderCrossStage(source.Stage),
                Props = 0
            };

            nint resultPtr = default;
            nuint size = 0;

            bool success = target switch
            {
                GraphicsShaderTarget.SPIRV => SetBytecode(source.Code, out bytecode),
                GraphicsShaderTarget.MSL => TranspileMSLFromSPIRV(ref spirvInfo, out resultPtr, out size),
                GraphicsShaderTarget.DXBC => CompileDXBCFromSPIRV(ref spirvInfo, out resultPtr, out size),
                GraphicsShaderTarget.DXIL => CompileDXILFromSPIRV(ref spirvInfo, out resultPtr, out size),
                _ => throw new NotSupportedException($"Cannot compile SPIRV to {target}")
            };

            if (success && target != GraphicsShaderTarget.SPIRV && resultPtr != IntPtr.Zero && size > 0)
            {
                bytecode = new byte[size];
                Marshal.Copy(resultPtr, bytecode, 0, (int)size);
                SDL3.SDL.Free(resultPtr);
                return true;
            }

            return success;
        }
        finally
        {
            handle.Free();
            Marshal.FreeCoTaskMem(entrypointPtr);
        }
    }

    private bool TranspileMSLFromSPIRV(ref SDL3.ShaderCross.SPIRVInfo spirvInfo, out nint result, out nuint size)
    {
        result = SDL3.ShaderCross.TranspileMSLFromSPIRV(ref spirvInfo);
        if (result != IntPtr.Zero)
        {
            var mslString = Marshal.PtrToStringUTF8(result);
            size = mslString != null ? (nuint)System.Text.Encoding.UTF8.GetByteCount(mslString) : 0;
            return true;
        }
        size = 0;
        return false;
    }

    private bool CompileDXBCFromSPIRV(ref SDL3.ShaderCross.SPIRVInfo spirvInfo, out nint result, out nuint size)
    {
        result = SDL3.ShaderCross.CompileDXBCFromSPIRV(ref spirvInfo, out size);
        return result != IntPtr.Zero;
    }

    private bool CompileDXILFromSPIRV(ref SDL3.ShaderCross.SPIRVInfo spirvInfo, out nint result, out nuint size)
    {
        result = SDL3.ShaderCross.CompileDXILFromSPIRV(ref spirvInfo, out size);
        return result != IntPtr.Zero;
    }

    private static bool SetBytecode(byte[] input, out byte[]? output)
    {
        output = input;
        return true;
    }

    private IShader LoadFromBytecodeInternal(string name, ShaderStage stage, byte[] bytecode,
        string entryPoint, SDL3.SDL.GPUShaderFormat format)
    {
        var shader = new SDL3Shader(name, stage, _loggerFactory.CreateLogger<SDL3Shader>());

        if (!shader.Compile(_device, bytecode, entryPoint, format))
            throw new InvalidOperationException($"Failed to load shader: {name}");

        return shader;
    }

    public IShader LoadFromBytecode(string name, ShaderStage stage, byte[] bytecode, string entryPoint = "main")
    {
        var shader = new SDL3Shader(name, stage, _loggerFactory.CreateLogger<SDL3Shader>());

        if (!shader.Compile(_device, bytecode, entryPoint, SDL3.SDL.GPUShaderFormat.SPIRV))
            throw new InvalidOperationException($"Failed to load shader: {name}");

        return shader;
    }

    private SDL3.SDL.GPUShaderFormat GetShaderFormat(GraphicsShaderTarget target)
    {
        return target switch
        {
            GraphicsShaderTarget.SPIRV => SDL3.SDL.GPUShaderFormat.SPIRV,
            GraphicsShaderTarget.MSL => SDL3.SDL.GPUShaderFormat.MSL,
            GraphicsShaderTarget.DXBC => SDL3.SDL.GPUShaderFormat.DXBC,
            GraphicsShaderTarget.DXIL => SDL3.SDL.GPUShaderFormat.DXIL,
            _ => SDL3.SDL.GPUShaderFormat.SPIRV
        };
    }

    public async Task<IShader> LoadFromFileAsync(string path, ShaderStage stage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading shader from file: {Path}", path);

        var bytecode = await File.ReadAllBytesAsync(path, cancellationToken);
        var extension = Path.GetExtension(path).ToLowerInvariant();

        var language = extension switch
        {
            ".hlsl" => ShaderLanguage.HLSL,
            ".spv" or ".spirv" => ShaderLanguage.SPIRV,
            _ => throw new NotSupportedException($"Unsupported shader file extension: {extension}. Use .hlsl or .spv")
        };

        var source = language == ShaderLanguage.HLSL
            ? new ShaderSource { Code = bytecode, Language = language, Stage = stage }
            : ShaderSource.FromSPIRV(bytecode, stage);

        return LoadFromSource(source);
    }

    public (IShader vertex, IShader fragment) CreateDefaultShaders()
    {
        _logger.LogInformation("Creating default shaders via ShaderCross");

        var vertexSource = ShaderSource.FromHLSL(DefaultShaders.SimpleVertexShaderHLSL, ShaderStage.Vertex, "main");
        var fragmentSource = ShaderSource.FromHLSL(DefaultShaders.SimpleFragmentShaderHLSL, ShaderStage.Fragment, "main");

        var vertex = LoadFromSource(vertexSource);
        var fragment = LoadFromSource(fragmentSource);

        _logger.LogInformation("Default shaders compiled via ShaderCross");
        return (vertex, fragment);
    }

    private static SDL3.ShaderCross.ShaderStage MapToShaderCrossStage(ShaderStage stage)
    {
        return stage switch
        {
            ShaderStage.Vertex => SDL3.ShaderCross.ShaderStage.Vertex,
            ShaderStage.Fragment => SDL3.ShaderCross.ShaderStage.Fragment,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported shader stage")
        };
    }

    private enum GraphicsShaderTarget
    {
        SPIRV,
        MSL,
        DXBC,
        DXIL
    }

    private GraphicsShaderTarget GetTargetFormat(string driverName)
    {
        return driverName switch
        {
            "vulkan" => GraphicsShaderTarget.SPIRV,
            "metal" => GraphicsShaderTarget.MSL,
            "direct3d12" => GraphicsShaderTarget.DXIL,
            "direct3d11" => GraphicsShaderTarget.DXBC,
            _ => GraphicsShaderTarget.SPIRV
        };
    }

    public void Dispose() { }
}