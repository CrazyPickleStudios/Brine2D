using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;

namespace Brine2D.Tests.ECS.Components.Joints;

public class PrismaticJointComponentTests : TestBase
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PrismaticJointComponent>();
        var joint = entity.GetComponent<PrismaticJointComponent>()!;

        Assert.Equal(Vector2.UnitX, joint.LocalAxisA);
        Assert.Equal(0f, joint.ReferenceAngle);
        Assert.False(joint.EnableLimit);
        Assert.Equal(0f, joint.LowerTranslation);
        Assert.Equal(0f, joint.UpperTranslation);
        Assert.False(joint.EnableMotor);
        Assert.Equal(0f, joint.MaxMotorForce);
        Assert.Equal(0f, joint.MotorSpeed);
        Assert.Equal(0f, joint.HertzFrequency);
        Assert.Equal(0f, joint.DampingRatio);
    }

    [Fact]
    public void LocalAxisA_Normalizes()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PrismaticJointComponent>();
        var joint = entity.GetComponent<PrismaticJointComponent>()!;

        joint.LocalAxisA = new Vector2(3f, 4f);

        var expected = Vector2.Normalize(new Vector2(3f, 4f));
        Assert.Equal(expected.X, joint.LocalAxisA.X, 0.0001f);
        Assert.Equal(expected.Y, joint.LocalAxisA.Y, 0.0001f);
    }

    [Fact]
    public void EnableLimit_WithTranslationBounds()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PrismaticJointComponent>();
        var joint = entity.GetComponent<PrismaticJointComponent>()!;

        joint.EnableLimit = true;
        joint.LowerTranslation = -50f;
        joint.UpperTranslation = 50f;

        Assert.True(joint.EnableLimit);
        Assert.Equal(-50f, joint.LowerTranslation);
        Assert.Equal(50f, joint.UpperTranslation);
    }

    [Fact]
    public void MaxMotorForce_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PrismaticJointComponent>();
        var joint = entity.GetComponent<PrismaticJointComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => joint.MaxMotorForce = -1f);
    }

    [Fact]
    public void HertzFrequency_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PrismaticJointComponent>();
        var joint = entity.GetComponent<PrismaticJointComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => joint.HertzFrequency = -1f);
    }

    [Fact]
    public void DampingRatio_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PrismaticJointComponent>();
        var joint = entity.GetComponent<PrismaticJointComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => joint.DampingRatio = -1f);
    }

    [Fact]
    public void PropertyChange_MarksDirty()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PrismaticJointComponent>();
        var joint = entity.GetComponent<PrismaticJointComponent>()!;

        joint.IsDirty = false;
        joint.MotorSpeed = 10f;

        Assert.True(joint.IsDirty);
    }
}