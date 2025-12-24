using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Slider UI component for adjusting numeric values.
/// </summary>
public class UISlider : IUIComponent
{
    public UITooltip? Tooltip { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

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
    public float MinValue { get; set; } = 0f;

    /// <summary>
    /// Maximum value.
    /// </summary>
    public float MaxValue { get; set; } = 1f;

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
    /// Event fired when value changes.
    /// </summary>
    public event Action<float>? OnValueChanged;

    private float _value;
    private bool _isDragging;
    private bool _isHovered;

    public UISlider(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
        _value = MinValue;
    }

    public void Update(float deltaTime)
    {
        // Slider state is managed by UICanvas
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        float trackHeight = Size.Y * 0.3f;
        float trackY = Position.Y + (Size.Y - trackHeight) / 2;

        // Draw track (background)
        renderer.DrawRectangle(Position.X, trackY, Size.X, trackHeight, TrackColor);

        // Draw fill (active portion)
        float fillWidth = Size.X * GetNormalizedValue();
        if (fillWidth > 0)
        {
            renderer.DrawRectangle(Position.X, trackY, fillWidth, trackHeight, FillColor);
        }

        // Draw handle (thumb)
        float handleX = Position.X + (Size.X * GetNormalizedValue()) - (HandleSize / 2);
        float handleY = Position.Y + (Size.Y - HandleSize) / 2;
        var handleColor = _isHovered || _isDragging ? HandleHoverColor : HandleColor;
        renderer.DrawRectangle(handleX, handleY, HandleSize, HandleSize, handleColor);

        // Draw value text if enabled
        if (ShowValue)
        {
            var valueText = Value.ToString(ValueFormat);
            var textX = Position.X + Size.X + 10;
            var textY = Position.Y + (Size.Y / 2) - 8;
            renderer.DrawText(valueText, textX, textY, ValueTextColor);
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

        // Calculate value from mouse position
        float relativeX = mousePosition.X - Position.X;
        float normalizedValue = Math.Clamp(relativeX / Size.X, 0f, 1f);
        float newValue = MinValue + (normalizedValue * (MaxValue - MinValue));

        // Apply step if set
        if (Step > 0)
        {
            newValue = MathF.Round(newValue / Step) * Step;
        }

        Value = newValue;
    }

    /// <summary>
    /// Returns whether the slider is currently being dragged.
    /// </summary>
    internal bool IsDragging => _isDragging;
}