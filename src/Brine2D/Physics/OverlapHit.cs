using Brine2D.ECS.Components;

namespace Brine2D.Physics;

/// <summary>
/// Result of a per-shape overlap query against the physics world.
/// </summary>
public readonly struct OverlapHit
{
    /// <summary>
    /// The <see cref="PhysicsBodyComponent"/> whose shape was overlapped, or <c>null</c> if
    /// the body has no associated component.
    /// </summary>
    public PhysicsBodyComponent? Component { get; init; }

    /// <summary>
    /// The specific sub-shape that was overlapped, or <c>null</c> when the primary shape was hit
    /// (or the body uses a chain shape whose segments have no individual <see cref="SubShape"/>).
    /// </summary>
    public SubShape? SubShape { get; init; }
}