using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.ECS;

/// <summary>
/// Base class for entity behaviors — logic that operates on a single entity.
/// Each entity gets its own behavior instance with constructor injection support.
/// </summary>
/// <remarks>
/// Use behaviors for entity-specific logic (player movement, boss AI, etc.).
/// For batch processing of many entities, use systems instead.
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

    /// <summary>Whether this behavior is enabled (affects Update and Render calls).</summary>
    public bool IsEnabled { get; set; } = true;

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

    /// <summary>Called every frame during the render pass if enabled.</summary>
    public virtual void Render(IRenderer renderer) { }

    /// <summary>Called when the behavior is removed from an entity.</summary>
    protected internal virtual void OnDetached() { }
}