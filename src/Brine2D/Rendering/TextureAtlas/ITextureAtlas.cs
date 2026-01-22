using Brine2D.Animation;

namespace Brine2D.Rendering.TextureAtlas;

/// <summary>
/// Represents a texture atlas containing multiple sprite regions packed into a single texture.
/// Reduces draw calls by batching multiple sprites that share the same atlas texture.
/// </summary>
public interface ITextureAtlas : IDisposable
{
    /// <summary>
    /// Gets a unique name for this atlas.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The underlying packed atlas texture.
    /// </summary>
    ITexture Texture { get; }

    /// <summary>
    /// Gets all region names in the atlas.
    /// </summary>
    IReadOnlyCollection<string> RegionNames { get; }

    /// <summary>
    /// Gets the number of regions in the atlas.
    /// </summary>
    int RegionCount { get; }

    /// <summary>
    /// Attempts to retrieve a region by name.
    /// </summary>
    /// <param name="name">The region name (typically the original filename without extension).</param>
    /// <param name="region">The found region, or null if not found.</param>
    /// <returns>True if the region was found; otherwise, false.</returns>
    bool TryGetRegion(string name, out AtlasRegion? region);

    /// <summary>
    /// Gets a region by name. Throws if not found.
    /// </summary>
    /// <param name="name">The region name.</param>
    /// <returns>The atlas region.</returns>
    /// <exception cref="KeyNotFoundException">If the region doesn't exist.</exception>
    AtlasRegion GetRegion(string name);

    /// <summary>
    /// Checks if a region with the given name exists.
    /// </summary>
    bool ContainsRegion(string name);

    /// <summary>
    /// Gets all regions in the atlas.
    /// </summary>
    IEnumerable<AtlasRegion> GetAllRegions();
}