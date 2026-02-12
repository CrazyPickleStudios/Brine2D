using Brine2D.Core;
using Brine2D.ECS.Query;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Brine2D.ECS;

/// <summary>
/// Default implementation of IEntityWorld.
/// Uses deferred operations pattern - structural changes (create/destroy) are queued
/// during frame execution and applied at frame boundaries.
/// 
/// Performance: Optimized for 1,000-10,000+ entities using hot list architecture.
/// Philosophy: "Good enough" performance with simple mental model (ASP.NET-style).
/// </summary>
public class EntityWorld : IEntityWorld
{
    // Main entity list (deferred)
    private readonly DeferredList<Entity> _entities = new();
    
    // Hot lists - only entities/components that override lifecycle methods (deferred)
    private readonly DeferredList<Entity> _updatableEntities = new();
    private readonly DeferredList<Component> _updatableComponents = new();
    private readonly DeferredList<Entity> _renderableEntities = new();
    private readonly DeferredList<Component> _renderableComponents = new();
    
    // Deferred operation queue for complex registrations
    private readonly DeferredOperationQueue<(Entity entity, Component component)> _deferredComponentRegistrations;
    private readonly DeferredOperationQueue<(Entity entity, Component component)> _deferredComponentUnregistrations;
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<EntityWorld>? _logger;
    private readonly ECSOptions _options;
    
    // Flag to control when structural changes are deferred
    private bool _isProcessing = false;

    private readonly List<ICachedQuery> _cachedQueries = new();

    public IReadOnlyList<Entity> Entities => _entities.AsReadOnly();

    public EntityWorld(
        IServiceProvider serviceProvider,
        ILoggerFactory? loggerFactory = null,
        IOptions<ECSOptions>? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<EntityWorld>();
        _options = options?.Value ?? new ECSOptions();
        
        // Initialize deferred operation queues for complex operations
        _deferredComponentRegistrations = new(tuple => RegisterComponentToHotLists(tuple.entity, tuple.component));
        _deferredComponentUnregistrations = new(tuple => UnregisterComponentFromHotLists(tuple.entity, tuple.component));
    }

    public Entity CreateEntity(string name = "")
    {
        var logger = _loggerFactory?.CreateLogger<Entity>();
        var entity = new Entity(logger)
        {
            Name = name,
            World = this
        };

        // Queue entity for creation
        _entities.Add(entity);
        _logger?.LogDebug("Queued entity creation: {Name} ({Id})", entity.Name, entity.Id);

        return entity;
    }

    public T CreateEntity<T>(string name = "") where T : Entity, new()
    {
        var entity = new T
        {
            Name = name,
            World = this
        };

        // Queue entity for creation
        _entities.Add(entity);
        _logger?.LogDebug("Queued entity creation: {Name} ({Id})", entity.Name, entity.Id);

        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
        // Avoid double-queuing for destruction
        if (!_entities.IsQueuedForRemoval(entity))
        {
            _entities.Remove(entity);
            entity.IsActive = false; // Deactivate immediately so queries skip it
            _logger?.LogDebug("Queued entity destruction: {Name} ({Id})", entity.Name, entity.Id);
        }
    }

    public Entity? GetEntityById(Guid id)
    {
        foreach (var entity in _entities)
        {
            if (entity.Id == id)
                return entity;
        }
        return null;
    }

    public Entity? GetEntityByName(string name)
    {
        foreach (var entity in _entities)
        {
            if (entity.Name == name)
                return entity;
        }
        return null;
    }

    public IEnumerable<Entity> GetEntitiesByTag(string tag)
    {
        foreach (var entity in _entities)
        {
            if (entity.IsActive && entity.Tags.Contains(tag))
                yield return entity;
        }
    }

    public IEnumerable<Entity> GetEntitiesWithComponent<T>() where T : Component
    {
        foreach (var entity in _entities)
        {
            if (entity.IsActive && entity.HasComponent<T>())
                yield return entity;
        }
    }

    public IEnumerable<Entity> GetEntitiesWithComponents<T1, T2>()
        where T1 : Component
        where T2 : Component
    {
        foreach (var entity in _entities)
        {
            if (entity.IsActive && entity.HasComponent<T1>() && entity.HasComponent<T2>())
                yield return entity;
        }
    }

    public Entity? FindEntity(Func<Entity, bool> predicate)
    {
        foreach (var entity in _entities)
        {
            if (predicate(entity))
                return entity;
        }
        return null;
    }

    public void Update(GameTime gameTime)
    {
        _isProcessing = true;

        try
        {
            // Apply deferred operations from previous frame
            ProcessDeferredOperations();

            // Update only entities that override OnUpdate (hot list optimization)
            foreach (var entity in _updatableEntities)
            {
                if (!entity.IsActive) continue;
                entity.OnUpdate(gameTime);
            }
            
            // Update only components that override OnUpdate (hot list optimization)
            foreach (var component in _updatableComponents)
            {
                if (!component.IsEnabled || component.Entity?.IsActive != true) continue;
                component.OnUpdate(gameTime);
            }
        }
        finally
        {
            _isProcessing = false;

            // Apply operations that were queued during this frame
            ProcessDeferredOperations();
        }
    }

    public void Render(IRenderer renderer)
    {
        // Render only entities that override OnRender (hot list optimization)
        foreach (var entity in _renderableEntities)
        {
            if (!entity.IsActive) continue;
            entity.OnRender(renderer);
        }
        
        // Render only components that override OnRender (hot list optimization)
        foreach (var component in _renderableComponents)
        {
            if (!component.IsEnabled || component.Entity?.IsActive != true) continue;
            component.OnRender(renderer);
        }
    }

    public void Clear()
    {
        _logger?.LogDebug("Clearing all entities from world");

        // Queue all entities for destruction
        foreach (var entity in _entities)
        {
            _entities.Remove(entity);
            entity.IsActive = false;
        }

        _logger?.LogInformation("World cleared: entities queued for destruction");

        // Force immediate processing
        var wasProcessing = _isProcessing;
        _isProcessing = false;
        
        try
        {
            ProcessDeferredOperations();
        }
        finally
        {
            _isProcessing = wasProcessing;
        }
    }

    /// <summary>
    /// Internal notification when a component is added.
    /// </summary>
    internal void NotifyComponentAdded(Entity entity, Component component)
    {
        // Invalidate all cached queries
        InvalidateAllCachedQueries();
        
        if (_isProcessing)
        {
            // Defer registration
            _deferredComponentRegistrations.Enqueue((entity, component));
        }
        else
        {
            RegisterComponentToHotLists(entity, component);
        }
    }

    /// <summary>
    /// Internal notification when a component is removed.
    /// </summary>
    internal void NotifyComponentRemoved(Entity entity, Component component)
    {
        // Invalidate all cached queries
        InvalidateAllCachedQueries();
        
        if (_isProcessing)
        {
            // Defer unregistration
            _deferredComponentUnregistrations.Enqueue((entity, component));
        }
        else
        {
            UnregisterComponentFromHotLists(entity, component);
        }
    }

    /// <summary>
    /// Registers entity to hot lists if it overrides lifecycle methods.
    /// </summary>
    private void RegisterEntityToHotLists(Entity entity)
    {
        var entityType = entity.GetType();

        // Only use reflection if entity is derived (optimization)
        if (entityType != typeof(Entity))
        {
            // Check OnUpdate override
            var updateMethod = entityType.GetMethod(
                nameof(Entity.OnUpdate),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (updateMethod != null)
            {
                _updatableEntities.Add(entity);
            }

            // Check OnRender override
            var renderMethod = entityType.GetMethod(
                nameof(Entity.OnRender),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (renderMethod != null)
            {
                _renderableEntities.Add(entity);
            }
        }
    }

    /// <summary>
    /// Removes entity from hot lists.
    /// </summary>
    private void UnregisterEntityFromHotLists(Entity entity)
    {
        _updatableEntities.Remove(entity);
        _renderableEntities.Remove(entity);
    }

    /// <summary>
    /// Registers component to hot lists if it overrides lifecycle methods.
    /// </summary>
    private void RegisterComponentToHotLists(Entity entity, Component component)
    {
        var componentType = component.GetType();

        // Only use reflection if component is derived (optimization)
        if (componentType != typeof(Component))
        {
            // Check OnUpdate override
            var updateMethod = componentType.GetMethod(
                nameof(Component.OnUpdate),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (updateMethod != null)
            {
                _updatableComponents.Add(component);
            }

            // Check OnRender override
            var renderMethod = componentType.GetMethod(
                nameof(Component.OnRender),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (renderMethod != null)
            {
                _renderableComponents.Add(component);
            }
        }
    }

    /// <summary>
    /// Removes component from hot lists.
    /// </summary>
    private void UnregisterComponentFromHotLists(Entity entity, Component component)
    {
        _updatableComponents.Remove(component);
        _renderableComponents.Remove(component);
    }

    /// <summary>
    /// Processes all deferred operations (creations, destructions, registrations).
    /// Loops until all queues are empty to handle cascading operations.
    /// </summary>
    private void ProcessDeferredOperations()
    {
        // Keep processing until ALL lists/queues are drained (handles cascading)
        while (_entities.HasPendingChanges || 
               _updatableEntities.HasPendingChanges ||
               _updatableComponents.HasPendingChanges ||
               _renderableEntities.HasPendingChanges ||
               _renderableComponents.HasPendingChanges ||
               _deferredComponentRegistrations.HasPending ||
               _deferredComponentUnregistrations.HasPending)
        {
            // Process entity additions first (triggers OnInitialize which may add components)
            ProcessEntityAdditions();
            
            // Process component registrations
            _deferredComponentRegistrations.ProcessAll();
            
            // Process component unregistrations
            _deferredComponentUnregistrations.ProcessAll();
            
            // Process all hot list changes
            _updatableEntities.ProcessChanges();
            _updatableComponents.ProcessChanges();
            _renderableEntities.ProcessChanges();
            _renderableComponents.ProcessChanges();
            
            // Process entity removals last (triggers OnDestroy which may remove more entities)
            ProcessEntityRemovals();
        }
    }

    /// <summary>
    /// Processes pending entity additions.
    /// </summary>
    private void ProcessEntityAdditions()
    {
        _entities.ProcessAdds(entity =>
        {
            RegisterEntityToHotLists(entity);
            entity.OnInitialize();
            _logger?.LogDebug("Created entity: {Name} ({Id})", entity.Name, entity.Id);
            InvalidateAllCachedQueries();
        });
    }

    /// <summary>
    /// Processes pending entity removals.
    /// </summary>
    private void ProcessEntityRemovals()
    {
        _entities.ProcessRemovals(entity =>
        {
            UnregisterEntityFromHotLists(entity);
            entity.OnDestroy();
            _logger?.LogDebug("Destroyed entity: {Name} ({Id})", entity.Name, entity.Id);
            InvalidateAllCachedQueries();
        });
    }

    public T? GetService<T>() where T : class
    {
        return _serviceProvider.GetService<T>();
    }

    public T GetRequiredService<T>() where T : class
    {
        var service = _serviceProvider.GetService<T>();
        if (service == null)
        {
            throw new InvalidOperationException(
                $"Required service '{typeof(T).Name}' is not registered. " +
                $"Did you forget to register it in your Program.cs?");
        }
        return service;
    }

    public EntityQuery Query()
    {
        return new EntityQuery(this, _options);
    }

    public CachedEntityQueryBuilder<T1> CreateCachedQuery<T1>() where T1 : Component
    {
        return new CachedEntityQueryBuilder<T1>(this);
    }

    public CachedEntityQueryBuilder<T1, T2> CreateCachedQuery<T1, T2>()
        where T1 : Component
        where T2 : Component
    {
        return new CachedEntityQueryBuilder<T1, T2>(this);
    }

    public CachedEntityQueryBuilder<T1, T2, T3> CreateCachedQuery<T1, T2, T3>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
    {
        return new CachedEntityQueryBuilder<T1, T2, T3>(this);
    }

    /// <summary>
    /// Immediately processes all pending structural changes.
    /// Use this for testing or when you need changes to be visible immediately.
    /// During normal gameplay, changes are automatically processed at frame boundaries via Update().
    /// </summary>
    public void Flush()
    {
        var wasProcessing = _isProcessing;
        _isProcessing = false;
        
        try
        {
            ProcessDeferredOperations();
        }
        finally
        {
            _isProcessing = wasProcessing;
        }
    }

    internal void RegisterCachedQuery(ICachedQuery query)
    {
        _cachedQueries.Add(query);
    }

    private void InvalidateAllCachedQueries()
    {
        foreach (var query in _cachedQueries)
        {
            query.Invalidate();
        }
    }
}