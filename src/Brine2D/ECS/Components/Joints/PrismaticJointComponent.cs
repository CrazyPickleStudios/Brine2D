using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components.Joints;

/// <summary>
///     Constrains two bodies to slide along a local axis while preventing relative rotation.
///     Supports translation limits, a linear motor, and a spring.
///     <see cref="JointComponent.LocalAnchorA" /> and <see cref="JointComponent.LocalAnchorB" />
///     are the anchor points on each body.
/// </summary>
public sealed class PrismaticJointComponent : JointComponent
{
    /// <summary>
    ///     Current translation along the axis in pixels. Returns 0 if not yet live.
    /// </summary>
    public float CurrentTranslation => IsLive ? B2.PrismaticJointGetTranslation(JointId) : 0f;

    /// <summary>
    ///     Spring damping ratio. 0 = undamped, 1 = critical.
    /// </summary>
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
    ///     Spring stiffness in Hz. 0 = rigid.
    /// </summary>
    public float HertzFrequency
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
    ///     Local axis of translation on body A (normalized). Default is <see cref="Vector2.UnitX" />.
    /// </summary>
    public Vector2 LocalAxisA
    {
        get;
        set
        {
            field = Vector2.Normalize(value);
            IsDirty = true;
        }
    } = Vector2.UnitX;

    /// <summary>
    ///     Lower translation limit in pixels.
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
    ///     Maximum motor force in Newtons.
    /// </summary>
    public float MaxMotorForce
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
    ///     Current motor force in Newtons. Returns 0 if not yet live.
    /// </summary>
    public float MotorForce => IsLive ? B2.PrismaticJointGetMotorForce(JointId) : 0f;

    /// <summary>
    ///     Desired motor speed in pixels per second.
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

    public float ReferenceAngle
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Upper translation limit in pixels.
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
        var def = B2.DefaultPrismaticJointDef();

        def.bodyIdA = bodyIdA;
        def.bodyIdB = ConnectedBody!.BodyId;
        def.localAnchorA = new B2.Vec2 { x = LocalAnchorA.X, y = LocalAnchorA.Y };
        def.localAnchorB = new B2.Vec2 { x = LocalAnchorB.X, y = LocalAnchorB.Y };
        def.localAxisA = new B2.Vec2 { x = LocalAxisA.X, y = LocalAxisA.Y };
        def.collideConnected = CollideConnected;
        def.referenceAngle = ReferenceAngle;
        def.enableLimit = EnableLimit;
        def.lowerTranslation = LowerTranslation;
        def.upperTranslation = UpperTranslation;
        def.enableMotor = EnableMotor;
        def.maxMotorForce = MaxMotorForce;
        def.motorSpeed = MotorSpeed;
        def.enableSpring = HertzFrequency > 0f;
        def.hertz = HertzFrequency;
        def.dampingRatio = DampingRatio;

        return world.CreatePrismaticJoint(&def);
    }
}