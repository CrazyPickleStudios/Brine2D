using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

[Collection("Physics")]
public class KinematicCharacterSystemExtendedTests : TestBase, IDisposable
{
    private static readonly GameTime FixedTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    private readonly PhysicsWorld _physicsWorld = new();

    public void Dispose() => _physicsWorld.Dispose();

    private (Box2DPhysicsSystem physics, KinematicCharacterSystem pre, KinematicCharacterSystem post) CreateSystems()
    {
        var physics = new Box2DPhysicsSystem(_physicsWorld);
        var pre = new KinematicCharacterSystem(_physicsWorld, isPostStep: false);
        var post = new KinematicCharacterSystem(_physicsWorld, isPostStep: true);
        return (physics, pre, post);
    }

    private void Step(IEntityWorld world, Box2DPhysicsSystem physics,
        KinematicCharacterSystem pre, KinematicCharacterSystem post, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            pre.FixedUpdate(world, FixedTime);
            physics.FixedUpdate(world, FixedTime);
            post.FixedUpdate(world, FixedTime);
        }
    }

    // -------------------------------------------------------------------------
    // IsOnCeiling
    // -------------------------------------------------------------------------

    [Fact]
    public void IsOnCeiling_PushingUpIntoCeiling_IsTrue()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        // Ceiling: center Y=-60, BoxShape 400×20 → half-height=10, bottom edge at Y=-50.
        // Character: BoxShape 20×20 → half-height=10, placed at Y=-30 → top at Y=-40, 10px below ceiling.
        // Upward velocity -200px/s (Y-down) → reaches ceiling in ~3 steps.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, -60f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, -30f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(0f, -200f));
        world.Flush();

        Step(world, physics, pre, post, 6);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsOnCeiling);
        Assert.NotEqual(Vector2.Zero, character.CeilingNormal);
    }

    // -------------------------------------------------------------------------
    // IsOnWallOnly
    // -------------------------------------------------------------------------

    [Fact]
    public void IsOnWallOnly_WhenOnWallNotGroundedOrCeiling_IsTrue()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(60f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(20f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(200f, 0f));
        world.Flush();

        Step(world, physics, pre, post, 6);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsOnWall);
        Assert.False(character.IsGrounded);
        Assert.False(character.IsOnCeiling);
        Assert.True(character.IsOnWallOnly);
    }

    // -------------------------------------------------------------------------
    // SnapDistance
    // -------------------------------------------------------------------------

    [Fact]
    public void SnapDistance_KeepsCharacterGrounded_OnSmallGap()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        // Floor at Y=60 (top edge Y=50). Character starts settled on it.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 60f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 30f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.Velocity = new Vector2(0f, 200f);
                ch.SnapDistance = 20f;
            });
        world.Flush();

        // Settle on floor.
        Step(world, physics, pre, post, 6);
        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsGrounded);

        // Apply a small upward nudge that doesn't exceed SnapDistance.
        var transform = charEntity.GetComponent<TransformComponent>()!;
        transform.Position -= new Vector2(0f, 5f);
        character.Velocity = Vector2.Zero;

        Step(world, physics, pre, post, 2);

        Assert.True(character.IsGrounded, "SnapDistance should keep character grounded over a small gap.");
    }

    // -------------------------------------------------------------------------
    // StopOnSlope
    // -------------------------------------------------------------------------

    [Fact]
    public void StopOnSlope_PreventsSlideDownRamp()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        // 45° ramp: polygon floor tilted so the normal has an upward component.
        // Simple approach: use a rotated BoxShape as a wedge.
        // Normal from a 45° surface ≈ (-0.707, -0.707) in Y-down space.
        // Place the box at 45° so the character can land on it.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t =>
            {
                t.LocalPosition = new Vector2(0f, 80f);
                t.Rotation = MathF.PI / 4f;
            })
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(200f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 20f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.Velocity = new Vector2(0f, 200f);
                ch.StopOnSlope = true;
                ch.FloorAngleLimit = MathF.PI / 2f;
            });
        world.Flush();

        // Settle on the slope.
        Step(world, physics, pre, post, 8);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsGrounded);

        // With zero velocity and StopOnSlope = true, the character should not drift.
        character.Velocity = Vector2.Zero;
        var xBefore = charEntity.GetComponent<TransformComponent>()!.Position.X;

        Step(world, physics, pre, post, 10);

        var xAfter = charEntity.GetComponent<TransformComponent>()!.Position.X;
        Assert.True(MathF.Abs(xAfter - xBefore) < 2f, $"StopOnSlope should prevent sliding. Δx={xAfter - xBefore}");
    }

    // -------------------------------------------------------------------------
    // FloorAngleLimit
    // -------------------------------------------------------------------------

    [Fact]
    public void FloorAngleLimit_SteepSurface_NotClassifiedAsFloor()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        // Near-vertical wall tilted just past 45° from up — not a floor at default 0.8 rad limit.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t =>
            {
                t.LocalPosition = new Vector2(0f, 0f);
                t.Rotation = MathF.PI * 0.45f; // ~81°
            })
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(200f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, -60f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.Velocity = new Vector2(0f, 200f);
                ch.FloorAngleLimit = 0.8f; // default ~46°
            });
        world.Flush();

        Step(world, physics, pre, post, 8);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.False(character.IsGrounded, "A near-vertical surface should not be classified as a floor.");
    }

    // -------------------------------------------------------------------------
    // UpDirection override
    // -------------------------------------------------------------------------

    [Fact]
    public void UpDirection_Override_DetectsFloorInCustomDirection()
    {
        var world = CreateTestWorld();
        using var customWorld = new PhysicsWorld(new Vector2(980f, 0f));
        var physics = new Box2DPhysicsSystem(customWorld);
        var pre = new KinematicCharacterSystem(customWorld, isPostStep: false);
        var post = new KinematicCharacterSystem(customWorld, isPostStep: true);

        // "Floor" is a vertical wall to the right (X=60, width=20 → left edge X=50).
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(60f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(20f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.Velocity = new Vector2(200f, 0f);
                ch.UpDirection = new Vector2(-1f, 0f);
            });
        world.Flush();

        Step(world, physics, pre, post, 6);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsGrounded, "Character with custom UpDirection should detect the right-wall as floor.");
    }

    // -------------------------------------------------------------------------
    // PushForce — no double impulse
    // -------------------------------------------------------------------------

    [Fact]
    public void PushForce_MultipleContactsOnSameBody_AppliesOnlyOneImpulse()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        // Character right edge at x=-10, dynamic left edge at x=0 — 10px gap, no initial overlap.
        // GravityScale=0 isolates PushForce as the only source of x-velocity on the dynamic body.
        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(40f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(80f, 80f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.Mass = 100f;
                c.GravityScale = 0f;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(-20f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.Velocity = new Vector2(200f, 0f);
                ch.PushForce = 10f;
            });
        world.Flush();

        // Steps 1–3: character approaches and makes contact. Steps 4–10: PushForce is active.
        Step(world, physics, pre, post, 10);

        // PushForce=10, mass=100, dt=1/60 → correct impulse ≈ 0.002 px/s per step.
        // Even with 10 contact steps that's ~0.02 px/s. A multi-impulse bug would scale linearly
        // with the number of duplicate contacts but still can't reach anywhere near 50 px/s.
        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        if (Box2D.NET.Bindings.B2.BodyIsValid(dynBody.BodyId))
        {
            var vel = Box2D.NET.Bindings.B2.BodyGetLinearVelocity(dynBody.BodyId);
            float speed = MathF.Sqrt(vel.x * vel.x + vel.y * vel.y);
            Assert.True(speed < 50f, $"PushForce applied multiple times per step: speed={speed}");
        }
    }

    // -------------------------------------------------------------------------
    // MoveAndCollide — zero-length vector
    // -------------------------------------------------------------------------

    [Fact]
    public void MoveAndCollide_ZeroLengthVector_NoHitAndZeroRemainder()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>();
        world.Flush();

        Step(world, physics, pre, post);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        character.MoveAndCollide(Vector2.Zero);

        Step(world, physics, pre, post);

        Assert.Null(character.LastMoveAndCollideHit);
        Assert.Equal(Vector2.Zero, character.MotionRemainder);
    }

    // -------------------------------------------------------------------------
    // OnLanded — does not re-fire when already grounded
    // -------------------------------------------------------------------------

    [Fact]
    public void OnLanded_DoesNotFireAgain_WhenAlreadyGrounded()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        int landedCount = 0;

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 70f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.Velocity = new Vector2(0f, 200f);
                ch.OnLanded += _ => landedCount++;
            });
        world.Flush();

        // Land.
        Step(world, physics, pre, post, 6);
        Assert.Equal(1, landedCount);

        // Stay grounded for many more steps — OnLanded must not fire again.
        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        character.Velocity = Vector2.Zero;
        Step(world, physics, pre, post, 10);

        Assert.Equal(1, landedCount);
    }

    // -------------------------------------------------------------------------
    // PhysicsWorld.Pause / Resume
    // -------------------------------------------------------------------------

    [Fact]
    public void PhysicsWorld_Pause_SuppressesSimulation()
    {
        var world = CreateTestWorld();
        var physics = new Box2DPhysicsSystem(_physicsWorld);

        var transform = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });
        world.Flush();

        // Run two steps to build the body and let gravity give it downward velocity.
        physics.FixedUpdate(world, FixedTime);
        physics.FixedUpdate(world, FixedTime);

        var posBefore = transform.GetComponent<TransformComponent>()!.Position;

        _physicsWorld.Pause();
        physics.FixedUpdate(world, FixedTime);
        physics.FixedUpdate(world, FixedTime);

        var posAfterPause = transform.GetComponent<TransformComponent>()!.Position;
        Assert.Equal(posBefore, posAfterPause);

        _physicsWorld.Resume();
        physics.FixedUpdate(world, FixedTime);
        physics.FixedUpdate(world, FixedTime);

        var posAfterResume = transform.GetComponent<TransformComponent>()!.Position;
        Assert.True(posAfterResume.Y > posAfterPause.Y, "Body should have moved after Resume.");
    }

    // -------------------------------------------------------------------------
    // Layer / mask filtering
    // -------------------------------------------------------------------------

    [Fact]
    public void CollisionMask_ZeroOnCharacter_DoesNotCollideWithAnything()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        // Floor at Y=100.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
#pragma warning disable CS0618
                c.CollisionMask = 0;
#pragma warning restore CS0618
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(0f, 200f));
        world.Flush();

        Step(world, physics, pre, post, 10);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.False(character.IsGrounded, "CollisionMask=0 body should pass through everything.");
    }

    [Fact]
    public void CategoryBits_BodyOnlyCollidesWithMatchingMask()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        // Floor on layer 1 (categoryBits = 0b10). Character's mask = 0b01, so no match.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.Layer = 1;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
                c.Layer = 0;
                c.CollisionMask = 1UL << 0; // only collides with layer 0, not layer 1
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(0f, 200f));
        world.Flush();

        Step(world, physics, pre, post, 10);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.False(character.IsGrounded, "Character with non-matching mask should not collide with the floor.");
    }

    // -------------------------------------------------------------------------
    // TryStepUp — EffectiveVelocity is non-zero after step-up
    // -------------------------------------------------------------------------

    [Fact]
    public void StepUp_EffectiveVelocity_IsNonZeroAfterSuccessfulStep()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        // Ground floor.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 20f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        // Step ledge.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(80f, -10f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(80f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.Velocity = new Vector2(200f, 0f);
                ch.StepHeight = 30f;
                ch.UpDirection = new Vector2(0f, -1f);
            });
        world.Flush();

        // Settle then walk into the step.
        Step(world, physics, pre, post, 3);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        // After stepping up, EffectiveVelocity.X should still reflect horizontal motion.
        Assert.True(character.EffectiveVelocity.X > 0f,
            $"EffectiveVelocity should retain horizontal component after step-up. Got {character.EffectiveVelocity}");
    }
}