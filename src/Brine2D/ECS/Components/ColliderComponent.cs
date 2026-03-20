using Brine2D.Collision;
using System.Numerics;

namespace Brine2D.ECS.Components;

/// <summary>
/// Component that defines a collision shape for an entity.
/// Supports circle and box (AABB) shapes.
/// Pure data - logic handled by CollisionDetectionSystem.
/// </summary>
public class ColliderComponent : Component
{
    /// <summary>
    /// The collision shape (managed by CollisionDetectionSystem).
    /// </summary>
    internal CollisionShape? Shape { get; set; }

    /// <summary>
    /// Collision layer this collider belongs to (0-31).
    /// </summary>
    public int Layer { get; set; } = 0;

    /// <summary>
    /// Bitmask of layers this collider can collide with.
    /// Default: 0xFFFFFFFF (all layers).
    /// </summary>
    public uint CollisionMask { get; set; } = 0xFFFFFFFF;

    /// <summary>
    /// Whether this collider is a trigger (no physics response).
    /// </summary>
    public bool IsTrigger { get; set; } = false;

    public Vector2 Offset { get; set; } = Vector2.Zero;

    public CollisionShapeType ShapeType { get; private set; }

    /// <summary>
    /// Circle radius (if ShapeType is Circle).
    /// </summary>
    public float Radius { get; private set; }

    /// <summary>
    /// Box width (if ShapeType is Box).
    /// </summary>
    public float Width { get; private set; }

    /// <summary>
    /// Box height (if ShapeType is Box).
    /// </summary>
    public float Height { get; private set; }

    /// <summary>
    /// Currently colliding entities (tracked by CollisionDetectionSystem).
    /// </summary>
    public HashSet<Entity> CollidingEntities { get; } = new();

    public event Action<ColliderComponent>? OnCollisionEnter;
    public event Action<ColliderComponent>? OnCollisionExit;
    public event Action<ColliderComponent>? OnTriggerEnter;
    public event Action<ColliderComponent>? OnTriggerExit;

    /// <summary>Nulls <see cref="Shape"/> so <c>CollisionDetectionSystem</c> recreates it next frame.</summary>
    public void SetCircle(float radius)
    {
        ShapeType = CollisionShapeType.Circle;
        Radius = radius;
        Shape = null;
    }

    /// <inheritdoc cref="SetCircle"/>
    public void SetBox(float width, float height)
    {
        ShapeType = CollisionShapeType.Box;
        Width = width;
        Height = height;
        Shape = null;
    }

    protected internal override void OnRemoved()
    {
        CollidingEntities.Clear();
    }

    internal void NotifyCollisionEnter(ColliderComponent other)
    {
        if (other.Entity == null) return;

        CollidingEntities.Add(other.Entity);

        if (IsTrigger)
            OnTriggerEnter?.Invoke(other);
        else
            OnCollisionEnter?.Invoke(other);
    }

    internal void NotifyCollisionExit(ColliderComponent other)
    {
        if (other.Entity == null) return;

        CollidingEntities.Remove(other.Entity);

        if (IsTrigger)
            OnTriggerExit?.Invoke(other);
        else
            OnCollisionExit?.Invoke(other);
    }
}