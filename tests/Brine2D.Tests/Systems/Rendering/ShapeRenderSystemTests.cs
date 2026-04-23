using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS.Components;
using Brine2D.ECS.Systems;
using Brine2D.Physics;
using Brine2D.Rendering;
using Brine2D.Systems.Rendering;
using NSubstitute;

namespace Brine2D.Tests.Systems.Rendering;

public class ShapeRenderSystemTests : TestBase
{
    [Fact]
    public void RenderOrder_IsSprites()
    {
        var system = new ShapeRenderSystem();
        Assert.Equal(SystemRenderOrder.Sprites, system.RenderOrder);
    }

    [Fact]
    public void Render_Rectangle_DrawsFilledRect()
    {
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 50f))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 40f;
                s.Height = 20f;
            });

        world.Flush();

        var system = new ShapeRenderSystem();
        system.Render(world, renderer);

        renderer.Received(1).DrawRectangleFilled(80f, 40f, 40f, 20f, Color.White);
    }

    [Fact]
    public void Render_Rectangle_WithOutline_DrawsBoth()
    {
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 50f))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 40f;
                s.Height = 20f;
                s.OutlineColor = Color.Red;
                s.OutlineThickness = 2f;
            });

        world.Flush();

        var system = new ShapeRenderSystem();
        system.Render(world, renderer);

        renderer.Received(1).DrawRectangleFilled(80f, 40f, 40f, 20f, Color.White);
        renderer.Received(1).DrawRectangleOutline(80f, 40f, 40f, 20f, Color.Red, 2f);
    }

    [Fact]
    public void Render_Circle_DrawsFilledCircle()
    {
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200f, 100f))
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 30f;
                s.FillColor = Color.Green;
            });
        world.Flush();

        var system = new ShapeRenderSystem();
        system.Render(world, renderer);

        renderer.Received(1).DrawCircleFilled(new Vector2(200f, 100f), 30f, Color.Green);
    }

    [Fact]
    public void Render_Circle_WithOutline_DrawsBoth()
    {
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200f, 100f))
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 30f;
                s.OutlineColor = Color.Yellow;
                s.OutlineThickness = 4f;
            });
        world.Flush();

        var system = new ShapeRenderSystem();
        system.Render(world, renderer);

        renderer.Received(1).DrawCircleFilled(new Vector2(200f, 100f), 30f, Color.White);
        renderer.Received(1).DrawCircleOutline(new Vector2(200f, 100f), 30f, Color.Yellow, 4f);
    }

    [Fact]
    public void Render_NoOutlineColor_SkipsOutline()
    {
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 10f;
                s.Height = 10f;
            });

        world.Flush();

        var system = new ShapeRenderSystem();
        system.Render(world, renderer);

        renderer.DidNotReceive().DrawRectangleOutline(
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<Color>(), Arg.Any<float>());
    }

    [Fact]
    public void Render_DisabledComponent_Skipped()
    {
        // TODO: 
        //var world = CreateTestWorld();
        //var renderer = Substitute.For<IRenderer>();

        //world.CreateEntity()
        //    .AddComponent<TransformComponent>()
        //    .AddComponent<RectangleShapeComponent>(s =>
        //    {
        //        s.Width = 10f;
        //        s.Height = 10f;
        //        s.IsEnabled = true;
        //    });
        //world.Flush();

        //var system = new ShapeRenderSystem();
        //system.Render(world, renderer);

        //renderer.DidNotReceive().DrawRectangleFilled(
        //    Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(),
        //    Arg.Any<Color>());
    }

    [Fact]
    public void Render_MissingTransform_Skipped()
    {
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        world.CreateEntity()
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 10f;
                s.Height = 10f;
            });
        world.Flush();

        var system = new ShapeRenderSystem();
        system.Render(world, renderer);

        renderer.DidNotReceive().DrawRectangleFilled(
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<Color>());
    }

    [Fact]
    public void Render_MultipleEntities_DrawsAll()
    {
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 10f;
                s.Height = 10f;
            });
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 5f;
            });
        world.Flush();

        var system = new ShapeRenderSystem();
        system.Render(world, renderer);

        renderer.Received(1).DrawRectangleFilled(
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<Color>());
        renderer.Received(1).DrawCircleFilled(
            Arg.Any<Vector2>(), Arg.Any<float>(), Arg.Any<Color>());
    }
}