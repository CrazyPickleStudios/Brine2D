using System.Drawing;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Interactive button UI component.
/// </summary>
public class UIButton : IUIComponent
{
    public UITooltip? Tooltip { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Button text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Normal state color.
    /// </summary>
    public Color NormalColor { get; set; } = Color.FromArgb(255, 70, 70, 70);

    /// <summary>
    /// Hover state color.
    /// </summary>
    public Color HoverColor { get; set; } = Color.FromArgb(255, 90, 90, 90);

    /// <summary>
    /// Pressed state color.
    /// </summary>
    public Color PressedColor { get; set; } = Color.FromArgb(255, 50, 50, 50);

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Event fired when button is clicked.
    /// </summary>
    public event Action? OnClick;

    private bool _isHovered;
    private bool _isPressed;

    public UIButton(string text, Vector2 position, Vector2 size)
    {
        Text = text;
        Position = position;
        Size = size;
    }

    public void Update(float deltaTime)
    {
        // Button states are updated by UICanvas based on input
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        // Determine current color based on state
        var currentColor = _isPressed ? PressedColor :
                          _isHovered ? HoverColor :
                          NormalColor;

        // Draw button background
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, currentColor);

        // Draw border
        var borderColor = Enabled ? Color.FromArgb(150, 150, 150) : Color.FromArgb(100, 100, 100);
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, 2, borderColor); // Top
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - 2, Size.X, 2, borderColor); // Bottom
        renderer.DrawRectangleFilled(Position.X, Position.Y, 2, Size.Y, borderColor); // Left
        renderer.DrawRectangleFilled(Position.X + Size.X - 2, Position.Y, 2, Size.Y, borderColor); // Right

        // Draw text (centered)
        var textX = Position.X + (Size.X / 2) - (Text.Length * 4); // Rough centering
        var textY = Position.Y + (Size.Y / 2) - 8;
        renderer.DrawText(Text, textX, textY, Enabled ? TextColor : Color.FromArgb(100, 100, 100));
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    /// <summary>
    /// Called by UICanvas when mouse hovers over button.
    /// </summary>
    internal void SetHovered(bool hovered)
    {
        _isHovered = hovered && Enabled;
    }

    /// <summary>
    /// Called by UICanvas when mouse is pressed on button.
    /// </summary>
    internal void SetPressed(bool pressed)
    {
        _isPressed = pressed && Enabled;
    }

    /// <summary>
    /// Called by UICanvas when button is clicked.
    /// </summary>
    internal void Click()
    {
        if (Enabled)
        {
            OnClick?.Invoke();
        }
    }
}