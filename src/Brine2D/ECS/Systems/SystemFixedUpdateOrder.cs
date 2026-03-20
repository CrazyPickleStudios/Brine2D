namespace Brine2D.ECS.Systems;

/// <summary>
/// Defines standard execution order values for fixed update systems.
/// Lower values execute first.
/// </summary>
/// <remarks>
/// <para>
/// Fixed update phases are narrower than variable-rate update phases because fixed
/// timestep work is typically limited to physics and collision simulation.
/// </para>
/// <example>
/// <code>
/// // Use standard order
/// public int FixedUpdateOrder => SystemFixedUpdateOrder.Physics;
///
/// // Custom offset (run right after physics)
/// public int FixedUpdateOrder => SystemFixedUpdateOrder.Physics + 10;
/// </code>
/// </example>
/// </remarks>
public static class SystemFixedUpdateOrder
{
    /// <summary>
    /// Early fixed update (e.g., applying forces, input-driven velocities).
    /// Order: -100
    /// </summary>
    public const int EarlyFixedUpdate = -100;

    /// <summary>
    /// Pre-physics phase (e.g., constraint setup, force accumulation).
    /// Order: -50
    /// </summary>
    public const int PrePhysics = -50;

    /// <summary>
    /// Physics simulation (e.g., velocity integration, position updates).
    /// Order: 0
    /// </summary>
    public const int Physics = 0;

    /// <summary>
    /// Post-physics phase (e.g., physics cleanup, constraint solving).
    /// Order: 50
    /// </summary>
    public const int PostPhysics = 50;

    /// <summary>
    /// Collision detection and resolution.
    /// Order: 100
    /// </summary>
    public const int Collision = 100;

    /// <summary>
    /// Post-collision processing (e.g., trigger events, damage calculation).
    /// Order: 150
    /// </summary>
    public const int PostCollision = 150;

    /// <summary>
    /// Late fixed update (e.g., final position adjustments).
    /// Order: 200
    /// </summary>
    public const int LateFixedUpdate = 200;
}