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
    /// <remarks>
    /// Returns a lazy enumerable for efficient iteration without allocation.
    /// Call .ToList() if you need to store results or iterate multiple times.
    /// </remarks>
    IEnumerable<Entity> GetEntitiesByTag(string tag);

    /// <summary>
    /// Gets all entities with a specific component type.
    /// </summary>
    /// <remarks>
    /// Returns a lazy enumerable for efficient iteration without allocation.
    /// Call .ToList() if you need to store results or iterate multiple times.
    /// </remarks>
    IEnumerable<Entity> GetEntitiesWithComponent<T>() where T : Component; 

    /// <summary>
    /// Gets all entities that have both specified component types.
    /// </summary>
    /// <remarks>
    /// Returns a lazy enumerable for efficient iteration without allocation.
    /// Call .ToList() if you need to store results or iterate multiple times.
    /// </remarks>
    IEnumerable<Entity> GetEntitiesWithComponents<T1, T2>() 
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
    /// Creates a cached query builder for better performance when querying repeatedly.
    /// </summary>
    CachedEntityQueryBuilder<T1> CreateCachedQuery<T1>() where T1 : Component;

    CachedEntityQueryBuilder<T1, T2> CreateCachedQuery<T1, T2>()
        where T1 : Component
        where T2 : Component;

    CachedEntityQueryBuilder<T1, T2, T3> CreateCachedQuery<T1, T2, T3>()
        where T1 : Component
        where T2 : Component
        where T3 : Component;

    /// <summary>
    /// Forces immediate processing of all deferred operations.
    /// </summary>
    void Flush();
}