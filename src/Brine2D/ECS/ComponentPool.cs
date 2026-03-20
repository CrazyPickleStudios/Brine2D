using System.Buffers;
using System.Diagnostics;

namespace Brine2D.ECS;

/// <summary>
/// Stores all components of a specific type for a single scene-scoped EntityWorld.
/// All access is game-thread-only via EntityWorld; no synchronization is required.
/// Provides ArrayPool-based snapshots for zero-allocation iteration.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
internal sealed class ComponentPool<T> : IComponentPool where T : Component
{
    private readonly Dictionary<long, T> _components = new();

    public Type ComponentType => typeof(T);

    public int Count => _components.Count;

    public void Add(long entityId, Component component)
    {
        Debug.Assert(
            !_components.ContainsKey(entityId),
            $"ComponentPool<{typeof(T).Name}> already contains entity {entityId}; duplicate AddComponent bypassed Entity's guard.");
        _components[entityId] = (T)component;
    }

    public Component? Get(long entityId)
        => _components.TryGetValue(entityId, out var c) ? c : null;

    public bool Contains(long entityId)
        => _components.ContainsKey(entityId);

    public bool Remove(long entityId)
        => _components.Remove(entityId);

    public T? GetTyped(long entityId)
        => _components.TryGetValue(entityId, out var c) ? c : null;

    /// <summary>
    /// Creates a strongly-typed ArrayPool snapshot for iteration.
    /// Avoids the <see cref="Array"/> boxing round-trip of <see cref="GetSnapshot"/>.
    /// MUST call <see cref="ReturnTypedSnapshot"/> in a finally block.
    /// </summary>
    public ((long EntityId, T Component)[] Snapshot, int Length) GetTypedSnapshot()
    {
        var count = _components.Count;
        var snapshot = ArrayPool<(long EntityId, T Component)>.Shared.Rent(count);
        int i = 0;
        foreach (var kvp in _components)
            snapshot[i++] = (kvp.Key, kvp.Value);
        return (snapshot, count);
    }

    /// <summary>
    /// Returns a typed snapshot to the ArrayPool.
    /// MUST be called after <see cref="GetTypedSnapshot"/> to prevent memory leaks.
    /// </summary>
    public void ReturnTypedSnapshot((long, T)[] snapshot)
        => ArrayPool<(long, T)>.Shared.Return(snapshot, clearArray: true);
    
    /// <summary>
    /// Creates an ArrayPool snapshot of entity IDs only, for type-erased iteration.
    /// MUST call <see cref="ReturnEntityIdSnapshot"/> in a finally block.
    /// </summary>
    public (long[] EntityIds, int Length) GetEntityIdSnapshot()
    {
        var count = _components.Count;
        var snapshot = ArrayPool<long>.Shared.Rent(count);
        int i = 0;
        foreach (var key in _components.Keys)
            snapshot[i++] = key;
        return (snapshot, count);
    }

    /// <summary>
    /// Returns an entity-ID snapshot to the ArrayPool.
    /// MUST be called after <see cref="GetEntityIdSnapshot"/> to prevent memory leaks.
    /// </summary>
    public void ReturnEntityIdSnapshot(long[] snapshot)
        => ArrayPool<long>.Shared.Return(snapshot, clearArray: true);

    /// <summary>
    /// Returns all component entries as a materialized list.
    /// Allocates a new list on every call; for hot-path iteration prefer
    /// <see cref="GetTypedSnapshot"/> + <see cref="ReturnTypedSnapshot"/> directly.
    /// </summary>
    public List<(long EntityId, T Component)> All()
    {
        var result = new List<(long, T)>(_components.Count);
        foreach (var (key, value) in _components)
            result.Add((key, value));
        return result;
    }
}