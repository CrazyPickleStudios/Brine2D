using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Base class for all rendered primitive shapes.
/// Add a concrete subtype (<see cref="RectangleShapeComponent"/>,
/// <see cref="CircleShapeComponent"/>, <see cref="LineShapeComponent"/>)
/// to an entity — do not use this type directly.
/// </summary>
public abstract class ShapeComponent : Component
{
    public Color FillColor { get; set; } = Color.White;
    public Color? OutlineColor { get; set; }
    public float OutlineThickness { get; set; } = 1f;
}