namespace Brine2D.Systems.Rendering;

/// <summary>
/// Renders a filled rectangle centered on the entity's transform position.
/// </summary>
public sealed class RectangleShapeComponent : ShapeComponent
{
    public float Width { get; set; }
    public float Height { get; set; }
}