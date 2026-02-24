using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Base class for render systems with default implementations.
/// </summary>
public abstract class RenderSystemBase : IRenderSystem
{
    /// <summary>
    /// Whether this system is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Execution order for this system. Override to customize.
    /// Default is <see cref="SystemRenderOrder.Sprites"/> (0).
    /// </summary>
    public virtual int RenderOrder => SystemRenderOrder.Sprites;
    
    /// <summary>
    /// Called every frame to render this system.
    /// </summary>
    public abstract void Render(IEntityWorld world, IRenderer renderer);
}