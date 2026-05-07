using System.Numerics;
using Brine2D.ECS.Components;

namespace Brine2D.Physics;

/// <summary>
/// Contact information passed to the pre-solve filter registered with
/// <see cref="PhysicsWorld.SetPreSolveFilter"/>. Return <c>false</c> to cancel the contact
/// for the current step without generating collision response forces.
/// </summary>
/// <remarks>
/// <para>
/// The pre-solve callback is invoked by the Box2D solver on the simulation thread every
/// step for every active contact pair. Keep the callback allocation-free.
/// </para>
/// <para>
/// <b>Primary use-case: one-way platforms.</b> Check <see cref="Normal"/> and cancel the
/// contact when the body is approaching from the wrong side:
/// <code>
/// world.SetPreSolveFilter(c =>
/// {
///     bool platformIsA = c.BodyA.Entity?.Name == "Platform";
///     var normal = platformIsA ? c.Normal : -c.Normal;
///     return normal.Y &lt;= 0f; // only collide from above (Y-down)
/// });
/// </code>
/// </para>
/// </remarks>
public readonly struct PreSolveContact
{
    /// <summary>Body A in the contact pair.</summary>
    public required PhysicsBodyComponent BodyA { get; init; }

    /// <summary>Body B in the contact pair.</summary>
    public required PhysicsBodyComponent BodyB { get; init; }

    /// <summary>
    /// The sub-shape on <see cref="BodyA"/> involved in the contact, or <c>null</c>
    /// when the primary shape is the contact shape.
    /// </summary>
    public SubShape? SubShapeA { get; init; }

    /// <summary>
    /// The sub-shape on <see cref="BodyB"/> involved in the contact, or <c>null</c>
    /// when the primary shape is the contact shape.
    /// </summary>
    public SubShape? SubShapeB { get; init; }

    /// <summary>
    /// The contact normal pointing from <see cref="BodyA"/> toward <see cref="BodyB"/>.
    /// </summary>
    public required Vector2 Normal { get; init; }
}