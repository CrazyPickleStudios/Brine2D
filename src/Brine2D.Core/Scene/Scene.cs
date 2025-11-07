using Brine2D.Core.Hosting;
using Brine2D.Core.Timing;

namespace Brine2D.Core.Scene;

/// <summary>
///     Manages a collection of <see cref="Entity" /> instances and orchestrates their lifecycle
///     (initialize, update, draw, unload) within a game scene.
/// </summary>
/// <remarks>
///     <para>Lifecycle:</para>
///     <list type="bullet">
///         <item><description>Entities can be created at any time via <see cref="CreateEntity(string)" />.</description></item>
///         <item><description>Before <see cref="Initialize(IEngineContext)" />, created entities are collected but not initialized.</description></item>
///         <item><description>During <see cref="Initialize(IEngineContext)" />: all existing entities are initialized, then <see cref="OnInitialize" /> is invoked.</description></item>
///         <item><description>Entities created after initialization (including inside <see cref="OnInitialize" />) are initialized immediately.</description></item>
///         <item><description><see cref="Update(Brine2D.Core.Timing.GameTime)" /> and <see cref="Draw(Brine2D.Core.Timing.GameTime)" /> are forwarded to entities in insertion order.</description></item>
///         <item><description><see cref="Unload" /> calls <see cref="OnUnload" />, clears entities, and resets state.</description></item>
///     </list>
///     <para>Threading: use from the engine's main thread.</para>
/// </remarks>
/// <example>
///     <code>
///     var scene = new Scene();
///
///     // Create before initialize (will init during Initialize)
///     var e1 = scene.CreateEntity("Player");
///
///     // Inject engine and initialize entities
///     scene.Initialize(engine);
///
///     // Create after initialize (immediate init)
///     var e2 = scene.CreateEntity("HUD");
///
///     // Per-frame (typically driven by a scene manager)
///     scene.Update(gameTime);
///     scene.Draw(gameTime);
///
///     // Tear down
///     scene.Unload();
///     </code>
/// </example>
public class Scene
{
    /// <summary>
    ///     Backing store for all entities attached to this scene.
    ///     Preserves insertion order for deterministic update/draw sequencing.
    /// </summary>
    private readonly List<Entity> _entities = new();

    /// <summary>
    ///     Gets the engine context associated with this scene.
    ///     Non-null only after <see cref="Initialize(IEngineContext)" />.
    /// </summary>
    public IEngineContext Engine { get; private set; } = null!;

    /// <summary>
    ///     Gets a value indicating whether the scene has completed initialization.
    ///     Becomes true after <see cref="Initialize(IEngineContext)" /> and false after <see cref="Unload" />.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///     Creates a new <see cref="Entity" />, attaches it to this scene, and initializes it immediately
    ///     if the scene is already initialized.
    /// </summary>
    /// <param name="name">Optional display name for identification/debugging. Defaults to "Entity".</param>
    /// <returns>The created <see cref="Entity" />.</returns>
    /// <remarks>
    ///     If <see cref="IsInitialized" /> is true, this method invokes the entity's
    ///     <see cref="Entity.InitializeAll(IEngineContext)" /> immediately.
    /// </remarks>
    public Entity CreateEntity(string name = "Entity")
    {
        var e = new Entity(name) { Scene = this };
        _entities.Add(e);
        if (IsInitialized)
        {
            e.InitializeAll(Engine);
        }

        return e;
    }

    /// <summary>
    ///     Removes the specified <see cref="Entity" /> from this scene.
    /// </summary>
    /// <param name="e">The entity to remove.</param>
    /// <returns>
    ///     True if the entity was present and removed; otherwise, false.
    ///     Note: This does not invoke any disposal on the entity or its components.
    /// </returns>
    public bool DestroyEntity(Entity e)
    {
        return _entities.Remove(e);
    }

    /// <summary>
    ///     Called once after the scene and all pre-existing entities have been initialized.
    ///     Override to perform scene-specific setup that depends on initialized entities/services.
    /// </summary>
    public virtual void OnInitialize()
    {
    }

    /// <summary>
    ///     Called during <see cref="Unload" /> before entities are cleared.
    ///     Override to release scene-level resources or detach from external systems.
    /// </summary>
    public virtual void OnUnload()
    {
    }

    /// <summary>
    ///     Forwards per-frame draw to all entities in insertion order.
    /// </summary>
    /// <param name="time">Frame timing information.</param>
    internal void Draw(GameTime time)
    {
        for (var i = 0; i < _entities.Count; i++)
        {
            _entities[i].DrawAll(time);
        }
    }

    /// <summary>
    ///     Initializes the scene with the provided engine context.
    ///     Initializes all currently attached entities and then invokes <see cref="OnInitialize" />.
    /// </summary>
    /// <param name="engine">Engine context providing shared services.</param>
    /// <remarks>
    ///     Order:
    ///     1) Store <paramref name="engine" />, set <see cref="IsInitialized" /> = true.
    ///     2) Initialize all existing entities in insertion order.
    ///     3) Invoke <see cref="OnInitialize" />.
    /// </remarks>
    internal void Initialize(IEngineContext engine)
    {
        Engine = engine;
        IsInitialized = true;
        foreach (var e in _entities)
        {
            e.InitializeAll(engine);
        }

        OnInitialize();
    }

    /// <summary>
    ///     Unloads the scene by invoking <see cref="OnUnload" />, clearing all entities,
    ///     and resetting <see cref="IsInitialized" /> to false.
    /// </summary>
    internal void Unload()
    {
        OnUnload();
        _entities.Clear();
        IsInitialized = false;
    }

    /// <summary>
    ///     Forwards per-frame update to all entities in insertion order.
    /// </summary>
    /// <param name="time">Frame timing information.</param>
    internal void Update(GameTime time)
    {
        for (var i = 0; i < _entities.Count; i++)
        {
            _entities[i].UpdateAll(time);
        }
    }
}