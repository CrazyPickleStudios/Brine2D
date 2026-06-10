namespace Brine2D.Tilemap;

/// <summary>
/// An animation sequence for a single tile GID, as authored in Tiled.
/// The sequence loops indefinitely.
/// </summary>
public sealed class TileAnimation
{
    /// <summary>
    /// The base GID that owns this animation (the GID that appears in the tile layer data).
    /// </summary>
    public int OwnerGid { get; }

    /// <summary>
    /// Ordered frames of the animation.
    /// </summary>
    public IReadOnlyList<TileAnimationFrame> Frames { get; }

    /// <summary>
    /// Total duration of one full loop in milliseconds.
    /// </summary>
    public int TotalDurationMs { get; }

    public TileAnimation(int ownerGid, IReadOnlyList<TileAnimationFrame> frames)
    {
        OwnerGid = ownerGid;
        Frames = frames;
        TotalDurationMs = 0;
        foreach (var f in frames)
            TotalDurationMs += f.DurationMs;
    }

    /// <summary>
    /// Returns the GID that should be rendered at the given accumulated time in milliseconds.
    /// </summary>
    public int ResolveGid(double elapsedMs)
    {
        if (Frames.Count == 0) return OwnerGid;
        if (TotalDurationMs <= 0) return Frames[0].Gid;

        var t = elapsedMs % TotalDurationMs;
        double accumulated = 0;
        foreach (var frame in Frames)
        {
            accumulated += frame.DurationMs;
            if (t < accumulated)
                return frame.Gid;
        }

        return Frames[^1].Gid;
    }
}
