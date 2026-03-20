using Brine2D.Core;
using Brine2D.ECS.Query;
using Brine2D.Rendering;
using Brine2D.ECS.Systems;

namespace Brine2D.ECS;

/// <summary>
/// Interface for the entity world that manages all entities and systems.
/// </summary>
public interface IEntityWorld : IDisposable
{
    /// <summary>
    /// Gets all entities in the world.
    /// </summary>
    IReadOnlyList<Entity> Entities { get; }

    /// <summary>
    /// Gets all update systems in this world.
    /// </summary>
    IReadOnlyList<IUpdateSystem> UpdateSystems { get; }

    /// <summary>
    /// Gets all fixed update systems in this world.
    /// </summary>
    IReadOnlyList<IFixedUpdateSystem> FixedUpdateSystems { get; }

    /// <summary>
    /// Gets all render systems in this world.
    /// </summary>
    IReadOnlyList<IRenderSystem> RenderSystems { get; }

    #region System Management

    /// <summary>
    /// Adds a system to this world, automatically creating it with dependency injection.
    /// Systems that implement IUpdateSystem are added to the update pipeline.
    /// Systems that implement IFixedUpdateSystem are added to the fixed update pipeline.
    /// Systems that implement IRenderSystem are added to the render pipeline.
    /// Systems can implement multiple interfaces.
    /// </summary>
    /// <typeparam name="T">The system type to create and add.</typeparam>
    /// <param name="configure">Optional configuration action for the system.</param>
    void AddSystem<T>(Action<T>? configure = null) where T : class, ISystem;

    /// <summary>
    /// Removes a system of the specified type from this world.
    /// If the system implements multiple pipeline interfaces it will be removed from all.
    /// </summary>
    bool RemoveSystem<T>() where T : class, ISystem;

    /// <summary>
    /// Removes a system by instance reference.
    /// If the system implements multiple pipeline interfaces it will be removed from all.
    /// </summary>
    bool RemoveSystem(ISystem system);

    /// <summary>
    /// Gets an update system of the specified type.
    /// </summary>
    T? GetUpdateSystem<T>() where T : class, IUpdateSystem;

    /// <summary>
    /// Gets a fixed update system of the specified type.
    /// </summary>
    T? GetFixedUpdateSystem<T>() where T : class, IFixedUpdateSystem;

    /// <summary>
    /// Gets a render system of the specified type.
    /// </summary>
    T? GetRenderSystem<T>() where T : class, IRenderSystem;

    /// <summary>
    /// Gets a system of the specified type (checks all pipelines).
    /// </summary>
    T? GetSystem<T>() where T : class;

    /// <summary>
    /// Checks if an update system of the specified type exists in this world.
    /// </summary>
    bool HasUpdateSystem<T>() where T : class, IUpdateSystem;

    /// <summary>
    /// Checks if a fixed update system of the specified type exists in this world.
    /// </summary>
    bool HasFixedUpdateSystem<T>() where T : class, IFixedUpdateSystem;

    /// <summary>
    /// Checks if a render system of the specified type exists in this world.
    /// </summary>
    bool HasRenderSystem<T>() where T : class, IRenderSystem;

    #endregion

    #region Entity Management

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
    Entity? GetEntityById(long id);

    /// <summary>
    /// Gets an entity by name (returns first match).
    /// </summary>
    Entity? GetEntityByName(string name);

    /// <summary>
    /// Gets all active entities with a specific tag.
    /// </summary>
    /// <remarks>
    /// Returns a materialized list built from the internal tag index in O(matching entities).
    /// For per-frame use, prefer <see cref="ForEachWithTag"/> or a cached query to avoid
    /// allocating a new list each call.
    /// </remarks>
    IEnumerable<Entity> GetEntitiesByTag(string tag);

    /// <summary>
    /// Gets all entities with a specific component type.
    /// </summary>
    /// <remarks>
    /// Returns a materialized list. For per-frame use in systems, prefer
    /// <see cref="CreateCachedQuery{T1}"/> which rebuilds only when components change.
    /// </remarks>
    IEnumerable<Entity> GetEntitiesWithComponent<T>() where T : Component;

    /// <summary>
    /// Invokes <paramref name="action"/> for every active entity that has the specified
    /// <paramref name="tag"/>, using the internal tag index for O(tagged entities) lookup
    /// and an <see cref="System.Buffers.ArrayPool{T}"/> snapshot for safe iteration when
    /// the action modifies tags.
    /// </summary>
    /// <remarks>
    /// Prefer this over <see cref="GetEntitiesByTag"/> in per-frame loops to avoid
    /// materializing a new list each call.
    /// </remarks>
    void ForEachWithTag(string tag, Action<Entity> action);

    /// <summary>
    /// Gets all entities that have both specified component types.
    /// Iterates the smaller pool to minimise cross-resolves.
    /// </summary>
    /// <remarks>
    /// Returns a materialized list. For per-frame use in systems,
    /// prefer <see cref="CreateCachedQuery{T1, T2}"/>
    /// </remarks>
    IEnumerable<Entity> GetEntitiesWithComponents<T1, T2>()
        where T1 : Component
        where T2 : Component;

    /// <summary>
    /// Finds the first entity that matches the specified predicate.
    /// </summary>
    Entity? FindEntity(Func<Entity, bool> predicate);

    #endregion

    #region Update and Render

    /// <summary>
    /// Updates all systems and entities in the world.
    /// </summary>
    void Update(GameTime gameTime);

    /// <summary>
    /// Runs one fixed timestep for all fixed update systems and entity behaviors.
    /// Called by the game loop's accumulator; not intended for direct use.
    /// </summary>
    void FixedUpdate(GameTime fixedTime);

    /// <summary>
    /// Renders all systems and entities in the world.
    /// </summary>
    void Render(IRenderer renderer);

    /// <summary>
    /// Clears all entities and systems from the world, disposing any systems that
    /// implement <see cref="IDisposable"/>. The world remains usable after this call.
    /// </summary>
    void Clear();

    #endregion

    #region Queries

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

    CachedEntityQueryBuilder<T1, T2, T3, T4> CreateCachedQuery<T1, T2, T3, T4>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component;

    #endregion

    #region Utilities

    /// <summary>
    /// Forces immediate processing of all deferred operations.
    /// </summary>
    void Flush();

    #endregion
}