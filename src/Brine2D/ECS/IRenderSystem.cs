using Brine2D.Rendering;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Interface for systems that process entities during the render phase.
/// Render systems run before behaviors during EntityWorld.Render().
/// </summary>
/// <remarks>
/// Systems are scene-scoped and automatically cleaned up when the scene unloads.
/// Use render systems for batch rendering of many entities (sprite batching, particle rendering, etc.).
/// For entity-specific rendering, use Behavior instead.
/// </remarks>
public interface IRenderSystem : ISystem
{
    /// <summary>
    /// Determines the order in which this system executes during the render phase.
    /// Lower values execute first. Default is 0 (<see cref="SystemRenderOrder.Sprites"/>).
    /// </summary>
    /// <remarks>
    /// This property must return a constant value. <see cref="EntityWorld"/> sorts systems
    /// once after registration; a value that changes at runtime will not trigger a re-sort.
    /// Use <see cref="SystemRenderOrder"/> constants for common phases.
    /// </remarks>
    int RenderOrder => SystemRenderOrder.Sprites;

    /// <summary>
    /// Called every frame to render this system.
    /// </summary>
    /// <param name="world">The entity world to process.</param>
    /// <param name="renderer">The renderer to draw with.</param>
    void Render(IEntityWorld world, IRenderer renderer);
}