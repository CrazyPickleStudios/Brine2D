using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components.Joints;

/// <summary>
///     Locks two bodies together at a shared anchor with optional spring softness.
///     A rigid weld (zero hertz) is equivalent to merging the two bodies.
/// </summary>
public sealed class WeldJointComponent : JointComponent
{
    public float AngularDampingRatio
    {
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Angular spring frequency in Hz. 0 = rigid.
    /// </summary>
    public float AngularHertz
    {
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float LinearDampingRatio
    {
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Linear spring frequency in Hz. 0 = rigid.
    /// </summary>
    public float LinearHertz
    {
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Initial relative angle between the bodies in radians.
    /// </summary>
    public float ReferenceAngle
    {
        get => field;
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