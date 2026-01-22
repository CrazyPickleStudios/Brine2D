namespace Brine2D.Rendering;

/// <summary>
/// Supported shader source languages.
/// </summary>
public enum ShaderLanguage
{
    /// <summary>
    /// High-Level Shading Language (Direct3D).
    /// ShaderCross will transpile this to the target format.
    /// </summary>
    HLSL,

    /// <summary>
    /// SPIR-V bytecode (pre-compiled).
    /// ShaderCross can use this directly or transpile to other formats.
    /// </summary>
    SPIRV
}