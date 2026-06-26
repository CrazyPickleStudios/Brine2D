using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Checkbox UI component for toggle/boolean values.
/// </summary>
public class UICheckbox : IUIComponent, IAnchoredUIComponent
{
    public UITooltip? Tooltip { get; set; }

    /// <inheritdoc />
    public UIAnchor Anchor { get; set; } = UIAnchor.TopLeft;

    /// <inheritdoc />
    public Vector2 AnchorOffset { get; set; }

    public Vector2 Position { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 0;
    public string? Name { get; set; }

    /// <summary>
    /// Checkbox label text. Changing this updates <see cref="Size"/> automatically.
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
    /// Optional font for label rendering (null = renderer default).
    /// </summary>
    public IFont? Font { get; set; }

    /// <summary>
    /// Size of the checkbox square in pixels. Updates <see cref="Size"/> automatically.
    /// </summary>
    public float BoxSize
    {
        get => _boxSize;
        set
        {
            _boxSize = value;
            RecalculateSize();
        }
    }

    /// <summary>
    /// Hit-test size, derived from <see cref="BoxSize"/> and <see cref="Label"/>.
    /// </summary>
    public Vector2 Size { get; set; }

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
    /// Unchecked box color.
    /// </summary>
    public Color UncheckedColor { get; set; } = new Color(60, 60, 60);

    /// <summary>
    /// Checked box color.
    /// </summary>
    public Color CheckedColor { get; set; } = new Color(100, 150, 255);

    /// <summary>
    /// Hover box color.
    /// </summary>
    public Color HoverColor { get; set; } = new Color(80, 80, 80);

    /// <summary>
    /// Checkmark color.
    /// </summary>
    public Color CheckmarkColor { get; set; } = Color.White;

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(150, 150, 150);

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

    /// <summary>
    /// Event fired when the mouse cursor enters the checkbox bounds.
    /// </summary>
    public event Action? OnHoverEnter;

    /// <summary>
    /// Event fired when the mouse cursor leaves the checkbox bounds.
    /// </summary>
    public event Action? OnHoverExit;

    /// <summary>
    /// Event fired when this checkbox gains keyboard focus.
    /// </summary>
    public event Action? OnFocusGained;

    /// <summary>
    /// Event fired when this checkbox loses keyboard focus.
    /// </summary>
    public event Action? OnFocusLost;

    private bool _isChecked;
    private bool _isHovered;
    private bool _isFocused;
    private string _label = string.Empty;
    private float _boxSize = 20f;

    /// <summary>
    /// Whether this checkbox currently has keyboard focus.
    /// </summary>
    public bool IsFocused => _isFocused;

    /// <summary>
    /// Color of the focus ring drawn around the box when the checkbox has keyboard focus.
    /// </summary>
    public Color FocusColor { get; set; } = new Color(120, 180, 255);

    public UICheckbox(string label, Vector2 position)
    {
        Position = position;
        _label = label;
        _boxSize = 20f;
        RecalculateSize();
    }

    public void Update(float deltaTime)
    {
        // Checkbox state is managed by UICanvas
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        RecalculateSize(renderer);

        var boxColor = _isChecked ? CheckedColor :
                      _isHovered ? HoverColor :
                      UncheckedColor;

        renderer.DrawRectangleFilled(Position.X, Position.Y, _boxSize, _boxSize, boxColor);

        var borderColor = _isFocused ? FocusColor :
                          Enabled ? BorderColor : new Color(100, 100, 100);
        float borderThickness = 2f;
        renderer.DrawRectangleFilled(Position.X, Position.Y, _boxSize, borderThickness, borderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + _boxSize - borderThickness, _boxSize, borderThickness, borderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, _boxSize, borderColor);
        renderer.DrawRectangleFilled(Position.X + _boxSize - borderThickness, Position.Y, borderThickness, _boxSize, borderColor);

        if (_isChecked)
        {
            var checkmark = "\u2713";
            var checkOpts = new TextRenderOptions { Color = CheckmarkColor, Font = Font, LineSpacing = 1.0f };
            var checkSize = renderer.MeasureText(checkmark, checkOpts);
            var checkX = MathF.Round(Position.X + (_boxSize - checkSize.X) / 2f);
            var checkY = MathF.Round(Position.Y + (_boxSize - checkSize.Y) / 2f);
            renderer.DrawText(checkmark, checkX, checkY, checkOpts);
        }

        if (!string.IsNullOrEmpty(_label))
        {
            var labelX = MathF.Round(Position.X + _boxSize + LabelSpacing);
            var labelColor = Enabled ? LabelColor : new Color(100, 100, 100);
            var labelOpts = new TextRenderOptions { Color = labelColor, Font = Font, LineSpacing = 1.0f };
            var labelHeight = renderer.MeasureText(_label, labelOpts).Y;
            var labelY = MathF.Round(Position.Y + (_boxSize - labelHeight) / 2f);
            renderer.DrawText(_label, labelX, labelY, labelOpts);
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
    /// Called by UICanvas when mouse hovers over checkbox.
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
    /// Called by UICanvas when checkbox is clicked.
    /// </summary>
    internal void Toggle()
    {
        if (Enabled)
        {
            IsChecked = !IsChecked;
        }
    }

    /// <summary>
    /// Called by UICanvas to set keyboard focus on this checkbox.
    /// </summary>
    internal void SetFocused(bool focused)
    {
        bool newFocused = focused && Enabled;
        if (newFocused == _isFocused) return;
        _isFocused = newFocused;
        if (_isFocused) OnFocusGained?.Invoke();
        else OnFocusLost?.Invoke();
    }

    private void RecalculateSize()
    {
        // Preserve previous behavior if renderer is not available; this will be
        // recalculated with accurate measurements during Render via RecalculateSize(renderer).
        Size = new Vector2(_boxSize + LabelSpacing + (_label.Length * 10), _boxSize);
    }

    internal void RecalculateSize(IRenderer? renderer)
    {
        if (renderer == null)
        {
            RecalculateSize();
            return;
        }

        float labelWidth = 0f;
        if (!string.IsNullOrEmpty(_label))
            labelWidth = renderer.MeasureText(_label, new TextRenderOptions { Font = Font }).X;

        Size = new Vector2(_boxSize + LabelSpacing + labelWidth, _boxSize);
    }
}