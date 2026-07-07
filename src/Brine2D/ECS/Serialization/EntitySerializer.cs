using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Brine2D.ECS.Serialization;

/// <summary>
/// Serializes and deserializes entities to/from JSON.
/// Uses System.Text.Json with support for custom converters.
/// Works automatically with ANY component type - no hardcoding needed!
/// </summary>
/// <remarks>
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
/// <b>NativeAOT / trimming.</b> This class uses runtime reflection and dynamic JSON
/// serialization to handle arbitrary component types. It is not compatible with
/// NativeAOT or IL trimming as-is; use source-generated <c>JsonSerializerContext</c>
/// with explicit type registration if either of those targets is required.
/// </para>
/// </remarks>
[RequiresDynamicCode("EntitySerializer uses runtime reflection and dynamic JSON serialization. Not compatible with NativeAOT.")]
[RequiresUnreferencedCode("EntitySerializer discovers component types at runtime via AppDomain assembly scanning. Not compatible with IL trimming.")]
public class EntitySerializer
{
    // Locates the open-generic AddComponent<T>(T component) overload — the one that takes
    // a single parameter of the generic type T (not Action<T> and not parameterless).
    private static readonly MethodInfo AddComponentMethod =
        typeof(Entity)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m =>
                m.Name == nameof(Entity.AddComponent) &&
                m.IsGenericMethodDefinition &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters() is { Length: 1 } p &&
                p[0].ParameterType == m.GetGenericArguments()[0]);

    private readonly ILogger<EntitySerializer>? _logger;
    private readonly JsonSerializerOptions _options;

    public EntitySerializer(ILogger<EntitySerializer>? logger = null)
    {
        _logger = logger;

        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(),
                new Vector2Converter()
            },
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            IncludeFields = false
        };
    }

    /// <summary>
    /// Registers a custom JSON converter for a specific type.
    /// Use this to provide custom serialization for user-defined types.
    /// </summary>
    /// <example>
    /// <code>
    /// serializer.RegisterConverter(new MyCustomTypeConverter());
    /// </code>
    /// </example>
    public void RegisterConverter(JsonConverter converter)
    {
        _options.Converters.Add(converter);
        _logger?.LogDebug("Registered custom converter: {ConverterType}", converter.GetType().Name);
    }

    /// <summary>
    /// Registers multiple custom JSON converters.
    /// </summary>
    public void RegisterConverters(params JsonConverter[] converters)
    {
        foreach (var converter in converters)
        {
            RegisterConverter(converter);
        }
    }

    /// <summary>
    /// Gets the current JSON serializer options (for advanced customization).
    /// </summary>
    public JsonSerializerOptions Options => _options;

    /// <summary>
    /// Creates a snapshot of an entity.
    /// </summary>
    /// <remarks>
    /// Only components are persisted. Behaviors are not serialized; they must be
    /// re-applied after restore (e.g., by instantiating through a prefab and then
    /// restoring component state, or by re-adding behaviors manually).
    /// <para>
    /// <b>ID remapping:</b> <see cref="EntitySnapshot.Id"/> captures the entity's runtime ID
    /// so that <see cref="RestoreWorldFromSnapshot"/> can rebuild parent–child hierarchy.
    /// Restored entities receive brand-new runtime IDs; the snapshot ID is not re-injected.
    /// Any component that stores a cross-entity reference as a <see cref="long"/> ID will
    /// hold a stale value after restore. Re-resolve such references by name or tag instead.
    /// </para>
    /// </remarks>
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
            var componentType = component.GetType();

            try
            {
                var jsonElement = JsonSerializer.SerializeToElement(component, componentType, _options);
                snapshot.Components[componentType.FullName ?? componentType.Name] = jsonElement;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to serialize component {Type} on entity {Entity}",
                    componentType.Name, entity.Name);
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Creates a snapshot of the entire world.
    /// </summary>
    public WorldSnapshot CreateWorldSnapshot(IEntityWorld world)
    {
        var snapshot = new WorldSnapshot();

        foreach (var entity in world.Entities)
        {
            snapshot.Entities.Add(CreateSnapshot(entity));
        }

        _logger?.LogInformation("Created world snapshot with {Count} entities", snapshot.Entities.Count);
        return snapshot;
    }

    /// <summary>
    /// Saves a world snapshot to a file.
    /// </summary>
    public async Task SaveWorldAsync(IEntityWorld world, string path, CancellationToken cancellationToken = default)
    {
        var snapshot = CreateWorldSnapshot(world);
        var json = JsonSerializer.Serialize(snapshot, _options);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, json, cancellationToken);
        _logger?.LogInformation("World saved to: {Path}", path);
    }

    /// <summary>
    /// Loads a world snapshot from a file.
    /// </summary>
    public async Task<WorldSnapshot> LoadWorldAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Save file not found: {path}");

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var snapshot = JsonSerializer.Deserialize<WorldSnapshot>(json, _options);

        if (snapshot == null)
            throw new InvalidOperationException("Failed to deserialize world snapshot");

        _logger?.LogInformation("World loaded from: {Path} ({Count} entities)", path, snapshot.Entities.Count);
        return snapshot;
    }

    /// <summary>
    /// Restores entities from a snapshot into the world, preserving the parent–child hierarchy.
    /// CLEARS the existing world's entities first, but leaves registered systems intact.
    /// </summary>
    /// <remarks>
    /// Uses a two-pass strategy: all entities are created in the first pass, then parent–child
    /// relationships are re-established in the second pass using the snapshot IDs recorded by
    /// <see cref="CreateSnapshot"/>. This means <see cref="EntitySnapshot.Id"/> values are
    /// used as keys during restore but are not injected back into the restored entities' runtime IDs.
    /// </remarks>
    public void RestoreWorldFromSnapshot(IEntityWorld world, WorldSnapshot snapshot)
    {
        world.ClearEntities();
        world.Flush();

        // Pass 1 — create all entities and map snapshot ID -> restored entity
        var idMap = new Dictionary<long, Entity>(snapshot.Entities.Count);
        foreach (var entitySnapshot in snapshot.Entities)
        {
            var entity = RestoreEntity(world, entitySnapshot);
            if (entitySnapshot.Id != 0)
                idMap[entitySnapshot.Id] = entity;
        }

        world.Flush();

        // Pass 2 — re-parent using the snapshot ID map
        foreach (var entitySnapshot in snapshot.Entities)
        {
            if (entitySnapshot.ParentId == 0) continue;
            if (!idMap.TryGetValue(entitySnapshot.Id, out var child)) continue;
            if (!idMap.TryGetValue(entitySnapshot.ParentId, out var parent)) continue;
            child.SetParent(parent);
        }

        _logger?.LogInformation("Restored {Count} entities from snapshot", snapshot.Entities.Count);
        if (snapshot.Entities.Count > 0)
            _logger?.LogDebug(
                "Behaviors are not persisted in snapshots and have not been restored. " +
                "Re-add behaviors after restore (e.g., via prefab instantiation) or apply them manually.");
    }

    /// <summary>
    /// Restores a single entity from a snapshot.
    /// </summary>
    public Entity RestoreEntity(IEntityWorld world, EntitySnapshot snapshot)
    {
        // Create entity
        var entity = world.CreateEntity(snapshot.Name);
        entity.IsActive = snapshot.IsActive;

        // Restore tags
        foreach (var tag in snapshot.Tags)
        {
            entity.AddTag(tag);
        }

        // Restore components
        foreach (var kvp in snapshot.Components)
        {
            var componentTypeName = kvp.Key;
            var componentData = kvp.Value;

            try
            {
                RestoreComponent(entity, componentTypeName, componentData);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to restore component {Type} on entity {Entity}",
                    componentTypeName, snapshot.Name);
            }
        }

        return entity;
    }

    /// <summary>
    /// Restores a component from JSON data onto an entity.
    /// The component is fully deserialized before being attached so that
    /// <see cref="Component.OnAdded"/> fires with all restored values in place.
    /// </summary>
    private void RestoreComponent(Entity entity, string componentTypeName, JsonElement componentData)
    {
        var componentType = FindComponentType(componentTypeName);
        if (componentType == null)
        {
            _logger?.LogWarning("Component type not found: {TypeName}", componentTypeName);
            return;
        }

        var component = JsonSerializer.Deserialize(componentData.GetRawText(), componentType, _options) as Component;

        if (component == null)
        {
            _logger?.LogWarning("Failed to deserialize component: {TypeName}", componentTypeName);
            return;
        }

        var genericMethod = AddComponentMethod.MakeGenericMethod(componentType);
        genericMethod.Invoke(entity, [component]);
    }

    /// <summary>
    /// Finds a component type by its full name or simple name.
    /// Searches all loaded assemblies.
    /// </summary>
    private Type? FindComponentType(string typeName)
    {
        // Try exact match first
        var type = Type.GetType(typeName);
        if (type != null)
            return type;

        // Search all loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
                return type;

            // Try simple name match (without namespace). GetTypes() can throw on
            // assemblies with unresolvable type references (e.g. native-interop glue),
            // so use GetLoadedModules / catch the specific exception and keep searching.
            Type[]? allTypes = null;
            try { allTypes = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { allTypes = ex.Types!; }

            if (allTypes != null)
            {
                var simpleNameMatch = Array.Find(allTypes,
                    t => t != null && (t.Name == typeName || t.FullName == typeName));
                if (simpleNameMatch != null)
                    return simpleNameMatch;
            }
        }

        return null;
    }

    /// <summary>
    /// Loads and restores a world from a file.
    /// CLEARS the existing world first!
    /// </summary>
    public async Task LoadAndRestoreWorldAsync(IEntityWorld world, string path, CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadWorldAsync(path, cancellationToken);
        RestoreWorldFromSnapshot(world, snapshot);
        _logger?.LogInformation("World loaded and restored from: {Path}", path);
    }
}

/// <summary>
/// JSON converter for Vector2.
/// </summary>
public class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        float x = 0, y = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new Vector2(x, y);

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName?.ToLower())
                {
                    case "x":
                        x = reader.GetSingle();
                        break;
                    case "y":
                        y = reader.GetSingle();
                        break;
                }
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}