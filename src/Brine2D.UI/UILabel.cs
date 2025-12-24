using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Simple text label UI component.
/// </summary>
public class UILabel : IUIComponent
{
    public UITooltip? Tooltip { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Text to display.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text color.
    /// </summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// Font to use for rendering. If null, uses renderer's default font.
    /// </summary>
    public IFont? Font { get; set; }

    public UILabel(string text, Vector2 position)
    {
        Text = text;
        Position = position;
        Size = new Vector2(200, 30); // Default size
    }

    public void Update(float deltaTime)
    {
        // Labels are typically static
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible || string.IsNullOrEmpty(Text)) return;

        // If we have a custom font, we could set it here
        // For now, just use the renderer's default font
        renderer.DrawText(Text, Position.X, Position.Y, Color);
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }
}