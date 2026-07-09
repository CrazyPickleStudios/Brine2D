using System.Numerics;
using System.Text.Json.Serialization;
using Brine2D.ECS;

namespace Brine2D.Tilemap;

/// <summary>
/// Holds a loaded <see cref="Tilemap"/> and its rendering state.
/// Add <see cref="TilemapSystem"/> to the world and this component handles the rest.
/// </summary>
public class TilemapComponent : Component
{
    [JsonIgnore]
    public Tilemap? Tilemap { get; set; }

    /// <summary>World-space offset applied to the entire map, without touching individual layer offsets.</summary>
    public Vector2 PositionOffset { get; set; } = Vector2.Zero;

    [JsonIgnore]
    public TilemapAnimator? Animator { get; internal set; }

    /// <summary>True once the async texture load finishes. The renderer skips this component until then.</summary>
    [JsonIgnore]
    public bool IsLoaded
    {
        get => _isLoaded;
        internal set => _isLoaded = value;
    }

    private volatile bool _isLoaded;

    // Used by TilemapSystem to detect when the Tilemap reference has been swapped.
    internal Tilemap? InitializedTilemap { get; set; }
}
