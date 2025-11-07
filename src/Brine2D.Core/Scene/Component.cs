using Brine2D.Core.Hosting;
using Brine2D.Core.Timing;

namespace Brine2D.Core.Scene;

/// <summary>
///     Base type for behaviors that can be attached to an <see cref="Entity" />.
///     Provides lifecycle hooks (Initialize/Update/Draw) and add/remove notifications.
/// </summary>
/// <remarks>
///     <para>Lifecycle (typical):</para>
///     <list type="number">
///         <item><description>Construct the component (constructors should avoid side effects).</description></item>
///         <item><description>Add to an <see cref="Entity" /> via <c>Entity.Add</c>.</description></item>
///         <item><description>If the parent <see cref="Scene" /> is already initialized, the component is <b>initialized</b> and then <b>OnAdded</b> is invoked immediately.</description></item>
///         <item><description>If added before the scene initializes, the scene will <b>Initialize</b> all components and then invoke <b>OnAdded</b> during its initialization.</description></item>
///         <item><description>Every frame (while enabled by the owning systems): <see cref="Update(GameTime)" /> then <see cref="Draw(GameTime)" />.</description></item>
///         <item><description>When removed from its entity in a live scene, <see cref="OnRemoved" /> is invoked once.</description></item>
///     </list>
///     <para>Threading: callbacks are expected to run on the engine's main thread.</para>
/// </remarks>
/// <example>
///     <code>
///     public sealed class SpinComponent : Component
///     {
///         private float _angle;
///         private IEngineContext _engine = default!;
///
///         public override void Initialize(IEngineContext engine)
///         {
///             _engine = engine;
///         }
///
///         public override void Update(GameTime time)
///         {
///             _angle += (float)time.DeltaSeconds;
///         }
///
///         public override void Draw(GameTime time)
///         {
///             // Submit draw calls via _engine.Renderer / _engine.Sprites if needed.
///         }
///     }
///     </code>
/// </example>
public abstract class Component
{
    /// <summary>
    ///     Owning entity. Set internally when the component is added to an <see cref="Entity" />.
    /// </summary>
    /// <remarks>
    ///     Not valid until the component is added. Accessing before attachment will result in a null reference.
    /// </remarks>
    public Entity Entity { get; internal set; } = null!;

    /// <summary>
    ///     Convenience accessor for the parent <see cref="Scene" /> of <see cref="Entity" />.
    ///     Can be <c>null</c> if the entity is not part of a scene.
    /// </summary>
    public Scene? Scene => Entity.Scene;

    /// <summary>
    ///     Per-frame draw. Submit rendering commands here.
    /// </summary>
    /// <param name="time">Time information for the current frame.</param>
    public virtual void Draw(GameTime time)
    {
    }

    /// <summary>
    ///     Looks up another component of type <typeparamref name="T" /> on the same entity.
    ///     Returns <c>null</c> if not found.
    /// </summary>
    /// <typeparam name="T">Component type to retrieve.</typeparam>
    public T? GetComponent<T>() where T : Component
    {
        return Entity.GetComponent<T>();
    }

    // Lifecycle hooks

    /// <summary>
    ///     One-time initialization called when the parent scene is initialized (or immediately if added to a live scene).
    ///     Use <paramref name="engine" /> to access shared services (content, input, rendering, etc.).
    /// </summary>
    /// <param name="engine">Engine context providing core services.</param>
    public virtual void Initialize(IEngineContext engine)
    {
    }

    /// <summary>
    ///     Called exactly once when the component is added to an entity in a live scene (after <see cref="Initialize(IEngineContext)" />).
    ///     Use this for wiring that depends on other components or scene state.
    /// </summary>
    public virtual void OnAdded()
    {
    }

    /// <summary>
    ///     Called exactly once when the component is removed from its entity in a live scene.
    ///     Use this to detach event handlers or release scene-level references.
    /// </summary>
    public virtual void OnRemoved()
    {
    }

    /// <summary>
    ///     Per-frame update. Perform simulation, input handling, and state changes here.
    /// </summary>
    /// <param name="time">Time information for the current frame.</param>
    public virtual void Update(GameTime time)
    {
    }
}