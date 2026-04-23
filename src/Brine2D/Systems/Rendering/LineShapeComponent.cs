using System.Numerics;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Renders a line between two local-space points, offset from the entity's transform position.
/// Line color is controlled by <see cref="ShapeComponent.FillColor"/>;
/// line thickness by <see cref="ShapeComponent.OutlineThickness"/>.
/// </summary>
public sealed class LineShapeComponent : ShapeComponent
{
    /// <summary>Start point in local space, relative to the entity's transform position.</summary>
    public Vector2 Start { get; set; }

    /// <summary>End point in local space, relative to the entity's transform position.</summary>
    public Vector2 End { get; set; }
}