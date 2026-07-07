using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.ECS;

/// <summary>
/// Base class for behaviors — logic that operates on a single entity.
/// Each entity gets its own behavior instance with constructor injection support.
/// </summary>
/// <remarks>
/// <para>
/// Use behaviors for entity-specific logic (player movement, boss AI, etc.).
/// For batch processing of many entities, use systems instead.
/// </para>
/// <para>
/// <b>Execution order:</b> During <see cref="EntityWorld.Update"/>, all
/// <see cref="IUpdateSystem"/>s run first (sorted by <c>UpdateOrder</c>), then all
/// behaviors (sorted by <see cref="UpdateOrder"/>). During <see cref="EntityWorld.FixedUpdate"/>, 
/// all <see cref="IFixedUpdateSystem"/>s run first (sorted by <c>FixedUpdateOrder</c>), then all
/// behaviors (sorted by <see cref="FixedUpdateOrder"/>). During <see cref="EntityWorld.Render"/>, 
/// all <see cref="IRenderSystem"/>s run first (sorted by <c>RenderOrder</c>), then all
/// behaviors (sorted by <see cref="RenderOrder"/>). Behaviors therefore always execute
/// after system-rendered content. If you need to render between systems, move the render
/// logic into a custom <see cref="IRenderSystem"/> with the appropriate <c>RenderOrder</c>.
/// </para>
/// <code>
/// public class PlayerMovementBehavior : Behavior
/// {
///     private readonly IInputContext _input;
///     private readonly IAudioService _audio;
///
///     public PlayerMovementBehavior(IInputContext input, IAudioService audio)
///     {
///         _input = input;
///         _audio = audio;
///     }
///
///     public override int UpdateOrder => 10;
///
///     public override void Update(GameTime gameTime)
///     {
///         if (_input.IsKeyDown(Key.W))
///             _audio.PlaySound(_footstep);
///     }
/// }
/// </code>
/// </remarks>
public abstract class Behavior
{
    /// <summary>
    /// The entity this behavior is attached to.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> after the behavior has been detached via
    /// <see cref="Entity.RemoveBehavior{T}"/> or when the owning entity is destroyed.
    /// Check this property before accessing entity state from outside the normal
    /// Update/FixedUpdate/Render lifecycle (e.g., from event callbacks or cached references).
    /// </remarks>
    public Entity? Entity { get; internal set; }

    private bool _isEnabled = true;

    /// <summary>Whether this behavior is enabled (affects Update, FixedUpdate, and Render calls).</summary>
    /// <remarks>
    /// When changed, <see cref="OnEnabled"/> or <see cref="OnDisabled"/> is called
    /// so that subclasses can react without overriding the property.
    /// </remarks>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            Entity?.NotifyEnabledChanged();
            if (_isEnabled)
                OnEnabled();
            else
                OnDisabled();
        }
    }

    /// <summary>
    /// Controls execution order relative to other behaviors during Update.
    /// Lower values run first. Default is 0. Behaviors with the same order
    /// run in the order they were added.
    /// </summary>
    /// <remarks>
    /// This property must return a constant value. <see cref="EntityWorld"/> sorts behaviors
    /// once when the order-dirty flag is set; a value that changes at runtime will not trigger
    /// a re-sort.
    /// </remarks>
    public virtual int UpdateOrder => 0;

    /// <summary>
    /// Controls execution order relative to other behaviors during FixedUpdate.
    /// Lower values run first. Default is 0. Behaviors with the same order
    /// run in the order they were added.
    /// </summary>
    /// <remarks>
    /// This property must return a constant value. <see cref="EntityWorld"/> sorts behaviors
    /// once when the order-dirty flag is set; a value that changes at runtime will not trigger
    /// a re-sort.
    /// </remarks>
    public virtual int FixedUpdateOrder => 0;

    /// <summary>
    /// Controls execution order relative to other behaviors during Render.
    /// Lower values run first. Default is 0. Behaviors with the same order
    /// run in the order they were added.
    /// </summary>
    /// <remarks>
    /// This property must return a constant value. <see cref="EntityWorld"/> sorts behaviors
    /// once when the order-dirty flag is set; a value that changes at runtime will not trigger
    /// a re-sort.
    /// </remarks>
    public virtual int RenderOrder => 0;

    /// <summary>
    /// Called when this behavior transitions from disabled to enabled
    /// (i.e., <see cref="IsEnabled"/> changes to <see langword="true"/>).
    /// Also called when the owning entity transitions from inactive to active
    /// (<see cref="Entity.IsActive"/> changes to <see langword="true"/>) and this
    /// behavior's <see cref="IsEnabled"/> is already <see langword="true"/>.
    /// Override to resume state or restart effects.
    /// </summary>
    protected internal virtual void OnEnabled() { }

    /// <summary>
    /// Called when this behavior transitions from enabled to disabled
    /// (i.e., <see cref="IsEnabled"/> changes to <see langword="false"/>).
    /// Also called when the owning entity transitions from active to inactive
    /// (<see cref="Entity.IsActive"/> changes to <see langword="false"/>) and this
    /// behavior's <see cref="IsEnabled"/> is already <see langword="true"/>.
    /// Override to pause state, stop effects, or clear accumulators.
    /// </summary>
    protected internal virtual void OnDisabled() { }

    /// <summary>
    /// Called once before the first tick of this behavior's lifecycle — guaranteed to run before
    /// the first <see cref="Update"/>, <see cref="FixedUpdate"/>, or <see cref="Render"/> call,
    /// whichever occurs first. Use this to perform initialization that depends on all entities and
    /// components being present — e.g., looking up sibling entities or caching cross-entity references.
    /// </summary>
    /// <remarks>
    /// <c>OnStart</c> fires in whichever pipeline dispatches this behavior first. On most frames the
    /// game loop runs <see cref="FixedUpdate"/> before <see cref="Update"/>, so <c>OnStart</c> will
    /// typically fire during the first fixed-update tick, not the first variable-update tick.
    /// If the behavior runs only in <see cref="Render"/>, <c>OnStart</c> fires before the first
    /// render call. The guarantee is simply: <c>OnStart</c> runs exactly once, before any other
    /// lifecycle method on this behavior instance.
    /// Unlike <see cref="OnAdded"/>, which fires immediately when the behavior is
    /// added, <c>OnStart</c> is deferred until the first frame the behavior actually
    /// ticks, so the entire entity hierarchy is guaranteed to be initialized.
    /// </remarks>
    public virtual void OnStart() { }

    /// <summary>Tracks whether <see cref="OnStart"/> has been called for this instance.</summary>
    internal bool _started;

    /// <summary>
    /// Tracks whether <see cref="OnStart"/> threw an exception.
    /// When <see langword="true"/>, all future Update/FixedUpdate/Render ticks are skipped
    /// so uninitialized state is never processed.
    /// </summary>
    internal bool _startFailed;

    /// <summary>
    /// Gets whether <see cref="OnStart"/> threw an exception on its last attempt.
    /// When <see langword="true"/>, the behavior is silently skipped each tick.
    /// Call <see cref="ResetStart"/> to allow <see cref="OnStart"/> to run again.
    /// </summary>
    public bool StartFailed => _startFailed;

    /// <summary>
    /// Clears the start-failed state so that <see cref="OnStart"/> will be retried
    /// on the next tick. Use this to recover from a transient initialization failure
    /// (e.g., a required service that was not ready on the first frame).
    /// </summary>
    /// <remarks>
    /// <see cref="OnStart"/> will run again exactly once on the next tick. If it throws
    /// again, <see cref="StartFailed"/> will be set to <see langword="true"/> again.
    /// </remarks>
    public void ResetStart()
    {
        _startFailed = false;
        _started = false;
    }

    /// <summary>
    /// Called once when the behavior is added to an entity.
    /// Use this to validate required components and cache references.
    /// </summary>
    /// <remarks>
    /// <see cref="Entity.OnInitialize"/> may not have been called yet when this method runs.
    /// <see cref="AddBehavior{T}()"/> executes immediately, while <see cref="Entity.OnInitialize"/>
    /// is deferred until the next <see cref="EntityWorld.Flush"/> /
    /// <see cref="EntityWorld.Update"/> call. Do not rely on state that <c>OnInitialize</c>
    /// sets up from inside this method; use <see cref="Entity.GetComponent{T}"/> and
    /// component data instead, which are available immediately after being added.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnAdded()
    /// {
    ///     _transform = Entity.GetRequiredComponent&lt;TransformComponent&gt;();
    ///     _velocity  = Entity.GetComponent&lt;VelocityComponent&gt;();
    /// }
    /// </code>
    /// </example>
    protected internal virtual void OnAdded() { }

    /// <summary>
    /// Called when a component is added to the same entity as this behavior.
    /// Fires after the component's own <see cref="Component.OnAdded"/> has run.
    /// </summary>
    /// <param name="component">The component that was added.</param>
    /// <remarks>
    /// Use this to react to dynamic component changes — for example, caching a newly-added
    /// weapon component or enabling a visual effect when a status effect is applied.
    /// <c>Entity</c> is guaranteed to be non-null inside this callback.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected internal override void OnComponentAdded(Component component)
    /// {
    ///     if (component is WeaponComponent weapon)
    ///         _currentWeapon = weapon;
    /// }
    /// </code>
    /// </example>
    protected internal virtual void OnComponentAdded(Component component) { }

    /// <summary>
    /// Called when a component is removed from the same entity as this behavior.
    /// Fires before the component's <see cref="Component.Entity"/> reference is cleared,
    /// so the component is still accessible via <see cref="Entity.GetComponent{T}"/> during this callback.
    /// </summary>
    /// <param name="component">The component that was removed.</param>
    /// <remarks>
    /// Use this to react to dynamic component changes — for example, releasing a cached
    /// weapon reference or disabling dependent logic when a required component is removed.
    /// <c>Entity</c> is guaranteed to be non-null inside this callback.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected internal override void OnComponentRemoved(Component component)
    /// {
    ///     if (component is WeaponComponent)
    ///         _currentWeapon = null;
    /// }
    /// </code>
    /// </example>
    protected internal virtual void OnComponentRemoved(Component component) { }

    /// <summary>Called every frame if enabled.</summary>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>
    /// Called at a fixed timestep if enabled. Use this for deterministic simulation
    /// logic (physics responses, AI ticks, etc.).
    /// </summary>
    /// <param name="fixedTime">
    /// Game time with a constant <see cref="GameTime.ElapsedTime"/> equal to the configured
    /// fixed timestep.
    /// </param>
    public virtual void FixedUpdate(GameTime fixedTime) { }

    /// <summary>
    /// Called every frame during the render pass if enabled.
    /// Runs <b>after</b> all <see cref="IRenderSystem"/>s, so behavior-rendered
    /// content always draws on top of system-rendered content.
    /// </summary>
    /// <param name="renderer">The renderer to draw with.</param>
    /// <param name="gameTime">
    /// Render-phase game time. <see cref="GameTime.Alpha"/> holds the physics interpolation
    /// factor (0–1) representing how far the current frame sits between the last two fixed
    /// timesteps. Use it to lerp rendered positions between previous and current physics state.
    /// </param>
    public virtual void Render(IRenderer renderer, GameTime gameTime) { }

    /// <summary>
    /// Called when the owning entity is destroyed (via <see cref="Entity.Destroy"/> or
    /// <see cref="IEntityWorld.DestroyEntity"/>). Fires before <see cref="OnRemoved"/> and
    /// while <see cref="Entity"/> is still set, so sibling components and behaviors are still
    /// accessible. Use this to distinguish entity destruction from a hot-swap removal triggered
    /// by <see cref="Entity.RemoveBehavior{T}"/>, which calls only <see cref="OnRemoved"/>.
    /// </summary>
    protected internal virtual void OnDestroyed() { }

    /// <summary>Called when the behavior is removed from an entity.</summary>
    protected internal virtual void OnRemoved() { }
}