using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Tooltip that appears when hovering over UI components.
/// This is metadata about components, not an interactive component itself.
/// </summary>
public class UITooltip
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = false; // Hidden by default

    /// <summary>
    /// Tooltip text content.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(40, 40, 40, 230);

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(200, 200, 200, 255);

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Padding around text.
    /// </summary>
    public float Padding { get; set; } = 8f;

    /// <summary>
    /// Delay before showing tooltip (in seconds).
    /// </summary>
    public float ShowDelay { get; set; } = 0.5f;

    /// <summary>
    /// Offset from mouse cursor.
    /// </summary>
    public Vector2 CursorOffset { get; set; } = new Vector2(15, 15);

    /// <summary>
    /// Maximum width before text wrapping (0 = no wrapping).
    /// </summary>
    public float MaxWidth { get; set; } = 200f;

    private float _hoverTime;
    private bool _isHovering;

    public UITooltip(string text)
    {
        Text = text;
        CalculateSize();
    }

    public void Update(float deltaTime)
    {
        if (_isHovering)
        {
            _hoverTime += deltaTime;

            if (_hoverTime >= ShowDelay)
            {
                Visible = true;
            }
        }
        else
        {
            _hoverTime = 0f;
            Visible = false;
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible || string.IsNullOrEmpty(Text)) return;

        // Draw background
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        // Draw border
        float borderThickness = 1f;
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, borderThickness, BorderColor); // Top
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, BorderColor); // Bottom
        renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, Size.Y, BorderColor); // Left
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, BorderColor); // Right

        // Draw text
        var textX = Position.X + Padding;
        var textY = Position.Y + Padding;
        renderer.DrawText(Text, textX, textY, TextColor);
    }

    /// <summary>
    /// Called by UICanvas when mouse hovers over target component.
    /// </summary>
    internal void OnHoverStart(Vector2 mousePosition)
    {
        _isHovering = true;
        UpdatePosition(mousePosition);
    }

    /// <summary>
    /// Called by UICanvas when mouse leaves target component.
    /// </summary>
    internal void OnHoverEnd()
    {
        _isHovering = false;
        _hoverTime = 0f;
        Visible = false;
    }

    /// <summary>
    /// Updates tooltip position based on mouse cursor.
    /// </summary>
    internal void UpdatePosition(Vector2 mousePosition)
    {
        Position = mousePosition + CursorOffset;

        // TODO: Add screen bounds clamping to keep tooltip on screen
        // This would require knowing screen dimensions
    }

    /// <summary>
    /// Calculates tooltip size based on text content.
    /// </summary>
    private void CalculateSize()
    {
        // Rough estimation: 8 pixels per character width, 16 pixels height
        float textWidth = Text.Length * 8;
        float textHeight = 16;

        // Apply max width
        if (MaxWidth > 0 && textWidth > MaxWidth - (Padding * 2))
        {
            // Estimate number of lines needed
            int estimatedLines = (int)Math.Ceiling(textWidth / (MaxWidth - (Padding * 2)));
            textWidth = MaxWidth - (Padding * 2);
            textHeight *= estimatedLines;
        }

        Size = new Vector2(
            textWidth + (Padding * 2),
            textHeight + (Padding * 2));
    }
}