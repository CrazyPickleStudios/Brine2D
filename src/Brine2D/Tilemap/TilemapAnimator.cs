namespace Brine2D.Tilemap;

/// <summary>
/// Drives tile animations for a loaded <see cref="Tilemap"/>. All instances of the same animated
/// GID share one clock, matching Tiled's behavior. Pass this to <c>TilemapRenderer.Render</c> each frame.
/// </summary>
public sealed class TilemapAnimator
{
    private readonly Dictionary<int, TileAnimation> _animations = new();
    private readonly Dictionary<int, double> _elapsed = new();

    public void Initialize(Tilemap tilemap)
    {
        _animations.Clear();
        _elapsed.Clear();

        foreach (var tileset in tilemap.Tilesets)
        {
            foreach (var (gid, animation) in tileset.Animations)
            {
                _animations[gid] = animation;
                _elapsed[gid] = 0.0;
            }
        }
    }

    /// <summary>True when the map has at least one animated tile.</summary>
    public bool HasAnimations => _animations.Count > 0;

    public void Update(float deltaTimeSeconds)
    {
        var deltaMs = deltaTimeSeconds * 1000.0;
        foreach (var gid in _animations.Keys)
        {
            var total = _animations[gid].TotalDurationMs;
            if (total > 0)
                _elapsed[gid] = (_elapsed[gid] + deltaMs) % total;
        }
    }

    public int ResolveGid(int tileGid)
    {
        if (!_animations.TryGetValue(tileGid, out var animation))
            return tileGid;

        return animation.ResolveGid(_elapsed[tileGid]);
    }
}
