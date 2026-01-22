using System.Drawing;

namespace Brine2D.Rendering;

/// <summary>
/// Represents a GPU shader program.
/// </summary>
public interface IShader : IDisposable
{
    /// <summary>
    /// Gets the name of the shader.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sets a uniform value in the shader.
    /// </summary>
    void SetUniform(string name, float value);

    /// <summary>
    /// Sets a uniform vector value in the shader.
    /// </summary>
    void SetUniform(string name, float x, float y);

    /// <summary>
    /// Sets a uniform color value in the shader.
    /// </summary>
    void SetUniform(string name, Color color);
}