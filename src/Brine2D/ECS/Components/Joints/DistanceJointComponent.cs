using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components.Joints;

/// <summary>
///     Constrains the distance between two anchor points to a target length.
///     Supports a spring, translation limits, and a linear motor.
/// </summary>
public sealed class DistanceJointComponent : JointComponent
{
    /// <summary>
    ///     Current distance between anchors in pixels. Returns 0 if not yet live.
    /// </summary>
    public float CurrentLength => IsLive ? B2.DistanceJointGetCurrentLength(JointId) : 0f;

    public float DampingRatio
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public bool EnableLimit
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public bool EnableMotor
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public bool EnableSpring
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Spring frequency in Hz.
    /// </summary>
    public float Hertz
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Target rest length in pixels. Leave at 0 to use Box2D's default (computed from
    ///     initial anchor positions when the joint is first created).
    /// </summary>
    public float Length
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float MaxLength
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float MaxMotorForce
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float MinLength
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float MotorSpeed
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
        var def = B2.DefaultDistanceJointDef();
        def.bodyIdA = bodyIdA;
        def.bodyIdB = ConnectedBody!.BodyId;
        def.localAnchorA = new B2.Vec2 { x = LocalAnchorA.X, y = LocalAnchorA.Y };
        def.localAnchorB = new B2.Vec2 { x = LocalAnchorB.X, y = LocalAnchorB.Y };
        def.collideConnected = CollideConnected;

        if (Length > 0f)
        {
            def.length = Length;
        }

        def.enableSpring = EnableSpring;
        def.hertz = Hertz;
        def.dampingRatio = DampingRatio;
        def.enableLimit = EnableLimit;
        def.minLength = MinLength;
        def.maxLength = MaxLength;
        def.enableMotor = EnableMotor;
        def.motorSpeed = MotorSpeed;
        def.maxMotorForce = MaxMotorForce;

        return world.CreateDistanceJoint(&def);
    }
}