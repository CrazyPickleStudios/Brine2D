namespace Brine2D.Physics;

/// <summary>
/// Determines how a collider participates in collision resolution.
/// </summary>
public enum PhysicsBodyType
{
    /// <summary>
    /// Fully participates in collision resolution and can be pushed by other bodies.
    /// </summary>
    Dynamic,

    /// <summary>
    /// Never moved by collision resolution. Other bodies are pushed out of it.
    /// </summary>
    Static,

    /// <summary>
    /// Moved by user code but not by collision resolution. Pushes dynamic bodies out.
    /// </summary>
    Kinematic
}