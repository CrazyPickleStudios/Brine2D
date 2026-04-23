using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components.Joints;

/// <summary>
///     Models a vehicle wheel: body B translates along a local axis on body A and can rotate freely.
///     Supports suspension spring, translation limits, and a spin motor.
/// </summary>
public sealed class WheelJointComponent : JointComponent
{
    public float DampingRatio
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
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

    /// <summary>
    ///     Enable the suspension spring.
    /// </summary>
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
    ///     Suspension spring frequency in Hz.
    /// </summary>
    public float Hertz
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Suspension axis in body A local space (normalized). Default is <see cref="Vector2.UnitY" />
    ///     (vertical suspension).
    /// </summary>
    public Vector2 LocalAxisA
    {
        get;
        set
        {
            field = Vector2.Normalize(value);
            IsDirty = true;
        }
    } = Vector2.UnitY;

    /// <summary>
    ///     Lower suspension travel limit in pixels.
    /// </summary>
    public float LowerTranslation
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Maximum torque the spin motor can apply (N·m).
    /// </summary>
    public float MaxMotorTorque
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Desired wheel spin speed in radians per second.
    /// </summary>
    public float MotorSpeed
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Current reaction torque at the motor (N·m). Returns 0 if not yet live.
    /// </summary>
    public float MotorTorque => IsLive ? B2.WheelJointGetMotorTorque(JointId) : 0f;

    /// <summary>
    ///     Upper suspension travel limit in pixels.
    /// </summary>
    public float UpperTranslation
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
        var def = B2.DefaultWheelJointDef();

        def.bodyIdA = bodyIdA;
        def.bodyIdB = ConnectedBody!.BodyId;
        def.localAnchorA = new B2.Vec2 { x = LocalAnchorA.X, y = LocalAnchorA.Y };
        def.localAnchorB = new B2.Vec2 { x = LocalAnchorB.X, y = LocalAnchorB.Y };
        def.localAxisA = new B2.Vec2 { x = LocalAxisA.X, y = LocalAxisA.Y };
        def.collideConnected = CollideConnected;
        def.enableLimit = EnableLimit;
        def.lowerTranslation = LowerTranslation;
        def.upperTranslation = UpperTranslation;
        def.enableMotor = EnableMotor;
        def.motorSpeed = MotorSpeed;
        def.maxMotorTorque = MaxMotorTorque;
        def.enableSpring = EnableSpring;
        def.hertz = Hertz;
        def.dampingRatio = DampingRatio;

        return world.CreateWheelJoint(&def);
    }
}