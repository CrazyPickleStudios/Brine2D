using Brine2D.Rendering;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Interface for systems that render visuals.
/// </summary>
public interface IRenderSystem : ISystem
{
    /// <summary>
    /// Render order for this system (lower values render first/background).
    /// </summary>
    int RenderOrder { get; }

    /// <summary>
    /// Renders the system for the current frame.
    /// </summary>
    /// <param name="renderer">The renderer to draw with.</param>
    /// <param name="world">The entity world to operate on.</param>
    void Render(IRenderer renderer, IEntityWorld world);
}