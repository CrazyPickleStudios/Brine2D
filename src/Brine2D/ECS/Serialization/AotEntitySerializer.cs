using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Brine2D.ECS.Serialization;

/// <summary>
/// AOT-compatible serializer for entity worlds. Functionally equivalent to
/// <see cref="EntitySerializer"/> but backed by an explicit <see cref="ComponentTypeRegistry"/>
/// instead of runtime AppDomain scanning, eliminating all reflection in the hot path.
/// </summary>
/// <remarks>
/// <para>
/// <b>Typical setup (non-trimmed publishing).</b> Two calls cover everything — all built-in
/// engine components and every component in your game assembly:
/// </para>
/// <code>
/// var registry = new ComponentTypeRegistry();
/// registry.RegisterBrineComponents();                    // all engine components
/// registry.RegisterAllComponents(GetType().Assembly);    // all your game components
///
/// var serializer = new AotEntitySerializer(registry);
/// await serializer.SaveWorldAsync(world, "save.json");
/// </code>
/// <para>
/// <b>Trimmed / NativeAOT publishing.</b> Use <see cref="ComponentTypeRegistry.Register{T}(System.Text.Json.Serialization.Metadata.JsonTypeInfo{T})"/>
/// with a source-generated <c>JsonSerializerContext</c> for each of your custom component
/// types. Built-in engine components do not yet have a fully AOT-safe registration path
/// (post-1.0 roadmap item).
/// </para>
/// <para>
/// <b>Behaviors are not serialized.</b> Only components are persisted. Re-add behaviors
/// after restore (e.g., by instantiating through a prefab) or add them manually.
/// </para>
/// <para>
/// <b>Entity ID remapping.</b> When a world is restored, every entity receives a new
/// runtime ID assigned by the global counter. The original IDs captured in the snapshot
/// are used only to reconstruct the parent–child hierarchy and are discarded afterwards.
/// Any component that stores a cross-entity reference as a <see langword="long"/> entity ID
/// will hold a stale value after restore. Re-resolve such references by entity name or tag
/// after calling <see cref="RestoreWorldFromSnapshot"/>.
/// </para>
/// <para>
/// <b>Unregistered component types</b> are skipped with a warning during both snapshot
/// creation and restore. No exception is thrown; the entity is still created with whatever
/// components <em>were</em> registered.
/// </para>
/// </remarks>
public sealed class AotEntitySerializer
{
    private readonly ComponentTypeRegistry _registry;
    private readonly ILogger<AotEntitySerializer>? _logger;

    public AotEntitySerializer(ComponentTypeRegistry registry, ILogger<AotEntitySerializer>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// Creates a snapshot of a single entity. Components whose types are not registered
    /// in the <see cref="ComponentTypeRegistry"/> are skipped with a warning.
    /// </summary>
    public EntitySnapshot CreateSnapshot(Entity entity)
    {
        var snapshot = new EntitySnapshot
        {
            Id = entity.Id,
            Name = entity.Name,
            IsActive = entity.IsActive,
            ParentId = entity.Parent?.Id ?? 0L
        };

        snapshot.Tags.AddRange(entity.Tags);

        var behaviors = entity.GetAllBehaviors();
        if (behaviors.Count > 0)
            _logger?.LogWarning(
                "Entity '{EntityName}' has {Count} behavior(s) ({Types}) that will not be serialized. " +
                "Re-add behaviors after restore, or instantiate the entity from a prefab.",
                entity.Name,
                behaviors.Count,
                string.Join(", ", behaviors.Select(b => b.GetType().Name)));

        foreach (var component in entity.GetAllComponents())
        {
            if (_registry.TrySerialize(component, out var element))
            {
                var key = component.GetType().FullName ?? component.GetType().Name;
                snapshot.Components[key] = element;
            }
            else
            {
                _logger?.LogWarning(
                    "Component type '{Type}' on entity '{Entity}' is not registered in the ComponentTypeRegistry and will not be serialized. " +
                    "Call registry.Register<{Type}>(...) at startup.",
                    component.GetType().Name, entity.Name, component.GetType().Name);
            }
        }

        return snapshot;
    }

    /// <summary>Creates a snapshot of the entire world.</summary>
    public WorldSnapshot CreateWorldSnapshot(IEntityWorld world)
    {
        var snapshot = new WorldSnapshot();
        foreach (var entity in world.Entities)
            snapshot.Entities.Add(CreateSnapshot(entity));

        _logger?.LogInformation("Created world snapshot with {Count} entities", snapshot.Entities.Count);
        return snapshot;
    }

    /// <summary>Saves a world snapshot to a file.</summary>
    public async Task SaveWorldAsync(IEntityWorld world, string path, CancellationToken cancellationToken = default)
    {
        var snapshot = CreateWorldSnapshot(world);
        var json = JsonSerializer.Serialize(snapshot, Brine2DSnapshotContext.Default.WorldSnapshot);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(path, json, cancellationToken);
        _logger?.LogInformation("World saved to: {Path}", path);
    }

    /// <summary>Loads a world snapshot from a file.</summary>
    public async Task<WorldSnapshot> LoadWorldAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Save file not found: {path}");

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var snapshot = JsonSerializer.Deserialize(json, Brine2DSnapshotContext.Default.WorldSnapshot);

        if (snapshot == null)
            throw new InvalidOperationException("Failed to deserialize world snapshot");

        _logger?.LogInformation("World loaded from: {Path} ({Count} entities)", path, snapshot.Entities.Count);
        return snapshot;
    }

    /// <summary>
    /// Restores entities from a snapshot into the world, preserving the parent–child hierarchy.
    /// Clears the existing world's entities first but leaves registered systems intact.
    /// </summary>
    public void RestoreWorldFromSnapshot(IEntityWorld world, WorldSnapshot snapshot)
    {
        world.ClearEntities();
        world.Flush();

        var idMap = new Dictionary<long, Entity>(snapshot.Entities.Count);
        foreach (var entitySnapshot in snapshot.Entities)
        {
            var entity = RestoreEntity(world, entitySnapshot);
            if (entitySnapshot.Id != 0)
                idMap[entitySnapshot.Id] = entity;
        }

        world.Flush();

        foreach (var entitySnapshot in snapshot.Entities)
        {
            if (entitySnapshot.ParentId == 0) continue;
            if (!idMap.TryGetValue(entitySnapshot.Id, out var child)) continue;
            if (!idMap.TryGetValue(entitySnapshot.ParentId, out var parent)) continue;
            child.SetParent(parent);
        }

        _logger?.LogInformation("Restored {Count} entities from snapshot", snapshot.Entities.Count);
    }

    /// <summary>Restores a single entity from a snapshot.</summary>
    public Entity RestoreEntity(IEntityWorld world, EntitySnapshot snapshot)
    {
        var entity = world.CreateEntity(snapshot.Name);
        entity.IsActive = snapshot.IsActive;

        foreach (var tag in snapshot.Tags)
            entity.AddTag(tag);

        foreach (var kvp in snapshot.Components)
        {
            if (!_registry.TryDeserializeAndAttach(kvp.Key, kvp.Value, entity))
            {
                _logger?.LogWarning(
                    "Component type '{TypeName}' on entity '{Entity}' is not registered in the ComponentTypeRegistry and will be skipped. " +
                    "Call registry.Register<T>(...) at startup for every component type you want to persist.",
                    kvp.Key, snapshot.Name);
            }
        }

        return entity;
    }

    /// <summary>Loads and restores a world from a file. Clears the existing world first.</summary>
    public async Task LoadAndRestoreWorldAsync(IEntityWorld world, string path, CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadWorldAsync(path, cancellationToken);
        RestoreWorldFromSnapshot(world, snapshot);
        _logger?.LogInformation("World loaded and restored from: {Path}", path);
    }
}
