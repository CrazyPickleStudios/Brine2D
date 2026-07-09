using System.Numerics;
using System.Text.Json.Serialization;
using Brine2D.Collision;
using Brine2D.Physics;

namespace Brine2D.ECS.Components;

/// <summary>
/// Adds character-controller behaviour to a <see cref="PhysicsBodyComponent"/> with
/// <see cref="PhysicsBodyType.Kinematic"/>. Set <see cref="Velocity"/> each fixed-update
/// frame (or call <see cref="MoveAndSlide"/>); the <see cref="Brine2D.Systems.Physics.KinematicCharacterSystem"/>
/// slides the velocity along contact surfaces and integrates it into
/// <see cref="TransformComponent.Position"/> before the physics step.
/// <see cref="IsGrounded"/>, <see cref="FloorNormal"/>, <see cref="CeilingNormal"/>, and
/// <see cref="GetSlideCollisions"/> are updated each tick after the step.
/// </summary>
/// <remarks>
/// Requires two <see cref="Brine2D.Systems.Physics.KinematicCharacterSystem"/> instances registered in DI
/// via <c>AddPhysics()</c>. Add both to the scene with
/// <c>world.AddSystem&lt;KinematicCharacterSystem&gt;()</c>.
/// <para>
/// <see cref="MoveAndCollide"/> and <see cref="MoveAndSlide"/> are mutually exclusive per tick.
/// When <see cref="MoveAndCollide"/> is queued, velocity integration is skipped for that tick.
/// </para>
/// </remarks>
public class KinematicCharacterBody : Component
{
    private readonly List<ContactPair> _slideCollisions = [];

    /// <summary>
    /// Desired movement velocity in pixels per second. Set this every fixed-update frame.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Maximum angle (radians) between the contact normal and the up direction for a surface
    /// to count as a floor. Default is ~46° (0.8 rad).
    /// </summary>
    public float FloorAngleLimit { get; set; } = 0.8f;

    /// <summary>
    /// Maximum angle (radians) between the contact normal and the wall angle limit for a surface
    /// to count as a wall. Contacts that are neither floor nor ceiling and fall within this range
    /// set <see cref="IsOnWall"/> and <see cref="WallNormal"/>.
    /// Default is <see cref="float.PositiveInfinity"/> (all non-floor, non-ceiling contacts qualify).
    /// </summary>
    public float WallAngleLimit { get; set; } = float.PositiveInfinity;

    /// <summary>
    /// Maximum angle (radians) between the contact normal and the down direction for a surface
    /// to count as a ceiling. Default is 0.8 rad (~46°). Contacts whose downward dot product
    /// is less than <c>cos(CeilingAngleLimit)</c> are not classified as ceiling.
    /// </summary>
    public float CeilingAngleLimit { get; set; } = 0.8f;

    /// <summary>
    /// Maximum number of velocity-deflection iterations in the pre-physics slide step.
    /// Default is 3. Increase for characters that must navigate tight corners reliably.
    /// </summary>
    public int MaxSlides { get; set; } = 3;

    /// <summary>
    /// Maximum distance in pixels to probe downward for a floor surface when the character
    /// leaves the ground. When greater than zero, the character snaps to the floor if a solid
    /// surface is found within this distance — useful for keeping the character grounded on
    /// stairs and slopes without going airborne. Default is 0 (disabled).
    /// </summary>
    public float SnapDistance { get; set; }

    /// <summary>
    /// The direction the character treats as "up" for floor detection and snap-to-floor.
    /// When <c>null</c> (default), the direction is derived each tick from the world gravity
    /// vector — correct for standard gravity. Override this for wall-walking, ceiling-walking,
    /// or per-character gravity effects.
    /// </summary>
    public Vector2? UpDirection { get; set; }

    /// <summary>
    /// When <c>true</c>, the character will not slide down a sloped floor when
    /// <see cref="Velocity"/> has no component pushing into the slope. Useful for holding a
    /// character in place on ramps without requiring friction. Default is <c>false</c>.
    /// </summary>
    public bool StopOnSlope { get; set; }

    /// <summary>
    /// Maximum force in pixels/s² the character can apply to dynamic bodies it walks into.
    /// When zero (default), no impulse is applied to dynamic bodies. Set to a positive value
    /// to push lightweight objects away when the character contacts them.
    /// </summary>
    public float PushForce { get; set; }

    /// <summary>
    /// <c>true</c> when the character is resting on a floor surface this tick.
    /// </summary>
    [JsonIgnore]
    public bool IsGrounded { get; internal set; }

    /// <summary>
    /// When <c>true</c>, the <see cref="KinematicCharacterSystem"/> emits per-tick diagnostic
    /// trace output for this character covering pre-deflection, slide iterations, and final
    /// position integration. Use only during debugging — disable before shipping.
    /// </summary>
    public bool EnableDebugLogging { get; set; }

    /// <summary>
    /// <c>true</c> when the character is touching a wall surface this tick.
    /// </summary>
    [JsonIgnore]
    public bool IsOnWall { get; internal set; }

    /// <summary>
    /// <c>true</c> when the character is touching a ceiling surface this tick.
    /// </summary>
    [JsonIgnore]
    public bool IsOnCeiling { get; internal set; }

    /// <summary>
    /// <c>true</c> when the character is touching a wall but is not on the floor or ceiling this tick.
    /// </summary>
    [JsonIgnore]
    public bool IsOnWallOnly => IsOnWall && !IsGrounded && !IsOnCeiling;

    /// <summary>
    /// The averaged floor contact normal from the last step, or <see cref="Vector2.Zero"/>
    /// when <see cref="IsGrounded"/> is <c>false</c>.
    /// </summary>
    [JsonIgnore]
    public Vector2 FloorNormal { get; internal set; }

    /// <summary>
    /// The averaged wall contact normal from the last step, or <see cref="Vector2.Zero"/>
    /// when <see cref="IsOnWall"/> is <c>false</c>.
    /// </summary>
    [JsonIgnore]
    public Vector2 WallNormal { get; internal set; }

    /// <summary>
    /// The ceiling contact normal from the last step, or <see cref="Vector2.Zero"/> when none.
    /// </summary>
    [JsonIgnore]
    public Vector2 CeilingNormal { get; internal set; }

    /// <summary>
    /// The linear velocity of the moving platform the character is standing on, or
    /// <see cref="Vector2.Zero"/> when not on a moving platform. Updated each post-physics step.
    /// </summary>
    [JsonIgnore]
    public Vector2 PlatformVelocity { get; internal set; }

    /// <summary>
    /// The slide-corrected velocity actually applied to the character this tick, in pixels per
    /// second. Unlike <see cref="Velocity"/> (the desired input), this reflects the velocity
    /// after deflection against contact surfaces. Set by the pre-physics step each tick.
    /// Useful for animation blending and air/ground speed readouts.
    /// </summary>
    [JsonIgnore]
    public Vector2 EffectiveVelocity { get; internal set; }

    /// <summary>
    /// Fired once when the character transitions from airborne to grounded.
    /// Invoked at the end of the post-physics step, after <see cref="IsGrounded"/> is set.
    /// </summary>
    public event Action<KinematicCharacterBody>? OnLanded;

    /// <summary>
    /// Fired once when the character transitions from grounded to airborne.
    /// Invoked at the end of the post-physics step, after <see cref="IsGrounded"/> is set.
    /// </summary>
    public event Action<KinematicCharacterBody>? OnAirborne;

    /// <summary>
    /// Returns all contact pairs resolved during the last post-physics step.
    /// Normals are oriented away from the other body toward this character.
    /// Useful for surface material detection, landing sounds, and wall-jump queries.
    /// The list is cleared and repopulated every tick — do not hold a reference across frames.
    /// </summary>
    public IReadOnlyList<ContactPair> GetSlideCollisions() => _slideCollisions;

    internal List<ContactPair> SlideCollisions => _slideCollisions;

    internal bool WasGrounded { get; set; }

    internal PhysicsBodyComponent? FloorBody { get; set; }

    internal float PlatformAngularVelocity { get; set; }

    internal Vector2 PlatformCenter { get; set; }

    /// <summary>
    /// Pending <see cref="MoveAndCollide"/> request set during FixedUpdate.
    /// The pre-step system consumes and clears this each tick.
    /// </summary>
    internal Vector2? PendingMoveAndCollide { get; set; }

    /// <summary>
    /// The result of the last <see cref="MoveAndCollide"/> call, or <c>null</c> when there was
    /// no collision. Set by the pre-step system after processing <see cref="PendingMoveAndCollide"/>.
    /// Contains full shape-cast detail including sub-shape information on compound bodies.
    /// </summary>
    [JsonIgnore]
    public ShapeCastHit? LastMoveAndCollideHit { get; internal set; }

    /// <summary>
    /// Maximum height in pixels the character can automatically step up onto when walking
    /// into a ledge. When zero (default), step climbing is disabled.
    /// </summary>
    public float StepHeight { get; set; }

    /// <summary>
    /// Maximum speed in pixels per second the character velocity is clamped to before the
    /// slide and integration step. When zero (default), no cap is applied.
    /// </summary>
    public float MaxSpeed { get; set; }

    /// <summary>
    /// The unused remainder of the last <see cref="MoveAndCollide"/> motion vector.
    /// <see cref="Vector2.Zero"/> when no collision occurred or the full motion was consumed.
    /// Available after the next pre-physics step.
    /// </summary>
    [JsonIgnore]
    public Vector2 MotionRemainder { get; internal set; }

    internal void RaiseLanded() => OnLanded?.Invoke(this);

    internal void RaiseAirborne() => OnAirborne?.Invoke(this);

    protected internal override void OnRemoved()
    {
        Velocity = Vector2.Zero;
        EffectiveVelocity = Vector2.Zero;
        PlatformVelocity = Vector2.Zero;
        PlatformAngularVelocity = 0f;
        PlatformCenter = Vector2.Zero;
        PendingMoveAndCollide = null;
        LastMoveAndCollideHit = null;
        OnLanded = null;
        OnAirborne = null;
        MotionRemainder = Vector2.Zero;
    }

    /// <summary>Sets <see cref="Velocity"/> and returns this component for fluent call sites.</summary>
    public KinematicCharacterBody MoveAndSlide(Vector2 velocity)
    {
        Velocity = velocity;
        return this;
    }

    /// <summary>
    /// Queues a discrete move for this tick. Unlike <see cref="MoveAndSlide"/>, the character
    /// moves exactly by <paramref name="motion"/> without sliding along surfaces. If a solid
    /// surface is hit during the cast, the character stops at the contact point and
    /// <see cref="LastMoveAndCollideHit"/> is set with full sub-shape detail; otherwise it is
    /// <c>null</c>. The result is available after the next pre-physics step.
    /// <para>
    /// Mutually exclusive with <see cref="MoveAndSlide"/> per tick — velocity integration is
    /// skipped for any tick in which a <see cref="MoveAndCollide"/> motion is queued.
    /// </para>
    /// </summary>
    public KinematicCharacterBody MoveAndCollide(Vector2 motion)
    {
        PendingMoveAndCollide = motion;
        return this;
    }
}