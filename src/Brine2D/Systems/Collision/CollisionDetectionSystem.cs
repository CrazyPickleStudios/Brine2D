using Brine2D.Collision;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;

namespace Brine2D.Systems.Collision;

/// <summary>
/// System that detects collisions between entities with ColliderComponent.
/// Uses a spatial hash grid for efficient broad-phase collision detection.
/// </summary>
public class CollisionDetectionSystem : UpdateSystemBase
{
    private readonly CollisionSystem _collisionSystem;
    private readonly Dictionary<Entity, CollisionShape> _entityShapes = new();
    private readonly Dictionary<Entity, HashSet<Entity>> _previousCollisions = new();
    private CachedEntityQuery<ColliderComponent>? _colliderQuery;

    public override int UpdateOrder => SystemUpdateOrder.Collision;

    public CollisionDetectionSystem()
    {
        _collisionSystem = new CollisionSystem();
    }

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        // Sync colliders (create/update shapes)
        SyncColliders(world);

        // Detect collisions
        DetectCollisions(world);

        // Clean up destroyed entities
        CleanupDestroyedEntities(world);
    }

    private void SyncColliders(IEntityWorld world)
    {
        _colliderQuery ??= world.CreateCachedQuery<ColliderComponent>().Build();

        foreach (var (entity, collider) in _colliderQuery)
        {
            var transform = entity.GetComponent<TransformComponent>();

            if (transform == null || !collider.IsEnabled)
                continue;

            if (collider.Shape == null)
            {
                collider.Shape = collider.ShapeType switch
                {
                    CollisionShapeType.Circle => new CircleCollider(
                        collider.Radius,
                        collider.Offset),

                    CollisionShapeType.Box => new BoxCollider(
                        collider.Width,
                        collider.Height,
                        collider.Offset),

                    _ => throw new InvalidOperationException(
                        $"ColliderComponent on entity '{entity.Name}' has unknown shape type: {collider.ShapeType}. " +
                        $"Did you forget to call SetCircle() or SetBox()?")
                };

                _entityShapes[entity] = collider.Shape;
                _collisionSystem.AddShape(collider.Shape);
            }

            collider.Shape.Position = transform.Position;
        }

        // Remove shapes for destroyed/disabled entities
        var toRemove = _entityShapes.Keys
            .Where(e => !e.IsActive || e.GetComponent<ColliderComponent>()?.IsEnabled == false)
            .ToList();

        foreach (var entity in toRemove)
        {
            _collisionSystem.RemoveShape(_entityShapes[entity]);
            _entityShapes.Remove(entity);
            _previousCollisions.Remove(entity);
        }
    }

    private void DetectCollisions(IEntityWorld world)
    {
        _colliderQuery ??= world.CreateCachedQuery<ColliderComponent>().Build();

        foreach (var (entity, collider) in _colliderQuery)
        {
            if (collider.Shape == null || !collider.IsEnabled) continue;

            // Get current collisions from low-level collision system
            var collisions = _collisionSystem.GetCollisions(collider.Shape);
            var currentColliding = new HashSet<Entity>();

            foreach (var shape in collisions)
            {
                // Find entity that owns this shape
                var other = _entityShapes.FirstOrDefault(kvp => kvp.Value == shape).Key;
                if (other == null || other == entity) continue;

                var otherCollider = other.GetComponent<ColliderComponent>();
                if (otherCollider == null || !otherCollider.IsEnabled) continue;

                // Check layer mask
                if ((collider.CollisionMask & (1u << otherCollider.Layer)) == 0)
                    continue;

                currentColliding.Add(other);

                // Check if this is a new collision
                if (!_previousCollisions.TryGetValue(entity, out var previous) || !previous.Contains(other))
                {
                    // Collision enter
                    collider.NotifyCollisionEnter(otherCollider);
                }
            }

            // Check for collision exits
            if (_previousCollisions.TryGetValue(entity, out var previousSet))
            {
                foreach (var previous in previousSet)
                {
                    if (!currentColliding.Contains(previous))
                    {
                        var otherCollider = previous.GetComponent<ColliderComponent>();
                        if (otherCollider != null)
                        {
                            collider.NotifyCollisionExit(otherCollider);
                        }
                    }
                }
            }

            // Update previous collisions
            _previousCollisions[entity] = currentColliding;
        }
    }

    private void CleanupDestroyedEntities(IEntityWorld world)
    {
        var destroyed = _entityShapes.Keys.Where(e => !e.IsActive).ToList();
        foreach (var entity in destroyed)
        {
            _collisionSystem.RemoveShape(_entityShapes[entity]);
            _entityShapes.Remove(entity);
            _previousCollisions.Remove(entity);
        }
    }
}