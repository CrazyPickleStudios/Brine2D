using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;

namespace Brine2D.Tests.ECS.Components;

public class VelocityComponentTests : TestBase
{
    [Fact]
    public void Constructor_DefaultValue_IsZero()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Assert
        Assert.Equal(Vector2.Zero, velocity.Value);
    }

    [Fact]
    public void AddComponent_WithConfiguration_ConfiguresInline()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.Value = new Vector2(5f, -2f));

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Assert
        Assert.Equal(new Vector2(5f, -2f), velocity.Value);
    }
}
