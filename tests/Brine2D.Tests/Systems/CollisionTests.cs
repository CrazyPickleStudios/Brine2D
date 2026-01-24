using System.Numerics;
using Brine2D.Collision;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Systems;

public class CollisionTests
{
    #region Box Collider Tests

    [Fact]
    public void BoxCollider_ShouldDetectOverlap()
    {
        // Arrange
        var box1 = new BoxCollider(10, 10, new Vector2(0, 0));
        var box2 = new BoxCollider(10, 10, new Vector2(5, 5));

        // Act
        var collides = box1.Intersects(box2);

        // Assert
        collides.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 0, 5, 5, true)]     // Overlapping
    [InlineData(0, 0, 20, 20, false)]  // Not overlapping
    [InlineData(0, 0, 10, 0, true)]    // Touching edges (horizontal)
    [InlineData(0, 0, 0, 10, true)]    // Touching edges (vertical)
    [InlineData(0, 0, 10, 10, true)]   // Touching corners (diagonal)
    public void BoxCollider_ShouldDetectVariousCollisions(
        float x1, float y1, float x2, float y2, bool expectedCollision)
    {
        // Arrange
        var box1 = new BoxCollider(10, 10, new Vector2(x1, y1));
        var box2 = new BoxCollider(10, 10, new Vector2(x2, y2));

        // Act
        var collides = box1.Intersects(box2);

        // Assert
        collides.Should().Be(expectedCollision);
    }

    [Fact]
    public void BoxCollider_ShouldDetectCompleteOverlap()
    {
        // Arrange - Same position and size
        var box1 = new BoxCollider(10, 10, new Vector2(0, 0));
        var box2 = new BoxCollider(10, 10, new Vector2(0, 0));

        // Act
        var collides = box1.Intersects(box2);

        // Assert
        collides.Should().BeTrue();
    }

    [Fact]
    public void BoxCollider_ShouldDetectWhenOneInsideAnother()
    {
        // Arrange - Small box inside large box
        var largeBox = new BoxCollider(100, 100, new Vector2(0, 0));
        var smallBox = new BoxCollider(10, 10, new Vector2(45, 45));

        // Act
        var collides = largeBox.Intersects(smallBox);

        // Assert
        collides.Should().BeTrue();
    }

    [Fact]
    public void BoxCollider_ShouldNotDetectWhenSeparated()
    {
        // Arrange - Far apart
        var box1 = new BoxCollider(10, 10, new Vector2(0, 0));
        var box2 = new BoxCollider(10, 10, new Vector2(50, 50));

        // Act
        var collides = box1.Intersects(box2);

        // Assert
        collides.Should().BeFalse();
    }

    #endregion

    #region Circle Collider Tests

    [Fact]
    public void CircleCollider_ShouldDetectOverlap()
    {
        // Arrange - Overlapping circles
        var circle1 = new CircleCollider(5, new Vector2(0, 0));
        var circle2 = new CircleCollider(5, new Vector2(5, 0));

        // Act
        var collides = circle1.Intersects(circle2);

        // Assert
        collides.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 0, 5, 0, true)]     // Overlapping (distance = 5, combined radius = 10)
    [InlineData(0, 0, 20, 0, false)]   // Not overlapping (distance = 20, combined radius = 10)
    [InlineData(0, 0, 10, 0, true)]    // Exactly touching (distance = combined radius)
    [InlineData(0, 0, 0, 0, true)]     // Same position
    public void CircleCollider_ShouldDetectVariousCollisions(
        float x1, float y1, float x2, float y2, bool expectedCollision)
    {
        // Arrange
        var circle1 = new CircleCollider(5, new Vector2(x1, y1));
        var circle2 = new CircleCollider(5, new Vector2(x2, y2));

        // Act
        var collides = circle1.Intersects(circle2);

        // Assert
        collides.Should().Be(expectedCollision);
    }

    [Fact]
    public void CircleCollider_ShouldDetectWhenOneInsideAnother()
    {
        // Arrange - Small circle inside large circle
        var largeCircle = new CircleCollider(50, new Vector2(0, 0));
        var smallCircle = new CircleCollider(5, new Vector2(10, 10));

        // Act
        var collides = largeCircle.Intersects(smallCircle);

        // Assert
        collides.Should().BeTrue();
    }

    [Fact]
    public void CircleCollider_ShouldNotDetectWhenSeparated()
    {
        // Arrange - Far apart
        var circle1 = new CircleCollider(5, new Vector2(0, 0));
        var circle2 = new CircleCollider(5, new Vector2(50, 0));

        // Act
        var collides = circle1.Intersects(circle2);

        // Assert
        collides.Should().BeFalse();
    }

    [Fact]
    public void CircleCollider_ShouldDetectDiagonalOverlap()
    {
        // Arrange - Diagonal overlap
        var circle1 = new CircleCollider(10, new Vector2(0, 0));
        var circle2 = new CircleCollider(10, new Vector2(10, 10));

        // Act
        var collides = circle1.Intersects(circle2);

        // Assert
        collides.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void BoxCollider_ShouldHandleZeroSizeCollider()
    {
        // Arrange
        var box1 = new BoxCollider(0, 0, new Vector2(5, 5));
        var box2 = new BoxCollider(10, 10, new Vector2(0, 0));

        // Act
        var collides = box1.Intersects(box2);

        // Assert
        collides.Should().BeFalse(); // Zero-size doesn't overlap
    }

    [Fact]
    public void CircleCollider_ShouldHandleZeroRadiusCollider()
    {
        // Arrange
        var circle1 = new CircleCollider(0, new Vector2(5, 5));
        var circle2 = new CircleCollider(10, new Vector2(5, 5));

        // Act
        var collides = circle1.Intersects(circle2);

        // Assert
        collides.Should().BeFalse(); // Zero radius doesn't overlap
    }

    [Fact]
    public void BoxCollider_ShouldHandleNegativeOffset()
    {
        // Arrange - Negative offsets
        var box1 = new BoxCollider(10, 10, new Vector2(-5, -5));
        var box2 = new BoxCollider(10, 10, new Vector2(0, 0));

        // Act
        var collides = box1.Intersects(box2);

        // Assert
        collides.Should().BeTrue();
    }

    [Fact]
    public void CircleCollider_ShouldHandleNegativeOffset()
    {
        // Arrange - Negative offsets
        var circle1 = new CircleCollider(5, new Vector2(-5, -5));
        var circle2 = new CircleCollider(5, new Vector2(0, 0));

        // Act
        var collides = circle1.Intersects(circle2);

        // Assert
        collides.Should().BeTrue();
    }

    #endregion

    #region Box vs Circle Collision Tests

    [Fact]
    public void BoxCircle_ShouldDetectOverlap()
    {
        var box = new BoxCollider(10, 10, new Vector2(0, 0));
        var circle = new CircleCollider(5, new Vector2(5, 5));
        
        box.Intersects(circle).Should().BeTrue();
        circle.Intersects(box).Should().BeTrue(); // Test symmetry
    }

    [Fact]
    public void BoxCircle_ShouldDetectCircleInsideBox()
    {
        var box = new BoxCollider(100, 100, new Vector2(0, 0));
        var circle = new CircleCollider(5, new Vector2(50, 50));
        
        box.Intersects(circle).Should().BeTrue();
    }

    [Fact]
    public void BoxCircle_ShouldDetectCircleTouchingEdge()
    {
        var box = new BoxCollider(10, 10, new Vector2(0, 0));
        var circle = new CircleCollider(5, new Vector2(15, 5)); // Touching right edge
        
        box.Intersects(circle).Should().BeTrue();
    }

    [Fact]
    public void BoxCircle_ShouldNotDetectWhenSeparated()
    {
        var box = new BoxCollider(10, 10, new Vector2(0, 0));
        var circle = new CircleCollider(5, new Vector2(50, 50));
        
        box.Intersects(circle).Should().BeFalse();
    }

    #endregion
}