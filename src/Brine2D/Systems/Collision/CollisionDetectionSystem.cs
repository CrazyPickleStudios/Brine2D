using Brine2D.Core;
using Brine2D.Collision;
using Brine2D.ECS.Components;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using System.Numerics;

namespace Brine2D.Systems.Collision;

/// <summary>
/// System that detects collisions between ColliderComponents.
/// Bridges ECS with the low-level CollisionSystem from Brine2D.Collision.
/// 
/// This is NOT a physics simulation system - it only detects overlaps.
/// For physics simulation (velocity, forces, mass), see PhysicsSimulationSystem.
/// </summary>
public class CollisionDetectionSystem : IUpdateSystem, IDisposable
{
    public string Name => "CollisionDetectionSystem";
    public int UpdateOrder => 200; // After movement, before rendering

    private readonly IEntityWorld _world;
    private readonly CollisionSystem _collisionSystem;
    private readonly Dictionary<Entity, CollisionShape> _entityShapes = new();
    private readonly Dictionary<Entity, HashSet<Entity>> _previousCollisions = new();

    public CollisionDetectionSystem(IEntityWorld world, CollisionSystem collisionSystem)
    {
        _world = world;
        _collisionSystem = collisionSystem;
    }

    public void Update(GameTime gameTime)
    {
        // Sync collider positions with transforms and create shapes if needed
        SyncColliders();

        // Detect collisions
        DetectCollisions();
    }

    private void SyncColliders()
    {
        var entities = _world.GetEntitiesWithComponent<ColliderComponent>();

        foreach (var entity in entities)
        {
            var collider = entity.GetComponent<ColliderComponent>();
            var transform = entity.GetComponent<TransformComponent>();

            if (collider == null || transform == null || !collider.IsEnabled)
                continue;

            // Create shape if needed
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

            // Update shape position (world position from transform)
            collider.Shape.Position = transform.WorldPosition;
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

    private void DetectCollisions()
    {
        var entities = _world.GetEntitiesWithComponent<ColliderComponent>();

        foreach (var entity in entities)
        {
            var collider = entity.GetComponent<ColliderComponent>();
            if (collider?.Shape == null || !collider.IsEnabled) continue;

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