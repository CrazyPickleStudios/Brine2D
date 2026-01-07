using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Panel/container UI component with background.
/// </summary>
public class UIPanel : IUIComponent
{
    public UITooltip? Tooltip { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(50, 50, 50, 200);

    /// <summary>
    /// Border color (optional).
    /// </summary>
    public Color? BorderColor { get; set; }

    /// <summary>
    /// Border thickness in pixels.
    /// </summary>
    public float BorderThickness { get; set; } = 2f;

    public UIPanel(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public void Update(float deltaTime)
    {
        // Panels are typically static
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        // Draw background
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        // Draw border if specified
        if (BorderColor.HasValue && BorderThickness > 0)
        {
            var border = BorderColor.Value;
            // Top
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, BorderThickness, border);
            // Bottom
            renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - BorderThickness, Size.X, BorderThickness, border);
            // Left
            renderer.DrawRectangleFilled(Position.X, Position.Y, BorderThickness, Size.Y, border);
            // Right
            renderer.DrawRectangleFilled(Position.X + Size.X - BorderThickness, Position.Y, BorderThickness, Size.Y, border);
        }
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }
}