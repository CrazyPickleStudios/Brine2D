using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;
using Brine2D.Physics;

namespace Brine2D.Tests.ECS.Components.Joints;

public class DistanceJointComponentTests : TestBase
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<DistanceJointComponent>();
        var joint = entity.GetComponent<DistanceJointComponent>()!;

        Assert.Equal(0f, joint.Length);
        Assert.Equal(0f, joint.MinLength);
        Assert.Equal(0f, joint.MaxLength);
        Assert.Equal(0f, joint.Hertz);
        Assert.Equal(0f, joint.DampingRatio);
        Assert.Null(joint.ConnectedBody);
        Assert.Equal(Vector2.Zero, joint.LocalAnchorA);
        Assert.Equal(Vector2.Zero, joint.LocalAnchorB);
        Assert.False(joint.CollideConnected);
    }

    [Fact]
    public void Length_SetAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<DistanceJointComponent>();
        var joint = entity.GetComponent<DistanceJointComponent>()!;

        joint.Length = 150f;

        Assert.Equal(150f, joint.Length);
    }

    [Fact]
    public void Length_Negative_IsAccepted()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<DistanceJointComponent>();
        var joint = entity.GetComponent<DistanceJointComponent>()!;

        joint.Length = -1f;

        Assert.Equal(-1f, joint.Length);
    }

    [Fact]
    public void Hertz_Negative_IsAccepted()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<DistanceJointComponent>();
        var joint = entity.GetComponent<DistanceJointComponent>()!;

        joint.Hertz = -1f;

        Assert.Equal(-1f, joint.Hertz);
    }

    [Fact]
    public void DampingRatio_Negative_IsAccepted()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<DistanceJointComponent>();
        var joint = entity.GetComponent<DistanceJointComponent>()!;

        joint.DampingRatio = -1f;

        Assert.Equal(-1f, joint.DampingRatio);
    }

    [Fact]
    public void ConnectedBody_SetAndGet()
    {
        var world = CreateTestWorld();
        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f))
            .AddComponent<DistanceJointComponent>();
        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));

        var joint = entityA.GetComponent<DistanceJointComponent>()!;
        var colliderB = entityB.GetComponent<PhysicsBodyComponent>()!;

        joint.ConnectedBody = colliderB;

        Assert.Same(colliderB, joint.ConnectedBody);
    }

    [Fact]
    public void LocalAnchors_SetAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<DistanceJointComponent>();
        var joint = entity.GetComponent<DistanceJointComponent>()!;

        joint.LocalAnchorA = new Vector2(5f, 10f);
        joint.LocalAnchorB = new Vector2(-5f, -10f);

        Assert.Equal(new Vector2(5f, 10f), joint.LocalAnchorA);
        Assert.Equal(new Vector2(-5f, -10f), joint.LocalAnchorB);
    }

    [Fact]
    public void CollideConnected_SetAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<DistanceJointComponent>();
        var joint = entity.GetComponent<DistanceJointComponent>()!;

        joint.CollideConnected = true;

        Assert.True(joint.CollideConnected);
    }

    [Fact]
    public void PropertyChange_MarksDirty()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<DistanceJointComponent>();
        var joint = entity.GetComponent<DistanceJointComponent>()!;

        joint.IsDirty = false;
        joint.Length = 100f;

        Assert.True(joint.IsDirty);
    }

    [Fact]
    public void OnRemoved_ClearsConnectedBody()
    {
        var world = CreateTestWorld();
        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f))
            .AddComponent<DistanceJointComponent>();
        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));

        var joint = entityA.GetComponent<DistanceJointComponent>()!;
        joint.ConnectedBody = entityB.GetComponent<PhysicsBodyComponent>();

        entityA.RemoveComponent<DistanceJointComponent>();

        Assert.Null(joint.ConnectedBody);
    }
}