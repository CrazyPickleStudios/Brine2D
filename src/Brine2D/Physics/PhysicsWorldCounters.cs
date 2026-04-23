namespace Brine2D.Physics;

/// <summary>
/// Live simulation counters for the Box2D world.
/// Obtained via <see cref="PhysicsWorld.GetCounters"/>.
/// </summary>
public readonly struct PhysicsWorldCounters
{
    public int BodyCount { get; init; }
    public int ShapeCount { get; init; }
    public int ContactCount { get; init; }
    public int JointCount { get; init; }
    public int IslandCount { get; init; }
    public int StackUsed { get; init; }
    public int StaticTreeHeight { get; init; }
    public int TreeHeight { get; init; }
    public int ByteCount { get; init; }
    public int TaskCount { get; init; }
    public int AwakeBodyCount { get; init; }
}