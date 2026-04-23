using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.Tests.Physics;

public class HitStructTests
{
    [Fact]
    public void RaycastHit_InitProperties()
    {
        var hit = new RaycastHit
        {
            Point = new Vector2(10f, 20f),
            Normal = new Vector2(0f, -1f),
            Fraction = 0.5f,
            ShapeId = default
        };

        Assert.Equal(new Vector2(10f, 20f), hit.Point);
        Assert.Equal(new Vector2(0f, -1f), hit.Normal);
        Assert.Equal(0.5f, hit.Fraction);
    }

    [Fact]
    public void ShapeCastHit_InitProperties()
    {
        var hit = new ShapeCastHit
        {
            Point = new Vector2(30f, 40f),
            Normal = new Vector2(1f, 0f),
            Fraction = 0.75f,
            ShapeId = default
        };

        Assert.Equal(new Vector2(30f, 40f), hit.Point);
        Assert.Equal(new Vector2(1f, 0f), hit.Normal);
        Assert.Equal(0.75f, hit.Fraction);
    }

    [Fact]
    public void RaycastHit_DefaultValues_AreZero()
    {
        var hit = new RaycastHit();

        Assert.Equal(Vector2.Zero, hit.Point);
        Assert.Equal(Vector2.Zero, hit.Normal);
        Assert.Equal(0f, hit.Fraction);
    }

    [Fact]
    public void ShapeCastHit_DefaultValues_AreZero()
    {
        var hit = new ShapeCastHit();

        Assert.Equal(Vector2.Zero, hit.Point);
        Assert.Equal(Vector2.Zero, hit.Normal);
        Assert.Equal(0f, hit.Fraction);
    }
}