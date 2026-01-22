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
public class EntitySerializer
{
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
    public EntitySnapshot CreateSnapshot(Entity entity)
    {
        var snapshot = new EntitySnapshot
        {
            Name = entity.Name,
            IsEnabled = entity.IsActive
        };

        snapshot.Tags.AddRange(entity.Tags);

        // Serialize each component using System.Text.Json
        foreach (var component in entity.GetAllComponents())
        {
            var componentType = component.GetType();

            try
            {
                // Just use JsonSerializer directly - it handles everything!
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
    /// Restores entities from a snapshot into the world.
    /// CLEARS the existing world first!
    /// </summary>
    public void RestoreWorldFromSnapshot(IEntityWorld world, WorldSnapshot snapshot)
    {
        // Clear existing entities
        world.Clear();

        foreach (var entitySnapshot in snapshot.Entities)
        {
            RestoreEntity(world, entitySnapshot);
        }

        _logger?.LogInformation("Restored {Count} entities from snapshot", snapshot.Entities.Count);
    }

    /// <summary>
    /// Restores a single entity from a snapshot.
    /// </summary>
    public Entity RestoreEntity(IEntityWorld world, EntitySnapshot snapshot)
    {
        // Create entity
        var entity = world.CreateEntity(snapshot.Name);
        entity.IsActive = snapshot.IsEnabled;

        // Restore tags
        foreach (var tag in snapshot.Tags)
        {
            entity.Tags.Add(tag);
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
    /// </summary>
    private void RestoreComponent(Entity entity, string componentTypeName, object componentData)
    {
        // Find the component type by name
        var componentType = FindComponentType(componentTypeName);
        if (componentType == null)
        {
            _logger?.LogWarning("Component type not found: {TypeName}", componentTypeName);
            return;
        }

        // Deserialize the component data
        var jsonElement = (JsonElement)componentData;
        var component = JsonSerializer.Deserialize(jsonElement.GetRawText(), componentType, _options);

        if (component == null)
        {
            _logger?.LogWarning("Failed to deserialize component: {TypeName}", componentTypeName);
            return;
        }

        // Add component to entity using reflection
        var addComponentMethod = typeof(Entity).GetMethod(nameof(Entity.AddComponent), 
            BindingFlags.Public | BindingFlags.Instance, 
            Type.EmptyTypes);

        if (addComponentMethod == null)
        {
            _logger?.LogError("AddComponent method not found on Entity");
            return;
        }

        // Create generic method for this component type
        var genericMethod = addComponentMethod.MakeGenericMethod(componentType);
        var addedComponent = genericMethod.Invoke(entity, null);

        // Copy properties from deserialized component to added component
        CopyComponentProperties(component, addedComponent);
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

            // Try simple name match (without namespace)
            var simpleNameMatch = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
            
            if (simpleNameMatch != null)
                return simpleNameMatch;
        }

        return null;
    }

    /// <summary>
    /// Copies all public properties from source to destination component.
    /// </summary>
    private void CopyComponentProperties(object source, object? destination)
    {
        if (destination == null) return;

        var sourceType = source.GetType();
        var destType = destination.GetType();

        var properties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.Name != "Entity");

        foreach (var property in properties)
        {
            try
            {
                var destProperty = destType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
                if (destProperty != null && destProperty.CanWrite)
                {
                    var value = property.GetValue(source);
                    destProperty.SetValue(destination, value);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to copy property {Property} on {Type}",
                    property.Name, sourceType.Name);
            }
        }
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