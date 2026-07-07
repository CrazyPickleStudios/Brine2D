using System.Buffers;
using System.Collections.Concurrent;
using System.Reflection;
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
    // Caches per-Behavior type which of the three sorted order lists need to be dirtied
    // when a behavior of that type is added or removed. Computed once via reflection and reused.
    private static readonly ConcurrentDictionary<Type, (bool Update, bool FixedUpdate, bool Render)>
        _behaviorPipelineCache = new();

    private static (bool Update, bool FixedUpdate, bool Render) GetBehaviorPipelines(Type behaviorType)
    {
        return _behaviorPipelineCache.GetOrAdd(behaviorType, static t =>
        {
            bool update = false, fixedUpdate = false, render = false;

            for (var current = t; current != null && current != typeof(Behavior); current = current.BaseType)
            {
                if (!update && current.GetMethod(nameof(Behavior.Update),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) != null)
                    update = true;
                if (!fixedUpdate && current.GetMethod(nameof(Behavior.FixedUpdate),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) != null)
                    fixedUpdate = true;
                if (!render && current.GetMethod(nameof(Behavior.Render),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) != null)
                    render = true;
                if (update && fixedUpdate && render) break;
            }

            // A behavior that overrides none of Update/FixedUpdate/Render participates in the
            // Update pipeline only. This ensures OnStart fires (dispatched from the Update loop
            // on the first tick) while keeping the per-frame cost to a single no-op Update call
            // rather than three. FixedUpdate and Render slots are omitted intentionally.
            if (!update && !fixedUpdate && !render)
                return (true, false, false);

            return (update, fixedUpdate, render);
        });
    }
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
    private readonly HashSet<Type> _sequentialSystemTypes = new();
    private Type? _currentSystemType;

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

    private ECSOptions? _sequentialOptions;

    private bool _disposed;

    public event Action<Entity>? EntityCreated;
    public event Action<Entity>? EntityDestroyed;

    public IReadOnlyList<Entity> Entities => _entities.AsReadOnly();
    public int EntityCount => _entities.Count;
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
    /// World.AddSystem&lt;Box2DPhysicsSystem&gt;();
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

        if (typeof(T).IsDefined(typeof(Systems.SequentialAttribute), inherit: false))
            _sequentialSystemTypes.Add(typeof(T));

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
            throw new InvalidOperationException(
                $"System '{typeof(T).Name}' does not implement IUpdateSystem, IFixedUpdateSystem, or IRenderSystem " +
                $"and cannot be added to the world. Implement at least one pipeline interface.");
        }
    }

    /// <summary>
    /// Adds a pre-constructed system instance to this world, bypassing dependency injection.
    /// </summary>
    public void AddSystem<T>(T instance) where T : class, ISystem
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (_registeredSystems.ContainsKey(typeof(T)))
        {
            _logger?.LogWarning("System {SystemType} already exists in world, skipping", typeof(T).Name);
            return;
        }

        if (typeof(T).IsDefined(typeof(Systems.SequentialAttribute), inherit: false))
            _sequentialSystemTypes.Add(typeof(T));

        bool added = false;

        if (instance is IUpdateSystem updateSystem)
        {
            _updateSystems.Add(updateSystem);
            _updateSystemsSorted = false;
            _logger?.LogDebug("Added update system (instance): {SystemType}", typeof(T).Name);
            added = true;
        }

        if (instance is IFixedUpdateSystem fixedUpdateSystem)
        {
            _fixedUpdateSystems.Add(fixedUpdateSystem);
            _fixedUpdateSystemsSorted = false;
            _logger?.LogDebug("Added fixed update system (instance): {SystemType}", typeof(T).Name);
            added = true;
        }

        if (instance is IRenderSystem renderSystem)
        {
            _renderSystems.Add(renderSystem);
            _renderSystemsSorted = false;
            _logger?.LogDebug("Added render system (instance): {SystemType}", typeof(T).Name);
            added = true;
        }

        if (added)
        {
            _registeredSystems[typeof(T)] = instance;
        }
        else
        {
            throw new InvalidOperationException(
                $"System '{typeof(T).Name}' does not implement IUpdateSystem, IFixedUpdateSystem, or IRenderSystem " +
                $"and cannot be added to the world. Implement at least one pipeline interface.");
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
            var systemType = system.GetType();
            _registeredSystems.Remove(systemType);
            _sequentialSystemTypes.Remove(systemType);

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

    public T GetRequiredSystem<T>() where T : class
    {
        var system = FindRegistered<T>();
        if (system == null)
            throw new InvalidOperationException(
                $"System '{typeof(T).Name}' is not registered in this world." + Environment.NewLine +
                Environment.NewLine +
                "Fix: Add the system before accessing it:" + Environment.NewLine +
                $"  world.AddSystem<{typeof(T).Name}>();" + Environment.NewLine +
                Environment.NewLine +
                $"Registered systems: {string.Join(", ", _registeredSystems.Keys.Select(k => k.Name))}");
        return system;
    }

    public bool HasSystem<T>() where T : class
        => FindRegistered<T>() != null;

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

    /// <remarks>
    /// O(n); avoid in hot paths. Prefer ID-based lookup via <see cref="GetEntityById"/>.
    /// Also searches entities that have been created but not yet committed by
    /// <see cref="Flush"/> / <see cref="ProcessDeferredOperations"/>, so a name lookup
    /// immediately after <see cref="CreateEntity"/> returns the new entity.
    /// Only active entities are returned; pending adds are always considered active.
    /// </remarks>
    public Entity? GetEntityByName(string name)
    {
        foreach (var entity in _entities)
            if (entity.IsActive && entity.Name == name) return entity;
        return _entities.FindPendingAdd(e => e.Name == name);
    }

    /// <remarks>
    /// O(n); avoid in hot paths. Prefer ID-based lookup via <see cref="GetEntityById"/>.
    /// Also searches entities that have been created but not yet committed by
    /// <see cref="Flush"/> / <see cref="ProcessDeferredOperations"/>, so a name lookup
    /// immediately after <see cref="CreateEntity"/> returns the new entity.
    /// When <paramref name="includeInactive"/> is <see langword="false"/>, only active
    /// entities (and pending adds, which are always active) are returned.
    /// </remarks>
    public Entity? GetEntityByName(string name, bool includeInactive)
    {
        if (!includeInactive) return GetEntityByName(name);
        foreach (var entity in _entities)
            if (entity.Name == name) return entity;
        return _entities.FindPendingAdd(e => e.Name == name);
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

    public IEnumerable<Entity> GetEntitiesByTag(string tag, bool includeInactive)
    {
        if (!includeInactive) return GetEntitiesByTag(tag);

        if (!_tagIndex.TryGetValue(tag, out var set) || set.Count == 0)
            return [];

        return new List<Entity>(set);
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

    public void ForEachWithComponents<T1, T2>(Action<Entity, T1, T2> action)
        where T1 : Component
        where T2 : Component
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        if (pool1 == null || pool2 == null) return;

        if (pool1.Count <= pool2.Count)
        {
            var (snapshot, length) = pool1.GetTypedSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, c1) = snapshot[i];
                    var entity = GetEntityById(entityId);
                    if (entity == null || !entity.IsActive) continue;
                    var c2 = GetComponentFromPool<T2>(entityId);
                    if (c2 != null)
                        action(entity, c1, c2);
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
                    var (entityId, c2) = snapshot[i];
                    var entity = GetEntityById(entityId);
                    if (entity == null || !entity.IsActive) continue;
                    var c1 = GetComponentFromPool<T1>(entityId);
                    if (c1 != null)
                        action(entity, c1, c2);
                }
            }
            finally { pool2.ReturnTypedSnapshot(snapshot); }
        }
    }

    public void ForEachWithComponents<T1, T2, T3>(Action<Entity, T1, T2, T3> action)
        where T1 : Component
        where T2 : Component
        where T3 : Component
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        var pool3 = GetPool<T3>();
        if (pool1 == null || pool2 == null || pool3 == null) return;

        int c1 = pool1.Count, c2 = pool2.Count, c3 = pool3.Count;

        if (c1 <= c2 && c1 <= c3)
        {
            var (snapshot, length) = pool1.GetTypedSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, comp1) = snapshot[i];
                    var entity = GetEntityById(entityId);
                    if (entity == null || !entity.IsActive) continue;
                    var comp2 = GetComponentFromPool<T2>(entityId);
                    if (comp2 == null) continue;
                    var comp3 = GetComponentFromPool<T3>(entityId);
                    if (comp3 != null)
                        action(entity, comp1, comp2, comp3);
                }
            }
            finally { pool1.ReturnTypedSnapshot(snapshot); }
        }
        else if (c2 <= c1 && c2 <= c3)
        {
            var (snapshot, length) = pool2.GetTypedSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, comp2) = snapshot[i];
                    var entity = GetEntityById(entityId);
                    if (entity == null || !entity.IsActive) continue;
                    var comp1 = GetComponentFromPool<T1>(entityId);
                    if (comp1 == null) continue;
                    var comp3 = GetComponentFromPool<T3>(entityId);
                    if (comp3 != null)
                        action(entity, comp1, comp2, comp3);
                }
            }
            finally { pool2.ReturnTypedSnapshot(snapshot); }
        }
        else
        {
            var (snapshot, length) = pool3.GetTypedSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, comp3) = snapshot[i];
                    var entity = GetEntityById(entityId);
                    if (entity == null || !entity.IsActive) continue;
                    var comp1 = GetComponentFromPool<T1>(entityId);
                    if (comp1 == null) continue;
                    var comp2 = GetComponentFromPool<T2>(entityId);
                    if (comp2 != null)
                        action(entity, comp1, comp2, comp3);
                }
            }
            finally { pool3.ReturnTypedSnapshot(snapshot); }
        }
    }

    public void ForEachWithComponents<T1, T2, T3, T4>(Action<Entity, T1, T2, T3, T4> action)
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        var pool3 = GetPool<T3>();
        var pool4 = GetPool<T4>();
        if (pool1 == null || pool2 == null || pool3 == null || pool4 == null) return;

        int c1 = pool1.Count, c2 = pool2.Count, c3 = pool3.Count, c4 = pool4.Count;
        int smallest = c1;
        int which = 1;
        if (c2 < smallest) { smallest = c2; which = 2; }
        if (c3 < smallest) { smallest = c3; which = 3; }
        if (c4 < smallest) { which = 4; }

        IComponentPool iterPool = which switch
        {
            2 => pool2,
            3 => pool3,
            4 => pool4,
            _ => pool1
        };

        var (ids, len) = iterPool.GetEntityIdSnapshot();
        try
        {
            for (int i = 0; i < len; i++)
            {
                var entityId = ids[i];
                var entity = GetEntityById(entityId);
                if (entity == null || !entity.IsActive) continue;
                var comp1 = GetComponentFromPool<T1>(entityId);
                if (comp1 == null) continue;
                var comp2 = GetComponentFromPool<T2>(entityId);
                if (comp2 == null) continue;
                var comp3 = GetComponentFromPool<T3>(entityId);
                if (comp3 == null) continue;
                var comp4 = GetComponentFromPool<T4>(entityId);
                if (comp4 != null)
                    action(entity, comp1, comp2, comp3, comp4);
            }
        }
        finally { iterPool.ReturnEntityIdSnapshot(ids); }
    }

    public void ForEachWithComponents<T1, T2, T3, T4, T5>(Action<Entity, T1, T2, T3, T4, T5> action)
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        where T5 : Component
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        var pool3 = GetPool<T3>();
        var pool4 = GetPool<T4>();
        var pool5 = GetPool<T5>();
        if (pool1 == null || pool2 == null || pool3 == null || pool4 == null || pool5 == null) return;

        int c1 = pool1.Count, c2 = pool2.Count, c3 = pool3.Count, c4 = pool4.Count, c5 = pool5.Count;
        int smallest = c1;
        int which = 1;
        if (c2 < smallest) { smallest = c2; which = 2; }
        if (c3 < smallest) { smallest = c3; which = 3; }
        if (c4 < smallest) { smallest = c4; which = 4; }
        if (c5 < smallest) { which = 5; }

        IComponentPool iterPool = which switch
        {
            2 => pool2,
            3 => pool3,
            4 => pool4,
            5 => pool5,
            _ => pool1
        };

        var (ids, len) = iterPool.GetEntityIdSnapshot();
        try
        {
            for (int i = 0; i < len; i++)
            {
                var entityId = ids[i];
                var entity = GetEntityById(entityId);
                if (entity == null || !entity.IsActive) continue;
                var comp1 = GetComponentFromPool<T1>(entityId);
                if (comp1 == null) continue;
                var comp2 = GetComponentFromPool<T2>(entityId);
                if (comp2 == null) continue;
                var comp3 = GetComponentFromPool<T3>(entityId);
                if (comp3 == null) continue;
                var comp4 = GetComponentFromPool<T4>(entityId);
                if (comp4 == null) continue;
                var comp5 = GetComponentFromPool<T5>(entityId);
                if (comp5 != null)
                    action(entity, comp1, comp2, comp3, comp4, comp5);
            }
        }
        finally { iterPool.ReturnEntityIdSnapshot(ids); }
    }

    public void ForEachWithBehavior<T>(Action<Entity, T> action) where T : Behavior
    {
        var count = _behaviors.Count;
        if (count == 0) return;

        var snapshot = ArrayPool<Behavior>.Shared.Rent(count);
        try
        {
            int i = 0;
            foreach (var b in _behaviors)
                snapshot[i++] = b;

            for (int j = 0; j < count; j++)
            {
                if (snapshot[j] is T match && match.Entity?.IsActive == true)
                    action(match.Entity!, match);
            }
        }
        finally
        {
            ArrayPool<Behavior>.Shared.Return(snapshot, clearArray: true);
        }
    }

    public void ForEachWithBehavior<T>(Action<Entity> action) where T : Behavior
    {
        var count = _behaviors.Count;
        if (count == 0) return;

        var snapshot = ArrayPool<Behavior>.Shared.Rent(count);
        try
        {
            int i = 0;
            foreach (var b in _behaviors)
                snapshot[i++] = b;

            for (int j = 0; j < count; j++)
            {
                if (snapshot[j] is T && snapshot[j].Entity?.IsActive == true)
                    action(snapshot[j].Entity!);
            }
        }
        finally
        {
            ArrayPool<Behavior>.Shared.Return(snapshot, clearArray: true);
        }
    }

    public IEnumerable<Entity> GetEntitiesWithBehavior<T>() where T : Behavior
    {
        var count = _behaviors.Count;
        if (count == 0) return [];

        var results = new List<Entity>();
        var snapshot = ArrayPool<Behavior>.Shared.Rent(count);
        try
        {
            int i = 0;
            foreach (var b in _behaviors)
                snapshot[i++] = b;

            for (int j = 0; j < count; j++)
            {
                if (snapshot[j] is T && snapshot[j].Entity?.IsActive == true)
                    results.Add(snapshot[j].Entity!);
            }
        }
        finally
        {
            ArrayPool<Behavior>.Shared.Return(snapshot, clearArray: true);
        }
        return results;
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

    public IEnumerable<Entity> GetEntitiesWithComponents<T1, T2, T3>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        var pool3 = GetPool<T3>();
        if (pool1 == null || pool2 == null || pool3 == null) return [];

        int c1 = pool1.Count, c2 = pool2.Count, c3 = pool3.Count;
        var results = new List<Entity>();

        // Iterate smallest pool
        if (c1 <= c2 && c1 <= c3)
        {
            var (snapshot, length) = pool1.GetTypedSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, _) = snapshot[i];
                    var entity = GetEntityById(entityId);
                    if (entity != null && entity.IsActive && entity.HasComponent<T2>() && entity.HasComponent<T3>())
                        results.Add(entity);
                }
            }
            finally { pool1.ReturnTypedSnapshot(snapshot); }
        }
        else if (c2 <= c1 && c2 <= c3)
        {
            var (snapshot, length) = pool2.GetTypedSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, _) = snapshot[i];
                    var entity = GetEntityById(entityId);
                    if (entity != null && entity.IsActive && entity.HasComponent<T1>() && entity.HasComponent<T3>())
                        results.Add(entity);
                }
            }
            finally { pool2.ReturnTypedSnapshot(snapshot); }
        }
        else
        {
            var (snapshot, length) = pool3.GetTypedSnapshot();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var (entityId, _) = snapshot[i];
                    var entity = GetEntityById(entityId);
                    if (entity != null && entity.IsActive && entity.HasComponent<T1>() && entity.HasComponent<T2>())
                        results.Add(entity);
                }
            }
            finally { pool3.ReturnTypedSnapshot(snapshot); }
        }

        return results;
    }

    public IEnumerable<Entity> GetEntitiesWithComponents<T1, T2, T3, T4>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        var pool3 = GetPool<T3>();
        var pool4 = GetPool<T4>();
        if (pool1 == null || pool2 == null || pool3 == null || pool4 == null) return [];

        int c1 = pool1.Count, c2 = pool2.Count, c3 = pool3.Count, c4 = pool4.Count;
        int smallest = c1;
        int which = 1;
        if (c2 < smallest) { smallest = c2; which = 2; }
        if (c3 < smallest) { smallest = c3; which = 3; }
        if (c4 < smallest) { which = 4; }

        var results = new List<Entity>();

        IComponentPool iterPool = which switch
        {
            2 => pool2,
            3 => pool3,
            4 => pool4,
            _ => pool1
        };

        var (ids, len) = iterPool.GetEntityIdSnapshot();
        try
        {
            for (int i = 0; i < len; i++)
            {
                var entity = GetEntityById(ids[i]);
                if (entity != null && entity.IsActive &&
                    entity.HasComponent<T1>() && entity.HasComponent<T2>() &&
                    entity.HasComponent<T3>() && entity.HasComponent<T4>())
                    results.Add(entity);
            }
        }
        finally { iterPool.ReturnEntityIdSnapshot(ids); }

        return results;
    }

    public IEnumerable<Entity> GetEntitiesWithComponents<T1, T2, T3, T4, T5>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        where T5 : Component
    {
        var pool1 = GetPool<T1>();
        var pool2 = GetPool<T2>();
        var pool3 = GetPool<T3>();
        var pool4 = GetPool<T4>();
        var pool5 = GetPool<T5>();
        if (pool1 == null || pool2 == null || pool3 == null || pool4 == null || pool5 == null) return [];

        int c1 = pool1.Count, c2 = pool2.Count, c3 = pool3.Count, c4 = pool4.Count, c5 = pool5.Count;
        int smallest = c1;
        int which = 1;
        if (c2 < smallest) { smallest = c2; which = 2; }
        if (c3 < smallest) { smallest = c3; which = 3; }
        if (c4 < smallest) { smallest = c4; which = 4; }
        if (c5 < smallest) { which = 5; }

        var results = new List<Entity>();

        IComponentPool iterPool = which switch
        {
            2 => pool2,
            3 => pool3,
            4 => pool4,
            5 => pool5,
            _ => pool1
        };

        var (ids, len) = iterPool.GetEntityIdSnapshot();
        try
        {
            for (int i = 0; i < len; i++)
            {
                var entity = GetEntityById(ids[i]);
                if (entity != null && entity.IsActive &&
                    entity.HasComponent<T1>() && entity.HasComponent<T2>() &&
                    entity.HasComponent<T3>() && entity.HasComponent<T4>() &&
                    entity.HasComponent<T5>())
                    results.Add(entity);
            }
        }
        finally { iterPool.ReturnEntityIdSnapshot(ids); }

        return results;
    }

    public Entity? FindEntity(Func<Entity, bool> predicate)
    {
        foreach (var entity in _entities)
            if (entity.IsActive && predicate(entity)) return entity;
        return null;
    }

    public Entity? FindEntity(Func<Entity, bool> predicate, bool includeInactive)
    {
        if (!includeInactive) return FindEntity(predicate);
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
                if (_updateSystems.IsQueuedForRemoval(system)) continue;
                _currentSystemType = system.GetType();
                try { system.Update(this, gameTime); }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error updating system: {SystemType}", system.GetType().Name);
                    if (_options.PropagateExceptions) throw;
                    try { _options.OnExceptionSwallowed?.Invoke(ex, system.GetType().Name); } catch { }
                }
                finally { _currentSystemType = null; }
            }

            RebuildBehaviorUpdateOrder();

            for (int i = 0; i < _behaviorsByUpdateOrder.Count; i++)
            {
                var behavior = _behaviorsByUpdateOrder[i];
                if (!behavior.IsEnabled || behavior.Entity?.IsActive != true)
                    continue;
                if (behavior._startFailed) continue;
                try
                {
                    if (!behavior._started)
                    {
                        behavior._started = true;
                        try { behavior.OnStart(); }
                        catch { behavior._startFailed = true; throw; }
                    }
                    behavior.Update(gameTime);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in behavior {BehaviorType} on '{EntityName}'", behavior.GetType().Name, behavior.Entity?.Name);
                    if (_options.PropagateExceptions) throw;
                    try { _options.OnExceptionSwallowed?.Invoke(ex, $"{behavior.GetType().Name} on '{behavior.Entity?.Name}'"); } catch { }
                }
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
                if (_fixedUpdateSystems.IsQueuedForRemoval(system)) continue;
                _currentSystemType = system.GetType();
                try { system.FixedUpdate(this, fixedTime); }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in fixed update system: {SystemType}", system.GetType().Name);
                    if (_options.PropagateExceptions) throw;
                    try { _options.OnExceptionSwallowed?.Invoke(ex, system.GetType().Name); } catch { }
                }
                finally { _currentSystemType = null; }
            }

            RebuildBehaviorFixedUpdateOrder();

            for (int i = 0; i < _behaviorsByFixedUpdateOrder.Count; i++)
            {
                var behavior = _behaviorsByFixedUpdateOrder[i];
                if (!behavior.IsEnabled || behavior.Entity?.IsActive != true)
                    continue;
                if (behavior._startFailed) continue;
                try
                {
                    if (!behavior._started)
                    {
                        behavior._started = true;
                        try { behavior.OnStart(); }
                        catch { behavior._startFailed = true; throw; }
                    }
                    behavior.FixedUpdate(fixedTime);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in fixed update behavior {BehaviorType} on '{EntityName}'", behavior.GetType().Name, behavior.Entity?.Name);
                    if (_options.PropagateExceptions) throw;
                    try { _options.OnExceptionSwallowed?.Invoke(ex, $"{behavior.GetType().Name} on '{behavior.Entity?.Name}'"); } catch { }
                }
            }
        }
        finally
        {
            ProcessDeferredOperations();
        }
    }

    public void Render(IRenderer renderer, GameTime gameTime)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // No ProcessDeferredOperations() call here: Render is intentionally read-only.
        // Entities destroyed in Update() have IsActive set to false immediately, so render
        // systems correctly skip them without needing a full structural flush.
        // Structural mutations made inside Render (e.g., lazy-spawning a VFX entity) will
        // be applied at the start of the next Update() call.

        if (!_renderSystemsSorted)
        {
            _renderSystems.SortCommitted((a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
            _renderSystemsSorted = true;
            _logger?.LogDebug("Render systems sorted ({Count} systems)", _renderSystems.Count);
        }

        foreach (var system in _renderSystems)
        {
            if (!system.IsEnabled) continue;
            if (_renderSystems.IsQueuedForRemoval(system)) continue;
            _currentSystemType = system.GetType();
            try { system.Render(this, renderer, gameTime); }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rendering system: {SystemType}", system.GetType().Name);
                if (_options.PropagateExceptions) throw;
                try { _options.OnExceptionSwallowed?.Invoke(ex, system.GetType().Name); } catch { }
            }
            finally { _currentSystemType = null; }
        }

        RebuildBehaviorRenderOrder();

        for (int i = 0; i < _behaviorsByRenderOrder.Count; i++)
        {
            var behavior = _behaviorsByRenderOrder[i];
            if (!behavior.IsEnabled || behavior.Entity?.IsActive != true)
                continue;
            if (behavior._startFailed) continue;
            try
            {
                if (!behavior._started)
                {
                    behavior._started = true;
                    try { behavior.OnStart(); }
                    catch { behavior._startFailed = true; throw; }
                }
                behavior.Render(renderer, gameTime);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rendering behavior {BehaviorType} on '{EntityName}'", behavior.GetType().Name, behavior.Entity?.Name);
                if (_options.PropagateExceptions) throw;
                try { _options.OnExceptionSwallowed?.Invoke(ex, $"{behavior.GetType().Name} on '{behavior.Entity?.Name}'"); } catch { }
            }
        }
    }

    /// <summary>
    /// Clears all entities and systems from the world.
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
    /// Destroys all entities but keeps systems intact. Use for scene resets
    /// where the system pipeline should remain unchanged.
    /// </summary>
    public void ClearEntities()
    {
        _logger?.LogDebug("Clearing entities");

        TeardownEntities();

        foreach (var q in _cachedQueries)
            q.Invalidate();

        _logger?.LogInformation("Entities cleared: {SystemCount} systems retained", _registeredSystems.Count);
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
        _sequentialSystemTypes.Clear();
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

#if DEBUG
        if (_cachedQueries.Count > 0)
        {
            _logger?.LogWarning(
                "{Count} CachedEntityQuery instance(s) were not disposed before the world was torn down. " +
                "Call Dispose() on each query (typically inside the owning system's Dispose method) to avoid " +
                "stale invalidation registrations and prevent GC of the query result sets. " +
                "Undisposed queries: {Types}",
                _cachedQueries.Count,
                string.Join(", ", _cachedQueries.Select(q => q.GetType().Name)));
        }
#endif

        foreach (var entity in _entities)
        {
            if (!entity.PoolsCleared)
                entity.OnDestroy();
            EntityDestroyed?.Invoke(entity);
            entity.SetWorld(null);
        }

        _entities.Clear();
        _behaviors.Clear();
        _entityLookup.Clear();
        _tagIndex.Clear();
        _componentPools.Clear();
        _typeHierarchy.Clear();
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
        {
            var (update, _, _) = GetBehaviorPipelines(behavior.GetType());
            if (update) _behaviorsByUpdateOrder.Add(behavior);
        }

        SortHelper.StableSort(_behaviorsByUpdateOrder, static (a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
        _behaviorUpdateOrderDirty = false;
    }

    private void RebuildBehaviorFixedUpdateOrder()
    {
        if (!_behaviorFixedUpdateOrderDirty) return;

        _behaviorsByFixedUpdateOrder.Clear();
        foreach (var behavior in _behaviors)
        {
            var (_, fixedUpdate, _) = GetBehaviorPipelines(behavior.GetType());
            if (fixedUpdate) _behaviorsByFixedUpdateOrder.Add(behavior);
        }

        SortHelper.StableSort(_behaviorsByFixedUpdateOrder, static (a, b) => a.FixedUpdateOrder.CompareTo(b.FixedUpdateOrder));
        _behaviorFixedUpdateOrderDirty = false;
    }

    private void RebuildBehaviorRenderOrder()
    {
        if (!_behaviorRenderOrderDirty) return;

        _behaviorsByRenderOrder.Clear();
        foreach (var behavior in _behaviors)
        {
            var (_, _, render) = GetBehaviorPipelines(behavior.GetType());
            if (render) _behaviorsByRenderOrder.Add(behavior);
        }

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
            EntityCreated?.Invoke(entity);
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

            EntityDestroyed?.Invoke(entity);
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

    internal void NotifyEnabledChanged()
    {
        foreach (var q in _queriesFilteringEnabled)
            q.Invalidate();
    }

    internal void NotifyBehaviorAdded(Entity entity, Behavior behavior)
    {
        _behaviors.Add(behavior);
        var (update, fixedUpdate, render) = GetBehaviorPipelines(behavior.GetType());
        if (update) _behaviorUpdateOrderDirty = true;
        if (fixedUpdate) _behaviorFixedUpdateOrderDirty = true;
        if (render) _behaviorRenderOrderDirty = true;
        InvalidateCachedQueriesForBehaviors();
    }

    internal void NotifyBehaviorRemoved(Entity entity, Behavior behavior)
    {
        _behaviors.Remove(behavior);
        var (update, fixedUpdate, render) = GetBehaviorPipelines(behavior.GetType());
        if (update) _behaviorUpdateOrderDirty = true;
        if (fixedUpdate) _behaviorFixedUpdateOrderDirty = true;
        if (render) _behaviorRenderOrderDirty = true;
        InvalidateCachedQueriesForBehaviors();
    }

    /// <summary>
    /// Returns <see langword="true"/> when the system currently executing inside
    /// <see cref="Update"/>, <see cref="FixedUpdate"/>, or <see cref="Render"/> has
    /// the <see cref="Systems.SequentialAttribute"/> or when
    /// <see cref="ECSOptions.EnableMultiThreading"/> is globally disabled.
    /// Used by <see cref="CachedEntityQueryBase"/> to suppress parallelism for
    /// individual systems without requiring engine-wide multi-threading to be off.
    /// </summary>
    internal bool IsCurrentSystemSequential()
        => !_options.EnableMultiThreading ||
           (_currentSystemType != null && _sequentialSystemTypes.Contains(_currentSystemType));

    /// <summary>
    /// Returns the <see cref="ECSOptions"/> instance that should govern parallel
    /// execution for the currently dispatching system. A cached copy with
    /// <c>EnableMultiThreading = false</c> is returned when the current system
    /// is sequential; otherwise the shared <see cref="_options"/> instance is returned.
    /// </summary>
    internal ECSOptions GetEffectiveOptions()
    {
        if (!IsCurrentSystemSequential())
            return _options;

        return _sequentialOptions ??= new ECSOptions
        {
            EnableMultiThreading = false,
            ParallelEntityThreshold = _options.ParallelEntityThreshold,
            InitialEntityCapacity = _options.InitialEntityCapacity,
            FixedTimeStepMs = _options.FixedTimeStepMs,
            MaxFixedStepsPerFrame = _options.MaxFixedStepsPerFrame,
            WorkerThreadCount = _options.WorkerThreadCount,
        };
    }

    public EntityQuery Query() => new(this, _options);

    public CachedEntityQueryBuilder<T1> CreateCachedQuery<T1>() where T1 : Component
        => new(this, GetEffectiveOptions());

    public CachedEntityQueryBuilder<T1, T2> CreateCachedQuery<T1, T2>()
        where T1 : Component
        where T2 : Component
        => new(this, GetEffectiveOptions());

    public CachedEntityQueryBuilder<T1, T2, T3> CreateCachedQuery<T1, T2, T3>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        => new(this, GetEffectiveOptions());

    public CachedEntityQueryBuilder<T1, T2, T3, T4> CreateCachedQuery<T1, T2, T3, T4>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        => new(this, GetEffectiveOptions());

    public CachedEntityQueryBuilder<T1, T2, T3, T4, T5> CreateCachedQuery<T1, T2, T3, T4, T5>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        where T5 : Component
        => new(this, GetEffectiveOptions());

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
            var component = pool.Get(entityId);
            if (component != null)
            {
                component.OnRemoved();
                component.Entity = null;
            }

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