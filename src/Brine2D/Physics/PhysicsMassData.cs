using System.Numerics;

namespace Brine2D.Physics;

/// <summary>
/// Mass properties of a physics body at a point in time.
/// </summary>
public readonly struct PhysicsMassData
{
    /// <summary>
    /// Total mass of the body in simulation units.
    /// </summary>
    public float Mass { get; init; }

    /// <summary>
    /// Rotational inertia about the center of mass.
    /// </summary>
    public float Inertia { get; init; }

    /// <summary>
    /// World-space position of the center of mass in pixel coordinates.
    /// </summary>
    public Vector2 WorldCenterOfMass { get; init; }
}