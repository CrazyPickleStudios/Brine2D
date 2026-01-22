using System.Drawing;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Radio button UI component for exclusive selections within a group.
/// </summary>
public class UIRadioButton : IUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Radio button label text.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Whether this radio button is selected.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        internal set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                if (value)
                {
                    OnSelected?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// The group this radio button belongs to.
    /// </summary>
    public UIRadioButtonGroup Group { get; }

    /// <summary>
    /// Radio button size in pixels (for the actual radio circle).
    /// </summary>
    public float ButtonSize { get; set; } = 20f;

    /// <summary>
    /// Unchecked button color.
    /// </summary>
    public Color UncheckedColor { get; set; } = Color.FromArgb(60, 60, 60);

    /// <summary>
    /// Checked button color.
    /// </summary>
    public Color CheckedColor { get; set; } = Color.FromArgb(100, 150, 255);

    /// <summary>
    /// Hover button color.
    /// </summary>
    public Color HoverColor { get; set; } = Color.FromArgb(80, 80, 80);

    /// <summary>
    /// Inner dot color when checked.
    /// </summary>
    public Color DotColor { get; set; } = Color.White;

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = Color.FromArgb(150, 150, 150);

    /// <summary>
    /// Label text color.
    /// </summary>
    public Color LabelColor { get; set; } = Color.White;

    /// <summary>
    /// Spacing between button and label.
    /// </summary>
    public float LabelSpacing { get; set; } = 10f;

    /// <summary>
    /// Event fired when this radio button is selected.
    /// </summary>
    public event Action? OnSelected;

    public UITooltip? Tooltip { get; set; }

    private bool _isChecked;
    private bool _isHovered;

    public UIRadioButton(string label, UIRadioButtonGroup group, Vector2 position)
    {
        Label = label;
        Group = group ?? throw new ArgumentNullException(nameof(group));
        Position = position;
        Size = new Vector2(ButtonSize + LabelSpacing + (label.Length * 8), ButtonSize);

        // Register with group
        Group.RegisterButton(this);
    }

    public void Update(float deltaTime)
    {
        // Radio button state is managed by UICanvas
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        // Determine button color based on state
        var buttonColor = _isChecked ? CheckedColor :
                         _isHovered ? HoverColor :
                         UncheckedColor;

        float radius = ButtonSize / 2;
        float centerX = Position.X + radius;
        float centerY = Position.Y + radius;

        // Draw radio button circle
        renderer.DrawCircleFilled(centerX, centerY, radius, buttonColor);

        // Draw border (outer circle)
        var borderColor = Enabled ? BorderColor : Color.FromArgb(100, 100, 100);
        float borderThickness = 2f;
        // Draw border as slightly larger circle (simplified, not perfect)
        renderer.DrawCircleFilled(centerX, centerY, radius + borderThickness / 2, borderColor);
        renderer.DrawCircleFilled(centerX, centerY, radius - borderThickness / 2, buttonColor);

        // Draw inner dot if checked
        if (_isChecked)
        {
            float dotRadius = radius * 0.5f;
            renderer.DrawCircleFilled(centerX, centerY, dotRadius, DotColor);
        }

        // Draw label text
        if (!string.IsNullOrEmpty(Label))
        {
            var labelX = Position.X + ButtonSize + LabelSpacing;
            var labelY = Position.Y + (ButtonSize / 2) - 8;
            var labelColor = Enabled ? LabelColor : Color.FromArgb(100, 100, 100);
            renderer.DrawText(Label, labelX, labelY, labelColor);
        }
    }

    public bool Contains(Vector2 screenPosition)
    {
        // Hit test includes both button and label
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    /// <summary>
    /// Called by UICanvas when mouse hovers over radio button.
    /// </summary>
    internal void SetHovered(bool hovered)
    {
        _isHovered = hovered && Enabled;
    }

    /// <summary>
    /// Selects this radio button (deselects others in the group).
    /// </summary>
    public void Select()
    {
        if (Enabled && !_isChecked)
        {
            Group.SelectButton(this);
            IsChecked = true;
        }
    }
}