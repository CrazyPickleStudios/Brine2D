using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.ECS.Query;

public class EntityQueryTests : TestBase
{
    #region Component Filters

    [Fact]
    public void With_SingleComponent_FiltersCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity().AddComponent<VelocityComponent>();
        var entity3 = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        // Act
        var results = world.Query()
            .With<TransformComponent>()
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void With_MultipleComponents_RequiresAll()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>();
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>();
        var entity3 = world.CreateEntity()
            .AddComponent<VelocityComponent>();
        world.Flush();

        // Act
        var results = world.Query()
            .With<TransformComponent>()
            .With<VelocityComponent>()
            .Execute()
            .ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void With_ComponentFilter_FiltersCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 100f);
        var entity2 = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 200f);
        var entity3 = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 50f);
        world.Flush();

        // Act
        var results = world.Query()
            .With<VelocityComponent>(v => v.MaxSpeed >= 100f)
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
        Assert.DoesNotContain(entity3, results);
    }

    [Fact]
    public void Without_ExcludesEntitiesWithComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>();
        var entity3 = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        // Act
        var results = world.Query()
            .With<TransformComponent>()
            .Without<VelocityComponent>()
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
        Assert.DoesNotContain(entity2, results);
    }

    #endregion

    #region Tag Filters

    [Fact]
    public void WithTag_SingleTag_FiltersCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddTag("Player");
        var entity2 = world.CreateEntity().AddTag("Enemy");
        var entity3 = world.CreateEntity().AddTag("Player");
        world.Flush();

        // Act
        var results = world.Query()
            .WithTag("Player")
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
    }

    [Fact]
    public void WithoutTag_ExcludesEntitiesWithTag()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddTag("Enemy");
        var entity2 = world.CreateEntity().AddTags("Enemy", "Boss");
        var entity3 = world.CreateEntity().AddTag("Enemy");
        world.Flush();

        // Act
        var results = world.Query()
            .WithTag("Enemy")
            .WithoutTag("Boss")
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
    }

    [Fact]
    public void WithAllTags_RequiresAllTags()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddTags("Enemy", "Flying", "Elite");
        var entity2 = world.CreateEntity().AddTags("Enemy", "Flying");
        var entity3 = world.CreateEntity().AddTags("Enemy", "Elite");
        world.Flush();

        // Act
        var results = world.Query()
            .WithAllTags("Enemy", "Flying", "Elite")
            .Execute()
            .ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void WithAnyTag_MatchesAnyTag()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddTag("Enemy");
        var entity2 = world.CreateEntity().AddTag("Boss");
        var entity3 = world.CreateEntity().AddTag("Player");
        world.Flush();

        // Act
        var results = world.Query()
            .WithAnyTag("Enemy", "Boss")
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
    }

    #endregion

    #region Spatial Filters

    [Fact]
    public void WithinRadius_FiltersEntitiesByDistance()
    {
        // Arrange
        var world = CreateTestWorld();
        var center = new Vector2(0, 0);
        
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10, 0)); // Distance: 10
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 0)); // Distance: 100
        var entity3 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(5, 5)); // Distance: ~7
        world.Flush();

        // Act
        var results = world.Query()
            .WithinRadius(center, 50f)
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void WithinRadius_RequiresTransformComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10, 0));
        var entity2 = world.CreateEntity(); // No transform
        world.Flush();

        // Act
        var results = world.Query()
            .WithinRadius(Vector2.Zero, 50f)
            .Execute()
            .ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void WithinBounds_FiltersEntitiesByRectangle()
    {
        // Arrange
        var world = CreateTestWorld();
        var bounds = new Rectangle(0, 0, 100, 100);
        
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50, 50)); // Inside
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(150, 150)); // Outside
        var entity3 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10, 10)); // Inside
        world.Flush();

        // Act
        var results = world.Query()
            .WithinBounds(bounds)
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
    }

    #endregion

    #region Custom Predicates

    [Fact]
    public void Where_CustomPredicate_FiltersCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity("Alpha");
        var entity2 = world.CreateEntity("Beta");
        var entity3 = world.CreateEntity("AlphaTwo");
        world.Flush();

        // Act
        var results = world.Query()
            .Where(e => e.Name.StartsWith("Alpha"))
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
    }

    [Fact]
    public void Where_MultiplePredicates_CombinesWithAnd()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity("Alpha").AddTag("Test");
        var entity2 = world.CreateEntity("Alpha");
        var entity3 = world.CreateEntity("Beta").AddTag("Test");
        world.Flush();

        // Act
        var results = world.Query()
            .Where(e => e.Name.StartsWith("Alpha"))
            .Where(e => e.HasTag("Test"))
            .Execute()
            .ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    #endregion

    #region Ordering

    [Fact]
    public void OrderBy_SortsAscending()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 300f);
        var entity2 = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 100f);
        var entity3 = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 200f);
        world.Flush();

        // Act
        var results = world.Query()
            .With<VelocityComponent>()
            .OrderBy(e => e.GetComponent<VelocityComponent>()!.MaxSpeed)
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(entity2, results[0]); // 100
        Assert.Equal(entity3, results[1]); // 200
        Assert.Equal(entity1, results[2]); // 300
    }

    [Fact]
    public void OrderByDescending_SortsDescending()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 300f);
        var entity2 = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 100f);
        var entity3 = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 200f);
        world.Flush();

        // Act
        var results = world.Query()
            .With<VelocityComponent>()
            .OrderByDescending(e => e.GetComponent<VelocityComponent>()!.MaxSpeed)
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(entity1, results[0]); // 300
        Assert.Equal(entity3, results[1]); // 200
        Assert.Equal(entity2, results[2]); // 100
    }

    [Fact]
    public void ThenBy_AppliesSecondarySort()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity("C")
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 100f);
        var entity2 = world.CreateEntity("A")
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 100f);
        var entity3 = world.CreateEntity("B")
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 100f);
        world.Flush();

        // Act
        var results = world.Query()
            .With<VelocityComponent>()
            .OrderBy(e => e.GetComponent<VelocityComponent>()!.MaxSpeed)
            .ThenBy(e => e.Name)
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("A", results[0].Name);
        Assert.Equal("B", results[1].Name);
        Assert.Equal("C", results[2].Name);
    }

    #endregion

    #region Pagination

    [Fact]
    public void Take_LimitsResults()
    {
        // Arrange
        var world = CreateTestWorld();
        for (int i = 0; i < 10; i++)
        {
            world.CreateEntity($"Entity{i}");
        }
        world.Flush();

        // Act
        var results = world.Query()
            .Take(5)
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public void Skip_SkipsResults()
    {
        // Arrange
        var world = CreateTestWorld();
        for (int i = 0; i < 10; i++)
        {
            world.CreateEntity($"Entity{i}");
        }
        world.Flush();

        // Act
        var results = world.Query()
            .Skip(7)
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void Skip_And_Take_WorksTogether()
    {
        // Arrange
        var world = CreateTestWorld();
        for (int i = 0; i < 10; i++)
        {
            world.CreateEntity($"Entity{i}");
        }
        world.Flush();

        // Act - Get page 2 (skip 3, take 3)
        var results = world.Query()
            .Skip(3)
            .Take(3)
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(3, results.Count);
    }

    #endregion

    #region Active/Inactive Filtering

    [Fact]
    public void OnlyActive_IsDefaultBehavior()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity("Active");
        var entity2 = world.CreateEntity("Inactive");
        entity2.IsActive = false;
        world.Flush();

        // Act
        var results = world.Query()
            .Execute()
            .ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void IncludeInactive_IncludesBothActiveAndInactive()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity("Active");
        var entity2 = world.CreateEntity("Inactive");
        entity2.IsActive = false;
        world.Flush();

        // Act
        var results = world.Query()
            .IncludeInactive()
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
    }

    #endregion

    #region Query Methods

    [Fact]
    public void First_ReturnsFirstMatch()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity("First").AddTag("Test");
        var entity2 = world.CreateEntity("Second").AddTag("Test");
        world.Flush();

        // Act
        var result = world.Query()
            .WithTag("Test")
            .First();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity1, result);
    }

    [Fact]
    public void First_NoMatch_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        world.Flush();

        // Act
        var result = world.Query()
            .WithTag("NonExistent")
            .First();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Count_ReturnsCorrectCount()
    {
        // Arrange
        var world = CreateTestWorld();
        for (int i = 0; i < 5; i++)
        {
            world.CreateEntity().AddTag("Test");
        }
        world.Flush();

        // Act
        var count = world.Query()
            .WithTag("Test")
            .Count();

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public void Any_WithMatches_ReturnsTrue()
    {
        // Arrange
        var world = CreateTestWorld();
        world.CreateEntity().AddTag("Test");
        world.Flush();

        // Act
        var hasAny = world.Query()
            .WithTag("Test")
            .Any();

        // Assert
        Assert.True(hasAny);
    }

    [Fact]
    public void Any_NoMatches_ReturnsFalse()
    {
        // Arrange
        var world = CreateTestWorld();
        world.Flush();

        // Act
        var hasAny = world.Query()
            .WithTag("Test")
            .Any();

        // Assert
        Assert.False(hasAny);
    }

    #endregion

    #region ForEach Methods

    [Fact]
    public void ForEach_Entity_ExecutesForEachMatch()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();
        world.Flush();

        var executedCount = 0;

        // Act
        world.Query()
            .ForEach(e => executedCount++);

        // Assert
        Assert.Equal(3, executedCount);
    }

    [Fact]
    public void ForEach_SingleComponent_PassesComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 100f);
        world.Flush();

        float capturedSpeed = 0;

        // Act
        world.Query()
            .With<VelocityComponent>()
            .ForEach<VelocityComponent>((e, v) => capturedSpeed = v.MaxSpeed);

        // Assert
        Assert.Equal(100f, capturedSpeed);
    }

    [Fact]
    public void ForEach_TwoComponents_PassesBothComponents()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10, 20))
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 100f);
        world.Flush();

        Vector2 capturedPos = Vector2.Zero;
        float capturedSpeed = 0;

        // Act
        world.Query()
            .With<TransformComponent>()
            .With<VelocityComponent>()
            .ForEach<TransformComponent, VelocityComponent>((e, t, v) =>
            {
                capturedPos = t.LocalPosition;
                capturedSpeed = v.MaxSpeed;
            });

        // Assert
        Assert.Equal(new Vector2(10, 20), capturedPos);
        Assert.Equal(100f, capturedSpeed);
    }

    #endregion

    #region Clone

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddTag("Test");
        world.Flush();

        var baseQuery = world.Query()
            .With<TransformComponent>();

        // Act - Clone and add additional filter
        var clonedQuery = baseQuery.Clone()
            .WithTag("Test");

        var baseResults = baseQuery.Execute().ToList();
        var clonedResults = clonedQuery.Execute().ToList();

        // Assert
        Assert.Equal(2, baseResults.Count); // Base query unmodified
        Assert.Single(clonedResults); // Cloned query has additional filter
    }

    #endregion

    #region Random

    [Fact]
    public void Random_Single_ReturnsRandomEntity()
    {
        // Arrange
        var world = CreateTestWorld();
        for (int i = 0; i < 10; i++)
        {
            world.CreateEntity($"Entity{i}");
        }
        world.Flush();

        // Act
        var result = world.Query().Random();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Random_NoMatches_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        world.Flush();

        // Act
        var result = world.Query()
            .WithTag("NonExistent")
            .Random();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Query_ComplexFilter_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        
        // Create various entities
        var player = world.CreateEntity("Player")
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0, 0))
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 200f)
            .AddTags("Player", "Controllable");

        var enemy1 = world.CreateEntity("Enemy1")
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50, 0))
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 150f)
            .AddTags("Enemy", "Flying");

        var enemy2 = world.CreateEntity("Enemy2")
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200, 0))
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 100f)
            .AddTag("Enemy");

        world.Flush();

        // Act - Find enemies within 100 units, sorted by speed descending, take top 1
        var results = world.Query()
            .With<TransformComponent>()
            .With<VelocityComponent>()
            .WithTag("Enemy")
            .WithinRadius(Vector2.Zero, 100f)
            .OrderByDescending(e => e.GetComponent<VelocityComponent>()!.MaxSpeed)
            .Take(1)
            .Execute()
            .ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(enemy1, results[0]);
    }

    #endregion
}