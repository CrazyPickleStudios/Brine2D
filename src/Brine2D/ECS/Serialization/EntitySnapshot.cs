using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brine2D.ECS.Serialization;

/// <summary>
/// Serializable snapshot of an entity's state.
/// </summary>
/// <remarks>
/// <para>
/// <b>ID remapping:</b> <see cref="Id"/> stores the entity's <see cref="Entity.Id"/>
/// at the time the snapshot was taken. When the snapshot is restored,
/// <see cref="EntitySerializer.RestoreWorldFromSnapshot"/> creates brand-new entities
/// that receive fresh runtime IDs assigned by the <see cref="Entity"/> counter.
/// The snapshot ID is <em>not</em> re-injected into the restored entity's runtime ID.
/// It is used only to reconstruct the parent–child hierarchy (via <see cref="ParentId"/>)
/// and is otherwise discarded after restoration.
/// </para>
/// <para>
/// If your components store cross-entity references as <see cref="long"/> entity IDs
/// (e.g., an AI target, a joint partner), those IDs will be stale after a restore.
/// Re-resolve such references after restoration, or store entity names/tags instead.
/// </para>
/// </remarks>
public class EntitySnapshot
{
    /// <summary>
    /// The runtime ID of the entity at snapshot time. Used during restore to
    /// reconstruct the parent–child hierarchy. Not injected back into the restored entity.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("components")]
    public Dictionary<string, JsonElement> Components { get; set; } = new();

    [JsonPropertyName("active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The runtime ID of this entity's parent at snapshot time, or <c>0</c> if this is a root entity.
    /// Used during <see cref="EntitySerializer.RestoreWorldFromSnapshot"/> to re-parent entities
    /// after all entities have been created (two-pass restore).
    /// </summary>
    [JsonPropertyName("parentId")]
    public long ParentId { get; set; }
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