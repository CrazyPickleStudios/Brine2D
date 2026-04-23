using System.Numerics;
using Brine2D.ECS;

namespace FeatureDemos.Components;

/// <summary>
/// Simple velocity-based movement for demos (no physics).
/// </summary>
public class MovementComponent : Component
{
    public Vector2 Velocity { get; set; }
}