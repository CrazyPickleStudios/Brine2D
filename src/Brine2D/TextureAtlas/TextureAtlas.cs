using Brine2D.Animation;
using Brine2D.Rendering.TextureAtlas;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL.TextureAtlas;

/// <summary>
/// SDL3 implementation of a texture atlas.
/// Manages a packed texture containing multiple sprite regions.
/// </summary>
public sealed class TextureAtlas : ITextureAtlas
{
    private readonly ILogger<TextureAtlas> _logger;
    private readonly Dictionary<string, AtlasRegion> _regions = new();
    private bool _disposed;

    public string Name { get; }
    public ITexture Texture { get; }
    public IReadOnlyCollection<string> RegionNames => _regions.Keys;
    public int RegionCount => _regions.Count;

    public TextureAtlas(
        string name,
        ITexture texture,
        ILogger<TextureAtlas> logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Texture = texture ?? throw new ArgumentNullException(nameof(texture));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Adds a region to the atlas (internal use by builder).
    /// </summary>
    internal void AddRegion(AtlasRegion region)
    {
        if (_regions.ContainsKey(region.Name))
        {
            _logger.LogWarning("Atlas '{AtlasName}' already contains region '{RegionName}', overwriting", Name, region.Name);
        }

        _regions[region.Name] = region;
    }

    public bool TryGetRegion(string name, out AtlasRegion? region)
    {
        return _regions.TryGetValue(name, out region);
    }

    public AtlasRegion GetRegion(string name)
    {
        if (!_regions.TryGetValue(name, out var region))
        {
            throw new KeyNotFoundException($"Region '{name}' not found in atlas '{Name}'");
        }

        return region;
    }

    public bool ContainsRegion(string name)
    {
        return _regions.ContainsKey(name);
    }

    public IEnumerable<AtlasRegion> GetAllRegions()
    {
        return _regions.Values;
    }

    public void Dispose()
    {
        if (_disposed) return;

        Texture?.Dispose();
        _regions.Clear();
        _disposed = true;

        _logger.LogDebug("Disposed texture atlas '{AtlasName}' with {RegionCount} regions", Name, RegionCount);
    }
}