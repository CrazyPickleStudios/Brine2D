using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components.Joints;

/// <summary>
///     Drives body B toward a target position and angle relative to body A using forces and
///     torques. Useful for AI-controlled characters, moving platforms, and procedural animation.
///     Unlike other joints, anchors are not used — the motor acts on the body centers directly.
/// </summary>
public sealed class MotorJointComponent : JointComponent
{
    /// <summary>
    ///     Target angular offset of body B relative to body A in radians.
    /// </summary>
    public float AngularOffset
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Position correction factor in [0, 1]. Higher values correct error faster
    ///     but can cause instability. Default is 0.3.
    /// </summary>
    public float CorrectionFactor
    {
        get;
        set
        {
            field = Math.Clamp(value, 0f, 1f);
            IsDirty = true;
        }
    } = 0.3f;

    /// <summary>
    ///     Target linear offset of body B relative to body A in pixels.
    /// </summary>
    public Vector2 LinearOffset
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Maximum force used to reach the linear target (Newtons).
    /// </summary>
    public float MaxForce
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
    ///     Maximum torque used to reach the angular target (N·m).
    /// </summary>
    public float MaxTorque
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
            IsDirty = true;
        }
    }

    internal override unsafe B2.JointId Build(PhysicsWorld world, B2.BodyId bodyIdA)
    {
        var def = B2.DefaultMotorJointDef();

        def.bodyIdA = bodyIdA;
        def.bodyIdB = ConnectedBody!.BodyId;
        def.linearOffset = new B2.Vec2 { x = LinearOffset.X, y = LinearOffset.Y };
        def.angularOffset = AngularOffset;
        def.maxForce = MaxForce;
        def.maxTorque = MaxTorque;
        def.correctionFactor = CorrectionFactor;
        def.collideConnected = CollideConnected;

        return world.CreateMotorJoint(&def);
    }
}