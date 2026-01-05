using System.Numerics;
using Brine2D.Core;

namespace Brine2D.ECS.Components;

/// <summary>
/// Component for AI-controlled entities.
/// Supports multiple AI behaviors (patrol, chase, flee, wander).
/// Lives in Brine2D.ECS because it's pure game logic (no external dependencies).
/// </summary>
public class AIControllerComponent : Component
{
    /// <summary>
    /// Current AI behavior mode.
    /// </summary>
    public AIBehavior Behavior { get; set; } = AIBehavior.Idle;

    /// <summary>
    /// Movement speed in units per second.
    /// </summary>
    public float MoveSpeed { get; set; } = 150f;

    /// <summary>
    /// Detection range for chase/flee behaviors (in units).
    /// </summary>
    public float DetectionRange { get; set; } = 300f;

    /// <summary>
    /// Tag of entities to track (e.g., "Player" for enemies).
    /// </summary>
    public string TargetTag { get; set; } = "Player";

    /// <summary>
    /// Current target entity (set by AISystem).
    /// </summary>
    public Entity? CurrentTarget { get; internal set; }

    /// <summary>
    /// Current movement direction (calculated by AISystem).
    /// </summary>
    public Vector2 MoveDirection { get; internal set; }

    /// <summary>
    /// Whether the AI has a target in range.
    /// </summary>
    public bool HasTarget => CurrentTarget != null;

    // === Patrol Behavior ===
    /// <summary>
    /// Patrol waypoints (for patrol behavior).
    /// </summary>
    public List<Vector2> PatrolPoints { get; set; } = new();

    /// <summary>
    /// Current patrol waypoint index.
    /// </summary>
    public int CurrentWaypointIndex { get; internal set; }

    /// <summary>
    /// Distance to waypoint before moving to next one.
    /// </summary>
    public float WaypointReachedDistance { get; set; } = 10f;

    /// <summary>
    /// Whether to loop patrol points (true) or ping-pong (false).
    /// </summary>
    public bool LoopPatrol { get; set; } = true;

    // === Wander Behavior ===
    /// <summary>
    /// Time between direction changes when wandering (seconds).
    /// </summary>
    public float WanderChangeInterval { get; set; } = 2f;

    /// <summary>
    /// Timer for wander behavior.
    /// </summary>
    internal float WanderTimer { get; set; }

    /// <summary>
    /// Maximum wander distance from spawn point.
    /// </summary>
    public float WanderRadius { get; set; } = 200f;

    /// <summary>
    /// Spawn position (used for wander radius).
    /// </summary>
    public Vector2 SpawnPosition { get; set; }

    // === Chase/Flee Behavior ===
    /// <summary>
    /// Whether to stop when target is within this distance (for chase).
    /// 0 = always chase.
    /// </summary>
    public float StopDistance { get; set; } = 50f;

    /// <summary>
    /// Current distance to target (calculated by AISystem).
    /// </summary>
    public float DistanceToTarget { get; internal set; }
}

/// <summary>
/// AI behavior modes.
/// </summary>
public enum AIBehavior
{
    /// <summary>
    /// No movement.
    /// </summary>
    Idle,

    /// <summary>
    /// Move between patrol points.
    /// </summary>
    Patrol,

    /// <summary>
    /// Chase target entity when in range.
    /// </summary>
    Chase,

    /// <summary>
    /// Flee from target entity when in range.
    /// </summary>
    Flee,

    /// <summary>
    /// Random wandering within a radius.
    /// </summary>
    Wander
}