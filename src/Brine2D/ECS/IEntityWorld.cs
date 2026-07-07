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
    /// Gets the number of entities currently active in the world.
    /// Prefer this over <c>Entities.Count</c> when you only need the count,
    /// as it avoids accessing the list wrapper.
    /// </summary>
    int EntityCount { get; }

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
    /// Adds a pre-constructed system instance to this world, bypassing dependency injection.
    /// Use this when the system requires constructor arguments that are not registered in DI,
    /// or when the system was obtained from an object pool or factory.
    /// </summary>
    /// <typeparam name="T">The system type. Must implement at least one pipeline interface.</typeparam>
    /// <param name="instance">The pre-constructed system instance to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a system of type <typeparamref name="T"/> is already registered,
    /// or when <paramref name="instance"/> does not implement any pipeline interface.
    /// </exception>
    void AddSystem<T>(T instance) where T : class, ISystem;

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
    /// Gets a system of the specified type, throwing if it is not found.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no system of type <typeparamref name="T"/> exists in this world.
    /// </exception>
    T GetRequiredSystem<T>() where T : class;

    /// <summary>
    /// Checks if a system of the specified type exists in this world (checks all pipelines).
    /// </summary>
    bool HasSystem<T>() where T : class;

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
    /// Fired after an entity is fully initialized and committed to the world
    /// (i.e., after <see cref="Entity.OnInitialize"/> has run).
    /// </summary>
    event Action<Entity>? EntityCreated;

    /// <summary>
    /// Fired just before an entity is removed from the world and its
    /// <see cref="Entity.World"/> reference is cleared.
    /// </summary>
    event Action<Entity>? EntityDestroyed;

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
    /// Destroys all entities but keeps systems and their configuration intact.
    /// Useful for scene resets without reloading.
    /// </summary>
    void ClearEntities();

    /// <summary>
    /// Gets an entity by its unique ID.
    /// </summary>
    Entity? GetEntityById(long id);

    /// <summary>
    /// Gets an entity by name (returns first match).
    /// </summary>
    /// <remarks>
    /// Only active entities are returned. To include inactive entities use the
    /// <c>includeInactive</c> overload.
    /// This is O(n); prefer ID-based lookup via <see cref="GetEntityById"/> on hot paths.
    /// Entities created in the same frame but not yet committed by <see cref="Flush"/>
    /// are also searched, so this method returns a newly-created entity immediately
    /// without requiring a <see cref="Flush"/> call first.
    /// </remarks>
    Entity? GetEntityByName(string name);

    /// <summary>
    /// Gets an entity by name (returns first match), optionally including inactive entities.
    /// </summary>
    /// <param name="name">The name to search for.</param>
    /// <param name="includeInactive">
    /// When <see langword="true"/>, inactive entities are also searched.
    /// Defaults to <see langword="false"/> to match the no-arg overload.
    /// </param>
    /// <remarks>
    /// This is O(n); prefer ID-based lookup via <see cref="GetEntityById"/> on hot paths.
    /// Entities created in the same frame but not yet committed by <see cref="Flush"/>
    /// are also searched, so this method returns a newly-created entity immediately
    /// without requiring a <see cref="Flush"/> call first.
    /// </remarks>
    Entity? GetEntityByName(string name, bool includeInactive);

    /// <summary>
    /// Gets all active entities with a specific tag.
    /// </summary>
    /// <remarks>
    /// Only active entities are returned. To include inactive entities,
    /// use the <c>includeInactive</c> overload.
    /// Returns a materialized list built from the internal tag index in O(matching entities).
    /// For per-frame use, prefer <see cref="ForEachWithTag"/> or a cached query to avoid
    /// allocating a new list each call.
    /// </remarks>
    IEnumerable<Entity> GetEntitiesByTag(string tag);

    /// <summary>
    /// Gets entities with a specific tag, optionally including inactive entities.
    /// </summary>
    /// <param name="tag">The tag to search for.</param>
    /// <param name="includeInactive">
    /// When <see langword="true"/>, inactive entities are also returned.
    /// Defaults to <see langword="false"/> to match the no-arg overload.
    /// </param>
    /// <remarks>
    /// Returns a materialized list built from the internal tag index in O(matching entities).
    /// For per-frame use, prefer <see cref="ForEachWithTag"/> or a cached query to avoid
    /// allocating a new list each call.
    /// </remarks>
    IEnumerable<Entity> GetEntitiesByTag(string tag, bool includeInactive);

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
    /// component type. Uses an <see cref="System.Buffers.ArrayPool{T}"/> snapshot internally
    /// so structural changes made inside the callback are safe.
    /// </summary>
    /// <remarks>
    /// Prefer this over <see cref="GetEntitiesWithComponent{T}"/> in per-frame loops to avoid
    /// allocating a new list each call. For systems that query the same set every frame,
    /// <see cref="CreateCachedQuery{T1}"/> is more efficient still.
    /// </remarks>
    void ForEachWithComponent<T>(Action<Entity> action) where T : Component;

    /// <summary>
    /// Invokes <paramref name="action"/> for every active entity that has the specified
    /// component type, passing both the entity and the resolved component.
    /// Uses an <see cref="System.Buffers.ArrayPool{T}"/> snapshot internally
    /// so structural changes made inside the callback are safe.
    /// </summary>
    /// <remarks>
    /// Prefer this over <see cref="GetEntitiesWithComponent{T}"/> in per-frame loops to avoid
    /// allocating a new list each call. For systems that query the same set every frame,
    /// <see cref="CreateCachedQuery{T1}"/> is more efficient still.
    /// </remarks>
    void ForEachWithComponent<T>(Action<Entity, T> action) where T : Component;

    /// <summary>
    /// Invokes <paramref name="action"/> for every active entity that has both
    /// <typeparamref name="T1"/> and <typeparamref name="T2"/>, passing the entity
    /// and both resolved components. Iterates the smaller pool internally for efficiency.
    /// Uses an <see cref="System.Buffers.ArrayPool{T}"/> snapshot so structural changes
    /// made inside the callback are safe.
    /// </summary>
    /// <remarks>
    /// Prefer this over <see cref="GetEntitiesWithComponents{T1, T2}"/> in per-frame loops
    /// to avoid allocating a new list each call. For systems that query the same set every
    /// frame, <see cref="CreateCachedQuery{T1, T2}"/> is more efficient still.
    /// </remarks>
    void ForEachWithComponents<T1, T2>(Action<Entity, T1, T2> action)
        where T1 : Component
        where T2 : Component;

    /// <summary>
    /// Invokes <paramref name="action"/> for every active entity that has all three component types,
    /// passing the entity and all three resolved components. Iterates the smallest pool internally
    /// for efficiency. Uses an <see cref="System.Buffers.ArrayPool{T}"/> snapshot so structural
    /// changes made inside the callback are safe.
    /// </summary>
    /// <remarks>
    /// Prefer this over <see cref="GetEntitiesWithComponents{T1, T2, T3}"/> in per-frame loops
    /// to avoid allocating a new list each call. For systems that query the same set every
    /// frame, <see cref="CreateCachedQuery{T1, T2, T3}"/> is more efficient still.
    /// </remarks>
    void ForEachWithComponents<T1, T2, T3>(Action<Entity, T1, T2, T3> action)
        where T1 : Component
        where T2 : Component
        where T3 : Component;

    /// <summary>
    /// Invokes <paramref name="action"/> for every active entity that has all four component types,
    /// passing the entity and all four resolved components. Iterates the smallest pool internally
    /// for efficiency. Uses an <see cref="System.Buffers.ArrayPool{T}"/> snapshot so structural
    /// changes made inside the callback are safe.
    /// </summary>
    /// <remarks>
    /// Prefer this over <see cref="GetEntitiesWithComponents{T1, T2, T3, T4}"/> in per-frame loops
    /// to avoid allocating a new list each call. For systems that query the same set every
    /// frame, <see cref="CreateCachedQuery{T1, T2, T3, T4}"/> is more efficient still.
    /// </remarks>
    void ForEachWithComponents<T1, T2, T3, T4>(Action<Entity, T1, T2, T3, T4> action)
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component;

    /// <summary>
    /// Invokes <paramref name="action"/> for every active entity that has all five component types,
    /// passing the entity and all five resolved components. Iterates the smallest pool internally
    /// for efficiency. Uses an <see cref="System.Buffers.ArrayPool{T}"/> snapshot so structural
    /// changes made inside the callback are safe.
    /// </summary>
    /// <remarks>
    /// Prefer this over <see cref="GetEntitiesWithComponents{T1, T2, T3, T4, T5}"/> in per-frame loops
    /// to avoid allocating a new list each call. For systems that query the same set every
    /// frame, <see cref="CreateCachedQuery{T1, T2, T3, T4, T5}"/> is more efficient still.
    /// </remarks>
    void ForEachWithComponents<T1, T2, T3, T4, T5>(Action<Entity, T1, T2, T3, T4, T5> action)
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        where T5 : Component;

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
    /// Invokes <paramref name="action"/> for every active entity that has a behavior of type
    /// <typeparamref name="T"/>, passing both the entity and the resolved behavior instance.
    /// Uses an <see cref="System.Buffers.ArrayPool{T}"/> snapshot so structural changes made
    /// inside the callback are safe.
    /// </summary>
    /// <remarks>
    /// Iterates the internal behavior list in O(total behaviors). For systems that query the
    /// same set every frame, consider a cached query with <c>.WithBehavior&lt;T&gt;()</c>.
    /// </remarks>
    void ForEachWithBehavior<T>(Action<Entity, T> action) where T : Behavior;

    /// <summary>
    /// Invokes <paramref name="action"/> for every active entity that has a behavior of type
    /// <typeparamref name="T"/>. Uses an <see cref="System.Buffers.ArrayPool{T}"/> snapshot
    /// so structural changes made inside the callback are safe.
    /// </summary>
    /// <remarks>
    /// Iterates the internal behavior list in O(total behaviors). Prefer
    /// <see cref="ForEachWithBehavior{T}(Action{Entity, T})"/> when you also need the behavior
    /// instance, to avoid a second lookup.
    /// </remarks>
    void ForEachWithBehavior<T>(Action<Entity> action) where T : Behavior;

    /// <summary>
    /// Gets all active entities that have a behavior of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Returns a materialized list. Iterates the internal behavior list in O(total behaviors).
    /// For per-frame use, prefer <see cref="ForEachWithBehavior{T}(Action{Entity, T})"/> to avoid
    /// allocating a new list each call, or a cached query with <c>.WithBehavior&lt;T&gt;()</c>.
    /// </remarks>
    IEnumerable<Entity> GetEntitiesWithBehavior<T>() where T : Behavior;

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
    /// Gets all entities that have all three specified component types.
    /// Iterates the smallest pool to minimise cross-resolves.
    /// </summary>
    /// <remarks>
    /// Returns a materialized list. For per-frame use in systems,
    /// prefer <see cref="CreateCachedQuery{T1, T2, T3}"/>
    /// </remarks>
    IEnumerable<Entity> GetEntitiesWithComponents<T1, T2, T3>()
        where T1 : Component
        where T2 : Component
        where T3 : Component;

    /// <summary>
    /// Gets all entities that have all four specified component types.
    /// Iterates the smallest pool to minimise cross-resolves.
    /// </summary>
    /// <remarks>
    /// Returns a materialized list. For per-frame use in systems,
    /// prefer <see cref="CreateCachedQuery{T1, T2, T3, T4}"/>
    /// </remarks>
    IEnumerable<Entity> GetEntitiesWithComponents<T1, T2, T3, T4>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component;

    /// <summary>
    /// Gets all entities that have all five specified component types.
    /// Iterates the smallest pool to minimise cross-resolves.
    /// </summary>
    /// <remarks>
    /// Returns a materialized list. For per-frame use in systems,
    /// prefer <see cref="CreateCachedQuery{T1, T2, T3, T4, T5}"/>
    /// </remarks>
    IEnumerable<Entity> GetEntitiesWithComponents<T1, T2, T3, T4, T5>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        where T5 : Component;

    /// <summary>
    /// Finds the first entity that matches the specified predicate.
    /// Only searches active entities.
    /// </summary>
    Entity? FindEntity(Func<Entity, bool> predicate);

    /// <summary>
    /// Finds the first entity that matches the specified predicate, optionally including inactive entities.
    /// </summary>
    /// <param name="predicate">The filter to apply to each entity.</param>
    /// <param name="includeInactive">
    /// When <see langword="true"/>, inactive entities are also searched.
    /// Defaults to <see langword="false"/> to match the no-arg overload.
    /// </param>
    Entity? FindEntity(Func<Entity, bool> predicate, bool includeInactive);

    #endregion

    #region Update and Render

    /// <summary>
    /// Updates all systems and entities in the world.
    /// </summary>
    void Update(GameTime gameTime);

    /// <summary>
    /// Runs one fixed timestep for all fixed update systems and behaviors.
    /// Called by the game loop's accumulator; not intended for direct use.
    /// </summary>
    void FixedUpdate(GameTime fixedTime);

    /// <summary>
    /// Renders all systems and entities in the world.
    /// </summary>
    void Render(IRenderer renderer, GameTime gameTime);

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
    /// <remarks>
    /// The <see cref="Query.CachedEntityQuery{T1}"/> returned by <c>.Build()</c> registers itself with
    /// this world's invalidation index. Call <c>Dispose()</c> on it (typically inside
    /// <c>protected override void Dispose(bool disposing)</c> of the owning system) when the system is
    /// removed mid-scene. Failing to dispose leaves the query in the index until the world is disposed,
    /// preventing GC of the query and its cached result set.
    /// </remarks>
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

    CachedEntityQueryBuilder<T1, T2, T3, T4, T5> CreateCachedQuery<T1, T2, T3, T4, T5>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        where T5 : Component;

    #endregion

    #region Utilities

    /// <summary>
    /// Forces immediate processing of all deferred operations.
    /// </summary>
    void Flush();

    #endregion
}