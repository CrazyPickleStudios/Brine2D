using Brine2D.Core;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Base class for update systems with default implementations.
/// </summary>
public abstract class UpdateSystemBase : IUpdateSystem
{
    /// <summary>
    /// Whether this system is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Execution order for this system. Override to customize.
    /// Default is <see cref="SystemUpdateOrder.Update"/> (0).
    /// </summary>
    public virtual int UpdateOrder => SystemUpdateOrder.Update;
    
    /// <summary>
    /// Called every frame to update this system.
    /// </summary>
    public abstract void Update(IEntityWorld world, GameTime gameTime);
}