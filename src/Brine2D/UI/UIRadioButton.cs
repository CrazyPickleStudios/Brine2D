using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Radio button UI component for exclusive selections within a group.
/// </summary>
public class UIRadioButton : IUIComponent, IAnchoredUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 0;
    public string? Name { get; set; }

    /// <inheritdoc />
    public UIAnchor Anchor { get; set; } = UIAnchor.TopLeft;

    /// <inheritdoc />
    public Vector2 AnchorOffset { get; set; }

    /// <summary>
    /// Radio button label text. Changing this updates <see cref="Size"/> automatically.
    /// </summary>
    public string Label
    {
        get => _label;
        set
        {
            _label = value;
            RecalculateSize();
        }
    }

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
    public Color UncheckedColor { get; set; } = new Color(60, 60, 60);

    /// <summary>
    /// Checked button color.
    /// </summary>
    public Color CheckedColor { get; set; } = new Color(100, 150, 255);

    /// <summary>
    /// Hover button color.
    /// </summary>
    public Color HoverColor { get; set; } = new Color(80, 80, 80);

    /// <summary>
    /// Inner dot color when checked.
    /// </summary>
    public Color DotColor { get; set; } = Color.White;

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(150, 150, 150);

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

    /// <summary>
    /// Event fired when the mouse cursor enters the radio button bounds.
    /// </summary>
    public event Action? OnHoverEnter;

    /// <summary>
    /// Event fired when the mouse cursor leaves the radio button bounds.
    /// </summary>
    public event Action? OnHoverExit;

    /// <summary>
    /// Event fired when this radio button gains keyboard focus.
    /// </summary>
    public event Action? OnFocusGained;

    /// <summary>
    /// Event fired when this radio button loses keyboard focus.
    /// </summary>
    public event Action? OnFocusLost;

    public UITooltip? Tooltip { get; set; }

    private bool _isChecked;
    private bool _isHovered;
    private bool _isFocused;
    private string _label = string.Empty;

    /// <summary>
    /// Whether this radio button currently has keyboard focus.
    /// </summary>
    public bool IsFocused => _isFocused;

    /// <summary>
    /// Color of the focus ring when the radio button has keyboard focus.
    /// </summary>
    public Color FocusColor { get; set; } = new Color(120, 180, 255);

    public UIRadioButton(string label, UIRadioButtonGroup group, Vector2 position)
    {
        _label = label;
        Group = group ?? throw new ArgumentNullException(nameof(group));
        Position = position;
        RecalculateSize();

        Group.RegisterButton(this);
    }

    /// <summary>
    /// Optional font for label rendering (null = renderer default).
    /// </summary>
    public IFont? Font { get; set; }

    public void Update(float deltaTime)
    {
        // Radio button state is managed by UICanvas
    }

    private void RecalculateSize()
    {
        Size = new Vector2(ButtonSize + LabelSpacing + (_label.Length * 10), ButtonSize);
    }

    internal void RecalculateSize(IRenderer? renderer)
    {
        if (renderer == null)
        {
            RecalculateSize();
            return;
        }

        float labelWidth = !string.IsNullOrEmpty(_label)
            ? renderer.MeasureText(_label, new TextRenderOptions { Font = Font }).X
            : 0f;
        Size = new Vector2(ButtonSize + LabelSpacing + labelWidth, ButtonSize);
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        RecalculateSize(renderer);

        var buttonColor = _isChecked ? CheckedColor :
                         _isHovered ? HoverColor :
                         UncheckedColor;

        float radius = ButtonSize / 2;
        float centerX = Position.X + radius;
        float centerY = Position.Y + radius;

        renderer.DrawCircleFilled(centerX, centerY, radius, buttonColor);

        var borderColor = _isFocused ? FocusColor : Enabled ? BorderColor : new Color(100, 100, 100);
        float borderThickness = 2f;
        renderer.DrawCircleFilled(centerX, centerY, radius + borderThickness / 2, borderColor);
        renderer.DrawCircleFilled(centerX, centerY, radius - borderThickness / 2, buttonColor);

        if (_isChecked)
        {
            float dotRadius = radius * 0.5f;
            renderer.DrawCircleFilled(centerX, centerY, dotRadius, DotColor);
        }

        if (!string.IsNullOrEmpty(_label))
        {
            var labelX = Position.X + ButtonSize + LabelSpacing;
            var labelY = Position.Y + (ButtonSize / 2) - 8;
            var labelColor = Enabled ? LabelColor : new Color(100, 100, 100);
            renderer.DrawText(_label, labelX, labelY, new TextRenderOptions { Color = labelColor, Font = Font });
        }
    }

    public bool Contains(Vector2 screenPosition)
    {
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
        bool newHovered = hovered && Enabled;
        if (newHovered != _isHovered)
        {
            _isHovered = newHovered;
            if (_isHovered) OnHoverEnter?.Invoke();
            else OnHoverExit?.Invoke();
        }
    }

    /// <summary>
    /// Called by UICanvas to set keyboard focus on this radio button.
    /// </summary>
    internal void SetFocused(bool focused)
    {
        bool newFocused = focused && Enabled;
        if (newFocused == _isFocused) return;
        _isFocused = newFocused;
        if (_isFocused) OnFocusGained?.Invoke();
        else OnFocusLost?.Invoke();
    }

    /// <summary>
    /// Selects this radio button (deselects others in the group).
    /// <see cref="IsChecked"/> is set before <see cref="UIRadioButtonGroup.OnSelectionChanged"/> fires.
    /// </summary>
    public void Select()
    {
        if (Enabled && !_isChecked)
        {
            Group.SelectButton(this);
        }
    }
}