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
    private readonly ECSOptions _options;
    private readonly List<Type> _withComponents = new();
    private readonly List<Type> _withoutComponents = new();
    private readonly List<string> _withTags = new();
    private readonly List<string> _withoutTags = new();
    private readonly List<string> _withAllTags = new();
    private readonly List<string> _withAnyTags = new();
    private readonly Dictionary<Type, Func<Component, bool>> _componentFilters = new();
    private Func<Entity, bool>? _predicate;
    private Func<Entity, object>? _orderBySelector;
    private Func<Entity, object>? _thenBySelector;
    private bool _orderDescending;
    private bool _thenByDescending;
    private int? _takeCount;
    private int? _skipCount;
    private bool _onlyActive = true;
    private Vector2? _spatialCenter;
    private float? _spatialRadius;
    private Rectangle? _spatialBounds;
    private Random? _random;

    internal EntityQuery(IEntityWorld world, ECSOptions? options = null)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _options = options ?? new ECSOptions();
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

    /// <summary>Requires entities to have the specified tag.</summary>
    public EntityQuery WithTag(string tag)
    {
        _withTags.Add(tag);
        return this;
    }

    /// <summary>Requires entities to NOT have the specified tag.</summary>
    public EntityQuery WithoutTag(string tag)
    {
        _withoutTags.Add(tag);
        return this;
    }

    /// <summary>Requires entities to have ALL of the specified tags.</summary>
    public EntityQuery WithAllTags(params string[] tags)
    {
        _withAllTags.AddRange(tags);
        return this;
    }

    /// <summary>Requires entities to have AT LEAST ONE of the specified tags.</summary>
    public EntityQuery WithAnyTag(params string[] tags)
    {
        _withAnyTags.AddRange(tags);
        return this;
    }

    /// <summary>
    /// Filters entities within a circular radius from a center point.
    /// Requires entities to have a TransformComponent.
    /// </summary>
    public EntityQuery WithinRadius(Vector2 center, float radius)
    {
        _spatialCenter = center;
        _spatialRadius = radius;
        _spatialBounds = null;
        return this;
    }

    /// <summary>
    /// Filters entities within a rectangular bounds.
    /// Requires entities to have a TransformComponent.
    /// </summary>
    public EntityQuery WithinBounds(Rectangle bounds)
    {
        _spatialBounds = bounds;
        _spatialCenter = null;
        _spatialRadius = null;
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
        _orderBySelector = e => selector(e)!;
        _orderDescending = false;
        return this;
    }

    /// <summary>Orders results by the specified selector in descending order.</summary>
    public EntityQuery OrderByDescending<TKey>(Func<Entity, TKey> selector)
    {
        _orderBySelector = e => selector(e)!;
        _orderDescending = true;
        return this;
    }

    /// <summary>Applies a secondary sort criteria (tie-breaker).</summary>
    public EntityQuery ThenBy<TKey>(Func<Entity, TKey> selector)
    {
        _thenBySelector = e => selector(e)!;
        _thenByDescending = false;
        return this;
    }

    /// <summary>Applies a secondary sort criteria in descending order.</summary>
    public EntityQuery ThenByDescending<TKey>(Func<Entity, TKey> selector)
    {
        _thenBySelector = e => selector(e)!;
        _thenByDescending = true;
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
    /// Creates a copy of this query that can be further modified without affecting the original.
    /// Note: Does not copy ordering, take, skip, or random state.
    /// </summary>
    public EntityQuery Clone()
    {
        var clone = new EntityQuery(_world, _options);
        clone._withComponents.AddRange(_withComponents);
        clone._withoutComponents.AddRange(_withoutComponents);
        clone._withTags.AddRange(_withTags);
        clone._withoutTags.AddRange(_withoutTags);
        clone._withAllTags.AddRange(_withAllTags);
        clone._withAnyTags.AddRange(_withAnyTags);
        foreach (var kvp in _componentFilters)
            clone._componentFilters[kvp.Key] = kvp.Value;
        clone._predicate = _predicate;
        clone._onlyActive = _onlyActive;
        clone._spatialCenter = _spatialCenter;
        clone._spatialRadius = _spatialRadius;
        clone._spatialBounds = _spatialBounds;
        clone._orderBySelector = _orderBySelector;
        clone._orderDescending = _orderDescending;
        clone._thenBySelector = _thenBySelector;
        clone._thenByDescending = _thenByDescending;
        clone._takeCount = _takeCount;
        clone._skipCount = _skipCount;
        // _random intentionally not copied; each clone gets independent lazy-init random state
        return clone;
    }

    /// <summary>
    /// Executes the query and returns matching entities.
    /// </summary>
    public IEnumerable<Entity> Execute()
    {
        if (_orderBySelector == null && !_skipCount.HasValue && !_takeCount.HasValue)
            return ExecuteCore();

        IEnumerable<Entity> results = ExecuteCore();

        if (_orderBySelector != null)
        {
            IOrderedEnumerable<Entity> ordered = _orderDescending
                ? results.OrderByDescending(_orderBySelector)
                : results.OrderBy(_orderBySelector);

            if (_thenBySelector != null)
                ordered = _thenByDescending
                    ? ordered.ThenByDescending(_thenBySelector)
                    : ordered.ThenBy(_thenBySelector);

            results = ordered;
        }

        if (_skipCount.HasValue) results = results.Skip(_skipCount.Value);
        if (_takeCount.HasValue) results = results.Take(_takeCount.Value);

        return results;
    }

    private IEnumerable<Entity> ExecuteCore()
    {
        var entities = _world.Entities;
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity == null) continue;
            if (_onlyActive && !entity.IsActive) continue;
            if (!MatchesRequiredComponents(entity)) continue;
            if (!ApplyFilters(entity)) continue;
            yield return entity;
        }
    }

    private bool MatchesRequiredComponents(Entity entity)
    {
        for (int i = 0; i < _withComponents.Count; i++)
            if (!entity.HasComponent(_withComponents[i])) return false;
        return true;
    }

    /// <summary>Executes the query and returns the first matching entity, or null.</summary>
    public Entity? First() => Execute().FirstOrDefault();

    /// <summary>Executes the query and returns a single random matching entity, or null.</summary>
    public Entity? Random()
    {
        var count = Execute().Count();
        if (count == 0) return null;

        var array = ArrayPool<Entity>.Shared.Rent(count);
        try
        {
            int index = 0;
            foreach (var entity in Execute())
                array[index++] = entity;

            _random ??= new Random();
            return array[_random.Next(count)];
        }
        finally
        {
            ArrayPool<Entity>.Shared.Return(array, clearArray: true);
        }
    }

    /// <summary>Executes the query and returns N random matching entities.</summary>
    public EntityQuery Random(int count)
    {
        _random ??= new Random();
        var results = Execute().ToList();

        for (int i = results.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (results[i], results[j]) = (results[j], results[i]);
        }

        return Take(Math.Min(count, results.Count));
    }

    /// <summary>Executes the query and returns the count of matching entities.</summary>
    public int Count() => Execute().Count();

    /// <summary>Executes the query and checks if any entities match.</summary>
    public bool Any() => Execute().Any();

    /// <summary>Executes an action on each matching entity.</summary>
    public void ForEach(Action<Entity> action)
    {
        ForEachOptimized(filter: _ => true, execute: action);
    }

    /// <summary>
    /// Executes an action for each entity with 1 component.
    /// Iterates the component pool directly; only visits entities that have T1.
    /// </summary>
    public void ForEach<T1>(Action<Entity, T1> action)
        where T1 : Component
    {
        if (_world is not EntityWorld entityWorld)
        {
            ForEachOptimized(
                filter: e => e.HasComponent<T1>(),
                execute: e => { var c1 = e.GetComponent<T1>(); if (c1 != null) action(e, c1); });
            return;
        }

        var pool = entityWorld.GetOrCreatePool<T1>();
        var (snapshot, length) = pool.GetSnapshot();
        try
        {
            for (int i = 0; i < length; i++)
            {
                var (entityId, c1) = ((ValueTuple<int, T1>[])snapshot)[i];
                var entity = entityWorld.GetEntityById(entityId);
                if (entity != null && (!_onlyActive || entity.IsActive) && ApplyFilters(entity))
                    action(entity, c1);
            }
        }
        finally { pool.ReturnSnapshot(snapshot); }
    }

    /// <summary>
    /// Executes an action for each entity with 2 components.
    /// Iterates the smaller pool for efficiency.
    /// </summary>
    public void ForEach<T1, T2>(Action<Entity, T1, T2> action)
        where T1 : Component
        where T2 : Component
    {
        if (_world is not EntityWorld entityWorld)
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
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c2 = entity.GetComponent<T2>();
                    if (c2 != null && ApplyFilters(entity)) action(entity, c1, c2);
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
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c1 = entity.GetComponent<T1>();
                    if (c1 != null && ApplyFilters(entity)) action(entity, c1, c2);
                }
            }
            finally { pool2.ReturnSnapshot(snapshot); }
        }
    }

    /// <summary>
    /// Executes an action for each entity with 3 components.
    /// Iterates the smallest pool.
    /// </summary>
    public void ForEach<T1, T2, T3>(Action<Entity, T1, T2, T3> action)
        where T1 : Component
        where T2 : Component
        where T3 : Component
    {
        if (_world is not EntityWorld entityWorld)
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
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c2 = entity.GetComponent<T2>(); var c3 = entity.GetComponent<T3>();
                    if (c2 != null && c3 != null && ApplyFilters(entity)) action(entity, c1, c2, c3);
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
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c1 = entity.GetComponent<T1>(); var c3 = entity.GetComponent<T3>();
                    if (c1 != null && c3 != null && ApplyFilters(entity)) action(entity, c1, c2, c3);
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
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c1 = entity.GetComponent<T1>(); var c2 = entity.GetComponent<T2>();
                    if (c1 != null && c2 != null && ApplyFilters(entity)) action(entity, c1, c2, c3);
                }
            }
            finally { pool3.ReturnSnapshot(snapshot); }
        }
    }

    /// <summary>
    /// Executes an action for each entity with 4 components.
    /// Iterates the smallest pool.
    /// </summary>
    public void ForEach<T1, T2, T3, T4>(Action<Entity, T1, T2, T3, T4> action)
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
    {
        if (_world is not EntityWorld entityWorld)
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

        var pool1 = entityWorld.GetOrCreatePool<T1>();
        var pool2 = entityWorld.GetOrCreatePool<T2>();
        var pool3 = entityWorld.GetOrCreatePool<T3>();
        var pool4 = entityWorld.GetOrCreatePool<T4>();
        var min = Math.Min(pool1.Count, Math.Min(pool2.Count, Math.Min(pool3.Count, pool4.Count)));

        if (min == pool1.Count)
        {
            var (snapshot, length) = pool1.GetSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, c1) = ((ValueTuple<int, T1>[])snapshot)[i];
                    var entity = entityWorld.GetEntityById(entityId);
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c2 = entity.GetComponent<T2>(); var c3 = entity.GetComponent<T3>(); var c4 = entity.GetComponent<T4>();
                    if (c2 != null && c3 != null && c4 != null && ApplyFilters(entity)) action(entity, c1, c2, c3, c4);
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
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c1 = entity.GetComponent<T1>(); var c3 = entity.GetComponent<T3>(); var c4 = entity.GetComponent<T4>();
                    if (c1 != null && c3 != null && c4 != null && ApplyFilters(entity)) action(entity, c1, c2, c3, c4);
                }
            }
            finally { pool2.ReturnSnapshot(snapshot); }
        }
        else if (min == pool3.Count)
        {
            var (snapshot, length) = pool3.GetSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, c3) = ((ValueTuple<int, T3>[])snapshot)[i];
                    var entity = entityWorld.GetEntityById(entityId);
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c1 = entity.GetComponent<T1>(); var c2 = entity.GetComponent<T2>(); var c4 = entity.GetComponent<T4>();
                    if (c1 != null && c2 != null && c4 != null && ApplyFilters(entity)) action(entity, c1, c2, c3, c4);
                }
            }
            finally { pool3.ReturnSnapshot(snapshot); }
        }
        else
        {
            var (snapshot, length) = pool4.GetSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, c4) = ((ValueTuple<int, T4>[])snapshot)[i];
                    var entity = entityWorld.GetEntityById(entityId);
                    if (entity == null || (_onlyActive && !entity.IsActive)) continue;
                    var c1 = entity.GetComponent<T1>(); var c2 = entity.GetComponent<T2>(); var c3 = entity.GetComponent<T3>();
                    if (c1 != null && c2 != null && c3 != null && ApplyFilters(entity)) action(entity, c1, c2, c3, c4);
                }
            }
            finally { pool4.ReturnSnapshot(snapshot); }
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
                Parallel.For(0, count, new ParallelOptions
                {
                    MaxDegreeOfParallelism = _options.WorkerThreadCount ?? -1
                }, i => execute(buffer[i]));
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
    /// Applies all query filters except active-status and component-existence checks
    /// (those are handled per-overload for performance).
    /// </summary>
    private bool ApplyFilters(Entity entity)
    {
        for (int i = 0; i < _withoutComponents.Count; i++)
            if (entity.HasComponent(_withoutComponents[i])) return false;

        foreach (var kvp in _componentFilters)
        {
            var component = entity.GetAllComponents().FirstOrDefault(c => kvp.Key.IsInstanceOfType(c));
            if (component == null || !kvp.Value(component)) return false;
        }

        for (int i = 0; i < _withTags.Count; i++)
            if (!entity.Tags.Contains(_withTags[i])) return false;

        for (int i = 0; i < _withoutTags.Count; i++)
            if (entity.Tags.Contains(_withoutTags[i])) return false;

        if (_withAllTags.Count > 0)
        {
            for (int i = 0; i < _withAllTags.Count; i++)
                if (!entity.Tags.Contains(_withAllTags[i])) return false;
        }

        if (_withAnyTags.Count > 0)
        {
            bool found = false;
            for (int i = 0; i < _withAnyTags.Count; i++)
            {
                if (entity.Tags.Contains(_withAnyTags[i]))
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }

        if (_spatialCenter.HasValue && _spatialRadius.HasValue)
        {
            var transform = entity.GetComponent<TransformComponent>();
            if (transform == null) return false;
            if (Vector2.Distance(transform.Position, _spatialCenter.Value) > _spatialRadius.Value) return false;
        }

        if (_spatialBounds.HasValue)
        {
            var transform = entity.GetComponent<TransformComponent>();
            if (transform == null) return false;
            var bounds = _spatialBounds.Value;
            var pos = transform.Position;
            if (pos.X < bounds.X || pos.X > bounds.X + bounds.Width ||
                pos.Y < bounds.Y || pos.Y > bounds.Y + bounds.Height)
                return false;
        }

        if (_predicate != null && !_predicate(entity)) return false;
        return true;
    }
}