using System.Numerics;
using System.Text.Json.Serialization;

namespace Brine2D.ECS.Serialization;

/// <summary>
/// Serializable snapshot of an entity's state.
/// </summary>
public class EntitySnapshot
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("components")]
    public Dictionary<string, object> Components { get; set; } = new();

    [JsonPropertyName("enabled")]
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Serializable snapshot of the entire world.
/// </summary>
public class WorldSnapshot
{
    [JsonPropertyName("entities")]
    public List<EntitySnapshot> Entities { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
}