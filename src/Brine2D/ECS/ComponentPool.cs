using System.Buffers;

namespace Brine2D.ECS;

/// <summary>
/// Interface for component pools (type-erased for storage in dictionary).
/// </summary>
internal interface IComponentPool
{
    void Add(int entityId, Component component);
    Component? Get(int entityId);
    bool Remove(int entityId);
    (Array Snapshot, int Length) GetSnapshot();
    void ReturnSnapshot(Array snapshot);
    int Count { get; }
    Type ComponentType { get; }
}

/// <summary>
/// Stores all components of a specific type.
/// Uses regular Dictionary with locks for thread-safety (faster than ConcurrentDictionary).
/// Provides ArrayPool-based snapshots for zero-allocation parallel iteration.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
internal sealed class ComponentPool<T> : IComponentPool where T : Component
{
    private readonly Dictionary<int, T> _components = new();
    private readonly object _lock = new();

    public Type ComponentType => typeof(T);

    public int Count
    {
        get { lock (_lock) { return _components.Count; } }
    }

    public void Add(int entityId, Component component)
    {
        lock (_lock) { _components[entityId] = (T)component; }
    }

    public Component? Get(int entityId)
    {
        lock (_lock) { return _components.TryGetValue(entityId, out var c) ? c : null; }
    }

    public bool Remove(int entityId)
    {
        lock (_lock) { return _components.Remove(entityId); }
    }

    /// <summary>
    /// Creates a snapshot array for safe parallel iteration.
    /// MUST call ReturnSnapshot() in a finally block to return to pool.
    /// </summary>
    public (Array Snapshot, int Length) GetSnapshot()
    {
        lock (_lock)
        {
            var count = _components.Count;
            var snapshot = ArrayPool<(int EntityId, T Component)>.Shared.Rent(count);
            int i = 0;
            foreach (var kvp in _components)
                snapshot[i++] = (kvp.Key, kvp.Value);
            return (snapshot, count);
        }
    }

    /// <summary>
    /// Returns a snapshot to the ArrayPool.
    /// MUST be called after GetSnapshot() to prevent memory leaks.
    /// </summary>
    public void ReturnSnapshot(Array snapshot)
    {
        if (snapshot is (int, T)[] typedSnapshot)
            ArrayPool<(int, T)>.Shared.Return(typedSnapshot, clearArray: true);
    }

    /// <summary>
    /// Returns all component entries as a materialized list.
    /// Uses the snapshot path internally to avoid LINQ allocation overhead.
    /// For hot-path iteration, prefer GetSnapshot() + ReturnSnapshot() directly.
    /// </summary>
    public IEnumerable<(int EntityId, T Component)> All()
    {
        var (snapshot, length) = GetSnapshot();
        try
        {
            var result = new List<(int, T)>(length);
            var typed = (ValueTuple<int, T>[])snapshot;
            for (int i = 0; i < length; i++)
                result.Add(typed[i]);
            return result;
        }
        finally
        {
            ReturnSnapshot(snapshot);
        }
    }

    /// <summary>
    /// Gets a strongly-typed component for a specific entity.
    /// </summary>
    public T? GetTyped(int entityId)
    {
        lock (_lock)
        {
            return _components.TryGetValue(entityId, out var c) ? c : null;
        }
    }
}