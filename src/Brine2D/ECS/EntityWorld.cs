using Brine2D.Core;
using Brine2D.ECS.Query;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brine2D.ECS;

/// <summary>
/// Default implementation of IEntityWorld.
/// Uses deferred operations pattern - structural changes (create/destroy) are queued
/// during frame execution and applied at frame boundaries.
/// 
/// Performance: Optimized for 1,000-5,000 entities (typical 2D game workload).
/// Philosophy: "Good enough" performance with simple mental model (ASP.NET-style).
/// </summary>
public class EntityWorld : IEntityWorld
{
    private readonly List<Entity> _entities = new();
    
    // Deferred operation queues (modifications are queued, not applied immediately)
    private readonly List<Entity> _entitiesToCreate = new();
    private readonly List<Entity> _entitiesToDestroy = new();
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<EntityWorld>? _logger;
    private readonly ECSOptions _options;
    
    // Flag to control when structural changes are deferred
    private bool _isProcessing = false;

    public IReadOnlyList<Entity> Entities => _entities.AsReadOnly();

    public event Action<Entity>? OnEntityCreated;
    public event Action<Entity>? OnEntityDestroyed;
    public event Action<Entity, Component>? OnComponentAdded;
    public event Action<Entity, Component>? OnComponentRemoved;

    public EntityWorld(
        IServiceProvider serviceProvider,
        ILoggerFactory? loggerFactory = null,
        IOptions<ECSOptions>? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<EntityWorld>();
        _options = options?.Value ?? new ECSOptions();
    }

    public Entity CreateEntity(string name = "")
    {
        var logger = _loggerFactory?.CreateLogger<Entity>();
        var entity = new Entity(logger)
        {
            Name = name,
            World = this
        };

        if (_isProcessing)
        {
            // Defer creation - will be applied at next frame boundary
            _entitiesToCreate.Add(entity);
            _logger?.LogDebug("Deferred entity creation: {Name} ({Id})", entity.Name, entity.Id);
        }
        else
        {
            // Safe to create immediately
            CreateEntityImmediate(entity);
        }

        return entity;
    }

    public T CreateEntity<T>(string name = "") where T : Entity, new()
    {
        var entity = new T
        {
            Name = name,
            World = this
        };

        if (_isProcessing)
        {
            // Defer creation - will be applied at next frame boundary
            _entitiesToCreate.Add(entity);
            _logger?.LogDebug("Deferred entity creation: {Name} ({Id})", entity.Name, entity.Id);
        }
        else
        {
            // Safe to create immediately
            CreateEntityImmediate(entity);
        }

        return entity;
    }

    private void CreateEntityImmediate(Entity entity)
    {
        _entities.Add(entity);
        entity.OnInitialize();
        OnEntityCreated?.Invoke(entity);
        _logger?.LogDebug("Created entity: {Name} ({Id})", entity.Name, entity.Id);
    }

    public void DestroyEntity(Entity entity)
    {
        if (_isProcessing)
        {
            // Defer destruction - will be applied at next frame boundary
            if (!_entitiesToDestroy.Contains(entity))
            {
                _entitiesToDestroy.Add(entity);
                entity.IsActive = false; // Deactivate immediately so queries/updates skip it
                _logger?.LogDebug("Deferred entity destruction: {Name} ({Id})", entity.Name, entity.Id);
            }
        }
        else
        {
            // Safe to destroy immediately
            DestroyEntityImmediate(entity);
        }
    }

    private void DestroyEntityImmediate(Entity entity)
    {
        if (_entities.Remove(entity))
        {
            entity.OnDestroy();
            OnEntityDestroyed?.Invoke(entity);
            _logger?.LogDebug("Destroyed entity: {Name} ({Id})", entity.Name, entity.Id);
        }
    }

    public Entity? GetEntityById(Guid id)
    {
        return _entities.FirstOrDefault(e => e.Id == id);
    }

    public Entity? GetEntityByName(string name)
    {
        return _entities.FirstOrDefault(e => e.Name == name);
    }

    public IReadOnlyList<Entity> GetEntitiesByTag(string tag)
    {
        return _entities.Where(e => e.IsActive && e.Tags.Contains(tag)).ToList();
    }

    public IReadOnlyList<Entity> GetEntitiesWithComponent<T>() where T : Component
    {
        return _entities.Where(e => e.IsActive && e.HasComponent<T>()).ToList();
    }

    public IReadOnlyList<Entity> GetEntitiesWithComponents<T1, T2>()
        where T1 : Component
        where T2 : Component
    {
        return _entities.Where(e => e.IsActive && e.HasComponent<T1>() && e.HasComponent<T2>()).ToList();
    }

    public Entity? FindEntity(Func<Entity, bool> predicate)
    {
        return _entities.FirstOrDefault(predicate);
    }

    public void Update(GameTime gameTime)
    {
        _isProcessing = true;

        try
        {
            // Apply deferred operations from previous frame
            ProcessDeferredOperations();

            // Update all active entities
            // Safe to iterate directly - no structural changes during this loop
            foreach (var entity in _entities)
            {
                if (!entity.IsActive) continue; // Skip deferred-destroy entities
                entity.OnUpdate(gameTime);
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
        // Direct iteration - render doesn't modify collections
        foreach (var entity in _entities)
        {
            if (!entity.IsActive) continue;
            entity.OnRender(renderer);
        }
    }

    public void Clear()
    {
        _logger?.LogDebug("Clearing all entities from world");

        // Queue all entities for destruction
        foreach (var entity in _entities)
        {
            if (!_entitiesToDestroy.Contains(entity))
            {
                _entitiesToDestroy.Add(entity);
                entity.IsActive = false;
            }
        }

        _logger?.LogInformation("World cleared: {Count} entities queued for destruction", _entitiesToDestroy.Count);

        var wasProcessing = _isProcessing;
        _isProcessing = false; // Allow processing
        
        try
        {
            ProcessDeferredOperations(); // Process all destructions NOW
        }
        finally
        {
            _isProcessing = wasProcessing; // Restore flag
        }
    }

    /// <summary>
    /// Processes all deferred operations (creations and destructions).
    /// Uses reverse iteration to avoid expensive array shifts.
    /// Loops until all queues are completely drained (handles cascading operations).
    /// </summary>
    private void ProcessDeferredOperations()
    {
        // Keep processing until ALL queues are empty (handles cascading operations)
        while (_entitiesToCreate.Count > 0 || _entitiesToDestroy.Count > 0)
        {
            // Process creations (forward iteration is fine - small list typically)
            while (_entitiesToCreate.Count > 0)
            {
                var entity = _entitiesToCreate[0];
                _entitiesToCreate.RemoveAt(0);
                CreateEntityImmediate(entity);
            }

            while (_entitiesToDestroy.Count > 0)
            {
                var entity = _entitiesToDestroy[^1]; // Get last item
                _entitiesToDestroy.RemoveAt(_entitiesToDestroy.Count - 1); // Remove from end (O(1))
                DestroyEntityImmediate(entity);
                // Any cascading destructions get added to the END, processed in next iteration
            }
        }
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

    void IEntityWorld.NotifyComponentAdded(Entity entity, Component component)
    {
        OnComponentAdded?.Invoke(entity, component);
    }

    void IEntityWorld.NotifyComponentRemoved(Entity entity, Component component)
    {
        OnComponentRemoved?.Invoke(entity, component);
    }

    void IEntityWorld.NotifyEntityDestroyed(Entity entity)
    {
        OnEntityDestroyed?.Invoke(entity);
    }

    public EntityQuery Query()
    {
        return new EntityQuery(this, _options);
    }

    public CachedEntityQuery<T1> CreateCachedQuery<T1>() where T1 : Component
    {
        return new CachedEntityQuery<T1>(this);
    }

    public CachedEntityQuery<T1, T2> CreateCachedQuery<T1, T2>()
        where T1 : Component
        where T2 : Component
    {
        return new CachedEntityQuery<T1, T2>(this);
    }

    public CachedEntityQuery<T1, T2, T3> CreateCachedQuery<T1, T2, T3>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
    {
        return new CachedEntityQuery<T1, T2, T3>(this);
    }

    /// <summary>
    /// Forces immediate processing of all deferred operations.
    /// Useful for advanced scenarios where you need entities available immediately.
    /// In most cases, deferred operations process automatically at frame boundaries.
    /// </summary>
    public void FlushDeferredOperations()
    {
        var wasProcessing = _isProcessing;
        _isProcessing = false; // Temporarily allow processing

        try
        {
            ProcessDeferredOperations();
        }
        finally
        {
            _isProcessing = wasProcessing;
        }
    }
}