using Brine2D.Core;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Interface for systems that process entities during the update phase.
/// Systems run before behaviors during EntityWorld.Update().
/// </summary>
/// <remarks>
/// Systems are scene-scoped and automatically cleaned up when the scene unloads.
/// Use systems for batch processing of many entities (physics, collision, etc.).
/// For entity-specific logic, use EntityBehavior instead.
/// </remarks>
public interface IUpdateSystem : ISystem
{
    /// <summary>
    /// Determines the order in which this system executes during the update phase.
    /// Lower values execute first. Default is 0.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="SystemUpdateOrder"/> constants for common phases:
    /// - Input systems: -100
    /// - Physics systems: 100
    /// - Collision systems: 200
    /// - Late update systems: 900
    /// </para>
    /// <para>
    /// You can use custom values or offsets:
    /// <code>
    /// public int UpdateOrder => SystemUpdateOrder.Physics + 10; // Right after physics
    /// </code>
    /// </para>
    /// </remarks>
    int UpdateOrder => 0; // Default implementation
    
    /// <summary>
    /// Called every frame to update this system.
    /// </summary>
    /// <param name="world">The entity world to process.</param>
    /// <param name="gameTime">Current game time.</param>
    void Update(IEntityWorld world, GameTime gameTime);
}