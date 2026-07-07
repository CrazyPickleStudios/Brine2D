using System.Numerics;
using Microsoft.Extensions.Logging;

namespace Brine2D.ECS;

/// <summary>
///     Manages a collection of entity prefabs.
///     Registered as a singleton service.
/// </summary>
public class PrefabLibrary(ILogger<PrefabLibrary>? logger = null)
{
    private readonly Dictionary<string, EntityPrefab> _prefabs = new();

    /// <summary>
    ///     Clears all prefabs.
    /// </summary>
    public void Clear()
    {
        _prefabs.Clear();
    }

    /// <summary>
    ///     Gets a prefab by name.
    /// </summary>
    public EntityPrefab? Get(string name)
    {
        return _prefabs.GetValueOrDefault(name);
    }

    /// <summary>
    ///     Gets all registered prefabs.
    /// </summary>
    public IReadOnlyDictionary<string, EntityPrefab> GetAll()
    {
        return _prefabs;
    }

    /// <summary>
    ///     Checks if a prefab exists.
    /// </summary>
    public bool Has(string name)
    {
        return _prefabs.ContainsKey(name);
    }

    /// <summary>
    ///     Registers a prefab in the library.
    /// </summary>
    public void Register(EntityPrefab prefab)
    {
        if (_prefabs.ContainsKey(prefab.Name))
            logger?.LogWarning("Prefab '{Name}' is already registered and will be overwritten", prefab.Name);

        _prefabs[prefab.Name] = prefab;
        logger?.LogDebug("Registered prefab: {Name}", prefab.Name);
    }

    /// <summary>
    ///     Removes a prefab from the library.
    /// </summary>
    public bool Remove(string name)
    {
        return _prefabs.Remove(name);
    }

    /// <summary>
    ///     Instantiates a registered prefab by name.
    /// </summary>
    /// <param name="name">The registered prefab name.</param>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="position">Optional spawn position applied to the root <see cref="ECS.Components.TransformComponent"/>.</param>
    /// <param name="rotation">Optional spawn rotation (in radians) applied to the root <see cref="ECS.Components.TransformComponent"/>.</param>
    /// <param name="scale">Optional spawn scale applied to the root <see cref="ECS.Components.TransformComponent"/>.</param>
    /// <param name="entityName">
    /// Optional override for the instantiated entity's name. When <see langword="null"/>
    /// (default), the prefab's own name is used. Pass an explicit value when spawning
    /// multiple instances so each entity can be found by name.
    /// </param>
    /// <returns>The instantiated entity.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no prefab with <paramref name="name"/> is registered.</exception>
    public Entity Instantiate(string name, IEntityWorld world, Vector2? position = null, float? rotation = null, Vector2? scale = null, string? entityName = null)
    {
        if (!_prefabs.TryGetValue(name, out var prefab))
            throw new KeyNotFoundException($"No prefab named '{name}' is registered in the library.");
        return prefab.Instantiate(world, position, rotation, scale, logger, entityName);
    }

    /// <summary>
    ///     Attempts to instantiate a registered prefab by name.
    /// </summary>
    /// <param name="name">The registered prefab name.</param>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="entity">The instantiated entity, or <see langword="null"/> if the prefab was not found.</param>
    /// <param name="position">Optional spawn position applied to the root <see cref="ECS.Components.TransformComponent"/>.</param>
    /// <param name="rotation">Optional spawn rotation (in radians) applied to the root <see cref="ECS.Components.TransformComponent"/>.</param>
    /// <param name="scale">Optional spawn scale applied to the root <see cref="ECS.Components.TransformComponent"/>.</param>
    /// <param name="entityName">
    /// Optional override for the instantiated entity's name. When <see langword="null"/>
    /// (default), the prefab's own name is used. Pass an explicit value when spawning
    /// multiple instances so each entity can be found by name.
    /// </param>
    /// <returns><see langword="true"/> if the prefab was found and instantiated; otherwise <see langword="false"/>.</returns>
    public bool TryInstantiate(string name, IEntityWorld world, out Entity? entity, Vector2? position = null, float? rotation = null, Vector2? scale = null, string? entityName = null)
    {
        if (!_prefabs.TryGetValue(name, out var prefab))
        {
            entity = null;
            return false;
        }
        entity = prefab.Instantiate(world, position, rotation, scale, logger, entityName);
        return true;
    }
}