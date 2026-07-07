using Brine2D.Core;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;

namespace Brine2D.ECS;

/// <summary>
///     Interface for systems that process entities during the render phase.
///     Render systems run before behaviors during EntityWorld.Render().
/// </summary>
/// <remarks>
///     Systems are scene-scoped and automatically cleaned up when the scene unloads.
///     Use render systems for batch rendering of many entities (sprite batching, particle rendering, etc.).
///     For entity-specific rendering, use Behavior instead.
/// </remarks>
public interface IRenderSystem : ISystem
{
    /// <summary>
    ///     Determines the order in which this system executes during the render phase.
    ///     Lower values execute first. Default is 0 (<see cref="SystemRenderOrder.Sprites" />).
    /// </summary>
    /// <remarks>
    ///     This property must return a constant value. <see cref="EntityWorld" /> sorts systems
    ///     once after registration; a value that changes at runtime will not trigger a re-sort.
    ///     Use <see cref="SystemRenderOrder" /> constants for common phases.
    /// </remarks>
    int RenderOrder => SystemRenderOrder.Sprites;

    /// <summary>
    ///     Called every frame to render this system.
    /// </summary>
    /// <param name="world">The entity world to process.</param>
    /// <param name="renderer">The renderer to draw with.</param>
    /// <param name="gameTime">
    ///     Render-phase game time. <see cref="GameTime.Alpha"/> holds the physics interpolation
    ///     factor (0–1) representing how far the current frame sits between the last two fixed
    ///     timesteps. Use it to lerp rendered positions between previous and current physics state.
    /// </param>
    /// <remarks>
    ///     For a first-frame initialization hook, extend <see cref="Systems.RenderSystemBase"/>
    ///     and override <see cref="Systems.RenderSystemBase.OnStart"/>.
    /// </remarks>
    void Render(IEntityWorld world, IRenderer renderer, GameTime gameTime);
}