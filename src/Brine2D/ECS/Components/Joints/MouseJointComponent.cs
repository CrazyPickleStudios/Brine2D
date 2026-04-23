using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components.Joints;

/// <summary>
///     Pulls body B toward a world-space target point using a soft spring force.
///     Ideal for click-to-drag interactions and soft positional control.
///     Body A is typically a static ground body; body B is the body being pulled.
///     <para>
///         Unlike other joints, <see cref="Target" /> can be updated every frame without
///         rebuilding the joint — set it directly and Box2D will immediately respond.
///     </para>
/// </summary>
public sealed class MouseJointComponent : JointComponent
{
    private Vector2 _target;

    /// <summary>
    ///     Spring damping ratio (0 = undamped, 1 = critically damped). Default is 0.7.
    /// </summary>
    public float DampingRatio
    {
        get;
        set
        {
            field = Math.Clamp(value, 0f, 1f);
            IsDirty = true;
        }
    } = 0.7f;

    /// <summary>
    ///     Spring frequency in Hz. Higher values make the spring stiffer. Default is 5.
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
    } = 5f;

    /// <summary>
    ///     Maximum force the joint can exert in pixels/s². Must be >= 0.
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
    ///     World-space target point in pixel coordinates.
    ///     If the joint is live, this updates Box2D immediately (no rebuild required).
    /// </summary>
    public Vector2 Target
    {
        get => _target;
        set
        {
            _target = value;
            if (IsLive)
            {
                B2.MouseJointSetTarget(JointId, new B2.Vec2 { x = value.X, y = value.Y });
            }
        }
    }

    internal override unsafe B2.JointId Build(PhysicsWorld world, B2.BodyId bodyIdA)
    {
        var def = B2.DefaultMouseJointDef();

        def.bodyIdA = bodyIdA;
        def.bodyIdB = ConnectedBody!.BodyId;
        def.target = new B2.Vec2 { x = _target.X, y = _target.Y };
        def.maxForce = MaxForce;
        def.hertz = Hertz;
        def.dampingRatio = DampingRatio;
        def.collideConnected = CollideConnected;

        return world.CreateMouseJoint(&def);
    }
}