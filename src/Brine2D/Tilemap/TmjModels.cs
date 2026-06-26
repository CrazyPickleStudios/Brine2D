using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brine2D.Tilemap;

internal class TmjMap
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("tilewidth")]
    public int TileWidth { get; set; }

    [JsonPropertyName("tileheight")]
    public int TileHeight { get; set; }

    [JsonPropertyName("layers")]
    public List<TmjLayer> Layers { get; set; } = new();

    [JsonPropertyName("tilesets")]
    public List<TmjTileset> Tilesets { get; set; } = new();

    [JsonPropertyName("properties")]
    public List<TmjProperty>? Properties { get; set; }

    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = "orthogonal";

    [JsonPropertyName("infinite")]
    public bool Infinite { get; set; }

    [JsonPropertyName("renderorder")]
    public string? RenderOrder { get; set; }

    [JsonPropertyName("backgroundcolor")]
    public string? BackgroundColor { get; set; }
}

internal class TmjLayer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "tilelayer";

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    /// <summary>
    /// Raw JSON element so we can handle both CSV (array) and base64 (string) encodings.
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    /// <summary>
    /// Present (non-null) when the map was saved in Tiled's infinite/chunk mode.
    /// Chunk data is not parsed; its presence is used to throw a clear error.
    /// </summary>
    [JsonPropertyName("chunks")]
    public JsonElement? Chunks { get; set; }

    /// <summary>
    /// Tiled layer encoding: "csv" (default) or "base64".
    /// </summary>
    [JsonPropertyName("encoding")]
    public string? Encoding { get; set; }

    /// <summary>
    /// Tiled layer compression when encoding is base64: "zlib", "gzip", "zstd", or absent for none.
    /// </summary>
    [JsonPropertyName("compression")]
    public string? Compression { get; set; }

    [JsonPropertyName("objects")]
    public List<TmjObject>? Objects { get; set; }

    /// <summary>
    /// Child layers when this layer is a group (type == "group").
    /// </summary>
    [JsonPropertyName("layers")]
    public List<TmjLayer>? Layers { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    [JsonPropertyName("opacity")]
    public float Opacity { get; set; } = 1.0f;

    [JsonPropertyName("offsetx")]
    public float OffsetX { get; set; }

    [JsonPropertyName("offsety")]
    public float OffsetY { get; set; }

    [JsonPropertyName("parallaxx")]
    public float ParallaxX { get; set; } = 1.0f;

    [JsonPropertyName("parallaxy")]
    public float ParallaxY { get; set; } = 1.0f;

    [JsonPropertyName("tintcolor")]
    public string? TintColor { get; set; }

    /// <summary>
    /// Image path for imagelayer type layers.
    /// </summary>
    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("properties")]
    public List<TmjProperty>? Properties { get; set; }
}

internal class TmjObject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The object's user-defined type/class. Tiled 1.8 and earlier export this as "type";
    /// Tiled 1.9+ exports it as "class". Both are read so maps from any Tiled version load correctly.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Tiled 1.9+ renamed the "type" field to "class". Populated when loading newer maps.
    /// <see cref="TmjLoader"/> uses <see cref="Type"/> first and falls back to this.
    /// </summary>
    [JsonPropertyName("class")]
    public string? Class { get; set; }

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("width")]
    public float Width { get; set; }

    [JsonPropertyName("height")]
    public float Height { get; set; }

    [JsonPropertyName("rotation")]
    public float Rotation { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Present and true when this is a point object.
    /// </summary>
    [JsonPropertyName("point")]
    public bool Point { get; set; }

    /// <summary>
    /// Present and true when this is an ellipse object.
    /// </summary>
    [JsonPropertyName("ellipse")]
    public bool Ellipse { get; set; }

    /// <summary>
    /// Vertex list for polygon objects. Coordinates are relative to the object origin.
    /// </summary>
    [JsonPropertyName("polygon")]
    public List<TmjPoint>? Polygon { get; set; }

    /// <summary>
    /// Vertex list for polyline objects. Coordinates are relative to the object origin.
    /// </summary>
    [JsonPropertyName("polyline")]
    public List<TmjPoint>? Polyline { get; set; }

    /// <summary>
    /// Raw GID from Tiled for tile objects, including any flip bits in the high bytes.
    /// Use <see cref="TmjLoader"/> to strip bits and populate the clean <see cref="TilemapObject.Gid"/>.
    /// Absent for all other shape types.
    /// </summary>
    [JsonPropertyName("gid")]
    public long? Gid { get; set; }

    /// <summary>
    /// Present for text objects. The nested Tiled "text" object; only the "text" string field
    /// is extracted — font, size, and style are editor-only concerns.
    /// </summary>
    [JsonPropertyName("text")]
    public TmjTextData? Text { get; set; }

    [JsonPropertyName("properties")]
    public List<TmjProperty>? Properties { get; set; }
}

internal class TmjPoint
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }
}

internal class TmjTileset
{
    [JsonPropertyName("firstgid")]
    public int FirstGid { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("imagewidth")]
    public int ImageWidth { get; set; }

    [JsonPropertyName("imageheight")]
    public int ImageHeight { get; set; }

    [JsonPropertyName("tilewidth")]
    public int TileWidth { get; set; }

    [JsonPropertyName("tileheight")]
    public int TileHeight { get; set; }

    [JsonPropertyName("columns")]
    public int Columns { get; set; }

    [JsonPropertyName("tilecount")]
    public int TileCount { get; set; }

    [JsonPropertyName("spacing")]
    public int Spacing { get; set; }

    [JsonPropertyName("margin")]
    public int Margin { get; set; }

    [JsonPropertyName("properties")]
    public List<TmjProperty>? Properties { get; set; }

    [JsonPropertyName("tiles")]
    public List<TmjTileDefinition>? Tiles { get; set; }
}

internal class TmjTileDefinition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("properties")]
    public List<TmjProperty>? Properties { get; set; }

    [JsonPropertyName("animation")]
    public List<TmjAnimationFrame>? Animation { get; set; }
}

internal class TmjAnimationFrame
{
    /// <summary>
    /// Local tile ID (0-based, not GID) within the tileset for this frame.
    /// </summary>
    [JsonPropertyName("tileid")]
    public int TileId { get; set; }

    /// <summary>
    /// Duration of this frame in milliseconds.
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
}

internal class TmjProperty
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }
}

internal class TmjTextData
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}