using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brine2D.Tests.Systems.Physics;

public class KinematicCharacterSystemBehaviourTests : PhysicsTestBase
{
    private (Box2DPhysicsSystem physics, KinematicCharacterSystem pre, KinematicCharacterSystem post) CreateSystems()
    {
        var physics = new Box2DPhysicsSystem(PhysicsWorld);
        var pre = new KinematicCharacterSystem(PhysicsWorld, isPostStep: false, NullLogger<KinematicCharacterSystem>.Instance);
        var post = new KinematicCharacterSystem(PhysicsWorld, isPostStep: true, NullLogger<KinematicCharacterSystem>.Instance);
        return (physics, pre, post);
    }

    private static void Step(IEntityWorld world, Box2DPhysicsSystem physics,
        KinematicCharacterSystem pre, KinematicCharacterSystem post, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            pre.FixedUpdate(world, FixedTime);
            physics.FixedUpdate(world, FixedTime);
            post.FixedUpdate(world, FixedTime);
        }
    }

    // Surface detection

    [Fact]
    public void IsOnCeiling_PushingUpIntoCeiling_IsTrue()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

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

    // Grounding: snap, slope, angle limit

    [Fact]
    public void SnapDistance_KeepsCharacterGrounded_OnSmallGap()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

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

        Step(world, physics, pre, post, 6);
        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsGrounded);

        charEntity.GetComponent<TransformComponent>()!.Position -= new Vector2(0f, 5f);
        character.Velocity = Vector2.Zero;

        Step(world, physics, pre, post, 2);

        Assert.True(character.IsGrounded, "SnapDistance should keep character grounded over a small gap.");
    }

    [Fact]
    public void StopOnSlope_PreventsSlideDownRamp()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

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

        Step(world, physics, pre, post, 8);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsGrounded);

        character.Velocity = Vector2.Zero;
        var xBefore = charEntity.GetComponent<TransformComponent>()!.Position.X;

        Step(world, physics, pre, post, 10);

        var xAfter = charEntity.GetComponent<TransformComponent>()!.Position.X;
        Assert.True(MathF.Abs(xAfter - xBefore) < 2f, $"StopOnSlope should prevent sliding. Δx={xAfter - xBefore}");
    }

    [Fact]
    public void FloorAngleLimit_SteepSurface_NotClassifiedAsFloor()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t =>
            {
                t.LocalPosition = new Vector2(0f, 0f);
                t.Rotation = MathF.PI * 0.45f;
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
                ch.FloorAngleLimit = 0.8f;
            });
        world.Flush();

        Step(world, physics, pre, post, 8);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.False(character.IsGrounded, "A near-vertical surface should not be classified as a floor.");
    }

    // Custom up direction

    [Fact]
    public void UpDirection_Override_DetectsFloorInCustomDirection()
    {
        var world = CreateTestWorld();
        using var customWorld = new PhysicsWorld(new Vector2(980f, 0f));
        var physics = new Box2DPhysicsSystem(customWorld);
        var pre = new KinematicCharacterSystem(customWorld, isPostStep: false, NullLogger<KinematicCharacterSystem>.Instance);
        var post = new KinematicCharacterSystem(customWorld, isPostStep: true, NullLogger<KinematicCharacterSystem>.Instance);

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

    // Push force

    [Fact]
    public void PushForce_MultipleContactsOnSameBody_AppliesOnlyOneImpulse()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

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

        Step(world, physics, pre, post, 10);

        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        Assert.True(dynBody.LinearVelocity.Length() < 50f,
            $"PushForce applied multiple times per step: speed={dynBody.LinearVelocity.Length()}");
    }

    // MoveAndCollide edge cases

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

    // Events: OnLanded, OnAirborne

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

        Step(world, physics, pre, post, 6);
        Assert.Equal(1, landedCount);

        charEntity.GetComponent<KinematicCharacterBody>()!.Velocity = Vector2.Zero;
        Step(world, physics, pre, post, 10);

        Assert.Equal(1, landedCount);
    }

    [Fact]
    public void OnAirborne_FiresOnce_WhenCharacterLeavesGround()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

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
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(0f, 200f));
        world.Flush();

        Step(world, physics, pre, post, 10);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsGrounded);

        int airborneCount = 0;
        character.OnAirborne += _ => airborneCount++;

        character.Velocity = new Vector2(0f, -400f);
        Step(world, physics, pre, post, 3);

        Assert.Equal(1, airborneCount);
    }

    [Fact]
    public void OnAirborne_DoesNotFireAgain_WhileAlreadyAirborne()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 160f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(0f, 200f));
        world.Flush();

        Step(world, physics, pre, post, 15);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsGrounded);

        int airborneCount = 0;
        character.OnAirborne += _ => airborneCount++;

        character.Velocity = new Vector2(0f, -400f);
        Step(world, physics, pre, post, 15);

        Assert.Equal(1, airborneCount);
    }

    // MaxSpeed clamping

    [Fact]
    public void MaxSpeed_ClampedBeforeIntegration_EffectiveVelocityWithinLimit()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        const float maxSpeed = 100f;

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch =>
            {
                ch.Velocity = new Vector2(5000f, 0f);
                ch.MaxSpeed = maxSpeed;
            });
        world.Flush();

        Step(world, physics, pre, post, 3);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.EffectiveVelocity.Length() <= maxSpeed + 0.01f,
            $"EffectiveVelocity {character.EffectiveVelocity.Length()} exceeds MaxSpeed {maxSpeed}.");
    }

    // SlideCollisions deduplication

    [Fact]
    public void SlideCollisions_DynamicFloor_AppearsExactlyOnce()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        var platformEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 60f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 20f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(0f, 200f));
        world.Flush();

        Step(world, physics, pre, post, 10);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        var platform = platformEntity.GetComponent<PhysicsBodyComponent>()!;

        var collisions = character.GetSlideCollisions();
        int platformCount = collisions.Count(c => ReferenceEquals(c.Other, platform));

        Assert.True(platformCount == 1,
            $"The dynamic/kinematic floor should appear exactly once in SlideCollisions. Actual count: {platformCount}");
    }

    // Collision mask / layer filtering

    [Fact]
    public void CollisionMask_ZeroOnCharacter_DoesNotCollideWithAnything()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

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
                c.CollisionMask = 1UL << 0;
            })
            .AddComponent<KinematicCharacterBody>(ch => ch.Velocity = new Vector2(0f, 200f));
        world.Flush();

        Step(world, physics, pre, post, 10);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.False(character.IsGrounded, "Character with non-matching mask should not collide with the floor.");
    }

    // Step-up

    [Fact]
    public void StepUp_EffectiveVelocity_IsNonZeroAfterSuccessfulStep()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 20f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

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

        Step(world, physics, pre, post, 3);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.EffectiveVelocity.X > 0f,
            $"EffectiveVelocity should retain horizontal component after step-up. Got {character.EffectiveVelocity}");
    }
}