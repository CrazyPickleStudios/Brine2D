using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.ECS;

/// <summary>
/// Base class for entity behaviors - logic that operates on a single entity.
/// Each entity gets its own behavior instance with automatic dependency injection.
/// </summary>
/// <remarks>
/// Behaviors are created per-entity and can have dependencies injected via constructor.
/// Use behaviors for entity-specific logic (player movement, boss AI, etc.).
/// For batch processing of many entities, use Systems instead.
/// 
/// <para><strong>Dependency Injection Example:</strong></para>
/// <code>
/// public class PlayerMovementBehavior : EntityBehavior
/// {
///     private readonly IInputContext _input;
///     private readonly AudioService _audio;
///     
///     // ✅ Constructor injection - framework handles this automatically
///     public PlayerMovementBehavior(IInputContext input, AudioService audio)
///     {
///         _input = input;
///         _audio = audio;
///     }
///     
///     public override void Update(GameTime gt)
///     {
///         if (_input.IsKeyDown(Key.W))
///         {
///             // Move player
///             _audio.PlaySound("footstep.wav");
///         }
///     }
/// }
/// </code>
/// </remarks>
public abstract class EntityBehavior
{
    /// <summary>
    /// The entity this behavior is attached to.
    /// </summary>
    public Entity Entity { get; internal set; } = null!;
    
    /// <summary>
    /// Whether this behavior is enabled (affects Update/Render calls).
    /// </summary>
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
    ///     _velocity = Entity.GetComponent&lt;VelocityComponent&gt;();
    /// }
    /// </code>
    /// </example>
    protected internal virtual void OnAttached() { }

    /// <summary>
    /// Called every frame if enabled.
    /// </summary>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>
    /// Called every frame during render pass if enabled.
    /// </summary>
    public virtual void Render(IRenderer renderer) { }

    /// <summary>
    /// Called when the behavior is removed from an entity.
    /// </summary>
    protected internal virtual void OnDetached() { }
}