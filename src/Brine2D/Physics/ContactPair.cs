using Brine2D.Collision;
using Brine2D.ECS.Components;

namespace Brine2D.Physics;

/// <summary>
/// A single active contact between this body and another, returned by
/// <see cref="PhysicsWorld.GetContacts"/>.
/// </summary>
public readonly struct ContactPair
{
    /// <summary>
    /// The other body in the contact pair.
    /// </summary>
    public PhysicsBodyComponent? Other { get; init; }

    /// <summary>
    /// Contact data for this pair. Normal is oriented away from <see cref="Other"/> toward the
    /// queried body — consistent with <see cref="PhysicsBodyComponent.OnCollisionEnter"/>.
    /// </summary>
    public CollisionContact Contact { get; init; }

    /// <summary>
    /// The sub-shape on the queried body involved in this contact, or <c>null</c> if
    /// the primary shape (or a chain segment) was the touching surface.
    /// </summary>
    public SubShape? SelfSubShape { get; init; }

    /// <summary>
    /// The sub-shape on <see cref="Other"/> involved in this contact, or <c>null</c> if
    /// the other body's primary shape (or a chain segment) was the touching surface.
    /// </summary>
    public SubShape? OtherSubShape { get; init; }
}