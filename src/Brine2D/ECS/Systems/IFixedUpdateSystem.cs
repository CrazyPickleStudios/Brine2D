using Brine2D.Core;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Interface for systems that process entities during the fixed update phase.
/// Fixed update systems run at a constant timestep, before the variable-rate update.
/// </summary>
/// <remarks>
/// Systems are scene-scoped and automatically cleaned up when the scene unloads.
/// Use fixed update systems for deterministic simulation (physics, collision, etc.).
/// For per-frame logic, use <see cref="IUpdateSystem"/> instead.
/// </remarks>
public interface IFixedUpdateSystem : ISystem
{
    /// <summary>
    /// Determines the order in which this system executes during the fixed update phase.
    /// Lower values execute first. Default is 0 (<see cref="SystemFixedUpdateOrder.Physics"/>).
    /// </summary>
    /// <remarks>
    /// This property must return a constant value. <see cref="EntityWorld"/> sorts systems
    /// once after registration; a value that changes at runtime will not trigger a re-sort.
    /// Use <see cref="SystemFixedUpdateOrder"/> constants for common phases.
    /// </remarks>
    int FixedUpdateOrder => SystemFixedUpdateOrder.Physics;

    /// <summary>
    /// Called at a fixed timestep to update this system.
    /// </summary>
    /// <param name="world">The entity world to process.</param>
    /// <param name="fixedTime">
    /// Game time with a constant <see cref="GameTime.ElapsedTime"/> equal to the configured
    /// fixed timestep, and a <see cref="GameTime.TotalTime"/> tracking total simulated fixed time.
    /// </param>
    void FixedUpdate(IEntityWorld world, GameTime fixedTime);
}