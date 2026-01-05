using Microsoft.Extensions.Logging;

namespace Brine2D.ECS;

/// <summary>
/// Manages a collection of entity prefabs.
/// Registered as a singleton service.
/// </summary>
public class PrefabLibrary
{
    private readonly Dictionary<string, EntityPrefab> _prefabs = new();
    private readonly ILogger<PrefabLibrary>? _logger;

    public PrefabLibrary(ILogger<PrefabLibrary>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a prefab in the library.
    /// </summary>
    public void Register(EntityPrefab prefab)
    {
        _prefabs[prefab.Name] = prefab;
        _logger?.LogDebug("Registered prefab: {Name}", prefab.Name);
    }

    /// <summary>
    /// Gets a prefab by name.
    /// </summary>
    public EntityPrefab? Get(string name)
    {
        return _prefabs.TryGetValue(name, out var prefab) ? prefab : null;
    }

    /// <summary>
    /// Checks if a prefab exists.
    /// </summary>
    public bool Has(string name)
    {
        return _prefabs.ContainsKey(name);
    }

    /// <summary>
    /// Gets all registered prefabs.
    /// </summary>
    public IReadOnlyDictionary<string, EntityPrefab> GetAll()
    {
        return _prefabs;
    }

    /// <summary>
    /// Removes a prefab from the library.
    /// </summary>
    public bool Remove(string name)
    {
        return _prefabs.Remove(name);
    }

    /// <summary>
    /// Clears all prefabs.
    /// </summary>
    public void Clear()
    {
        _prefabs.Clear();
    }
}