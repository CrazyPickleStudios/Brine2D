using Brine2D.Core;
using Brine2D.Collision;
using Brine2D.ECS.Components;
using Brine2D.ECS;
using Brine2D.ECS.Systems;

namespace Brine2D.Systems.Physics;

/// <summary>
/// System that handles collision detection between entities.
/// </summary>
public class PhysicsSystem : IUpdateSystem, IDisposable
{
    public string Name => "PhysicsSystem";
    public int UpdateOrder => 200;

    private readonly CollisionSystem _collisionSystem;
    private readonly Dictionary<Entity, CollisionShape> _entityShapes = new();
    private readonly Dictionary<Entity, HashSet<Entity>> _previousCollisions = new();

    public PhysicsSystem(CollisionSystem collisionSystem)
    {
        _collisionSystem = collisionSystem;
    }

    public void Update(GameTime gameTime, IEntityWorld world)
    {
        // Sync collider positions with transforms
        SyncColliders(world);

        // Detect collisions
        DetectCollisions(world);
    }

    private void SyncColliders(IEntityWorld world)
    {
        var entities = world.GetEntitiesWithComponent<ColliderComponent>();

        foreach (var entity in entities)
        {
            var collider = entity.GetComponent<ColliderComponent>();
            var transform = entity.GetComponent<TransformComponent>();

            if (collider?.Shape == null || transform == null)
                continue;

            // Update shape position
            collider.Shape.Position = transform.Position;

            // Register with collision system if not already
            if (!_entityShapes.ContainsKey(entity))
            {
                _entityShapes[entity] = collider.Shape;
                _collisionSystem.AddShape(collider.Shape);
            }
        }

        // Remove shapes for destroyed entities
        var toRemove = _entityShapes.Keys.Where(e => !e.IsActive).ToList();
        foreach (var entity in toRemove)
        {
            _collisionSystem.RemoveShape(_entityShapes[entity]);
            _entityShapes.Remove(entity);
            _previousCollisions.Remove(entity);
        }
    }

    private void DetectCollisions(IEntityWorld world)
    {
        var entities = world.GetEntitiesWithComponent<ColliderComponent>();

        foreach (var entity in entities)
        {
            var collider = entity.GetComponent<ColliderComponent>();
            if (collider?.Shape == null) continue;

            // Get current collisions
            var collisions = _collisionSystem.GetCollisions(collider.Shape);
            var currentColliding = new HashSet<Entity>();

            foreach (var shape in collisions)
            {
                // Find entity that owns this shape
                var other = _entityShapes.FirstOrDefault(kvp => kvp.Value == shape).Key;
                if (other == null || other == entity) continue;

                var otherCollider = other.GetComponent<ColliderComponent>();
                if (otherCollider == null) continue;

                // Check layer mask
                if ((collider.CollisionMask & (1 << otherCollider.Layer)) == 0)
                    continue;

                currentColliding.Add(other);

                // Check if this is a new collision
                if (!_previousCollisions.ContainsKey(entity) || 
                    !_previousCollisions[entity].Contains(other))
                {
                    collider.NotifyCollisionEnter(otherCollider);
                    otherCollider.NotifyCollisionEnter(collider);
                }
            }

            // Detect collision exits
            if (_previousCollisions.ContainsKey(entity))
            {
                var exited = _previousCollisions[entity].Except(currentColliding);
                foreach (var other in exited)
                {
                    var otherCollider = other.GetComponent<ColliderComponent>();
                    if (otherCollider != null)
                    {
                        collider.NotifyCollisionExit(otherCollider);
                        otherCollider.NotifyCollisionExit(collider);
                    }
                }
            }

            _previousCollisions[entity] = currentColliding;
        }
    }

    public void Dispose()
    {
        foreach (var shape in _entityShapes.Values)
        {
            _collisionSystem.RemoveShape(shape);
        }
        _entityShapes.Clear();
        _previousCollisions.Clear();
    }
}