namespace Brine2D.Tilemap;

public class TileProperties
{
    public int TileId { get; set; }

    /// <summary>
    /// Set this to true in Tiled via a boolean custom property named <c>solid</c> or <c>isSolid</c>.
    /// Used by <see cref="Tilemap.GenerateCollisionRects"/> and <see cref="Tilemap.MergeCollisionRects"/>.
    /// </summary>
    public bool IsSolid { get; set; }

    /// <summary>
    /// Passable from below; blocks only from above. Set in Tiled via a boolean custom property
    /// named <c>onewayplatform</c> or <c>isOneWayPlatform</c>.
    /// Used by <see cref="Tilemap.GenerateOneWayPlatformRects"/> and <see cref="Tilemap.MergeOneWayPlatformRects"/>.
    /// </summary>
    public bool IsOneWayPlatform { get; set; }

    public Dictionary<string, string> CustomProperties { get; set; } = new();

    public TileProperties(int tileId)
    {
        TileId = tileId;
    }
}