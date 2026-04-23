using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;

namespace Brine2D.Tests.ECS.Components.Joints;

public class RevoluteJointComponentTests : TestBase
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<RevoluteJointComponent>();
        var joint = entity.GetComponent<RevoluteJointComponent>()!;

        Assert.Equal(0f, joint.ReferenceAngle);
        Assert.False(joint.EnableLimit);
        Assert.Equal(0f, joint.LowerAngle);
        Assert.Equal(0f, joint.UpperAngle);
        Assert.False(joint.EnableMotor);
        Assert.Equal(0f, joint.MotorSpeed);
        Assert.Equal(0f, joint.MaxMotorTorque);
        Assert.Equal(0f, joint.HertzFrequency);
        Assert.Equal(0f, joint.DampingRatio);
    }

    [Fact]
    public void EnableLimit_SetAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<RevoluteJointComponent>();
        var joint = entity.GetComponent<RevoluteJointComponent>()!;

        joint.EnableLimit = true;
        joint.LowerAngle = -MathF.PI / 4f;
        joint.UpperAngle = MathF.PI / 4f;

        Assert.True(joint.EnableLimit);
        Assert.Equal(-MathF.PI / 4f, joint.LowerAngle);
        Assert.Equal(MathF.PI / 4f, joint.UpperAngle);
    }

    [Fact]
    public void EnableMotor_SetAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<RevoluteJointComponent>();
        var joint = entity.GetComponent<RevoluteJointComponent>()!;

        joint.EnableMotor = true;
        joint.MotorSpeed = 5f;
        joint.MaxMotorTorque = 100f;

        Assert.True(joint.EnableMotor);
        Assert.Equal(5f, joint.MotorSpeed);
        Assert.Equal(100f, joint.MaxMotorTorque);
    }

    [Fact]
    public void MaxMotorTorque_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<RevoluteJointComponent>();
        var joint = entity.GetComponent<RevoluteJointComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => joint.MaxMotorTorque = -1f);
    }

    [Fact]
    public void HertzFrequency_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<RevoluteJointComponent>();
        var joint = entity.GetComponent<RevoluteJointComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => joint.HertzFrequency = -1f);
    }

    [Fact]
    public void DampingRatio_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<RevoluteJointComponent>();
        var joint = entity.GetComponent<RevoluteJointComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => joint.DampingRatio = -1f);
    }

    [Fact]
    public void PropertyChange_MarksDirty()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<RevoluteJointComponent>();
        var joint = entity.GetComponent<RevoluteJointComponent>()!;

        joint.IsDirty = false;
        joint.EnableMotor = true;

        Assert.True(joint.IsDirty);
    }
}