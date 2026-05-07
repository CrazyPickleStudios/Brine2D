using Brine2D.Collision;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;
using Microsoft.Extensions.Logging.Abstractions;
using System.Numerics;

namespace Brine2D.Tests.Systems.Physics;

[Collection("Physics")]
public class KinematicCharacterSystemTests : TestBase, IDisposable
{
    private static readonly GameTime FixedTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));
    private readonly PhysicsWorld _physicsWorld = new(new Vector2(0f, 0f));

    public void Dispose() => _physicsWorld.Dispose();

    private (IEntityWorld world, Box2DPhysicsSystem physics, KinematicCharacterSystem preStep, KinematicCharacterSystem postStep) CreateSystems()
    {
        var world = CreateTestWorld();
        var physics = new Box2DPhysicsSystem(_physicsWorld);
        var pre = new KinematicCharacterSystem(_physicsWorld, isPostStep: false, NullLogger<KinematicCharacterSystem>.Instance);
        var post = new KinematicCharacterSystem(_physicsWorld, isPostStep: true, NullLogger<KinematicCharacterSystem>.Instance);
        return (world, physics, pre, post);
    }

    private void Step(IEntityWorld world, Box2DPhysicsSystem physics, KinematicCharacterSystem pre, KinematicCharacterSystem post, int steps = 1)
    {
        for (int i = 0; i < steps; i++)
        {
            pre.FixedUpdate(world, FixedTime);
            physics.FixedUpdate(world, FixedTime);
            post.FixedUpdate(world, FixedTime);
        }
    }

    // ── MoveAndSlide: open space movement ────────────────────────────────────

    [Fact]
    public void MoveAndSlide_NoObstacle_TranslatesFullDistance()
    {
        var (world, physics, pre, post) = CreateSystems();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(300f, 0f));

        world.Flush();
        Step(world, physics, pre, post);

        var transform = entity.GetComponent<TransformComponent>()!;
        float dt = (float)FixedTime.DeltaTime;
        Assert.True(transform.Position.X > 0f, $"Expected X > 0, got {transform.Position.X}");
        Assert.Equal(300f * dt, transform.Position.X, precision: 1);
    }

    // ── MoveAndSlide: wall slide ──────────────────────────────────────────────

    [Fact]
    public void MoveAndSlide_HitVerticalWall_SlidesAlongWall()
    {
        var (world, physics, pre, post) = CreateSystems();

        // Wall at x=200
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(5000f, 100f));

        world.Flush();
        Step(world, physics, pre, post, steps: 5);

        var transform = character.GetComponent<TransformComponent>()!;
        // Should have slid: Y changed but X stayed below the wall
        Assert.True(transform.Position.X < 200f, $"Character passed through wall, X={transform.Position.X}");
        Assert.True(transform.Position.Y != 0f, "Expected Y slide motion");
    }

    // ── MoveAndCollide ────────────────────────────────────────────────────────

    [Fact]
    public void MoveAndCollide_HitsWall_StopsAtContact()
    {
        var (world, physics, pre, post) = CreateSystems();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>();

        world.Flush();
        Step(world, physics, pre, post);

        var charBody = character.GetComponent<KinematicCharacterBody>()!;
        charBody.MoveAndCollide(new Vector2(500f, 0f));
        Step(world, physics, pre, post);

        var transform = character.GetComponent<TransformComponent>()!;
        Assert.True(transform.Position.X < 100f, $"Expected stop before wall, X={transform.Position.X}");
        Assert.NotNull(charBody.LastMoveAndCollideHit);
    }

    [Fact]
    public void MoveAndCollide_NoObstacle_FullMotionApplied()
    {
        var (world, physics, pre, post) = CreateSystems();

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>();

        world.Flush();
        Step(world, physics, pre, post);

        var charBody = character.GetComponent<KinematicCharacterBody>()!;
        charBody.MoveAndCollide(new Vector2(50f, 0f));
        Step(world, physics, pre, post);

        var transform = character.GetComponent<TransformComponent>()!;
        Assert.Equal(50f, transform.Position.X, precision: 1);
        Assert.Null(charBody.LastMoveAndCollideHit);
    }

    [Fact]
    public void MoveAndCollide_MotionRemainder_SetOnPartialMove()
    {
        var (world, physics, pre, post) = CreateSystems();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>();

        world.Flush();
        Step(world, physics, pre, post);

        var charBody = character.GetComponent<KinematicCharacterBody>()!;
        charBody.MoveAndCollide(new Vector2(200f, 0f));
        Step(world, physics, pre, post);

        Assert.True(charBody.MotionRemainder.X > 0f, $"Expected remainder > 0, got {charBody.MotionRemainder.X}");
    }

    // ── Post-step grounding ───────────────────────────────────────────────────

    [Fact]
    public void PostStep_CharacterRestingOnFloor_IsGroundedTrue()
    {
        var (world, physics, pre, post) = CreateSystems();

        // Floor at y=100
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 60f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>();

        world.Flush();
        Step(world, physics, pre, post, steps: 3);

        var charBody = character.GetComponent<KinematicCharacterBody>()!;
        Assert.True(charBody.IsGrounded, "Character should be grounded after landing on floor");
    }

    [Fact]
    public void PostStep_CharacterInAir_IsGroundedFalse()
    {
        var (world, physics, pre, post) = CreateSystems();

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>();

        world.Flush();
        Step(world, physics, pre, post);

        var charBody = character.GetComponent<KinematicCharacterBody>()!;
        Assert.False(charBody.IsGrounded);
    }

    [Fact]
    public void PostStep_CharacterTouchingWall_IsOnWallTrue()
    {
        var (world, physics, pre, post) = CreateSystems();

        // Wall to the right
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(5000f, 0f));

        world.Flush();
        Step(world, physics, pre, post, steps: 5);

        var charBody = character.GetComponent<KinematicCharacterBody>()!;
        Assert.True(charBody.IsOnWall, "Character should detect wall contact");
    }

    [Fact]
    public void PostStep_CharacterTouchingCeiling_IsOnCeilingTrue()
    {
        var (world, physics, pre, post) = CreateSystems();

        // Ceiling directly above at y=-50
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, -50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(0f, -5000f));

        world.Flush();
        Step(world, physics, pre, post, steps: 5);

        var charBody = character.GetComponent<KinematicCharacterBody>()!;
        Assert.True(charBody.IsOnCeiling, "Character should detect ceiling contact");
    }

    // ── OnLanded / OnAirborne events ──────────────────────────────────────────

    [Fact]
    public void OnLanded_TransitionFromAirToGround_Fires()
    {
        var (world, physics, pre, post) = CreateSystems();
        bool landed = false;

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 60f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.OnLanded += _ => landed = true);

        world.Flush();
        Step(world, physics, pre, post, steps: 5);

        Assert.True(landed, "OnLanded should fire when character reaches the floor");
    }

    [Fact]
    public void OnAirborne_TransitionFromGroundToAir_Fires()
    {
        var (world, physics, pre, post) = CreateSystems();
        bool airborne = false;

        // Floor
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 60f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.OnAirborne += _ => airborne = true);

        world.Flush();
        Step(world, physics, pre, post, steps: 5);

        var charBody = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(charBody.IsGrounded);

        // Move character away from floor
        var transform = charEntity.GetComponent<TransformComponent>()!;
        transform.Position = new Vector2(0f, -500f);
        charBody.Velocity = Vector2.Zero;

        Step(world, physics, pre, post, steps: 3);

        Assert.True(airborne, "OnAirborne should fire when character leaves the floor");
    }

    // ── Snap to floor ─────────────────────────────────────────────────────────

    [Fact]
    public void SnapDistance_CharacterJustLeavingFloor_SnapsBack()
    {
        var (world, physics, pre, post) = CreateSystems();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 10f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 60f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.SnapDistance = 30f;
            });

        world.Flush();
        Step(world, physics, pre, post, steps: 3);

        var charBody = character.GetComponent<KinematicCharacterBody>()!;
        Assert.True(charBody.IsGrounded, "Character should have snapped to floor");
    }

    // ── MaxSpeed ──────────────────────────────────────────────────────────────

    [Fact]
    public void MaxSpeed_VelocityAboveCap_ClampsEffectiveVelocity()
    {
        var (world, physics, pre, post) = CreateSystems();

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.Velocity = new Vector2(5000f, 0f);
                ch.MaxSpeed = 100f;
            });

        world.Flush();
        Step(world, physics, pre, post);

        var charBody = world.Entities
            .Select(e => e.GetComponent<KinematicCharacterBody>())
            .First(b => b != null)!;

        Assert.True(charBody.EffectiveVelocity.Length() <= 100f + 0.01f,
            $"Expected effective speed <= 100, got {charBody.EffectiveVelocity.Length()}");
    }

    // ── StopOnSlope ───────────────────────────────────────────────────────────

    [Fact]
    public void StopOnSlope_CharacterOnSlope_DoesNotSlideWhenVelocityZero()
    {
        var (world, physics, pre, post) = CreateSystems();

        // 45° slope surface
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new PolygonShape([
                    new Vector2(-200f, 0f),
                    new Vector2(200f, 0f),
                    new Vector2(200f, 200f),
                    new Vector2(-200f, 200f)
                ]);
                c.BodyType = PhysicsBodyType.Static;
            });

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 60f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(18f, 18f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.StopOnSlope = true;
                ch.Velocity = Vector2.Zero;
            });

        world.Flush();
        Step(world, physics, pre, post, steps: 5);

        var transform = character.GetComponent<TransformComponent>()!;
        float yAfterSettle = transform.Position.Y;

        // With zero velocity and StopOnSlope, Y should not drift further down
        Step(world, physics, pre, post, steps: 3);

        Assert.Equal(yAfterSettle, transform.Position.Y, precision: 1);
    }

    // ── EffectiveVelocity ─────────────────────────────────────────────────────

    [Fact]
    public void EffectiveVelocity_OpenSpace_MatchesInputVelocity()
    {
        var (world, physics, pre, post) = CreateSystems();

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(200f, 0f));

        world.Flush();
        Step(world, physics, pre, post);

        var charBody = world.Entities
            .Select(e => e.GetComponent<KinematicCharacterBody>())
            .First(b => b != null)!;

        Assert.Equal(200f, charBody.EffectiveVelocity.X, precision: 1);
    }

    // ── Platform riding ───────────────────────────────────────────────────────

    [Fact]
    public void PlatformVelocity_MovingKinematicPlatform_CharacterRides()
    {
        var (world, physics, pre, post) = CreateSystems();

        var platformEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(200f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            });

        var character = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 60f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>();

        world.Flush();

        // Let the character land on the platform first
        Step(world, physics, pre, post, steps: 5);

        var charBody = character.GetComponent<KinematicCharacterBody>()!;
        Assert.True(charBody.IsGrounded, "Precondition: character must be grounded before riding test");

        // Now move the platform laterally
        var platformTransform = platformEntity.GetComponent<TransformComponent>()!;
        float xBefore = character.GetComponent<TransformComponent>()!.Position.X;

        for (int i = 0; i < 5; i++)
        {
            platformTransform.Position += new Vector2(10f, 0f);
            Step(world, physics, pre, post);
        }

        float xAfter = character.GetComponent<TransformComponent>()!.Position.X;
        Assert.True(xAfter > xBefore, $"Character should have moved with platform, X before={xBefore} after={xAfter}");
    }
}