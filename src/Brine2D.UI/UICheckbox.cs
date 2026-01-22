using System.Drawing;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Checkbox UI component for toggle/boolean values.
/// </summary>
public class UICheckbox : IUIComponent
{
    public UITooltip? Tooltip { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Checkbox label text.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Whether the checkbox is checked.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnCheckedChanged?.Invoke(_isChecked);
            }
        }
    }

    /// <summary>
    /// Box size in pixels (for the actual checkbox square).
    /// </summary>
    public float BoxSize { get; set; } = 20f;

    /// <summary>
    /// Unchecked box color.
    /// </summary>
    public Color UncheckedColor { get; set; } = Color.FromArgb(60, 60, 60);

    /// <summary>
    /// Checked box color.
    /// </summary>
    public Color CheckedColor { get; set; } = Color.FromArgb(100, 150, 255);

    /// <summary>
    /// Hover box color.
    /// </summary>
    public Color HoverColor { get; set; } = Color.FromArgb(80, 80, 80);

    /// <summary>
    /// Checkmark color.
    /// </summary>
    public Color CheckmarkColor { get; set; } = Color.White;

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = Color.FromArgb(150, 150, 150);

    /// <summary>
    /// Label text color.
    /// </summary>
    public Color LabelColor { get; set; } = Color.White;

    /// <summary>
    /// Spacing between box and label.
    /// </summary>
    public float LabelSpacing { get; set; } = 10f;

    /// <summary>
    /// Event fired when checked state changes.
    /// </summary>
    public event Action<bool>? OnCheckedChanged;

    private bool _isChecked;
    private bool _isHovered;

    public UICheckbox(string label, Vector2 position)
    {
        Label = label;
        Position = position;
        Size = new Vector2(BoxSize + LabelSpacing + (label.Length * 8), BoxSize); // Auto-size based on label
    }

    public void Update(float deltaTime)
    {
        // Checkbox state is managed by UICanvas
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        // Determine box color based on state
        var boxColor = _isChecked ? CheckedColor :
                      _isHovered ? HoverColor :
                      UncheckedColor;

        // Draw checkbox box
        renderer.DrawRectangleFilled(Position.X, Position.Y, BoxSize, BoxSize, boxColor);

        // Draw border
        var borderColor = Enabled ? BorderColor : Color.FromArgb(100, 100, 100);
        float borderThickness = 2f;
        renderer.DrawRectangleFilled(Position.X, Position.Y, BoxSize, borderThickness, borderColor); // Top
        renderer.DrawRectangleFilled(Position.X, Position.Y + BoxSize - borderThickness, BoxSize, borderThickness, borderColor); // Bottom
        renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, BoxSize, borderColor); // Left
        renderer.DrawRectangleFilled(Position.X + BoxSize - borderThickness, Position.Y, borderThickness, BoxSize, borderColor); // Right

        // Draw checkmark if checked
        if (_isChecked)
        {
            // Simple checkmark using lines (X shape for simplicity without line drawing API)
            // Draw a filled rectangle to represent checkmark
            float checkPadding = BoxSize * 0.25f;
            float checkSize = BoxSize - (checkPadding * 2);
            renderer.DrawRectangleFilled(
                Position.X + checkPadding,
                Position.Y + checkPadding,
                checkSize,
                checkSize,
                CheckmarkColor);
        }

        // Draw label text
        if (!string.IsNullOrEmpty(Label))
        {
            var labelX = Position.X + BoxSize + LabelSpacing;
            var labelY = Position.Y + (BoxSize / 2) - 8;
            var labelColor = Enabled ? LabelColor : Color.FromArgb(100, 100, 100);
            renderer.DrawText(Label, labelX, labelY, labelColor);
        }
    }

    public bool Contains(Vector2 screenPosition)
    {
        // Hit test includes both box and label
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    /// <summary>
    /// Called by UICanvas when mouse hovers over checkbox.
    /// </summary>
    internal void SetHovered(bool hovered)
    {
        _isHovered = hovered && Enabled;
    }

    /// <summary>
    /// Called by UICanvas when checkbox is clicked.
    /// </summary>
    internal void Toggle()
    {
        if (Enabled)
        {
            IsChecked = !IsChecked;
        }
    }
}