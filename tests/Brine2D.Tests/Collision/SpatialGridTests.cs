using System.Numerics;
using Brine2D.Collision;
using Brine2D.Core;
using Brine2D.ECS;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Collision;

public class SpatialGridTests : TestBase
{
    private Entity CreateEntity(string name = "Test")
    {
        var world = CreateTestWorld();
        return world.CreateEntity(name);
    }

    private static Rectangle MakeBounds(float x, float y, float w, float h)
        => new(x - w * 0.5f, y - h * 0.5f, w, h);

    [Fact]
    public void Insert_And_QueryArea_ReturnsNearbyEntities()
    {
        var grid = new SpatialGrid(100);
        var entity1 = CreateEntity("E1");
        var entity2 = CreateEntity("E2");

        grid.Insert(entity1, MakeBounds(0, 0, 10, 10));
        grid.Insert(entity2, MakeBounds(50, 50, 10, 10));

        var results = new HashSet<Entity>();
        grid.QueryArea(new Rectangle(-10, -10, 120, 120), results);

        results.Should().Contain(entity1);
        results.Should().Contain(entity2);
    }

    [Fact]
    public void QueryArea_DoesNotReturnDistantEntities()
    {
        var grid = new SpatialGrid(100);
        var entity1 = CreateEntity("E1");
        var entity2 = CreateEntity("E2");

        grid.Insert(entity1, MakeBounds(0, 0, 10, 10));
        grid.Insert(entity2, MakeBounds(500, 500, 10, 10));

        var results = new HashSet<Entity>();
        grid.QueryArea(new Rectangle(-20, -20, 40, 40), results);

        results.Should().Contain(entity1);
        results.Should().NotContain(entity2);
    }

    [Fact]
    public void Clear_RemovesAllEntities()
    {
        var grid = new SpatialGrid(100);
        var entity1 = CreateEntity("E1");
        var entity2 = CreateEntity("E2");

        grid.Insert(entity1, MakeBounds(0, 0, 10, 10));
        grid.Insert(entity2, MakeBounds(50, 50, 10, 10));
        grid.Clear();

        var results = new HashSet<Entity>();
        grid.QueryArea(new Rectangle(-100, -100, 500, 500), results);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Insert_EntitySpanningMultipleCells_FoundFromEachCell()
    {
        var grid = new SpatialGrid(50);
        var largeEntity = CreateEntity("Large");
        var queryEntity = CreateEntity("Query");

        grid.Insert(largeEntity, MakeBounds(0, 0, 120, 120));
        grid.Insert(queryEntity, MakeBounds(55, 55, 10, 10));

        var results = new HashSet<Entity>();
        grid.QueryArea(new Rectangle(50, 50, 20, 20), results);

        results.Should().Contain(largeEntity);
    }

    [Fact]
    public void QueryRay_ReturnsEntitiesAlongRay()
    {
        var grid = new SpatialGrid(100);
        var entity = CreateEntity("Target");
        grid.Insert(entity, MakeBounds(50, 0, 10, 10));

        var results = new HashSet<Entity>();
        grid.QueryRay(new Vector2(0, 0), new Vector2(1, 0), 200f, results);

        results.Should().Contain(entity);
    }

    [Fact]
    public void QueryRay_DoesNotReturnEntitiesOffRay()
    {
        var grid = new SpatialGrid(100);
        var entity = CreateEntity("Far");
        grid.Insert(entity, MakeBounds(0, 500, 10, 10));

        var results = new HashSet<Entity>();
        grid.QueryRay(new Vector2(0, 0), new Vector2(1, 0), 200f, results);

        results.Should().NotContain(entity);
    }

    [Fact]
    public void QueryRay_RespectsMaxDistance()
    {
        var grid = new SpatialGrid(100);
        var entity = CreateEntity("Distant");
        grid.Insert(entity, MakeBounds(500, 0, 10, 10));

        var results = new HashSet<Entity>();
        grid.QueryRay(new Vector2(0, 0), new Vector2(1, 0), 50f, results);

        results.Should().NotContain(entity);
    }

    [Fact]
    public void Clear_MultipleTimes_DoesNotThrow()
    {
        var grid = new SpatialGrid(100);
        var entity = CreateEntity("E");
        var bounds = MakeBounds(0, 0, 10, 10);

        for (int i = 0; i < 700; i++)
        {
            grid.Insert(entity, bounds);
            grid.Clear();
        }

        var results = new HashSet<Entity>();
        grid.QueryArea(new Rectangle(-100, -100, 500, 500), results);
        results.Should().BeEmpty();
    }

    [Fact]
    public void QueryPoint_ReturnsEntityContainingPoint()
    {
        var grid = new SpatialGrid(100);
        var entity = CreateEntity("E");
        grid.Insert(entity, MakeBounds(50, 50, 20, 20));

        var results = new HashSet<Entity>();
        grid.QueryPoint(new Vector2(50, 50), results);

        results.Should().Contain(entity);
    }

    [Fact]
    public void QueryPoint_DoesNotReturnEntityNotContainingPoint()
    {
        var grid = new SpatialGrid(100);
        var entity = CreateEntity("E");
        grid.Insert(entity, MakeBounds(50, 50, 10, 10));

        var results = new HashSet<Entity>();
        grid.QueryPoint(new Vector2(200, 200), results);

        results.Should().NotContain(entity);
    }
}