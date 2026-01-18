namespace Brine2D.Rendering.TextureAtlas;

/// <summary>
/// Represents a collection of texture atlases that can be queried as a single logical atlas.
/// Used when textures don't fit in a single atlas and are automatically split across multiple atlases.
/// </summary>
public interface ITextureAtlasCollection : IDisposable
{
    /// <summary>
    /// Gets the name of this atlas collection.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets all atlases in this collection.
    /// </summary>
    IReadOnlyList<ITextureAtlas> Atlases { get; }

    /// <summary>
    /// Gets the total number of regions across all atlases.
    /// </summary>
    int TotalRegionCount { get; }

    /// <summary>
    /// Gets all region names across all atlases.
    /// </summary>
    IEnumerable<string> GetAllRegionNames();

    /// <summary>
    /// Attempts to retrieve a region by name from any atlas in the collection.
    /// </summary>
    /// <param name="name">The region name.</param>
    /// <param name="region">The found region, or null if not found.</param>
    /// <returns>True if the region was found; otherwise, false.</returns>
    bool TryGetRegion(string name, out AtlasRegion? region);

    /// <summary>
    /// Gets a region by name. Throws if not found.
    /// </summary>
    /// <param name="name">The region name.</param>
    /// <returns>The atlas region.</returns>
    /// <exception cref="KeyNotFoundException">If the region doesn't exist in any atlas.</exception>
    AtlasRegion GetRegion(string name);

    /// <summary>
    /// Checks if a region with the given name exists in any atlas.
    /// </summary>
    bool ContainsRegion(string name);

    /// <summary>
    /// Gets all regions from all atlases.
    /// </summary>
    IEnumerable<AtlasRegion> GetAllRegions();
}