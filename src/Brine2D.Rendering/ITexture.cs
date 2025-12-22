namespace Brine2D.Rendering;

/// <summary>
/// Represents a 2D texture that can be rendered.
/// </summary>
public interface ITexture : IDisposable
{
    /// <summary>
    /// Gets the width of the texture in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the texture in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the path or name of the texture source.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Gets whether the texture is loaded and ready to use.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets or sets the texture scale mode (linear or nearest neighbor).
    /// </summary>
    TextureScaleMode ScaleMode { get; set; }
}