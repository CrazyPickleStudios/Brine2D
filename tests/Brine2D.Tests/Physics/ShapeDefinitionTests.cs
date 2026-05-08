//using System.Numerics;
//using Brine2D.Physics;

//namespace Brine2D.Tests.Physics;

//public class ShapeDefinitionTests
//{
//    [Theory]
//    [InlineData(0f)]
//    [InlineData(-1f)]
//    [InlineData(float.NegativeInfinity)]
//    public void CircleShape_ZeroOrNegativeRadius_Throws(float radius)
//    {
//        Assert.Throws<ArgumentOutOfRangeException>(() => new CircleShape(radius));
//    }

//    [Fact]
//    public void CircleShape_PositiveRadius_Stores()
//    {
//        var shape = new CircleShape(25f);

//        Assert.Equal(25f, shape.Radius);
//        Assert.Equal(Vector2.Zero, shape.Offset);
//    }

//    [Fact]
//    public void CircleShape_WithOffset_Stores()
//    {
//        var shape = new CircleShape(10f) { Offset = new Vector2(5f, -3f) };

//        Assert.Equal(new Vector2(5f, -3f), shape.Offset);
//    }

//    [Theory]
//    [InlineData(0f, 10f)]
//    [InlineData(-1f, 10f)]
//    public void BoxShape_ZeroOrNegativeWidth_Throws(float width, float height)
//    {
//        Assert.Throws<ArgumentOutOfRangeException>(() => new BoxShape(width, height));
//    }

//    [Theory]
//    [InlineData(10f, 0f)]
//    [InlineData(10f, -1f)]
//    public void BoxShape_ZeroOrNegativeHeight_Throws(float width, float height)
//    {
//        Assert.Throws<ArgumentOutOfRangeException>(() => new BoxShape(width, height));
//    }

//    [Fact]
//    public void BoxShape_ValidDimensions_Stores()
//    {
//        var shape = new BoxShape(100f, 50f);

//        Assert.Equal(100f, shape.Width);
//        Assert.Equal(50f, shape.Height);
//        Assert.Equal(Vector2.Zero, shape.Offset);
//        Assert.Equal(0f, shape.Angle);
//    }

//    [Fact]
//    public void BoxShape_WithOffsetAndAngle_Stores()
//    {
//        var shape = new BoxShape(40f, 20f) { Offset = new Vector2(1f, 2f), Angle = MathF.PI / 4f };

//        Assert.Equal(new Vector2(1f, 2f), shape.Offset);
//        Assert.Equal(MathF.PI / 4f, shape.Angle);
//    }

//    [Theory]
//    [InlineData(0f)]
//    [InlineData(-5f)]
//    public void CapsuleShape_ZeroOrNegativeRadius_Throws(float radius)
//    {
//        Assert.Throws<ArgumentOutOfRangeException>(() =>
//            new CapsuleShape(Vector2.Zero, Vector2.UnitY * 20f, radius));
//    }

//    [Fact]
//    public void CapsuleShape_ValidArgs_Stores()
//    {
//        var c1 = new Vector2(0f, -10f);
//        var c2 = new Vector2(0f, 10f);
//        var shape = new CapsuleShape(c1, c2, 5f);

//        Assert.Equal(c1, shape.Center1);
//        Assert.Equal(c2, shape.Center2);
//        Assert.Equal(5f, shape.Radius);
//    }

//    [Fact]
//    public void PolygonShape_TooFewVertices_Throws()
//    {
//        Assert.Throws<ArgumentOutOfRangeException>(() =>
//            new PolygonShape([Vector2.Zero, Vector2.UnitX]));
//    }

//    [Fact]
//    public void PolygonShape_TooManyVertices_Throws()
//    {
//        var verts = new Vector2[ShapeDefinition.MaxPolygonVertices + 1];
//        Assert.Throws<ArgumentOutOfRangeException>(() => new PolygonShape(verts));
//    }

//    [Fact]
//    public void PolygonShape_ValidTriangle_StoresVertices()
//    {
//        Vector2[] verts = [Vector2.Zero, Vector2.UnitX * 10f, Vector2.UnitY * 10f];
//        var shape = new PolygonShape(verts);

//        Assert.Equal(3, shape.Vertices.Count);
//    }

//    [Fact]
//    public void PolygonShape_NegativeRadius_Throws()
//    {
//        Vector2[] verts = [Vector2.Zero, Vector2.UnitX * 10f, Vector2.UnitY * 10f];
//        Assert.Throws<ArgumentOutOfRangeException>(() => new PolygonShape(verts, -1f));
//    }

//    [Fact]
//    public void ChainShape_TooFewPoints_Throws()
//    {
//        Assert.Throws<ArgumentOutOfRangeException>(() =>
//            new ChainShape([Vector2.Zero]));
//    }

//    [Fact]
//    public void ChainShape_TwoPoints_Stores()
//    {
//        var shape = new ChainShape([Vector2.Zero, Vector2.UnitX * 100f]);

//        Assert.Equal(2, shape.Points.Length);
//        Assert.False(shape.IsLoop);
//    }

//    [Fact]
//    public void ChainShape_Loop_StoresIsLoop()
//    {
//        Vector2[] pts = [Vector2.Zero, new Vector2(100, 0), new Vector2(100, 100), new Vector2(0, 100)];
//        var shape = new ChainShape(pts, isLoop: true);

//        Assert.True(shape.IsLoop);
//        Assert.Equal(4, shape.Points.Length);
//    }

//    [Fact]
//    public void ChainShape_SegmentMaterials_StoresCorrectly()
//    {
//        Vector2[] pts = [Vector2.Zero, Vector2.UnitX * 50f, Vector2.UnitX * 100f];
//        (float Friction, float Restitution)[] mats = [(0.5f, 0.1f), (0.8f, 0.3f)];
//        var shape = new ChainShape(pts) { SegmentMaterials = mats };

//        Assert.NotNull(shape.SegmentMaterials);
//        Assert.Equal(2, shape.SegmentMaterials!.Length);
//        Assert.Equal(0.5f, shape.SegmentMaterials[0].Friction);
//    }
//}