using Brine2D.ECS;

namespace Brine2D.Collision;

/// <summary>
/// Component for collision detection.
/// Bridges ECS entities with the existing CollisionSystem.
/// </summary>
public class ColliderComponent : Component
{
    /// <summary>
    /// The collision shape (BoxCollider or CircleCollider).
    /// </summary>
    public CollisionShape? Shape { get; set; }

    /// <summary>
    /// Whether this collider is a trigger (no physical response).
    /// </summary>
    public bool IsTrigger { get; set; } = false;

    /// <summary>
    /// Collision layer (for filtering).
    /// </summary>
    public int Layer { get; set; } = 0;

    /// <summary>
    /// Collision mask (which layers this collider can hit).
    /// </summary>
    public int CollisionMask { get; set; } = -1; // All layers by default

    /// <summary>
    /// Event fired when collision starts.
    /// </summary>
    public event Action<ColliderComponent>? OnCollisionEnter;

    /// <summary>
    /// Event fired when collision ends.
    /// </summary>
    public event Action<ColliderComponent>? OnCollisionExit;

    /// <summary>
    /// Event fired when trigger is entered.
    /// </summary>
    public event Action<ColliderComponent>? OnTriggerEnter;

    /// <summary>
    /// Event fired when trigger is exited.
    /// </summary>
    public event Action<ColliderComponent>? OnTriggerExit;

    /// <summary>
    /// Currently colliding entities.
    /// </summary>
    public HashSet<Entity> CollidingEntities { get; } = new();

    protected internal override void OnAdded()
    {
        // Shape will be synced with transform by PhysicsSystem
    }

    protected internal override void OnRemoved()
    {
        // Cleanup handled by PhysicsSystem
        CollidingEntities.Clear();
    }

    /// <summary>
    /// Called by PhysicsSystem when collision starts.
    /// </summary>
    internal void NotifyCollisionEnter(ColliderComponent other)
    {
        if (other.Entity == null) return;

        CollidingEntities.Add(other.Entity);

        if (IsTrigger)
            OnTriggerEnter?.Invoke(other);
        else
            OnCollisionEnter?.Invoke(other);
    }

    /// <summary>
    /// Called by PhysicsSystem when collision ends.
    /// </summary>
    internal void NotifyCollisionExit(ColliderComponent other)
    {
        if (other.Entity == null) return;

        CollidingEntities.Remove(other.Entity);

        if (IsTrigger)
            OnTriggerExit?.Invoke(other);
        else
            OnCollisionExit?.Invoke(other);
    }

    /// <summary>
    /// Checks if currently colliding with a specific entity.
    /// </summary>
    public bool IsCollidingWith(Entity entity)
    {
        return CollidingEntities.Contains(entity);
    }

    /// <summary>
    /// Checks if currently colliding with any entity with a specific tag.
    /// </summary>
    public bool IsCollidingWithTag(string tag)
    {
        return CollidingEntities.Any(e => e.Tags.Contains(tag));
    }
}
