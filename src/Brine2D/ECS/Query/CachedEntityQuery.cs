using System.Buffers;

namespace Brine2D.ECS.Query;

/// <summary>
/// A cached query that maintains pre-resolved (Entity, Component) pairs for zero-overhead iteration.
/// Automatically invalidates when entities gain or lose components.
/// The cache rebuilds using pool-direct iteration, touching only entities with the queried components.
/// Implements IDisposable; dispose when the owning system is removed mid-scene to
/// stop receiving invalidation notifications and allow GC.
/// </summary>
public class CachedEntityQuery<T1> : ICachedQuery, IDisposable where T1 : Component
{
    private readonly IEntityWorld _world;
    private readonly ECSOptions _options;
    private readonly List<string> _tags;
    private readonly List<Func<Entity, bool>> _predicates;
    private readonly bool _onlyActive;
    private List<(Entity Entity, T1 Component)>? _cachedPairs;
    private bool _isDirty = true;

    internal CachedEntityQuery(
        IEntityWorld world,
        List<string> tags,
        List<Func<Entity, bool>> predicates,
        bool onlyActive,
        ECSOptions? options = null)
    {
        _world = world;
        _options = options ?? new ECSOptions();
        // Defensive copies; builder lists must not affect the query after Build()
        _tags = new List<string>(tags);
        _predicates = new List<Func<Entity, bool>>(predicates);
        _onlyActive = onlyActive;

        if (world is EntityWorld entityWorld)
            entityWorld.RegisterCachedQuery(this);
    }

    internal CachedEntityQuery(IEntityWorld world)
        : this(world, [], [], true, null) { }

    public IEnumerable<Entity> Execute()
    {
        EnsureCache();
        return _cachedPairs!.Select(static p => p.Entity);
    }

    /// <summary>
    /// Executes an action for each cached entity and its component.
    /// Components are pre-resolved; no pool lookup occurs in the hot path.
    /// Automatically parallelizes when entity count exceeds ECSOptions.ParallelEntityThreshold.
    /// </summary>
    public void ForEach(Action<Entity, T1> action)
    {
        EnsureCache();

        var pairs = _cachedPairs!;
        var count = pairs.Count;
        if (count == 0) return;

        if (_options.EnableMultiThreading && count >= _options.ParallelEntityThreshold)
        {
            Parallel.For(0, count, new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.WorkerThreadCount ?? -1
            }, i =>
            {
                var (entity, c1) = pairs[i];
                action(entity, c1);
            });
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                var (entity, c1) = pairs[i];
                action(entity, c1);
            }
        }
    }

    /// <summary>
    /// Returns a struct enumerator over the cached (Entity, Component) pairs.
    /// Use with foreach for zero-allocation iteration; no delegate or closure is created.
    /// </summary>
    public List<(Entity Entity, T1 Component)>.Enumerator GetEnumerator()
    {
        EnsureCache();
        return (_cachedPairs ??= new()).GetEnumerator();
    }

    /// <summary>Forces the cache to refresh on the next execution.</summary>
    public void Invalidate() => _isDirty = true;

    /// <summary>Gets the cached count without re-querying. Returns 0 if cache is dirty.</summary>
    public int Count() => _isDirty ? 0 : _cachedPairs?.Count ?? 0;

    /// <summary>
    /// Unregisters this query from the world's invalidation index.
    /// Call when the owning system is removed mid-scene.
    /// </summary>
    public void Dispose()
    {
        if (_world is EntityWorld entityWorld)
            entityWorld.UnregisterCachedQuery(this);
    }

    private void EnsureCache()
    {
        if (!_isDirty) return;

        (_cachedPairs ??= new()).Clear();

        if (_world is EntityWorld entityWorld)
        {
            var pool = entityWorld.GetOrCreatePool<T1>();
            var (snapshot, length) = pool.GetSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, c1) = ((ValueTuple<int, T1>[])snapshot)[i];
                    var entity = entityWorld.GetEntityById(entityId);
                    if (entity != null && (!_onlyActive || entity.IsActive) && MatchesFilters(entity))
                        _cachedPairs.Add((entity, c1));
                }
            }
            finally { pool.ReturnSnapshot(snapshot); }
        }
        else
        {
            foreach (var entity in _world.Entities)
            {
                if (!_onlyActive || entity.IsActive)
                {
                    var c1 = entity.GetComponent<T1>();
                    if (c1 != null && MatchesFilters(entity))
                        _cachedPairs.Add((entity, c1));
                }
            }
        }

        _isDirty = false;
    }

    private bool MatchesFilters(Entity entity)
    {
        foreach (var tag in _tags)
            if (!entity.HasTag(tag)) return false;
        foreach (var predicate in _predicates)
            if (!predicate(entity)) return false;
        return true;
    }

    public IReadOnlyCollection<Type> ComponentTypes { get; } = [typeof(T1)];
}

/// <summary>
/// Cached query for entities with two components.
/// Cache is rebuilt by iterating the smaller pool and cross-resolving the second component.
/// </summary>
public class CachedEntityQuery<T1, T2> : ICachedQuery, IDisposable
    where T1 : Component
    where T2 : Component
{
    private readonly IEntityWorld _world;
    private readonly ECSOptions _options;
    private readonly List<string> _tags;
    private readonly List<Func<Entity, bool>> _predicates;
    private readonly bool _onlyActive;
    private List<(Entity Entity, T1 C1, T2 C2)>? _cachedPairs;
    private bool _isDirty = true;

    internal CachedEntityQuery(
        IEntityWorld world,
        List<string> tags,
        List<Func<Entity, bool>> predicates,
        bool onlyActive,
        ECSOptions? options = null)
    {
        _world = world;
        _options = options ?? new ECSOptions();
        _tags = new List<string>(tags);
        _predicates = new List<Func<Entity, bool>>(predicates);
        _onlyActive = onlyActive;

        if (world is EntityWorld entityWorld)
            entityWorld.RegisterCachedQuery(this);
    }

    internal CachedEntityQuery(IEntityWorld world)
        : this(world, [], [], true, null) { }

    public IEnumerable<Entity> Execute()
    {
        EnsureCache();
        return _cachedPairs!.Select(static p => p.Entity);
    }

    public void ForEach(Action<Entity, T1, T2> action)
    {
        EnsureCache();

        var pairs = _cachedPairs!;
        var count = pairs.Count;
        if (count == 0) return;

        if (_options.EnableMultiThreading && count >= _options.ParallelEntityThreshold)
        {
            Parallel.For(0, count, new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.WorkerThreadCount ?? -1
            }, i =>
            {
                var (entity, c1, c2) = pairs[i];
                action(entity, c1, c2);
            });
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                var (entity, c1, c2) = pairs[i];
                action(entity, c1, c2);
            }
        }
    }

    /// <summary>
    /// Returns a struct enumerator over the cached (Entity, C1, C2) tuples.
    /// Use with foreach for zero-allocation iteration; no delegate or closure is created.
    /// </summary>
    public List<(Entity Entity, T1 C1, T2 C2)>.Enumerator GetEnumerator()
    {
        EnsureCache();
        return (_cachedPairs ??= new()).GetEnumerator();
    }

    public void Invalidate() => _isDirty = true;
    public int Count() => _isDirty ? 0 : _cachedPairs?.Count ?? 0;

    /// <inheritdoc cref="CachedEntityQuery{T1}.Dispose"/>
    public void Dispose()
    {
        if (_world is EntityWorld entityWorld)
            entityWorld.UnregisterCachedQuery(this);
    }

    private void EnsureCache()
    {
        if (!_isDirty) return;

        (_cachedPairs ??= new()).Clear();

        if (_world is EntityWorld entityWorld)
        {
            var pool1 = entityWorld.GetOrCreatePool<T1>();
            var pool2 = entityWorld.GetOrCreatePool<T2>();

            if (pool1.Count <= pool2.Count)
            {
                var (snapshot, length) = pool1.GetSnapshot();
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        var (entityId, c1) = ((ValueTuple<int, T1>[])snapshot)[i];
                        var entity = entityWorld.GetEntityById(entityId);
                        if (entity == null || (_onlyActive && !entity.IsActive) || !MatchesFilters(entity)) continue;
                        var c2 = entityWorld.GetComponentFromPool<T2>(entityId);
                        if (c2 != null) _cachedPairs.Add((entity, c1, c2));
                    }
                }
                finally { pool1.ReturnSnapshot(snapshot); }
            }
            else
            {
                var (snapshot, length) = pool2.GetSnapshot();
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        var (entityId, c2) = ((ValueTuple<int, T2>[])snapshot)[i];
                        var entity = entityWorld.GetEntityById(entityId);
                        if (entity == null || (_onlyActive && !entity.IsActive) || !MatchesFilters(entity)) continue;
                        var c1 = entityWorld.GetComponentFromPool<T1>(entityId);
                        if (c1 != null) _cachedPairs.Add((entity, c1, c2));
                    }
                }
                finally { pool2.ReturnSnapshot(snapshot); }
            }
        }
        else
        {
            foreach (var entity in _world.Entities)
            {
                if (!_onlyActive || entity.IsActive)
                {
                    var c1 = entity.GetComponent<T1>();
                    var c2 = entity.GetComponent<T2>();
                    if (c1 != null && c2 != null && MatchesFilters(entity))
                        _cachedPairs.Add((entity, c1, c2));
                }
            }
        }

        _isDirty = false;
    }

    private bool MatchesFilters(Entity entity)
    {
        foreach (var tag in _tags)
            if (!entity.HasTag(tag)) return false;
        foreach (var predicate in _predicates)
            if (!predicate(entity)) return false;
        return true;
    }

    public IReadOnlyCollection<Type> ComponentTypes { get; } = [typeof(T1), typeof(T2)];
}

/// <summary>
/// Cached query for entities with three components.
/// Iterates the smallest pool and cross-resolves the remaining two.
/// </summary>
public class CachedEntityQuery<T1, T2, T3> : ICachedQuery, IDisposable
    where T1 : Component
    where T2 : Component
    where T3 : Component
{
    private readonly IEntityWorld _world;
    private readonly ECSOptions _options;
    private readonly List<string> _tags;
    private readonly List<Func<Entity, bool>> _predicates;
    private readonly bool _onlyActive;
    private List<(Entity Entity, T1 C1, T2 C2, T3 C3)>? _cachedPairs;
    private bool _isDirty = true;

    internal CachedEntityQuery(
        IEntityWorld world,
        List<string> tags,
        List<Func<Entity, bool>> predicates,
        bool onlyActive,
        ECSOptions? options = null)
    {
        _world = world;
        _options = options ?? new ECSOptions();
        _tags = new List<string>(tags);
        _predicates = new List<Func<Entity, bool>>(predicates);
        _onlyActive = onlyActive;

        if (world is EntityWorld entityWorld)
            entityWorld.RegisterCachedQuery(this);
    }

    internal CachedEntityQuery(IEntityWorld world)
        : this(world, [], [], true, null) { }

    public IEnumerable<Entity> Execute()
    {
        EnsureCache();
        return _cachedPairs!.Select(static p => p.Entity);
    }

    public void ForEach(Action<Entity, T1, T2, T3> action)
    {
        EnsureCache();

        var pairs = _cachedPairs!;
        var count = pairs.Count;
        if (count == 0) return;

        if (_options.EnableMultiThreading && count >= _options.ParallelEntityThreshold)
        {
            Parallel.For(0, count, new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.WorkerThreadCount ?? -1
            }, i =>
            {
                var (entity, c1, c2, c3) = pairs[i];
                action(entity, c1, c2, c3);
            });
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                var (entity, c1, c2, c3) = pairs[i];
                action(entity, c1, c2, c3);
            }
        }
    }

    public void Invalidate() => _isDirty = true;
    public int Count() => _isDirty ? 0 : _cachedPairs?.Count ?? 0;

    /// <inheritdoc cref="CachedEntityQuery{T1}.Dispose"/>
    public void Dispose()
    {
        if (_world is EntityWorld entityWorld)
            entityWorld.UnregisterCachedQuery(this);
    }

    private void EnsureCache()
    {
        if (!_isDirty) return;

        (_cachedPairs ??= new()).Clear();

        if (_world is EntityWorld entityWorld)
        {
            var pool1 = entityWorld.GetOrCreatePool<T1>();
            var pool2 = entityWorld.GetOrCreatePool<T2>();
            var pool3 = entityWorld.GetOrCreatePool<T3>();

            var min = Math.Min(pool1.Count, Math.Min(pool2.Count, pool3.Count));

            if (min == pool1.Count)
            {
                var (snapshot, length) = pool1.GetSnapshot();
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        var (entityId, c1) = ((ValueTuple<int, T1>[])snapshot)[i];
                        var entity = entityWorld.GetEntityById(entityId);
                        if (entity == null || (_onlyActive && !entity.IsActive) || !MatchesFilters(entity)) continue;
                        var c2 = entityWorld.GetComponentFromPool<T2>(entityId);
                        if (c2 == null) continue;
                        var c3 = entityWorld.GetComponentFromPool<T3>(entityId);
                        if (c3 != null) _cachedPairs.Add((entity, c1, c2, c3));
                    }
                }
                finally { pool1.ReturnSnapshot(snapshot); }
            }
            else if (min == pool2.Count)
            {
                var (snapshot, length) = pool2.GetSnapshot();
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        var (entityId, c2) = ((ValueTuple<int, T2>[])snapshot)[i];
                        var entity = entityWorld.GetEntityById(entityId);
                        if (entity == null || (_onlyActive && !entity.IsActive) || !MatchesFilters(entity)) continue;
                        var c1 = entityWorld.GetComponentFromPool<T1>(entityId);
                        if (c1 == null) continue;
                        var c3 = entityWorld.GetComponentFromPool<T3>(entityId);
                        if (c3 != null) _cachedPairs.Add((entity, c1, c2, c3));
                    }
                }
                finally { pool2.ReturnSnapshot(snapshot); }
            }
            else
            {
                var (snapshot, length) = pool3.GetSnapshot();
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        var (entityId, c3) = ((ValueTuple<int, T3>[])snapshot)[i];
                        var entity = entityWorld.GetEntityById(entityId);
                        if (entity == null || (_onlyActive && !entity.IsActive) || !MatchesFilters(entity)) continue;
                        var c1 = entityWorld.GetComponentFromPool<T1>(entityId);
                        if (c1 == null) continue;
                        var c2 = entityWorld.GetComponentFromPool<T2>(entityId);
                        if (c2 != null) _cachedPairs.Add((entity, c1, c2, c3));
                    }
                }
                finally { pool3.ReturnSnapshot(snapshot); }
            }
        }
        else
        {
            foreach (var entity in _world.Entities)
            {
                if (!_onlyActive || entity.IsActive)
                {
                    var c1 = entity.GetComponent<T1>();
                    var c2 = entity.GetComponent<T2>();
                    var c3 = entity.GetComponent<T3>();
                    if (c1 != null && c2 != null && c3 != null && MatchesFilters(entity))
                        _cachedPairs.Add((entity, c1, c2, c3));
                }
            }
        }

        _isDirty = false;
    }

    private bool MatchesFilters(Entity entity)
    {
        foreach (var tag in _tags)
            if (!entity.HasTag(tag)) return false;
        foreach (var predicate in _predicates)
            if (!predicate(entity)) return false;
        return true;
    }

    public IReadOnlyCollection<Type> ComponentTypes { get; } = [typeof(T1), typeof(T2), typeof(T3)];
}