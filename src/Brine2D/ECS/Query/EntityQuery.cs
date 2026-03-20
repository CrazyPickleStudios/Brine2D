using System.Numerics;
using Brine2D.Core;
using Brine2D.Animation;
using Brine2D.ECS.Components;
using System.Buffers;

namespace Brine2D.ECS.Query;

/// <summary>
/// Represents an executable entity query with filtering criteria.
/// </summary>
public class EntityQuery
{
    private readonly IEntityWorld _world;
    private readonly EntityWorld? _entityWorld;
    private readonly ECSOptions _options;
    private readonly List<Type> _withComponents = new();
    private readonly List<Type> _withoutComponents = new();
    private readonly List<Type> _withBehaviors = new();
    private readonly List<Type> _withoutBehaviors = new();
    private readonly List<string> _withoutTags = new();
    private readonly List<string> _withAllTags = new();
    private readonly List<string> _withAnyTags = new();
    private readonly Dictionary<Type, Func<Component, bool>> _componentFilters = new();
    private readonly EntityFilterState _filterState;
    private Func<Entity, bool>? _predicate;
    private IComparer<Entity>? _orderComparer;
    private IComparer<Entity>? _thenByComparer;
    private int? _takeCount;
    private int? _skipCount;
    private bool _onlyActive = true;
    private bool _onlyEnabled;
    private Vector2? _spatialCenter;
    private float? _spatialRadius;
    private Rectangle? _spatialBounds;
    private readonly List<IComponentPool> _poolBuffer = new();

    private static readonly Func<Entity, bool> AlwaysTrue = static _ => true;

    internal EntityQuery(IEntityWorld world, ECSOptions? options = null)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _entityWorld = world as EntityWorld;
        _options = options ?? new ECSOptions();
        _filterState = new EntityFilterState(
            _withoutComponents, _withBehaviors, _withoutBehaviors,
            _withAllTags, _withoutTags, _withAnyTags);
    }

    /// <summary>
    /// Requires entities to have the specified component.
    /// Optionally filters by component property values.
    /// </summary>
    public EntityQuery With<T>(Func<T, bool>? filter = null) where T : Component
    {
        _withComponents.Add(typeof(T));
        if (filter != null)
            _componentFilters[typeof(T)] = c => filter((T)c);
        return this;
    }

    /// <summary>Requires entities to NOT have the specified component.</summary>
    public EntityQuery Without<T>() where T : Component
    {
        _withoutComponents.Add(typeof(T));
        return this;
    }

    /// <summary>Requires entities to have the specified behavior.</summary>
    public EntityQuery WithBehavior<T>() where T : EntityBehavior
    {
        _withBehaviors.Add(typeof(T));
        return this;
    }

    /// <summary>Requires entities to NOT have the specified behavior.</summary>
    public EntityQuery WithoutBehavior<T>() where T : EntityBehavior
    {
        _withoutBehaviors.Add(typeof(T));
        return this;
    }

    /// <summary>Requires entities to have the specified tag.</summary>
    public EntityQuery WithTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        _withAllTags.Add(tag);
        return this;
    }

    /// <summary>Requires entities to NOT have the specified tag.</summary>
    public EntityQuery WithoutTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        _withoutTags.Add(tag);
        return this;
    }

    /// <summary>Requires entities to have ALL of the specified tags.</summary>
    public EntityQuery WithAllTags(params string[] tags)
    {
        foreach (var tag in tags)
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        _withAllTags.AddRange(tags);
        return this;
    }

    /// <summary>Requires entities to have AT LEAST ONE of the specified tags.</summary>
    public EntityQuery WithAnyTag(params string[] tags)
    {
        foreach (var tag in tags)
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        _withAnyTags.AddRange(tags);
        return this;
    }

    /// <summary>
    /// Filters entities within a circular radius from a center point.
    /// Requires entities to have a TransformComponent.
    /// Can be combined with <see cref="WithinBounds"/> for compound spatial filtering.
    /// </summary>
    public EntityQuery WithinRadius(Vector2 center, float radius)
    {
        _spatialCenter = center;
        _spatialRadius = radius;
        if (!_withComponents.Contains(typeof(TransformComponent)))
            _withComponents.Add(typeof(TransformComponent));
        return this;
    }

    /// <summary>
    /// Filters entities within a rectangular bounds.
    /// Requires entities to have a TransformComponent.
    /// Can be combined with <see cref="WithinRadius"/> for compound spatial filtering.
    /// </summary>
    public EntityQuery WithinBounds(Rectangle bounds)
    {
        _spatialBounds = bounds;
        if (!_withComponents.Contains(typeof(TransformComponent)))
            _withComponents.Add(typeof(TransformComponent));
        return this;
    }

    /// <summary>Adds a custom filter predicate.</summary>
    public EntityQuery Where(Func<Entity, bool> predicate)
    {
        if (_predicate == null)
            _predicate = predicate;
        else
        {
            var existing = _predicate;
            _predicate = e => existing(e) && predicate(e);
        }
        return this;
    }

    /// <summary>Orders results by the specified selector in ascending order.</summary>
    public EntityQuery OrderBy<TKey>(Func<Entity, TKey> selector)
    {
        _orderComparer = Comparer<Entity>.Create((a, b) =>
            Comparer<TKey>.Default.Compare(selector(a), selector(b)));
        return this;
    }

    /// <summary>Orders results by the specified selector in descending order.</summary>
    public EntityQuery OrderByDescending<TKey>(Func<Entity, TKey> selector)
    {
        _orderComparer = Comparer<Entity>.Create((a, b) =>
            Comparer<TKey>.Default.Compare(selector(b), selector(a)));
        return this;
    }

    /// <summary>Applies a secondary sort criteria (tie-breaker).</summary>
    public EntityQuery ThenBy<TKey>(Func<Entity, TKey> selector)
    {
        _thenByComparer = Comparer<Entity>.Create((a, b) =>
            Comparer<TKey>.Default.Compare(selector(a), selector(b)));
        return this;
    }

    /// <summary>Applies a secondary sort criteria in descending order.</summary>
    public EntityQuery ThenByDescending<TKey>(Func<Entity, TKey> selector)
    {
        _thenByComparer = Comparer<Entity>.Create((a, b) =>
            Comparer<TKey>.Default.Compare(selector(b), selector(a)));
        return this;
    }

    /// <summary>Takes only the first N entities from the results.</summary>
    public EntityQuery Take(int count)
    {
        _takeCount = count;
        return this;
    }

    /// <summary>Skips the first N entities from the results.</summary>
    public EntityQuery Skip(int count)
    {
        _skipCount = count;
        return this;
    }

    /// <summary>Includes only active entities (default behavior).</summary>
    public EntityQuery OnlyActive()
    {
        _onlyActive = true;
        return this;
    }

    /// <summary>Includes both active and inactive entities.</summary>
    public EntityQuery IncludeInactive()
    {
        _onlyActive = false;
        return this;
    }

    /// <summary>
    /// Filters to entities where all required components have <see cref="Component.IsEnabled"/>
    /// set to <see langword="true"/>. Only the components specified via <c>.With&lt;T&gt;()</c>
    /// are checked.
    /// </summary>
    /// <remarks>
    /// This is distinct from <see cref="OnlyActive"/>: <c>OnlyActive()</c> filters by
    /// <see cref="Entity.IsActive"/> (entity-level), while <c>OnlyEnabled()</c> filters by
    /// <see cref="Component.IsEnabled"/> (component-level). Resolves each required component
    /// once per entity, so prefer <see cref="CachedEntityQuery{T1}"/> with <c>OnlyEnabled()</c>
    /// for per-frame queries where components are already pre-resolved.
    /// </remarks>
    public EntityQuery OnlyEnabled()
    {
        _onlyEnabled = true;
        return this;
    }

    /// <summary>
    /// Creates a copy of this query that can be further modified without affecting the original.
    /// All state is copied except random state, which is lazily initialized per clone.
    /// </summary>
    public EntityQuery Clone()
    {
        var clone = new EntityQuery(_world, _options);
        clone._withComponents.AddRange(_withComponents);
        clone._withoutComponents.AddRange(_withoutComponents);
        clone._withBehaviors.AddRange(_withBehaviors);
        clone._withoutBehaviors.AddRange(_withoutBehaviors);
        clone._withoutTags.AddRange(_withoutTags);
        clone._withAllTags.AddRange(_withAllTags);
        clone._withAnyTags.AddRange(_withAnyTags);
        foreach (var kvp in _componentFilters)
            clone._componentFilters[kvp.Key] = kvp.Value;
        clone._predicate = _predicate;
        clone._onlyActive = _onlyActive;
        clone._onlyEnabled = _onlyEnabled;
        clone._spatialCenter = _spatialCenter;
        clone._spatialRadius = _spatialRadius;
        clone._spatialBounds = _spatialBounds;
        clone._orderComparer = _orderComparer;
        clone._thenByComparer = _thenByComparer;
        clone._takeCount = _takeCount;
        clone._skipCount = _skipCount;
        return clone;
    }

    /// <summary>
    /// Executes the query and returns matching entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pool-based path rents an <see cref="ArrayPool{T}"/> buffer that is returned
    /// when the enumerator is disposed. Standard <c>foreach</c> and LINQ methods handle
    /// this automatically. Avoid calling <c>GetEnumerator()</c> without ensuring disposal.
    /// </para>
    /// <para>
    /// This query re-evaluates filters on every call. For per-frame iteration in systems,
    /// prefer <see cref="CachedEntityQuery{T1}"/> (via <see cref="IEntityWorld.CreateCachedQuery{T1}"/>),
    /// which rebuilds only when structural changes invalidate the cache.
    /// </para>
    /// </remarks>
    public IEnumerable<Entity> Execute()
    {
        if (_orderComparer == null && !_skipCount.HasValue && !_takeCount.HasValue)
            return ExecuteCore();

        IEnumerable<Entity> results = ExecuteCore();

        if (_orderComparer != null)
        {
            IComparer<Entity> comparer = _thenByComparer != null
                ? Comparer<Entity>.Create((a, b) =>
                {
                    int r = _orderComparer.Compare(a, b);
                    return r != 0 ? r : _thenByComparer.Compare(a, b);
                })
                : _orderComparer;

            results = results.Order(comparer);
        }

        if (_skipCount.HasValue) results = results.Skip(_skipCount.Value);
        if (_takeCount.HasValue) results = results.Take(_takeCount.Value);

        return results;
    }

    /// <summary>
    /// Dispatches to the pool-based path when the world is an <see cref="EntityWorld"/>
    /// and at least one <c>.With&lt;T&gt;()</c> filter is present; otherwise falls back to
    /// a full entity-list scan.
    /// </summary>
    private IEnumerable<Entity> ExecuteCore()
    {
        if (_entityWorld != null && _withComponents.Count > 0)
            return ExecuteCorePoolBased(_entityWorld);
        return ExecuteCoreEntityBased();
    }

    /// <summary>
    /// Iterates the smallest required-component pool and checks remaining filters.
    /// Touches only entities that have at least one required component, not the full entity list.
    /// </summary>
    private IEnumerable<Entity> ExecuteCorePoolBased(EntityWorld entityWorld)
    {
        Type? smallestType = null;
        int smallestTotal = int.MaxValue;
        foreach (var type in _withComponents)
        {
            int total = entityWorld.GetTotalPoolCount(type);
            if (total == 0) yield break;
            if (total < smallestTotal)
            {
                smallestTotal = total;
                smallestType = type;
            }
        }

        if (smallestType == null) yield break;

        // Allocating overload is intentional: yield-return iterators have complex lifetimes
        // that prevent safe use of a reusable buffer field. For per-frame use, prefer
        // CachedEntityQuery which uses the buffer-accepting overload.
        var pools = entityWorld.GetPoolsAssignableTo(smallestType);
        foreach (var pool in pools)
        {
            var (ids, length) = pool.GetEntityIdSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var entity = entityWorld.GetEntityById(ids[i]);
                    if (entity == null) continue;
                    if (_onlyActive && !entity.IsActive) continue;
                    if (!ApplyFilters(entity)) continue;
                    yield return entity;
                }
            }
            finally
            {
                pool.ReturnEntityIdSnapshot(ids);
            }
        }
    }

    private IEnumerable<Entity> ExecuteCoreEntityBased()
    {
        var entities = _world.Entities;
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity == null) continue;
            if (_onlyActive && !entity.IsActive) continue;
            if (!ApplyFilters(entity)) continue;
            yield return entity;
        }
    }
    
    /// <summary>Executes the query and returns the first matching entity, or null.</summary>
    public Entity? First() => Execute().FirstOrDefault();

    /// <summary>Executes the query and returns a single random matching entity, or null.</summary>
    /// <remarks>
    /// Uses reservoir sampling to avoid materializing the full result set.
    /// </remarks>
    public Entity? Random()
    {
        Entity? chosen = null;
        int seen = 0;
        foreach (var entity in Execute())
        {
            seen++;
            if (System.Random.Shared.Next(seen) == 0)
                chosen = entity;
        }
        return chosen;
    }

    /// <summary>Executes the query and returns N random matching entities.</summary>
    /// <remarks>
    /// Uses reservoir sampling (Algorithm R) to select a uniformly random subset
    /// without materializing the full result set.
    /// </remarks>
    public List<Entity> Random(int count)
    {
        var reservoir = new List<Entity>(count);
        int seen = 0;
        foreach (var entity in Execute())
        {
            seen++;
            if (reservoir.Count < count)
            {
                reservoir.Add(entity);
            }
            else
            {
                int j = System.Random.Shared.Next(seen);
                if (j < count)
                    reservoir[j] = entity;
            }
        }
        return reservoir;
    }

    /// <summary>Executes the query and returns the count of matching entities.</summary>
    public int Count()
    {
        if (!_skipCount.HasValue && !_takeCount.HasValue && TryGetPoolCount(out int poolCount))
            return poolCount;

        IEnumerable<Entity> results = ExecuteCore();
        if (_skipCount.HasValue) results = results.Skip(_skipCount.Value);
        if (_takeCount.HasValue) results = results.Take(_takeCount.Value);

        int count = 0;
        foreach (var _ in results)
            count++;
        return count;
    }

    /// <summary>Executes the query and checks if any entities match.</summary>
    public bool Any()
    {
        if (!_skipCount.HasValue && !_takeCount.HasValue && TryGetPoolCount(out int poolCount))
            return poolCount > 0;

        IEnumerable<Entity> results = ExecuteCore();
        if (_skipCount.HasValue) results = results.Skip(_skipCount.Value);
        if (_takeCount.HasValue) results = results.Take(_takeCount.Value);
        return results.Any();
    }

    /// <summary>Executes an action on each matching entity.</summary>
    public void ForEach(Action<Entity> action)
    {
        ForEachOptimized(filter: AlwaysTrue, execute: action);
    }

    /// <summary>
    /// Executes an action for each entity with 1 component.
    /// Iterates all assignable component pools; supports polymorphic queries.
    /// </summary>
    public void ForEach<T1>(Action<Entity, T1> action)
        where T1 : Component
    {
        if (_entityWorld == null)
        {
            ForEachOptimized(
                filter: e => e.HasComponent<T1>(),
                execute: e => { var c1 = e.GetComponent<T1>(); if (c1 != null) action(e, c1); });
            return;
        }

        _entityWorld.GetPoolsAssignableTo(typeof(T1), _poolBuffer);
        if (_poolBuffer.Count == 0) return;

        foreach (var pool in _poolBuffer)
        {
            var (ids, length) = pool.GetEntityIdSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var entity = _entityWorld.GetEntityById(ids[i]);
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c1 = (T1?)pool.Get(ids[i]);
                    if (c1 != null && ApplyFilters(entity))
                        action(entity, c1);
                }
            }
            finally { pool.ReturnEntityIdSnapshot(ids); }
        }
    }

    /// <summary>
    /// Executes an action for each entity with 2 components.
    /// Iterates the smaller pool set for efficiency; supports polymorphic queries.
    /// </summary>
    public void ForEach<T1, T2>(Action<Entity, T1, T2> action)
        where T1 : Component
        where T2 : Component
    {
        if (_entityWorld == null)
        {
            ForEachOptimized(
                filter: e => e.HasComponent<T1>() && e.HasComponent<T2>(),
                execute: e =>
                {
                    var c1 = e.GetComponent<T1>(); var c2 = e.GetComponent<T2>();
                    if (c1 != null && c2 != null) action(e, c1, c2);
                });
            return;
        }

        int totalT1 = _entityWorld.GetTotalPoolCount(typeof(T1));
        int totalT2 = _entityWorld.GetTotalPoolCount(typeof(T2));
        if (totalT1 == 0 || totalT2 == 0) return;

        _entityWorld.GetPoolsAssignableTo(totalT1 <= totalT2 ? typeof(T1) : typeof(T2), _poolBuffer);
        foreach (var pool in _poolBuffer)
        {
            var (ids, length) = pool.GetEntityIdSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var entity = _entityWorld.GetEntityById(ids[i]);
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c1 = _entityWorld.GetComponentFromPool<T1>(ids[i]);
                    if (c1 == null) continue;
                    var c2 = _entityWorld.GetComponentFromPool<T2>(ids[i]);
                    if (c2 != null && ApplyFilters(entity)) action(entity, c1, c2);
                }
            }
            finally { pool.ReturnEntityIdSnapshot(ids); }
        }
    }

    /// <summary>
    /// Executes an action for each entity with 3 components.
    /// Iterates the smallest pool set; supports polymorphic queries.
    /// </summary>
    public void ForEach<T1, T2, T3>(Action<Entity, T1, T2, T3> action)
        where T1 : Component
        where T2 : Component
        where T3 : Component
    {
        if (_entityWorld == null)
        {
            ForEachOptimized(
                filter: e => e.HasComponent<T1>() && e.HasComponent<T2>() && e.HasComponent<T3>(),
                execute: e =>
                {
                    var c1 = e.GetComponent<T1>(); var c2 = e.GetComponent<T2>(); var c3 = e.GetComponent<T3>();
                    if (c1 != null && c2 != null && c3 != null) action(e, c1, c2, c3);
                });
            return;
        }

        int totalT1 = _entityWorld.GetTotalPoolCount(typeof(T1));
        int totalT2 = _entityWorld.GetTotalPoolCount(typeof(T2));
        int totalT3 = _entityWorld.GetTotalPoolCount(typeof(T3));
        if (totalT1 == 0 || totalT2 == 0 || totalT3 == 0) return;

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
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c1 = _entityWorld.GetComponentFromPool<T1>(ids[i]);
                    if (c1 == null) continue;
                    var c2 = _entityWorld.GetComponentFromPool<T2>(ids[i]);
                    if (c2 == null) continue;
                    var c3 = _entityWorld.GetComponentFromPool<T3>(ids[i]);
                    if (c3 != null && ApplyFilters(entity)) action(entity, c1, c2, c3);
                }
            }
            finally { pool.ReturnEntityIdSnapshot(ids); }
        }
    }

    /// <summary>
    /// Executes an action for each entity with 4 components.
    /// Iterates the smallest pool set; supports polymorphic queries.
    /// </summary>
    public void ForEach<T1, T2, T3, T4>(Action<Entity, T1, T2, T3, T4> action)
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
    {
        if (_entityWorld == null)
        {
            ForEachOptimized(
                filter: e => e.HasComponent<T1>() && e.HasComponent<T2>() &&
                             e.HasComponent<T3>() && e.HasComponent<T4>(),
                execute: e =>
                {
                    var c1 = e.GetComponent<T1>(); var c2 = e.GetComponent<T2>();
                    var c3 = e.GetComponent<T3>(); var c4 = e.GetComponent<T4>();
                    if (c1 != null && c2 != null && c3 != null && c4 != null) action(e, c1, c2, c3, c4);
                });
            return;
        }

        int totalT1 = _entityWorld.GetTotalPoolCount(typeof(T1));
        int totalT2 = _entityWorld.GetTotalPoolCount(typeof(T2));
        int totalT3 = _entityWorld.GetTotalPoolCount(typeof(T3));
        int totalT4 = _entityWorld.GetTotalPoolCount(typeof(T4));
        if (totalT1 == 0 || totalT2 == 0 || totalT3 == 0 || totalT4 == 0) return;

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
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c1 = _entityWorld.GetComponentFromPool<T1>(ids[i]);
                    if (c1 == null) continue;
                    var c2 = _entityWorld.GetComponentFromPool<T2>(ids[i]);
                    if (c2 == null) continue;
                    var c3 = _entityWorld.GetComponentFromPool<T3>(ids[i]);
                    if (c3 == null) continue;
                    var c4 = _entityWorld.GetComponentFromPool<T4>(ids[i]);
                    if (c4 != null && ApplyFilters(entity)) action(entity, c1, c2, c3, c4);
                }
            }
            finally { pool.ReturnEntityIdSnapshot(ids); }
        }
    }

    /// <summary>
    /// Core optimized ForEach using ArrayPool for zero allocation.
    /// Used by the non-generic overload and the non-EntityWorld fallback path.
    /// </summary>
    private void ForEachOptimized(Func<Entity, bool> filter, Action<Entity> execute, bool forceParallel = false)
    {
        var snapshot = _world.Entities;
        var buffer = ArrayPool<Entity>.Shared.Rent(snapshot.Count);
        int count = 0;

        try
        {
            foreach (var entity in snapshot)
            {
                if (_onlyActive && !entity.IsActive) continue;
                if (!filter(entity)) continue;
                if (!ApplyFilters(entity)) continue;
                buffer[count++] = entity;
            }

            bool useParallel = forceParallel ||
                (_options.EnableMultiThreading && count >= _options.ParallelEntityThreshold);

            if (useParallel)
            {
                Parallel.For(0, count, _options.GetParallelOptions(), i => execute(buffer[i]));
            }
            else
            {
                for (int i = 0; i < count; i++)
                    execute(buffer[i]);
            }
        }
        finally
        {
            ArrayPool<Entity>.Shared.Return(buffer, clearArray: true);
        }
    }

    /// <summary>
    /// Fast path for <see cref="Count"/>/<see cref="Any"/>: returns the pool count directly
    /// when the query has a single required component, <see cref="IncludeInactive"/> is set,
    /// and no other filters would reduce the result set.
    /// </summary>
    private bool TryGetPoolCount(out int count)
    {
        count = 0;
        if (_onlyActive || _onlyEnabled || _predicate != null ||
            _withComponents.Count != 1 || _withoutComponents.Count > 0 ||
            _componentFilters.Count > 0 ||
            _withBehaviors.Count > 0 || _withoutBehaviors.Count > 0 ||
            _withAllTags.Count > 0 || _withAnyTags.Count > 0 || _withoutTags.Count > 0 ||
            _spatialCenter.HasValue || _spatialBounds.HasValue ||
            _entityWorld == null)
            return false;

        count = _entityWorld.GetTotalPoolCount(_withComponents[0]);
        return true;
    }

    /// <summary>
    /// Applies all query filters (component requirements, exclusions, behaviors, tags,
    /// enabled state, spatial, predicates).
    /// Active-status checks are handled per-overload for performance.
    /// </summary>
    private bool ApplyFilters(Entity entity)
    {
        for (int i = 0; i < _withComponents.Count; i++)
        {
            var type = _withComponents[i];
            _componentFilters.TryGetValue(type, out var filter);

            if (_onlyEnabled || filter != null)
            {
                var component = _entityWorld != null
                    ? _entityWorld.GetComponentOfType(entity.Id, type)
                    : entity.GetAllComponents().FirstOrDefault(c => type.IsInstanceOfType(c));
                if (component == null) return false;
                if (_onlyEnabled && !component.IsEnabled) return false;
                if (filter != null && !filter(component)) return false;
            }
            else
            {
                if (!entity.HasComponent(type)) return false;
            }
        }

        if (!_filterState.Matches(entity)) return false;

        if (_spatialCenter.HasValue || _spatialBounds.HasValue)
        {
            var transform = entity.GetComponent<TransformComponent>();
            if (transform == null) return false;

            if (_spatialCenter.HasValue && _spatialRadius.HasValue)
            {
                if (Vector2.Distance(transform.Position, _spatialCenter.Value) > _spatialRadius.Value)
                    return false;
            }

            if (_spatialBounds.HasValue)
            {
                var bounds = _spatialBounds.Value;
                var pos = transform.Position;
                if (pos.X < bounds.X || pos.X > bounds.X + bounds.Width ||
                    pos.Y < bounds.Y || pos.Y > bounds.Y + bounds.Height)
                    return false;
            }
        }

        if (_predicate != null && !_predicate(entity)) return false;
        return true;
    }
}