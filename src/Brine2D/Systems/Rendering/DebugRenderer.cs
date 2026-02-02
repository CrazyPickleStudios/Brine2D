using System.Drawing;
using System.Numerics;
using Brine2D.Core;
using Brine2D.Collision;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using Brine2D.ECS.Systems;
using Brine2D.Systems.AI;
using Brine2D.Systems.Physics;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Renders debug visualization for entities (colliders, velocities, AI paths, etc.).
/// Lives in Brine2D.Rendering.ECS because it's the bridge between ECS and Rendering.
/// </summary>
public class DebugRenderer : IRenderSystem
{
    public string Name => "DebugRenderer"; 
    public int RenderOrder => 1000;

    public bool ShowColliders { get; set; } = true;
    public bool ShowVelocities { get; set; } = true;
    public bool ShowAIDebug { get; set; } = true;
    public bool ShowEntityNames { get; set; } = true;

    public DebugRenderer()
    {
    }

    public void Render(IRenderer renderer, IEntityWorld world)
    {
        var entities = world.Entities;

        foreach (var entity in entities)
        {
            var transform = entity.GetComponent<TransformComponent>();
            if (transform == null) continue;

            // Draw colliders
            if (ShowColliders)
            {
                var collider = entity.GetComponent<ColliderComponent>();
                if (collider?.Shape != null)
                {
                    DrawCollider(renderer, collider.Shape, Color.FromArgb(100, 0, 255, 0));
                }
            }

            // Draw velocities
            if (ShowVelocities)
            {
                var velocity = entity.GetComponent<VelocityComponent>();
                if (velocity != null && velocity.Velocity != Vector2.Zero)
                {
                    DrawVelocityArrow(renderer, transform.Position, velocity.Velocity);
                }
            }

            // Draw AI debug info
            if (ShowAIDebug)
            {
                var ai = entity.GetComponent<AIControllerComponent>();
                if (ai != null)
                {
                    DrawAIDebug(renderer, transform.Position, ai);
                }
            }

            // Draw entity names
            if (ShowEntityNames)
            {
                renderer.DrawText(entity.Name, transform.Position.X, transform.Position.Y - 30, Color.White);
            }
        }
    }

    private void DrawCollider(IRenderer renderer, CollisionShape shape, Color color)
    {
        var bounds = shape.GetBounds();

        if (shape is BoxCollider)
        {
            renderer.DrawRectangleFilled(bounds.X, bounds.Y, bounds.Width, bounds.Height, color);
        }
        else if (shape is CircleCollider circle)
        {
            renderer.DrawCircleFilled(
                bounds.X + bounds.Width / 2,
                bounds.Y + bounds.Height / 2,
                circle.Radius,
                color);
        }
    }

    private void DrawVelocityArrow(IRenderer renderer, Vector2 position, Vector2 velocity)
    {
        var endPos = position + velocity * 0.1f; // Scale down for visibility

        // Draw line (approximated with thin rectangle)
        var angle = MathF.Atan2(velocity.Y, velocity.X);
        var length = velocity.Length() * 0.1f;

        renderer.DrawRectangleFilled(position.X, position.Y - 1, length, 2, Color.FromArgb(200, 255, 255, 0));

        // Draw arrowhead (small circle)
        renderer.DrawCircleFilled(endPos.X, endPos.Y, 3, Color.FromArgb(255, 255, 255, 0));
    }

    private void DrawAIDebug(IRenderer renderer, Vector2 position, AIControllerComponent ai)
    {
        // Draw detection range
        renderer.DrawCircleFilled(position.X, position.Y, ai.DetectionRange, Color.FromArgb(50, 255, 0, 0));

        // Draw line to target
        if (ai.CurrentTarget != null)
        {
            var targetTransform = ai.CurrentTarget.GetComponent<TransformComponent>();
            if (targetTransform != null)
            {
                // Draw line (approximated)
                renderer.DrawText($"→ {ai.CurrentTarget.Name}", position.X, position.Y + 20, Color.FromArgb(255, 0, 0));
            }
        }

        // Draw behavior state
        renderer.DrawText(ai.Behavior.ToString(), position.X, position.Y - 15, Color.FromArgb(255, 200, 0));
    }
}