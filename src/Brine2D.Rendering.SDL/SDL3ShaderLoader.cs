using Brine2D.Rendering;
using Brine2D.Rendering.SDL.Shaders;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// SDL3 shader loader using ShaderCross for cross-platform compilation.
/// ShaderCross accepts HLSL or SPIRV as input.
/// </summary>
public class SDL3ShaderLoader : IShaderLoader, IDisposable
{
    private readonly ILogger<SDL3ShaderLoader> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly nint _device;
    private static bool _shaderCrossInitialized = false;
    private static readonly object _initLock = new object();

    public SDL3ShaderLoader(ILogger<SDL3ShaderLoader> logger, ILoggerFactory loggerFactory, nint device)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _device = device;

        // Initialize ShaderCross (only once)
        lock (_initLock)
        {
            if (!_shaderCrossInitialized)
            {
                if (!SDL3.ShaderCross.Init())
                {
                    throw new InvalidOperationException("Failed to initialize SDL_ShaderCross");
                }
                _shaderCrossInitialized = true;
                _logger.LogInformation("SDL_ShaderCross initialized");
            }
        }
    }

    public IShader LoadFromSource(ShaderSource source)
    {
        _logger.LogDebug("Compiling shader using ShaderCross: {Stage} from {Language}",
            source.Stage, source.Language);

        // Get the target format based on the GPU backend
        var driverName = SDL3.SDL.GetGPUDeviceDriver(_device);
        var targetFormat = GetTargetFormat(driverName);

        _logger.LogInformation("Transpiling {Stage} shader for {Driver} backend (target: {Target})",
            source.Stage, driverName, targetFormat);

        byte[]? bytecode = null;
        bool success = false;

        // ShaderCross has different compilation paths for HLSL vs SPIRV
        if (source.Language == ShaderLanguage.HLSL)
        {
            success = CompileFromHLSL(source, targetFormat, out bytecode);
        }
        else if (source.Language == ShaderLanguage.SPIRV)
        {
            success = CompileFromSPIRV(source, targetFormat, out bytecode);
        }
        else
        {
            throw new NotSupportedException($"Unsupported shader language: {source.Language}");
        }

        if (!success || bytecode == null)
        {
            var errorMsg = $"Shader compilation failed for {source.Language} to {targetFormat}. " +
                          $"Driver: {driverName}, Stage: {source.Stage}";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        _logger.LogInformation("Shader compiled successfully for {Driver} backend ({Size} bytes)",
            driverName, bytecode.Length);

        // Pass the correct format to the internal helper
        var shaderFormat = GetShaderFormat(targetFormat);
        return LoadFromBytecodeInternal($"{source.Stage}Shader", source.Stage, bytecode, source.EntryPoint, shaderFormat);
    }

    private bool CompileFromHLSL(ShaderSource source, GraphicsShaderTarget target, out byte[]? bytecode)
    {
        bytecode = null;

        _logger.LogDebug("Compiling HLSL to {Target}", target);
        _logger.LogDebug("Shader source length: {Length} bytes", source.Code.Length);
        _logger.LogDebug("Entry point: {EntryPoint}", source.EntryPoint);

        // Decode and log the actual shader source for debugging
        var shaderSourceText = System.Text.Encoding.UTF8.GetString(source.Code);
        _logger.LogDebug("=== SHADER SOURCE ===");
        foreach (var line in shaderSourceText.Split('\n'))
        {
            _logger.LogDebug(line.TrimEnd());
        }
        _logger.LogDebug("=== END SHADER SOURCE ===");

        // Create HLSL info structure
        var hlslInfo = new SDL3.ShaderCross.HLSLInfo
        {
            Source = shaderSourceText,
            Entrypoint = source.EntryPoint,
            IncludeDir = null,
            Defines = IntPtr.Zero,
            ShaderStage = MapToShaderCrossStage(source.Stage),
            Props = 0
        };

        _logger.LogDebug("HLSLInfo created with ShaderStage: {Stage}", hlslInfo.ShaderStage);

        try
        {
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

            if (!success)
            {
                _logger.LogError("ShaderCross compilation returned false for {Target}", target);
                _logger.LogError("This usually means:");
                _logger.LogError("  - Shader syntax error (but ShaderCross doesn't provide error details)");
                _logger.LogError("  - Missing shader compiler DLL (glslang, dxcompiler, etc.)");
                _logger.LogError("  - Unsupported HLSL feature for the target");
                return false;
            }

            if (resultPtr == IntPtr.Zero)
            {
                _logger.LogError("ShaderCross returned null pointer for {Target}", target);
                return false;
            }

            if (size == 0)
            {
                _logger.LogError("ShaderCross returned zero size for {Target}", target);
                return false;
            }

            bytecode = new byte[size];
            Marshal.Copy(resultPtr, bytecode, 0, (int)size);
            SDL3.SDL.Free(resultPtr);
            
            _logger.LogDebug("Successfully compiled to {Target}, output size: {Size} bytes", target, size);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during HLSL shader compilation to {Target}", target);
            return false;
        }
    }

    private bool CompileSPIRVFromHLSL(ref SDL3.ShaderCross.HLSLInfo hlslInfo, out nint result, out nuint size)
    {
        _logger.LogDebug("Calling SDL_ShaderCross_CompileSPIRVFromHLSL");
        _logger.LogDebug("  ShaderStage: {Stage}", hlslInfo.ShaderStage);
        _logger.LogDebug("  Entrypoint: {Entry}", hlslInfo.Entrypoint);
        _logger.LogDebug("  Source length: {Length}", hlslInfo.Source?.Length ?? 0);
        
        result = SDL3.ShaderCross.CompileSPIRVFromHLSL(ref hlslInfo, out size);
        var success = result != IntPtr.Zero;
        
        _logger.LogDebug("SPIRV compilation {Result}, size: {Size}", success ? "succeeded" : "failed", size);
        
        if (!success)
        {
            _logger.LogError("SPIRV compilation failed. Possible causes:");
            _logger.LogError("  1. HLSL syntax not compatible with SPIRV compiler");
            _logger.LogError("  2. Missing glslang or SPIRV-Tools DLL");
            _logger.LogError("  3. Shader model version issue");
            _logger.LogError("  Suggestion: Try using pre-compiled SPIRV shaders instead of HLSL");
        }
        
        return success;
    }

    private bool CompileDXBCFromHLSL(ref SDL3.ShaderCross.HLSLInfo hlslInfo, out nint result, out nuint size)
    {
        _logger.LogDebug("Calling SDL_ShaderCross_CompileDXBCFromHLSL");
        result = SDL3.ShaderCross.CompileDXBCFromHLSL(ref hlslInfo, out size);
        var success = result != IntPtr.Zero;
        _logger.LogDebug("DXBC compilation {Result}, size: {Size}", success ? "succeeded" : "failed", size);
        return success;
    }

    private bool CompileDXILFromHLSL(ref SDL3.ShaderCross.HLSLInfo hlslInfo, out nint result, out nuint size)
    {
        _logger.LogDebug("Calling SDL_ShaderCross_CompileDXILFromHLSL");
        result = SDL3.ShaderCross.CompileDXILFromHLSL(ref hlslInfo, out size);
        var success = result != IntPtr.Zero;
        
        if (!success)
        {
            _logger.LogError("DXIL compilation failed. This may indicate:");
            _logger.LogError("  - Missing dxcompiler.dll or dxil.dll in your application directory");
            _logger.LogError("  - Invalid HLSL syntax for DXIL target");
            _logger.LogError("  - Shader model not supported");
            _logger.LogError("Consider falling back to DXBC (Shader Model 5.1) or SPIRV");
        }
        else
        {
            _logger.LogDebug("DXIL compilation succeeded, size: {Size}", size);
        }
        
        return success;
    }

    private bool CompileHLSLToMSL(ShaderSource source, out nint result, out nuint size)
    {
        _logger.LogDebug("Compiling HLSL -> SPIRV -> MSL");

        var hlslInfo = new SDL3.ShaderCross.HLSLInfo
        {
            Source = System.Text.Encoding.UTF8.GetString(source.Code),
            Entrypoint = source.EntryPoint,
            IncludeDir = null,
            Defines = IntPtr.Zero,
            ShaderStage = MapToShaderCrossStage(source.Stage),
            Props = 0
        };

        // First compile to SPIRV
        var spirvPtr = SDL3.ShaderCross.CompileSPIRVFromHLSL(ref hlslInfo, out var spirvSize);
        if (spirvPtr == IntPtr.Zero)
        {
            _logger.LogError("Failed to compile HLSL to SPIRV (intermediate step for MSL)");
            result = IntPtr.Zero;
            size = 0;
            return false;
        }

        try
        {
            // Create SPIRV info structure
            var spirvInfo = new SDL3.ShaderCross.SPIRVInfo
            {
                ByteCode = spirvPtr,
                ByteCodeSize = spirvSize,
                Entrypoint = hlslInfo.Entrypoint,
                ShaderStage = MapToShaderCrossStage(source.Stage),
                Props = 0
            };

            // Transpile SPIRV to MSL (returns string)
            result = SDL3.ShaderCross.TranspileMSLFromSPIRV(ref spirvInfo);
            if (result != IntPtr.Zero)
            {
                // MSL is returned as a UTF-8 string, get its length
                var mslString = Marshal.PtrToStringUTF8(result);
                size = mslString != null ? (nuint)System.Text.Encoding.UTF8.GetByteCount(mslString) : 0;
                _logger.LogDebug("MSL transpilation succeeded, size: {Size}", size);
                return true;
            }

            _logger.LogError("Failed to transpile SPIRV to MSL");
            size = 0;
            return false;
        }
        finally
        {
            SDL3.SDL.Free(spirvPtr);
        }
    }

    private bool CompileFromSPIRV(ShaderSource source, GraphicsShaderTarget target, out byte[]? bytecode)
    {
        bytecode = null;

        // Pin the SPIRV bytecode
        var handle = GCHandle.Alloc(source.Code, GCHandleType.Pinned);
        try
        {
            var spirvInfo = new SDL3.ShaderCross.SPIRVInfo
            {
                ByteCode = handle.AddrOfPinnedObject(),
                ByteCodeSize = (nuint)source.Code.Length,
                Entrypoint = source.EntryPoint,
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

    private IShader LoadFromBytecodeInternal(string name, ShaderStage stage, byte[] bytecode, string entryPoint, SDL3.SDL.GPUShaderFormat format)
    {
        _logger.LogDebug("Loading shader {Name} from bytecode (format: {Format})", name, format);

        var shader = new SDL3Shader(name, stage, _loggerFactory.CreateLogger<SDL3Shader>());

        if (!shader.Compile(_device, bytecode, entryPoint, format))
        {
            throw new InvalidOperationException($"Failed to load shader: {name}");
        }

        return shader;
    }

    public IShader LoadFromBytecode(string name, ShaderStage stage, byte[] bytecode, string entryPoint = "main")
    {
        _logger.LogDebug("Loading shader {Name} from bytecode (using default SPIRV format)", name);

        var shader = new SDL3Shader(name, stage, _loggerFactory.CreateLogger<SDL3Shader>());

        if (!shader.Compile(_device, bytecode, entryPoint, SDL3.SDL.GPUShaderFormat.SPIRV))
        {
            throw new InvalidOperationException($"Failed to load shader: {name}");
        }

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

    public async Task<IShader> LoadFromFileAsync(string path, ShaderStage stage, CancellationToken cancellationToken = default)
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

        ShaderSource source;

        if (language == ShaderLanguage.HLSL)
        {
            source = new ShaderSource
            {
                Code = bytecode,
                Language = language,
                Stage = stage
            };
        }
        else
        {
            source = ShaderSource.FromSPIRV(bytecode, stage);
        }

        return LoadFromSource(source);
    }

    /// <summary>
    /// Checks if runtime shader compilation is available (ShaderCross DLLs present).
    /// </summary>
    private bool IsRuntimeCompilationAvailable()
    {
        // ShaderCross initialization already happened in constructor
        // If we got here, it's available
        return _shaderCrossInitialized;
    }

    public (IShader vertex, IShader fragment) CreateDefaultShaders()
    {
        _logger.LogInformation("Creating default shaders");

        // Determine the correct shader format based on the GPU backend
        var driverName = SDL3.SDL.GetGPUDeviceDriver(_device);
        var targetFormat = GetTargetFormat(driverName);
        var shaderFormat = GetShaderFormat(targetFormat);

        // Strategy: Try pre-compiled first (always reliable), fallback to runtime compilation
        try
        {
            _logger.LogInformation("Loading pre-compiled {Format} shaders from embedded resources", targetFormat);
            
            byte[]? vertexBytecode = null;
            byte[]? fragmentBytecode = null;

            // Load the correct pre-compiled shader format based on the backend
            switch (targetFormat)
            {
                case GraphicsShaderTarget.SPIRV:
                    vertexBytecode = DefaultShaders.LoadVertexShaderSPIRV();
                    fragmentBytecode = DefaultShaders.LoadFragmentShaderSPIRV();
                    break;
                    
                case GraphicsShaderTarget.DXIL:
                    vertexBytecode = DefaultShaders.LoadVertexShaderDXIL();
                    fragmentBytecode = DefaultShaders.LoadFragmentShaderDXIL();
                    break;
                    
                case GraphicsShaderTarget.DXBC:
                    vertexBytecode = DefaultShaders.LoadVertexShaderDXBC();
                    fragmentBytecode = DefaultShaders.LoadFragmentShaderDXBC();
                    break;
                    
                case GraphicsShaderTarget.MSL:
                    vertexBytecode = DefaultShaders.LoadVertexShaderMSL();
                    fragmentBytecode = DefaultShaders.LoadFragmentShaderMSL();
                    break;
            }

            if (vertexBytecode == null || fragmentBytecode == null)
            {
                throw new InvalidOperationException(
                    $"Pre-compiled {targetFormat} shader resources not found for {driverName} backend. " +
                    $"Expected embedded resources not available.");
            }

            var vertexShader = LoadFromBytecodeInternal(
                "VertexShader", 
                ShaderStage.Vertex, 
                vertexBytecode, 
                "main",
                shaderFormat);
            
            var fragmentShader = LoadFromBytecodeInternal(
                "FragmentShader", 
                ShaderStage.Fragment, 
                fragmentBytecode, 
                "main",
                shaderFormat);

            _logger.LogInformation("Successfully loaded pre-compiled {Format} shaders ({VertexSize} + {FragmentSize} bytes)",
                targetFormat, vertexBytecode.Length, fragmentBytecode.Length);
            
            return (vertexShader, fragmentShader);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load pre-compiled shaders, attempting runtime compilation");
            
            // Fallback to runtime compilation if available
            if (IsRuntimeCompilationAvailable())
            {
                _logger.LogInformation("Attempting runtime shader compilation from HLSL");
                
                try
                {
                    var vertexSource = ShaderSource.FromHLSL(
                        DefaultShaders.SimpleVertexShaderHLSL,
                        ShaderStage.Vertex,
                        "main"
                    );

                    var fragmentSource = ShaderSource.FromHLSL(
                        DefaultShaders.SimpleFragmentShaderHLSL,
                        ShaderStage.Fragment,
                        "main"
                    );

                    return (LoadFromSource(vertexSource), LoadFromSource(fragmentSource));
                }
                catch (Exception compileEx)
                {
                    _logger.LogError(compileEx, "Runtime shader compilation also failed");
                    throw new InvalidOperationException(
                        "Failed to load shaders. Both pre-compiled and runtime compilation failed. " +
                        "Ensure ShaderCross compiler DLLs (glslang.dll, SPIRV-Tools.dll) are present for runtime compilation.",
                        compileEx);
                }
            }
            
            throw new InvalidOperationException(
                "No shaders available. Pre-compiled shaders failed to load and runtime compilation is unavailable. " +
                "This may indicate missing embedded resources or a GPU driver issue.", 
                ex);
        }
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
        var target = driverName switch
        {
            "vulkan" => GraphicsShaderTarget.SPIRV,
            "metal" => GraphicsShaderTarget.MSL,
            "direct3d12" => GraphicsShaderTarget.DXIL,
            "direct3d11" => GraphicsShaderTarget.DXBC,
            _ => GraphicsShaderTarget.SPIRV
        };

        _logger.LogInformation("Selected shader target {Target} for driver {Driver}", target, driverName);
        return target;
    }

    public void Dispose()
    {
        // SDL_ShaderCross_Quit should be called during application shutdown
        // Not here, as this is per-loader instance
    }
}