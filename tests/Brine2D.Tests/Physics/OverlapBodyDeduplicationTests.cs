using System.Numerics;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;
using Brine2D.Core;

namespace Brine2D.Tests.Physics;

[Collection("Physics")]
public class OverlapBodyDeduplicatonTests : TestBase, IDisposable
{
    private static readonly GameTime FixedTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    private readonly PhysicsWorld _physicsWorld = new(new Vector2(0f, 0f), 100f);

    public void Dispose() => _physicsWorld.Dispose();

    [Fact]
    public void OverlapBodyAll_MultipleOverlappingBodies_ReturnsDistinctResults()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        // Query body: static circle at origin
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.BodyType = PhysicsBodyType.Static;
                c.Shape = new CircleShape(10f);
            });

        // Three overlapping static targets near origin
        for (int i = 0; i < 3; i++)
        {
            world.CreateEntity()
                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(i * 2f, 0f))
                .AddComponent<PhysicsBodyComponent>(c =>
                {
                    c.BodyType = PhysicsBodyType.Static;
                    c.Shape = new CircleShape(10f);
                });
        }

        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var queryBody = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        var buffer = new OverlapHit[8];
        int count = _physicsWorld.OverlapBody(queryBody, buffer, out bool wasTruncated);

        Assert.False(wasTruncated);
        Assert.Equal(3, count);

        // All results must be distinct bodies
        var bodyIds = buffer[..count].Select(h => h.Component).Distinct().ToList();
        Assert.Equal(3, bodyIds.Count);
    }

    [Fact]
    public void OverlapBodyAll_CompoundQueryBody_DeduplicatesTargetsAcrossShapes()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        // Compound query body with two overlapping sub-shapes
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.BodyType = PhysicsBodyType.Static;
                c.Shape = new CircleShape(30f);
                c.AddSubShape(new CircleShape(30f));
            });

        // Two target bodies both overlapping the query body
        for (int i = 0; i < 2; i++)
        {
            world.CreateEntity()
                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(i * 5f, 0f))
                .AddComponent<PhysicsBodyComponent>(c =>
                {
                    c.BodyType = PhysicsBodyType.Static;
                    c.Shape = new CircleShape(20f);
                });
        }

        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var queryBody = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        var buffer = new OverlapHit[8];
        int count = _physicsWorld.OverlapBody(queryBody, buffer, out bool wasTruncated);

        Assert.False(wasTruncated);

        // Despite query body having 2 shapes, each target should appear only once
        var distinct = buffer[..count].Select(h => h.Component).Distinct().ToList();
        Assert.Equal(count, distinct.Count);
        Assert.Equal(2, count);
    }

    [Fact]
    public void OverlapBodyAll_BufferSmallerThanOverlapCount_SetsTruncated()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.BodyType = PhysicsBodyType.Static;
                c.Shape = new CircleShape(10f);
            });

        for (int i = 0; i < 4; i++)
        {
            world.CreateEntity()
                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(i * 2f, 0f))
                .AddComponent<PhysicsBodyComponent>(c =>
                {
                    c.BodyType = PhysicsBodyType.Static;
                    c.Shape = new CircleShape(10f);
                });
        }

        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var queryBody = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        var buffer = new OverlapHit[2]; // intentionally smaller than 4
        int count = _physicsWorld.OverlapBody(queryBody, buffer, out bool wasTruncated);

        Assert.True(wasTruncated);
        Assert.Equal(2, count);

        var distinct = buffer[..count].Select(h => h.Component).Distinct().ToList();
        Assert.Equal(2, distinct.Count);
    }

    [Fact]
    public void OverlapBodyAll_ExactBufferSize_NoTruncation()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.BodyType = PhysicsBodyType.Static;
                c.Shape = new CircleShape(10f);
            });

        for (int i = 0; i < 3; i++)
        {
            world.CreateEntity()
                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(i * 2f, 0f))
                .AddComponent<PhysicsBodyComponent>(c =>
                {
                    c.BodyType = PhysicsBodyType.Static;
                    c.Shape = new CircleShape(10f);
                });
        }

        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var queryBody = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        var buffer = new OverlapHit[3]; // exactly matches target count
        int count = _physicsWorld.OverlapBody(queryBody, buffer, out bool wasTruncated);

        Assert.False(wasTruncated);
        Assert.Equal(3, count);
    }
}