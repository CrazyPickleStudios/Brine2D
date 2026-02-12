using System.Numerics;
using Brine2D.Core;

namespace Brine2D.Tests.Core;

public class RectangleTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithFloats_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var rect = new Rectangle(10, 20, 100, 50);

        // Assert
        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
    }

    [Fact]
    public void Constructor_WithVectors_SetsPropertiesCorrectly()
    {
        // Arrange
        var position = new Vector2(10, 20);
        var size = new Vector2(100, 50);

        // Act
        var rect = new Rectangle(position, size);

        // Assert
        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_ReturnCorrectDerivedValues()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);

        // Assert
        Assert.Equal(10, rect.Left);
        Assert.Equal(110, rect.Right);
        Assert.Equal(20, rect.Top);
        Assert.Equal(70, rect.Bottom);
        Assert.Equal(new Vector2(10, 20), rect.Position);
        Assert.Equal(new Vector2(100, 50), rect.Size);
        Assert.Equal(new Vector2(60, 45), rect.Center);
        Assert.Equal(5000, rect.Area);
    }

    [Fact]
    public void Corner_Properties_ReturnCorrectValues()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);

        // Assert
        Assert.Equal(new Vector2(10, 20), rect.TopLeft);
        Assert.Equal(new Vector2(110, 20), rect.TopRight);
        Assert.Equal(new Vector2(10, 70), rect.BottomLeft);
        Assert.Equal(new Vector2(110, 70), rect.BottomRight);
    }

    [Fact]
    public void IsEmpty_WithZeroWidth_ReturnsTrue()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 0, 50);

        // Act & Assert
        Assert.True(rect.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WithZeroHeight_ReturnsTrue()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 0);

        // Act & Assert
        Assert.True(rect.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WithPositiveDimensions_ReturnsFalse()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);

        // Act & Assert
        Assert.False(rect.IsEmpty);
    }

    #endregion

    #region Static Factory Methods

    [Fact]
    public void FromPoints_CreatesCorrectRectangle()
    {
        // Arrange
        var min = new Vector2(10, 20);
        var max = new Vector2(110, 70);

        // Act
        var rect = Rectangle.FromPoints(min, max);

        // Assert
        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
    }

    [Fact]
    public void FromCenter_WithVector_CreatesCorrectRectangle()
    {
        // Arrange
        var center = new Vector2(60, 45);
        var size = new Vector2(100, 50);

        // Act
        var rect = Rectangle.FromCenter(center, size);

        // Assert
        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
        Assert.Equal(center, rect.Center);
    }

    [Fact]
    public void FromCenter_WithFloats_CreatesCorrectRectangle()
    {
        // Arrange
        var center = new Vector2(60, 45);

        // Act
        var rect = Rectangle.FromCenter(center, 100, 50);

        // Assert
        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
    }

    [Fact]
    public void Empty_ReturnsZeroRectangle()
    {
        // Act
        var rect = Rectangle.Empty;

        // Assert
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(0, rect.Width);
        Assert.Equal(0, rect.Height);
        Assert.True(rect.IsEmpty);
    }

    #endregion

    #region Contains Tests

    [Fact]
    public void Contains_PointInside_ReturnsTrue()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);
        var point = new Vector2(50, 40);

        // Act & Assert
        Assert.True(rect.Contains(point));
        Assert.True(rect.Contains(50, 40));
    }

    [Fact]
    public void Contains_PointOnLeftEdge_ReturnsTrue()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);
        var point = new Vector2(10, 40);

        // Act & Assert
        Assert.True(rect.Contains(point));
    }

    [Fact]
    public void Contains_PointOnRightEdge_ReturnsFalse()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);
        var point = new Vector2(110, 40);

        // Act & Assert
        Assert.False(rect.Contains(point));
    }

    [Fact]
    public void Contains_PointOnBottomEdge_ReturnsFalse()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);
        var point = new Vector2(50, 70);

        // Act & Assert
        Assert.False(rect.Contains(point));
    }

    [Fact]
    public void Contains_PointOutside_ReturnsFalse()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);
        var point = new Vector2(200, 200);

        // Act & Assert
        Assert.False(rect.Contains(point));
    }

    [Fact]
    public void ContainsInclusive_PointOnRightEdge_ReturnsTrue()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);
        var point = new Vector2(110, 40);

        // Act & Assert
        Assert.True(rect.ContainsInclusive(point));
        Assert.True(rect.ContainsInclusive(110, 40));
    }

    [Fact]
    public void ContainsInclusive_PointOnBottomRightCorner_ReturnsTrue()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);
        var point = new Vector2(110, 70);

        // Act & Assert
        Assert.True(rect.ContainsInclusive(point));
    }

    [Fact]
    public void Contains_RectangleFullyInside_ReturnsTrue()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);
        var inner = new Rectangle(30, 30, 40, 20);

        // Act & Assert
        Assert.True(rect.Contains(inner));
    }

    [Fact]
    public void Contains_RectanglePartiallyInside_ReturnsFalse()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);
        var other = new Rectangle(50, 30, 100, 20);

        // Act & Assert
        Assert.False(rect.Contains(other));
    }

    [Fact]
    public void Contains_RectangleOutside_ReturnsFalse()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);
        var other = new Rectangle(200, 200, 50, 30);

        // Act & Assert
        Assert.False(rect.Contains(other));
    }

    #endregion

    #region Intersection Tests

    [Fact]
    public void Intersects_OverlappingRectangles_ReturnsTrue()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 100, 50);
        var rect2 = new Rectangle(50, 30, 100, 50);

        // Act & Assert
        Assert.True(rect1.Intersects(rect2));
        Assert.True(rect2.Intersects(rect1));
    }

    [Fact]
    public void Intersects_TouchingRectangles_ReturnsFalse()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 100, 50);
        var rect2 = new Rectangle(110, 20, 100, 50);

        // Act & Assert
        Assert.False(rect1.Intersects(rect2));
    }

    [Fact]
    public void Intersects_SeparateRectangles_ReturnsFalse()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 100, 50);
        var rect2 = new Rectangle(200, 200, 100, 50);

        // Act & Assert
        Assert.False(rect1.Intersects(rect2));
    }

    [Fact]
    public void Intersection_OverlappingRectangles_ReturnsCorrectIntersection()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 100, 50);
        var rect2 = new Rectangle(50, 30, 100, 50);

        // Act
        var intersection = rect1.Intersection(rect2);

        // Assert
        Assert.NotNull(intersection);
        Assert.Equal(50, intersection.Value.X);
        Assert.Equal(30, intersection.Value.Y);
        Assert.Equal(60, intersection.Value.Width);
        Assert.Equal(40, intersection.Value.Height);
    }

    [Fact]
    public void Intersection_NonIntersectingRectangles_ReturnsNull()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 100, 50);
        var rect2 = new Rectangle(200, 200, 100, 50);

        // Act
        var intersection = rect1.Intersection(rect2);

        // Assert
        Assert.Null(intersection);
    }

    #endregion

    #region Union Tests

    [Fact]
    public void Union_TwoRectangles_ReturnsCorrectBoundingBox()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 100, 50);
        var rect2 = new Rectangle(150, 80, 50, 30);

        // Act
        var union = rect1.Union(rect2);

        // Assert
        Assert.Equal(10, union.X);
        Assert.Equal(20, union.Y);
        Assert.Equal(190, union.Width);
        Assert.Equal(90, union.Height);
    }

    [Fact]
    public void Union_OverlappingRectangles_ReturnsCorrectBoundingBox()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 100, 50);
        var rect2 = new Rectangle(50, 30, 100, 50);

        // Act
        var union = rect1.Union(rect2);

        // Assert
        Assert.Equal(10, union.X);
        Assert.Equal(20, union.Y);
        Assert.Equal(140, union.Width);
        Assert.Equal(60, union.Height);
    }

    #endregion

    #region Penetration Tests

    [Fact]
    public void GetPenetration_IntersectingRectangles_ReturnsCorrectVector()
    {
        // Arrange
        var rect1 = new Rectangle(0, 0, 100, 100);
        var rect2 = new Rectangle(80, 40, 100, 100);

        // Act
        var penetration = rect1.GetPenetration(rect2);

        // Assert - Should push left (negative X) by 20 units (smallest overlap)
        Assert.Equal(-20, penetration.X);
        Assert.Equal(0, penetration.Y);
    }

    [Fact]
    public void GetPenetration_VerticalOverlap_ReturnsVerticalPenetration()
    {
        // Arrange
        var rect1 = new Rectangle(0, 0, 100, 100);
        var rect2 = new Rectangle(10, 80, 100, 100);

        // Act
        var penetration = rect1.GetPenetration(rect2);

        // Assert - Should push up (negative Y) by 20 units
        Assert.Equal(0, penetration.X);
        Assert.Equal(-20, penetration.Y);
    }

    [Fact]
    public void GetPenetration_NonIntersectingRectangles_ReturnsZero()
    {
        // Arrange
        var rect1 = new Rectangle(0, 0, 100, 100);
        var rect2 = new Rectangle(200, 200, 100, 100);

        // Act
        var penetration = rect1.GetPenetration(rect2);

        // Assert
        Assert.Equal(Vector2.Zero, penetration);
    }

    #endregion

    #region Transformation Tests

    [Fact]
    public void Inflate_ExpandsRectangleOnAllSides()
    {
        // Arrange
        var rect = new Rectangle(50, 50, 100, 100);

        // Act
        var inflated = rect.Inflate(10, 20);

        // Assert
        Assert.Equal(40, inflated.X);
        Assert.Equal(30, inflated.Y);
        Assert.Equal(120, inflated.Width);
        Assert.Equal(140, inflated.Height);
    }

    [Fact]
    public void Inflate_WithNegativeValues_ShrinksRectangle()
    {
        // Arrange
        var rect = new Rectangle(50, 50, 100, 100);

        // Act
        var inflated = rect.Inflate(-10, -20);

        // Assert
        Assert.Equal(60, inflated.X);
        Assert.Equal(70, inflated.Y);
        Assert.Equal(80, inflated.Width);
        Assert.Equal(60, inflated.Height);
    }

    [Fact]
    public void Offset_WithFloats_MovesRectangle()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);

        // Act
        var offset = rect.Offset(30, 40);

        // Assert
        Assert.Equal(40, offset.X);
        Assert.Equal(60, offset.Y);
        Assert.Equal(100, offset.Width);
        Assert.Equal(50, offset.Height);
    }

    [Fact]
    public void Offset_WithVector_MovesRectangle()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);

        // Act
        var offset = rect.Offset(new Vector2(30, 40));

        // Assert
        Assert.Equal(40, offset.X);
        Assert.Equal(60, offset.Y);
        Assert.Equal(100, offset.Width);
        Assert.Equal(50, offset.Height);
    }

    #endregion

    #region Conversion Tests

    [Fact]
    public void ToInt_RoundsToIntegerCoordinates()
    {
        // Arrange
        var rect = new Rectangle(10.4f, 20.6f, 99.3f, 50.7f);

        // Act
        var (x, y, width, height) = rect.ToInt();

        // Assert
        Assert.Equal(10, x);
        Assert.Equal(21, y);
        Assert.Equal(99, width);
        Assert.Equal(51, height);
    }

    [Fact]
    public void Deconstruct_ExtractsAllComponents()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);

        // Act
        var (x, y, width, height) = rect;

        // Assert
        Assert.Equal(10, x);
        Assert.Equal(20, y);
        Assert.Equal(100, width);
        Assert.Equal(50, height);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 100, 50);
        var rect2 = new Rectangle(10, 20, 100, 50);

        // Act & Assert
        Assert.True(rect1.Equals(rect2));
        Assert.True(rect1 == rect2);
        Assert.False(rect1 != rect2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 100, 50);
        var rect2 = new Rectangle(10, 20, 100, 51);

        // Act & Assert
        Assert.False(rect1.Equals(rect2));
        Assert.False(rect1 == rect2);
        Assert.True(rect1 != rect2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 100, 50);
        var rect2 = new Rectangle(10, 20, 100, 50);

        // Act & Assert
        Assert.Equal(rect1.GetHashCode(), rect2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 100, 50);

        // Act
        var result = rect.ToString();

        // Assert
        Assert.Equal("Rectangle(X:10, Y:20, W:100, H:50)", result);
    }

    #endregion
}