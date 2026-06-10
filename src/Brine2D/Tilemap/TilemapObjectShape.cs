namespace Brine2D.Tilemap;

/// <summary>
/// The geometric shape of a <see cref="TilemapObject"/> as authored in Tiled.
/// </summary>
public enum TilemapObjectShape
{
    Rectangle,
    Point,
    Ellipse,
    Polygon,
    Polyline,
    /// <summary>
    /// A tile placed as an object on an object layer. Use <see cref="TilemapObject.Gid"/> to
    /// identify what to spawn; render manually if visual representation is needed.
    /// </summary>
    Tile,
    Text,
}