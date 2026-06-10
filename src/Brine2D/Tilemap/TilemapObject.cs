namespace Brine2D.Tilemap;

/// <summary>
/// Represents a single object from a Tiled objectgroup layer.
/// </summary>
public class TilemapObject
{
    /// <summary>
    /// Tiled-assigned unique ID for this object within the map.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The object's name as set in Tiled.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The object's type/class as set in Tiled.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// World-space X position in pixels.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// World-space Y position in pixels.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Width in pixels (0 for point objects).
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Height in pixels (0 for point objects).
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Clockwise rotation in degrees as set in Tiled.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Whether this object is marked visible in Tiled.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// The geometric shape of this object as authored in Tiled.
    /// </summary>
    public TilemapObjectShape Shape { get; set; } = TilemapObjectShape.Rectangle;

    /// <summary>
    /// For <see cref="TilemapObjectShape.Tile"/> objects: the GID of the tile from the map's tileset(s),
    /// with flip bits already stripped. Null for all other shape types.
    /// </summary>
    public int? Gid { get; set; }

    /// <summary>
    /// Whether this tile object is flipped horizontally. Always false for non-tile objects.
    /// </summary>
    public bool FlipHorizontal { get; set; }

    /// <summary>
    /// Whether this tile object is flipped vertically. Always false for non-tile objects.
    /// </summary>
    public bool FlipVertical { get; set; }

    /// <summary>
    /// Whether this tile object has the anti-diagonal flip applied (Tiled's 90-degree rotation encoding).
    /// Always false for non-tile objects.
    /// </summary>
    public bool FlipDiagonal { get; set; }

    /// <summary>
    /// Vertex list for <see cref="TilemapObjectShape.Polygon"/> and <see cref="TilemapObjectShape.Polyline"/>
    /// objects. Coordinates are relative to the object's <see cref="X"/>/<see cref="Y"/> origin.
    /// Null for all other shape types.
    /// </summary>
    public IReadOnlyList<(float X, float Y)>? Points { get; set; }

    /// <summary>
    /// The text string for <see cref="TilemapObjectShape.Text"/> objects as authored in Tiled.
    /// <c>null</c> for all other shape types.
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// Custom properties defined on this object in Tiled.
    /// </summary>
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}