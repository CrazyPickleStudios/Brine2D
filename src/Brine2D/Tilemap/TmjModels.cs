using System.Text.Json.Serialization;

namespace Brine2D.Tilemap;

/// <summary>
/// Internal models for deserializing Tiled JSON (.tmj) format.
/// These are internal and map to Tiled's JSON structure.
/// </summary>
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

    [JsonPropertyName("data")]
    public List<int> Data { get; set; } = new();

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    [JsonPropertyName("opacity")]
    public float Opacity { get; set; } = 1.0f;

    [JsonPropertyName("properties")]
    public List<TmjProperty>? Properties { get; set; }
}

internal class TmjTileset
{
    [JsonPropertyName("firstgid")]
    public int FirstGid { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

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

    [JsonPropertyName("tiles")]
    public List<TmjTileDefinition>? Tiles { get; set; }
}

internal class TmjTileDefinition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("properties")]
    public List<TmjProperty>? Properties { get; set; }
}

internal class TmjProperty
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("value")]
    public object? Value { get; set; }
}