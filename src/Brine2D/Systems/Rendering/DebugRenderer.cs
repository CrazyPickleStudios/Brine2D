using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using Brine2D.ECS.Systems;
using Brine2D.Physics;
using Brine2D.Systems.AI;
using Brine2D.Systems.Physics;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Renders debug visualization for entities (colliders, velocities, AI paths, etc.).
/// </summary>
public class DebugRenderer : RenderSystemBase
{
    public string Name => "DebugRenderer";
    public override int RenderOrder => 1000;

    public bool ShowColliders { get; set; } = true;
    public bool ShowVelocities { get; set; } = true;
    public bool ShowAIDebug { get; set; } = true;
    public bool ShowEntityNames { get; set; } = true;

    public override void Render(IEntityWorld world, IRenderer renderer)
    {
        foreach (var entity in world.Entities)
        {
            var transform = entity.GetComponent<TransformComponent>();
            if (transform == null) continue;

            if (ShowColliders)
            {
                var collider = entity.GetComponent<PhysicsBodyComponent>();
                if (collider != null)
                    DrawCollider(renderer, transform, collider);
            }

            if (ShowVelocities)
            {
                var physicsBody = entity.GetComponent<PhysicsBodyComponent>();
                if (physicsBody != null && physicsBody.LinearVelocity != Vector2.Zero)
                    DrawVelocityArrow(renderer, transform.Position, physicsBody.LinearVelocity);
            }

            if (ShowAIDebug)
            {
                var ai = entity.GetComponent<AIControllerComponent>();
                if (ai != null)
                    DrawAIDebug(renderer, transform.Position, ai);
            }

            if (ShowEntityNames)
                renderer.DrawText(entity.Name, transform.Position.X, transform.Position.Y - 30, Color.White);
        }
    }

    private static Color ColliderColor(PhysicsBodyComponent collider) => collider.BodyType switch
    {
        PhysicsBodyType.Static => new Color(100, 100, 255, 220),
        PhysicsBodyType.Kinematic => new Color(255, 165, 0, 220),
        _ => collider.IsTrigger ? new Color(180, 0, 255, 220) : new Color(0, 255, 80, 220)
    };

    private static void DrawCollider(IRenderer renderer, TransformComponent transform, PhysicsBodyComponent collider)
    {
        var color = ColliderColor(collider);
        var bodyPos = transform.Position + collider.Offset;
        var rotation = transform.Rotation;

        DrawShape(renderer, collider.Shape, bodyPos, rotation, color);

        var subColor = color with { R = (byte)(color.R / 2), G = (byte)(color.G / 2), B = (byte)(color.B / 2) };
        foreach (var sub in collider.SubShapes)
            DrawShape(renderer, sub.Definition, bodyPos, rotation, subColor);
    }

    private static void DrawShape(IRenderer renderer, ShapeDefinition? shape, Vector2 bodyPos, float rotation, Color color)
    {
        switch (shape)
        {
            case CircleShape circle:
            {
                var center = bodyPos + RotatePoint(circle.Offset, rotation);
                renderer.DrawCircleOutline(center, circle.Radius, color, 1.5f);
                var spoke = center + new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)) * circle.Radius;
                renderer.DrawLine(center, spoke, color, 1f);
                break;
            }
            case BoxShape box:
            {
                var totalAngle = rotation + box.Angle;
                DrawOBB(renderer, bodyPos + RotatePoint(box.Offset, rotation), box.Width, box.Height, totalAngle, color);
                break;
            }
            case CapsuleShape capsule:
            {
                var c1 = bodyPos + RotatePoint(capsule.Center1, rotation);
                var c2 = bodyPos + RotatePoint(capsule.Center2, rotation);
                renderer.DrawCircleOutline(c1, capsule.Radius, color, 1.5f);
                renderer.DrawCircleOutline(c2, capsule.Radius, color, 1.5f);
                if (c1 != c2)
                {
                    var perp = Vector2.Normalize(new Vector2(-(c2.Y - c1.Y), c2.X - c1.X)) * capsule.Radius;
                    renderer.DrawLine(c1 + perp, c2 + perp, color, 1.5f);
                    renderer.DrawLine(c1 - perp, c2 - perp, color, 1.5f);
                }
                break;
            }
            case PolygonShape poly:
            {
                var verts = poly.Vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    var a = bodyPos + RotatePoint(verts[i], rotation);
                    var b = bodyPos + RotatePoint(verts[(i + 1) % verts.Length], rotation);
                    renderer.DrawLine(a, b, color, 1.5f);
                }
                break;
            }
            case ChainShape chain:
            {
                var pts = chain.Points;
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    var a = bodyPos + RotatePoint(pts[i], rotation);
                    var b = bodyPos + RotatePoint(pts[i + 1], rotation);
                    renderer.DrawLine(a, b, color, 1.5f);
                }
                if (chain.IsLoop && pts.Length > 1)
                {
                    var a = bodyPos + RotatePoint(pts[^1], rotation);
                    var b = bodyPos + RotatePoint(pts[0], rotation);
                    renderer.DrawLine(a, b, color, 1.5f);
                }
                break;
            }
        }
    }

    private static void DrawOBB(IRenderer renderer, Vector2 center, float width, float height, float angle, Color color)
    {
        float hw = width * 0.5f;
        float hh = height * 0.5f;
        ReadOnlySpan<Vector2> local =
        [
            new(-hw, -hh), new(hw, -hh), new(hw, hh), new(-hw, hh)
        ];

        for (int i = 0; i < 4; i++)
        {
            var a = center + RotatePoint(local[i], angle);
            var b = center + RotatePoint(local[(i + 1) % 4], angle);
            renderer.DrawLine(a, b, color, 1.5f);
        }
    }

    private static Vector2 RotatePoint(Vector2 point, float angle)
    {
        if (angle == 0f) return point;
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        return new Vector2(point.X * cos - point.Y * sin, point.X * sin + point.Y * cos);
    }

    private static void DrawVelocityArrow(IRenderer renderer, Vector2 position, Vector2 velocity)
    {
        var tip = position + velocity * 0.1f;
        renderer.DrawLine(position, tip, new Color(255, 255, 0, 200), 1.5f);
        renderer.DrawCircleFilled(tip, 3f, new Color(255, 255, 0, 255));
    }

    private static void DrawAIDebug(IRenderer renderer, Vector2 position, AIControllerComponent ai)
    {
        renderer.DrawCircleOutline(position, ai.DetectionRange, new Color(255, 0, 0, 120), 1f);

        if (ai.CurrentTarget != null)
        {
            var targetTransform = ai.CurrentTarget.GetComponent<TransformComponent>();
            if (targetTransform != null)
                renderer.DrawText($"→ {ai.CurrentTarget.Name}", position.X, position.Y + 20, new Color(255, 0, 0));
        }

        renderer.DrawText(ai.Behavior.ToString(), position.X, position.Y - 15, new Color(255, 200, 0));
    }
}