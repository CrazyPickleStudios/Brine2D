namespace Brine2D.ECS.Query;

/// <summary>
/// Abstract base for cached entity queries. Holds shared state (world, options, filters)
/// and implements invalidation, disposal, and filter matching once for all arities.
/// </summary>
public abstract class CachedEntityQueryBase : ICachedQuery, IDisposable
{
    private protected readonly IEntityWorld _world;
    private protected readonly EntityWorld? _entityWorld;
    private protected readonly ECSOptions _options;
    private protected readonly List<string> _tags;
    private protected readonly List<string> _withoutTags;
    private protected readonly List<string> _withAnyTags;
    private protected readonly List<Func<Entity, bool>> _predicates;
    private protected readonly List<Type> _withoutTypes;
    private protected readonly List<Type> _withBehaviorTypes;
    private protected readonly List<Type> _withoutBehaviorTypes;
    private protected readonly bool _onlyActive;
    private protected readonly bool _onlyEnabled;
    private protected bool _isDirty = true;
    private bool _disposed;

    private protected readonly EntityFilterState _filterState;
    private protected readonly List<IComponentPool> _poolBuffer = new();

    private Type[]? _allTrackedTypes;
    private string[]? _allTagFilters;

    internal CachedEntityQueryBase(
        IEntityWorld world,
        List<string> tags,
        List<Func<Entity, bool>> predicates,
        List<Type> withoutTypes,
        List<Type> withBehaviorTypes,
        List<Type> withoutBehaviorTypes,
        List<string> withoutTags,
        List<string> withAnyTags,
        bool onlyActive,
        bool onlyEnabled,
        ECSOptions? options)
    {
        _world = world;
        _entityWorld = world as EntityWorld;
        _options = options ?? new ECSOptions();
        _tags = new List<string>(tags);
        _predicates = new List<Func<Entity, bool>>(predicates);
        _withoutTypes = new List<Type>(withoutTypes);
        _withBehaviorTypes = new List<Type>(withBehaviorTypes);
        _withoutBehaviorTypes = new List<Type>(withoutBehaviorTypes);
        _withoutTags = new List<string>(withoutTags);
        _withAnyTags = new List<string>(withAnyTags);
        _onlyActive = onlyActive;
        _onlyEnabled = onlyEnabled;

        _filterState = new EntityFilterState(
            _withoutTypes, _withBehaviorTypes, _withoutBehaviorTypes,
            _tags, _withoutTags, _withAnyTags);

        _entityWorld?.RegisterCachedQuery(this);
    }

    /// <summary>
    /// The component types this query tracks for invalidation.
    /// Includes both required and excluded types so that adding or removing
    /// an excluded component correctly triggers a cache rebuild.
    /// </summary>
    public IReadOnlyCollection<Type> ComponentTypes
    {
        get
        {
            if (_withoutTypes.Count == 0) return RequiredTypes;
            return _allTrackedTypes ??= [.. RequiredTypes, .. _withoutTypes];
        }
    }

    /// <summary>
    /// All tag strings this query filters by (with-all, without, any-of).
    /// Used by <see cref="EntityWorld"/> for per-tag invalidation targeting.
    /// </summary>
    public IReadOnlyCollection<string> TagFilters
    {
        get
        {
            if (_withoutTags.Count == 0 && _withAnyTags.Count == 0) return _tags;
            return _allTagFilters ??= [.. _tags, .. _withoutTags, .. _withAnyTags];
        }
    }

    /// <summary>
    /// Whether this query filters by entity tags.
    /// Used by <see cref="EntityWorld"/> to target tag-change invalidation.
    /// </summary>
    public bool HasTagFilters => _tags.Count > 0 || _withoutTags.Count > 0 || _withAnyTags.Count > 0;

    /// <summary>
    /// Whether this query filters by behaviors.
    /// Used by <see cref="EntityWorld"/> to target behavior-change invalidation.
    /// </summary>
    public bool HasBehaviorFilters => _withBehaviorTypes.Count > 0 || _withoutBehaviorTypes.Count > 0;

    /// <summary>
    /// Whether this query filters by entity active state (default <c>true</c>).
    /// Queries built with <c>IncludeInactive()</c> return <c>false</c> and are
    /// skipped when only <see cref="Entity.IsActive"/> changes.
    /// </summary>
    public bool FiltersActiveState => _onlyActive;

    /// <summary>
    /// Whether this query filters by component enabled state.
    /// Queries built with <c>OnlyEnabled()</c> return <c>true</c> and are
    /// invalidated when any <see cref="Component.IsEnabled"/> changes.
    /// </summary>
    public bool FiltersEnabledState => _onlyEnabled;

    private protected abstract IReadOnlyCollection<Type> RequiredTypes { get; }

    /// <summary>Forces the cache to refresh on the next execution.</summary>
    public void Invalidate() => _isDirty = true;

    /// <summary>Gets the cached count, refreshing the cache if dirty.</summary>
    public int Count()
    {
        EnsureCache();
        return CachedCount;
    }

    private protected abstract int CachedCount { get; }

    /// <summary>Rebuilds the cache if dirty.</summary>
    private protected abstract void EnsureCache();

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if this query has been disposed.
    /// Called by <see cref="EnsureCache"/> in each concrete arity to prevent
    /// silent use of a query that no longer receives invalidation notifications.
    /// </summary>
    private protected void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Unregisters this query from the world's invalidation index.
    /// Call when the owning system is removed mid-scene.
    /// Failing to dispose leaves the query in the world's invalidation index,
    /// preventing GC until the world itself is disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _entityWorld?.UnregisterCachedQuery(this);
    }

    private protected bool MatchesFilters(Entity entity)
    {
        if (!_filterState.Matches(entity)) return false;

        for (int i = 0; i < _predicates.Count; i++)
            if (!_predicates[i](entity)) return false;

        return true;
    }

    private protected bool ShouldParallelize(int count)
        => _options.EnableMultiThreading && count >= _options.ParallelEntityThreshold;
}

/// <summary>
/// A cached query that maintains pre-resolved (Entity, Component) pairs for zero-overhead iteration.
/// Automatically invalidates when entities gain or lose components.
/// The cache rebuilds using pool-direct iteration, touching only entities with the queried components.
/// Implements IDisposable; dispose when the owning system is removed mid-scene to
/// stop receiving invalidation notifications and allow GC.
/// </summary>
public class CachedEntityQuery<T1> : CachedEntityQueryBase where T1 : Component
{
    private List<(Entity Entity, T1 Component)>? _cachedPairs;
    private EntityProjection<(Entity Entity, T1 Component)>? _entityProjection;

    internal CachedEntityQuery(
        IEntityWorld world,
        List<string> tags,
        List<Func<Entity, bool>> predicates,
        List<Type> withoutTypes,
        List<Type> withBehaviorTypes,
        List<Type> withoutBehaviorTypes,
        List<string> withoutTags,
        List<string> withAnyTags,
        bool onlyActive,
        bool onlyEnabled,
        ECSOptions? options = null)
        : base(world, tags, predicates, withoutTypes, withBehaviorTypes, withoutBehaviorTypes, withoutTags, withAnyTags, onlyActive, onlyEnabled, options) { }

    internal CachedEntityQuery(IEntityWorld world)
        : this(world, [], [], [], [], [], [], [], true, false, null) { }

    private protected override IReadOnlyCollection<Type> RequiredTypes { get; } = [typeof(T1)];
    private protected override int CachedCount => _cachedPairs?.Count ?? 0;

    /// <summary>
    /// Executes the query and returns matching entities.
    /// The returned list is cached; no allocation occurs per call.
    /// </summary>
    public IReadOnlyList<Entity> Execute()
    {
        EnsureCache();
        return _entityProjection ??= new(_cachedPairs!, static pair => pair.Entity);
    }

    /// <summary>
    /// Executes an action for each cached entity and its component.
    /// Components are pre-resolved; no pool lookup occurs in the hot path.
    /// Automatically parallelizes when entity count exceeds ECSOptions.ParallelEntityThreshold.
    /// </summary>
    /// <remarks>
    /// <b>Thread safety:</b> When parallelized, the <paramref name="action"/> delegate runs
    /// on multiple threads simultaneously. The caller is responsible for ensuring thread-safe
    /// access to any shared or mutable state, including component properties.
    /// </remarks>
    public void ForEach(Action<Entity, T1> action)
    {
        EnsureCache();

        var pairs = _cachedPairs!;
        var count = pairs.Count;
        if (count == 0) return;

        if (ShouldParallelize(count))
        {
            Parallel.For(0, count, _options.GetParallelOptions(), i =>
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

    private protected override void EnsureCache()
    {
        ThrowIfDisposed();
        if (!_isDirty) return;

        (_cachedPairs ??= new()).Clear();

        if (_entityWorld != null)
        {
            _entityWorld.GetPoolsAssignableTo(typeof(T1), _poolBuffer);
            if (_poolBuffer.Count == 1 && _poolBuffer[0] is ComponentPool<T1> exactPool)
            {
                var (snapshot, length) = exactPool.GetTypedSnapshot();
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        var (entityId, c1) = snapshot[i];
                        var entity = _entityWorld.GetEntityById(entityId);
                        if (entity != null && (!_onlyActive || entity.IsActive) &&
                            (!_onlyEnabled || c1.IsEnabled) && MatchesFilters(entity))
                            _cachedPairs.Add((entity, c1));
                    }
                }
                finally { exactPool.ReturnTypedSnapshot(snapshot); }
            }
            else
            {
                foreach (var pool in _poolBuffer)
                {
                    var (ids, length) = pool.GetEntityIdSnapshot();
                    try
                    {
                        for (int i = 0; i < length; i++)
                        {
                            var entity = _entityWorld.GetEntityById(ids[i]);
                            if (entity == null || (_onlyActive && !entity.IsActive) || !MatchesFilters(entity)) continue;
                            var c1 = (T1?)pool.Get(ids[i]);
                            if (c1 != null && (!_onlyEnabled || c1.IsEnabled))
                                _cachedPairs.Add((entity, c1));
                        }
                    }
                    finally { pool.ReturnEntityIdSnapshot(ids); }
                }
            }
        }
        else
        {
            foreach (var entity in _world.Entities)
            {
                if (!_onlyActive || entity.IsActive)
                {
                    var c1 = entity.GetComponent<T1>();
                    if (c1 != null && (!_onlyEnabled || c1.IsEnabled) && MatchesFilters(entity))
                        _cachedPairs.Add((entity, c1));
                }
            }
        }

        _isDirty = false;
    }
}

/// <summary>
/// Cached query for entities with two components.
/// Cache is rebuilt by iterating the smaller pool and cross-resolving the second component.
/// </summary>
public class CachedEntityQuery<T1, T2> : CachedEntityQueryBase
    where T1 : Component
    where T2 : Component
{
    private List<(Entity Entity, T1 C1, T2 C2)>? _cachedPairs;
    private EntityProjection<(Entity Entity, T1 C1, T2 C2)>? _entityProjection;

    internal CachedEntityQuery(
        IEntityWorld world,
        List<string> tags,
        List<Func<Entity, bool>> predicates,
        List<Type> withoutTypes,
        List<Type> withBehaviorTypes,
        List<Type> withoutBehaviorTypes,
        List<string> withoutTags,
        List<string> withAnyTags,
        bool onlyActive,
        bool onlyEnabled,
        ECSOptions? options = null)
        : base(world, tags, predicates, withoutTypes, withBehaviorTypes, withoutBehaviorTypes, withoutTags, withAnyTags, onlyActive, onlyEnabled, options) { }

    internal CachedEntityQuery(IEntityWorld world)
        : this(world, [], [], [], [], [], [], [], true, false, null) { }

    private protected override IReadOnlyCollection<Type> RequiredTypes { get; } = [typeof(T1), typeof(T2)];
    private protected override int CachedCount => _cachedPairs?.Count ?? 0;

    /// <summary>
    /// Executes the query and returns matching entities.
    /// The returned list is cached; no allocation occurs per call.
    /// </summary>
    public IReadOnlyList<Entity> Execute()
    {
        EnsureCache();
        return _entityProjection ??= new(_cachedPairs!, static pair => pair.Entity);
    }

    /// <summary>
    /// Executes an action for each cached entity and its components.
    /// Automatically parallelizes when entity count exceeds ECSOptions.ParallelEntityThreshold.
    /// </summary>
    /// <remarks>
    /// <b>Thread safety:</b> When parallelized, the <paramref name="action"/> delegate runs
    /// on multiple threads simultaneously. The caller is responsible for ensuring thread-safe
    /// access to any shared or mutable state, including component properties.
    /// </remarks>
    public void ForEach(Action<Entity, T1, T2> action)
    {
        EnsureCache();

        var pairs = _cachedPairs!;
        var count = pairs.Count;
        if (count == 0) return;

        if (ShouldParallelize(count))
        {
            Parallel.For(0, count, _options.GetParallelOptions(), i =>
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

    private protected override void EnsureCache()
    {
        ThrowIfDisposed();
        if (!_isDirty) return;

        (_cachedPairs ??= new()).Clear();

        if (_entityWorld != null)
        {
            int totalT1 = _entityWorld.GetTotalPoolCount(typeof(T1));
            int totalT2 = _entityWorld.GetTotalPoolCount(typeof(T2));

            if (totalT1 > 0 && totalT2 > 0)
            {
                _entityWorld.GetPoolsAssignableTo(
                    totalT1 <= totalT2 ? typeof(T1) : typeof(T2), _poolBuffer);

                foreach (var pool in _poolBuffer)
                {
                    var (ids, length) = pool.GetEntityIdSnapshot();
                    try
                    {
                        for (int i = 0; i < length; i++)
                        {
                            var entity = _entityWorld.GetEntityById(ids[i]);
                            if (entity == null || (_onlyActive && !entity.IsActive) || !MatchesFilters(entity)) continue;
                            var c1 = _entityWorld.GetComponentFromPool<T1>(ids[i]);
                            if (c1 == null || (_onlyEnabled && !c1.IsEnabled)) continue;
                            var c2 = _entityWorld.GetComponentFromPool<T2>(ids[i]);
                            if (c2 != null && (!_onlyEnabled || c2.IsEnabled))
                                _cachedPairs.Add((entity, c1, c2));
                        }
                    }
                    finally { pool.ReturnEntityIdSnapshot(ids); }
                }
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
                    if (c1 != null && c2 != null &&
                        (!_onlyEnabled || (c1.IsEnabled && c2.IsEnabled)) &&
                        MatchesFilters(entity))
                        _cachedPairs.Add((entity, c1, c2));
                }
            }
        }

        _isDirty = false;
    }
}

/// <summary>
/// Cached query for entities with three components.
/// Iterates the smallest pool and cross-resolves the remaining two.
/// </summary>
public class CachedEntityQuery<T1, T2, T3> : CachedEntityQueryBase
    where T1 : Component
    where T2 : Component
    where T3 : Component
{
    private List<(Entity Entity, T1 C1, T2 C2, T3 C3)>? _cachedPairs;
    private EntityProjection<(Entity Entity, T1 C1, T2 C2, T3 C3)>? _entityProjection;

    internal CachedEntityQuery(
        IEntityWorld world,
        List<string> tags,
        List<Func<Entity, bool>> predicates,
        List<Type> withoutTypes,
        List<Type> withBehaviorTypes,
        List<Type> withoutBehaviorTypes,
        List<string> withoutTags,
        List<string> withAnyTags,
        bool onlyActive,
        bool onlyEnabled,
        ECSOptions? options = null)
        : base(world, tags, predicates, withoutTypes, withBehaviorTypes, withoutBehaviorTypes, withoutTags, withAnyTags, onlyActive, onlyEnabled, options) { }

    internal CachedEntityQuery(IEntityWorld world)
        : this(world, [], [], [], [], [], [], [], true, false, null) { }

    private protected override IReadOnlyCollection<Type> RequiredTypes { get; } = [typeof(T1), typeof(T2), typeof(T3)];
    private protected override int CachedCount => _cachedPairs?.Count ?? 0;

    /// <summary>
    /// Executes the query and returns matching entities.
    /// The returned list is cached; no allocation occurs per call.
    /// </summary>
    public IReadOnlyList<Entity> Execute()
    {
        EnsureCache();
        return _entityProjection ??= new(_cachedPairs!, static pair => pair.Entity);
    }

    /// <summary>
    /// Executes an action for each cached entity and its components.
    /// Automatically parallelizes when entity count exceeds ECSOptions.ParallelEntityThreshold.
    /// </summary>
    /// <remarks>
    /// <b>Thread safety:</b> When parallelized, the <paramref name="action"/> delegate runs
    /// on multiple threads simultaneously. The caller is responsible for ensuring thread-safe
    /// access to any shared or mutable state, including component properties.
    /// </remarks>
    public void ForEach(Action<Entity, T1, T2, T3> action)
    {
        EnsureCache();

        var pairs = _cachedPairs!;
        var count = pairs.Count;
        if (count == 0) return;

        if (ShouldParallelize(count))
        {
            Parallel.For(0, count, _options.GetParallelOptions(), i =>
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

    /// <summary>
    /// Returns a struct enumerator over the cached (Entity, C1, C2, C3) tuples.
    /// Use with foreach for zero-allocation iteration; no delegate or closure is created.
    /// </summary>
    public List<(Entity Entity, T1 C1, T2 C2, T3 C3)>.Enumerator GetEnumerator()
    {
        EnsureCache();
        return (_cachedPairs ??= new()).GetEnumerator();
    }

    private protected override void EnsureCache()
    {
        ThrowIfDisposed();
        if (!_isDirty) return;

        (_cachedPairs ??= new()).Clear();

        if (_entityWorld != null)
        {
            int totalT1 = _entityWorld.GetTotalPoolCount(typeof(T1));
            int totalT2 = _entityWorld.GetTotalPoolCount(typeof(T2));
            int totalT3 = _entityWorld.GetTotalPoolCount(typeof(T3));

            if (totalT1 > 0 && totalT2 > 0 && totalT3 > 0)
            {
                int smallest = totalT1;
                Type smallestType = typeof(T1);
                if (totalT2 < smallest) { smallest = totalT2; smallestType = typeof(T2); }
                if (totalT3 < smallest) { smallest = totalT3; smallestType = typeof(T3); }

                _entityWorld.GetPoolsAssignableTo(smallestType, _poolBuffer);

                foreach (var pool in _poolBuffer)
                {
                    var (ids, length) = pool.GetEntityIdSnapshot();
                    try
                    {
                        for (int i = 0; i < length; i++)
                        {
                            var entity = _entityWorld.GetEntityById(ids[i]);
                            if (entity == null || (_onlyActive && !entity.IsActive) || !MatchesFilters(entity)) continue;
                            var c1 = _entityWorld.GetComponentFromPool<T1>(ids[i]);
                            if (c1 == null || (_onlyEnabled && !c1.IsEnabled)) continue;
                            var c2 = _entityWorld.GetComponentFromPool<T2>(ids[i]);
                            if (c2 == null || (_onlyEnabled && !c2.IsEnabled)) continue;
                            var c3 = _entityWorld.GetComponentFromPool<T3>(ids[i]);
                            if (c3 != null && (!_onlyEnabled || c3.IsEnabled))
                                _cachedPairs.Add((entity, c1, c2, c3));
                        }
                    }
                    finally { pool.ReturnEntityIdSnapshot(ids); }
                }
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
                    if (c1 != null && c2 != null && c3 != null &&
                        (!_onlyEnabled || (c1.IsEnabled && c2.IsEnabled && c3.IsEnabled)) &&
                        MatchesFilters(entity))
                        _cachedPairs.Add((entity, c1, c2, c3));
                }
            }
        }

        _isDirty = false;
    }
}

/// <summary>
/// Cached query for entities with four components.
/// Iterates the smallest pool and cross-resolves the remaining three.
/// </summary>
public class CachedEntityQuery<T1, T2, T3, T4> : CachedEntityQueryBase
    where T1 : Component
    where T2 : Component
    where T3 : Component
    where T4 : Component
{
    private List<(Entity Entity, T1 C1, T2 C2, T3 C3, T4 C4)>? _cachedPairs;
    private EntityProjection<(Entity Entity, T1 C1, T2 C2, T3 C3, T4 C4)>? _entityProjection;

    internal CachedEntityQuery(
        IEntityWorld world,
        List<string> tags,
        List<Func<Entity, bool>> predicates,
        List<Type> withoutTypes,
        List<Type> withBehaviorTypes,
        List<Type> withoutBehaviorTypes,
        List<string> withoutTags,
        List<string> withAnyTags,
        bool onlyActive,
        bool onlyEnabled,
        ECSOptions? options = null)
        : base(world, tags, predicates, withoutTypes, withBehaviorTypes, withoutBehaviorTypes, withoutTags, withAnyTags, onlyActive, onlyEnabled, options) { }

    internal CachedEntityQuery(IEntityWorld world)
        : this(world, [], [], [], [], [], [], [], true, false, null) { }

    private protected override IReadOnlyCollection<Type> RequiredTypes { get; } = [typeof(T1), typeof(T2), typeof(T3), typeof(T4)];
    private protected override int CachedCount => _cachedPairs?.Count ?? 0;

    /// <summary>
    /// Executes the query and returns matching entities.
    /// The returned list is cached; no allocation occurs per call.
    /// </summary>
    public IReadOnlyList<Entity> Execute()
    {
        EnsureCache();
        return _entityProjection ??= new(_cachedPairs!, static pair => pair.Entity);
    }

    /// <summary>
    /// Executes an action for each cached entity and its components.
    /// Automatically parallelizes when entity count exceeds ECSOptions.ParallelEntityThreshold.
    /// </summary>
    /// <remarks>
    /// <b>Thread safety:</b> When parallelized, the <paramref name="action"/> delegate runs
    /// on multiple threads simultaneously. The caller is responsible for ensuring thread-safe
    /// access to any shared or mutable state, including component properties.
    /// </remarks>
    public void ForEach(Action<Entity, T1, T2, T3, T4> action)
    {
        EnsureCache();

        var pairs = _cachedPairs!;
        var count = pairs.Count;
        if (count == 0) return;

        if (ShouldParallelize(count))
        {
            Parallel.For(0, count, _options.GetParallelOptions(), i =>
            {
                var (entity, c1, c2, c3, c4) = pairs[i];
                action(entity, c1, c2, c3, c4);
            });
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                var (entity, c1, c2, c3, c4) = pairs[i];
                action(entity, c1, c2, c3, c4);
            }
        }
    }

    /// <summary>
    /// Returns a struct enumerator over the cached (Entity, C1, C2, C3, C4) tuples.
    /// Use with foreach for zero-allocation iteration; no delegate or closure is created.
    /// </summary>
    public List<(Entity Entity, T1 C1, T2 C2, T3 C3, T4 C4)>.Enumerator GetEnumerator()
    {
        EnsureCache();
        return (_cachedPairs ??= new()).GetEnumerator();
    }

    private protected override void EnsureCache()
    {
        ThrowIfDisposed();
        if (!_isDirty) return;

        (_cachedPairs ??= new()).Clear();

        if (_entityWorld != null)
        {
            int totalT1 = _entityWorld.GetTotalPoolCount(typeof(T1));
            int totalT2 = _entityWorld.GetTotalPoolCount(typeof(T2));
            int totalT3 = _entityWorld.GetTotalPoolCount(typeof(T3));
            int totalT4 = _entityWorld.GetTotalPoolCount(typeof(T4));

            if (totalT1 > 0 && totalT2 > 0 && totalT3 > 0 && totalT4 > 0)
            {
                int smallest = totalT1;
                Type smallestType = typeof(T1);
                if (totalT2 < smallest) { smallest = totalT2; smallestType = typeof(T2); }
                if (totalT3 < smallest) { smallest = totalT3; smallestType = typeof(T3); }
                if (totalT4 < smallest) { smallest = totalT4; smallestType = typeof(T4); }

                _entityWorld.GetPoolsAssignableTo(smallestType, _poolBuffer);

                foreach (var pool in _poolBuffer)
                {
                    var (ids, length) = pool.GetEntityIdSnapshot();
                    try
                    {
                        for (int i = 0; i < length; i++)
                        {
                            var entity = _entityWorld.GetEntityById(ids[i]);
                            if (entity == null || (_onlyActive && !entity.IsActive) || !MatchesFilters(entity)) continue;
                            var c1 = _entityWorld.GetComponentFromPool<T1>(ids[i]);
                            if (c1 == null || (_onlyEnabled && !c1.IsEnabled)) continue;
                            var c2 = _entityWorld.GetComponentFromPool<T2>(ids[i]);
                            if (c2 == null || (_onlyEnabled && !c2.IsEnabled)) continue;
                            var c3 = _entityWorld.GetComponentFromPool<T3>(ids[i]);
                            if (c3 == null || (_onlyEnabled && !c3.IsEnabled)) continue;
                            var c4 = _entityWorld.GetComponentFromPool<T4>(ids[i]);
                            if (c4 != null && (!_onlyEnabled || c4.IsEnabled))
                                _cachedPairs.Add((entity, c1, c2, c3, c4));
                        }
                    }
                    finally { pool.ReturnEntityIdSnapshot(ids); }
                }
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
                    var c4 = entity.GetComponent<T4>();
                    if (c1 != null && c2 != null && c3 != null && c4 != null &&
                        (!_onlyEnabled || (c1.IsEnabled && c2.IsEnabled && c3.IsEnabled && c4.IsEnabled)) &&
                        MatchesFilters(entity))
                        _cachedPairs.Add((entity, c1, c2, c3, c4));
                }
            }
        }

        _isDirty = false;
    }
}