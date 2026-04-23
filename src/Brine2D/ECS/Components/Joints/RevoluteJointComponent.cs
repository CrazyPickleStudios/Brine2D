using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components.Joints;

/// <summary>
///     Constrains two bodies to share a common anchor point, allowing relative rotation.
///     Supports angle limits, a rotational motor, and a spring.
///     <see cref="JointComponent.LocalAnchorA" /> and <see cref="JointComponent.LocalAnchorB" />
///     define the pivot point on each body.
/// </summary>
public sealed class RevoluteJointComponent : JointComponent
{
    /// <summary>
    ///     Current joint angle in radians. Returns 0 if the joint is not yet live.
    /// </summary>
    public float CurrentAngle => IsLive ? B2.RevoluteJointGetAngle(JointId) : 0f;

    public float DampingRatio
    {
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public bool EnableLimit
    {
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public bool EnableMotor
    {
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public bool EnableSpring
    {
        get => field;
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
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Lower angle limit in radians.
    /// </summary>
    public float LowerAngle
    {
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Maximum torque the motor can exert (N·m).
    /// </summary>
    public float MaxMotorTorque
    {
        get => field;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Desired motor speed in radians per second.
    /// </summary>
    public float MotorSpeed
    {
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Current reaction torque at the motor (N·m). Returns 0 if not yet live.
    /// </summary>
    public float MotorTorque => IsLive ? B2.RevoluteJointGetMotorTorque(JointId) : 0f;

    public float ReferenceAngle
    {
        get => field;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Upper angle limit in radians.
    /// </summary>
    public float UpperAngle
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
        var def = B2.DefaultRevoluteJointDef();

        def.bodyIdA = bodyIdA;
        def.bodyIdB = ConnectedBody!.BodyId;
        def.localAnchorA = new B2.Vec2 { x = LocalAnchorA.X, y = LocalAnchorA.Y };
        def.localAnchorB = new B2.Vec2 { x = LocalAnchorB.X, y = LocalAnchorB.Y };
        def.collideConnected = CollideConnected;
        def.referenceAngle = ReferenceAngle;
        def.enableLimit = EnableLimit;
        def.lowerAngle = LowerAngle;
        def.upperAngle = UpperAngle;
        def.enableMotor = EnableMotor;
        def.motorSpeed = MotorSpeed;
        def.maxMotorTorque = MaxMotorTorque;
        def.enableSpring = EnableSpring;
        def.hertz = Hertz;
        def.dampingRatio = DampingRatio;

        return world.CreateRevoluteJoint(&def);
    }
}