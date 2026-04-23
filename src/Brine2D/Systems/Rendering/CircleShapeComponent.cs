namespace Brine2D.Systems.Rendering;

/// <summary>
/// Renders a filled circle centered on the entity's transform position.
/// </summary>
public sealed class CircleShapeComponent : ShapeComponent
{
    public float Radius { get; set; }
}