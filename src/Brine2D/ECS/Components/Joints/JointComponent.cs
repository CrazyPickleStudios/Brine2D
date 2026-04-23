using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components.Joints;

/// <summary>
///     Base class for all physics joint components. Place this component on the entity that
///     acts as body A. Set <see cref="ConnectedBody" /> to the other body (body B).
///     The joint is created by <see cref="Brine2D.Systems.Physics.Box2DPhysicsSystem" /> once both
///     bodies have live Box2D bodies, and destroyed automatically when this component is removed
///     or either body is destroyed.
/// </summary>
public abstract class JointComponent : Component
{
    /// <summary>
    ///     Fired once when this joint is destroyed for any reason:
    ///     <list type="bullet">
    ///         <item>The reaction force exceeded <see cref="BreakForce" />.</item>
    ///         <item>The reaction torque exceeded <see cref="BreakTorque" />.</item>
    ///         <item>
    ///             The connected body's entity was destroyed or its <see cref="PhysicsBodyComponent" />
    ///             was removed, causing Box2D to implicitly destroy the joint.
    ///         </item>
    ///     </list>
    ///     After this fires, <see cref="IsLive" /> is <c>false</c> and the joint rebuilds
    ///     automatically on the next step if <see cref="ConnectedBody" /> still has a valid body.
    /// </summary>
    public event Action<JointComponent>? OnBreak;

    /// <summary>
    ///     Maximum constraint force magnitude (in simulation force units: mass × pixels/s²) before
    ///     the joint breaks. When the reaction force exceeds this value the joint is destroyed and
    ///     <see cref="OnBreak" /> is fired. Default is <see cref="float.PositiveInfinity" /> (never breaks).
    /// </summary>
    public float BreakForce { get; set; } = float.PositiveInfinity;

    /// <summary>
    ///     Maximum constraint torque magnitude (in simulation torque units: mass × pixels²/s²) before
    ///     the joint breaks. When the reaction torque exceeds this value the joint is destroyed and
    ///     <see cref="OnBreak" /> is fired. Default is <see cref="float.PositiveInfinity" /> (never breaks).
    /// </summary>
    public float BreakTorque { get; set; } = float.PositiveInfinity;

    /// <summary>
    ///     When <c>true</c>, the two joined bodies can still collide with each other.
    ///     Default is <c>false</c>.
    /// </summary>
    public bool CollideConnected
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     The other collider this joint is connected to (body B).
    ///     The entity this component is attached to supplies body A via its own
    ///     <see cref="PhysicsBodyComponent" />.
    /// </summary>
    public PhysicsBodyComponent? ConnectedBody
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Returns <c>true</c> if the underlying Box2D joint has been created and is still valid.
    /// </summary>
    public bool IsLive => B2.JointIsValid(JointId);

    /// <summary>
    ///     Local anchor point on body A, relative to its center of mass (pixels).
    /// </summary>
    public Vector2 LocalAnchorA
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Local anchor point on body B, relative to its center of mass (pixels).
    /// </summary>
    public Vector2 LocalAnchorB
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    internal bool IsDirty { get; set; } = true;

    internal B2.JointId JointId { get; set; }

    /// <summary>
    ///     Returns the constraint force exerted by this joint during the last step (simulation units).
    ///     Returns <see cref="Vector2.Zero" /> if the joint is not live.
    ///     Useful for detecting overloaded joints or triggering break logic.
    /// </summary>
    public Vector2 GetReactionForce()
    {
        if (!B2.JointIsValid(JointId))
        {
            return Vector2.Zero;
        }

        var f = B2.JointGetConstraintForce(JointId);

        return new Vector2(f.x, f.y);
    }

    /// <summary>
    ///     Returns the constraint torque exerted by this joint during the last step.
    ///     Returns 0 if the joint is not live.
    /// </summary>
    public float GetReactionTorque()
    {
        if (!B2.JointIsValid(JointId))
        {
            return 0f;
        }

        return B2.JointGetConstraintTorque(JointId);
    }

    /// <summary>
    ///     Creates the Box2D joint. Called by the physics system once both bodies are ready.
    /// </summary>
    internal abstract B2.JointId Build(PhysicsWorld world, B2.BodyId bodyIdA);

    internal void RaiseBreak()
    {
        OnBreak?.Invoke(this);
    }

    protected internal override void OnRemoved()
    {
        if (B2.JointIsValid(JointId))
        {
            B2.DestroyJoint(JointId);
        }

        JointId = default;
        ConnectedBody = null;
        IsDirty = true;
    }
}