using Brine2D.Rendering;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// Service for loading and compiling shaders using ShaderCross.
/// ShaderCross supports HLSL source or SPIRV bytecode as input.
/// </summary>
public interface IShaderLoader
{
    /// <summary>
    /// Loads a shader from source code (HLSL) or bytecode (SPIRV).
    /// ShaderCross will automatically transpile to the correct format for the current GPU backend.
    /// </summary>
    IShader LoadFromSource(ShaderSource source);

    /// <summary>
    /// Loads a shader from pre-compiled bytecode.
    /// </summary>
    IShader LoadFromBytecode(string name, ShaderStage stage, byte[] bytecode, string entryPoint = "main");

    /// <summary>
    /// Loads a shader from a file (.hlsl or .spv).
    /// </summary>
    Task<IShader> LoadFromFileAsync(string path, ShaderStage stage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the default shader pipeline for basic rendering.
    /// </summary>
    (IShader vertex, IShader fragment) CreateDefaultShaders();
}