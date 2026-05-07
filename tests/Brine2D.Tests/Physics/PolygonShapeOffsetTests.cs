using System.Numerics;
using Brine2D.Physics;

namespace Brine2D.Tests.Physics;

public class PolygonShapeOffsetTests
{
    private static readonly Vector2[] SquareVerts =
    [
        new(-10f, -10f), new(10f, -10f),
        new(10f, 10f),   new(-10f, 10f)
    ];

    [Fact]
    public void PolygonShape_DefaultOffset_IsZero()
    {
        var shape = new PolygonShape(SquareVerts);

        Assert.Equal(Vector2.Zero, shape.Offset);
    }

    [Fact]
    public void PolygonShape_InitOffset_SetsValue()
    {
        var offset = new Vector2(30f, 15f);
        var shape = new PolygonShape(SquareVerts) { Offset = offset };

        Assert.Equal(offset, shape.Offset);
    }

    [Fact]
    public void PolygonShape_WithExpression_PreservesOffset()
    {
        var shape = new PolygonShape(SquareVerts) { Offset = new Vector2(5f, 10f) };
        var copy = shape with { Radius = 1f };

        Assert.Equal(new Vector2(5f, 10f), copy.Offset);
    }

    [Fact]
    public void PolygonShape_WithExpression_CanChangeOffset()
    {
        var shape = new PolygonShape(SquareVerts) { Offset = new Vector2(5f, 0f) };
        var moved = shape with { Offset = new Vector2(20f, 0f) };

        Assert.Equal(new Vector2(5f, 0f), shape.Offset);
        Assert.Equal(new Vector2(20f, 0f), moved.Offset);
    }

    [Fact]
    public void PolygonShape_Offset_DoesNotMutateVertices()
    {
        var original = new PolygonShape(SquareVerts);
        var withOffset = original with { Offset = new Vector2(100f, 100f) };

        Assert.Equal(original.Vertices.Count, withOffset.Vertices.Count);
        for (int i = 0; i < original.Vertices.Count; i++)
            Assert.Equal(original.Vertices[i], withOffset.Vertices[i]);
    }

    [Fact]
    public void PolygonShape_DifferentOffsets_OffsetValuesAreNotEqual()
    {
        var a = new PolygonShape(SquareVerts) { Offset = new Vector2(10f, 0f) };
        var b = new PolygonShape(SquareVerts) { Offset = new Vector2(20f, 0f) };

        Assert.NotEqual(a.Offset, b.Offset);
    }

    [Fact]
    public void PolygonShape_SameOffset_OffsetValuesAreEqual()
    {
        var a = new PolygonShape(SquareVerts) { Offset = new Vector2(10f, 5f) };
        var b = new PolygonShape(SquareVerts) { Offset = new Vector2(10f, 5f) };

        Assert.Equal(a.Offset, b.Offset);
        Assert.Equal(a.Radius, b.Radius);
    }
}