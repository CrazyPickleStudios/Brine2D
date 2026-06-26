using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Tooltip shown while hovering over a UI component.
/// Attach one to any component via its <c>Tooltip</c> property.
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

    /// <summary>
    /// Optional font for rendering tooltip text (null = renderer default).
    /// </summary>
    public IFont? Font { get; set; }

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

        // Recalculate size using renderer metrics
        CalculateSize(renderer);

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
        renderer.DrawText(Text, textX, textY, new TextRenderOptions { Color = TextColor, Font = Font });
    }

    internal void OnHoverStart(Vector2 mousePosition)
    {
        _isHovering = true;
        UpdatePosition(mousePosition);
    }

    internal void OnHoverEnd()
    {
        _isHovering = false;
        _hoverTime = 0f;
        Visible = false;
    }

    internal void UpdatePosition(Vector2 mousePosition, Vector2? screenSize = null)
    {
        Position = mousePosition + CursorOffset;

        // Clamp to screen bounds if provided
        if (screenSize.HasValue)
        {
            var ss = screenSize.Value;
            var pos = Position;
            if (pos.X + Size.X > ss.X)
                pos.X = Math.Max(0, ss.X - Size.X - 4);
            if (pos.Y + Size.Y > ss.Y)
                pos.Y = Math.Max(0, ss.Y - Size.Y - 4);
            if (pos.X < 0) pos.X = 4;
            if (pos.Y < 0) pos.Y = 4;
            Position = pos;
        }
    }

    /// <summary>
    /// Calculates tooltip size based on text content.
    /// </summary>
    private void CalculateSize()
    {
        // Fallback estimate when no renderer is available
        float textWidth = Text.Length * 8;
        float textHeight = 16;

        if (MaxWidth > 0 && textWidth > MaxWidth - (Padding * 2))
        {
            int estimatedLines = (int)Math.Ceiling(textWidth / (MaxWidth - (Padding * 2)));
            textWidth = MaxWidth - (Padding * 2);
            textHeight *= estimatedLines;
        }

        Size = new Vector2(
            textWidth + (Padding * 2),
            textHeight + (Padding * 2));
    }

    private void CalculateSize(IRenderer renderer)
    {
        var options = new TextRenderOptions { MaxWidth = MaxWidth > 0 ? MaxWidth - (Padding * 2) : (float?)null, Font = Font };
        var measured = renderer.MeasureText(Text, options);
        Size = new Vector2(measured.X + (Padding * 2), measured.Y + (Padding * 2));
    }
}