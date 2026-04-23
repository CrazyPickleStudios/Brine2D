using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS.Components;
using Brine2D.Systems.Rendering;

namespace Brine2D.Tests.Systems.Rendering;

public class ShapeComponentTests : TestBase
{
    [Fact]
    public void RectangleShapeComponent_DefaultValues_AreCorrect()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<RectangleShapeComponent>();
        var shape = entity.GetComponent<RectangleShapeComponent>()!;

        Assert.Equal(0f, shape.Width);
        Assert.Equal(0f, shape.Height);
        Assert.Equal(Color.White, shape.FillColor);
        Assert.Null(shape.OutlineColor);
        Assert.Equal(1f, shape.OutlineThickness);
    }

    [Fact]
    public void CircleShapeComponent_DefaultValues_AreCorrect()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<CircleShapeComponent>();
        var shape = entity.GetComponent<CircleShapeComponent>()!;

        Assert.Equal(0f, shape.Radius);
        Assert.Equal(Color.White, shape.FillColor);
        Assert.Null(shape.OutlineColor);
        Assert.Equal(1f, shape.OutlineThickness);
    }

    [Fact]
    public void LineShapeComponent_DefaultValues_AreCorrect()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<LineShapeComponent>();
        var shape = entity.GetComponent<LineShapeComponent>()!;

        Assert.Equal(Vector2.Zero, shape.Start);
        Assert.Equal(Vector2.Zero, shape.End);
        Assert.Equal(Color.White, shape.FillColor);
        Assert.Equal(1f, shape.OutlineThickness);
    }

    [Fact]
    public void GetComponent_BaseType_ReturnsConcreteSubtype()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<RectangleShapeComponent>(r => { r.Width = 50f; r.Height = 30f; });

        var shape = entity.GetComponent<ShapeComponent>();

        Assert.NotNull(shape);
        Assert.IsType<RectangleShapeComponent>(shape);
    }

    [Fact]
    public void RectangleShapeComponent_Properties_RoundTrip()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<RectangleShapeComponent>(r =>
        {
            r.Width = 100f;
            r.Height = 60f;
            r.FillColor = Color.Red;
            r.OutlineColor = Color.Blue;
            r.OutlineThickness = 3f;
        });
        var shape = entity.GetComponent<RectangleShapeComponent>()!;

        Assert.Equal(100f, shape.Width);
        Assert.Equal(60f, shape.Height);
        Assert.Equal(Color.Red, shape.FillColor);
        Assert.Equal(Color.Blue, shape.OutlineColor);
        Assert.Equal(3f, shape.OutlineThickness);
    }

    [Fact]
    public void CircleShapeComponent_Properties_RoundTrip()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<CircleShapeComponent>(c => { c.Radius = 25f; c.FillColor = Color.Green; });
        var shape = entity.GetComponent<CircleShapeComponent>()!;

        Assert.Equal(25f, shape.Radius);
        Assert.Equal(Color.Green, shape.FillColor);
    }

    [Fact]
    public void LineShapeComponent_Properties_RoundTrip()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<LineShapeComponent>(l =>
        {
            l.Start = new Vector2(-20f, 0f);
            l.End = new Vector2(20f, 0f);
            l.FillColor = Color.White;
            l.OutlineThickness = 2f;
        });
        var shape = entity.GetComponent<LineShapeComponent>()!;

        Assert.Equal(new Vector2(-20f, 0f), shape.Start);
        Assert.Equal(new Vector2(20f, 0f), shape.End);
        Assert.Equal(2f, shape.OutlineThickness);
    }
}