using System.Numerics;

namespace Brine2D.ECS.Components;

/// <summary>
///     A simple linear velocity in world units per second, consumed by a movement system
///     that adds <c>Value * deltaTime</c> to <see cref="TransformComponent.Position"/>.
/// </summary>
public class VelocityComponent : Component
{
    public Vector2 Value { get; set; }
}
