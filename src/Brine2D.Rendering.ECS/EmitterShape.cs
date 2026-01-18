namespace Brine2D.Rendering.ECS;

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
    /// Uses ShapeSize.X property for line length.
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