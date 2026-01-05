using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS.Components;

namespace Brine2D.ECS.Systems;

/// <summary>
/// System that processes AI behaviors and applies movement.
/// Supports patrol, chase, flee, and wander behaviors.
/// </summary>
public class AISystem : IUpdateSystem
{
    public int UpdateOrder => 50; 

    private readonly IEntityWorld _world;
    private readonly Random _random = new();

    public AISystem(IEntityWorld world)
    {
        _world = world;
    }

    public void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;
        var aiEntities = _world.GetEntitiesWithComponent<AIControllerComponent>();

        foreach (var entity in aiEntities)
        {
            var ai = entity.GetComponent<AIControllerComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            var velocity = entity.GetComponent<VelocityComponent>();

            if (ai == null || transform == null || !ai.IsEnabled)
                continue;

            // Update AI behavior
            Vector2 moveDirection = ai.Behavior switch
            {
                AIBehavior.Idle => Vector2.Zero,
                AIBehavior.Patrol => UpdatePatrolBehavior(ai, transform, deltaTime),
                AIBehavior.Chase => UpdateChaseBehavior(ai, transform),
                AIBehavior.Flee => UpdateFleeBehavior(ai, transform),
                AIBehavior.Wander => UpdateWanderBehavior(ai, transform, deltaTime),
                _ => Vector2.Zero
            };

            ai.MoveDirection = moveDirection;

            // Apply to velocity component if it exists
            if (velocity != null && moveDirection != Vector2.Zero)
            {
                velocity.SetDirection(moveDirection, ai.MoveSpeed);
            }
        }
    }

    private Vector2 UpdatePatrolBehavior(AIControllerComponent ai, TransformComponent transform, float deltaTime)
    {
        if (ai.PatrolPoints.Count == 0)
            return Vector2.Zero;

        var currentWaypoint = ai.PatrolPoints[ai.CurrentWaypointIndex];
        var direction = currentWaypoint - transform.Position;
        var distance = direction.Length();

        // Check if reached waypoint
        if (distance < ai.WaypointReachedDistance)
        {
            // Move to next waypoint
            if (ai.LoopPatrol)
            {
                ai.CurrentWaypointIndex = (ai.CurrentWaypointIndex + 1) % ai.PatrolPoints.Count;
            }
            else
            {
                // Ping-pong (not implemented yet - just loop for now)
                ai.CurrentWaypointIndex = (ai.CurrentWaypointIndex + 1) % ai.PatrolPoints.Count;
            }

            return Vector2.Zero; // Pause briefly at waypoint
        }

        return Vector2.Normalize(direction);
    }

    private Vector2 UpdateChaseBehavior(AIControllerComponent ai, TransformComponent transform)
    {
        // Find target
        var target = FindNearestTarget(ai, transform);
        ai.CurrentTarget = target;

        if (target == null)
            return Vector2.Zero;

        var targetTransform = target.GetComponent<TransformComponent>();
        if (targetTransform == null)
            return Vector2.Zero;

        var direction = targetTransform.Position - transform.Position;
        var distance = direction.Length();
        ai.DistanceToTarget = distance;

        // Stop if too close
        if (distance < ai.StopDistance)
            return Vector2.Zero;

        return Vector2.Normalize(direction);
    }

    private Vector2 UpdateFleeBehavior(AIControllerComponent ai, TransformComponent transform)
    {
        // Find target to flee from
        var target = FindNearestTarget(ai, transform);
        ai.CurrentTarget = target;

        if (target == null)
            return Vector2.Zero;

        var targetTransform = target.GetComponent<TransformComponent>();
        if (targetTransform == null)
            return Vector2.Zero;

        var direction = transform.Position - targetTransform.Position; // Opposite of chase
        var distance = direction.Length();
        ai.DistanceToTarget = distance;

        // Only flee if target is in range
        if (distance > ai.DetectionRange)
            return Vector2.Zero;

        return Vector2.Normalize(direction);
    }

    private Vector2 UpdateWanderBehavior(AIControllerComponent ai, TransformComponent transform, float deltaTime)
    {
        ai.WanderTimer -= deltaTime;

        // Change direction periodically
        if (ai.WanderTimer <= 0)
        {
            ai.WanderTimer = ai.WanderChangeInterval;

            // Random direction
            var randomAngle = (float)(_random.NextDouble() * Math.PI * 2);
            var randomDirection = new Vector2(
                MathF.Cos(randomAngle),
                MathF.Sin(randomAngle));

            // Check if we're too far from spawn point
            var distanceFromSpawn = Vector2.Distance(transform.Position, ai.SpawnPosition);
            if (distanceFromSpawn > ai.WanderRadius)
            {
                // Head back towards spawn
                randomDirection = Vector2.Normalize(ai.SpawnPosition - transform.Position);
            }

            ai.MoveDirection = randomDirection;
        }

        return ai.MoveDirection;
    }

    private Entity? FindNearestTarget(AIControllerComponent ai, TransformComponent transform)
    {
        var targets = _world.GetEntitiesByTag(ai.TargetTag);
        Entity? nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (var target in targets)
        {
            var targetTransform = target.GetComponent<TransformComponent>();
            if (targetTransform == null)
                continue;

            var distance = Vector2.Distance(transform.Position, targetTransform.Position);

            // Check if in detection range and closer than previous nearest
            if (distance < ai.DetectionRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = target;
            }
        }

        return nearestTarget;
    }
}