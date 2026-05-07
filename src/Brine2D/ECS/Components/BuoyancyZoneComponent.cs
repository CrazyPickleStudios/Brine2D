using System.Numerics;

namespace Brine2D.ECS.Components;

/// <summary>
/// Marks a trigger <see cref="PhysicsBodyComponent"/> as a fluid zone that applies buoyancy,
/// linear drag, and optional flow forces to overlapping dynamic bodies each fixed-update tick.
/// </summary>
/// <remarks>
/// <para>
/// The entity must also have a <see cref="PhysicsBodyComponent"/> with <c>IsTrigger = true</c>.
/// The <see cref="Brine2D.Systems.Physics.BuoyancySystem"/> handles force application automatically
/// when added to the scene.
/// </para>
/// <para>
/// Submersion fraction is estimated via AABB intersection between the zone and each body.
/// This is fast and accurate for axis-aligned rectangular bodies but will over- or under-estimate
/// for rotated, circular, or non-rectangular shapes. For most gameplay use cases the approximation
/// is imperceptible.
/// </para>
/// </remarks>
public class BuoyancyZoneComponent : Component
{
    /// <summary>
    /// Density of the fluid relative to the default body density.
    /// A value of <c>1</c> neutrally suspends a body with density 1; greater than <c>1</c>
    /// causes bodies to float, less than <c>1</c> causes them to sink.
    /// Default is <c>1</c>.
    /// </summary>
    public float FluidDensity { get; set; } = 1f;

    /// <summary>
    /// Drag coefficient applied to the relative velocity between the body and the fluid.
    /// Higher values dampen motion more aggressively. Default is <c>1</c>.
    /// </summary>
    public float LinearDrag { get; set; } = 1f;

    /// <summary>
    /// Bulk velocity of the fluid in pixels per second. Non-zero values push overlapping
    /// bodies in the flow direction (e.g. rivers, currents). Default is <see cref="Vector2.Zero"/>.
    /// </summary>
    public Vector2 FlowVelocity { get; set; }
}