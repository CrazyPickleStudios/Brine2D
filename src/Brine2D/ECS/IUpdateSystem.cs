using Brine2D.Core;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Interface for systems that process entities during the update phase.
/// Systems run before behaviors during EntityWorld.Update().
/// </summary>
/// <remarks>
/// Systems are scene-scoped and automatically cleaned up when the scene unloads.
/// Use systems for batch processing of many entities (physics, collision, etc.).
/// For entity-specific logic, use Behavior instead.
/// </remarks>
public interface IUpdateSystem : ISystem
{
    /// <summary>
    /// Determines the order in which this system executes during the update phase.
    /// Lower values execute first. Default is 0 (<see cref="SystemUpdateOrder.Update"/>).
    /// </summary>
    /// <remarks>
    /// This property must return a constant value. <see cref="EntityWorld"/> sorts systems
    /// once after registration; a value that changes at runtime will not trigger a re-sort.
    /// Use <see cref="SystemUpdateOrder"/> constants for common phases, or add offsets for
    /// fine-grained control (e.g., <c>SystemUpdateOrder.Physics + 10</c>).
    /// </remarks>
    int UpdateOrder => SystemUpdateOrder.Update;

    /// <summary>
    /// Called every frame to update this system.
    /// </summary>
    /// <param name="world">The entity world to process.</param>
    /// <param name="gameTime">Current game time.</param>
    void Update(IEntityWorld world, GameTime gameTime);
}