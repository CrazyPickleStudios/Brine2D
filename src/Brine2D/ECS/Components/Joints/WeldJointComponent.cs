using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components.Joints;

/// <summary>
///     Locks two bodies together at a shared anchor with optional spring softness.
///     A rigid weld (<see cref="LinearHertz"/> = 0) is equivalent to merging the two bodies.
/// </summary>
/// <remarks>
///     <b>Break force limitation:</b> Box2D 3.x only reports constraint force for soft joints.
///     <see cref="JointComponent.BreakForce"/> has no effect when <see cref="LinearHertz"/> is 0 (the default).
///     Set <see cref="LinearHertz"/> to a positive value to enable break detection.
/// </remarks>
public sealed class WeldJointComponent : JointComponent
{
    public float AngularDampingRatio
    {
        get;
        set
        {
            field = value;
            if (IsLive)
                B2.WeldJointSetAngularDampingRatio(JointId, value);
            else
                IsDirty = true;
        }
    }

    /// <summary>
    ///     Angular spring frequency in Hz. 0 = rigid.
    /// </summary>
    public float AngularHertz
    {
        get;
        set
        {
            field = value;
            if (IsLive)
                B2.WeldJointSetAngularHertz(JointId, value);
            else
                IsDirty = true;
        }
    }

    public float LinearDampingRatio
    {
        get;
        set
        {
            field = value;
            if (IsLive)
                B2.WeldJointSetLinearDampingRatio(JointId, value);
            else
                IsDirty = true;
        }
    }

    /// <summary>
    ///     Linear spring frequency in Hz. 0 = rigid.
    /// </summary>
    public float LinearHertz
    {
        get;
        set
        {
            field = value;
            if (IsLive)
                B2.WeldJointSetLinearHertz(JointId, value);
            else
                IsDirty = true;
        }
    }

    /// <summary>
    ///     Initial relative angle between the bodies in radians.
    ///     Requires a joint rebuild — set once at construction time.
    /// </summary>
    public float ReferenceAngle
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    internal override unsafe B2.JointId Build(PhysicsWorld world, B2.BodyId bodyIdA)
    {
        var def = B2.DefaultWeldJointDef();

        def.bodyIdA = bodyIdA;
        def.bodyIdB = ConnectedBody!.BodyId;
        def.localAnchorA = new B2.Vec2 { x = LocalAnchorA.X, y = LocalAnchorA.Y };
        def.localAnchorB = new B2.Vec2 { x = LocalAnchorB.X, y = LocalAnchorB.Y };
        def.collideConnected = CollideConnected;
        def.referenceAngle = ReferenceAngle;
        def.linearHertz = LinearHertz;
        def.linearDampingRatio = LinearDampingRatio;
        def.angularHertz = AngularHertz;
        def.angularDampingRatio = AngularDampingRatio;

        return world.CreateWeldJoint(&def);
    }
}