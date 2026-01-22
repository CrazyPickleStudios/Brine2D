namespace Brine2D.Rendering;

/// <summary>
/// Represents shader source code.
/// </summary>
public class ShaderSource
{
    /// <summary>
    /// Gets the shader source code or bytecode.
    /// </summary>
    public required byte[] Code { get; init; }

    /// <summary>
    /// Gets the shader language/format.
    /// </summary>
    public required ShaderLanguage Language { get; init; }

    /// <summary>
    /// Gets the entry point function name.
    /// </summary>
    public string EntryPoint { get; init; } = "main";

    /// <summary>
    /// Gets the shader stage.
    /// </summary>
    public required ShaderStage Stage { get; init; }

    /// <summary>
    /// Creates a shader source from HLSL string.
    /// </summary>
    public static ShaderSource FromHLSL(string code, ShaderStage stage, string entryPoint = "main")
    {
        return new ShaderSource
        {
            Code = System.Text.Encoding.UTF8.GetBytes(code),
            Language = ShaderLanguage.HLSL,
            Stage = stage,
            EntryPoint = entryPoint
        };
    }

    /// <summary>
    /// Creates a shader source from SPIRV bytecode.
    /// </summary>
    public static ShaderSource FromSPIRV(byte[] bytecode, ShaderStage stage, string entryPoint = "main")
    {
        return new ShaderSource
        {
            Code = bytecode,
            Language = ShaderLanguage.SPIRV,
            Stage = stage,
            EntryPoint = entryPoint
        };
    }
}