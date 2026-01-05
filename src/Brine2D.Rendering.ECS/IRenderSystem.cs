using Brine2D.ECS.Systems;
using Brine2D.Rendering;

namespace Brine2D.Rendering.ECS;

/// <summary>
/// Interface for systems that render visuals.
/// Lives in Brine2D.Rendering.ECS because it depends on IRenderer.
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
    void Render(IRenderer renderer);
}