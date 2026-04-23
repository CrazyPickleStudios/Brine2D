using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;

namespace Brine2D.Systems.Rendering;

public class ShapeRenderSystem : RenderSystemBase, IDisposable
{
    private CachedEntityQuery<ShapeComponent, TransformComponent>? _query;

    public override int RenderOrder => SystemRenderOrder.Sprites;

    public override void Render(IEntityWorld world, IRenderer renderer)
    {
        _query ??= world.CreateCachedQuery<ShapeComponent, TransformComponent>()
            .OnlyEnabled()
            .Build();

        foreach (var (_, shape, transform) in _query)
        {
            var pos = transform.Position;

            switch (shape)
            {
                case RectangleShapeComponent rect:
                    renderer.DrawRectangleFilled(
                        pos.X - rect.Width * 0.5f,
                        pos.Y - rect.Height * 0.5f,
                        rect.Width, rect.Height, rect.FillColor);
                    if (rect.OutlineColor is { } rectOutline)
                        renderer.DrawRectangleOutline(
                            pos.X - rect.Width * 0.5f,
                            pos.Y - rect.Height * 0.5f,
                            rect.Width, rect.Height, rectOutline, rect.OutlineThickness);
                    break;

                case CircleShapeComponent circle:
                    renderer.DrawCircleFilled(pos, circle.Radius, circle.FillColor);
                    if (circle.OutlineColor is { } circleOutline)
                        renderer.DrawCircleOutline(pos, circle.Radius, circleOutline, circle.OutlineThickness);
                    break;

                case LineShapeComponent line:
                    renderer.DrawLine(
                        pos + line.Start,
                        pos + line.End,
                        line.FillColor,
                        line.OutlineThickness);
                    break;
            }
        }
    }

    public void Dispose()
    {
        _query?.Dispose();
        GC.SuppressFinalize(this);
    }
}