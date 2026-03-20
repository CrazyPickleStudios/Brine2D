using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.ECS;

/// <summary>
/// Base class for entity behaviors — logic that operates on a single entity.
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
/// public class PlayerMovementBehavior : EntityBehavior
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
public abstract class EntityBehavior
{
    /// <summary>The entity this behavior is attached to.</summary>
    public Entity Entity { get; internal set; } = null!;

    /// <summary>Whether this behavior is enabled (affects Update, FixedUpdate, and Render calls).</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Controls execution order relative to other behaviors during Update.
    /// Lower values run first. Default is 0. Behaviors with the same order
    /// run in the order they were added.
    /// </summary>
    public virtual int UpdateOrder => 0;

    /// <summary>
    /// Controls execution order relative to other behaviors during FixedUpdate.
    /// Lower values run first. Default is 0. Behaviors with the same order
    /// run in the order they were added.
    /// </summary>
    public virtual int FixedUpdateOrder => 0;

    /// <summary>
    /// Controls execution order relative to other behaviors during Render.
    /// Lower values run first. Default is 0. Behaviors with the same order
    /// run in the order they were added.
    /// </summary>
    public virtual int RenderOrder => 0;

    /// <summary>
    /// Called once when the behavior is attached to an entity.
    /// Use this to validate required components and cache references.
    /// </summary>
    /// <example>
    /// <code>
    /// protected override void OnAttached()
    /// {
    ///     _transform = Entity.GetRequiredComponent&lt;TransformComponent&gt;();
    ///     _velocity  = Entity.GetComponent&lt;VelocityComponent&gt;();
    /// }
    /// </code>
    /// </example>
    protected internal virtual void OnAttached() { }

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
    public virtual void Render(IRenderer renderer) { }

    /// <summary>Called when the behavior is removed from an entity.</summary>
    protected internal virtual void OnDetached() { }
}