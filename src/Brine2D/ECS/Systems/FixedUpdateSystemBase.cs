using Brine2D.Core;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Base class for fixed update systems with default implementations.
/// </summary>
public abstract class FixedUpdateSystemBase : IFixedUpdateSystem
{
    /// <summary>
    /// Whether this system is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Execution order for this system. Override to customize.
    /// Default is <see cref="SystemFixedUpdateOrder.Physics"/> (0).
    /// </summary>
    public virtual int FixedUpdateOrder => SystemFixedUpdateOrder.Physics;

    /// <summary>
    /// Called at a fixed timestep to update this system.
    /// </summary>
    public abstract void FixedUpdate(IEntityWorld world, GameTime fixedTime);
}