using Brine2D.ECS;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.ECS;

public class EntityWorldTests
{
    [Fact]
    public void ShouldCreateEntityAndAddToWorld()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity("TestEntity");

        world.Entities.Should().Contain(entity);
        entity.Name.Should().Be("TestEntity");
        entity.World.Should().Be(world);
    }

    [Fact]
    public void ShouldDestroyEntityAndRemoveFromWorld()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        world.Entities.Should().Contain(entity);

        world.DestroyEntity(entity);

        world.Entities.Should().NotContain(entity);
    }

    [Fact]
    public void ShouldQueryEntityById()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var found = world.GetEntityById(entity.Id);

        found.Should().Be(entity);
    }

    [Fact]
    public void ShouldQueryEntityByName()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity("Hero");
        var found = world.GetEntityByName("Hero");

        found.Should().Be(entity);
    }

    [Fact]
    public void ShouldQueryEntitiesByTag()
    {
        var world = new EntityWorld();
        var entity1 = world.CreateEntity();
        entity1.Tags.Add("enemy");
        var entity2 = world.CreateEntity();
        entity2.Tags.Add("enemy");
        var entity3 = world.CreateEntity();
        entity3.Tags.Add("player");

        var enemies = world.GetEntitiesByTag("enemy");

        enemies.Should().Contain(entity1);
        enemies.Should().Contain(entity2);
        enemies.Should().NotContain(entity3);
    }

    [Fact]
    public void ShouldQueryEntitiesByComponent()
    {
        var world = new EntityWorld();
        var entity1 = world.CreateEntity();
        entity1.AddComponent(new TestComponent());
        var entity2 = world.CreateEntity();
        entity2.AddComponent(new TestComponent());
        var entity3 = world.CreateEntity();

        var withTestComponent = world.GetEntitiesWithComponent<TestComponent>();

        withTestComponent.Should().Contain(entity1);
        withTestComponent.Should().Contain(entity2);
        withTestComponent.Should().NotContain(entity3);
    }

    [Fact]
    public void ShouldFireOnEntityCreatedEvent()
    {
        var world = new EntityWorld();
        Entity? created = null;
        world.OnEntityCreated += e => created = e;

        var entity = world.CreateEntity();

        created.Should().Be(entity);
    }

    [Fact]
    public void ShouldFireOnEntityDestroyedEvent()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        Entity? destroyed = null;
        world.OnEntityDestroyed += e => destroyed = e;

        world.DestroyEntity(entity);

        destroyed.Should().Be(entity);
    }

    [Fact]
    public void ShouldClearAllEntities()
    {
        var world = new EntityWorld();
        world.CreateEntity();
        world.CreateEntity();
        world.Entities.Count.Should().Be(2);

        world.Clear();

        world.Entities.Should().BeEmpty();
    }

    public class TestComponent : Component { }
}