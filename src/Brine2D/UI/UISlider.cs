using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Position of the label relative to a <see cref="UISlider"/>.
/// </summary>
public enum SliderLabelPosition
{
    Left,
    Right,
    Above,
    Below
}

/// <summary>
/// Slider UI component for adjusting numeric values.
/// </summary>
public class UISlider : IUIComponent, IAnchoredUIComponent
{
    public UITooltip? Tooltip { get; set; }

    /// <inheritdoc />
    public UIAnchor Anchor { get; set; } = UIAnchor.TopLeft;

    /// <inheritdoc />
    public Vector2 AnchorOffset { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 0;
    public string? Name { get; set; }

    /// <summary>
    /// Current value (between MinValue and MaxValue).
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            var clampedValue = Math.Clamp(value, MinValue, MaxValue);
            if (Math.Abs(_value - clampedValue) > 0.0001f)
            {
                _value = clampedValue;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Minimum value.
    /// </summary>
    public float MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            if (_minValue <= _maxValue)
                _value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    /// <summary>
    /// Maximum value.
    /// </summary>
    public float MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            if (_minValue <= _maxValue)
                _value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    /// <summary>
    /// Step size for value increments (0 = continuous).
    /// </summary>
    public float Step { get; set; } = 0f;

    /// <summary>
    /// Track color (background bar).
    /// </summary>
    public Color TrackColor { get; set; } = new Color(60, 60, 60);

    /// <summary>
    /// Fill color (filled portion).
    /// </summary>
    public Color FillColor { get; set; } = new Color(100, 150, 255);

    /// <summary>
    /// Handle (thumb) color.
    /// </summary>
    public Color HandleColor { get; set; } = new Color(200, 200, 200);

    /// <summary>
    /// Handle color when hovered.
    /// </summary>
    public Color HandleHoverColor { get; set; } = new Color(255, 255, 255);

    /// <summary>
    /// Handle size in pixels.
    /// </summary>
    public float HandleSize { get; set; } = 16f;

    /// <summary>
    /// Whether to show the value as text.
    /// </summary>
    public bool ShowValue { get; set; } = true;

    /// <summary>
    /// Text color for value display.
    /// </summary>
    public Color ValueTextColor { get; set; } = Color.White;

    /// <summary>
    /// Value display format (e.g., "0.00" for 2 decimal places).
    /// </summary>
    public string ValueFormat { get; set; } = "0.00";

    /// <summary>
    /// Whether the slider moves horizontally or vertically.
    /// </summary>
    public SliderOrientation Orientation { get; set; } = SliderOrientation.Horizontal;

    /// <summary>
    /// Event fired when value changes.
    /// </summary>
    public event Action<float>? OnValueChanged;

    /// <summary>
    /// Event fired when this slider gains keyboard focus.
    /// </summary>
    public event Action? OnFocusGained;

    /// <summary>
    /// Event fired when this slider loses keyboard focus.
    /// </summary>
    public event Action? OnFocusLost;

    private float _value;
    private float _minValue = 0f;
    private float _maxValue = 1f;
    private bool _isDragging;
    private bool _isHovered;
    private bool _isFocused;

    /// <summary>
    /// Whether this slider currently has keyboard focus.
    /// </summary>
    public bool IsFocused => _isFocused;

    /// <summary>
    /// Color of the focus ring drawn around the slider when it has keyboard focus.
    /// </summary>
    public Color FocusColor { get; set; } = new Color(120, 180, 255);

    /// <summary>
    /// Optional label text rendered adjacent to the slider track.
    /// Empty or null disables label rendering.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Where the label is drawn relative to the slider track. Defaults to <see cref="SliderLabelPosition.Left"/>.
    /// </summary>
    public SliderLabelPosition LabelPosition { get; set; } = SliderLabelPosition.Left;

    /// <summary>
    /// Label text color.
    /// </summary>
    public Color LabelColor { get; set; } = Color.White;

    /// <summary>
    /// Optional font for the label (null = renderer default).
    /// </summary>
    public IFont? LabelFont { get; set; }

    /// <summary>
    /// Gap in pixels between the label and the slider track.
    /// </summary>
    public float LabelSpacing { get; set; } = 8f;

    public UISlider(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
        _value = _minValue;
    }

    public void Update(float deltaTime)
    {
        // Slider state is managed by UICanvas
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        if (Orientation == SliderOrientation.Vertical)
            RenderVertical(renderer);
        else
            RenderHorizontal(renderer);
    }

    private void RenderHorizontal(IRenderer renderer)
    {
        float trackHeight = Size.Y * 0.3f;
        float trackY = Position.Y + (Size.Y - trackHeight) / 2;

        renderer.DrawRectangleFilled(Position.X, trackY, Size.X, trackHeight, TrackColor);

        float fillWidth = Size.X * GetNormalizedValue();
        if (fillWidth > 0)
            renderer.DrawRectangleFilled(Position.X, trackY, fillWidth, trackHeight, FillColor);

        float handleX = Position.X + (Size.X * GetNormalizedValue()) - (HandleSize / 2);
        float handleY = Position.Y + (Size.Y - HandleSize) / 2;
        var handleColor = _isHovered || _isDragging ? HandleHoverColor : HandleColor;
        renderer.DrawRectangleFilled(handleX, handleY, HandleSize, HandleSize, handleColor);

        if (_isFocused)
        {
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, 2f, FocusColor);
            renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - 2f, Size.X, 2f, FocusColor);
            renderer.DrawRectangleFilled(Position.X, Position.Y, 2f, Size.Y, FocusColor);
            renderer.DrawRectangleFilled(Position.X + Size.X - 2f, Position.Y, 2f, Size.Y, FocusColor);
        }

        if (ShowValue)
        {
            var valueText = Value.ToString(ValueFormat);
            var opts = new TextRenderOptions { Color = ValueTextColor, LineSpacing = 1.0f };
            var valueSize = renderer.MeasureText(valueText, opts);
            var textX = MathF.Round(Position.X + Size.X + 10);
            var textY = MathF.Round(Position.Y + (Size.Y - valueSize.Y) / 2f);
            renderer.DrawText(valueText, textX, textY, opts);
        }

        DrawLabel(renderer);
    }

    private void RenderVertical(IRenderer renderer)
    {
        float trackWidth = Size.X * 0.3f;
        float trackX = Position.X + (Size.X - trackWidth) / 2;

        renderer.DrawRectangleFilled(trackX, Position.Y, trackWidth, Size.Y, TrackColor);

        // For vertical: value 1.0 = top, value 0.0 = bottom (natural fader feel).
        float normalizedFromBottom = GetNormalizedValue();
        float fillHeight = Size.Y * normalizedFromBottom;
        if (fillHeight > 0)
            renderer.DrawRectangleFilled(trackX, Position.Y + Size.Y - fillHeight, trackWidth, fillHeight, FillColor);

        float handleX = Position.X + (Size.X - HandleSize) / 2;
        float handleY = Position.Y + (Size.Y - Size.Y * normalizedFromBottom) - (HandleSize / 2);
        var handleColor = _isHovered || _isDragging ? HandleHoverColor : HandleColor;
        renderer.DrawRectangleFilled(handleX, handleY, HandleSize, HandleSize, handleColor);

        if (_isFocused)
        {
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, 2f, FocusColor);
            renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - 2f, Size.X, 2f, FocusColor);
            renderer.DrawRectangleFilled(Position.X, Position.Y, 2f, Size.Y, FocusColor);
            renderer.DrawRectangleFilled(Position.X + Size.X - 2f, Position.Y, 2f, Size.Y, FocusColor);
        }

        if (ShowValue)
        {
            var valueText = Value.ToString(ValueFormat);
            var opts = new TextRenderOptions { Color = ValueTextColor, LineSpacing = 1.0f };
            var valueSize = renderer.MeasureText(valueText, opts);
            var textX = MathF.Round(Position.X + (Size.X - valueSize.X) / 2f);
            var textY = MathF.Round(Position.Y + Size.Y + 6);
            renderer.DrawText(valueText, textX, textY, opts);
        }

        DrawLabel(renderer);
    }

    private void DrawLabel(IRenderer renderer)
    {
        if (string.IsNullOrEmpty(Label)) return;

        var opts = new TextRenderOptions { Color = LabelColor, Font = LabelFont };
        var labelSize = renderer.MeasureText(Label, opts);

        float lx, ly;
        switch (LabelPosition)
        {
            case SliderLabelPosition.Right:
                lx = Position.X + Size.X + LabelSpacing;
                ly = Position.Y + (Size.Y - labelSize.Y) / 2f;
                break;
            case SliderLabelPosition.Above:
                lx = Position.X + (Size.X - labelSize.X) / 2f;
                ly = Position.Y - labelSize.Y - LabelSpacing;
                break;
            case SliderLabelPosition.Below:
                lx = Position.X + (Size.X - labelSize.X) / 2f;
                ly = Position.Y + Size.Y + LabelSpacing;
                break;
            default: // Left
                lx = Position.X - labelSize.X - LabelSpacing;
                ly = Position.Y + (Size.Y - labelSize.Y) / 2f;
                break;
        }

        renderer.DrawText(Label, lx, ly, opts);
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    /// <summary>
    /// Gets the normalized value (0.0 to 1.0).
    /// </summary>
    private float GetNormalizedValue()
    {
        if (Math.Abs(MaxValue - MinValue) < 0.0001f)
            return 0f;

        return (_value - MinValue) / (MaxValue - MinValue);
    }

    /// <summary>
    /// Called by UICanvas when mouse hovers over slider.
    /// </summary>
    internal void SetHovered(bool hovered)
    {
        _isHovered = hovered && Enabled;
    }

    /// <summary>
    /// Called by UICanvas when mouse drag starts.
    /// </summary>
    internal void StartDrag()
    {
        if (Enabled)
        {
            _isDragging = true;
        }
    }

    /// <summary>
    /// Called by UICanvas when mouse drag ends.
    /// </summary>
    internal void EndDrag()
    {
        _isDragging = false;
    }

    /// <summary>
    /// Called by UICanvas during mouse drag.
    /// </summary>
    internal void UpdateDrag(Vector2 mousePosition)
    {
        if (!_isDragging || !Enabled) return;

        float normalizedValue;

        if (Orientation == SliderOrientation.Vertical)
        {
            // Top of the track = MaxValue, bottom = MinValue.
            float relativeY = mousePosition.Y - Position.Y;
            normalizedValue = 1f - Math.Clamp(relativeY / Size.Y, 0f, 1f);
        }
        else
        {
            float relativeX = mousePosition.X - Position.X;
            normalizedValue = Math.Clamp(relativeX / Size.X, 0f, 1f);
        }

        float newValue = MinValue + (normalizedValue * (MaxValue - MinValue));

        if (Step > 0)
            newValue = MathF.Round(newValue / Step) * Step;

        Value = newValue;
    }

    /// <summary>
    /// Returns whether the slider is currently being dragged.
    /// </summary>
    internal bool IsDragging => _isDragging;

    /// <summary>
    /// Called by UICanvas to set keyboard focus on this slider.
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
    /// Nudges the slider value by one step (or 1% of the range when <see cref="Step"/> is 0).
    /// Called by UICanvas when the focused slider receives an arrow-key press.
    /// </summary>
    internal void NudgeValue(float direction)
    {
        if (!Enabled) return;

        float step = Step > 0 ? Step : (MaxValue - MinValue) * 0.01f;
        Value = Math.Clamp(_value + direction * step, MinValue, MaxValue);
    }
}