namespace Brine2D.Physics;

/// <summary>
/// An immutable surface material preset that sets <see cref="Friction"/> and
/// <see cref="Restitution"/> together on a <see cref="Brine2D.ECS.Components.PhysicsBodyComponent"/>.
/// Assign via <see cref="Brine2D.ECS.Components.PhysicsBodyComponent.Material"/>.
/// </summary>
public sealed record PhysicsMaterial(float Friction, float Restitution)
{
    /// <summary>Default surface material. Friction 0.6, Restitution 0.</summary>
    public static readonly PhysicsMaterial Default = new(0.6f, 0f);

    /// <summary>Slippery surface. Friction 0.05, Restitution 0.</summary>
    public static readonly PhysicsMaterial Ice = new(0.05f, 0f);

    /// <summary>Highly elastic surface. Friction 0.4, Restitution 0.9.</summary>
    public static readonly PhysicsMaterial Bouncy = new(0.4f, 0.9f);

    /// <summary>Smooth metal surface. Friction 0.2, Restitution 0.1.</summary>
    public static readonly PhysicsMaterial Metal = new(0.2f, 0.1f);

    /// <summary>Rough wood surface. Friction 0.8, Restitution 0.05.</summary>
    public static readonly PhysicsMaterial Wood = new(0.8f, 0.05f);

    /// <summary>Soft rubber surface. Friction 0.9, Restitution 0.7.</summary>
    public static readonly PhysicsMaterial Rubber = new(0.9f, 0.7f);

    public float Friction { get; init; } = Math.Clamp(Friction, 0f, 1f);
    public float Restitution { get; init; } = Math.Clamp(Restitution, 0f, 1f);
}