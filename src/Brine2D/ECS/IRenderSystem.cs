using Brine2D.Rendering;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Interface for systems that process entities during the render phase.
/// Render systems run before behaviors during EntityWorld.Render().
/// </summary>
/// <remarks>
/// Systems are scene-scoped and automatically cleaned up when the scene unloads.
/// Use render systems for batch rendering of many entities (sprite batching, particle rendering, etc.).
/// For entity-specific rendering, use EntityBehavior instead.
/// </remarks>
public interface IRenderSystem : ISystem
{
    /// <summary>
    /// Determines the order in which this system executes during the render phase.
    /// Lower values execute first. Default is 0.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="SystemRenderOrder"/> constants for common phases:
    /// - Background rendering: -100
    /// - Sprite rendering: 0
    /// - UI rendering: 900
    /// - Debug rendering: 1000
    /// </para>
    /// </remarks>
    int RenderOrder => 0; // Default implementation
    
    /// <summary>
    /// Called every frame to render this system.
    /// </summary>
    /// <param name="world">The entity world to process.</param>
    /// <param name="renderer">The renderer to draw with.</param>
    void Render(IEntityWorld world, IRenderer renderer);
}