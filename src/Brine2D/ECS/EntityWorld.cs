using Brine2D.Core;
using Brine2D.ECS.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brine2D.ECS;

/// <summary>
/// Default implementation of IEntityWorld.
/// Manages all entities in the game world.
/// </summary>
public class EntityWorld : IEntityWorld
{
    private readonly List<Entity> _entities = new();
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<EntityWorld>? _logger;
    private readonly ECSOptions _options; 

    public IReadOnlyList<Entity> Entities => _entities.AsReadOnly();

    public event Action<Entity>? OnEntityCreated;
    public event Action<Entity>? OnEntityDestroyed;
    public event Action<Entity, Component>? OnComponentAdded;
    public event Action<Entity, Component>? OnComponentRemoved;

    // UPDATE CONSTRUCTOR:
    public EntityWorld(ILoggerFactory? loggerFactory = null, IOptions<ECSOptions>? options = null)
    {
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

        _entities.Add(entity);
        entity.OnInitialize();

        OnEntityCreated?.Invoke(entity);

        _logger?.LogDebug("Created entity: {Name} ({Id})", entity.Name, entity.Id);
        return entity;
    }

    public T CreateEntity<T>(string name = "") where T : Entity, new()
    {
        var entity = new T
        {
            Name = name,
            World = this
        };

        _entities.Add(entity);
        entity.OnInitialize();

        OnEntityCreated?.Invoke(entity);

        _logger?.LogDebug("Created entity: {Name} ({Id})", entity.Name, entity.Id);
        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
        if (_entities.Remove(entity))
        {
            entity.OnDestroy();
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
        // Always return snapshot
        return _entities.Where(e => e.Tags.Contains(tag)).ToList();
    }

    public IReadOnlyList<Entity> GetEntitiesWithComponent<T>() where T : Component
    {
        // Always return snapshot for safe iteration
        return _entities.Where(e => e.HasComponent<T>()).ToList();
    }

    public IReadOnlyList<Entity> GetEntitiesWithComponents<T1, T2>() 
        where T1 : Component 
        where T2 : Component
    {
        // Always return snapshot
        return _entities.Where(e => e.HasComponent<T1>() && e.HasComponent<T2>()).ToList();
    }

    public Entity? FindEntity(Func<Entity, bool> predicate)
    {
        return _entities.FirstOrDefault(predicate);
    }

    public void Update(GameTime gameTime)
    {
        foreach (var entity in _entities.ToList())
        {
            entity.OnUpdate(gameTime);
        }
    }

    public void Clear()
    {
        _logger?.LogDebug("Clearing all entities from world");
        
        // Destroy entities in REVERSE creation order to handle dependencies properly.
        // This ensures children are destroyed before parents, spawned entities before spawners, etc.
        var allEntities = _entities.ToList();
        
        for (int i = allEntities.Count - 1; i >= 0; i--)
        {
            DestroyEntity(allEntities[i]);
        }
        
        _logger?.LogInformation("World cleared: {Count} entities destroyed", allEntities.Count);
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

    // These DON'T need options (just caching entities):
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
}