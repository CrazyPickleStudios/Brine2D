using Brine2D.Core;

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
    IEnumerable<Entity> GetEntitiesByTag(string tag);

    /// <summary>
    /// Gets all entities with a specific component type.
    /// </summary>
    IEnumerable<Entity> GetEntitiesWithComponent<T>() where T : Component;

    /// <summary>
    /// Gets all entities that have both specified component types.
    /// Useful for querying entities that need multiple components together.
    /// </summary>
    /// <example>
    /// <code>
    /// // Get all entities with both Transform and Velocity
    /// var movingEntities = world.GetEntitiesWithComponents&lt;TransformComponent, VelocityComponent&gt;();
    /// </code>
    /// </example>
    IEnumerable<Entity> GetEntitiesWithComponents<T1, T2>()
        where T1 : Component 
        where T2 : Component;

    /// <summary>
    /// Finds the first entity that matches the specified predicate.
    /// Returns null if no entity is found.
    /// </summary>
    /// <example>
    /// <code>
    /// // Find player with most health
    /// var player = world.FindEntity(e => 
    ///     e.Tags.Contains("Player") && 
    ///     e.GetComponent&lt;HealthComponent&gt;()?.CurrentHealth > 0
    /// );
    /// </code>
    /// </example>
    Entity? FindEntity(Func<Entity, bool> predicate);

    /// <summary>
    /// Updates all entities in the world.
    /// </summary>
    void Update(GameTime gameTime);

    /// <summary>
    /// Clears all entities from the world.
    /// </summary>
    void Clear();

    /// <summary>
    /// Internal notification that a component was added.
    /// </summary>
    internal void NotifyComponentAdded(Entity entity, Component component);

    /// <summary>
    /// Internal notification that a component was removed.
    /// </summary>
    internal void NotifyComponentRemoved(Entity entity, Component component);

    /// <summary>
    /// Internal notification that an entity was destroyed.
    /// </summary>
    internal void NotifyEntityDestroyed(Entity entity);
}