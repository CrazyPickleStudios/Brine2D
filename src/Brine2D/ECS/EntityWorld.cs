using System.Buffers;
using Brine2D.Core;
using Brine2D.ECS.Systems;
using Brine2D.ECS.Query;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.ECS;

/// <summary>
/// Default implementation of IEntityWorld.
/// Uses a deferred operations pattern. Structural changes (create/destroy) are queued
/// during frame execution and applied at frame boundaries.
/// Targets 1,000-10,000+ entities with a straightforward mental model.
/// </summary>
internal class EntityWorld : IEntityWorld, IDisposable
{
    private readonly DeferredList<Entity> _entities;
    private readonly DeferredList<Behavior> _behaviors = new();
    private readonly DeferredList<IUpdateSystem> _updateSystems = new();
    private readonly DeferredList<IFixedUpdateSystem> _fixedUpdateSystems = new();
    private readonly DeferredList<IRenderSystem> _renderSystems = new();

    private readonly Dictionary<Type, object> _registeredSystems = new();

    private readonly IActivator _activator;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<EntityWorld>? _logger;
    private readonly ECSOptions _options;

    private readonly Dictionary<long, Entity> _entityLookup;

    private readonly List<ICachedQuery> _cachedQueries = new();
    private readonly Dictionary<Type, List<ICachedQuery>> _queriesByComponentType = new();
    private readonly Dictionary<string, List<ICachedQuery>> _queriesByTag = new();
    private readonly List<ICachedQuery> _queriesWithBehaviors = new();
    private readonly List<ICachedQuery> _queriesFilteringActive = new();
    private readonly List<ICachedQuery> _queriesFilteringEnabled = new();

    private readonly HashSet<object> _systemsPendingDisposal = new(ReferenceEqualityComparer.Instance);

    private bool _updateSystemsSorted = false;
    private bool _fixedUpdateSystemsSorted = false;
    private bool _renderSystemsSorted = false;

    private readonly List<Behavior> _behaviorsByUpdateOrder = new();
    private readonly List<Behavior> _behaviorsByFixedUpdateOrder = new();
    private readonly List<Behavior> _behaviorsByRenderOrder = new();
    private bool _behaviorUpdateOrderDirty = true;
    private bool _behaviorFixedUpdateOrderDirty = true;
    private bool _behaviorRenderOrderDirty = true;

    private readonly Dictionary<Type, IComponentPool> _componentPools = new();
    private readonly Dictionary<Type, List<Type>> _typeHierarchy = new();
    private readonly Dictionary<string, HashSet<Entity>> _tagIndex = new();

    private readonly HashSet<ICachedQuery> _queryInvalidationBuffer = new(ReferenceEqualityComparer.Instance);
    private readonly List<Type> _emptyPoolBuffer = [];

    private bool _disposed;

    public IReadOnlyList<Entity> Entities => _entities.AsReadOnly();
    public IReadOnlyList<IUpdateSystem> UpdateSystems => _updateSystems.AsReadOnly();
    public IReadOnlyList<IFixedUpdateSystem> FixedUpdateSystems => _fixedUpdateSystems.AsReadOnly();
    public IReadOnlyList<IRenderSystem> RenderSystems => _renderSystems.AsReadOnly();

    internal ECSOptions Options => _options;

    public EntityWorld(
        IActivator activator,
        ILoggerFactory? loggerFactory = null,
        ECSOptions? options = null)
    {
        _activator = activator ?? throw new ArgumentNullException(nameof(activator));
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<EntityWorld>();
        _options = options ?? new ECSOptions();
        _entities = new DeferredList<Entity>(_options.InitialEntityCapacity);
        _entityLookup = new Dictionary<long, Entity>(_options.InitialEntityCapacity);
    }

    /// <summary>
    /// Adds a system to this world, automatically creating it with dependency injection.
    /// Systems that implement IUpdateSystem are added to the update pipeline.
    /// Systems that implement IFixedUpdateSystem are added to the fixed update pipeline.
    /// Systems that implement IRenderSystem are added to the render pipeline.
    /// Systems can implement multiple interfaces.
    /// </summary>
    /// <typeparam name="T">The system type to create and add.</typeparam>
    /// <param name="configure">Optional configuration action for the system.</param>
    /// <example>
    /// <code>
    /// World.AddSystem&lt;PhysicsSystem&gt;();
    ///
    /// World.AddSystem&lt;DebugRenderer&gt;(debug =>
    /// {
    ///     debug.ShowColliders = true;
    ///     debug.IsEnabled = false;
    /// });
    ///
    /// // Dependencies are resolved automatically from DI
    /// World.AddSystem&lt;PlayerControllerSystem&gt;();
    /// </code>
    /// </example>
    public void AddSystem<T>(Action<T>? configure = null) where T : class, ISystem
    {
        if (_registeredSystems.ContainsKey(typeof(T)))
        {
            _logger?.LogWarning("System {SystemType} already exists in world, skipping", typeof(T).Name);
            return;
        }

        var system = _activator.CreateInstance<T>();
        configure?.Invoke(system);

        bool added = false;

        if (system is IUpdateSystem updateSystem)
        {
            _updateSystems.Add(updateSystem);
            _updateSystemsSorted = false;
            _logger?.LogDebug("Added update system: {SystemType}", typeof(T).Name);
            added = true;
        }

        if (system is IFixedUpdateSystem fixedUpdateSystem)
        {
            _fixedUpdateSystems.Add(fixedUpdateSystem);
            _fixedUpdateSystemsSorted = false;
            _logger?.LogDebug("Added fixed update system: {SystemType}", typeof(T).Name);
            added = true;
        }

        if (system is IRenderSystem renderSystem)
        {
            _renderSystems.Add(renderSystem);
            _renderSystemsSorted = false;
            _logger?.LogDebug("Added render system: {SystemType}", typeof(T).Name);
            added = true;
        }

        if (added)
        {
            _registeredSystems[typeof(T)] = system;
        }
        else
        {
            _logger?.LogWarning("System {SystemType} does not implement IUpdateSystem, IFixedUpdateSystem, or IRenderSystem", typeof(T).Name);
        }
    }

    /// <summary>
    /// Removes a system of the specified type from this world.
    /// If the system implements multiple pipeline interfaces it will be removed from all.
    /// </summary>
    public bool RemoveSystem<T>() where T : class, ISystem
    {
        var system = GetSystem<T>();
        return system != null && RemoveSystem((ISystem)system);
    }

    /// <summary>
    /// Removes a system from this world.
    /// If the system implements multiple pipeline interfaces it will be removed from all.
    /// Disposal is deferred until the next <see cref="ProcessDeferredOperations"/> call
    /// so that systems are never disposed while the update/render loop is iterating.
    /// </summary>
    /// <remarks>
    /// The <see cref="_registeredSystems"/> lookup is cleared immediately so that
    /// <see cref="AddSystem{T}"/> can re-register the same type within the same frame,
    /// while the actual pipeline removal is deferred. This means <see cref="GetSystem{T}"/>
    /// will return <c>null</c> for the removed type before the pipeline has been pruned.
    /// </remarks>
    public bool RemoveSystem(ISystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        bool found = false;

        if (system is IUpdateSystem updateSystem && _updateSystems.Contains(updateSystem))
        {
            _updateSystems.Remove(updateSystem);
            _logger?.LogDebug("Queued update system removal: {SystemType}", system.GetType().Name);
            found = true;
        }

        if (system is IFixedUpdateSystem fixedUpdateSystem && _fixedUpdateSystems.Contains(fixedUpdateSystem))
        {
            _fixedUpdateSystems.Remove(fixedUpdateSystem);
            _logger?.LogDebug("Queued fixed update system removal: {SystemType}", system.GetType().Name);
            found = true;
        }

        if (system is IRenderSystem renderSystem && _renderSystems.Contains(renderSystem))
        {
            _renderSystems.Remove(renderSystem);
            _logger?.LogDebug("Queued render system removal: {SystemType}", system.GetType().Name);
            found = true;
        }

        if (found)
        {
            _registeredSystems.Remove(system.GetType());

            if (system is IDisposable)
                _systemsPendingDisposal.Add(system);
        }

        return found;
    }

    private T? FindRegistered<T>() where T : class
    {
        if (_registeredSystems.TryGetValue(typeof(T), out var system) && system is T match)
            return match;
        foreach (var s in _registeredSystems.Values)
            if (s is T m) return m;
        return null;
    }

    public T? GetUpdateSystem<T>() where T : class, IUpdateSystem
        => FindRegistered<T>();

    public T? GetFixedUpdateSystem<T>() where T : class, IFixedUpdateSystem
        => FindRegistered<T>();

    public T? GetRenderSystem<T>() where T : class, IRenderSystem
        => FindRegistered<T>();

    public T? GetSystem<T>() where T : class
        => FindRegistered<T>();

    public bool HasUpdateSystem<T>() where T : class, IUpdateSystem
        => FindRegistered<T>() != null;

    public bool HasFixedUpdateSystem<T>() where T : class, IFixedUpdateSystem
        => FindRegistered<T>() != null;

    public bool HasRenderSystem<T>() where T : class, IRenderSystem
        => FindRegistered<T>() != null;

    /// <summary>
    /// Creates a new entity and queues it for initialization.
    /// </summary>
    /// <remarks>
    /// The entity is added to the ID lookup immediately so that components and
    /// behaviors can be attached right away. It will not appear in
    /// <see cref="Entities"/> or receive <see cref="Entity.OnInitialize"/> until
    /// the next <see cref="ProcessDeferredOperations"/> call.
    /// </remarks>
    public Entity CreateEntity(string name = "")
    {
        var entity = new Entity() { Name = name };
        entity.SetLogger(_loggerFactory?.CreateLogger<Entity>());

        entity.SetWorld(this);
        _entities.Add(entity);
        _entityLookup[entity.Id] = entity;
        _logger?.LogDebug("Queued entity creation: {Name} ({Id})", entity.Name, entity.Id);

        return entity;
    }

    /// <inheritdoc cref="CreateEntity(string)"/>
    public T CreateEntity<T>(string name = "") where T : Entity, new()
    {
        var entity = new T { Name = name };
        entity.SetLogger(_loggerFactory?.CreateLogger<T>());

        entity.SetWorld(this);
        _entities.Add(entity);
        _entityLookup[entity.Id] = entity;
        _logger?.LogDebug("Queued entity creation: {Name} ({Id})", entity.Name, entity.Id);

        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
        if (!_entities.IsQueuedForRemoval(entity))
        {
            _entities.Remove(entity);
            entity.SetActiveWithoutNotification(false);
            _logger?.LogDebug("Queued entity destruction: {Name} ({Id})", entity.Name, entity.Id);
        }
    }

    public Entity? GetEntityById(long id)
        => _entityLookup.TryGetValue(id, out var entity) ? entity : null;

    /// <remarks>O(n); avoid in hot paths. Prefer ID-based lookup via <see cref="GetEntityById"/>.</remarks>
    public Entity? GetEntityByName(string name)
    {
        foreach (var entity in _entities)
            if (entity.Name == name) return entity;
        return null;
    }

    public IEnumerable<Entity> GetEntitiesByTag(string tag)
    {
        if (!_tagIndex.TryGetValue(tag, out var set) || set.Count == 0)
            return [];

        var results = new List<Entity>(set.Count);
        foreach (var entity in set)
            if (entity.IsActive)
                results.Add(entity);
        return results;
    }

    /// <remarks>
    /// Returns a materialized list. For per-frame use in systems,
    /// prefer <see cref="IEntityWorld.CreateCachedQuery{T}"/>; cached queries rebuild only
    /// when components change and have zero per-frame allocation.
    /// </remarks>
    public IEnumerable<Entity> GetEntitiesWithComponent<T>() where T : Component
    {
        var pool = GetPool<T>();
        if (pool == null) return [];

        var (snapshot, length) = pool.GetTypedSnapshot();
        try
        {
            var results = new List<Entity>();
            for (int i = 0; i < length; i++)
            {
                var (entityId, _) = snapshot[i];
                var entity = GetEntityById(entityId);
                if(entity != null && entity.IsActive)
                    results.Add(entity);
            }
            return results;
        }
        finally
        {
            pool.ReturnTypedSnapshot(snapshot);
        }
    }

    public void ForEachWithComponent<T>(Action<Entity> action) where T : Component
    {
        var pool = GetPool<T>();
        if (pool == null) return;

        var (ids, length) = pool.GetEntityIdSnapshot();
        try
        {
            for (int i = 0; i < length; i++)
            {
                var entity = GetEntityById(ids[i]);
                if (entity != null && entity.IsActive)
                    action(entity);
            }
        }
        finally
        {
            pool.ReturnEntityIdSnapshot(ids);
        }
    }

    public void ForEachWithComponent<T>(Action<Entity, T> action) where T : Component
    {
        var pool = GetPool<T>();
        if (pool == null) return;

        var (snapshot, length) = pool.GetTypedSnapshot();
        try
        {
            for (int i = 0; i < length; i++)
            {
                var (entityId, component) = snapshot[i];
                var entity = GetEntityById(entityId);
                if (entity != null && entity.IsActive)
                    action(entity, component);
            }
        }
        finally
        {
            pool.ReturnTypedSnapshot(snapshot);
        }
    }

    public void ForEachWithTag(string tag, Action<Entity> action)
    {
        if (!_tagIndex.TryGetValue(tag, out var set) || set.Count == 0)
            return;

        var buffer = ArrayPool<Entity>.Shared.Rent(set.Count);
        try
        {
            int count = 0;
            foreach (var entity in set)
                buffer[count++] = entity;

            for (int i = 0; i < count; i++)
                if (buffer[i].IsActive)
                    action(buffer[i]);
        }
        finally
        {
            ArrayPool<Entity>.Shared.Return(buffer, clearArray: true);
        }
    }

    /// <remarks>
    /// Returns a materialized list. For per-frame use, prefer
    /// <see cref="IEntityWorld.CreateCachedQuery{T1,T2}"/>.
    /// </remarks>
    public IEnumerable<Entity> GetEntitiesWithComponents<T1, T2>()
        where T1 : Component
        where T2 : Component
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        if (pool1 == null || pool2 == null) return [];

        var results = new List<Entity>();

        if (pool1.Count <= pool2.Count)
        {
            var (snapshot, length) = pool1.GetTypedSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, _) = snapshot[i];
                    var entity = GetEntityById(entityId);
                    if (entity != null && entity.IsActive && entity.HasComponent<T2>())
                        results.Add(entity);
                }
            }
            finally { pool1.ReturnTypedSnapshot(snapshot); }
        }
        else
        {
            var (snapshot, length) = pool2.GetTypedSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, _) = snapshot[i];
                    var entity = GetEntityById(entityId);
                    if (entity != null && entity.IsActive && entity.HasComponent<T1>())
                        results.Add(entity);
                }
            }
            finally { pool2.ReturnTypedSnapshot(snapshot); }
        }

        return results;
    }

    public Entity? FindEntity(Func<Entity, bool> predicate)
    {
        foreach (var entity in _entities)
            if (predicate(entity)) return entity;
        return null;
    }

    public void Update(GameTime gameTime)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            ProcessDeferredOperations();

            if (!_updateSystemsSorted)
            {
                _updateSystems.SortCommitted((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
                _updateSystemsSorted = true;
                _logger?.LogDebug("Update systems sorted ({Count} systems)", _updateSystems.Count);
            }

            foreach (var system in _updateSystems)
            {
                if (!system.IsEnabled) continue;
                try { system.Update(this, gameTime); }
                catch (Exception ex) { _logger?.LogError(ex, "Error updating system: {SystemType}", system.GetType().Name); }
            }

            RebuildBehaviorUpdateOrder();

            for (int i = 0; i < _behaviorsByUpdateOrder.Count; i++)
            {
                var behavior = _behaviorsByUpdateOrder[i];
                if (!behavior.IsEnabled || behavior.Entity?.IsActive != true)
                    continue;
                try { behavior.Update(gameTime); }
                catch (Exception ex) { _logger?.LogError(ex, "Error in behavior {BehaviorType} on '{EntityName}'", behavior.GetType().Name, behavior.Entity?.Name); }
            }
        }
        finally
        {
            ProcessDeferredOperations();
        }
    }

    public void FixedUpdate(GameTime fixedTime)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            ProcessDeferredOperations();

            if (!_fixedUpdateSystemsSorted)
            {
                _fixedUpdateSystems.SortCommitted((a, b) => a.FixedUpdateOrder.CompareTo(b.FixedUpdateOrder));
                _fixedUpdateSystemsSorted = true;
                _logger?.LogDebug("Fixed update systems sorted ({Count} systems)", _fixedUpdateSystems.Count);
            }

            foreach (var system in _fixedUpdateSystems)
            {
                if (!system.IsEnabled) continue;
                try { system.FixedUpdate(this, fixedTime); }
                catch (Exception ex) { _logger?.LogError(ex, "Error in fixed update system: {SystemType}", system.GetType().Name); }
            }

            RebuildBehaviorFixedUpdateOrder();

            for (int i = 0; i < _behaviorsByFixedUpdateOrder.Count; i++)
            {
                var behavior = _behaviorsByFixedUpdateOrder[i];
                if (!behavior.IsEnabled || behavior.Entity?.IsActive != true)
                    continue;
                try { behavior.FixedUpdate(fixedTime); }
                catch (Exception ex) { _logger?.LogError(ex, "Error in fixed update behavior {BehaviorType} on '{EntityName}'", behavior.GetType().Name, behavior.Entity?.Name); }
            }
        }
        finally
        {
            ProcessDeferredOperations();
        }
    }

    public void Render(IRenderer renderer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_renderSystemsSorted)
        {
            _renderSystems.SortCommitted((a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
            _renderSystemsSorted = true;
            _logger?.LogDebug("Render systems sorted ({Count} systems)", _renderSystems.Count);
        }

        foreach (var system in _renderSystems)
        {
            if (!system.IsEnabled) continue;
            try { system.Render(this, renderer); }
            catch (Exception ex) { _logger?.LogError(ex, "Error rendering system: {SystemType}", system.GetType().Name); }
        }

        RebuildBehaviorRenderOrder();

        for (int i = 0; i < _behaviorsByRenderOrder.Count; i++)
        {
            var behavior = _behaviorsByRenderOrder[i];
            if (!behavior.IsEnabled || behavior.Entity?.IsActive != true)
                continue;
            try { behavior.Render(renderer); }
            catch (Exception ex) { _logger?.LogError(ex, "Error rendering behavior {BehaviorType} on '{EntityName}'", behavior.GetType().Name, behavior.Entity?.Name); }
        }
    }

    /// <summary>
    /// Clears all entities and systems from the world. Entities are destroyed in a single
    /// pass (no deferred round-trip), then all internal state is reset.
    /// The world remains usable after this call.
    /// </summary>
    public void Clear()
    {
        _logger?.LogDebug("Clearing world");

        TeardownEntities();
        DisposeAndClearSystems();

        foreach (var q in _cachedQueries)
            q.Invalidate();

        _logger?.LogInformation("World cleared: entities and systems removed");
    }

    /// <summary>
    /// Disposes all systems and clears all pipelines. Systems that implement multiple
    /// pipeline interfaces are disposed exactly once.
    /// </summary>
    private void DisposeAndClearSystems()
    {
        _updateSystems.ProcessChanges();
        _fixedUpdateSystems.ProcessChanges();
        _renderSystems.ProcessChanges();

        var allSystems = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var s in _updateSystems) allSystems.Add(s);
        foreach (var s in _fixedUpdateSystems) allSystems.Add(s);
        foreach (var s in _renderSystems) allSystems.Add(s);

        foreach (var system in allSystems)
        {
            if (system is IDisposable disposable)
            {
                try { disposable.Dispose(); }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disposing system {SystemType}", system.GetType().Name);
                }
            }
        }

        _updateSystems.Clear();
        _fixedUpdateSystems.Clear();
        _renderSystems.Clear();
        _registeredSystems.Clear();
        _systemsPendingDisposal.Clear();
        _updateSystemsSorted = false;
        _fixedUpdateSystemsSorted = false;
        _renderSystemsSorted = false;
    }

    /// <summary>
    /// Releases all internal references, runs entity lifecycle callbacks, and disposes systems.
    /// Called automatically by the DI scope when the scene is unloaded.
    /// Entities receive <see cref="Entity.OnDestroy"/> so cleanup logic (event
    /// unsubscription, behavior detach, etc.) runs even without an explicit
    /// <see cref="Clear"/> call.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        TeardownEntities();
        _typeHierarchy.Clear();

        _cachedQueries.Clear();
        _queriesByComponentType.Clear();
        _queriesByTag.Clear();
        _queriesWithBehaviors.Clear();
        _queriesFilteringActive.Clear();
        _queriesFilteringEnabled.Clear();

        DisposeAndClearSystems();
    }

    /// <summary>
    /// Shared entity teardown used by both <see cref="Clear"/> and <see cref="Dispose"/>.
    /// Flushes pending changes, runs <see cref="Entity.OnDestroy"/> on every entity,
    /// and resets all entity-related state.
    /// </summary>
    private void TeardownEntities()
    {
        _entities.ProcessChanges();

        foreach (var entity in _entities)
        {
            if (!entity.PoolsCleared)
                entity.OnDestroy();
            entity.SetWorld(null);
        }

        _entities.Clear();
        _behaviors.Clear();
        _entityLookup.Clear();
        _tagIndex.Clear();
        _componentPools.Clear();
        _behaviorsByUpdateOrder.Clear();
        _behaviorsByFixedUpdateOrder.Clear();
        _behaviorsByRenderOrder.Clear();
        _behaviorUpdateOrderDirty = true;
        _behaviorFixedUpdateOrderDirty = true;
        _behaviorRenderOrderDirty = true;
    }

    internal void RegisterCachedQuery(ICachedQuery query)
    {
        _cachedQueries.Add(query);
        foreach (var type in query.ComponentTypes)
        {
            if (!_queriesByComponentType.TryGetValue(type, out var list))
                _queriesByComponentType[type] = list = [];
            list.Add(query);
        }
        foreach (var tag in query.TagFilters)
        {
            if (!_queriesByTag.TryGetValue(tag, out var list))
                _queriesByTag[tag] = list = [];
            list.Add(query);
        }
        if (query.HasBehaviorFilters)
            _queriesWithBehaviors.Add(query);
        if (query.FiltersActiveState)
            _queriesFilteringActive.Add(query);
        if (query.FiltersEnabledState)
            _queriesFilteringEnabled.Add(query);
    }

    /// <summary>
    /// Removes a cached query from the invalidation index.
    /// Called by <see cref="CachedEntityQuery{T1}"/>.Dispose() and its siblings so that
    /// queries removed mid-scene (e.g., when a system is unregistered) stop receiving
    /// invalidation notifications and are eligible for GC.
    /// </summary>
    internal void UnregisterCachedQuery(ICachedQuery query)
    {
        _cachedQueries.Remove(query);
        foreach (var type in query.ComponentTypes)
        {
            if (_queriesByComponentType.TryGetValue(type, out var list))
                list.Remove(query);
        }
        foreach (var tag in query.TagFilters)
        {
            if (_queriesByTag.TryGetValue(tag, out var list))
                list.Remove(query);
        }
        _queriesWithBehaviors.Remove(query);
        _queriesFilteringActive.Remove(query);
        _queriesFilteringEnabled.Remove(query);
    }

    private void InvalidateCachedQueriesForType(Type componentType)
    {
        var current = componentType;
        while (current != null && current != typeof(Component))
        {
            if (_queriesByComponentType.TryGetValue(current, out var queries))
                foreach (var q in queries)
                    q.Invalidate();
            current = current.BaseType;
        }
    }

    private void InvalidateCachedQueriesForTag(string tag)
    {
        if (_queriesByTag.TryGetValue(tag, out var queries))
            foreach (var q in queries)
                q.Invalidate();
    }

    private void InvalidateCachedQueriesForBehaviors()
    {
        foreach (var q in _queriesWithBehaviors)
            q.Invalidate();
    }

    private void RebuildBehaviorUpdateOrder()
    {
        if (!_behaviorUpdateOrderDirty) return;

        _behaviorsByUpdateOrder.Clear();
        foreach (var behavior in _behaviors)
            _behaviorsByUpdateOrder.Add(behavior);

        SortHelper.StableSort(_behaviorsByUpdateOrder, static (a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
        _behaviorUpdateOrderDirty = false;
    }

    private void RebuildBehaviorFixedUpdateOrder()
    {
        if (!_behaviorFixedUpdateOrderDirty) return;

        _behaviorsByFixedUpdateOrder.Clear();
        foreach (var behavior in _behaviors)
            _behaviorsByFixedUpdateOrder.Add(behavior);

        SortHelper.StableSort(_behaviorsByFixedUpdateOrder, static (a, b) => a.FixedUpdateOrder.CompareTo(b.FixedUpdateOrder));
        _behaviorFixedUpdateOrderDirty = false;
    }

    private void RebuildBehaviorRenderOrder()
    {
        if (!_behaviorRenderOrderDirty) return;

        _behaviorsByRenderOrder.Clear();
        foreach (var behavior in _behaviors)
            _behaviorsByRenderOrder.Add(behavior);

        SortHelper.StableSort(_behaviorsByRenderOrder, static (a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
        _behaviorRenderOrderDirty = false;
    }
    
    private void ProcessDeferredOperations()
    {
        const int maxPasses = 16;
        int pass = 0;

        while (_entities.HasPendingChanges || _behaviors.HasPendingChanges ||
               _updateSystems.HasPendingChanges || _fixedUpdateSystems.HasPendingChanges ||
               _renderSystems.HasPendingChanges)
        {
            if (++pass > maxPasses)
            {
                _logger?.LogWarning(
                    "ProcessDeferredOperations exceeded {MaxPasses} passes; breaking to prevent infinite loop",
                    maxPasses);
                break;
            }

            ProcessEntityAdditions();
            _behaviors.ProcessChanges();
            ProcessSystemChanges();
            ProcessEntityRemovals();
        }
    }

    private void ProcessEntityAdditions()
    {
        _entities.ProcessAdds(entity =>
        {
            entity.OnInitialize();
            _logger?.LogDebug("Created entity: {Name} ({Id})", entity.Name, entity.Id);
        });
    }

    private void ProcessEntityRemovals()
    {
        _entities.ProcessRemovals(entity =>
        {
            _entityLookup.Remove(entity.Id);
            RemoveEntityFromTagIndex(entity);

            if (!entity.PoolsCleared)
            {
                entity.OnDestroy();

                if (!entity.PoolsCleared)
                    RemoveEntityFromAllPools(entity.Id);
            }

            entity.SetWorld(null);

            _logger?.LogDebug("Destroyed entity: {Name} ({Id})", entity.Name, entity.Id);
        });
    }

    private void ProcessSystemChanges()
    {
        if (!_updateSystems.HasPendingChanges && !_fixedUpdateSystems.HasPendingChanges &&
            !_renderSystems.HasPendingChanges && _systemsPendingDisposal.Count == 0)
            return;

        _updateSystems.ProcessChanges();
        _fixedUpdateSystems.ProcessChanges();
        _renderSystems.ProcessChanges();

        if (_systemsPendingDisposal.Count > 0)
        {
            foreach (var system in _systemsPendingDisposal)
            {
                if (system is IDisposable disposable)
                {
                    try { disposable.Dispose(); }
                    catch (Exception ex) { _logger?.LogError(ex, "Error disposing system {SystemType}", system.GetType().Name); }
                }
            }
            _systemsPendingDisposal.Clear();
        }
    }

    internal bool HasComponentInPool<T>(long entityId) where T : Component
        => HasComponentOfType(entityId, typeof(T));

    internal bool HasComponentOfType(long entityId, Type componentType)
    {
        if (!_typeHierarchy.TryGetValue(componentType, out var concreteTypes))
            return false;
        for (int i = 0; i < concreteTypes.Count; i++)
            if (_componentPools.TryGetValue(concreteTypes[i], out var pool) && pool.Contains(entityId))
                return true;
        return false;
    }

    internal void NotifyComponentAdded(Entity entity, Component component)
    {
        InvalidateCachedQueriesForType(component.GetType());
    }

    internal void NotifyComponentRemoved(Entity entity, Component component)
    {
        InvalidateCachedQueriesForType(component.GetType());
    }

    internal void NotifyTagAdded(Entity entity, string tag)
    {
        if (!_tagIndex.TryGetValue(tag, out var set))
            _tagIndex[tag] = set = new();
        set.Add(entity);

        InvalidateCachedQueriesForTag(tag);
    }

    internal void NotifyTagRemoved(Entity entity, string tag)
    {
        if (_tagIndex.TryGetValue(tag, out var set))
        {
            set.Remove(entity);
            if (set.Count == 0)
                _tagIndex.Remove(tag);
        }

        InvalidateCachedQueriesForTag(tag);
    }

    internal void NotifyTagsCleared(Entity entity, IReadOnlySet<string> tags)
    {
        foreach (var tag in tags)
        {
            if (_tagIndex.TryGetValue(tag, out var set))
            {
                set.Remove(entity);
                if (set.Count == 0)
                    _tagIndex.Remove(tag);
            }

            InvalidateCachedQueriesForTag(tag);
        }
    }

    internal void NotifyActiveChanged()
    {
        foreach (var q in _queriesFilteringActive)
            q.Invalidate();
    }

    internal void NotifyComponentEnabledChanged()
    {
        foreach (var q in _queriesFilteringEnabled)
            q.Invalidate();
    }

    internal void NotifyBehaviorAdded(Entity entity, Behavior behavior)
    {
        _behaviors.Add(behavior);
        _behaviorUpdateOrderDirty = true;
        _behaviorFixedUpdateOrderDirty = true;
        _behaviorRenderOrderDirty = true;
        InvalidateCachedQueriesForBehaviors();
    }

    internal void NotifyBehaviorRemoved(Entity entity, Behavior behavior)
    {
        _behaviors.Remove(behavior);
        _behaviorUpdateOrderDirty = true;
        _behaviorFixedUpdateOrderDirty = true;
        _behaviorRenderOrderDirty = true;
        InvalidateCachedQueriesForBehaviors();
    }

    public EntityQuery Query() => new(this, _options);

    public CachedEntityQueryBuilder<T1> CreateCachedQuery<T1>() where T1 : Component
        => new(this, _options);

    public CachedEntityQueryBuilder<T1, T2> CreateCachedQuery<T1, T2>()
        where T1 : Component
        where T2 : Component
        => new(this, _options);

    public CachedEntityQueryBuilder<T1, T2, T3> CreateCachedQuery<T1, T2, T3>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        => new(this, _options);

    public CachedEntityQueryBuilder<T1, T2, T3, T4> CreateCachedQuery<T1, T2, T3, T4>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        => new(this, _options);

    public void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ProcessDeferredOperations();
    }

    internal ComponentPool<T> GetOrCreatePool<T>() where T : Component
    {
        if (_componentPools.TryGetValue(typeof(T), out var existing))
            return (ComponentPool<T>)existing;

        var pool = new ComponentPool<T>();
        _componentPools[typeof(T)] = pool;
        RegisterTypeHierarchy(typeof(T));
        return pool;
    }

    internal ComponentPool<T>? GetPool<T>() where T : Component
    {
        return _componentPools.TryGetValue(typeof(T), out var pool) ? (ComponentPool<T>)pool : null;
    }

    internal void AddComponentToPool<T>(long entityId, T component) where T : Component
    {
        var pool = GetOrCreatePool<T>();
        pool.Add(entityId, component);
    }

    internal T? GetComponentFromPool<T>(long entityId) where T : Component
    {
        if (_componentPools.TryGetValue(typeof(T), out var exactPool))
        {
            var result = ((ComponentPool<T>)exactPool).GetTyped(entityId);
            if (result != null) return result;
        }

        if (_typeHierarchy.TryGetValue(typeof(T), out var concreteTypes))
        {
            for (int i = 0; i < concreteTypes.Count; i++)
            {
                var type = concreteTypes[i];
                if (type == typeof(T)) continue;
                if (_componentPools.TryGetValue(type, out var pool))
                {
                    var component = pool.Get(entityId);
                    if (component != null) return (T)component;
                }
            }
        }

        return null;
    }

    internal bool RemoveComponentFromPool<T>(long entityId) where T : Component
    {
        if (_componentPools.TryGetValue(typeof(T), out var exactPool) && exactPool.Remove(entityId))
            return true;

        if (_typeHierarchy.TryGetValue(typeof(T), out var concreteTypes))
        {
            for (int i = 0; i < concreteTypes.Count; i++)
            {
                var type = concreteTypes[i];
                if (type == typeof(T)) continue;
                if (_componentPools.TryGetValue(type, out var pool) && pool.Remove(entityId))
                    return true;
            }
        }

        return false;
    }

    internal IComponentPool? GetPool(Type componentType)
    {
        return _componentPools.TryGetValue(componentType, out var pool) ? pool : null;
    }

    internal Component? GetComponentOfType(long entityId, Type componentType)
    {
        if (!_typeHierarchy.TryGetValue(componentType, out var concreteTypes))
            return null;
        for (int i = 0; i < concreteTypes.Count; i++)
        {
            if (_componentPools.TryGetValue(concreteTypes[i], out var pool))
            {
                var component = pool.Get(entityId);
                if (component != null) return component;
            }
        }
        return null;
    }

    internal IEnumerable<Component> GetAllComponentsFromPool(long entityId)
    {
        foreach (var pool in _componentPools.Values)
        {
            var component = pool.Get(entityId);
            if (component != null)
                yield return component;
        }
    }

    internal int GetComponentCountForEntity(long entityId)
    {
        int count = 0;
        foreach (var pool in _componentPools.Values)
            if (pool.Contains(entityId))
                count++;
        return count;
    }

    internal T CreateBehavior<T>() where T : Behavior
        => _activator.CreateInstance<T>();

    internal void RemoveEntityFromAllPools(long entityId)
    {
        _emptyPoolBuffer.Clear();
        _queryInvalidationBuffer.Clear();

        foreach (var (type, pool) in _componentPools)
        {
            if (pool.Remove(entityId))
            {
                var current = type;
                while (current != null && current != typeof(Component))
                {
                    if (_queriesByComponentType.TryGetValue(current, out var queries))
                        foreach (var q in queries)
                            _queryInvalidationBuffer.Add(q);
                    current = current.BaseType;
                }

                if (pool.Count == 0)
                    _emptyPoolBuffer.Add(type);
            }
        }

        foreach (var type in _emptyPoolBuffer)
        {
            _componentPools.Remove(type);
            PruneTypeHierarchy(type);
        }

        foreach (var q in _queryInvalidationBuffer)
            q.Invalidate();
    }

    private void RemoveEntityFromTagIndex(Entity entity)
    {
        foreach (var tag in entity.Tags)
        {
            if (_tagIndex.TryGetValue(tag, out var set))
            {
                set.Remove(entity);
                if (set.Count == 0)
                    _tagIndex.Remove(tag);
            }
        }
    }

    /// <summary>
    /// Returns the combined entity count across all pools whose component type
    /// is assignable to <paramref name="componentType"/> (including itself).
    /// </summary>
    internal int GetTotalPoolCount(Type componentType)
    {
        if (!_typeHierarchy.TryGetValue(componentType, out var concreteTypes))
            return 0;
        int total = 0;
        for (int i = 0; i < concreteTypes.Count; i++)
            if (_componentPools.TryGetValue(concreteTypes[i], out var pool))
                total += pool.Count;
        return total;
    }

    /// <summary>
    /// Returns all non-empty pools whose component type is assignable to
    /// <paramref name="componentType"/>. Used by queries for polymorphic iteration.
    /// </summary>
    internal List<IComponentPool> GetPoolsAssignableTo(Type componentType)
    {
        if (!_typeHierarchy.TryGetValue(componentType, out var concreteTypes))
            return [];
        var result = new List<IComponentPool>(concreteTypes.Count);
        for (int i = 0; i < concreteTypes.Count; i++)
            if (_componentPools.TryGetValue(concreteTypes[i], out var pool) && pool.Count > 0)
                result.Add(pool);
        return result;
    }

    /// <summary>
    /// Populates <paramref name="result"/> with all non-empty pools whose component type
    /// is assignable to <paramref name="componentType"/>. Caller owns the list and can
    /// reuse it across rebuilds to avoid per-call allocation.
    /// </summary>
    internal void GetPoolsAssignableTo(Type componentType, List<IComponentPool> result)
    {
        result.Clear();
        if (!_typeHierarchy.TryGetValue(componentType, out var concreteTypes))
            return;
        for (int i = 0; i < concreteTypes.Count; i++)
            if (_componentPools.TryGetValue(concreteTypes[i], out var pool) && pool.Count > 0)
                result.Add(pool);
    }

    /// <summary>
    /// Removes <paramref name="concreteType"/> from the type hierarchy index.
    /// Mirror of <see cref="RegisterTypeHierarchy"/>; called when a pool is emptied
    /// and removed so that the hierarchy does not accumulate stale entries.
    /// </summary>
    private void PruneTypeHierarchy(Type concreteType)
    {
        var current = concreteType;
        while (current != null && current != typeof(Component))
        {
            if (_typeHierarchy.TryGetValue(current, out var list))
            {
                list.Remove(concreteType);
                if (list.Count == 0)
                    _typeHierarchy.Remove(current);
            }
            current = current.BaseType;
        }
    }

    /// <summary>
    /// Registers <paramref name="concreteType"/> in the type hierarchy index under
    /// itself and every ancestor up to (but not including) <see cref="Component"/>.
    /// Called once when a new pool is created.
    /// </summary>
    /// <remarks>
    /// Only class inheritance is indexed. Interface-based queries are intentionally
    /// unsupported to keep the index linear (one chain per concrete type) and
    /// invalidation predictable. The <c>where T : Component</c> constraint on all
    /// public APIs enforces this at compile time.
    /// </remarks>
    private void RegisterTypeHierarchy(Type concreteType)
    {
        var current = concreteType;
        while (current != null && current != typeof(Component))
        {
            if (!_typeHierarchy.TryGetValue(current, out var list))
                _typeHierarchy[current] = list = [];
            if (!list.Contains(concreteType))
                list.Add(concreteType);
            current = current.BaseType;
        }
    }
}