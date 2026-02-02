using Brine2D.Core;
using Brine2D.ECS.Query;
using Brine2D.Rendering;

namespace Brine2D.ECS;

/// <summary>
/// Interface for the entity world that manages all entities.
/// </summary>
public interface IEntityWorld
{
    /// <summary>
    /// Gets all entities in the world.
    /// </summary>
    IReadOnlyList<Entity> Entities { get; }

    /// <summary>
    /// Event fired when an entity is created.
    /// </summary>
    event Action<Entity>? OnEntityCreated;

    /// <summary>
    /// Event fired when an entity is destroyed.
    /// </summary>
    event Action<Entity>? OnEntityDestroyed;

    /// <summary>
    /// Event fired when a component is added to any entity.
    /// </summary>
    event Action<Entity, Component>? OnComponentAdded;

    /// <summary>
    /// Event fired when a component is removed from any entity.
    /// </summary>
    event Action<Entity, Component>? OnComponentRemoved;

    /// <summary>
    /// Creates a new entity in the world.
    /// </summary>
    Entity CreateEntity(string name = "");

    /// <summary>
    /// Creates a new entity of a specific type.
    /// </summary>
    T CreateEntity<T>(string name = "") where T : Entity, new();

    /// <summary>
    /// Destroys an entity and removes it from the world.
    /// </summary>
    void DestroyEntity(Entity entity);

    /// <summary>
    /// Gets an entity by its unique ID.
    /// </summary>
    Entity? GetEntityById(Guid id);

    /// <summary>
    /// Gets an entity by name (returns first match).
    /// </summary>
    Entity? GetEntityByName(string name);

    /// <summary>
    /// Gets all entities with a specific tag.
    /// </summary>
    IReadOnlyList<Entity> GetEntitiesByTag(string tag);

    /// <summary>
    /// Gets all entities with a specific component type.
    /// </summary>
    IReadOnlyList<Entity> GetEntitiesWithComponent<T>() where T : Component;

    /// <summary>
    /// Gets all entities that have both specified component types.
    /// </summary>
    IReadOnlyList<Entity> GetEntitiesWithComponents<T1, T2>()
        where T1 : Component 
        where T2 : Component;

    /// <summary>
    /// Finds the first entity that matches the specified predicate.
    /// </summary>
    Entity? FindEntity(Func<Entity, bool> predicate);

    /// <summary>
    /// Updates all entities in the world.
    /// </summary>
    void Update(GameTime gameTime);

    /// <summary>
    /// Renders all entities in the world.
    /// </summary>
    void Render(IRenderer renderer);

    /// <summary>
    /// Clears all entities from the world.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a service from the DI container.
    /// </summary>
    T? GetService<T>() where T : class;

    /// <summary>
    /// Gets a required service from the DI container.
    /// </summary>
    T GetRequiredService<T>() where T : class;

    /// <summary>
    /// Notifies the world that a component was added to an entity.
    /// </summary>
    void NotifyComponentAdded(Entity entity, Component component);

    /// <summary>
    /// Notifies the world that a component was removed from an entity.
    /// </summary>
    void NotifyComponentRemoved(Entity entity, Component component);

    /// <summary>
    /// Notifies the world that an entity was destroyed.
    /// </summary>
    void NotifyEntityDestroyed(Entity entity);

    /// <summary>
    /// Creates a fluent query builder for searching entities.
    /// </summary>
    /// <example>
    /// <code>
    /// var enemies = World.Query()
    ///     .With&lt;TransformComponent&gt;()
    ///     .WithTag("Enemy")
    ///     .WithinRadius(playerPos, 100f)
    ///     .Execute();
    /// </code>
    /// </example>
    EntityQuery Query();

    /// <summary>
    /// Creates a cached query for better performance when querying repeatedly.
    /// </summary>
    CachedEntityQuery<T1> CreateCachedQuery<T1>() where T1 : Component;

    /// <summary>
    /// Creates a cached query for better performance when querying repeatedly.
    /// </summary>
    CachedEntityQuery<T1, T2> CreateCachedQuery<T1, T2>()
        where T1 : Component
        where T2 : Component;

    /// <summary>
    /// Creates a cached query for better performance when querying repeatedly.
    /// </summary>
    CachedEntityQuery<T1, T2, T3> CreateCachedQuery<T1, T2, T3>()
        where T1 : Component
        where T2 : Component
        where T3 : Component;

    /// <summary>
    /// Forces immediate processing of all deferred operations.
    /// </summary>
    void FlushDeferredOperations();
}