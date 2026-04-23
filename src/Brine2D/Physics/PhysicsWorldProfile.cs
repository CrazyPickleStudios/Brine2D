using System.Numerics;

namespace Brine2D.Physics;

/// <summary>
/// Timing breakdown (in milliseconds) for the last Box2D world step.
/// Obtained via <see cref="PhysicsWorld.GetProfile"/>.
/// </summary>
public readonly struct PhysicsWorldProfile
{
    public float Step { get; init; }
    public float Pairs { get; init; }
    public float Collide { get; init; }
    public float Solve { get; init; }
    public float MergeIslands { get; init; }
    public float PrepareStages { get; init; }
    public float SolveConstraints { get; init; }
    public float PrepareConstraints { get; init; }
    public float IntegrateVelocities { get; init; }
    public float WarmStart { get; init; }
    public float SolveImpulses { get; init; }
    public float IntegratePositions { get; init; }
    public float RelaxImpulses { get; init; }
    public float ApplyRestitution { get; init; }
    public float StoreImpulses { get; init; }
    public float SplitIslands { get; init; }
    public float Transforms { get; init; }
    public float HitEvents { get; init; }
    public float Refit { get; init; }
    public float Bullets { get; init; }
    public float SleepIslands { get; init; }
    public float Sensors { get; init; }
}