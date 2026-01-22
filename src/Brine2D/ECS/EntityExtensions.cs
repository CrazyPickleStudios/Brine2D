using Brine2D.ECS.Components;

namespace Brine2D.ECS;

/// <summary>
/// Extension methods for Entity hierarchy management.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Sets the parent entity of this entity.
    /// Both entities must have TransformComponents.
    /// </summary>
    public static void SetParent(this Entity entity, Entity? parent, bool keepWorldPosition = true)
    {
        var childTransform = entity.GetComponent<TransformComponent>();
        if (childTransform == null)
        {
            throw new InvalidOperationException("Entity must have a TransformComponent to set parent");
        }

        var parentTransform = parent?.GetComponent<TransformComponent>();
        childTransform.SetParent(parentTransform, keepWorldPosition);
    }

    /// <summary>
    /// Gets the parent entity (if any).
    /// </summary>
    public static Entity? GetParent(this Entity entity)
    {
        var transform = entity.GetComponent<TransformComponent>();
        return transform?.Parent?.Entity;
    }

    /// <summary>
    /// Gets all child entities.
    /// </summary>
    public static IEnumerable<Entity> GetChildren(this Entity entity)
    {
        var transform = entity.GetComponent<TransformComponent>();
        if (transform == null)
            yield break;

        foreach (var childTransform in transform.Children)
        {
            if (childTransform.Entity != null)
                yield return childTransform.Entity;
        }
    }

    /// <summary>
    /// Gets all descendant entities (children, grandchildren, etc.).
    /// </summary>
    public static IEnumerable<Entity> GetDescendants(this Entity entity)
    {
        foreach (var child in entity.GetChildren())
        {
            yield return child;

            foreach (var descendant in child.GetDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Gets the root entity (topmost parent).
    /// </summary>
    public static Entity GetRoot(this Entity entity)
    {
        var parent = entity.GetParent();
        return parent != null ? parent.GetRoot() : entity;
    }
}