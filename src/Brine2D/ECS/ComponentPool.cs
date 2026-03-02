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
/// Stores all components of a specific type for a single scene-scoped EntityWorld.
/// ComponentPool is accessed exclusively from the game thread via EntityWorld, so individual
/// Get/Add/Remove/GetTyped/Count operations are lock-free. GetSnapshot() retains its lock
/// to produce a consistent array copy; if snapshot consumers are ever moved off the game
/// thread the lock already provides the necessary fence.
/// Provides ArrayPool-based snapshots for zero-allocation iteration.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
internal sealed class ComponentPool<T> : IComponentPool where T : Component
{
    private readonly Dictionary<int, T> _components = new();
    private readonly object _lock = new();

    public Type ComponentType => typeof(T);

    // No lock needed — game-thread-only access
    public int Count => _components.Count;

    // No lock needed — game-thread-only access
    public void Add(int entityId, Component component)
        => _components[entityId] = (T)component;

    // No lock needed — game-thread-only access
    public Component? Get(int entityId)
        => _components.TryGetValue(entityId, out var c) ? c : null;

    // No lock needed — game-thread-only access
    public bool Remove(int entityId)
        => _components.Remove(entityId);

    // No lock needed — game-thread-only access
    public T? GetTyped(int entityId)
        => _components.TryGetValue(entityId, out var c) ? c : null;

    /// <summary>
    /// Creates an ArrayPool snapshot for iteration.
    /// The lock is not strictly required today (game-thread-only access), but is retained
    /// so that if snapshot consumers are ever moved to worker threads the fence is already
    /// in place. MUST call <see cref="ReturnSnapshot"/> in a finally block.
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
    /// MUST be called after <see cref="GetSnapshot"/> to prevent memory leaks.
    /// </summary>
    public void ReturnSnapshot(Array snapshot)
    {
        if (snapshot is (int, T)[] typedSnapshot)
            ArrayPool<(int, T)>.Shared.Return(typedSnapshot, clearArray: true);
    }

    /// <summary>
    /// Returns all component entries as a materialized list.
    /// For hot-path iteration, prefer <see cref="GetSnapshot"/> + <see cref="ReturnSnapshot"/> directly.
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
}