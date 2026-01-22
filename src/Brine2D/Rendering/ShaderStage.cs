namespace Brine2D.Rendering;

/// <summary>
/// Shader stage types in the graphics pipeline.
/// </summary>
public enum ShaderStage
{
    /// <summary>
    /// Vertex shader stage.
    /// Processes each vertex and transforms positions.
    /// </summary>
    Vertex,

    /// <summary>
    /// Fragment/Pixel shader stage.
    /// Processes each pixel and determines final color.
    /// </summary>
    Fragment
}