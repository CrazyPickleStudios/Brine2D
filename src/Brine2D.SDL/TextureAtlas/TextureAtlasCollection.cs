using Brine2D.Rendering.TextureAtlas;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL.TextureAtlas;

/// <summary>
/// Collection of texture atlases that can be queried as a single logical atlas.
/// Automatically created when textures are split across multiple atlases.
/// </summary>
public sealed class TextureAtlasCollection : ITextureAtlasCollection
{
    private readonly ILogger<TextureAtlasCollection> _logger;
    private readonly List<ITextureAtlas> _atlases = new();
    private readonly Dictionary<string, AtlasRegion> _regionLookup = new();
    private bool _disposed;

    public string Name { get; }
    public IReadOnlyList<ITextureAtlas> Atlases => _atlases.AsReadOnly();
    public int TotalRegionCount => _regionLookup.Count;

    public TextureAtlasCollection(string name, ILogger<TextureAtlasCollection> logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Adds an atlas to the collection (internal use by builder).
    /// </summary>
    internal void AddAtlas(ITextureAtlas atlas)
    {
        _atlases.Add(atlas);

        // Build lookup table for fast region queries
        foreach (var region in atlas.GetAllRegions())
        {
            if (_regionLookup.ContainsKey(region.Name))
            {
                _logger.LogWarning(
                    "Duplicate region name '{RegionName}' found across atlases. Using first occurrence.",
                    region.Name);
            }
            else
            {
                _regionLookup[region.Name] = region;
            }
        }

        _logger.LogDebug(
            "Added atlas '{AtlasName}' to collection '{CollectionName}'. Total atlases: {Count}",
            atlas.Name, Name, _atlases.Count);
    }

    public IEnumerable<string> GetAllRegionNames()
    {
        return _regionLookup.Keys;
    }

    public bool TryGetRegion(string name, out AtlasRegion? region)
    {
        return _regionLookup.TryGetValue(name, out region);
    }

    public AtlasRegion GetRegion(string name)
    {
        if (!_regionLookup.TryGetValue(name, out var region))
        {
            throw new KeyNotFoundException(
                $"Region '{name}' not found in atlas collection '{Name}' ({_atlases.Count} atlases, {TotalRegionCount} regions)");
        }

        return region;
    }

    public bool ContainsRegion(string name)
    {
        return _regionLookup.ContainsKey(name);
    }

    public IEnumerable<AtlasRegion> GetAllRegions()
    {
        return _regionLookup.Values;
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var atlas in _atlases)
        {
            atlas?.Dispose();
        }

        _atlases.Clear();
        _regionLookup.Clear();
        _disposed = true;

        _logger.LogDebug(
            "Disposed atlas collection '{CollectionName}' with {AtlasCount} atlases",
            Name, _atlases.Count);
    }
}