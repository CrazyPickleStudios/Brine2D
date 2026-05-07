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
public class KinematicCharacterSystemMoreTests : PhysicsTestBase
{
    public KinematicCharacterSystemMoreTests() : base(gravity: Vector2.Zero) { }

    private (Box2DPhysicsSystem physics, KinematicCharacterSystem pre, KinematicCharacterSystem post) CreateSystems()
    {
        var physics = new Box2DPhysicsSystem(PhysicsWorld);
        var pre = new KinematicCharacterSystem(PhysicsWorld, isPostStep: false, NullLogger<KinematicCharacterSystem>.Instance);
        var post = new KinematicCharacterSystem(PhysicsWorld, isPostStep: true, NullLogger<KinematicCharacterSystem>.Instance);
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
    private static readonly GameTime FixedTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    private readonly PhysicsWorld _physicsWorld = new();

    // -------------------------------------------------------------------------
    // OnAirborne event
    // -------------------------------------------------------------------------

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

        // Land and settle.
        Step(world, physics, pre, post, 10);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsGrounded);

        int airborneCount = 0;
        character.OnAirborne += _ => airborneCount++;

        // Jump upward — character leaves the ground.
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

        // Character bottom at Y=170, floor top at Y=190 — 20px gap, lands in ~6 steps.
        Step(world, physics, pre, post, 15);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        Assert.True(character.IsGrounded);

        int airborneCount = 0;
        character.OnAirborne += _ => airborneCount++;

        character.Velocity = new Vector2(0f, -400f);
        Step(world, physics, pre, post, 15);

        Assert.Equal(1, airborneCount);
    }

    // -------------------------------------------------------------------------
    // MaxSpeed clamping
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // SlideCollisions dedup — dynamic surface must appear exactly once
    // -------------------------------------------------------------------------

    [Fact]
    public void SlideCollisions_DynamicFloor_AppearsExactlyOnce()
    {
        var world = CreateTestWorld();
        var (physics, pre, post) = CreateSystems();

        // A kinematic platform acts as the floor.
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
            $"The dynamic/kinematic floor should appear exactly once in SlideCollisions after the dedup fix. Actual count: {platformCount}");
    }
}