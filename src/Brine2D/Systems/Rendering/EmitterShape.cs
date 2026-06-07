namespace Brine2D.Systems.Rendering;

/// <summary>
/// Shape of particle emitter spawn area.
/// </summary>
public enum EmitterShape
{
    /// <summary>
    /// Spawn within a circle radius (default).
    /// Uses SpawnRadius property.
    /// </summary>
    Circle,

    /// <summary>
    /// Spawn within a rectangular area.
    /// Uses ShapeSize property (width, height).
    /// </summary>
    Box,

    /// <summary>
    /// Spawn along a line.
    /// Uses <see cref="ParticleEmitterComponent.LineLength"/> when non-zero,
    /// otherwise falls back to <see cref="ParticleEmitterComponent.ShapeSize"/>.X.
    /// <see cref="ParticleEmitterComponent.ShapeSize"/>.Y is not used for this shape.
    /// </summary>
    Line,

    /// <summary>
    /// Spawn within a cone (directional).
    /// Uses SpawnRadius and ConeAngle properties.
    /// </summary>
    Cone,

    /// <summary>
    /// Spawn at exact position (point emitter).
    /// Good for explosions.
    /// </summary>
    Point
}