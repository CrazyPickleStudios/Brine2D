using Brine2D.Core;
using Brine2D.ECS.Systems;
using Brine2D.ECS.Query;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.ECS;

/// <summary>
/// Default implementation of IEntityWorld.
/// Uses a deferred operations pattern. Structural changes (create/destroy) are queued
/// during frame execution and applied at frame boundaries.
/// Targets 1,000-10,000+ entities with a straightforward mental model.
/// </summary>
internal class EntityWorld : IEntityWorld
{
    // Main entity list (deferred)
    private readonly DeferredList<Entity> _entities = new();

    // Behaviors (deferred)
    private readonly DeferredList<EntityBehavior> _behaviors = new();

    // Systems (scene-scoped)
    private readonly List<IUpdateSystem> _updateSystems = new();
    private readonly List<IRenderSystem> _renderSystems = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<EntityWorld>? _logger;
    private readonly ECSOptions _options;

    // Flag to control when structural changes are deferred
    private bool _isProcessing = false;

    // O(1) entity lookup by ID
    private readonly Dictionary<int, Entity> _entityLookup = new();

    // Per-type query index; targeted invalidation instead of broadcast
    private readonly List<ICachedQuery> _cachedQueries = new();
    private readonly Dictionary<Type, List<ICachedQuery>> _queriesByComponentType = new();

    // Lazy sort flags; invalidated when systems are added or removed
    private bool _updateSystemsSorted = false;
    private bool _renderSystemsSorted = false;

    // Component pools (one pool per component type)
    private readonly Dictionary<Type, IComponentPool> _componentPools = new();
    private readonly object _poolsLock = new();

    public IReadOnlyList<Entity> Entities => _entities.AsReadOnly();
    public IReadOnlyList<IUpdateSystem> UpdateSystems => _updateSystems.AsReadOnly();
    public IReadOnlyList<IRenderSystem> RenderSystems => _renderSystems.AsReadOnly();

    internal ECSOptions Options => _options;

    public EntityWorld(
        IServiceProvider serviceProvider,
        ILoggerFactory? loggerFactory = null,
        ECSOptions? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<EntityWorld>();
        _options = options ?? new ECSOptions();
    }

    #region System Management

    /// <summary>
    /// Adds a system to this world, automatically creating it with dependency injection.
    /// Systems that implement IUpdateSystem are added to the update pipeline.
    /// Systems that implement IRenderSystem are added to the render pipeline.
    /// Systems can implement both interfaces.
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
    public void AddSystem<T>(Action<T>? configure = null) where T : class
    {
        if (_updateSystems.Any(s => s.GetType() == typeof(T)) ||
            _renderSystems.Any(s => s.GetType() == typeof(T)))
        {
            _logger?.LogWarning("System {SystemType} already exists in world, skipping", typeof(T).Name);
            return;
        }

        var system = ActivatorUtilities.CreateInstance<T>(_serviceProvider);
        configure?.Invoke(system);

        bool added = false;

        if (system is IUpdateSystem updateSystem)
        {
            _updateSystems.Add(updateSystem);
            _updateSystemsSorted = false;
            _logger?.LogDebug("Added update system: {SystemType}", typeof(T).Name);
            added = true;
        }

        if (system is IRenderSystem renderSystem)
        {
            _renderSystems.Add(renderSystem);
            _renderSystemsSorted = false;
            _logger?.LogDebug("Added render system: {SystemType}", typeof(T).Name);
            added = true;
        }

        if (!added)
            _logger?.LogWarning("System {SystemType} does not implement IUpdateSystem or IRenderSystem", typeof(T).Name);
    }

    /// <summary>
    /// Removes a system from this world.
    /// If the system implements both IUpdateSystem and IRenderSystem it will be removed from both.
    /// </summary>
    public bool RemoveSystem(object system)
    {
        if (system == null) throw new ArgumentNullException(nameof(system));

        bool removed = false;

        if (system is IUpdateSystem updateSystem && _updateSystems.Remove(updateSystem))
        {
            _logger?.LogDebug("Removed update system: {SystemType}", system.GetType().Name);
            removed = true;
        }

        if (system is IRenderSystem renderSystem && _renderSystems.Remove(renderSystem))
        {
            _logger?.LogDebug("Removed render system: {SystemType}", system.GetType().Name);
            removed = true;
        }

        return removed;
    }

    public T? GetUpdateSystem<T>() where T : class, IUpdateSystem
        => _updateSystems.OfType<T>().FirstOrDefault();

    public T? GetRenderSystem<T>() where T : class, IRenderSystem
        => _renderSystems.OfType<T>().FirstOrDefault();

    public T? GetSystem<T>() where T : class
    {
        var updateSystem = _updateSystems.OfType<T>().FirstOrDefault();
        if (updateSystem != null) return updateSystem;
        return _renderSystems.OfType<T>().FirstOrDefault();
    }

    public bool HasUpdateSystem<T>() where T : class, IUpdateSystem
        => _updateSystems.Any(s => s is T);

    public bool HasRenderSystem<T>() where T : class, IRenderSystem
        => _renderSystems.Any(s => s is T);

    #endregion

    #region Entity Management

    public Entity CreateEntity(string name = "")
    {
        var logger = _loggerFactory?.CreateLogger<Entity>();
        var entity = new Entity(logger) { Name = name };

        entity.SetWorld(this);
        _entities.Add(entity);
        _entityLookup[entity.Id] = entity;
        _logger?.LogDebug("Queued entity creation: {Name} ({Id})", entity.Name, entity.Id);

        return entity;
    }

    public T CreateEntity<T>(string name = "") where T : Entity, new()
    {
        var entity = new T { Name = name };

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
            entity.IsActive = false;
            _logger?.LogDebug("Queued entity destruction: {Name} ({Id})", entity.Name, entity.Id);
        }
    }

    public Entity? GetEntityById(int id)
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
        foreach (var entity in _entities)
            if (entity.IsActive && entity.Tags.Contains(tag))
                yield return entity;
    }

    /// <remarks>
    /// Hot path: allocates an ArrayPool snapshot on every call. For per-frame use in systems,
    /// prefer <see cref="IEntityWorld.CreateCachedQuery{T}"/>; cached queries rebuild only
    /// when components change and have zero per-frame allocation.
    /// </remarks>
    public IEnumerable<Entity> GetEntitiesWithComponent<T>() where T : Component
    {
        var pool = GetOrCreatePool<T>();
        var (snapshot, length) = pool.GetSnapshot();
        try
        {
            for (int i = 0; i < length; i++)
            {
                var (entityId, _) = ((ValueTuple<int, T>[])snapshot)[i];
                var entity = GetEntityById(entityId);
                if (entity != null && entity.IsActive)
                    yield return entity;
            }
        }
        finally
        {
            pool.ReturnSnapshot(snapshot);
        }
    }

    /// <remarks>
    /// Hot path: prefer <see cref="IEntityWorld.CreateCachedQuery{T1,T2}"/> for per-frame use.
    /// </remarks>
    public IEnumerable<Entity> GetEntitiesWithComponents<T1, T2>()
        where T1 : Component
        where T2 : Component
    {
        var pool1 = GetOrCreatePool<T1>();
        var pool2 = GetOrCreatePool<T2>();

        // Iterate the smaller pool to minimize cross-resolves
        if (pool1.Count <= pool2.Count)
        {
            var (snapshot, length) = pool1.GetSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, _) = ((ValueTuple<int, T1>[])snapshot)[i];
                    var entity = GetEntityById(entityId);
                    if (entity != null && entity.IsActive && entity.HasComponent<T2>())
                        yield return entity;
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
                    var (entityId, _) = ((ValueTuple<int, T2>[])snapshot)[i];
                    var entity = GetEntityById(entityId);
                    if (entity != null && entity.IsActive && entity.HasComponent<T1>())
                        yield return entity;
                }
            }
            finally { pool2.ReturnSnapshot(snapshot); }
        }
    }

    public Entity? FindEntity(Func<Entity, bool> predicate)
    {
        foreach (var entity in _entities)
            if (predicate(entity)) return entity;
        return null;
    }

    #endregion

    #region Update and Render

    public void Update(GameTime gameTime)
    {
        _isProcessing = true;

        try
        {
            ProcessDeferredOperations();

            if (!_updateSystemsSorted)
            {
                _updateSystems.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
                _updateSystemsSorted = true;

                _logger?.LogDebug("Update systems execution order:");
                foreach (var system in _updateSystems)
                    _logger?.LogDebug("  [{Order,4}] {SystemType}", system.UpdateOrder, system.GetType().Name);
            }

            foreach (var system in _updateSystems)
            {
                if (!system.IsEnabled) continue;
                try { system.Update(this, gameTime); }
                catch (Exception ex) { _logger?.LogError(ex, "Error updating system: {SystemType}", system.GetType().Name); }
            }

            foreach (var behavior in _behaviors)
            {
                if (behavior.IsEnabled && behavior.Entity?.IsActive == true)
                    behavior.Update(gameTime);
            }
        }
        finally
        {
            _isProcessing = false;
            ProcessDeferredOperations();
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!_renderSystemsSorted)
        {
            _renderSystems.Sort((a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
            _renderSystemsSorted = true;

            _logger?.LogDebug("Render systems execution order:");
            foreach (var system in _renderSystems)
                _logger?.LogDebug("  [{Order,4}] {SystemType}", system.RenderOrder, system.GetType().Name);
        }

        foreach (var system in _renderSystems)
        {
            if (!system.IsEnabled) continue;
            try { system.Render(this, renderer); }
            catch (Exception ex) { _logger?.LogError(ex, "Error rendering system: {SystemType}", system.GetType().Name); }
        }

        foreach (var behavior in _behaviors)
        {
            if (behavior.IsEnabled && behavior.Entity?.IsActive == true)
                behavior.Render(renderer);
        }
    }

    public void Clear()
    {
        _logger?.LogDebug("Clearing world");

        foreach (var entity in _entities)
        {
            _entities.Remove(entity);
            entity.IsActive = false;
        }

        _updateSystems.Clear();
        _renderSystems.Clear();

        _logger?.LogInformation("World cleared: entities and systems removed");

        var wasProcessing = _isProcessing;
        _isProcessing = false;
        try { ProcessDeferredOperations(); }
        finally { _isProcessing = wasProcessing; }
    }

    #endregion

    #region Internal Notifications

    internal void NotifyComponentAdded(Entity entity, Component component)
        => InvalidateCachedQueriesForType(component.GetType());

    internal void NotifyComponentRemoved(Entity entity, Component component)
        => InvalidateCachedQueriesForType(component.GetType());

    internal void NotifyBehaviorAdded(Entity entity, EntityBehavior behavior)
        => _behaviors.Add(behavior);

    internal void NotifyBehaviorRemoved(Entity entity, EntityBehavior behavior)
        => _behaviors.Remove(behavior);

    #endregion

    #region Deferred Operations

    private void ProcessDeferredOperations()
    {
        while (_entities.HasPendingChanges || _behaviors.HasPendingChanges)
        {
            ProcessEntityAdditions();
            _behaviors.ProcessChanges();
            ProcessEntityRemovals();
        }
    }

    private void ProcessEntityAdditions()
    {
        _entities.ProcessAdds(entity =>
        {
            _entityLookup[entity.Id] = entity;
            entity.OnInitialize();
            _logger?.LogDebug("Created entity: {Name} ({Id})", entity.Name, entity.Id);
            // No query invalidation needed; new entities have no components yet.
        });
    }

    private void ProcessEntityRemovals()
    {
        _entities.ProcessRemovals(entity =>
        {
            _entityLookup.Remove(entity.Id);

            // Call OnDestroy first; overrides may still need to read components
            // (e.g., cleanup logic, saving state). OnDestroy calls RemoveEntityFromAllPools
            // internally, which handles pool cleanup and query invalidation.
            entity.OnDestroy();
            _logger?.LogDebug("Destroyed entity: {Name} ({Id})", entity.Name, entity.Id);

            // Safety net: if OnDestroy didn't call RemoveEntityFromAllPools
            // (e.g., base.OnDestroy() was not called), clear any stragglers.
            RemoveEntityFromAllPools(entity.Id);
        });
    }

    #endregion

    #region Service Provider (Internal; used by Behaviors)

    public T? GetService<T>() where T : class
        => _serviceProvider.GetService<T>();

    public T GetRequiredService<T>() where T : class
    {
        var service = _serviceProvider.GetService<T>();
        if (service == null)
            throw new InvalidOperationException(
                $"Required service '{typeof(T).Name}' is not registered. " +
                $"Did you forget to register it in your Program.cs?");
        return service;
    }

    #endregion

    #region Queries

    public EntityQuery Query() => new EntityQuery(this, _options);

    public CachedEntityQueryBuilder<T1> CreateCachedQuery<T1>() where T1 : Component
        => new CachedEntityQueryBuilder<T1>(this, _options);

    public CachedEntityQueryBuilder<T1, T2> CreateCachedQuery<T1, T2>()
        where T1 : Component
        where T2 : Component
        => new CachedEntityQueryBuilder<T1, T2>(this, _options);

    public CachedEntityQueryBuilder<T1, T2, T3> CreateCachedQuery<T1, T2, T3>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        => new CachedEntityQueryBuilder<T1, T2, T3>(this, _options);

    internal void RegisterCachedQuery(ICachedQuery query)
    {
        _cachedQueries.Add(query);
        foreach (var type in query.ComponentTypes)
        {
            if (!_queriesByComponentType.TryGetValue(type, out var list))
                _queriesByComponentType[type] = list = [];
            list.Add(query);
        }
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
    }

    private void InvalidateCachedQueriesForType(Type componentType)
    {
        if (_queriesByComponentType.TryGetValue(componentType, out var queries))
            foreach (var q in queries)
                q.Invalidate();
    }

    private void InvalidateAllCachedQueries()
    {
        foreach (var query in _cachedQueries)
            query.Invalidate();
    }

    #endregion

    #region Utilities

    public void Flush()
    {
        var wasProcessing = _isProcessing;
        _isProcessing = false;
        try { ProcessDeferredOperations(); }
        finally { _isProcessing = wasProcessing; }
    }

    #endregion

    #region Component Pools

    internal ComponentPool<T> GetOrCreatePool<T>() where T : Component
    {
        lock (_poolsLock)
        {
            var type = typeof(T);
            if (!_componentPools.TryGetValue(type, out var pool))
            {
                pool = new ComponentPool<T>();
                _componentPools[type] = pool;
                _logger?.LogDebug("Created component pool for type: {ComponentType}", type.Name);
            }
            return (ComponentPool<T>)pool;
        }
    }

    internal void AddComponentToPool<T>(int entityId, T component) where T : Component
    {
        var pool = GetOrCreatePool<T>();
        pool.Add(entityId, component);
        InvalidateCachedQueriesForType(typeof(T));
    }

    internal T? GetComponentFromPool<T>(int entityId) where T : Component
    {
        var pool = GetOrCreatePool<T>();
        return pool.GetTyped(entityId);
    }

    internal bool RemoveComponentFromPool<T>(int entityId) where T : Component
    {
        var pool = GetOrCreatePool<T>();
        var removed = pool.Remove(entityId);
        if (removed)
            InvalidateCachedQueriesForType(typeof(T));
        return removed;
    }

    internal bool HasComponentInPool<T>(int entityId) where T : Component
    {
        var pool = GetOrCreatePool<T>();
        return pool.Get(entityId) != null;
    }

    internal bool HasComponentOfType(int entityId, Type componentType)
    {
        lock (_poolsLock)
        {
            if (_componentPools.TryGetValue(componentType, out var pool))
                return pool.Get(entityId) != null;
            return false;
        }
    }

    /// <summary>
    /// Gets all components for a specific entity across all pools.
    /// Used by Entity.GetAllComponents().
    /// </summary>
    /// <remarks>
    /// Materializes inside the lock to avoid holding _poolsLock across a yield iterator,
    /// which would deadlock if any code path triggered during enumeration also acquires the lock
    /// (e.g., adding a component inside an OnDestroy callback). CS9232 flags this exact pattern.
    /// </remarks>
    internal IEnumerable<Component> GetAllComponentsFromPool(int entityId)
    {
        List<Component>? results = null;
        lock (_poolsLock)
        {
            foreach (var pool in _componentPools.Values)
            {
                var component = pool.Get(entityId);
                if (component != null)
                    (results ??= new()).Add(component);
            }
        }
        return results ?? [];
    }

    internal T CreateBehavior<T>() where T : EntityBehavior
        => ActivatorUtilities.CreateInstance<T>(_serviceProvider);

    /// <summary>
    /// Removes all components for the specified entity from every pool in a single lock pass.
    /// Called by <see cref="Entity.OnDestroy"/> for bulk cleanup without acquiring the lock
    /// once per component type. Invalidates affected cached queries after the lock is released.
    /// </summary>
    internal void RemoveEntityFromAllPools(int entityId)
    {
        List<Type>? removedTypes = null;
        lock (_poolsLock)
        {
            foreach (var (type, pool) in _componentPools)
            {
                if (pool.Remove(entityId))
                    (removedTypes ??= new()).Add(type);
            }
        }

        // Invalidate outside the lock; query invalidation must not hold _poolsLock
        if (removedTypes != null)
            foreach (var t in removedTypes)
                InvalidateCachedQueriesForType(t);
    }

    #endregion
}