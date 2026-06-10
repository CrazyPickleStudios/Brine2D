namespace Brine2D.Tilemap;

/// <summary>
/// A single frame in a tile animation sequence, as defined in the Tiled tileset.
/// </summary>
public sealed class TileAnimationFrame
{
    /// <summary>
    /// The GID of the tile to display for this frame.
    /// </summary>
    public int Gid { get; }

    /// <summary>
    /// Duration of this frame in milliseconds.
    /// </summary>
    public int DurationMs { get; }

    public TileAnimationFrame(int gid, int durationMs)
    {
        Gid = gid;
        DurationMs = durationMs;
    }
}
