using System.Threading;
using Brine2D.Core;

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
    /// Gets the name or path of the texture.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether the texture is loaded and ready to use.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets or sets the texture scale mode (linear or nearest neighbor).
    /// </summary>
    TextureScaleMode ScaleMode { get; }
    
    /// <summary>
    /// Gets the bounding rectangle of the texture (0, 0, Width, Height).
    /// Convenience property for texture operations.
    /// </summary>
    Rectangle Bounds => new(0, 0, Width, Height);

    /// <summary>
    /// Gets a stable key used for sorting textures during batching.
    /// Must be deterministic for the lifetime of the texture within a process.
    /// </summary>
    int SortKey { get; }

    /// <summary>
    /// Allocates the next globally unique sort key for texture batching.
    /// All <see cref="ITexture"/> implementations must use this to avoid key collisions.
    /// </summary>
    protected static int NextSortKey() => Interlocked.Increment(ref _nextSortKey);
    private static int _nextSortKey;
}