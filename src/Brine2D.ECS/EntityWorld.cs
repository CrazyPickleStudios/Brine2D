using Brine2D.Core;
using Microsoft.Extensions.Logging;

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

    public IReadOnlyList<Entity> Entities => _entities.AsReadOnly();

    public event Action<Entity>? OnEntityCreated;
    public event Action<Entity>? OnEntityDestroyed;
    public event Action<Entity, Component>? OnComponentAdded;
    public event Action<Entity, Component>? OnComponentRemoved;

    public EntityWorld(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<EntityWorld>();
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

    public IEnumerable<Entity> GetEntitiesByTag(string tag)
    {
        return _entities.Where(e => e.Tags.Contains(tag));
    }

    public IEnumerable<Entity> GetEntitiesWithComponent<T>() where T : Component
    {
        return _entities.Where(e => e.HasComponent<T>());
    }

    public IEnumerable<Entity> GetEntitiesWithComponents<T1, T2>() 
        where T1 : Component 
        where T2 : Component
    {
        return _entities.Where(e => e.HasComponent<T1>() && e.HasComponent<T2>());
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
        foreach (var entity in _entities.ToList())
        {
            DestroyEntity(entity);
        }
        _entities.Clear();
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
}