using Brine2D.Core;
using Brine2D.ECS;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.ECS;

public class EntityWorldTests : TestBase
{
    [Fact]
    public void ShouldCreateEntityAndAddToWorld()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("TestEntity");
        world.Flush();

        world.Entities.Should().Contain(entity);
        entity.Name.Should().Be("TestEntity");
        entity.World.Should().Be(world);
    }

    [Fact]
    public void ShouldDestroyEntityAndRemoveFromWorld()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        world.Flush();
        
        world.Entities.Should().Contain(entity);

        world.DestroyEntity(entity);
        world.Flush();

        world.Entities.Should().NotContain(entity);
    }

    [Fact]
    public void ShouldQueryEntityById()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        world.Flush();
        
        var found = world.GetEntityById(entity.Id);

        found.Should().Be(entity);
    }

    [Fact]
    public void ShouldQueryEntityByName()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Hero");
        world.Flush();
        
        var found = world.GetEntityByName("Hero");

        found.Should().Be(entity);
    }

    [Fact]
    public void ShouldQueryEntitiesByTag()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity();
        entity1.Tags.Add("enemy");
        var entity2 = world.CreateEntity();
        entity2.Tags.Add("enemy");
        var entity3 = world.CreateEntity();
        entity3.Tags.Add("player");
        world.Flush();

        var enemies = world.GetEntitiesByTag("enemy");

        enemies.Should().Contain(entity1);
        enemies.Should().Contain(entity2);
        enemies.Should().NotContain(entity3);
    }

    [Fact]
    public void ShouldQueryEntitiesByComponent()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity();
        entity1.AddComponent(new TestComponent());
        var entity2 = world.CreateEntity();
        entity2.AddComponent(new TestComponent());
        var entity3 = world.CreateEntity();
        world.Flush();

        var withTestComponent = world.GetEntitiesWithComponent<TestComponent>();

        withTestComponent.Should().Contain(entity1);
        withTestComponent.Should().Contain(entity2);
        withTestComponent.Should().NotContain(entity3);
    }
    
    [Fact]
    public void ShouldClearAllEntities()
    {
        var world = CreateTestWorld();
        world.CreateEntity();
        world.CreateEntity();
        world.Flush();
        
        world.Entities.Count.Should().Be(2);

        world.Clear();

        world.Entities.Should().BeEmpty();
    }

    public class TestComponent : Component { }
}