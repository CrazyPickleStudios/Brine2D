using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;

namespace Brine2D.Tests.ECS.Components.Joints;

public class WeldJointComponentTests : TestBase
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<WeldJointComponent>();
        var joint = entity.GetComponent<WeldJointComponent>()!;

        Assert.Equal(0f, joint.ReferenceAngle);
        Assert.Equal(0f, joint.AngularHertz);
        Assert.Equal(0f, joint.AngularDampingRatio);
        Assert.Equal(0f, joint.LinearHertz);
        Assert.Equal(0f, joint.LinearDampingRatio);
    }

    [Fact]
    public void AngularHertz_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<WeldJointComponent>();
        var joint = entity.GetComponent<WeldJointComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => joint.AngularHertz = -1f);
    }

    [Fact]
    public void AngularDampingRatio_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<WeldJointComponent>();
        var joint = entity.GetComponent<WeldJointComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => joint.AngularDampingRatio = -1f);
    }

    [Fact]
    public void LinearHertz_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<WeldJointComponent>();
        var joint = entity.GetComponent<WeldJointComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => joint.LinearHertz = -1f);
    }

    [Fact]
    public void LinearDampingRatio_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<WeldJointComponent>();
        var joint = entity.GetComponent<WeldJointComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => joint.LinearDampingRatio = -1f);
    }

    [Fact]
    public void ReferenceAngle_SetAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<WeldJointComponent>();
        var joint = entity.GetComponent<WeldJointComponent>()!;

        joint.ReferenceAngle = MathF.PI / 2f;

        Assert.Equal(MathF.PI / 2f, joint.ReferenceAngle);
    }

    [Fact]
    public void SoftWeld_Configuration()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<WeldJointComponent>();
        var joint = entity.GetComponent<WeldJointComponent>()!;

        joint.AngularHertz = 5f;
        joint.AngularDampingRatio = 0.7f;
        joint.LinearHertz = 10f;
        joint.LinearDampingRatio = 0.9f;

        Assert.Equal(5f, joint.AngularHertz);
        Assert.Equal(0.7f, joint.AngularDampingRatio);
        Assert.Equal(10f, joint.LinearHertz);
        Assert.Equal(0.9f, joint.LinearDampingRatio);
    }

    [Fact]
    public void PropertyChange_MarksDirty()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<WeldJointComponent>();
        var joint = entity.GetComponent<WeldJointComponent>()!;

        joint.IsDirty = false;
        joint.AngularHertz = 5f;

        Assert.True(joint.IsDirty);
    }

    [Fact]
    public void OnRemoved_ClearsState()
    {
        var world = CreateTestWorld();
        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.SetCircle(10f))
            .AddComponent<WeldJointComponent>();
        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.SetCircle(10f));

        var joint = entityA.GetComponent<WeldJointComponent>()!;
        joint.ConnectedBody = entityB.GetComponent<PhysicsBodyComponent>();

        entityA.RemoveComponent<WeldJointComponent>();

        Assert.Null(joint.ConnectedBody);
        Assert.True(joint.IsDirty);
    }
}