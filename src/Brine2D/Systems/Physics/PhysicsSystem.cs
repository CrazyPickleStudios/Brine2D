using Brine2D.Core;
using Brine2D.Collision;
using Brine2D.ECS.Components;
using Brine2D.ECS;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;

namespace Brine2D.Systems.Physics;

/// <summary>
/// System that handles collision detection between entities.
/// </summary>
public class PhysicsSystem : UpdateSystemBase
{
    public string Name => "PhysicsSystem";
    public override int UpdateOrder => SystemUpdateOrder.Physics; // Explicit order

    private readonly CollisionSystem _collisionSystem;
    private readonly Dictionary<Entity, CollisionShape> _entityShapes = new();
    private readonly Dictionary<Entity, HashSet<Entity>> _previousCollisions = new();
    private readonly List<Entity> _toRemoveBuffer = new();
    private readonly HashSet<Entity> _currentCollidingBuffer = new();
    private readonly List<CollisionShape> _collisionsBuffer = new();
    private CachedEntityQuery<ColliderComponent>? _colliderQuery;

    public PhysicsSystem(CollisionSystem collisionSystem)
    {
        _collisionSystem = collisionSystem;
    }

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        _colliderQuery ??= world.CreateCachedQuery<ColliderComponent>().Build();

        SyncColliders(world);
        DetectCollisions(world);
    }

    private void SyncColliders(IEntityWorld world)
    {
        foreach (var (entity, collider) in _colliderQuery!)
        {
            var transform = entity.GetComponent<TransformComponent>();

            if (collider.Shape == null || transform == null)
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

        _toRemoveBuffer.Clear();
        foreach (var entity in _entityShapes.Keys)
            if (!entity.IsActive) _toRemoveBuffer.Add(entity);

        foreach (var entity in _toRemoveBuffer)
        {
            _collisionSystem.RemoveShape(_entityShapes[entity]);
            _entityShapes.Remove(entity);
            _previousCollisions.Remove(entity);
        }
    }

    private void DetectCollisions(IEntityWorld world)
    {
        foreach (var (entity, collider) in _colliderQuery!)
        {
            if (collider.Shape == null) continue;

            _collisionSystem.GetCollisions(collider.Shape, _collisionsBuffer);
            _currentCollidingBuffer.Clear();

            _previousCollisions.TryGetValue(entity, out var previousSet);

            foreach (var shape in _collisionsBuffer)
            {
                var other = _entityShapes.FirstOrDefault(kvp => kvp.Value == shape).Key;
                if (other == null || other == entity) continue;

                var otherCollider = other.GetComponent<ColliderComponent>();
                if (otherCollider == null) continue;

                if ((collider.CollisionMask & (1 << otherCollider.Layer)) == 0)
                    continue;

                _currentCollidingBuffer.Add(other);

                if (previousSet == null || !previousSet.Contains(other))
                {
                    collider.NotifyCollisionEnter(otherCollider);
                    otherCollider.NotifyCollisionEnter(collider);
                }
            }

            if (previousSet == null)
            {
                if (_currentCollidingBuffer.Count == 0) continue;
                previousSet = new HashSet<Entity>();
                _previousCollisions[entity] = previousSet;
            }

            foreach (var other in previousSet)
            {
                if (!_currentCollidingBuffer.Contains(other))
                {
                    var otherCollider = other.GetComponent<ColliderComponent>();
                    if (otherCollider != null)
                    {
                        collider.NotifyCollisionExit(otherCollider);
                        otherCollider.NotifyCollisionExit(collider);
                    }
                }
            }

            previousSet.Clear();
            foreach (var e in _currentCollidingBuffer)
                previousSet.Add(e);
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