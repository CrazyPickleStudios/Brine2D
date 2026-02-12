namespace Brine2D.Rendering;

/// <summary>
/// Platform-agnostic render target interface for off-screen rendering.
/// GPU renderer only - legacy renderer throws NotSupportedException.
/// </summary>
public interface IRenderTarget : IDisposable
{
    /// <summary>
    /// Width of the render target in pixels.
    /// </summary>
    int Width { get; }
    
    /// <summary>
    /// Height of the render target in pixels.
    /// </summary>
    int Height { get; }
    
    /// <summary>
    /// The underlying texture that can be sampled after rendering.
    /// </summary>
    ITexture Texture { get; }
}